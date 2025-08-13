"""
AI/LLM helper module for parsing natural language instructions into structured CAD commands.

This module provides functionality to convert text instructions into JSON commands
using OpenAI's API when available and configured.
"""

import json
import os
from typing import Dict, Any
from openai import OpenAI


class LLMParseError(Exception):
    """Custom exception raised when LLM parsing fails, allowing fallback to rule-based parsing."""
    pass


def parse_instruction_with_ai(text: str) -> Dict[str, Any]:
    """
    Parse natural language instruction into structured CAD command using OpenAI API.
    
    Args:
        text (str): Natural language instruction to parse
        
    Returns:
        Dict[str, Any]: Structured command matching the CAD schema
        
    Raises:
        LLMParseError: When API call fails or JSON parsing fails, allowing fallback to rules
        
    Example:
        >>> parse_instruction_with_ai("create a 5mm hole")
        {
            "action": "create_hole",
            "parameters": {
                "diameter_mm": 5,
                "count": null,
                "height_mm": null,
                "pattern": null
            }
        }
    """
    # Read environment variables with defaults
    api_key = os.getenv("OPENAI_API_KEY")
    model = os.getenv("OPENAI_MODEL", "gpt-4o")
    timeout = int(os.getenv("OPENAI_TIMEOUT_S", "20"))
    
    # Check if API key is available
    if not api_key:
        raise LLMParseError("OpenAI API key not configured")
    
    # Initialize OpenAI client
    client = OpenAI(api_key=api_key, timeout=timeout)
    
    # Build the prompt for structured JSON output
    system_prompt = """You are a CAD command parser. Convert natural language instructions into JSON commands.

Return ONLY valid JSON matching this exact schema:
{
  "action": "create_hole" | "extrude" | "fillet" | "pattern" | "create_feature",
  "parameters": {
    "count": number | null,
    "diameter_mm": number | null,
    "height_mm": number | null,
    "pattern": {
      "type": "circular" | "linear" | null,
      "count": number | null,
      "angle_deg": number | null
    } | null
  }
}

Rules:
- Use null for unspecified parameters
- Extract numeric values when mentioned
- Choose the most appropriate action type
- For patterns, include type, count, and angle if specified
- Return ONLY the JSON, no explanations or markdown"""

    try:
        # Make API call to OpenAI
        response = client.chat.completions.create(
            model=model,
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": text}
            ],
            temperature=0.1,  # Low temperature for consistent structured output
            max_tokens=500    # Limit tokens for JSON response
        )
        
        # Extract the response content
        content = response.choices[0].message.content
        if not content:
            raise LLMParseError("Empty response from OpenAI API")
        
        # Strip any potential markdown formatting or extra whitespace
        content = content.strip()
        if content.startswith("```json"):
            content = content[7:]
        if content.endswith("```"):
            content = content[:-3]
        content = content.strip()
        
        # Parse JSON response
        try:
            parsed_command = json.loads(content)
        except json.JSONDecodeError as e:
            raise LLMParseError(f"Failed to parse JSON response: {e}")
        
        # Basic validation of required fields
        if not isinstance(parsed_command, dict):
            raise LLMParseError("Response is not a JSON object")
        
        if "action" not in parsed_command:
            raise LLMParseError("Missing 'action' field in response")
        
        if "parameters" not in parsed_command:
            raise LLMParseError("Missing 'parameters' field in response")
        
        return parsed_command
        
    except Exception as e:
        # Catch all network, API, and parsing errors
        if isinstance(e, LLMParseError):
            raise  # Re-raise our custom errors
        else:
            # Wrap other exceptions in our custom error for consistent handling
            raise LLMParseError(f"OpenAI API call failed: {str(e)}")
