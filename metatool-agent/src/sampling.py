"""Sample queries from MetaTool's all_clean_data.csv and synthesize negatives.

Public entry point: build_sampled_dataset(...)
"""

from __future__ import annotations

import json
import random
from pathlib import Path
from typing import Iterable

import numpy as np
import pandas as pd

from . import RANDOM_SEED


def _seed_everything(seed: int = RANDOM_SEED) -> None:
    random.seed(seed)
    np.random.seed(seed)


def load_tool_list(big_tool_des_path: Path) -> list[dict]:
    """Load the 48-tool descriptor list. Each item has at least 'name' and 'description'."""
    with open(big_tool_des_path, "r", encoding="utf-8") as f:
        raw = json.load(f)

    # The MetaTool repo ships big_tool_des.json as either a list of dicts or a
    # dict keyed by tool name. Normalize to list[{name, description}].
    if isinstance(raw, dict):
        tools = [{"name": k, "description": _coerce_description(v)} for k, v in raw.items()]
    elif isinstance(raw, list):
        tools = []
        for entry in raw:
            if isinstance(entry, dict):
                name = entry.get("name") or entry.get("tool") or entry.get("tool_name")
                desc = entry.get("description") or entry.get("desc") or entry.get("summary")
                if name is None:
                    raise ValueError(f"Tool entry missing name field: {entry}")
                tools.append({"name": str(name).strip(), "description": _coerce_description(desc)})
            else:
                raise ValueError(f"Unexpected tool entry type: {type(entry)}")
    else:
        raise ValueError(f"Unexpected big_tool_des.json root type: {type(raw)}")

    if len(tools) == 0:
        raise ValueError("Loaded 0 tools from big_tool_des.json")
    return tools


def _coerce_description(value) -> str:
    if value is None:
        return ""
    if isinstance(value, str):
        return value.strip()
    if isinstance(value, dict):
        # Some MetaTool variants nest description under another key.
        for k in ("description", "desc", "summary", "text"):
            if k in value and isinstance(value[k], str):
                return value[k].strip()
        return json.dumps(value, ensure_ascii=False)
    return str(value)


def load_clean_data(all_clean_data_path: Path) -> pd.DataFrame:
    """Load all_clean_data.csv and normalize column names to {query, tool}."""
    df = pd.read_csv(all_clean_data_path)
    df = df.rename(columns={c: c.strip().lower() for c in df.columns})

    # Try to map common alternative column names.
    query_candidates = ["query", "user_query", "question", "input"]
    tool_candidates = ["tool", "gold_tool", "label", "tool_name", "category"]

    qcol = next((c for c in query_candidates if c in df.columns), None)
    tcol = next((c for c in tool_candidates if c in df.columns), None)
    if qcol is None or tcol is None:
        raise ValueError(
            f"all_clean_data.csv missing expected columns. Found: {list(df.columns)}. "
            "Expected one of {query_candidates} and one of {tool_candidates}."
        )

    df = df[[qcol, tcol]].rename(columns={qcol: "query", tcol: "tool"})
    df["query"] = df["query"].astype(str).str.strip()
    df["tool"] = df["tool"].astype(str).str.strip()
    df = df.dropna(subset=["query", "tool"])
    df = df[(df["query"] != "") & (df["tool"] != "")]
    return df.reset_index(drop=True)


def sample_positives(
    df: pd.DataFrame,
    tools: list[dict],
    per_tool: int,
    seed: int = RANDOM_SEED,
) -> pd.DataFrame:
    """Sample `per_tool` queries for each of the 48 tools.

    Tools with fewer than `per_tool` rows in df are sampled with replacement=False
    up to whatever is available, and a warning is printed.
    """
    _seed_everything(seed)
    rng = np.random.default_rng(seed)

    rows: list[dict] = []
    tool_names = {t["name"] for t in tools}
    df_in_scope = df[df["tool"].isin(tool_names)].copy()

    missing_tools = sorted(tool_names - set(df_in_scope["tool"].unique()))
    if missing_tools:
        print(f"[sampling] WARNING: {len(missing_tools)} tools have 0 rows in clean data: {missing_tools}")

    for tool in sorted(tool_names):
        candidates = df_in_scope[df_in_scope["tool"] == tool]
        n = min(per_tool, len(candidates))
        if n == 0:
            continue
        if n < per_tool:
            print(f"[sampling] WARNING: tool '{tool}' only has {n} rows (< {per_tool})")
        idx = rng.choice(candidates.index.to_numpy(), size=n, replace=False)
        for i in idx:
            rows.append({
                "query": candidates.loc[i, "query"],
                "gold_tool": candidates.loc[i, "tool"],
                "requires_tool": True,
            })
    return pd.DataFrame(rows)


# Curated non-tool queries: knowledge/reasoning that is clearly outside the
# scope of any of the 48 MetaTool tools (which are productivity/API-style
# tools like calendar, email, weather, code-interpreter, image-gen, etc.).
# Hand-authored to avoid an external dataset dependency.
NEGATIVE_QUERIES: list[str] = [
    "Who wrote the play Hamlet?",
    "What is the capital of Australia?",
    "Name the largest planet in our solar system.",
    "What year did World War II end?",
    "Who painted the Mona Lisa?",
    "What is the chemical symbol for gold?",
    "How many continents are there?",
    "What is the longest river in the world?",
    "Who discovered penicillin?",
    "What language is primarily spoken in Brazil?",
    "What is the boiling point of water in Celsius?",
    "Who was the first president of the United States?",
    "What is photosynthesis?",
    "Define the word 'serendipity'.",
    "What does DNA stand for?",
    "Who is the author of '1984'?",
    "What is the speed of light in a vacuum?",
    "Name the smallest country in the world.",
    "What is the Pythagorean theorem?",
    "Who composed the Ninth Symphony?",
    "Explain what gravity is in one sentence.",
    "What is a synonym for 'happy'?",
    "What is the past tense of the verb 'to run'?",
    "Define the term 'metaphor'.",
    "Who is credited with inventing the telephone?",
    "What is the tallest mountain on Earth?",
    "Name the four oceans.",
    "What is the freezing point of water in Fahrenheit?",
    "Who wrote 'Pride and Prejudice'?",
    "What is the largest desert in the world?",
    "Define 'democracy'.",
    "What is the square root of 144?",
    "Who developed the theory of relativity?",
    "Name the seven colors of the rainbow.",
    "What does the acronym NASA stand for?",
    "Who is the Greek god of the sea?",
    "What is mitosis?",
    "Name the three branches of the U.S. government.",
    "What is the difference between weather and climate?",
    "Who wrote 'The Odyssey'?",
    "Define 'osmosis' in biology.",
    "What is the capital of Japan?",
    "Who painted the ceiling of the Sistine Chapel?",
    "What is the largest mammal?",
    "Name the inventor of the light bulb.",
    "What does the word 'ephemeral' mean?",
    "What is the formula for the area of a circle?",
    "Who wrote 'Romeo and Juliet'?",
    "What is the smallest prime number?",
    "Name the planet known as the Red Planet.",
    "What is the main ingredient in bread?",
    "Who wrote 'To Kill a Mockingbird'?",
    "What is the currency of the United Kingdom?",
    "Define 'altruism'.",
    "Who is the founder of Microsoft?",
    "What is the largest organ in the human body?",
    "Name the bones that make up the human skull.",
    "What does 'HTTP' stand for?",
    "Who wrote 'The Great Gatsby'?",
    "What is the capital of Canada?",
    "Define 'photosynthesis' in one sentence.",
    "What is the speed of sound in air?",
    "Who is the current Dalai Lama? (no real-time lookup needed for general definition)",
    "What is an antonym of 'brave'?",
    "Define 'inflation' in economics.",
    "Who wrote 'Don Quixote'?",
    "Name the deepest ocean trench.",
    "What is the chemical formula for water?",
    "Who developed the polio vaccine?",
    "What is the difference between a simile and a metaphor?",
    "Define 'sovereignty'.",
    "Who wrote 'War and Peace'?",
    "Name the three states of matter.",
    "What is the capital of Egypt?",
    "What is the meaning of the word 'ubiquitous'?",
    "Who wrote 'The Catcher in the Rye'?",
    "What is the formula for kinetic energy?",
    "Name the largest island in the world.",
    "Who was Cleopatra?",
    "Define 'entropy' in thermodynamics.",
    "What is the capital of Russia?",
    "Who wrote 'Moby-Dick'?",
    "Name the inventor of the World Wide Web.",
    "What is the Fibonacci sequence?",
    "Who is the Roman god of war?",
    "What is the largest bird in the world?",
    "Define 'mitochondria'.",
    "What is the capital of Argentina?",
    "Who wrote 'Crime and Punishment'?",
    "What is Newton's third law of motion?",
    "Name the gas most abundant in Earth's atmosphere.",
    "Who is considered the father of modern physics?",
    "What is the meaning of 'laissez-faire'?",
    "Who wrote 'Brave New World'?",
    "Name the official language of China.",
    "What is the capital of South Africa?",
    "Define 'biodiversity'.",
    "Who invented the printing press?",
    "What is the smallest unit of life?",
    "Name the most populated country in the world.",
    "What is the difference between a noun and a verb?",
    "Who wrote 'Wuthering Heights'?",
    "Define 'recursion' in computer science.",
    "What is the area of a triangle with base 4 and height 3?",
    "Who is the author of 'The Iliad'?",
    "What is the capital of Italy?",
    "Define 'symbiosis'.",
    "Who painted 'Starry Night'?",
    "Name the chemical element with symbol Fe.",
    "What is the slowest land animal?",
    "Who wrote 'Frankenstein'?",
    "Define 'meritocracy'.",
    "What is the perimeter of a square with side 5?",
    "Name the longest bone in the human body.",
    "Who is known as the father of geometry?",
    "What is the capital of India?",
    "Define 'inertia'.",
    "Who wrote 'Anna Karenina'?",
    "Name the smallest planet in our solar system.",
    "What is the meaning of 'pragmatic'?",
    "Who painted 'The Persistence of Memory'?",
    "What is the capital of Spain?",
    "Define 'oligarchy'.",
    "Who wrote 'Les Misérables'?",
    "Name the largest volcano in the solar system.",
    "What is the difference between mass and weight?",
    "Who is the author of 'A Tale of Two Cities'?",
    "Define 'allegory'.",
    "What is the capital of Germany?",
    "Who wrote 'Beloved'?",
]


def sample_negatives(n: int, seed: int = RANDOM_SEED) -> pd.DataFrame:
    """Sample `n` curated non-tool queries.

    If n > len(NEGATIVE_QUERIES) we sample with replacement and emit a warning;
    in practice we should keep n <= 240.
    """
    _seed_everything(seed)
    rng = np.random.default_rng(seed + 1)  # different stream than positives
    pool = NEGATIVE_QUERIES.copy()
    if n > len(pool):
        print(f"[sampling] WARNING: requested {n} negatives but only {len(pool)} curated; sampling with replacement")
        idx = rng.choice(len(pool), size=n, replace=True)
    else:
        idx = rng.choice(len(pool), size=n, replace=False)
    return pd.DataFrame([
        {"query": pool[i], "gold_tool": "", "requires_tool": False} for i in idx
    ])


def build_sampled_dataset(
    big_tool_des_path: Path,
    all_clean_data_path: Path,
    out_path: Path,
    per_tool: int = 5,
    n_negatives: int | None = None,
    seed: int = RANDOM_SEED,
) -> pd.DataFrame:
    """End-to-end: load tools + clean data, sample positives + negatives, write CSV."""
    tools = load_tool_list(big_tool_des_path)
    df = load_clean_data(all_clean_data_path)

    pos = sample_positives(df, tools, per_tool=per_tool, seed=seed)
    if n_negatives is None:
        n_negatives = len(pos)  # balanced
    neg = sample_negatives(n_negatives, seed=seed)

    sampled = pd.concat([pos, neg], ignore_index=True)
    # Shuffle so order doesn't trivially leak the label.
    sampled = sampled.sample(frac=1.0, random_state=seed).reset_index(drop=True)

    out_path.parent.mkdir(parents=True, exist_ok=True)
    sampled.to_csv(out_path, index=False)

    print(f"[sampling] wrote {len(sampled)} rows ({len(pos)} positive / {len(neg)} negative) -> {out_path}")
    return sampled
