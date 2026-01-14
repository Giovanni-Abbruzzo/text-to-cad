"""
CadQuery Model Builder for Text-to-CAD Plugin

This module provides functions to convert parsed JSON parameters into CadQuery 
geometry operations. It maps the current JSON schema to CadQuery operations
for building simple solids and exporting STEP/STL files.

All dimensions are in millimeters (mm) as per the project standard.

Author: Text-to-CAD Team
"""

import cadquery as cq
from typing import Dict, Any, Optional
import logging

# Configure logging
logger = logging.getLogger(__name__)


def build_plate_with_holes(params: Dict[str, Any]) -> cq.Workplane:
    """
    Build a base plate with circular hole pattern on top face.
    
    Creates a rectangular base plate and adds a pattern of circular holes
    on the top surface. Uses sensible defaults for missing parameters.
    
    Args:
        params (Dict[str, Any]): Parameters from parsed JSON schema
            Expected keys:
            - height_mm: Plate thickness (default: 10mm)
            - diameter_mm: Hole diameter (default: 5mm) 
            - count: Number of holes (default: 4)
            - pattern: Pattern information with type, count, angle_deg
            
    Returns:
        cq.Workplane: CadQuery workplane with plate and holes
        
    Example:
        params = {
            "height_mm": 15.0,
            "diameter_mm": 6.0,
            "count": 6,
            "pattern": {"type": "circular", "count": 6, "angle_deg": 60.0}
        }
        result = build_plate_with_holes(params)
    """
    # Extract parameters with sensible defaults
    plate_thickness = params.get("height_mm") or params.get("depth_mm") or 10.0  # Default 10mm thick
    hole_radius = params.get("radius_mm")
    hole_diameter = params.get("diameter_mm") or (hole_radius * 2.0 if hole_radius else None) or 5.0
    hole_count = params.get("count", 4)  # Default 4 holes

    # Base plate dimensions (sensible defaults)
    plate_length = params.get("length_mm") or params.get("width_mm") or params.get("diameter_mm") or 100.0
    plate_width = params.get("width_mm") or params.get("length_mm") or params.get("diameter_mm") or 80.0
    
    logger.info(f"Building plate: {plate_length}x{plate_width}x{plate_thickness}mm with {hole_count} holes of {hole_diameter}mm diameter")
    
    # Create base plate
    plate = (cq.Workplane("XY")
             .box(plate_length, plate_width, plate_thickness))
    
    # Extract pattern information
    pattern_info = params.get("pattern", {}) or {}
    pattern_type = pattern_info.get("type", "circular")
    pattern_count = pattern_info.get("count", hole_count)
    pattern_radius = pattern_info.get("radius_mm")
    pattern_angle = pattern_info.get("angle_deg") or 360.0
    
    # Create hole pattern
    if pattern_type == "circular":
        # Circular pattern of holes
        if not pattern_radius:
            pattern_radius = min(plate_length, plate_width) * 0.3  # 30% of smaller dimension
        angle_step = float(pattern_angle) / max(1, pattern_count)
        
        logger.info(f"Creating circular hole pattern: {pattern_count} holes at radius {pattern_radius}mm")
        
        # Create holes in circular pattern
        for i in range(pattern_count):
            angle = i * angle_step
            x = pattern_radius * cq.Vector(1, 0, 0).rotateZ(angle).x
            y = pattern_radius * cq.Vector(1, 0, 0).rotateZ(angle).y
            
            plate = (plate.faces(">Z")  # Select top face
                    .workplane()
                    .center(x, y)
                    .hole(hole_diameter))
    
    else:  # Linear pattern
        # Linear pattern of holes
        spacing = plate_length * 0.8 / max(1, pattern_count - 1)  # 80% of length
        start_x = -plate_length * 0.4  # Start at 40% from center
        
        logger.info(f"Creating linear hole pattern: {pattern_count} holes with {spacing}mm spacing")
        
        # Create holes in linear pattern
        for i in range(pattern_count):
            x = start_x + (i * spacing)
            y = 0  # Center line
            
            plate = (plate.faces(">Z")  # Select top face
                    .workplane()
                    .center(x, y)
                    .hole(hole_diameter))
    
    logger.info("Plate with holes created successfully")
    return plate


def build_block(params: Dict[str, Any]) -> cq.Workplane:
    """
    Build a rectangular block using width/length/height parameters.
    """
    width = params.get("width_mm") or params.get("length_mm") or params.get("diameter_mm") or 50.0
    length = params.get("length_mm") or params.get("width_mm") or params.get("diameter_mm") or 50.0
    height = params.get("height_mm") or params.get("depth_mm") or 10.0

    logger.info(f"Building block: {length}x{width}x{height}mm")
    return cq.Workplane("XY").box(length, width, height)


def build_extruded_cylinder(params: Dict[str, Any]) -> cq.Workplane:
    """
    Build a cylinder via extrude operation.
    
    Creates a circular sketch and extrudes it to form a cylinder.
    Uses sensible defaults for missing parameters.
    
    Args:
        params (Dict[str, Any]): Parameters from parsed JSON schema
            Expected keys:
            - diameter_mm: Cylinder diameter (default: 20mm)
            - height_mm: Cylinder height (default: 30mm)
            
    Returns:
        cq.Workplane: CadQuery workplane with extruded cylinder
        
    Example:
        params = {
            "diameter_mm": 25.0,
            "height_mm": 40.0
        }
        result = build_extruded_cylinder(params)
    """
    # Extract parameters with sensible defaults
    radius = params.get("radius_mm")
    diameter = params.get("diameter_mm") or (radius * 2.0 if radius else None) or 20.0
    height = params.get("height_mm", 30.0)      # Default 30mm height
    radius = diameter / 2.0
    
    logger.info(f"Building extruded cylinder: diameter={diameter}mm, height={height}mm")
    
    # Create cylinder via circle extrude
    cylinder = (cq.Workplane("XY")
                .circle(radius)
                .extrude(height))
    
    logger.info("Extruded cylinder created successfully")
    return cylinder


def dispatch_build(action: str, params: Dict[str, Any]) -> cq.Workplane:
    """
    Choose which builder to call based on action and parameters.
    
    Dispatches to the appropriate builder function based on the action
    and shape parameters. Defaults to plate with holes if no specific
    builder is matched.
    
    Args:
        action (str): The CAD action to perform
        params (Dict[str, Any]): Parameters from parsed JSON schema
        
    Returns:
        cq.Workplane: CadQuery workplane with built geometry
        
    Raises:
        ValueError: If unsupported action/shape combination is requested
        
    Example:
        # Build a cylinder
        result = dispatch_build("extrude", {"shape": "cylinder", "diameter_mm": 15, "height_mm": 25})
        
        # Build plate with holes (default)
        result = dispatch_build("create_feature", {"count": 6, "diameter_mm": 4})
    """
    logger.info(f"Dispatching build for action='{action}' with params: {params}")
    
    # Extract shape if available
    shape = params.get("shape", "").lower() if params.get("shape") else ""
    
    # Dispatch based on action and shape
    if action == "extrude" and shape == "cylinder":
        logger.info("Dispatching to build_extruded_cylinder")
        return build_extruded_cylinder(params)
    elif action == "extrude" and shape in ["block", "cube", "base_plate"]:
        logger.info("Dispatching to build_block")
        return build_block(params)
    
    elif action in ["create_hole", "pattern", "create_feature"] or "hole" in action.lower():
        logger.info("Dispatching to build_plate_with_holes")
        return build_plate_with_holes(params)
    
    elif shape == "cylinder":
        logger.info("Dispatching to build_extruded_cylinder (shape-based)")
        return build_extruded_cylinder(params)
    elif shape in ["block", "cube", "base_plate"]:
        logger.info("Dispatching to build_block (shape-based)")
        return build_block(params)
    
    else:
        # Default to plate with holes for unrecognized patterns
        logger.info(f"No specific builder for action='{action}', shape='{shape}'. Defaulting to build_plate_with_holes")
        return build_plate_with_holes(params)


def export_step(workplane: cq.Workplane, filepath: str) -> bool:
    """
    Export CadQuery workplane to STEP file format.
    
    Args:
        workplane (cq.Workplane): The geometry to export
        filepath (str): Output file path (should end with .step or .stp)
        
    Returns:
        bool: True if export successful, False otherwise
    """
    try:
        logger.info(f"Exporting STEP file to: {filepath}")
        cq.exporters.export(workplane, filepath)
        logger.info("STEP export completed successfully")
        return True
    except Exception as e:
        logger.error(f"Failed to export STEP file: {str(e)}")
        return False


def export_stl(workplane: cq.Workplane, filepath: str) -> bool:
    """
    Export CadQuery workplane to STL file format.
    
    Args:
        workplane (cq.Workplane): The geometry to export
        filepath (str): Output file path (should end with .stl)
        
    Returns:
        bool: True if export successful, False otherwise
    """
    try:
        logger.info(f"Exporting STL file to: {filepath}")
        cq.exporters.export(workplane, filepath)
        logger.info("STL export completed successfully")
        return True
    except Exception as e:
        logger.error(f"Failed to export STL file: {str(e)}")
        return False


# Example usage and testing
if __name__ == "__main__":
    # Test the builders with sample parameters

    # Test plate with holes
    plate_params = {
        "height_mm": 12.0,
        "diameter_mm": 6.0,
        "count": 6,
        "pattern": {"type": "circular", "count": 6, "angle_deg": 60.0}
    }

    plate = build_plate_with_holes(plate_params)
    print("OK: Plate with holes created")

    # Test extruded cylinder
    cylinder_params = {
        "diameter_mm": 25.0,
        "height_mm": 40.0
    }

    cylinder = build_extruded_cylinder(cylinder_params)
    print("OK: Extruded cylinder created")

    # Test dispatch
    dispatched_plate = dispatch_build("create_feature", plate_params)
    dispatched_cylinder = dispatch_build("extrude", {"shape": "cylinder", **cylinder_params})

    print("OK: Dispatch system working")
    print("Model builder module ready for integration")
