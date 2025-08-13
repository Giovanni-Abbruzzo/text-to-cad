"""
Configuration module for Text-to-CAD Backend

This module handles loading configuration from environment variables using python-dotenv.
It provides default values and graceful fallbacks when .env file is not present.

Author: Text-to-CAD Team
"""

import os
from typing import Optional
from dotenv import load_dotenv

# Load .env file if present (gracefully handles absence)
load_dotenv()


class Config:
    """
    Configuration class that loads settings from environment variables.
    
    Provides sensible defaults and handles missing .env files gracefully.
    All behavior works correctly if .env is absent.
    """
    
    # Development Settings
    DEBUG: bool = os.getenv("DEBUG", "false").lower() == "true"
    LOG_LEVEL: str = os.getenv("LOG_LEVEL", "info").lower()
    
    # AI/LLM Configuration
    USE_LLM: bool = os.getenv("USE_LLM", "false").lower() == "true"
    OPENAI_API_KEY: Optional[str] = os.getenv("OPENAI_API_KEY")
    OPENAI_MODEL: str = os.getenv("OPENAI_MODEL", "gpt-4")
    OPENAI_MAX_TOKENS: int = int(os.getenv("OPENAI_MAX_TOKENS", "1000"))
    
    # Database Configuration (for future use)
    DATABASE_URL: str = os.getenv("DATABASE_URL", "sqlite:///./text_to_cad.db")
    DB_ECHO: bool = os.getenv("DB_ECHO", "false").lower() == "true"
    
    # API Configuration
    API_HOST: str = os.getenv("API_HOST", "127.0.0.1")
    API_PORT: int = int(os.getenv("API_PORT", "8000"))
    CORS_ORIGINS: list = os.getenv("CORS_ORIGINS", "http://localhost:5173,*").split(",")
    
    # Security (for future use)
    SECRET_KEY: Optional[str] = os.getenv("SECRET_KEY")
    ACCESS_TOKEN_EXPIRE_MINUTES: int = int(os.getenv("ACCESS_TOKEN_EXPIRE_MINUTES", "30"))
    
    @classmethod
    def get_config_info(cls) -> dict:
        """
        Get current configuration information (excluding sensitive data).
        
        Returns:
            dict: Configuration information safe for logging/debugging
        """
        return {
            "debug": cls.DEBUG,
            "log_level": cls.LOG_LEVEL,
            "use_llm": cls.USE_LLM,
            "openai_model": cls.OPENAI_MODEL if cls.USE_LLM else "N/A",
            "openai_api_key_set": bool(cls.OPENAI_API_KEY),
            "api_host": cls.API_HOST,
            "api_port": cls.API_PORT,
            "cors_origins": cls.CORS_ORIGINS,
            "database_url": cls.DATABASE_URL.replace(cls.SECRET_KEY or "", "***") if cls.SECRET_KEY else cls.DATABASE_URL
        }


# Global config instance
config = Config()
