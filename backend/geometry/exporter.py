"""
CAD File Exporter for Text-to-CAD Plugin

This module provides functions to export CadQuery solids to various file formats
(STEP, STL) and manage output file paths for download via FastAPI.

Author: Text-to-CAD Team
"""

import os
import uuid
from datetime import datetime
from pathlib import Path
from typing import Union, Literal
import cadquery as cq
import logging

# Configure logging
logger = logging.getLogger(__name__)

# Define output directory relative to backend
OUTPUTS_DIR = Path(__file__).parent.parent / "outputs"


def ensure_outputs_directory() -> Path:
    """
    Ensure the outputs directory exists and return its path.
    
    Returns:
        Path: Path to the outputs directory
    """
    OUTPUTS_DIR.mkdir(exist_ok=True)
    logger.info(f"Outputs directory ensured at: {OUTPUTS_DIR.absolute()}")
    return OUTPUTS_DIR


def generate_filename(kind: str, prefix: str = "model") -> str:
    """
    Generate a unique filename for exported files.
    
    Args:
        kind (str): File extension/type ("step", "stl")
        prefix (str): Filename prefix (default: "model")
        
    Returns:
        str: Unique filename with timestamp and UUID
        
    Example:
        "model_20250814_010615_a1b2c3d4.step"
    """
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    unique_id = str(uuid.uuid4())[:8]  # First 8 chars of UUID
    
    # Normalize file extension
    extension = kind.lower()
    if extension == "step":
        extension = "step"
    elif extension == "stl":
        extension = "stl"
    else:
        extension = "step"  # Default to STEP
    
    filename = f"{prefix}_{timestamp}_{unique_id}.{extension}"
    logger.info(f"Generated filename: {filename}")
    return filename


def export_solid(solid: cq.Workplane, kind: Literal["step", "stl"] = "step", prefix: str = "model") -> str:
    """
    Export CadQuery solid to specified file format and return the file path.
    
    Args:
        solid (cq.Workplane): The CadQuery workplane/solid to export
        kind (Literal["step", "stl"]): Export format - "step" or "stl"
        prefix (str): Filename prefix (default: "model")
        
    Returns:
        str: Relative path to the exported file (relative to backend directory)
        
    Raises:
        ValueError: If unsupported export format is specified
        RuntimeError: If export operation fails
        
    Example:
        solid = build_extruded_cylinder({"diameter_mm": 20, "height_mm": 30})
        path = export_solid(solid, kind="step", prefix="cylinder")
        # Returns: "outputs/cylinder_20250814_010615_a1b2c3d4.step"
    """
    # Validate export format
    if kind not in ["step", "stl"]:
        raise ValueError(f"Unsupported export format: {kind}. Must be 'step' or 'stl'")
    
    # Ensure outputs directory exists
    outputs_dir = ensure_outputs_directory()
    
    # Generate unique filename
    filename = generate_filename(kind, prefix)
    filepath = outputs_dir / filename
    
    logger.info(f"Exporting {kind.upper()} file: {filepath}")
    
    try:
        # Export using CadQuery exporters
        if kind == "step":
            cq.exporters.export(solid, str(filepath))
        elif kind == "stl":
            cq.exporters.export(solid, str(filepath))
        
        # Verify file was created
        if not filepath.exists():
            raise RuntimeError(f"Export failed - file not created: {filepath}")
        
        file_size = filepath.stat().st_size
        logger.info(f"Export successful: {filepath} ({file_size} bytes)")
        
        # Return relative path from backend directory
        relative_path = f"outputs/{filename}"
        return relative_path
        
    except Exception as e:
        logger.error(f"Failed to export {kind.upper()} file: {str(e)}")
        raise RuntimeError(f"Export failed: {str(e)}") from e


def export_step(solid: cq.Workplane, prefix: str = "model") -> str:
    """
    Export CadQuery solid to STEP format.
    
    Convenience function for STEP export.
    
    Args:
        solid (cq.Workplane): The CadQuery workplane/solid to export
        prefix (str): Filename prefix (default: "model")
        
    Returns:
        str: Relative path to the exported STEP file
    """
    return export_solid(solid, kind="step", prefix=prefix)


def export_stl(solid: cq.Workplane, prefix: str = "model") -> str:
    """
    Export CadQuery solid to STL format.
    
    Convenience function for STL export.
    
    Args:
        solid (cq.Workplane): The CadQuery workplane/solid to export
        prefix (str): Filename prefix (default: "model")
        
    Returns:
        str: Relative path to the exported STL file
    """
    return export_solid(solid, kind="stl", prefix=prefix)


def list_output_files() -> list[dict]:
    """
    List all files in the outputs directory with metadata.
    
    Returns:
        list[dict]: List of file information dictionaries
        
    Example:
        [
            {
                "filename": "model_20250814_010615_a1b2c3d4.step",
                "size_bytes": 12345,
                "created": "2025-08-14T01:06:15",
                "type": "step"
            }
        ]
    """
    outputs_dir = ensure_outputs_directory()
    files = []
    
    try:
        for filepath in outputs_dir.iterdir():
            if filepath.is_file():
                stat = filepath.stat()
                file_info = {
                    "filename": filepath.name,
                    "size_bytes": stat.st_size,
                    "created": datetime.fromtimestamp(stat.st_ctime).isoformat(),
                    "type": filepath.suffix.lstrip('.').lower()
                }
                files.append(file_info)
        
        # Sort by creation time (newest first)
        files.sort(key=lambda x: x["created"], reverse=True)
        logger.info(f"Listed {len(files)} output files")
        
    except Exception as e:
        logger.error(f"Failed to list output files: {str(e)}")
    
    return files


def cleanup_old_files(max_files: int = 50) -> int:
    """
    Clean up old output files, keeping only the most recent ones.
    
    Args:
        max_files (int): Maximum number of files to keep (default: 50)
        
    Returns:
        int: Number of files deleted
    """
    outputs_dir = ensure_outputs_directory()
    deleted_count = 0
    
    try:
        # Get all files sorted by creation time (oldest first)
        files = []
        for filepath in outputs_dir.iterdir():
            if filepath.is_file():
                files.append((filepath, filepath.stat().st_ctime))
        
        files.sort(key=lambda x: x[1])  # Sort by creation time
        
        # Delete oldest files if we exceed max_files
        if len(files) > max_files:
            files_to_delete = files[:-max_files]  # Keep the newest max_files
            
            for filepath, _ in files_to_delete:
                try:
                    filepath.unlink()
                    deleted_count += 1
                    logger.info(f"Deleted old file: {filepath.name}")
                except Exception as e:
                    logger.warning(f"Failed to delete {filepath.name}: {str(e)}")
        
        logger.info(f"Cleanup complete: deleted {deleted_count} old files")
        
    except Exception as e:
        logger.error(f"Failed to cleanup old files: {str(e)}")
    
    return deleted_count


# Example usage and testing
if __name__ == "__main__":
    # This would normally be run with actual CadQuery objects
    print("Exporter module loaded successfully")
    print(f"Outputs directory: {OUTPUTS_DIR.absolute()}")
    
    # Test directory creation
    ensure_outputs_directory()
    
    # Test filename generation
    test_filename = generate_filename("step", "test")
    print(f"Generated test filename: {test_filename}")
    
    # List any existing files
    files = list_output_files()
    print(f"Found {len(files)} existing output files")
