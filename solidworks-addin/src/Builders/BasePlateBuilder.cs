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
    /// This is the first "real" CAD operation that creates actual geometry.
    /// It demonstrates:
    /// - Checking for existing bodies
    /// - Selecting reference planes
    /// - Creating sketches with geometric constraints
    /// - Creating boss-extrude features
    /// - Using UndoScope for safe rollback
    /// - Proper unit conversion (mm to meters)
    /// 
    /// USAGE:
    /// var builder = new BasePlateBuilder(swApp, logger);
    /// bool success = builder.EnsureBasePlate(modelDoc, 80.0, 6.0);
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
        /// <returns>True if base plate exists or was created successfully; false on error</returns>
        /// <remarks>
        /// WORKFLOW:
        /// 1. Check if model already has solid bodies → skip if yes
        /// 2. Select Top Plane
        /// 3. Create sketch with center rectangle
        /// 4. Boss-extrude to create solid
        /// 5. Commit changes if successful, rollback on error
        /// 
        /// PARAMETERS:
        /// - sizeMm: Creates a square base plate (e.g., 80mm = 80×80mm plate)
        /// - thicknessMm: How tall the base plate is (e.g., 6mm thick)
        /// 
        /// DEFAULT VALUES:
        /// - Size: 80mm × 80mm (suitable for most small parts)
        /// - Thickness: 6mm (common sheet metal thickness)
        /// </remarks>
        /// <example>
        /// // Create default 80×80×6mm base plate
        /// var builder = new BasePlateBuilder(swApp, logger);
        /// bool success = builder.EnsureBasePlate(modelDoc);
        /// 
        /// // Create custom 100×100×10mm base plate
        /// bool success = builder.EnsureBasePlate(modelDoc, 100.0, 10.0);
        /// </example>
        public bool EnsureBasePlate(IModelDoc2 model, double sizeMm = 80.0, double thicknessMm = 6.0)
        {
            if (model == null)
            {
                _log.Error("EnsureBasePlate: model is null");
                return false;
            }

            // Validate parameters
            if (sizeMm <= 0)
            {
                _log.Error($"Invalid size: {sizeMm} mm (must be > 0)");
                return false;
            }

            if (thicknessMm <= 0)
            {
                _log.Error($"Invalid thickness: {thicknessMm} mm (must be > 0)");
                return false;
            }

            // Check document type
            if (model.GetType() != (int)swDocumentTypes_e.swDocPART)
            {
                _log.Error("EnsureBasePlate only works with Part documents");
                return false;
            }

            _log.Info($"Ensuring base plate exists (size={sizeMm}mm, thickness={thicknessMm}mm)");

            // Check if model already has solid bodies
            if (HasSolidBodies(model))
            {
                _log.Info("Model already has bodies; skipping base plate creation");
                return true;  // Not an error - plate already exists
            }

            // Use UndoScope for safe rollback on failure
            using (var scope = new UndoScope(model, "Create Base Plate", _log))
            {
                try
                {
                    // Step 1: Select Top Plane
                    _log.Info("Selecting Top Plane...");
                    if (!Selection.SelectPlaneByName(_sw, model, "Top Plane", logger: _log))
                    {
                        _log.Error("Failed to select Top Plane");
                        return false;
                    }

                    // Step 2: Start sketch on selected plane
                    _log.Info("Starting sketch...");
                    model.SketchManager.InsertSketch(true);

                    // Verify sketch is active and clear selections
                    if (model.SketchManager.ActiveSketch == null)
                    {
                        _log.Error("Failed to activate sketch");
                        return false;
                    }
                    
                    // Clear any selections to avoid conflicts
                    model.ClearSelection2(true);
                    
                    _log.Info("✓ Sketch active and ready");

                    // Step 3: Create center rectangle
                    // Convert size from mm to meters for API
                    double sizeM = Units.MmToM(sizeMm);
                    double halfSizeM = sizeM / 2.0;

                    _log.Info($"Creating center rectangle ({sizeMm}×{sizeMm} mm)...");
                    _log.Info($"  Center: (0, 0, 0), Half-size: {halfSizeM:F6} meters");
                    
                    // CreateCenterRectangle parameters:
                    // X, Y, Z of center point (origin)
                    // X, Y, Z of corner point (half-width from center)
                    object rectObj = model.SketchManager.CreateCenterRectangle(
                        0, 0, 0,           // Center at origin
                        halfSizeM,         // Half-width in X
                        halfSizeM,         // Half-width in Y (or could be different for non-square)
                        0                  // Z = 0 (on plane)
                    );

                    if (rectObj == null)
                    {
                        _log.Error("CreateCenterRectangle returned null");
                        _log.Error("Attempting alternative: CreateCornerRectangle...");
                        
                        // Try corner rectangle as fallback
                        rectObj = model.SketchManager.CreateCornerRectangle(
                            -halfSizeM, -halfSizeM, 0,  // Bottom-left corner
                            halfSizeM, halfSizeM, 0     // Top-right corner
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
                        
                        _log.Info("✓ Rectangle created using corner method (fallback)");
                    }
                    else
                    {
                        _log.Info("✓ Center rectangle created successfully");
                    }

                    // Step 4: Exit sketch
                    _log.Info("Exiting sketch...");
                    model.SketchManager.InsertSketch(true);

                    // Step 5: Create boss-extrude feature
                    double thicknessM = Units.MmToM(thicknessMm);
                    
                    _log.Info($"Creating boss-extrude (thickness={thicknessMm} mm)...");
                    
                    // Use FeatureExtrusion method (20 parameters required)
                    IFeature feature = model.FeatureManager.FeatureExtrusion(
                        true,              // SD: Single direction
                        false,             // Flip: Don't flip direction (extrude up)
                        false,             // Dir: Direction (not used for blind)
                        (int)swEndConditions_e.swEndCondBlind,  // T1: Blind extrusion
                        0,                 // T2: Not used (single direction)
                        thicknessM,        // D1: Depth in meters
                        0.0,               // D2: Not used
                        false,             // DDir: No draft
                        false,             // DDir2: No draft
                        false,             // DDirBoth: No draft
                        false,             // DFlag: Draft flag
                        0.0,               // DDirAngle: No draft angle
                        0.0,               // DDirAngle2: No draft angle
                        false,             // OffsetReverse1: No offset reverse
                        false,             // OffsetReverse2: No offset reverse
                        false,             // TranslateSurface1: No translate
                        false,             // TranslateSurface2: No translate
                        false,             // Merge: Don't merge (first feature)
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

                    // Success!
                    string featureName = feature.Name;
                    _log.Info($"✓ Base plate created successfully: '{featureName}'");
                    _log.Info($"  Dimensions: {sizeMm}×{sizeMm}×{thicknessMm} mm");
                    
                    // Rebuild to ensure feature is fully created
                    _log.Info("Rebuilding model...");
                    model.ForceRebuild3(false);  // false = don't show errors in UI

                    // Commit the UndoScope - operation succeeded
                    scope.Commit();
                    
                    return true;
                }
                catch (Exception ex)
                {
                    _log.Error($"Exception creating base plate: {ex.Message}");
                    _log.Error($"Stack trace: {ex.StackTrace}");
                    return false;
                    // UndoScope will automatically rollback changes
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
        /// 
        /// BODY TYPES:
        /// - Solid bodies: 3D solid geometry (what we check for)
        /// - Surface bodies: Sheet/surface geometry (not checked)
        /// - Wireframe: Curves/edges only (not checked)
        /// </remarks>
        /// <example>
        /// if (!builder.HasSolidBodies(model))
        /// {
        ///     // Safe to create base plate
        ///     builder.EnsureBasePlate(model);
        /// }
        /// </example>
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
