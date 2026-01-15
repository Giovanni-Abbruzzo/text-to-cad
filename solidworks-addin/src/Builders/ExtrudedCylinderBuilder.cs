using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using TextToCad.SolidWorksAddin.Interfaces;
using TextToCad.SolidWorksAddin.Utils;

namespace TextToCad.SolidWorksAddin.Builders
{
    /// <summary>
    /// Creates extruded cylinders (circular boss-extrude) in SolidWorks parts.
    /// </summary>
    public class ExtrudedCylinderBuilder
    {
        private readonly ISldWorks _sw;
        private readonly ILogger _log;

        /// <summary>
        /// Initializes a new instance of the ExtrudedCylinderBuilder.
        /// </summary>
        /// <param name="sw">SolidWorks application instance (required for API access)</param>
        /// <param name="log">Logger instance for operation tracking and debugging</param>
        /// <exception cref="ArgumentNullException">Thrown if sw or log is null</exception>
        public ExtrudedCylinderBuilder(ISldWorks sw, ILogger log)
        {
            _sw = sw ?? throw new ArgumentNullException(nameof(sw));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>
        /// Creates a cylindrical boss-extrude feature on the Top Plane.
        /// </summary>
        /// <param name="model">Active SolidWorks part document (must be Part type)</param>
        /// <param name="diameterMm">Cylinder diameter in millimeters (must be > 0). Default: 20mm</param>
        /// <param name="heightMm">Extrusion height in millimeters (must be > 0). Default: 10mm</param>
        /// <param name="draftAngleDeg">Optional draft angle in degrees (applies taper to the extrude)</param>
        /// <param name="draftOutward">Optional draft direction: true = outward, false = inward</param>
        /// <param name="flipDirection">Optional: true to flip the extrusion direction</param>
        /// <returns>True if cylinder created successfully; false otherwise</returns>
        public bool CreateCylinderOnTopPlane(
            IModelDoc2 model,
            double diameterMm = 20.0,
            double heightMm = 10.0,
            double? draftAngleDeg = null,
            bool? draftOutward = null,
            bool? flipDirection = null,
            double? centerXmm = null,
            double? centerYmm = null,
            double? centerZmm = null,
            string axis = null,
            bool? useTopFace = null,
            bool? extrudeMidplane = null)
        {
            if (model == null)
            {
                _log.Error("CreateCylinderOnTopPlane: model is null");
                return false;
            }

            // Validate parameters
            if (diameterMm <= 0)
            {
                _log.Error($"Invalid diameter: {diameterMm} mm (must be > 0)");
                return false;
            }

            if (heightMm <= 0)
            {
                _log.Error($"Invalid height: {heightMm} mm (must be > 0)");
                return false;
            }

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
            bool midPlane = extrudeMidplane ?? false;

            string axisNormalized = axis?.Trim().ToLowerInvariant();
            if (axisNormalized != "x" && axisNormalized != "z")
            {
                axisNormalized = "y";
            }

            // Check if model is a part document
            if (model.GetType() != (int)swDocumentTypes_e.swDocPART)
            {
                _log.Error("Active document is not a Part document");
                return false;
            }

            _log.Info("Creating cylinder:");
            _log.Info($"  Diameter: {diameterMm} mm");
            _log.Info($"  Height: {heightMm} mm");
            if (useDraft)
                _log.Info($"  Draft: {sanitizedDraftAngleDeg} deg, outward={draftOut}");
            if (flip)
                _log.Info("  Flip direction: true");
            if (midPlane)
                _log.Info("  Midplane: true");
            _log.Info($"  Axis: {axisNormalized}");
            if (centerXmm.HasValue || centerYmm.HasValue || centerZmm.HasValue)
                _log.Info($"  Center: ({centerXmm ?? 0} mm, {centerYmm ?? 0} mm, {centerZmm ?? 0} mm)");

            // Use UndoScope for safe rollback on failure
            using (var scope = new UndoScope(model, "Create Extruded Cylinder", _log))
            {
                try
                {
                    // Step 1: Select sketch plane or face
                    string planeName = axisNormalized == "z" ? "Front Plane" :
                                       axisNormalized == "x" ? "Right Plane" :
                                       "Top Plane";

                    bool useFace = useTopFace ?? false;
                    if (useFace && axisNormalized != "y")
                    {
                        _log.Warn("use_top_face is only supported for vertical cylinders (axis=y); ignoring");
                        useFace = false;
                    }

                    if (useFace)
                    {
                        double targetXmm = centerXmm ?? 0.0;
                        double targetZmm = centerZmm ?? centerYmm ?? 0.0;
                        _log.Info("Selecting top face at target location...");
                        IFace2 topFace = Selection.GetTopMostPlanarFaceAt(model, targetXmm, targetZmm, _log);
                        if (topFace == null)
                        {
                            _log.Warn("No planar face found at location - falling back to global top face");
                            topFace = Selection.GetTopMostPlanarFace(model, _log);
                        }

                        if (topFace == null)
                        {
                            _log.Error("Failed to find a planar face for cylinder");
                            return false;
                        }

                        if (!Selection.SelectFace(model, topFace, false, _log))
                        {
                            _log.Error("Failed to select top face for cylinder");
                            return false;
                        }
                        _log.Info("Top face selected");
                    }
                    else
                    {
                        _log.Info($"Selecting {planeName}...");
                        if (!Selection.SelectPlaneByName(_sw, model, planeName, logger: _log))
                        {
                            _log.Error($"Failed to select {planeName}");
                            return false;
                        }
                        _log.Info($"{planeName} selected");
                    }

                    // Step 2: Start sketch on selected plane
                    _log.Info("Starting sketch...");
                    model.SketchManager.InsertSketch(true);

                    // Verify sketch is active
                    if (model.SketchManager.ActiveSketch == null)
                    {
                        _log.Error("Failed to activate sketch");
                        return false;
                    }

                    // Clear any selections to avoid conflicts
                    model.ClearSelection2(true);
                    _log.Info("Sketch active and ready");

                    // Step 3: Create circle at origin
                    // Convert dimensions to meters for SolidWorks API
                    double radiusMm = diameterMm / 2.0;
                    double radiusM = Units.MmToM(radiusMm);

                    double centerX = Units.MmToM(centerXmm ?? 0.0);
                    double centerY = Units.MmToM(centerYmm ?? 0.0);
                    double centerZ = Units.MmToM(centerZmm ?? 0.0);

                    if (axisNormalized == "y")
                    {
                        double planarZmm = centerZmm ?? centerYmm ?? 0.0;
                        double planarZ = Units.MmToM(planarZmm);
                        _log.Info($"Creating circle at center ({centerXmm ?? 0} mm, {planarZmm} mm) (radius={radiusMm} mm)...");
                        centerY = planarZ;
                        centerZ = 0.0;
                    }
                    else if (axisNormalized == "z")
                    {
                        _log.Info($"Creating circle at center ({centerXmm ?? 0} mm, {centerYmm ?? 0} mm) (radius={radiusMm} mm)...");
                        centerZ = 0.0;
                    }
                    else
                    {
                        double planarYmm = centerYmm ?? 0.0;
                        double planarZmm = centerZmm ?? 0.0;
                        _log.Info($"Creating circle at center ({planarYmm} mm, {planarZmm} mm) (radius={radiusMm} mm)...");
                        centerX = 0.0;
                        centerY = Units.MmToM(planarYmm);
                        centerZ = Units.MmToM(planarZmm);
                    }

                    object circleObj = model.SketchManager.CreateCircleByRadius(
                        centerX,
                        centerY,
                        centerZ,
                        radiusM
                    );

                    if (circleObj == null)
                    {
                        _log.Error("CreateCircleByRadius returned null - circle creation failed");
                        _log.Error("Possible causes:");
                        _log.Error("  - Sketch plane not properly selected");
                        _log.Error("  - Invalid radius (too small or too large)");
                        _log.Error("  - SolidWorks in unexpected state");
                        return false;
                    }

                    _log.Info($"Circle created (radius={radiusMm} mm, diameter={diameterMm} mm)");

                    // Step 4: Exit sketch
                    _log.Info("Exiting sketch...");
                    model.SketchManager.InsertSketch(true);

                    // Step 5: Create boss-extrude feature
                    double heightM = Units.MmToM(heightMm);
                    double draftAngleRad = useDraft ? (sanitizedDraftAngleDeg.Value * Math.PI / 180.0) : 0.0;

                    int endCondition = midPlane
                        ? (int)swEndConditions_e.swEndCondMidPlane
                        : (int)swEndConditions_e.swEndCondBlind;

                    _log.Info($"Creating boss-extrude (height={heightMm} mm)...");

                    IFeature feature = model.FeatureManager.FeatureExtrusion(
                        true,              // SD: Single direction
                        flip,              // Flip: Flip direction
                        false,             // Dir: Direction (not used for blind)
                        endCondition,      // T1: End condition
                        0,                 // T2: Not used (single direction)
                        heightM,           // D1: Depth in meters (height of cylinder)
                        0.0,               // D2: Not used
                        draftOut,          // DDir: Draft direction
                        false,             // DDir2: No draft
                        false,             // DDirBoth: No draft
                        useDraft,          // DFlag: Draft flag
                        draftAngleRad,     // DDirAngle: Draft angle (radians)
                        0.0,               // DDirAngle2: No draft angle
                        false,             // OffsetReverse1: No offset reverse
                        false,             // OffsetReverse2: No offset reverse
                        false,             // TranslateSurface1: No translate
                        false,             // TranslateSurface2: No translate
                        false,             // Merge: Merge with existing bodies if any
                        false,             // UseFeatScope: Not used
                        false              // UseAutoSelect: Not used
                    ) as IFeature;

                    if (feature == null)
                    {
                        _log.Error("FeatureExtrusion returned null - extrusion failed");
                        _log.Error("Possible causes:");
                        _log.Error("  - Sketch was not properly closed");
                        _log.Error("  - Invalid extrusion parameters");
                        _log.Error("  - No sketch profile selected");
                        _log.Error("  - Sketch selection was cleared before extrusion");
                        return false;
                    }

                    string featureName = feature.Name;
                    _log.Info($"Cylinder created: '{featureName}'");
                    _log.Info($"  Dimensions: {diameterMm}mm diameter x {heightMm}mm height");

                    // Step 6: Rebuild model
                    _log.Info("Rebuilding model...");
                    model.ForceRebuild3(false);

                    // Mark operation as successful
                    _log.Info("Cylinder created successfully");
                    scope.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    _log.Error($"Exception creating cylinder: {ex.Message}");
                    _log.Error($"Stack trace: {ex.StackTrace}");
                    return false;
                }
            }
        }
    }
}
