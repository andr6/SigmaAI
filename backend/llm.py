import os
from typing import List

import openai


async def summarize_iocs(iocs: List[str]) -> str:
    """Generate a summary using the configured LLM provider."""
    provider = os.getenv("LLM_PROVIDER", "openai")
    if provider == "openai":
        api_key = os.getenv("OPENAI_API_KEY")
        openai.api_key = api_key
        prompt = "Summarize the following IOCs for a SOC analyst:\n" + "\n".join(iocs)
        chat_completion = await openai.ChatCompletion.acreate(
            model="gpt-3.5-turbo",
            messages=[{"role": "user", "content": prompt}],
        )
        return chat_completion.choices[0].message.content
    # Placeholder for other providers like claude, gemini, perplexity, ollama
    return "LLM provider not configured"
