# Step 5.ii — Prompts for ChatGPT and Claude

Same task as step 5.i (open-source LLMs), but run interactively in the ChatGPT and Claude web UIs. Both support file upload, so the cleanest reproducible setup is:

1. From your local extracted coreutils, create one archive of just the `src/` directory:
   ```bash
   tar -czf coreutils-9.1-src.tar.gz -C coreutils-9.1 src
   ```
2. In ChatGPT (a model that supports code execution / files, e.g. GPT-5 with the Python tool) **and** in Claude (Sonnet 4.6 or Opus 4.7 with file upload), start a new chat and upload `coreutils-9.1-src.tar.gz`.
3. Paste the **identical** prompt below into both chats.
4. Save each chat's reply (function list + total) into `results/`:
   - `results/chatgpt_functions.txt` and `results/chatgpt_summary.txt`
   - `results/claude_functions.txt` and `results/claude_summary.txt`
5. Also save a screenshot of each chat for the report.

Keeping the prompt byte-identical across both chatbots is what makes this a fair comparison against the open-source models in step 5.i.

---

## Master prompt (paste into both ChatGPT and Claude)

> I have uploaded `coreutils-9.1-src.tar.gz`, an archive of the `src/` directory from GNU coreutils 9.1. Inside the archive, all C source files live directly under `src/` and have the `.c` extension.
>
> Your task is to analyze **every `.c` file in `src/`** and list every function that is **defined** in those files (a function definition is a function with a body, not just a declaration). Apply the following rules:
>
> 1. Include `static` functions.
> 2. Include `main` if defined.
> 3. Do **not** include functions that are only declared (e.g. in `extern` declarations or prototypes).
> 4. Do **not** include functions that are only called.
> 5. Do **not** include macros, typedefs, struct names, or function-pointer typedefs.
> 6. If the same function name is defined in multiple files, count it once per file (i.e. report the total number of function definitions across the codebase, not unique names) — but also report unique-name count separately.
>
> Process the archive programmatically (use your code-execution / Python tool — do not eyeball it). A reasonable approach: extract the archive, then for every `*.c` file under `src/`, parse out function definitions. You may use a regex over the source, or `ctags`, or any AST-based approach you prefer; state which method you used.
>
> Return your answer in exactly this format:
>
> ```
> METHOD: <one short sentence describing how you extracted the names>
> TOTAL_DEFINITIONS: <integer — total function definitions across all files>
> UNIQUE_NAMES: <integer — number of distinct function names>
> FILES_PROCESSED: <integer>
>
> --- function names (one per line, sorted, deduped) ---
> <name1>
> <name2>
> ...
> ```
>
> Do not summarize, do not truncate, do not add commentary after the list.

---

## Notes for the report (step 6)

When you compare the four sources (srcml vs. open-source LLM vs. ChatGPT vs. Claude), be sure to record for each:

- **Method** — AST/XPath (srcml), prompt-only generation (open LLMs), prompt + code-tool (ChatGPT/Claude).
- **Total function definitions** — the canonical count.
- **Unique names** — important because some helpers (e.g. `usage`) are defined in many files.
- **Wall-clock time** — included in `openllm_summary.csv`; jot it down for ChatGPT/Claude based on how long the chat took.
- **Hallucinations / omissions** — set-difference each LLM's name list against srcml's. Names srcml found and the LLM missed = false negatives. Names the LLM produced that srcml didn't = false positives (likely hallucinations or names of called/declared-only functions).

A useful ground-truth comparison snippet (run locally, after you have the srcml output and the four LLM output files):

```bash
sort -u srcml_functions.txt > /tmp/gt.txt
for f in openllm_gemma2-2b-it openllm_llama3.2-3b-instruct openllm_qwen2.5-7b-instruct-4bit chatgpt claude; do
    sort -u results/${f}_functions.txt > /tmp/pred.txt
    echo "=== $f ==="
    echo "  predicted : $(wc -l < /tmp/pred.txt)"
    echo "  true pos  : $(comm -12 /tmp/gt.txt /tmp/pred.txt | wc -l)"
    echo "  false pos : $(comm -13 /tmp/gt.txt /tmp/pred.txt | wc -l)"
    echo "  false neg : $(comm -23 /tmp/gt.txt /tmp/pred.txt | wc -l)"
done
```

Precision = TP / (TP + FP); recall = TP / (TP + FN); F1 = 2·P·R / (P+R). Use these as the evaluation metrics for step 6.i.
