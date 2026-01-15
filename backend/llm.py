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
    base_url = os.getenv("OPENAI_BASE_URL")
    organization = os.getenv("OPENAI_ORG")
    project = os.getenv("OPENAI_PROJECT")
    
    # Check if API key is available
    if not api_key:
        raise LLMParseError("OpenAI API key not configured")
    
    # Initialize OpenAI client (supports alternate base_url for local gateways)
    client = OpenAI(
        api_key=api_key,
        timeout=timeout,
        base_url=base_url,
        organization=organization,
        project=project,
    )
    
    # Build the prompt for structured JSON output
    system_prompt = """You are a CAD command parser. Convert natural language instructions into JSON commands.

Return ONLY valid JSON matching this exact schema:
{
  "action": "create_hole" | "extrude" | "fillet" | "pattern" | "chamfer" | "create_feature",
    "parameters": {
      "count": number | null,
      "diameter_mm": number | null,
      "height_mm": number | null,
      "width_mm": number | null,
      "length_mm": number | null,
      "depth_mm": number | null,
      "radius_mm": number | null,
      "center_x_mm": number | null,
      "center_y_mm": number | null,
      "center_z_mm": number | null,
      "axis": "x" | "y" | "z" | null,
      "use_top_face": boolean | null,
      "extrude_midplane": boolean | null,
      "angle_deg": number | null,
      "draft_angle_deg": number | null,
      "draft_outward": boolean | null,
      "flip_direction": boolean | null,
      "fillet_target": "all_edges" | "recent_feature" | null,
      "chamfer_distance_mm": number | null,
      "chamfer_target": "all_edges" | "recent_feature" | null,
      "shape": string | null,
      "pattern": {
        "type": "circular" | "linear" | null,
        "count": number | null,
        "angle_deg": number | null,
      "radius_mm": number | null
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
- Width/length/depth mapping: Use width_mm, length_mm, depth_mm when explicit
- Radius mapping: Map "radius" to radius_mm (leave diameter_mm null unless explicitly stated)
- Position mapping: If instruction specifies coordinates (x/y/z), set center_x_mm/center_y_mm/center_z_mm
- Axis mapping: If instruction specifies axis (x, y, z) for a cylinder, set axis
- Top-face mapping: If instruction says "on top face" or "on top", set use_top_face = true
- Midplane mapping: If instruction says "midplane" or "centered", set extrude_midplane = true
- Draft mapping: If instruction mentions draft or taper, set draft_angle_deg and draft_outward when specified
- Flip mapping: If instruction mentions flip/reverse direction, set flip_direction = true
- Fillet target mapping: If instruction mentions all edges or all sharp edges, set fillet_target = "all_edges"; if it mentions last/recent feature, set fillet_target = "recent_feature"
- Chamfer action: If instruction requests chamfer or bevel, set action = "chamfer"
- Chamfer mapping: Use chamfer_distance_mm when "chamfer" or "bevel" is requested; include angle_deg if specified
- Chamfer target mapping: If instruction mentions all edges or all sharp edges, set chamfer_target = "all_edges"; if it mentions last/recent feature, set chamfer_target = "recent_feature"
- For patterns, include type, count, and angle if specified
- Pattern detection: Look for keywords like "array", "pattern", "circular", "linear", "repeat", "copy", "around", "in a circle", "in a line"
- If no pattern is mentioned, set pattern to null
- If no shape is mentioned, set shape to null
- Return ONLY the JSON, no explanations or markdown

Examples:
- "create a 5mm hole" -> shape: null, pattern: null
- "extrude a 5mm tall cylinder with 10mm diameter" -> shape: "cylinder", pattern: null
- "create a box that is 10mm wide" -> shape: "box", pattern: null
- "make a sphere with 5mm radius" -> shape: "sphere", pattern: null
- "create 4 holes in a circular pattern" -> shape: null, pattern: {"type": "circular", "count": 4, "angle_deg": null}
- "make 3 cylinders in a line" -> shape: "cylinder", pattern: {"type": "linear", "count": 3, "angle_deg": null}"""

    prompt_override = os.getenv("OPENAI_SYSTEM_PROMPT")
    prompt_path = os.getenv("OPENAI_SYSTEM_PROMPT_PATH")
    if prompt_path:
        try:
            with open(prompt_path, "r", encoding="utf-8") as handle:
                prompt_override = handle.read()
        except OSError:
            pass
    if prompt_override:
        system_prompt = prompt_override

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
