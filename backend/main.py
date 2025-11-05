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
import json
import os
from datetime import datetime
from fastapi import FastAPI, HTTPException, Depends
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel, validator
from typing import Dict, Optional, Union, List, Any
from sqlalchemy.orm import Session
from sqlalchemy import desc

# Import configuration
from config import config

# Import database components
from db import engine, Base, SessionLocal, get_db
from models import Command

# Import AI/LLM functionality
from llm import parse_instruction_with_ai, LLMParseError

# Import job runner functionality
from jobs import start_job, get_job

# Configure logging first (needed for geometry import error handling)
logging.basicConfig(
    level=getattr(logging, config.LOG_LEVEL.upper()),
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger("text-to-cad-backend")

# Import geometry building and export functionality (optional for development)
try:
    from geometry.model_builder import dispatch_build
    from geometry.exporter import export_solid, ensure_outputs_directory
    GEOMETRY_AVAILABLE = True
    logger.info("Geometry modules loaded successfully")
except ImportError as e:
    logger.warning(f"Geometry modules not available: {e}")
    logger.warning("CadQuery dependencies may not be installed. /generate_model endpoint will be disabled.")
    GEOMETRY_AVAILABLE = False
    
    # Define dummy functions to prevent import errors
    def dispatch_build(action, params):
        raise HTTPException(status_code=503, detail="Geometry functionality not available - CadQuery dependencies not installed")
    
    def export_solid(solid, kind="step", prefix="model"):
        raise HTTPException(status_code=503, detail="Export functionality not available - CadQuery dependencies not installed")
    
    def ensure_outputs_directory():
        raise HTTPException(status_code=503, detail="Export functionality not available - CadQuery dependencies not installed")

# Initialize FastAPI application
app = FastAPI(
    title="Text-to-CAD Backend API",
    description="Backend service for converting natural language to CAD commands",
    version="0.1.0"
)

# Initialize database tables on startup
@app.on_event("startup")
async def startup_event():
    """
    Initialize database tables on application startup.
    
    Creates all tables defined in models if they don't already exist.
    This ensures the database schema is ready before handling requests.
    """
    logger.info("Initializing database tables...")
    Base.metadata.create_all(bind=engine)
    logger.info("Database tables initialized successfully")

# Configure CORS middleware using configuration
# Origins are loaded from environment variables with sensible defaults
app.add_middleware(
    CORSMiddleware,
    allow_origins=config.CORS_ORIGINS,  # Configurable CORS origins
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Mount static files for downloadable CAD outputs
app.mount("/outputs", StaticFiles(directory="outputs"), name="outputs")


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
            "dry_run": "/dry_run",
            "generate_model": "/generate_model",
            "commands": "/commands",
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
        use_ai (bool): Whether to use AI/LLM for parsing (default: False)
                      If True and OPENAI_API_KEY is set, uses AI parsing;
                      otherwise falls back to rule-based parsing
    """
    instruction: str
    use_ai: bool = False
    
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


class PatternInfo(BaseModel):
    """
    Pattern information for CAD operations.
    """
    type: Optional[str] = None  # "circular" or "linear"
    count: Optional[int] = None
    angle_deg: Optional[float] = None

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
        pattern (Optional[PatternInfo]): Pattern information if applicable - null if not detected
    """
    action: str
    shape: Optional[str] = None
    height_mm: Optional[float] = None
    diameter_mm: Optional[float] = None
    count: Optional[int] = None
    pattern: Optional[PatternInfo] = None


class InstructionResponse(BaseModel):
    """
    Response model containing original instruction and parsed parameters.
    
    Attributes:
        schema_version (str): API schema version for contract stability
        instruction (str): Original instruction text
        source (str): Parsing source - "ai" or "rule"
        plan (List[str]): Human-readable plan steps describing what will be done
        parsed_parameters (Dict): Extracted CAD parameters (always present, with nulls where unknown)
    """
    schema_version: str = "1.0"
    instruction: str
    source: str  # "ai" or "rule"
    plan: List[str]
    parsed_parameters: Dict


class CommandResponse(BaseModel):
    """
    Response model for saved Command records from database.
    
    Attributes:
        id (int): Database ID of the saved command
        prompt (str): Original natural language instruction
        action (str): Parsed CAD action/command
        parameters (Dict): Parsed parameters as dictionary (converted from JSON)
        created_at (datetime): Timestamp when command was created
    """
    id: int
    prompt: str
    action: str
    parameters: Dict
    created_at: datetime
    
    class Config:
        from_attributes = True  # Enable ORM mode for SQLAlchemy models


class CommandOut(BaseModel):
    """
    Output model for Command records in list responses.
    
    Used for GET /commands endpoint to return command history.
    Same structure as CommandResponse but optimized for list operations.
    
    Attributes:
        id (int): Database ID of the command
        prompt (str): Original natural language instruction
        action (str): Parsed CAD action/command
        parameters (Dict): Parsed parameters as dictionary (converted from JSON)
        created_at (datetime): Timestamp when command was created
    """
    id: int
    prompt: str
    action: str
    parameters: Dict
    created_at: datetime
    
    class Config:
        from_attributes = True  # Enable ORM mode for SQLAlchemy models


# Parsing Logic
def generate_plan_from_parsed(parsed_result: dict) -> List[str]:
    """
    Generate a human-readable plan from parsed CAD parameters.
    
    Converts structured parameters into descriptive action steps that explain
    what the CAD operation will do. Useful for preview/dry-run scenarios.
    
    Args:
        parsed_result (dict): Parsed result with 'action' and 'parameters' keys
        
    Returns:
        List[str]: List of human-readable plan steps
        
    Example:
        Input: {"action": "create_hole", "parameters": {"diameter_mm": 6, "count": 4}}
        Output: ["Create 4 holes with Ø6 mm diameter"]
    """
    action = parsed_result.get("action", "unknown")
    params = parsed_result.get("parameters", {})
    plan = []
    
    # Extract common parameters
    shape = params.get("shape")
    diameter_mm = params.get("diameter_mm")
    height_mm = params.get("height_mm")
    count = params.get("count")
    pattern = params.get("pattern")
    
    # Generate plan based on action type
    if action == "extrude":
        if shape == "cylinder":
            desc = "Extrude cylinder"
            if diameter_mm:
                desc += f" Ø{diameter_mm} mm"
            if height_mm:
                desc += f" × {height_mm} mm height"
            plan.append(desc)
        elif shape == "block":
            desc = "Extrude rectangular block"
            if diameter_mm:  # Using diameter as width for blocks
                desc += f" {diameter_mm} mm wide"
            if height_mm:
                desc += f" × {height_mm} mm height"
            plan.append(desc)
        else:
            desc = "Extrude feature"
            if height_mm:
                desc += f" {height_mm} mm height"
            plan.append(desc)
    
    elif action == "create_hole":
        desc = "Create"
        if count and count > 1:
            desc += f" {count} holes"
        else:
            desc += " hole"
        if diameter_mm:
            desc += f" Ø{diameter_mm} mm"
        plan.append(desc)
        
        # Add pattern information if present
        if pattern and pattern.get("type"):
            pattern_type = pattern.get("type")
            pattern_count = pattern.get("count", count)
            if pattern_type == "circular":
                pattern_desc = f"Arrange in circular pattern"
                if pattern_count:
                    pattern_desc += f" ({pattern_count} instances)"
                if pattern.get("angle_deg"):
                    pattern_desc += f" at {pattern.get('angle_deg')}° spacing"
                plan.append(pattern_desc)
            elif pattern_type == "linear":
                pattern_desc = f"Arrange in linear pattern"
                if pattern_count:
                    pattern_desc += f" ({pattern_count} instances)"
                plan.append(pattern_desc)
    
    elif action == "pattern":
        desc = "Create pattern"
        if count:
            desc += f" of {count} features"
        if pattern and pattern.get("type"):
            desc += f" in {pattern.get('type')} arrangement"
        plan.append(desc)
    
    elif action == "fillet":
        desc = "Apply fillet"
        if diameter_mm:  # Using diameter as radius for fillets
            desc += f" with {diameter_mm/2} mm radius"
        plan.append(desc)
    
    elif action == "create_feature":
        # Generic feature creation
        if shape == "cylinder":
            desc = "Create cylinder"
            if diameter_mm:
                desc += f" Ø{diameter_mm} mm"
            if height_mm:
                desc += f" × {height_mm} mm height"
            plan.append(desc)
        elif shape:
            desc = f"Create {shape}"
            if diameter_mm:
                desc += f" {diameter_mm} mm"
            if height_mm:
                desc += f" × {height_mm} mm"
            plan.append(desc)
        else:
            # Check if it's a plate with holes pattern
            if count and count > 1:
                desc = f"Create base plate"
                if height_mm:
                    desc += f" {height_mm} mm thick"
                plan.append(desc)
                
                hole_desc = f"Pattern {count} holes"
                if diameter_mm:
                    hole_desc += f" Ø{diameter_mm} mm"
                if pattern and pattern.get("type"):
                    hole_desc += f" in {pattern.get('type')} arrangement"
                plan.append(hole_desc)
            else:
                plan.append("Create CAD feature")
    
    else:
        plan.append(f"Execute {action} operation")
    
    # If no plan was generated, provide a fallback
    if not plan:
        plan.append("Process CAD instruction")
    
    return plan


def parse_instruction_internal(text: str, use_ai: bool) -> dict:
    """
    Internal helper function to parse natural language CAD instructions.
    
    This function encapsulates the parsing logic that can be used by both
    /process_instruction and /generate_model endpoints. It handles AI parsing
    attempts with fallback to rule-based parsing.
    
    Parsing Logic:
    - If use_ai=True and OPENAI_API_KEY is set: attempts AI parsing via OpenAI
    - If AI fails or use_ai=False: falls back to rule-based parsing
    - Always returns consistent normalized format with source indication
    
    Args:
        text (str): Natural language instruction to parse
        use_ai (bool): Whether to attempt AI parsing first
        
    Returns:
        dict: Parsed result with structure:
            {
                "result": {
                    "action": str,
                    "parameters": {
                        "count": Optional[int],
                        "diameter_mm": Optional[float],
                        "height_mm": Optional[float],
                        "shape": Optional[str],
                        "pattern": Optional[dict]
                    }
                },
                "source": str  # "ai" or "rule"
            }
    """
    logger.info(f"Parsing instruction: '{text}' (use_ai={use_ai})")
    
    parsed_result = None
    source = "rule"  # Default to rule-based
    
    # Try AI parsing if requested and API key is available
    if use_ai and os.getenv("OPENAI_API_KEY"):
        try:
            logger.info("Attempting AI parsing with OpenAI")
            parsed_result = parse_instruction_with_ai(text)
            source = "ai"
            logger.info(f"AI parsing successful: {parsed_result}")
        except LLMParseError as e:
            logger.warning(f"AI parsing failed, falling back to rules: {e}")
            parsed_result = None  # Will trigger fallback
        except Exception as e:
            logger.error(f"Unexpected error in AI parsing, falling back to rules: {e}")
            parsed_result = None  # Will trigger fallback
    elif use_ai:
        logger.info("AI requested but OPENAI_API_KEY not configured, using rule-based parsing")
    
    # Fallback to rule-based parsing if AI failed or wasn't used
    if parsed_result is None:
        logger.info("Using rule-based parsing")
        parsed_params = parse_cad_instruction(text)
        source = "rule"
        
        # Convert rule-based result to normalized format
        parsed_result = {
            "action": parsed_params.action,
            "parameters": {
                "count": parsed_params.count,
                "diameter_mm": parsed_params.diameter_mm,
                "height_mm": parsed_params.height_mm,
                "shape": parsed_params.shape,  # Always include shape field (null if not detected)
                "pattern": parsed_params.pattern.dict() if parsed_params.pattern else None
            }
        }
    
    # Log parsing results
    logger.info(f"Parsing complete - Source: {source}, Action: {parsed_result.get('action')}, "
               f"Parameters: {parsed_result.get('parameters')}")
    
    return {
        "result": parsed_result,
        "source": source
    }


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
        r'(?:make|create)\s*([0-9]+)',
        r'([0-9]+)\s*(?:cylinder|cylinders|block|blocks|sphere|spheres|cone|cones|hole|holes|feature|features)'
    ]
    
    for pattern in count_patterns:
        match = re.search(pattern, instruction_lower)
        if match:
            try:
                count = int(match.group(1))
                break
            except (ValueError, IndexError):
                continue
    
    # Pattern extraction
    pattern_info = None
    if action == "pattern":
        pattern_type = None
        pattern_count = count  # Use the count we already extracted
        pattern_angle = None
        
        # Detect pattern type
        if any(word in instruction_lower for word in ["circle", "circular", "round", "around"]):
            pattern_type = "circular"
        elif any(word in instruction_lower for word in ["line", "linear", "row", "straight"]):
            pattern_type = "linear"
        
        # Extract angle for circular patterns
        if pattern_type == "circular":
            angle_patterns = [
                r'([0-9]*\.?[0-9]+)\s*(?:degree|degrees|deg|°)',
                r'(?:angle|rotation)\s*(?:of\s*)?([0-9]*\.?[0-9]+)'
            ]
            for angle_pattern in angle_patterns:
                match = re.search(angle_pattern, instruction_lower)
                if match:
                    try:
                        pattern_angle = float(match.group(1))
                        break
                    except (ValueError, IndexError):
                        continue
        
        # Create pattern info if we detected a pattern type
        if pattern_type:
            pattern_info = PatternInfo(
                type=pattern_type,
                count=pattern_count,
                angle_deg=pattern_angle
            )
    
    # Always return consistent structure with all fields present
    # Missing/undetected values are explicitly set to None for consistent JSON shape
    return ParsedParameters(
        action=action,
        shape=shape,
        height_mm=height_mm,  # Always in millimeters (documented by *_mm suffix)
        diameter_mm=diameter_mm,  # Always in millimeters (documented by *_mm suffix)
        count=count,
        pattern=pattern_info
    )


@app.post("/process_instruction")
async def process_instruction(request: InstructionRequest, db: Session = Depends(get_db)) -> InstructionResponse:
    """
    Process natural language CAD instruction, extract parameters, and save to database.
    
    This endpoint takes a natural language instruction, optionally uses AI/LLM parsing
    when use_ai=True and OPENAI_API_KEY is configured, otherwise falls back to
    rule-based parsing. Saves the command to database and returns normalized response.
    
    Parsing Logic:
    - If use_ai=True and OPENAI_API_KEY is set: attempts AI parsing via OpenAI
    - If AI fails or use_ai=False: falls back to rule-based parsing
    - Always returns consistent response format with source indication
    
    Validation:Prompt S8-B (endpoint)

Add POST /generate_model with body:

{ "instruction": "...", "use_ai": false, "export_step": true, "export_stl": false }


Steps:

Parse via parse_instruction_internal.

dispatch_build(action, params) → CadQuery solid.

Export STEP/STL based on flags; return { step_url, stl_url, summary }.

Mount /outputs if not already.
    - Instructions must be at least 3 characters long and non-empty
    - Returns 422 with clear error message for invalid instructions
    
    Database Persistence:
    - Saves each processed command to the database for history tracking
    - Parameters are serialized to JSON string for storage
    
    Args:
        request (InstructionRequest): Request containing instruction text and use_ai flag
        db (Session): Database session dependency
        
    Returns:
        InstructionResponse: Normalized response with instruction, source, and parsed_parameters
        
    Raises:
        HTTPException: 422 if instruction validation fails, 500 for database errors
        
    Example:
        Input: {"instruction": "create a 5mm hole", "use_ai": true}
        Output: {
            "instruction": "create a 5mm hole",
            "source": "ai",
            "parsed_parameters": {
                "action": "create_hole",
                "parameters": {
                    "diameter_mm": 5,
                    "count": null,
                    "height_mm": null,
                    "pattern": null
                }
            }
        }
    """
    # Log incoming request with AI flag
    logger.info(f"Processing instruction: '{request.instruction}' (use_ai={request.use_ai})")
    
    try:
        # Use the refactored parsing helper
        parse_result = parse_instruction_internal(request.instruction, request.use_ai)
        parsed_result = parse_result["result"]
        source = parse_result["source"]
        
        # Serialize parameters for database storage
        parameters_json = json.dumps(parsed_result.get("parameters", {}))
        
        # Create and save Command record to database
        logger.info(f"Saving command to database: action='{parsed_result.get('action')}'")
        
        db_command = Command(
            prompt=request.instruction,
            action=parsed_result.get("action", "unknown"),
            parameters=parameters_json
        )
        
        db.add(db_command)
        db.commit()
        db.refresh(db_command)  # Refresh to get auto-generated ID and timestamp
        
        logger.info(f"Command saved successfully with ID: {db_command.id} (source: {source})")
        
        # Generate human-readable plan
        plan = generate_plan_from_parsed(parsed_result)
        
        # Create normalized response
        response = InstructionResponse(
            schema_version="1.0",
            instruction=request.instruction,
            source=source,
            plan=plan,
            parsed_parameters=parsed_result
        )
        
        logger.info(f"Successfully processed instruction with ID: {db_command.id} using {source} parsing")
        return response
        
    except Exception as e:
        logger.error(f"Error processing instruction '{request.instruction}': {str(e)}")
        # Rollback transaction on error
        db.rollback()
        raise HTTPException(
            status_code=500,
            detail=f"Internal error processing instruction: {str(e)}"
        )


@app.post("/dry_run")
async def dry_run_instruction(request: InstructionRequest) -> InstructionResponse:
    """
    Preview CAD instruction parsing without executing or saving to database.
    
    This endpoint performs a "dry run" of instruction parsing, returning the same
    structured response as /process_instruction but WITHOUT:
    - Saving to the database
    - Generating actual geometry
    - Creating any side effects
    
    Perfect for:
    - Previewing what will happen before execution
    - Validating instruction syntax
    - Testing parsing logic (AI or rule-based)
    - SolidWorks Add-In contract validation
    
    The response includes a human-readable plan array that describes the steps
    that would be executed if this were a real operation.
    
    Args:
        request (InstructionRequest): Request containing instruction text and use_ai flag
        
    Returns:
        InstructionResponse: Preview response with schema_version, source, plan, and parsed_parameters
        
    Raises:
        HTTPException: 422 if instruction validation fails
        
    Example:
        Input: {"instruction": "add 4 holes on the top face", "use_ai": false}
        Output: {
            "schema_version": "1.0",
            "instruction": "add 4 holes on the top face",
            "source": "rule",
            "plan": [
                "Create 4 holes",
                "Arrange in circular pattern (4 instances)"
            ],
            "parsed_parameters": {
                "action": "create_hole",
                "parameters": {
                    "count": 4,
                    "diameter_mm": null,
                    "height_mm": null,
                    "shape": null,
                    "pattern": null
                }
            }
        }
    """
    # Log incoming request
    logger.info(f"Dry run for instruction: '{request.instruction}' (use_ai={request.use_ai})")
    
    try:
        # Parse instruction using the shared helper (no database interaction)
        parse_result = parse_instruction_internal(request.instruction, request.use_ai)
        parsed_result = parse_result["result"]
        source = parse_result["source"]
        
        # Generate human-readable plan
        plan = generate_plan_from_parsed(parsed_result)
        
        # Create response (no database save)
        response = InstructionResponse(
            schema_version="1.0",
            instruction=request.instruction,
            source=source,
            plan=plan,
            parsed_parameters=parsed_result
        )
        
        logger.info(f"Dry run complete - Source: {source}, Plan steps: {len(plan)}")
        return response
        
    except Exception as e:
        logger.error(f"Error in dry run for instruction '{request.instruction}': {str(e)}")
        raise HTTPException(
            status_code=500,
            detail=f"Internal error during dry run: {str(e)}"
        )


@app.get("/commands")
async def get_commands(
    limit: int = 20,
    db: Session = Depends(get_db)
) -> List[CommandOut]:
    """
    Retrieve command history in reverse chronological order.
    
    Returns a list of previously processed CAD commands, ordered by creation
    time (most recent first). Useful for viewing command history, debugging,
    and analyzing user interaction patterns.
    
    Query Parameters:
        limit (int): Maximum number of commands to return.
                    Default: 20, Maximum: 100
                    Values above 100 are clamped to 100 for performance.
    
    Database Query:
        - Orders by created_at DESC (newest first)
        - Applies limit for pagination
        - Parses JSON parameters back to dictionary format
    
    Args:
        limit (int): Number of commands to retrieve (default 20, max 100)
        db (Session): Database session dependency
        
    Returns:
        List[CommandOut]: List of command records with parsed parameters
        
    Raises:
        HTTPException: 500 for database errors
        
    Example Response:
        [
            {
                "id": 3,
                "prompt": "extrude a 1mm tall box that is 3mm wide",
                "action": "extrude",
                "parameters": {
                    "shape": "block",
                    "height_mm": 1.0,
                    "diameter_mm": 3.0,
                    "count": null
                },
                "created_at": "2025-08-13T16:27:41.123456"
            },
            {
                "id": 2,
                "prompt": "extrude a 1mm tall square with 3mm length",
                "action": "extrude",
                "parameters": {
                    "shape": null,
                    "height_mm": 1.0,
                    "diameter_mm": null,
                    "count": null
                },
                "created_at": "2025-08-13T16:25:09.123456"
            }
        ]
    """
    # Clamp limit to maximum of 100 for performance
    if limit > 100:
        limit = 100
        logger.warning(f"Limit clamped to maximum of 100")
    
    # Log the request
    logger.info(f"Retrieving {limit} commands in reverse chronological order")
    
    try:
        # Query commands ordered by created_at DESC (newest first)
        db_commands = db.query(Command).order_by(Command.created_at.desc()).limit(limit).all()
        
        logger.info(f"Retrieved {len(db_commands)} commands from database")
        
        # Convert to response models, parsing JSON parameters back to dict
        commands_out = []
        for db_command in db_commands:
            try:
                # Parse JSON parameters back to dictionary
                parameters_dict = json.loads(db_command.parameters) if db_command.parameters else {}
            except json.JSONDecodeError as e:
                logger.warning(f"Failed to parse parameters for command {db_command.id}: {e}")
                # Fallback to empty dict if JSON parsing fails
                parameters_dict = {}
            
            command_out = CommandOut(
                id=db_command.id,
                prompt=db_command.prompt,
                action=db_command.action,
                parameters=parameters_dict,
                created_at=db_command.created_at
            )
            commands_out.append(command_out)
        
        logger.info(f"Successfully processed {len(commands_out)} commands for response")
        return commands_out
        
    except Exception as e:
        logger.error(f"Error retrieving commands: {str(e)}")
        raise HTTPException(
            status_code=500,
            detail=f"Internal error retrieving commands: {str(e)}"
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


# Job Management Endpoints
class GenerateModelRequest(BaseModel):
    """
    Request model for generating CAD models with file exports.
    
    Attributes:
        instruction (str): Natural language instruction for CAD operation
        use_ai (bool): Whether to use AI/LLM for parsing (default: False)
        export_step (bool): Whether to export STEP file (default: True)
        export_stl (bool): Whether to export STL file (default: False)
    """
    instruction: str
    use_ai: bool = False
    export_step: bool = True
    export_stl: bool = False
    
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
            raise ValueError("Instruction cannot be blank")
        if len(v.strip()) < 3:
            raise ValueError("Instruction must be at least 3 characters long")
        return v.strip()


class GenerateModelResponse(BaseModel):
    """
    Response model for generated CAD models with download links.
    
    Attributes:
        instruction (str): Original instruction text
        source (str): Parsing source - "ai" or "rule"
        step_url (Optional[str]): Download URL for STEP file if exported
        stl_url (Optional[str]): Download URL for STL file if exported
        summary (str): Summary of the generated model and operations performed
    """
    instruction: str
    source: str
    step_url: Optional[str] = None
    stl_url: Optional[str] = None
    summary: str


class JobRequest(BaseModel):
    """
    Request model for starting a new job.
    
    Attributes:
        command_id (Optional[int]): ID of a saved command to associate with this job
    """
    command_id: Optional[int] = None


@app.post("/generate_model")
async def generate_model(request: GenerateModelRequest) -> GenerateModelResponse:
    """
    Generate CAD model from natural language instruction and export to files.
    
    This endpoint processes a natural language instruction, builds a 3D CAD model,
    and exports it to the requested file formats (STEP/STL). Returns download URLs
    for the generated files.
    
    Workflow:
    1. Parse instruction using parse_instruction_internal helper
    2. Build 3D model using dispatch_build from geometry.model_builder
    3. Export to STEP/STL files as requested using geometry.exporter
    4. Return download URLs and summary
    
    Args:
        request (GenerateModelRequest): Request with instruction and export options
        
    Returns:
        GenerateModelResponse: Response with download URLs and model summary
        
    Raises:
        HTTPException: 422 for validation errors, 500 for processing errors
        
    Example:
        Input: {
            "instruction": "create a cylinder 20mm high and 15mm diameter",
            "use_ai": true,
            "export_step": true,
            "export_stl": false
        }
        Output: {
            "instruction": "create a cylinder 20mm high and 15mm diameter",
            "source": "ai",
            "step_url": "/outputs/cylinder_20250814_014500_a1b2c3d4.step",
            "stl_url": null,
            "summary": "Generated cylinder with 15mm diameter and 20mm height. Exported STEP file."
        }
    """
    logger.info(f"Generating model for instruction: '{request.instruction}' (use_ai={request.use_ai}, export_step={request.export_step}, export_stl={request.export_stl})")
    
    # Check if geometry functionality is available
    if not GEOMETRY_AVAILABLE:
        logger.error("Geometry functionality not available - CadQuery dependencies missing")
        raise HTTPException(
            status_code=503,
            detail="CAD model generation not available. CadQuery dependencies are not properly installed. Please install: pip install cadquery"
        )
    
    try:
        # Step 1: Parse instruction using the refactored helper
        parse_result = parse_instruction_internal(request.instruction, request.use_ai)
        parsed_result = parse_result["result"]
        source = parse_result["source"]
        
        action = parsed_result.get("action", "unknown")
        parameters = parsed_result.get("parameters", {})
        
        logger.info(f"Parsed instruction - Action: {action}, Parameters: {parameters}")
        
        # Step 2: Build 3D model using dispatch_build
        logger.info("Building 3D model...")
        solid = dispatch_build(action, parameters)
        logger.info("3D model built successfully")
        
        # Step 3: Export files as requested
        step_url = None
        stl_url = None
        exported_files = []
        
        # Ensure outputs directory exists
        ensure_outputs_directory()
        
        # Export STEP file if requested
        if request.export_step:
            try:
                logger.info("Exporting STEP file...")
                step_path = export_solid(solid, kind="step", prefix="model")
                step_url = f"/{step_path}"  # Add leading slash for URL
                exported_files.append("STEP")
                logger.info(f"STEP file exported: {step_url}")
            except Exception as e:
                logger.error(f"Failed to export STEP file: {str(e)}")
                raise HTTPException(
                    status_code=500,
                    detail=f"Failed to export STEP file: {str(e)}"
                )
        
        # Export STL file if requested
        if request.export_stl:
            try:
                logger.info("Exporting STL file...")
                stl_path = export_solid(solid, kind="stl", prefix="model")
                stl_url = f"/{stl_path}"  # Add leading slash for URL
                exported_files.append("STL")
                logger.info(f"STL file exported: {stl_url}")
            except Exception as e:
                logger.error(f"Failed to export STL file: {str(e)}")
                raise HTTPException(
                    status_code=500,
                    detail=f"Failed to export STL file: {str(e)}"
                )
        
        # Step 4: Generate summary
        shape_info = parameters.get("shape", "feature")
        diameter = parameters.get("diameter_mm")
        height = parameters.get("height_mm")
        count = parameters.get("count")
        
        # Build descriptive summary
        summary_parts = []
        
        if action == "create_hole":
            if count and count > 1:
                summary_parts.append(f"Generated {count} holes")
            else:
                summary_parts.append("Generated hole")
            
            if diameter:
                summary_parts.append(f"with {diameter}mm diameter")
        
        elif action == "create_feature" and shape_info == "cylinder":
            summary_parts.append("Generated cylinder")
            if diameter:
                summary_parts.append(f"with {diameter}mm diameter")
            if height:
                summary_parts.append(f"and {height}mm height")
        
        else:
            summary_parts.append(f"Generated {shape_info or 'CAD model'}")
            if diameter:
                summary_parts.append(f"with {diameter}mm diameter")
            if height:
                summary_parts.append(f"and {height}mm height")
        
        # Add export information
        if exported_files:
            summary_parts.append(f"Exported {' and '.join(exported_files)} file{'s' if len(exported_files) > 1 else ''}")
        
        summary = ". ".join(summary_parts) + "."
        
        # Create response
        response = GenerateModelResponse(
            instruction=request.instruction,
            source=source,
            step_url=step_url,
            stl_url=stl_url,
            summary=summary
        )
        
        logger.info(f"Model generation complete - Summary: {summary}")
        return response
        
    except HTTPException:
        # Re-raise HTTP exceptions as-is
        raise
    except Exception as e:
        logger.error(f"Error generating model for instruction '{request.instruction}': {str(e)}")
        raise HTTPException(
            status_code=500,
            detail=f"Internal error generating model: {str(e)}"
        )


@app.post("/jobs")
async def create_job(request: JobRequest) -> Dict:
    """
    Start a new async job for CAD processing.
    
    Creates a new job that simulates long-running CAD work. The job will
    progress from 0 to 100% over time and can be monitored via the GET /jobs/{job_id} endpoint.
    
    Args:
        request (JobRequest): Request containing optional command_id to associate
    
    Returns:
        Dict: Job creation response with job_id, status, and progress
        
    Example Response:
        {
            "job_id": "550e8400-e29b-41d4-a716-446655440000",
            "status": "queued",
            "progress": 0
        }
    """
    logger.info(f"Starting new job with command_id: {request.command_id}")
    
    # Prepare metadata for the job
    meta = {"command_id": request.command_id} if request.command_id else {}
    
    # Start the job
    job_id = start_job(meta=meta)
    
    logger.info(f"Created job {job_id} successfully")
    
    return {
        "job_id": job_id,
        "status": "queued",
        "progress": 0
    }


@app.get("/jobs/{job_id}")
async def get_job_status(job_id: str) -> Dict:
    """
    Get the current status of a job by its ID.
    
    Retrieves the current state of a running or completed job, including
    progress percentage, status, and any error information.
    
    Args:
        job_id (str): The unique job identifier
        
    Returns:
        Dict: Current job state with status, progress, and metadata
        
    Raises:
        HTTPException: 404 if job is not found
        
    Example Response:
        {
            "job_id": "550e8400-e29b-41d4-a716-446655440000",
            "status": "running",
            "progress": 45,
            "error": null,
            "created_at": "2025-08-13T19:00:00.000000",
            "updated_at": "2025-08-13T19:00:15.000000"
        }
    """
    logger.info(f"Retrieving status for job {job_id}")
    
    # Get job from the job runner
    job_data = get_job(job_id)
    
    if job_data is None:
        logger.warning(f"Job {job_id} not found")
        raise HTTPException(
            status_code=404,
            detail=f"Job with ID '{job_id}' not found. Please check the job ID and try again."
        )
    
    logger.info(f"Retrieved job {job_id} with status: {job_data['status']}, progress: {job_data['progress']}%")
    
    return job_data
