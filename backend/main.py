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
from pydantic import BaseModel, validator
from typing import Dict, Optional, Union, List
from sqlalchemy.orm import Session

# Import configuration
from config import config

# Import database components
from db import engine, Base, SessionLocal, get_db
from models import Command

# Import AI/LLM functionality
from llm import parse_instruction_with_ai, LLMParseError

# Import job runner functionality
from jobs import start_job, get_job

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
        source (str): Parsing source - "ai" or "rule"
        parsed_parameters (Dict): Extracted CAD parameters (always present, with nulls where unknown)
    """
    instruction: str
    source: str  # "ai" or "rule"
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
    
    Validation:
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
        parsed_result = None
        source = "rule"  # Default to rule-based
        
        # Try AI parsing if requested and API key is available
        if request.use_ai and os.getenv("OPENAI_API_KEY"):
            try:
                logger.info("Attempting AI parsing with OpenAI")
                parsed_result = parse_instruction_with_ai(request.instruction)
                source = "ai"
                logger.info(f"AI parsing successful: {parsed_result}")
            except LLMParseError as e:
                logger.warning(f"AI parsing failed, falling back to rules: {e}")
                parsed_result = None  # Will trigger fallback
            except Exception as e:
                logger.error(f"Unexpected error in AI parsing, falling back to rules: {e}")
                parsed_result = None  # Will trigger fallback
        elif request.use_ai:
            logger.info("AI requested but OPENAI_API_KEY not configured, using rule-based parsing")
        
        # Fallback to rule-based parsing if AI failed or wasn't used
        if parsed_result is None:
            logger.info("Using rule-based parsing")
            parsed_params = parse_cad_instruction(request.instruction)
            source = "rule"
            
            # Convert rule-based result to normalized format
            parsed_result = {
                "action": parsed_params.action,
                "parameters": {
                    "count": parsed_params.count,
                    "diameter_mm": parsed_params.diameter_mm,
                    "height_mm": parsed_params.height_mm,
                    "pattern": None  # Rule-based parsing doesn't support patterns yet
                }
            }
            # Add shape info if available (rule-based specific)
            if hasattr(parsed_params, 'shape') and parsed_params.shape:
                parsed_result["parameters"]["shape"] = parsed_params.shape
        
        # Log parsing results
        logger.info(f"Parsing complete - Source: {source}, Action: {parsed_result.get('action')}, "
                   f"Parameters: {parsed_result.get('parameters')}")
        
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
        
        # Create normalized response
        response = InstructionResponse(
            instruction=request.instruction,
            source=source,
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
class JobRequest(BaseModel):
    """
    Request model for starting a new job.
    
    Attributes:
        command_id (Optional[int]): ID of a saved command to associate with this job
    """
    command_id: Optional[int] = None


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
