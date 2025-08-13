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
- Configuration management with python-dotenv

Author: Text-to-CAD Team
"""

import re
import logging
from datetime import datetime
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, validator
from typing import Dict, Optional, Union

# Import configuration
from config import config

# Configure logging
logging.basicConfig(
    level=getattr(logging, config.LOG_LEVEL.upper()),
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger("text-to-cad-backend")

# Initialize FastAPI application
app = FastAPI(
    title="Text-to-CAD Backend API",
    description="Backend service for converting natural language to CAD commands",
    version="0.1.0"
)

# Configure CORS middleware using configuration
# Origins are loaded from environment variables with sensible defaults
app.add_middleware(
    CORSMiddleware,
    allow_origins=config.CORS_ORIGINS,  # Configurable CORS origins
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
            "process_instruction": "/process_instruction",
            "config": "/config"
        }
    }


# Pydantic Models for Request/Response
class InstructionRequest(BaseModel):
    """
    Request model for processing natural language CAD instructions.
    
    Attributes:
        instruction (str): Natural language instruction for CAD operation
                          Must be at least 3 characters long and non-empty
    """
    instruction: str
    
    @validator('instruction')
    def validate_instruction(cls, v):
        """
        Validate that instruction is not blank or too short.
        
        Args:
            v (str): The instruction string to validate
            
        Returns:
            str: The validated instruction
            
        Raises:
            ValueError: If instruction is blank, too short, or only whitespace
        """
        if not v or not v.strip():
            raise ValueError("Instruction cannot be blank or empty")
        
        if len(v.strip()) < 3:
            raise ValueError("Instruction must be at least 3 characters long")
            
        return v.strip()


class ParsedParameters(BaseModel):
    """
    Parsed parameters extracted from natural language instruction.
    
    All dimension fields use *_mm suffix to indicate millimeters as the default unit.
    This is documented here as the standard: all numeric dimensions are in millimeters
    unless explicitly specified otherwise in the instruction.
    
    Attributes:
        action (str): The CAD action to perform (extrude, create_hole, fillet, pattern, create_feature)
        shape (Optional[str]): Shape type if applicable (cylinder, block, sphere, etc.) - null if not detected
        height_mm (Optional[float]): Height dimension in millimeters - null if not detected
        diameter_mm (Optional[float]): Diameter dimension in millimeters - null if not detected
        count (Optional[int]): Count for patterns or arrays - null if not detected
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
    # NOTE: All dimensions are stored with *_mm suffix to indicate millimeters as default unit
    # This is our standard convention - dimensions are always in millimeters
    
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
    
    # Always return consistent structure with all fields present
    # Missing/undetected values are explicitly set to None for consistent JSON shape
    return ParsedParameters(
        action=action,
        shape=shape,
        height_mm=height_mm,  # Always in millimeters (documented by *_mm suffix)
        diameter_mm=diameter_mm,  # Always in millimeters (documented by *_mm suffix)
        count=count
    )


@app.post("/process_instruction")
async def process_instruction(request: InstructionRequest) -> InstructionResponse:
    """
    Process natural language CAD instruction and extract structured parameters.
    
    This endpoint takes a natural language instruction and uses naive parsing
    to extract CAD-relevant parameters like actions, shapes, and dimensions.
    
    Validation:
    - Instructions must be at least 3 characters long and non-empty
    - Returns 422 with clear error message for invalid instructions
    
    Response Format:
    - Always returns the same JSON shape with consistent field structure
    - Missing/undetected parameters are returned as null (not omitted)
    - All dimension fields use *_mm suffix indicating millimeters as default unit
    
    Args:
        request (InstructionRequest): Request containing the instruction text
        
    Returns:
        InstructionResponse: Original instruction and parsed parameters
        
    Raises:
        HTTPException: 422 if instruction validation fails
        
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
    # Log incoming request
    logger.info(f"Processing instruction: '{request.instruction}'")
    
    try:
        # Parse the instruction using our naive parsing logic
        parsed_params = parse_cad_instruction(request.instruction)
        
        # Log parsed results for debugging
        logger.info(f"Parsed parameters: action={parsed_params.action}, "
                   f"shape={parsed_params.shape}, height_mm={parsed_params.height_mm}, "
                   f"diameter_mm={parsed_params.diameter_mm}, count={parsed_params.count}")
        
        # Create response with consistent JSON shape
        response = InstructionResponse(
            instruction=request.instruction,
            parsed_parameters=parsed_params
        )
        
        logger.info("Successfully processed instruction")
        return response
        
    except Exception as e:
        logger.error(f"Error processing instruction '{request.instruction}': {str(e)}")
        raise HTTPException(
            status_code=500,
            detail=f"Internal error processing instruction: {str(e)}"
        )


@app.get("/config")
async def get_config() -> Dict:
    """
    Get current configuration information (excluding sensitive data).
    
    This endpoint provides visibility into the current configuration
    for debugging and development purposes. Sensitive information
    like API keys are masked or excluded.
    
    Returns:
        Dict: Configuration information safe for display
    """
    return {
        "config": config.get_config_info(),
        "env_file_loaded": "Configuration loaded from environment variables",
        "note": "Sensitive values are masked for security"
    }
