"""CLI: run all 3 models on sampled_data.csv and write per-model + summary metrics.

Usage:
    python -m scripts.run_evaluation \
        --tools data/big_tool_des.json \
        --sampled data/sampled_data.csv \
        --results-dir results/

Each model is loaded, run, and unloaded sequentially so a single GPU can
handle all three. Skip a model with --skip <label>.
"""

from __future__ import annotations

import argparse
from pathlib import Path

import pandas as pd

from src.agent import run_agent
from src.evaluate import evaluate_predictions, write_summary
from src.llm_clients import ALL_SPECS, build_client
from src.sampling import load_tool_list


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--tools", type=Path, default=Path("data/big_tool_des.json"))
    parser.add_argument("--sampled", type=Path, default=Path("data/sampled_data.csv"))
    parser.add_argument("--results-dir", type=Path, default=Path("results"))
    parser.add_argument("--max-tokens", type=int, default=256)
    parser.add_argument("--skip", action="append", default=[],
                        help="Model label(s) to skip. Repeat for multiple.")
    args = parser.parse_args()

    tools = load_tool_list(args.tools)
    sampled = pd.read_csv(args.sampled)
    print(f"Loaded {len(tools)} tools and {len(sampled)} sampled queries.")

    metrics_all: list[dict] = []
    for spec in ALL_SPECS:
        if spec.label in args.skip:
            print(f"[run] skipping {spec.label}")
            continue

        print(f"\n[run] === {spec.label} ===")
        client = build_client(spec)

        preds_path = args.results_dir / f"predictions_{spec.label}.csv"
        malformed_path = args.results_dir / f"malformed_{spec.label}.jsonl"
        metrics_path = args.results_dir / f"metrics_{spec.label}.json"

        run_agent(
            client=client,
            sampled_df=sampled,
            tools=tools,
            out_predictions_path=preds_path,
            out_malformed_path=malformed_path,
            max_tokens=args.max_tokens,
        )
        metrics = evaluate_predictions(preds_path, metrics_path, model_label=spec.label)
        metrics_all.append(metrics)

        client.unload()
        del client

    if metrics_all:
        write_summary(metrics_all, args.results_dir / "summary.csv")


if __name__ == "__main__":
    main()
