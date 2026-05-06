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
2. A numbered list of **all 47 tools** with their verbatim descriptions from `big_tool_des.json`.
3. Two worked examples — one tool-needed, one not — that demonstrate the JSON output format.
4. A strict "output ONLY valid JSON" instruction with the exact schema:
   ```json
   {"tool_needed": <true|false>, "tool_name": <string or null>, "reasoning": <one-sentence string>}
   ```
5. The user's query.

The first example's `tool_name` is patched at render time to be the first tool in the actual list, guaranteeing the example is always a real tool name (not a placeholder).

## 3. Sampled dataset summary

- **Source:** `all_clean_data.csv` from the MetaTool repository (positives) + a curated list of 250 hand-authored knowledge/reasoning prompts (negatives). MetaTool as packaged ships no non-tool subset, so negatives were synthesized; this is documented in §6 as a limitation.
- **Per-tool positives:** 5 — every tool has exactly 5 rows (min = max = 5).
- **Negatives:** 235, balanced 1:1 with positives, sampled without replacement from the 250-query curated pool.
- **Total:** **470 queries** (235 positive + 235 negative).
- **Tools covered:** **47** (the dataset has 47 tools, not the 48 referenced in the original brief).
- **Seed:** `RANDOM_SEED = 42`, set on `random` and `numpy` inside `src/sampling.py`.
- **Order:** rows are shuffled after concatenation so prompt position cannot trivially leak the label.

## 4. Results

Computed by `src/evaluate.py` with `sklearn.metrics`. Positive class = "tool needed". CSR is the MetaTool paper definition: *correct tool selections / all gold-positive samples* (denominator = 235 across every model; a missed positive counts as an incorrect selection).

| Model                        | Accuracy | Precision | Recall | F1     |   CSR    | CSR (with fuzzy) |
|-----------------------------|---------:|----------:|-------:|-------:|---------:|-----------------:|
| `gemma2-2b-it`              |   0.883  |    0.833  |  0.957 |  0.891 |   0.681  |     0.681        |
| `llama3.2-3b-instruct`      |   0.881  |    0.848  |  0.928 |  0.886 |   0.711  |     0.715        |
| `qwen2.5-7b-instruct-4bit`  | **0.951**|  **1.000**|  0.902 |**0.949**| **0.774**|   **0.774**      |

**Diagnostic CSR breakdown.** The same correct-tool-selection counts can also be reported with the denominator restricted to *positives where the model attempted a tool* — this isolates pure tool-selection ability from the binary-classification ability captured by recall:

| Model                       | Correct tool picks | Attempted (model said yes) | CSR-attempted | CSR (paper) |
|----------------------------|-------------------:|---------------------------:|--------------:|------------:|
| `gemma2-2b-it`             | 160                | 225                        | 0.711         | 0.681       |
| `llama3.2-3b-instruct`     | 167                | 218                        | 0.766         | 0.711       |
| `qwen2.5-7b-instruct-4bit` | 182                | 212                        | 0.858         | 0.774       |

Auxiliary diagnostics (from `metrics_<model>.json`):

| Model                       | Malformed JSON | Fuzzy matches used | "Unknown tool" predictions |
|----------------------------|---------------:|-------------------:|---------------------------:|
| `gemma2-2b-it`             | 0              | 0                  | 10                         |
| `llama3.2-3b-instruct`     | 0              | 1                  | 15                         |
| `qwen2.5-7b-instruct-4bit` | 0              | 0                  | 0                          |

**Reading the table:**

- **Qwen 2.5 7B (4-bit) is the clear winner** on every metric. Notably it achieves **perfect precision (1.000)** — across all 235 negative queries it never once said `tool_needed=true`. It pays for that with slightly lower recall (0.902) than the smaller models, meaning it is more conservative about invoking tools.
- **Gemma 2B has the highest recall (0.957) but the lowest precision (0.833)**: it is the most eager of the three to invoke a tool, including on knowledge questions that need none. F1 ends up nearly identical to Llama because the precision/recall trade-off is roughly symmetric.
- **Llama 3.2 3B sits between the two on binary metrics** but is meaningfully better at picking the right tool when it commits: CSR-attempted of 0.766 vs Gemma's 0.711.
- **CSR ≈ CSR-with-fuzzy for every model.** Fuzzy normalization rescued at most one prediction (Llama). Headline tool-selection numbers therefore reflect strict exact-match performance, not propped up by fuzzy fallback.
- **Zero malformed JSON across all three models.** The single shared prompt with two worked examples and the explicit "Output ONLY valid JSON" instruction was sufficient to get parseable output without any model-specific tweaks.
- **Hallucinated tool names**: Gemma named a non-existent tool 10 times, Llama 15 times, Qwen never. This pattern matches general findings that larger instruction-tuned models adhere more reliably to closed-vocabulary outputs.

## 5. Error analysis

We sampled three representative failures per model from `results/predictions_<model>.csv`. Most failures are **plausibly wrong** rather than nonsense — they reveal overlapping tool semantics in the MetaTool benchmark itself, not just model weakness.

### `gemma2-2b-it`

| Query (truncated) | Gold tool | Predicted tool | Cause |
|---|---|---|---|
| "Any suggestions for quick and easy dinner ideas?" | `DietTool` | `ProductSearch` | Topic-confusion: "dinner ideas" → "products". Misses that `DietTool` is the recipe-and-meal tool. |
| "I need a unique and catchy domain name … to use specifically for showcasing my portfolio of … photographs" | `URLTool` | `WebsiteTool` | Both tools are domain-related. The model latched onto "showcasing portfolio" and chose the website-builder, but the gold task is domain-name selection (`URLTool`). |
| "Find a pet-friendly apartment in Chicago…" | `HouseRentingTool` | `TripTool` | `TripTool`'s description includes "accommodation bookings" — the model conflates short-term travel with long-term renting. This same confusion appears across all three models. |

### `llama3.2-3b-instruct`

| Query (truncated) | Gold tool | Predicted tool | Cause |
|---|---|---|---|
| "Why is it so difficult to find a rental property in Montreal? Help me find one!" | `HouseRentingTool` | `TripTool` | Same rental↔travel confusion as Gemma. |
| "Find a pet-friendly apartment in Chicago…" | `HouseRentingTool` | `TripTool` | Same. |
| "Does Best Buy happen to have discounts on their latest laptops?" | `Discount` | NaN (predicted false-negative) | Llama incorrectly classified this as not needing a tool. The query needs the `Discount` tool to look up real-time coupon codes — a recall failure. |

### `qwen2.5-7b-instruct-4bit`

| Query (truncated) | Gold tool | Predicted tool | Cause |
|---|---|---|---|
| "Paraphrase my resume to emphasize my skills…" | `PolishTool` | `ResumeTool` | Both are valid: `ResumeTool` is the resume-feedback tool; `PolishTool` is the general rewriting/paraphrasing tool. Qwen picked the topically-closer one but the gold expects the action-closer one. Arguably an annotation-ambiguity issue. |
| "Scientific research paper about COVID-19 published recently, can you find it from a reputable medical journal's website?" | `PDF&URLTool` | `ResearchFinder` | `ResearchFinder` is *the* academic-paper-search tool — Qwen's prediction is arguably **more correct than the gold label**. The benchmark labels this as `PDF&URLTool` (PDF interaction) which seems to assume the user already has the URL. |
| "Help me locate startups that are seeking buyers." | `ProductSearch` | `CompanyInfoTool` | `CompanyInfoTool` describes "obtain relevant information about global companies" — a defensible match. Gold expects `ProductSearch`, which the model didn't see as a fit because "startups" reads as companies, not products. |

**Cross-cutting themes:**

1. **`TripTool` vs `HouseRentingTool` is a systematic trap.** Every model picked `TripTool` for long-term rental queries because its description mentions "accommodation". Worth flagging to MetaTool maintainers.
2. **Polish vs Resume vs Research collisions.** When a query has both a topic (resume, research paper) and an action (polish, find), models tend to anchor on the topic.
3. **No malformed JSON anywhere.** The prompt's explicit schema + two examples generalized cleanly across all three model families.

## 6. Limitations

- **Synthetic negatives.** The MetaTool benchmark as packaged in `all_clean_data.csv` does not ship a non-tool subset. We synthesized ~240 negatives by hand-authoring knowledge/reasoning queries that lie clearly outside the 48-tool scope. This means the binary metrics measure *separability of our curated negatives from MetaTool positives*, not separability against the natural distribution of non-tool queries a user would issue.
- **47 tools in every prompt.** No retrieval over tool descriptions; the full list is included in every request. Token-budget pressure (~1.5–2 K tokens of tool list + query) may disadvantage the smallest model.
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
