# srcml-coreutils — assignment step 5

Step 5 of the coreutils / function-extraction assignment: ask Generative AI to find every function defined in `coreutils-9.1/src/*.c` and compare against the srcml ground truth from step 4.

## Layout

```
srcml-coreutils/
├── README.md                              # this file
├── notebooks/
│   └── openllm_function_extraction.ipynb  # 5.i — Colab notebook, Gemma2 / Llama3.2 / Qwen2.5
├── prompts/
│   └── chatgpt_claude_prompts.md          # 5.ii — prompt to paste into ChatGPT and Claude
└── results/                               # populated by step 5 runs (see below)
```

## Step 5.i — open-source LLMs

Open `notebooks/openllm_function_extraction.ipynb` in Colab on a **T4 GPU runtime**, provide a Hugging Face token with access to `google/gemma-2-2b-it` and `meta-llama/Llama-3.2-3B-Instruct`, and run all cells. The final cell zips and downloads `openllm_results.zip`. Unzip it into `results/`. You should end up with:

```
results/
├── openllm_gemma2-2b-it_functions.txt
├── openllm_gemma2-2b-it_per_file.csv
├── openllm_llama3.2-3b-instruct_functions.txt
├── openllm_llama3.2-3b-instruct_per_file.csv
├── openllm_qwen2.5-7b-instruct-4bit_functions.txt
├── openllm_qwen2.5-7b-instruct-4bit_per_file.csv
└── openllm_summary.csv
```

The same prompt is sent to all three models — only the chat template wrapping differs. Greedy decoding (`do_sample=False`) so runs are reproducible.

## Step 5.ii — ChatGPT and Claude

See `prompts/chatgpt_claude_prompts.md`. Upload `coreutils-9.1-src.tar.gz` to each chat, paste the master prompt, save the reply to `results/chatgpt_functions.txt` / `results/claude_functions.txt`, and screenshot each chat for the report.

## Step 6 — comparison

`prompts/chatgpt_claude_prompts.md` contains a shell snippet that diffs each model's function-name list against the srcml ground-truth list to compute true positives, false positives, false negatives, precision, recall, and F1. Use those as the evaluation metrics for 6.i.

## Notes

- coreutils version pinned to **9.1** (downloaded from `https://ftp.gnu.org/gnu/coreutils/coreutils-9.1.tar.xz` inside the notebook).
- Scope is `coreutils-9.1/src/*.c` only — same as your srcml run. Do not change the scope without re-running srcml so the comparison stays apples-to-apples.
- Long files are split into ~6000-character chunks before being sent to the smaller models so they fit the context window. The aggregation across chunks dedupes by name.
