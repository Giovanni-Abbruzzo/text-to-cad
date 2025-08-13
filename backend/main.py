"""
FastAPI Backend for Text-to-CAD Plugin

This module provides the main FastAPI application for the text-to-cad project.
It serves as the backend API that will eventually convert natural language 
instructions into structured CAD commands for applications like SolidWorks 
and Fusion360.

Currently implements:
- Health check endpoint for service monitoring
- CORS configuration for frontend integration
- Natural language instruction processing with naive parsing

Author: Text-to-CAD Team
"""

import re
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import Dict, Optional, Union

# Initialize FastAPI application
app = FastAPI(
    title="Text-to-CAD Backend API",
    description="Backend service for converting natural language to CAD commands",
    version="0.1.0"
)

# Configure CORS middleware
# Allow requests from frontend (localhost:5173) and wildcard for development
app.add_middleware(
    CORSMiddleware,
    allow_origins=[
        "http://localhost:5173",  # Frontend development server
        "*"  # Wildcard for development - should be restricted in production
    ],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.get("/health")
async def health_check() -> Dict[str, str]:
    """
    Health check endpoint to verify the API is running.
    
    Returns:
        Dict[str, str]: Status response indicating service health
    """
    return {"status": "ok"}


# Root endpoint for basic API information
@app.get("/")
async def root() -> Dict[str, str]:
    """
    Root endpoint providing basic API information.
    
    Returns:
        Dict[str, str]: Welcome message and API info
    """
    return {
        "message": "Text-to-CAD Backend API",
        "version": "0.1.0",
        "health": "/health",
        "endpoints": {
            "process_instruction": "/process_instruction"
        }
    }


# Pydantic Models for Request/Response
class InstructionRequest(BaseModel):
    """
    Request model for processing natural language CAD instructions.
    
    Attributes:
        instruction (str): Natural language instruction for CAD operation
    """
    instruction: str


class ParsedParameters(BaseModel):
    """
    Parsed parameters extracted from natural language instruction.
    
    Attributes:
        action (str): The CAD action to perform (extrude, create_hole, fillet, pattern, create_feature)
        shape (Optional[str]): Shape type if applicable (cylinder, block, etc.)
        height_mm (Optional[float]): Height dimension in millimeters
        diameter_mm (Optional[float]): Diameter dimension in millimeters
        count (Optional[int]): Count for patterns or arrays
    """
    action: str
    shape: Optional[str] = None
    height_mm: Optional[float] = None
    diameter_mm: Optional[float] = None
    count: Optional[int] = None


class InstructionResponse(BaseModel):
    """
    Response model containing original instruction and parsed parameters.
    
    Attributes:
        instruction (str): Original instruction text
        parsed_parameters (ParsedParameters): Extracted CAD parameters
    """
    instruction: str
    parsed_parameters: ParsedParameters


# Parsing Logic
def parse_cad_instruction(instruction: str) -> ParsedParameters:
    """
    Parse natural language CAD instruction into structured parameters.
    
    This function uses naive keyword detection and regex pattern matching
    to extract CAD parameters from natural language text. It includes
    safe fallbacks for unrecognized patterns.
    
    Parsing Assumptions:
    - Actions are detected by keywords (extrude, hole, fillet, pattern, create)
    - Shapes are detected by common geometric terms
    - Dimensions are extracted using regex for "number + unit" patterns
    - Default action is "create_feature" if no specific action is detected
    - Units are assumed to be millimeters if not specified
    
    Args:
        instruction (str): Natural language instruction
        
    Returns:
        ParsedParameters: Structured parameters extracted from instruction
    """
    instruction_lower = instruction.lower().strip()
    
    # Action detection with keyword matching
    action = "create_feature"  # Safe fallback
    if any(word in instruction_lower for word in ["extrude", "extrusion"]):
        action = "extrude"
    elif any(word in instruction_lower for word in ["hole", "drill", "bore"]):
        action = "create_hole"
    elif any(word in instruction_lower for word in ["fillet", "round", "radius"]):
        action = "fillet"
    elif any(word in instruction_lower for word in ["pattern", "array", "repeat"]):
        action = "pattern"
    
    # Shape detection
    shape = None
    if any(word in instruction_lower for word in ["cylinder", "cylindrical", "round"]):
        shape = "cylinder"
    elif any(word in instruction_lower for word in ["block", "box", "cube", "rectangular"]):
        shape = "block"
    elif any(word in instruction_lower for word in ["sphere", "ball", "spherical"]):
        shape = "sphere"
    
    # Dimension extraction using regex patterns
    # Pattern: number followed by optional unit (mm, millimeter, etc.)
    
    # Height extraction (height, tall, length, depth)
    height_mm = None
    height_patterns = [
        r'(?:height|tall|length|depth)\s*(?:of\s*)?([0-9]*\.?[0-9]+)\s*(?:mm|millimeter|millimeters)?',
        r'([0-9]*\.?[0-9]+)\s*(?:mm|millimeter|millimeters)?\s*(?:height|tall|length|depth)',
        r'([0-9]*\.?[0-9]+)\s*(?:mm|millimeter|millimeters)?\s*(?:high|long|deep)'
    ]
    
    for pattern in height_patterns:
        match = re.search(pattern, instruction_lower)
        if match:
            try:
                height_mm = float(match.group(1))
                break
            except (ValueError, IndexError):
                continue
    
    # Diameter extraction
    diameter_mm = None
    diameter_patterns = [
        r'(?:diameter|dia|width|wide)\s*(?:of\s*)?([0-9]*\.?[0-9]+)\s*(?:mm|millimeter|millimeters)?',
        r'([0-9]*\.?[0-9]+)\s*(?:mm|millimeter|millimeters)?\s*(?:diameter|dia|width|wide)',
        r'([0-9]*\.?[0-9]+)\s*(?:mm|millimeter|millimeters)?\s*(?:across|around)'
    ]
    
    for pattern in diameter_patterns:
        match = re.search(pattern, instruction_lower)
        if match:
            try:
                diameter_mm = float(match.group(1))
                break
            except (ValueError, IndexError):
                continue
    
    # Count extraction for patterns/arrays
    count = None
    count_patterns = [
        r'(?:count|number|qty|quantity)\s*(?:of\s*)?([0-9]+)',
        r'([0-9]+)\s*(?:times|copies|instances)',
        r'(?:make|create)\s*([0-9]+)'
    ]
    
    for pattern in count_patterns:
        match = re.search(pattern, instruction_lower)
        if match:
            try:
                count = int(match.group(1))
                break
            except (ValueError, IndexError):
                continue
    
    return ParsedParameters(
        action=action,
        shape=shape,
        height_mm=height_mm,
        diameter_mm=diameter_mm,
        count=count
    )


@app.post("/process_instruction")
async def process_instruction(request: InstructionRequest) -> InstructionResponse:
    """
    Process natural language CAD instruction and extract structured parameters.
    
    This endpoint takes a natural language instruction and uses naive parsing
    to extract CAD-relevant parameters like actions, shapes, and dimensions.
    
    Args:
        request (InstructionRequest): Request containing the instruction text
        
    Returns:
        InstructionResponse: Original instruction and parsed parameters
        
    Example:
        Input: {"instruction": "extrude a 5mm cylinder with 10mm diameter"}
        Output: {
            "instruction": "extrude a 5mm cylinder with 10mm diameter",
            "parsed_parameters": {
                "action": "extrude",
                "shape": "cylinder",
                "height_mm": 5.0,
                "diameter_mm": 10.0,
                "count": null
            }
        }
    """
    # Parse the instruction using our naive parsing logic
    parsed_params = parse_cad_instruction(request.instruction)
    
    # Return structured response
    return InstructionResponse(
        instruction=request.instruction,
        parsed_parameters=parsed_params
    )
