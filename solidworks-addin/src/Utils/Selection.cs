using System;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using TextToCad.SolidWorksAddin.Interfaces;

namespace TextToCad.SolidWorksAddin.Utils
{
    /// <summary>
    /// Utilities for selecting planes, faces, and other geometry in SolidWorks.
    /// Provides high-level helpers for common selection operations.
    /// </summary>
    public static class Selection
    {
        /// <summary>
        /// Select a reference plane by name.
        /// Common plane names: "Front Plane", "Top Plane", "Right Plane"
        /// </summary>
        /// <param name="app">SolidWorks application instance</param>
        /// <param name="model">Active model document</param>
        /// <param name="planeName">Name of the plane to select (e.g., "Top Plane")</param>
        /// <param name="append">If true, adds to current selection; if false, clears selection first</param>
        /// <param name="logger">Optional logger for diagnostics</param>
        /// <returns>True if plane was selected successfully; false otherwise</returns>
        /// <remarks>
        /// PLANE NAMES BY DOCUMENT TYPE:
        /// - Part: "Front Plane", "Top Plane", "Right Plane"
        /// - Assembly: May need fully qualified names like "Part1/Front Plane"
        /// - Drawing: Planes not typically available
        /// 
        /// TROUBLESHOOTING:
        /// - Ensure model is a Part document (not Drawing or Assembly)
        /// - Check spelling and capitalization of plane name
        /// - Some configurations may rename default planes
        /// - Use FeatureManager tree to verify exact plane names
        /// </remarks>
        /// <example>
        /// // Select Top Plane to start a sketch
        /// if (Selection.SelectPlaneByName(swApp, model, "Top Plane", logger: logger))
        /// {
        ///     model.SketchManager.InsertSketch(true);
        ///     // Draw sketch entities...
        /// }
        /// </example>
        public static bool SelectPlaneByName(
            ISldWorks app,
            IModelDoc2 model,
            string planeName,
            bool append = false,
            ILogger logger = null)
        {
            if (app == null)
            {
                logger?.Error("SelectPlaneByName: app is null");
                return false;
            }

            if (model == null)
            {
                logger?.Error("SelectPlaneByName: model is null");
                return false;
            }

            if (string.IsNullOrWhiteSpace(planeName))
            {
                logger?.Error("SelectPlaneByName: planeName is null or empty");
                return false;
            }

            try
            {
                logger?.Info($"Attempting to select plane: {planeName}");

                // SelectByID2 parameters:
                // Name, Type, X, Y, Z, Append, Mark, Callout, SelectOption
                bool success = model.Extension.SelectByID2(
                    planeName,      // Name of the plane
                    "PLANE",        // Type (PLANE, FACE, EDGE, etc.)
                    0, 0, 0,       // X, Y, Z coordinates (0,0,0 for named selection)
                    append,         // Append to selection or replace
                    0,             // Mark (user selection mark for advanced use)
                    null,          // Callout (for drawing annotations)
                    (int)swSelectOption_e.swSelectOptionDefault  // Select options
                );

                if (success)
                {
                    logger?.Info($"✓ Plane selected: {planeName}");
                    return true;
                }
                else
                {
                    logger?.Warn($"✗ Failed to select plane: {planeName}");
                    logger?.Warn("Possible causes:");
                    logger?.Warn("  - Plane name is incorrect (check spelling/capitalization)");
                    logger?.Warn("  - Document type is not Part (planes not available in Drawings)");
                    logger?.Warn("  - Plane has been renamed or deleted");
                    logger?.Warn($"  - Try one of: 'Front Plane', 'Top Plane', 'Right Plane'");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger?.Error($"Exception selecting plane '{planeName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Find the topmost planar face in the model.
        /// "Topmost" is defined as the planar face with the highest Z-coordinate center.
        /// </summary>
        /// <param name="model">Model document (must be a Part)</param>
        /// <param name="logger">Optional logger for diagnostics</param>
        /// <returns>The topmost planar face, or null if none found</returns>
        /// <remarks>
        /// ALGORITHM:
        /// 1. Ensure document is a Part (not Assembly or Drawing)
        /// 2. Get all solid bodies in the part
        /// 3. Iterate through all faces of all bodies
        /// 4. Filter to planar faces only (using IsSurfaceType)
        /// 5. Calculate center point Z-coordinate for each planar face
        /// 6. Return the face with maximum Z value
        /// 
        /// LIMITATIONS:
        /// - Only works for Part documents
        /// - Assumes "top" means +Z direction (standard orientation)
        /// - If model is rotated, results may be unexpected
        /// - Does not account for face area (small face at top will still win)
        /// 
        /// USE CASES:
        /// - Selecting top face for hole patterns
        /// - Finding surface for sketch creation
        /// - Identifying machining surfaces
        /// </remarks>
        /// <example>
        /// var topFace = Selection.GetTopMostPlanarFace(model, logger);
        /// if (topFace != null)
        /// {
        ///     Selection.SelectFace(model, topFace);
        ///     model.SketchManager.InsertSketch(true);
        ///     // Create hole pattern on top face...
        /// }
        /// else
        /// {
        ///     logger.Warn("No planar faces found, using Top Plane instead");
        ///     Selection.SelectPlaneByName(swApp, model, "Top Plane");
        /// }
        /// </example>
        public static IFace2 GetTopMostPlanarFace(IModelDoc2 model, ILogger logger = null)
        {
            if (model == null)
            {
                logger?.Error("GetTopMostPlanarFace: model is null");
                return null;
            }

            try
            {
                // Ensure document is a Part
                if (model.GetType() != (int)swDocumentTypes_e.swDocPART)
                {
                    logger?.Warn($"GetTopMostPlanarFace: Document type is {model.GetType()}, not Part");
                    logger?.Warn("This method only works with Part documents");
                    return null;
                }

                // Cast to IPartDoc to access Part-specific methods
                IPartDoc partDoc = (IPartDoc)model;

                // Get all solid bodies in the part
                // GetBodies2 parameters: body type, include hidden bodies
                object[] bodies = (object[])partDoc.GetBodies2(
                    (int)swBodyType_e.swSolidBody,  // Solid bodies only
                    true  // Include hidden bodies
                );

                if (bodies == null || bodies.Length == 0)
                {
                    logger?.Warn("No solid bodies found in part");
                    return null;
                }

                logger?.Info($"Searching {bodies.Length} solid body(ies) for topmost planar face");

                IFace2 topFace = null;
                double maxY = double.MinValue;
                int planarFaceCount = 0;

                // Iterate through all bodies
                foreach (object bodyObj in bodies)
                {
                    IBody2 body = bodyObj as IBody2;
                    if (body == null) continue;

                    // Get all faces of this body
                    object[] faces = (object[])body.GetFaces();
                    if (faces == null) continue;

                    // Check each face
                    foreach (object faceObj in faces)
                    {
                        IFace2 face = faceObj as IFace2;
                        if (face == null) continue;

                        // Check if face is planar
                        ISurface surface = face.GetSurface() as ISurface;
                        if (surface == null) continue;

                        if (surface.IsPlane())
                        {
                            planarFaceCount++;

                            // Get face bounding box to find center point
                            double[] box = (double[])face.GetBox();
                            if (box != null && box.Length >= 6)
                            {
                                // Box format: [xMin, yMin, zMin, xMax, yMax, zMax]
                                // Y-coordinate: top/bottom plane (yMin at index 1, yMax at index 4)
                                double centerY = (box[1] + box[4]) / 2.0;  // Average of yMin and yMax

                                // Track face with maximum Y (highest = top)
                                if (centerY > maxY)
                                {
                                    maxY = centerY;
                                    topFace = face;
                                }
                            }
                        }
                    }
                }

                if (topFace != null)
                {
                    logger?.Info($"✓ Found topmost planar face (Y={Units.MToMm(maxY):F2} mm)");
                    logger?.Info($"  Total planar faces evaluated: {planarFaceCount}");
                }
                else
                {
                    logger?.Warn("No planar faces found in model");
                }

                return topFace;
            }
            catch (Exception ex)
            {
                logger?.Error($"Exception finding topmost face: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Select a specific face in the model.
        /// </summary>
        /// <param name="model">Model document containing the face</param>
        /// <param name="face">Face to select</param>
        /// <param name="append">If true, adds to current selection; if false, clears selection first</param>
        /// <param name="logger">Optional logger for diagnostics</param>
        /// <returns>True if face was selected successfully; false otherwise</returns>
        /// <remarks>
        /// This method uses IFace2.Select4() which is available in most SolidWorks versions.
        /// For older versions, you may need to use Select() or Select2() instead.
        /// </remarks>
        /// <example>
        /// var topFace = Selection.GetTopMostPlanarFace(model);
        /// if (Selection.SelectFace(model, topFace))
        /// {
        ///     model.SketchManager.InsertSketch(true);
        /// }
        /// </example>
        public static bool SelectFace(IModelDoc2 model, IFace2 face, bool append = false, ILogger logger = null)
        {
            if (model == null)
            {
                logger?.Error("SelectFace: model is null");
                return false;
            }

            if (face == null)
            {
                logger?.Error("SelectFace: face is null");
                return false;
            }

            try
            {
                // Clear selection if not appending
                if (!append)
                {
                    model.ClearSelection2(true);
                }

                // Select the face
                // Use IEntity interface for selection (IFace2.Select methods vary by version)
                IEntity entity = face as IEntity;
                bool success = entity?.Select4(append, null) ?? false;

                if (success)
                {
                    logger?.Info("✓ Face selected");
                }
                else
                {
                    logger?.Warn("✗ Failed to select face");
                }

                return success;
            }
            catch (Exception ex)
            {
                logger?.Error($"Exception selecting face: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clear all selections in the model.
        /// </summary>
        /// <param name="model">Model document</param>
        /// <param name="logger">Optional logger for diagnostics</param>
        /// <returns>True if selection was cleared successfully</returns>
        /// <example>
        /// Selection.ClearSelection(model, logger);
        /// </example>
        public static bool ClearSelection(IModelDoc2 model, ILogger logger = null)
        {
            if (model == null)
            {
                logger?.Error("ClearSelection: model is null");
                return false;
            }

            try
            {
                model.ClearSelection2(true);
                logger?.Info("Selection cleared");
                return true;
            }
            catch (Exception ex)
            {
                logger?.Error($"Exception clearing selection: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get the number of currently selected objects.
        /// </summary>
        /// <param name="model">Model document</param>
        /// <returns>Number of selected objects, or -1 if error</returns>
        public static int GetSelectionCount(IModelDoc2 model)
        {
            if (model == null)
                return -1;

            try
            {
                ISelectionMgr selMgr = model.SelectionManager as ISelectionMgr;
                return selMgr?.GetSelectedObjectCount2(-1) ?? -1;
            }
            catch
            {
                return -1;
            }
        }
    }
}
