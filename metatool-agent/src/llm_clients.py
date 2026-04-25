"""Thin LLM client abstraction over Hugging Face transformers.

All three models share one HFClient implementation. Per-model differences
(chat template, BOS handling, quantization) are captured as constructor args.
Models are loaded lazily and `unload()` frees VRAM so a single Colab T4 can
run all three sequentially.
"""

from __future__ import annotations

import gc
from dataclasses import dataclass
from typing import Optional


@dataclass
class ModelSpec:
    label: str  # short tag used in filenames, e.g. "gemma2-2b-it"
    repo_id: str  # HF repo
    load_in_4bit: bool = False
    torch_dtype: str = "float16"  # "float16" | "bfloat16"
    trust_remote_code: bool = False


# Curated specs matching the report's small/mid/large progression.
GEMMA2_2B_IT = ModelSpec(
    label="gemma2-2b-it",
    repo_id="google/gemma-2-2b-it",
    torch_dtype="bfloat16",  # gemma-2 prefers bf16
)

LLAMA32_3B_INSTRUCT = ModelSpec(
    label="llama3.2-3b-instruct",
    repo_id="meta-llama/Llama-3.2-3B-Instruct",
    torch_dtype="bfloat16",
)

QWEN25_7B_INSTRUCT_4BIT = ModelSpec(
    label="qwen2.5-7b-instruct-4bit",
    repo_id="Qwen/Qwen2.5-7B-Instruct",
    load_in_4bit=True,
    torch_dtype="float16",
)

ALL_SPECS: list[ModelSpec] = [GEMMA2_2B_IT, LLAMA32_3B_INSTRUCT, QWEN25_7B_INSTRUCT_4BIT]


class LLMClient:
    """Generates text from a chat-formatted prompt. One generate() call per query."""

    def __init__(self, spec: ModelSpec, device: str = "cuda"):
        import torch  # lazy import
        from transformers import AutoModelForCausalLM, AutoTokenizer

        self.spec = spec
        self.device = device

        kwargs: dict = {
            "trust_remote_code": spec.trust_remote_code,
        }

        if spec.load_in_4bit:
            from transformers import BitsAndBytesConfig

            kwargs["quantization_config"] = BitsAndBytesConfig(
                load_in_4bit=True,
                bnb_4bit_quant_type="nf4",
                bnb_4bit_compute_dtype=torch.float16,
                bnb_4bit_use_double_quant=True,
            )
            kwargs["device_map"] = "auto"
        else:
            dtype = getattr(torch, spec.torch_dtype)
            kwargs["torch_dtype"] = dtype
            kwargs["device_map"] = "auto"

        self.tokenizer = AutoTokenizer.from_pretrained(
            spec.repo_id, trust_remote_code=spec.trust_remote_code
        )
        if self.tokenizer.pad_token_id is None:
            self.tokenizer.pad_token_id = self.tokenizer.eos_token_id

        self.model = AutoModelForCausalLM.from_pretrained(spec.repo_id, **kwargs)
        self.model.eval()

    def generate(self, prompt: str, max_tokens: int = 512, temperature: float = 0.0) -> str:
        """Generate a response. We use the model's chat template — passing the
        prompt as a single user turn — to ensure correct special tokens.
        """
        import torch
        messages = [{"role": "user", "content": prompt}]
        # apply_chat_template returns a bare tensor on older transformers and a
        # BatchEncoding on newer versions. Forcing return_dict=True normalizes
        # this so we always get attention_mask alongside input_ids.
        encoded = self.tokenizer.apply_chat_template(
            messages,
            add_generation_prompt=True,
            return_tensors="pt",
            return_dict=True,
        )
        input_ids = encoded["input_ids"].to(self.model.device)
        if "attention_mask" in encoded:
            attention_mask = encoded["attention_mask"].to(self.model.device)
        else:
            attention_mask = torch.ones_like(input_ids)

        do_sample = temperature > 0.0
        gen_kwargs = dict(
            input_ids=input_ids,
            attention_mask=attention_mask,
            max_new_tokens=max_tokens,
            do_sample=do_sample,
            pad_token_id=self.tokenizer.pad_token_id,
        )
        if do_sample:
            gen_kwargs["temperature"] = temperature
        else:
            # Deterministic / greedy. Avoid passing temperature=0 (transformers warns).
            pass

        with torch.no_grad():
            output_ids = self.model.generate(**gen_kwargs)

        # Strip the prompt prefix.
        new_tokens = output_ids[0, input_ids.shape[-1]:]
        text = self.tokenizer.decode(new_tokens, skip_special_tokens=True)
        return text.strip()

    def unload(self) -> None:
        """Free VRAM. Call between models when running sequentially on Colab."""
        import torch
        del self.model
        del self.tokenizer
        gc.collect()
        if torch.cuda.is_available():
            torch.cuda.empty_cache()
            torch.cuda.ipc_collect()


def build_client(spec: ModelSpec, device: Optional[str] = None) -> LLMClient:
    import torch
    if device is None:
        device = "cuda" if torch.cuda.is_available() else "cpu"
    return LLMClient(spec, device=device)
