# MetaTool Tool-Selection Agent

Prompt-based tool-selection agent evaluated on the [MetaTool benchmark](https://arxiv.org/abs/2310.03128) for a graduate assignment. Compares three open-source instruction-tuned LLMs on:

1. **Tool Usage Awareness** — binary: does the query need a tool at all?
2. **Tool Selection** — if so, which of the 47 tools is correct?

The agent is a single prompt sent to each model. No fine-tuning, no retrieval, no chain-of-thought ensembles.

## Quickstart (Colab)

1. Open `notebooks/metatool_agent.ipynb` in Colab on a T4 GPU runtime.
2. Provide a Hugging Face token with access to the gated Gemma 2 and Llama 3.2 models.
3. Run all cells. Predictions, per-model metrics, and a combined `results/summary.csv` will be written under `results/`.

## Quickstart (local)

```bash
pip install -r requirements.txt

# 1. Place big_tool_des.json and all_clean_data.csv into data/ (clone the
#    MetaTool repo and copy them in; the Colab notebook does this automatically).

# 2. Build the sampled dataset (240 positives + ~240 negatives, seed 42).
python -m scripts.run_sampling --per-tool 5

# 3. Run all three models sequentially and write results/.
python -m scripts.run_evaluation
```

A working CUDA GPU with ≥16 GB VRAM is recommended; the 7B Qwen model is loaded in 4-bit via `bitsandbytes` so it fits a Colab T4.

## Models

| Label                       | HF repo                            | Size    | Notes                |
|----------------------------|------------------------------------|---------|----------------------|
| `gemma2-2b-it`             | `google/gemma-2-2b-it`             | 2.6 B   | bf16, gated          |
| `llama3.2-3b-instruct`     | `meta-llama/Llama-3.2-3B-Instruct` | 3.2 B   | bf16, gated          |
| `qwen2.5-7b-instruct-4bit` | `Qwen/Qwen2.5-7B-Instruct`         | 7.6 B   | NF4 4-bit quantized  |

## Project layout

```
metatool-agent/
├── README.md
├── report.md                # graded write-up
├── requirements.txt
├── data/                    # downloaded + sampled CSVs (git-ignored)
├── notebooks/
│   └── metatool_agent.ipynb # Colab end-to-end runner
├── results/                 # per-model predictions + metrics (git-ignored)
├── scripts/
│   ├── run_sampling.py
│   └── run_evaluation.py
└── src/
    ├── __init__.py          # RANDOM_SEED = 42
    ├── sampling.py          # Step 2: sampled_data.csv builder
    ├── llm_clients.py       # Step 3: HF chat-template wrapper
    ├── prompts.py           # Step 4: single shared prompt
    ├── agent.py             # Step 5: render→generate→parse→normalize
    └── evaluate.py          # Step 6: accuracy / P / R / F1 / CSR
```

## Reproducibility

- All randomness (`random`, `numpy`, sampling) is seeded with `RANDOM_SEED = 42`.
- Generation uses greedy decoding (`do_sample=False`).
- All three models receive byte-identical prompt content (chat template wrapping is the only difference).
- Pinned dependency versions in `requirements.txt`.
