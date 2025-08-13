"""
FastAPI Backend for Text-to-CAD Plugin

This module provides the main FastAPI application for the text-to-cad project.
It serves as the backend API that will eventually convert natural language 
instructions into structured CAD commands for applications like SolidWorks 
and Fusion360.

Currently implements:
- Health check endpoint for service monitoring
- CORS configuration for frontend integration

Author: Text-to-CAD Team
"""

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from typing import Dict

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
        "health": "/health"
    }
