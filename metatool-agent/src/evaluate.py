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

    # CSR computed two ways: strict (exact-match only) and including-fuzzy.
    tool_required = df[df["requires_tool"] & df["pred_tool_needed_filled"]]
    n_csr_eligible = len(tool_required)

    if n_csr_eligible == 0:
        csr_strict = float("nan")
        csr_with_fuzzy = float("nan")
    else:
        gold = tool_required["gold_tool"].astype(str).str.strip()
        pred = tool_required["pred_tool_name"].fillna("").astype(str).str.strip()
        normalization = tool_required["normalization"].astype(str)

        exact_correct = (gold == pred) & (normalization == "exact")
        any_correct = (gold == pred) & (normalization.isin(["exact", "fuzzy"]))

        csr_strict = float(exact_correct.sum() / n_csr_eligible)
        csr_with_fuzzy = float(any_correct.sum() / n_csr_eligible)

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
        "csr_strict": csr_strict,
        "csr_with_fuzzy": csr_with_fuzzy,
        "csr_eligible": n_csr_eligible,
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
        "csr_strict",
        "csr_with_fuzzy",
        "csr_eligible",
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
