using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using TextToCad.SolidWorksAddin.Interfaces;
using TextToCad.SolidWorksAddin.Utils;

namespace TextToCad.SolidWorksAddin.Builders
{
    /// <summary>
    /// Builder for creating rectangular base plate features in SolidWorks parts.
    /// Ensures a base plate exists before adding other features.
    /// </summary>
    /// <remarks>
    /// This is the first core CAD operation that creates actual geometry.
    /// It demonstrates:
    /// - Checking for existing bodies
    /// - Selecting reference planes
    /// - Creating sketches with geometric constraints
    /// - Creating boss-extrude features
    /// - Using UndoScope for safe rollback
    /// - Proper unit conversion (mm to meters)
    /// </remarks>
    public class BasePlateBuilder
    {
        private readonly ISldWorks _sw;
        private readonly ILogger _log;

        /// <summary>
        /// Create a new base plate builder.
        /// </summary>
        /// <param name="sw">SolidWorks application instance</param>
        /// <param name="log">Logger for operation tracking</param>
        /// <exception cref="ArgumentNullException">If sw or log is null</exception>
        public BasePlateBuilder(ISldWorks sw, ILogger log)
        {
            _sw = sw ?? throw new ArgumentNullException(nameof(sw));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>
        /// Ensure a base plate exists in the model.
        /// If solid bodies already exist, skips creation.
        /// Otherwise, creates a centered rectangular base plate.
        /// </summary>
        /// <param name="model">SolidWorks model document (must be a Part)</param>
        /// <param name="sizeMm">Side length of the square base plate in millimeters (default: 80mm)</param>
        /// <param name="thicknessMm">Extrusion thickness in millimeters (default: 6mm)</param>
        /// <param name="widthMm">Optional custom width in millimeters (overrides sizeMm)</param>
        /// <param name="lengthMm">Optional custom length in millimeters (overrides sizeMm)</param>
        /// <param name="draftAngleDeg">Optional draft angle in degrees (applies taper to the extrude)</param>
        /// <param name="draftOutward">Optional draft direction: true = outward, false = inward</param>
        /// <param name="flipDirection">Optional: true to flip the extrusion direction</param>
        /// <returns>True if base plate exists or was created successfully; false on error</returns>
        /// <remarks>
        /// WORKFLOW:
        /// 1. Check if model already has solid bodies; skip if yes
        /// 2. Select Top Plane
        /// 3. Create sketch with center rectangle
        /// 4. Boss-extrude to create solid
        /// 5. Commit changes if successful, rollback on error
        /// </remarks>
        /// <example>
        /// // Create default 80x80x6mm base plate
        /// var builder = new BasePlateBuilder(swApp, logger);
        /// bool success = builder.EnsureBasePlate(modelDoc);
        /// 
        /// // Create custom 100x120x10mm base plate
        /// bool success = builder.EnsureBasePlate(modelDoc, 80.0, 10.0, 100.0, 120.0);
        /// </example>
        public bool EnsureBasePlate(
            IModelDoc2 model,
            double sizeMm = 80.0,
            double thicknessMm = 6.0,
            double? widthMm = null,
            double? lengthMm = null,
            double? draftAngleDeg = null,
            bool? draftOutward = null,
            bool? flipDirection = null)
        {
            if (model == null)
            {
                _log.Error("EnsureBasePlate: model is null");
                return false;
            }

            if (model.GetType() != (int)swDocumentTypes_e.swDocPART)
            {
                _log.Error("EnsureBasePlate only works with Part documents");
                return false;
            }

            if (HasSolidBodies(model))
            {
                _log.Info("Solid body already exists; skipping base plate creation");
                return true;
            }

            double actualWidthMm = widthMm ?? sizeMm;
            double actualLengthMm = lengthMm ?? sizeMm;

            if (actualWidthMm <= 0)
            {
                _log.Error($"Invalid width: {actualWidthMm} mm (must be > 0)");
                return false;
            }

            if (actualLengthMm <= 0)
            {
                _log.Error($"Invalid length: {actualLengthMm} mm (must be > 0)");
                return false;
            }

            if (thicknessMm <= 0)
            {
                _log.Error($"Invalid thickness: {thicknessMm} mm (must be > 0)");
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

            _log.Info($"Creating base plate (width={actualWidthMm}mm, length={actualLengthMm}mm, thickness={thicknessMm}mm)");
            if (useDraft)
                _log.Info($"  Draft: {sanitizedDraftAngleDeg} deg, outward={draftOut}");
            if (flip)
                _log.Info("  Flip direction: true");

            using (var scope = new UndoScope(model, "Create Base Plate", _log))
            {
                try
                {
                    _log.Info("Selecting Top Plane...");
                    if (!Selection.SelectPlaneByName(_sw, model, "Top Plane", logger: _log))
                    {
                        _log.Error("Failed to select Top Plane");
                        return false;
                    }

                    _log.Info("Starting sketch...");
                    model.SketchManager.InsertSketch(true);

                    if (model.SketchManager.ActiveSketch == null)
                    {
                        _log.Error("Failed to activate sketch");
                        return false;
                    }

                    model.ClearSelection2(true);
                    _log.Info("Sketch active and ready");

                    double widthM = Units.MmToM(actualWidthMm);
                    double lengthM = Units.MmToM(actualLengthMm);
                    double halfWidthM = widthM / 2.0;
                    double halfLengthM = lengthM / 2.0;

                    _log.Info($"Creating center rectangle ({actualLengthMm}x{actualWidthMm} mm)...");
                    _log.Info($"  Center: (0, 0, 0), Half-size: {halfLengthM:F6} x {halfWidthM:F6} meters");

                    object rectObj = model.SketchManager.CreateCenterRectangle(
                        0, 0, 0,
                        halfLengthM,
                        halfWidthM,
                        0
                    );

                    if (rectObj == null)
                    {
                        _log.Error("CreateCenterRectangle returned null");
                        _log.Error("Attempting alternative: CreateCornerRectangle...");

                        rectObj = model.SketchManager.CreateCornerRectangle(
                            -halfLengthM, -halfWidthM, 0,
                            halfLengthM, halfWidthM, 0
                        );

                        if (rectObj == null)
                        {
                            _log.Error("CreateCornerRectangle also returned null - sketch geometry creation failed");
                            _log.Error("Possible causes:");
                            _log.Error("  - Sketch plane not properly selected");
                            _log.Error("  - Invalid dimensions (too small or too large)");
                            _log.Error("  - SolidWorks in unexpected state");
                            return false;
                        }

                        _log.Info("Rectangle created using corner method (fallback)");
                    }
                    else
                    {
                        _log.Info("Center rectangle created successfully");
                    }

                    _log.Info("Exiting sketch...");
                    model.SketchManager.InsertSketch(true);

                    double thicknessM = Units.MmToM(thicknessMm);

                    _log.Info($"Creating boss-extrude (thickness={thicknessMm} mm)...");

                    double draftAngleRad = useDraft ? (sanitizedDraftAngleDeg.Value * Math.PI / 180.0) : 0.0;

                    IFeature feature = model.FeatureManager.FeatureExtrusion(
                        true,              // SD: Single direction
                        flip,              // Flip: Flip direction
                        false,             // Dir: Direction (not used for blind)
                        (int)swEndConditions_e.swEndCondBlind,  // T1: Blind extrusion
                        0,                 // T2: Not used (single direction)
                        thicknessM,        // D1: Depth in meters
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
                        _log.Error("  - Model rebuild errors");
                        return false;
                    }

                    string featureName = feature.Name;
                    _log.Info($"Base plate created successfully: '{featureName}'");
                    _log.Info($"  Dimensions: {actualLengthMm}x{actualWidthMm}x{thicknessMm} mm");

                    _log.Info("Rebuilding model...");
                    model.ForceRebuild3(false);

                    scope.Commit();

                    return true;
                }
                catch (Exception ex)
                {
                    _log.Error($"Exception creating base plate: {ex.Message}");
                    _log.Error($"Stack trace: {ex.StackTrace}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Check if the model has any solid bodies.
        /// </summary>
        /// <param name="model">SolidWorks model document</param>
        /// <returns>True if at least one solid body exists; false otherwise</returns>
        /// <remarks>
        /// This is used to determine if a base plate needs to be created.
        /// If bodies already exist, we skip base plate creation to avoid
        /// overwriting existing geometry.
        /// </remarks>
        public bool HasSolidBodies(IModelDoc2 model)
        {
            if (model == null)
            {
                _log.Warn("HasSolidBodies: model is null");
                return false;
            }

            try
            {
                // Ensure document is a Part
                if (model.GetType() != (int)swDocumentTypes_e.swDocPART)
                {
                    _log.Warn("HasSolidBodies: Document is not a Part");
                    return false;
                }

                // Cast to IPartDoc to access Part-specific methods
                IPartDoc partDoc = model as IPartDoc;
                if (partDoc == null)
                {
                    _log.Warn("Failed to cast model to IPartDoc");
                    return false;
                }

                // Get all solid bodies
                // GetBodies2 parameters: body type, include hidden
                object[] bodies = partDoc.GetBodies2(
                    (int)swBodyType_e.swSolidBody,  // Solid bodies only
                    true  // Include hidden bodies
                ) as object[];

                if (bodies == null || bodies.Length == 0)
                {
                    _log.Info("No solid bodies found in model");
                    return false;
                }

                _log.Info($"Found {bodies.Length} solid body(ies) in model");
                return true;
            }
            catch (Exception ex)
            {
                _log.Error($"Exception checking for solid bodies: {ex.Message}");
                return false;
            }
        }
    }
}
