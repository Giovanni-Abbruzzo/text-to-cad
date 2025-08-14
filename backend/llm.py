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
    model = os.getenv("OPENAI_MODEL", "gpt-4o-mini")
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
    "shape": string | null,
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
- Shape detection: Look for shape keywords like "cylinder", "box", "cube", "sphere", "cone", "block", "circle", "square", "rectangle"
- Dimension mapping: Map "width", "wide", "diameter", "dia", "across" to diameter_mm field
- Height mapping: Map "height", "tall", "high", "thick", "thickness" to height_mm field
- For patterns, include type, count, and angle if specified
- Pattern detection: Look for keywords like "array", "pattern", "circular", "linear", "repeat", "copy", "around", "in a circle", "in a line"
- If no pattern is mentioned, set pattern to null
- If no shape is mentioned, set shape to null
- Return ONLY the JSON, no explanations or markdown

Examples:
- "create a 5mm hole" → shape: null, pattern: null
- "extrude a 5mm tall cylinder with 10mm diameter" → shape: "cylinder", pattern: null
- "create a box that is 10mm wide" → shape: "box", pattern: null
- "make a sphere with 5mm radius" → shape: "sphere", pattern: null
- "create 4 holes in a circular pattern" → shape: null, pattern: {"type": "circular", "count": 4, "angle_deg": null}
- "make 3 cylinders in a line" → shape: "cylinder", pattern: {"type": "linear", "count": 3, "angle_deg": null}"""

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
