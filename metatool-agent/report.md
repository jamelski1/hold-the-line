# MetaTool Tool-Selection Agent — Report

> **Status:** template. Numerical results are filled in after running the Colab notebook on the sampled set.

## 1. LLM choices and justification

We evaluate one model from each of three size tiers, all open-weights and instruction-tuned:

| Tier   | Model                              | Params | Why                                                                 |
|--------|------------------------------------|--------|---------------------------------------------------------------------|
| Small  | `google/gemma-2-2b-it`             | 2.6 B  | Strong small-model baseline; matches the in-class notebook.         |
| Medium | `meta-llama/Llama-3.2-3B-Instruct` | 3.2 B  | Different family from Gemma; well-tuned for instruction following.  |
| Larger | `Qwen/Qwen2.5-7B-Instruct` (4-bit) | 7.6 B  | Tests whether more parameters help structured-JSON tool routing.    |

The 7B model is loaded with NF4 4-bit quantization (`bitsandbytes`) so the entire pipeline runs on a single Colab T4 GPU.

## 2. Prompt design

A single prompt template is shared across all three models. The model is the only independent variable; chat-template wrapping (Gemma's `<start_of_turn>`, Llama's `<|start_header_id|>`, Qwen's ChatML) is applied via `tokenizer.apply_chat_template(...)`.

The full template lives in `src/prompts.py`. Rendered for a query, it concatenates:

1. A short two-sentence task definition for both sub-tasks.
2. A numbered list of **all 48 tools** with their verbatim descriptions from `big_tool_des.json`.
3. Two worked examples — one tool-needed, one not — that demonstrate the JSON output format.
4. A strict "output ONLY valid JSON" instruction with the exact schema:
   ```json
   {"tool_needed": <true|false>, "tool_name": <string or null>, "reasoning": <one-sentence string>}
   ```
5. The user's query.

The first example's `tool_name` is patched at render time to be the first tool in the actual list, guaranteeing the example is always a real tool name (not a placeholder).

## 3. Sampled dataset summary

- **Source:** `all_clean_data.csv` from the MetaTool repository (positives) + a curated list of 120+ knowledge/reasoning prompts (negatives, hand-authored).
- **Per-tool positives:** 5 (or fewer if a tool has fewer than 5 rows in the source — flagged at sampling time).
- **Negatives:** balanced to match positive count, sampled without replacement from the curated pool.
- **Total:** ~240 positive + ~240 negative ≈ 480 queries.
- **Seed:** `RANDOM_SEED = 42` (set on `random` and `numpy`).
- **Order:** rows are shuffled after concatenation so prompt position can't trivially leak the label.

> Per-tool counts and the actual totals will be filled in after running the notebook (cell "3. Build sampled dataset").

## 4. Results

> Filled in from `results/summary.csv` after the run.

| Model                       | Accuracy | Precision | Recall | F1   | CSR (strict) | CSR (with fuzzy) |
|----------------------------|---------:|----------:|-------:|-----:|-------------:|-----------------:|
| `gemma2-2b-it`             | TBD      | TBD       | TBD    | TBD  | TBD          | TBD              |
| `llama3.2-3b-instruct`     | TBD      | TBD       | TBD    | TBD  | TBD          | TBD              |
| `qwen2.5-7b-instruct-4bit` | TBD      | TBD       | TBD    | TBD  | TBD          | TBD              |

- **Accuracy / Precision / Recall / F1** are computed on the binary tool-needed task (positive class = "tool needed").
- **CSR (Correct Selection Rate)** is computed only over rows where the gold answer requires a tool **and** the model said `tool_needed=true` — among those, the fraction with `predicted_tool == gold_tool`.
- **Strict** CSR counts only exact-match (after case-folding); **with fuzzy** additionally credits matches found by `rapidfuzz` at threshold ≥ 90. We track the fuzzy delta because the brief explicitly forbids silently using fuzzy matching.

## 5. Error analysis

> Pull 3–5 examples per model from `results/predictions_<model>.csv` after the run. Categories to fill in:

For each model, include:

- **Malformed JSON** — example raw response, what went wrong (e.g. trailing prose, markdown fences, hallucinated keys).
- **Wrong-tool selection** — query, gold tool, predicted tool, plausible cause (e.g. confusion between two similar tools).
- **False positive on tool-needed** — query that should be answerable from general knowledge but the model invoked a tool anyway.
- **False negative on tool-needed** — query that clearly needs a tool but the model declined.

Cell 6 of the notebook prints each model's failure tallies and a sample of wrong-tool rows to seed this section.

## 6. Limitations

- **Synthetic negatives.** The MetaTool benchmark as packaged in `all_clean_data.csv` does not ship a non-tool subset. We synthesized ~240 negatives by hand-authoring knowledge/reasoning queries that lie clearly outside the 48-tool scope. This means the binary metrics measure *separability of our curated negatives from MetaTool positives*, not separability against the natural distribution of non-tool queries a user would issue.
- **48 tools in every prompt.** No retrieval over tool descriptions; the full list is included in every request. Token-budget pressure (~1.5–2 K tokens of tool list + query) may disadvantage the smallest model.
- **Single prompt template.** No prompt-engineering search per model. A model-specific prompt could close part of the gap we observe.
- **Greedy decoding, single sample per query.** No self-consistency or temperature sweeps.
- **4-bit quantization for Qwen 7B.** Some accuracy loss vs full-precision is expected; we did not run an unquantized comparison due to T4 VRAM limits.
- **CSR conditional on the model's own binary prediction.** A model that says `tool_needed=true` only when it's confident will have artificially high CSR. We report the eligible-row count (`csr_eligible`) alongside CSR so this can be inspected.

## 7. Reproducibility

- **Python:** 3.10+
- **Hardware tested:** Google Colab T4 (free tier), 16 GB VRAM.
- **Seed:** `RANDOM_SEED = 42` (in `src/__init__.py`); set on `random` and `numpy` inside `src/sampling.py`.
- **Decoding:** greedy (`do_sample=False`, no temperature).
- **Pinned versions:** see `requirements.txt`.
- **Exact commands:**
  ```bash
  # Step 1: sample
  python -m scripts.run_sampling --per-tool 5
  # Step 2: run all 3 models + write results/
  python -m scripts.run_evaluation
  ```
- **Model versions:** as resolved by `huggingface_hub` at run time (Hugging Face does not strictly version weights — record the resolved revisions printed by `transformers` at load time if exact reproducibility is required).
