using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using TextToCad.SolidWorksAddin.Interfaces;
using TextToCad.SolidWorksAddin.Utils;

namespace TextToCad.SolidWorksAddin.Builders
{
    /// <summary>
    /// Builder for creating circular patterns of cut holes on the top face of a part.
    /// Uses the topmost planar face and creates evenly-spaced holes in a circular pattern.
    /// </summary>
    /// <remarks>
    /// WORKFLOW:
    /// 1. Ensure model has at least one solid body (creates base plate if needed)
    /// 2. Find topmost planar face
    /// 3. Create sketch on that face
    /// 4. Draw circles at polar positions (evenly distributed)
    /// 5. Cut-Extrude through all (or blind depth if provided)
    /// 6. Rebuild model
    /// 
    /// DEPENDENCIES:
    /// - Selection.GetTopMostPlanarFace() - Find top face
    /// - BasePlateBuilder - Create base if needed
    /// - Units - Convert mm to meters
    /// - UndoScope - Safe rollback on failure
    /// - ILogger - Operation logging
    /// 
    /// COORDINATE SYSTEM:
    /// - Pattern centered at model origin (0, 0)
    /// - Angles measured counter-clockwise from +X axis
    /// - First hole at angle 0 deg (positive X direction)
    /// </remarks>
    public class CircularHolesBuilder
    {
        #region Private Fields

        private readonly ISldWorks _sw;
        private readonly ILogger _log;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize the circular holes builder with SolidWorks application and logger.
        /// </summary>
        /// <param name="sw">SolidWorks application instance</param>
        /// <param name="log">Logger for operation tracking</param>
        public CircularHolesBuilder(ISldWorks sw, ILogger log)
        {
            _sw = sw ?? throw new ArgumentNullException(nameof(sw));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Create a circular pattern of cut holes on the topmost planar face.
        /// If no solid body exists, creates a base plate first.
        /// </summary>
        /// <param name="model">SolidWorks model document (must be a Part)</param>
        /// <param name="count">Number of holes in the pattern (must be >= 1)</param>
        /// <param name="diameterMm">Diameter of each hole in millimeters (must be > 0)</param>
        /// <param name="angleDeg">Optional angle span for pattern in degrees (default: 360 = full circle)</param>
        /// <param name="patternRadiusMm">Optional radius of pattern circle in mm (default: plateSizeMm * 0.3)</param>
        /// <param name="plateSizeMm">Size of base plate if created (default: 80mm, used for pattern radius calculation)</param>
        /// <param name="depthMm">Optional cut depth in mm. If null, uses Through All.</param>
        /// <param name="draftAngleDeg">Optional draft angle in degrees (adds taper to the cut)</param>
        /// <param name="draftOutward">Optional draft direction: true = outward, false = inward</param>
        /// <param name="flipDirection">Optional: true to flip the cut direction</param>
        /// <returns>True if holes created successfully; false on error</returns>
        /// <remarks>
        /// PATTERN LAYOUT:
        /// - Holes evenly distributed around a circle of radius patternRadiusMm
        /// - First hole at angle 0 deg (positive X axis)
        /// - Subsequent holes at angleStep intervals counter-clockwise
        /// - angleStep = angleDeg / count
        /// </remarks>
        public bool CreatePatternOnTopFace(
            IModelDoc2 model,
            int count,
            double diameterMm,
            double? angleDeg = null,
            double? patternRadiusMm = null,
            double? plateSizeMm = 80.0,
            double? depthMm = null,
            double? draftAngleDeg = null,
            bool? draftOutward = null,
            bool? flipDirection = null)
        {
            if (model == null)
            {
                _log.Error("CreatePatternOnTopFace: model is null");
                return false;
            }

            // Validate parameters
            if (count < 1)
            {
                _log.Error($"Invalid hole count: {count} (must be >= 1)");
                return false;
            }

            if (diameterMm <= 0)
            {
                _log.Error($"Invalid hole diameter: {diameterMm} mm (must be > 0)");
                return false;
            }

            // Set defaults for optional parameters
            double actualAngleDeg = angleDeg ?? 360.0;
            double actualPlateSizeMm = plateSizeMm ?? 80.0;
            double actualPatternRadiusMm = patternRadiusMm ?? (actualPlateSizeMm * 0.3);

            double? sanitizedDraftAngleDeg = null;
            if (draftAngleDeg.HasValue)
            {
                if (draftAngleDeg.Value <= 0)
                {
                    _log.Warn($"Draft angle must be > 0 deg; ignoring value {draftAngleDeg.Value}");
                }
                else if (draftAngleDeg.Value >= 89)
                {
                    _log.Warn($"Draft angle too large; capping to 89 deg (requested {draftAngleDeg.Value})");
                    sanitizedDraftAngleDeg = 89.0;
                }
                else
                {
                    sanitizedDraftAngleDeg = draftAngleDeg.Value;
                }
            }

            bool useDraft = sanitizedDraftAngleDeg.HasValue;
            bool draftOut = draftOutward ?? false;
            bool flip = flipDirection ?? false;

            if (actualAngleDeg <= 0)
            {
                _log.Warn($"Invalid angle span: {actualAngleDeg} deg. Defaulting to 360 deg.");
                actualAngleDeg = 360.0;
            }

            if (actualPatternRadiusMm <= 0)
            {
                _log.Warn($"Invalid pattern radius: {actualPatternRadiusMm} mm. Defaulting to {actualPlateSizeMm * 0.3} mm.");
                actualPatternRadiusMm = actualPlateSizeMm * 0.3;
            }

            _log.Info("Creating circular hole pattern:");
            _log.Info($"  Count: {count} holes");
            _log.Info($"  Diameter: {diameterMm} mm");
            _log.Info($"  Angle span: {actualAngleDeg} deg");
            _log.Info($"  Pattern radius: {actualPatternRadiusMm} mm");
            if (useDraft)
                _log.Info($"  Draft: {sanitizedDraftAngleDeg} deg, outward={draftOut}");
            if (flip)
                _log.Info("  Flip direction: true");

            // Validate pattern radius isn't too large
            if (actualPatternRadiusMm > actualPlateSizeMm / 2.0)
            {
                _log.Warn($"Pattern radius ({actualPatternRadiusMm}mm) is larger than half plate size ({actualPlateSizeMm / 2.0}mm)");
                _log.Warn("Holes may extend beyond the part boundary");
            }

            // Step 1: Ensure there's a solid body to cut into
            if (!EnsureBodyExists(model, actualPlateSizeMm))
            {
                _log.Error("Failed to ensure solid body exists");
                return false;
            }

            // Use UndoScope for safe rollback on failure
            using (var scope = new UndoScope(model, "Create Circular Hole Pattern", _log))
            {
                try
                {
                    // Step 2: Find topmost planar face
                    _log.Info("Finding topmost planar face...");
                    IFace2 topFace = Selection.GetTopMostPlanarFace(model, _log);

                    if (topFace == null)
                    {
                        _log.Error("Failed to find topmost planar face");
                        _log.Error("Ensure the model has at least one flat top surface");
                        return false;
                    }

                    _log.Info("Top face found");

                    // Step 3: Select the face and start sketch
                    if (!Selection.SelectFace(model, topFace, false, _log))
                    {
                        _log.Error("Failed to select top face");
                        return false;
                    }

                    _log.Info("Starting sketch on top face...");
                    model.SketchManager.InsertSketch(true);

                    // Verify sketch is active
                    if (model.SketchManager.ActiveSketch == null)
                    {
                        _log.Error("Failed to activate sketch on top face");
                        return false;
                    }

                    // Clear selections to avoid conflicts
                    model.ClearSelection2(true);
                    _log.Info("Sketch active on top face");

                    // Step 4: Calculate hole positions and draw circles
                    _log.Info($"Drawing {count} circles in pattern...");

                    double angleStepDeg = actualAngleDeg / count;
                    double patternRadiusM = Units.MmToM(actualPatternRadiusMm);
                    double holeRadiusM = Units.MmToM(diameterMm / 2.0);

                    for (int i = 0; i < count; i++)
                    {
                        // Calculate polar position
                        double angleDegrees = i * angleStepDeg;
                        double angleRadians = angleDegrees * Math.PI / 180.0;

                        // Convert to Cartesian (origin at model center)
                        double x = patternRadiusM * Math.Cos(angleRadians);
                        double y = patternRadiusM * Math.Sin(angleRadians);

                        _log.Info($"  Hole {i + 1}: angle={angleDegrees:F1} deg, position=({x * 1000:F2}, {y * 1000:F2}) mm");

                        // Draw circle at this position
                        object circleObj = model.SketchManager.CreateCircleByRadius(
                            x, y, 0,        // Center position (Z=0 on sketch plane)
                            holeRadiusM     // Radius in meters
                        );

                        if (circleObj == null)
                        {
                            _log.Error($"Failed to create circle at hole position {i + 1}");
                            _log.Error("Possible causes:");
                            _log.Error("  - Hole extends beyond part boundary");
                            _log.Error("  - Invalid sketch state");
                            return false;
                        }
                    }

                    _log.Info($"{count} circles created successfully");

                    // Step 5: Exit sketch
                    _log.Info("Exiting sketch...");
                    model.SketchManager.InsertSketch(true);

                    // Step 6: Create cut-extrude
                    bool useThroughAll = !depthMm.HasValue || depthMm.Value <= 0.0;
                    double depthM = useThroughAll ? 0.0 : Units.MmToM(depthMm.Value);
                    int endCondition = useThroughAll
                        ? (int)swEndConditions_e.swEndCondThroughAll
                        : (int)swEndConditions_e.swEndCondBlind;
                    double draftAngleRad = useDraft ? (sanitizedDraftAngleDeg.Value * Math.PI / 180.0) : 0.0;

                    _log.Info(useThroughAll
                        ? "Creating cut-extrude (Through All)..."
                        : $"Creating cut-extrude (depth={depthMm} mm)...");

                    IFeature cutFeature = model.FeatureManager.FeatureCut4(
                        true,              // 1. SD: Single direction
                        flip,              // 2. Flip: Flip cut direction
                        false,             // 3. Dir: Direction (not used for through all/blind)
                        endCondition,      // 4. T1: End condition type
                        0,                 // 5. T2: Not used (single direction)
                        depthM,            // 6. D1: Depth in meters
                        0.0,               // 7. D2: Not used
                        draftOut,          // 8. DDir: Draft direction
                        false,             // 9. DDir2: No draft
                        false,             // 10. DDirBoth: No draft
                        useDraft,          // 11. DFlag: Draft flag
                        draftAngleRad,     // 12. DDirAngle: Draft angle (radians)
                        0.0,               // 13. DDirAngle2: No draft angle
                        false,             // 14. OffsetReverse1: No offset reverse
                        false,             // 15. OffsetReverse2: No offset reverse
                        false,             // 16. TranslateSurface1: No translate
                        false,             // 17. TranslateSurface2: No translate
                        false,             // 18. Merge: Do not merge (cut operation)
                        false,             // 19. UseFeatScope: Not used
                        false,             // 20. UseAutoSelect: Not used
                        false,             // 21. T2AutoSelect: Not used
                        false,             // 22. AssemblyFeatureScope: Not an assembly
                        false,             // 23. AutoSelectComponents: Not an assembly
                        0,                 // 24. PropagateFeatureToParts: Feature scope (int)
                        0.0,               // 25. FeatScope: Feature scope parameter (double)
                        false,             // 26. FeatScopeParm: Not used (bool)
                        false              // 27. IsSolid: Not creating solid (cutting) (bool)
                    ) as IFeature;

                    if (cutFeature == null)
                    {
                        _log.Error("FeatureCut4 returned null - cut operation failed");
                        _log.Error("Possible causes:");
                        _log.Error("  - Sketch geometry not properly closed");
                        _log.Error("  - Circles outside part boundaries");
                        _log.Error("  - Invalid cut parameters");
                        return false;
                    }

                    string featureName = cutFeature.Name;
                    _log.Info($"Cut feature created: '{featureName}'");

                    // Step 7: Rebuild model
                    _log.Info("Rebuilding model...");
                    model.ForceRebuild3(false);

                    _log.Info("Circular pattern of cut holes created successfully");
                    _log.Info($"  Feature: {featureName}");
                    _log.Info($"  Holes: {count} x {diameterMm}mm diameter");
                    _log.Info($"  Pattern: {actualPatternRadiusMm}mm radius, {actualAngleDeg} deg span");

                    // Mark operation as successful
                    scope.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    _log.Error($"Exception creating hole pattern: {ex.Message}");
                    _log.Error($"Stack trace: {ex.StackTrace}");
                    return false;
                }
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Ensure the model has at least one solid body.
        /// If no bodies exist, creates a base plate using BasePlateBuilder.
        /// </summary>
        /// <param name="model">SolidWorks model document</param>
        /// <param name="plateSizeMm">Size for base plate if created</param>
        /// <returns>True if body exists or was created; false on error</returns>
        private bool EnsureBodyExists(IModelDoc2 model, double plateSizeMm)
        {
            // Check if model is a part document
            if (model.GetType() != (int)swDocumentTypes_e.swDocPART)
            {
                _log.Error("Model is not a Part document");
                return false;
            }

            IPartDoc partDoc = model as IPartDoc;
            if (partDoc == null)
            {
                _log.Error("Failed to cast to IPartDoc");
                return false;
            }

            // Check for existing solid bodies
            object[] bodies = partDoc.GetBodies2((int)swBodyType_e.swSolidBody, true) as object[];

            if (bodies != null && bodies.Length > 0)
            {
                _log.Info($"Model has {bodies.Length} solid body(ies) - ready for holes");
                return true;
            }

            // No bodies - need to create base plate
            _log.Info("No solid bodies found - creating base plate first...");

            var basePlateBuilder = new BasePlateBuilder(_sw, _log);
            bool success = basePlateBuilder.EnsureBasePlate(
                model,
                sizeMm: plateSizeMm,
                thicknessMm: 6.0  // Default thickness
            );

            if (success)
            {
                _log.Info("Base plate created - ready for holes");
            }
            else
            {
                _log.Error("Failed to create base plate");
            }

            return success;
        }

        #endregion
    }
}
