"""
Database models for Text-to-CAD application.

This module defines SQLAlchemy ORM models for storing CAD command history
and related data structures.
"""

from sqlalchemy import Column, Integer, String, Text, DateTime, Index
from sqlalchemy.sql import func
from db import Base


class Command(Base):
    """
    Model for storing CAD command history and parameters.
    
    This table stores each text-to-CAD conversion request along with
    the generated action and parameters for future reference and analysis.
    
    Attributes:
        id: Primary key, auto-incrementing integer
        prompt: The original natural language input from user
        action: The CAD action/command that was generated (e.g., 'create_box', 'extrude')
        parameters: JSON string containing command parameters (nullable)
        created_at: Timestamp when the command was created (indexed for performance)
    """
    
    __tablename__ = "commands"
    
    # Primary key - auto-incrementing integer
    id = Column(Integer, primary_key=True, autoincrement=True)
    
    # Original user prompt - required field, can be long text
    prompt = Column(Text, nullable=False, comment="Natural language input from user")
    
    # Generated CAD action/command - required, limited length for performance
    action = Column(String(64), nullable=False, comment="Generated CAD command type")
    
    # Command parameters as JSON string - optional field
    parameters = Column(Text, nullable=True, comment="JSON string of command parameters")
    
    # Creation timestamp - auto-populated, indexed for efficient queries
    created_at = Column(
        DateTime, 
        nullable=False, 
        default=func.now(), 
        comment="Timestamp when command was created"
    )
    
    def __repr__(self) -> str:
        """String representation of Command instance for debugging."""
        return f"<Command(id={self.id}, action='{self.action}', created_at='{self.created_at}')>"


# Create index on created_at for efficient time-based queries
Index('idx_commands_created_at', Command.created_at)
