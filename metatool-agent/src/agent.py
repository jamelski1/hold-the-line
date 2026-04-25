"""Agent loop: render prompt -> generate -> parse -> normalize."""

from __future__ import annotations

import json
import re
from dataclasses import dataclass, asdict
from pathlib import Path
from typing import Optional

import pandas as pd
from rapidfuzz import fuzz, process

from .llm_clients import LLMClient
from .prompts import render_prompt

JSON_BLOCK_RE = re.compile(r"\{.*?\}", re.DOTALL)


@dataclass
class AgentPrediction:
    query: str
    raw_response: str
    tool_needed: Optional[bool]
    tool_name: Optional[str]
    reasoning: Optional[str]
    parse_status: str  # "ok" | "regex_recovered" | "malformed"
    normalization: str  # "exact" | "fuzzy" | "none" | "unknown_tool"
    fuzzy_score: Optional[float]


def _try_parse_json(text: str) -> tuple[Optional[dict], str]:
    """Return (parsed, status). status in {ok, regex_recovered, malformed}."""
    try:
        return json.loads(text), "ok"
    except Exception:
        pass

    # Strip markdown fences if present.
    fenced = re.sub(r"```(?:json)?", "", text).strip("` \n")
    try:
        return json.loads(fenced), "ok"
    except Exception:
        pass

    # Grab the first {...} block as a final fallback.
    match = JSON_BLOCK_RE.search(text)
    if match:
        try:
            return json.loads(match.group(0)), "regex_recovered"
        except Exception:
            return None, "malformed"
    return None, "malformed"


def _normalize_tool(
    raw_name: Optional[str],
    tool_names: list[str],
    fuzzy_threshold: float = 90.0,
) -> tuple[Optional[str], str, Optional[float]]:
    """Map raw model output to one of `tool_names`.

    Returns (canonical_name, mode, fuzzy_score). mode in {exact, fuzzy, unknown_tool, none}.
    """
    if raw_name is None or (isinstance(raw_name, str) and raw_name.strip() == ""):
        return None, "none", None

    raw = str(raw_name).strip()
    lowered = {t.lower(): t for t in tool_names}

    # Exact, then case-insensitive exact.
    if raw in tool_names:
        return raw, "exact", None
    if raw.lower() in lowered:
        return lowered[raw.lower()], "exact", None

    # Fuzzy fallback (logged separately).
    match = process.extractOne(raw, tool_names, scorer=fuzz.ratio)
    if match is not None:
        canonical, score, _ = match
        if score >= fuzzy_threshold:
            return canonical, "fuzzy", float(score)

    return None, "unknown_tool", None


def predict_one(
    client: LLMClient,
    query: str,
    tools: list[dict],
    max_tokens: int = 256,
) -> AgentPrediction:
    prompt = render_prompt(query, tools)
    raw = client.generate(prompt, max_tokens=max_tokens)

    parsed, parse_status = _try_parse_json(raw)
    if parsed is None:
        return AgentPrediction(
            query=query,
            raw_response=raw,
            tool_needed=None,
            tool_name=None,
            reasoning=None,
            parse_status="malformed",
            normalization="none",
            fuzzy_score=None,
        )

    tool_needed = parsed.get("tool_needed")
    if isinstance(tool_needed, str):
        tool_needed = tool_needed.strip().lower() in ("true", "yes", "1")

    raw_tool_name = parsed.get("tool_name")
    canonical, mode, fuzzy_score = _normalize_tool(
        raw_tool_name, [t["name"] for t in tools]
    )

    return AgentPrediction(
        query=query,
        raw_response=raw,
        tool_needed=bool(tool_needed) if tool_needed is not None else None,
        tool_name=canonical,
        reasoning=str(parsed.get("reasoning", "") or "")[:500],
        parse_status=parse_status,
        normalization=mode,
        fuzzy_score=fuzzy_score,
    )


def run_agent(
    client: LLMClient,
    sampled_df: pd.DataFrame,
    tools: list[dict],
    out_predictions_path: Path,
    out_malformed_path: Optional[Path] = None,
    max_tokens: int = 256,
    progress_every: int = 50,
) -> pd.DataFrame:
    """Run the agent over all queries in `sampled_df`. Writes predictions CSV."""
    rows: list[dict] = []
    malformed_log: list[dict] = []

    for i, row in enumerate(sampled_df.itertuples(index=False), start=1):
        pred = predict_one(client, row.query, tools, max_tokens=max_tokens)

        rows.append({
            "query": row.query,
            "gold_tool": row.gold_tool if isinstance(row.gold_tool, str) else "",
            "requires_tool": bool(row.requires_tool),
            "pred_tool_needed": pred.tool_needed,
            "pred_tool_name": pred.tool_name,
            "reasoning": pred.reasoning,
            "parse_status": pred.parse_status,
            "normalization": pred.normalization,
            "fuzzy_score": pred.fuzzy_score,
            "raw_response": pred.raw_response,
        })

        if pred.parse_status == "malformed":
            malformed_log.append({
                "query": row.query,
                "raw_response": pred.raw_response,
            })

        if i % progress_every == 0:
            print(f"[agent] {i}/{len(sampled_df)} queries done")

    out_predictions_path.parent.mkdir(parents=True, exist_ok=True)
    df = pd.DataFrame(rows)
    df.to_csv(out_predictions_path, index=False)
    print(f"[agent] wrote predictions -> {out_predictions_path}")

    if out_malformed_path is not None and malformed_log:
        out_malformed_path.parent.mkdir(parents=True, exist_ok=True)
        with open(out_malformed_path, "w", encoding="utf-8") as f:
            for entry in malformed_log:
                f.write(json.dumps(entry, ensure_ascii=False) + "\n")
        print(f"[agent] {len(malformed_log)} malformed responses -> {out_malformed_path}")

    return df
