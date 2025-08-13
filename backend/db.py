"""
Database configuration and session management for Text-to-CAD application.

This module sets up SQLite database connection, session management, and provides
dependency injection for database sessions in FastAPI endpoints.
"""

from sqlalchemy import create_engine
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker, Session
from typing import Generator

# SQLite database URL - creates app.db file in current directory
SQLITE_DATABASE_URL = "sqlite:///./app.db"

# Create SQLAlchemy engine
# connect_args={"check_same_thread": False} is needed for SQLite to work with FastAPI
engine = create_engine(
    SQLITE_DATABASE_URL, 
    connect_args={"check_same_thread": False}
)

# Create SessionLocal class for database sessions
# autocommit=False: transactions must be explicitly committed
# autoflush=False: changes aren't automatically flushed to DB
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

# Create Base class for declarative models
Base = declarative_base()


def get_db() -> Generator[Session, None, None]:
    """
    Database dependency for FastAPI endpoints.
    
    Creates a new database session for each request and ensures it's properly
    closed after the request is completed, even if an exception occurs.
    
    Yields:
        Session: SQLAlchemy database session
        
    Usage:
        @app.get("/endpoint")
        def endpoint(db: Session = Depends(get_db)):
            # Use db session here
            pass
    """
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()
