"""
Geometry package for Text-to-CAD Plugin

This package contains modules for converting parsed JSON parameters 
into CadQuery geometry operations and exporting CAD files.

Modules:
    model_builder: Core geometry building functions
"""

from .model_builder import (
    build_plate_with_holes,
    build_extruded_cylinder,
    dispatch_build
)

from .exporter import (
    export_solid,
    export_step,
    export_stl,
    list_output_files,
    cleanup_old_files
)

__all__ = [
    "build_plate_with_holes",
    "build_extruded_cylinder", 
    "dispatch_build",
    "export_solid",
    "export_step",
    "export_stl",
    "list_output_files",
    "cleanup_old_files"
]
