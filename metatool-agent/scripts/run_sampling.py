"""CLI: build sampled_data.csv from MetaTool source files.

Usage:
    python -m scripts.run_sampling \
        --tools data/big_tool_des.json \
        --clean data/all_clean_data.csv \
        --out   data/sampled_data.csv \
        --per-tool 5
"""

from __future__ import annotations

import argparse
from pathlib import Path

from src.sampling import build_sampled_dataset


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--tools", type=Path, default=Path("data/big_tool_des.json"))
    parser.add_argument("--clean", type=Path, default=Path("data/all_clean_data.csv"))
    parser.add_argument("--out", type=Path, default=Path("data/sampled_data.csv"))
    parser.add_argument("--per-tool", type=int, default=5)
    parser.add_argument("--negatives", type=int, default=None,
                        help="Number of non-tool queries (default: balanced with positives)")
    parser.add_argument("--seed", type=int, default=42)
    args = parser.parse_args()

    build_sampled_dataset(
        big_tool_des_path=args.tools,
        all_clean_data_path=args.clean,
        out_path=args.out,
        per_tool=args.per_tool,
        n_negatives=args.negatives,
        seed=args.seed,
    )


if __name__ == "__main__":
    main()
