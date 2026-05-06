"""Compute per-model metrics on the agent's predictions."""

from __future__ import annotations

import json
from pathlib import Path

import pandas as pd
from sklearn.metrics import (
    accuracy_score,
    precision_recall_fscore_support,
    confusion_matrix,
)


def compute_metrics(predictions_df: pd.DataFrame) -> dict:
    df = predictions_df.copy()

    # Treat malformed parses as "tool_needed=False" predictions for binary metrics
    # (a defensive default: a malformed agent shouldn't get credit for tool use).
    # We also report a separate malformed_rate.
    n_total = len(df)
    n_malformed = int((df["parse_status"] == "malformed").sum())
    n_regex_recovered = int((df["parse_status"] == "regex_recovered").sum())

    df["pred_tool_needed_filled"] = df["pred_tool_needed"].fillna(False).astype(bool)

    y_true = df["requires_tool"].astype(bool).to_numpy()
    y_pred = df["pred_tool_needed_filled"].to_numpy()

    accuracy = accuracy_score(y_true, y_pred)
    precision, recall, f1, _ = precision_recall_fscore_support(
        y_true, y_pred, labels=[True], average="binary", zero_division=0
    )
    tn, fp, fn, tp = confusion_matrix(y_true, y_pred, labels=[False, True]).ravel()

    # CSR is reported two ways:
    #
    #  csr_paper:   correct_tool_selections / all gold-positive samples.
    #               Matches the MetaTool paper / assignment definition. A query
    #               where gold required a tool but the model said "no tool"
    #               counts as an incorrect selection.
    #
    #  csr_attempted: correct_tool_selections / (gold-positive AND model said yes).
    #                 More diagnostic: "when the model decides to invoke a tool,
    #                 how often does it pick the right one?" Decouples tool-
    #                 selection ability from binary-classification ability.
    #
    # Both are reported in strict (exact-match) and with-fuzzy variants.
    n_gold_positive = int(df["requires_tool"].sum())
    tool_attempted = df[df["requires_tool"] & df["pred_tool_needed_filled"]]
    n_csr_attempted = len(tool_attempted)

    if n_csr_attempted == 0:
        csr_attempted_strict = float("nan")
        csr_attempted_with_fuzzy = float("nan")
        n_correct_strict = 0
        n_correct_with_fuzzy = 0
    else:
        gold = tool_attempted["gold_tool"].astype(str).str.strip()
        pred = tool_attempted["pred_tool_name"].fillna("").astype(str).str.strip()
        normalization = tool_attempted["normalization"].astype(str)

        exact_correct = (gold == pred) & (normalization == "exact")
        any_correct = (gold == pred) & (normalization.isin(["exact", "fuzzy"]))

        n_correct_strict = int(exact_correct.sum())
        n_correct_with_fuzzy = int(any_correct.sum())
        csr_attempted_strict = float(n_correct_strict / n_csr_attempted)
        csr_attempted_with_fuzzy = float(n_correct_with_fuzzy / n_csr_attempted)

    if n_gold_positive == 0:
        csr_paper_strict = float("nan")
        csr_paper_with_fuzzy = float("nan")
    else:
        csr_paper_strict = float(n_correct_strict / n_gold_positive)
        csr_paper_with_fuzzy = float(n_correct_with_fuzzy / n_gold_positive)

    n_fuzzy = int((df["normalization"] == "fuzzy").sum())
    n_unknown_tool = int((df["normalization"] == "unknown_tool").sum())

    return {
        "n_total": n_total,
        "n_positive_gold": int(y_true.sum()),
        "n_negative_gold": int((~y_true).sum()),
        "accuracy": float(accuracy),
        "precision": float(precision),
        "recall": float(recall),
        "f1": float(f1),
        # MetaTool-paper CSR: denominator = all gold positives (a missed positive
        # counts as an incorrect tool selection).
        "csr": csr_paper_strict,
        "csr_with_fuzzy": csr_paper_with_fuzzy,
        # Diagnostic CSR: denominator = positives where the model attempted a tool.
        # Decouples tool-selection ability from binary-classification ability.
        "csr_attempted": csr_attempted_strict,
        "csr_attempted_with_fuzzy": csr_attempted_with_fuzzy,
        "csr_attempted_n": n_csr_attempted,
        "n_correct_tool_selections": n_correct_strict,
        "confusion_matrix": {"tn": int(tn), "fp": int(fp), "fn": int(fn), "tp": int(tp)},
        "malformed_rate": n_malformed / n_total if n_total else 0.0,
        "regex_recovered_rate": n_regex_recovered / n_total if n_total else 0.0,
        "fuzzy_match_count": n_fuzzy,
        "unknown_tool_count": n_unknown_tool,
    }


def evaluate_predictions(
    predictions_path: Path,
    metrics_out_path: Path,
    model_label: str,
) -> dict:
    df = pd.read_csv(predictions_path)
    metrics = compute_metrics(df)
    metrics["model"] = model_label

    metrics_out_path.parent.mkdir(parents=True, exist_ok=True)
    with open(metrics_out_path, "w", encoding="utf-8") as f:
        json.dump(metrics, f, indent=2)
    print(f"[evaluate] wrote metrics -> {metrics_out_path}")
    return metrics


def write_summary(metrics_list: list[dict], out_path: Path) -> pd.DataFrame:
    """One-row-per-model summary CSV."""
    cols = [
        "model",
        "n_total",
        "accuracy",
        "precision",
        "recall",
        "f1",
        "csr",
        "csr_with_fuzzy",
        "csr_attempted",
        "csr_attempted_n",
        "n_correct_tool_selections",
        "malformed_rate",
        "fuzzy_match_count",
        "unknown_tool_count",
    ]
    rows = [{c: m.get(c) for c in cols} for m in metrics_list]
    df = pd.DataFrame(rows)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    df.to_csv(out_path, index=False)
    print(f"[evaluate] wrote summary -> {out_path}")
    return df
