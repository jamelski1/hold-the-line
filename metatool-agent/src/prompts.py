"""Prompt construction. ONE prompt template, identical across all 3 models."""

from __future__ import annotations

import json

SYSTEM_INSTRUCTION = (
    "You are a tool-routing assistant. For each user query you do TWO things:\n"
    "  1. Tool Usage Awareness: decide whether the query requires using one of the listed tools.\n"
    "     A query requires a tool only if it cannot be answered from general knowledge or simple\n"
    "     reasoning alone — for example, when it needs real-time data, an API call, code execution,\n"
    "     image generation, or external services.\n"
    "  2. Tool Selection: if a tool is required, pick the SINGLE best matching tool from the list\n"
    "     by its exact name.\n"
)

OUTPUT_INSTRUCTION = (
    'Output ONLY valid JSON matching this exact schema, with no surrounding prose, '
    'no markdown code fences, and no extra keys:\n'
    '{"tool_needed": <true|false>, "tool_name": <string or null>, "reasoning": <one-sentence string>}\n'
    'If "tool_needed" is false, "tool_name" MUST be null. '
    'If "tool_needed" is true, "tool_name" MUST be one of the exact tool names listed above.'
)

EXAMPLES = [
    {
        "query": "What is the capital of France?",
        "output": {
            "tool_needed": False,
            "tool_name": None,
            "reasoning": "This is general knowledge that can be answered without any external tool.",
        },
    },
]


def _pick_positive_example(tools: list[dict]) -> tuple[str, dict]:
    """Build a tool-needed example anchored to a real tool from the list.

    We match against tool *names* (more reliable than descriptions, which can
    coincidentally contain unrelated keywords — e.g. MapTool's description
    mentions 'images'). The candidate list below is ordered by preference; we
    use the first one whose substring is a case-insensitive match against any
    tool name. Falls back to the first tool with a description-derived query.
    """
    # (name_substring, query, reasoning) — name_substring matched against tool NAME only.
    candidates = [
        ("weather", "What is the current weather in Tokyo right now?",
         "This needs real-time weather data, not general knowledge."),
        ("chart", "Create a bar chart comparing quarterly sales for 2024.",
         "Generating a chart requires a chart-rendering tool."),
        ("notes", "Add 'buy groceries' to my reminders for tomorrow.",
         "Adding a reminder requires a notes/reminders tool."),
        ("news", "Show me the top news stories about climate change today.",
         "Real-time news requires a news tool, not general knowledge."),
        ("finance", "What is Apple's current stock price?",
         "Live stock data requires a finance tool."),
        ("map", "Give me driving directions from San Francisco to Los Angeles.",
         "Navigation requires a maps/directions tool."),
        ("podcast", "Find a podcast episode about machine learning ethics.",
         "Searching podcasts requires a podcast tool."),
        ("music", "Build me a workout playlist of high-energy rock songs.",
         "Creating playlists requires a music tool."),
        ("game", "Recommend a strategy game similar to Civilization.",
         "Game recommendations require a game-info tool."),
        ("research", "Find recent academic papers on transformer architectures.",
         "Searching academic papers requires a research tool."),
    ]
    for substr, query, reasoning in candidates:
        for t in tools:
            if substr in t["name"].lower():
                return query, {
                    "tool_needed": True,
                    "tool_name": t["name"],
                    "reasoning": reasoning,
                }
    # Fallback: anchor to first tool with a description-derived query.
    t = tools[0]
    desc = (t.get("description") or "this tool's task").strip().rstrip(".")
    return (
        f"Please help me with the following: {desc[:120]}.",
        {
            "tool_needed": True,
            "tool_name": t["name"],
            "reasoning": f"This task matches what '{t['name']}' is designed to do.",
        },
    )


def format_tool_list(tools: list[dict]) -> str:
    """Numbered tool list with verbatim descriptions from big_tool_des.json."""
    lines = []
    for i, t in enumerate(tools, start=1):
        name = t["name"]
        desc = (t.get("description") or "").replace("\n", " ").strip()
        if not desc:
            desc = "(no description)"
        lines.append(f"  {i}. {name}: {desc}")
    return "\n".join(lines)


def render_prompt(query: str, tools: list[dict]) -> str:
    """Render the final prompt for a single query.

    The first example's tool name is patched at render-time to the first tool
    in `tools` so it's always a valid name from the actual list.
    """
    tool_list_str = format_tool_list(tools)

    pos_query, pos_output = _pick_positive_example(tools)
    examples = [
        {"query": pos_query, "output": pos_output},
        EXAMPLES[0],  # the negative example
    ]

    example_blocks = []
    for ex in examples:
        example_blocks.append(
            f'Query: "{ex["query"]}"\nOutput: {json.dumps(ex["output"], ensure_ascii=False)}'
        )
    examples_str = "\n\n".join(example_blocks)

    return (
        f"{SYSTEM_INSTRUCTION}\n"
        f"Available tools (you must use one of these exact names if a tool is needed):\n"
        f"{tool_list_str}\n\n"
        f"Examples:\n{examples_str}\n\n"
        f"{OUTPUT_INSTRUCTION}\n\n"
        f'Query: "{query}"\n'
        f"Output:"
    )
