using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using TextToCad.SolidWorksAddin.Interfaces;
using TextToCad.SolidWorksAddin.Utils;

namespace TextToCad.SolidWorksAddin.Builders
{
    /// <summary>
    /// Creates extruded cylinders (circular boss-extrude) in SolidWorks parts.
    /// 
    /// <para>
    /// This builder provides a simple way to create cylindrical features by:
    /// 1. Sketching a circle on the Top Plane
    /// 2. Boss-extruding to the specified height
    /// </para>
    /// 
    /// <para><strong>Sprint SW-5: Operation #3</strong></para>
    /// 
    /// <para><strong>Default Values:</strong></para>
    /// <list type="bullet">
    ///   <item>Diameter: 20 mm (medium-sized cylinder)</item>
    ///   <item>Height: 10 mm (short cylinder/disc)</item>
    /// </list>
    /// 
    /// <para><strong>Smart Behavior:</strong></para>
    /// <list type="bullet">
    ///   <item>Creates standalone cylinder from Top Plane</item>
    ///   <item>No requirement for existing bodies (unlike hole patterns)</item>
    ///   <item>Validates all parameters (diameter > 0, height > 0)</item>
    ///   <item>Uses UndoScope for automatic rollback on failure</item>
    ///   <item>Comprehensive logging at every step</item>
    /// </list>
    /// 
    /// <para><strong>Common Uses:</strong></para>
    /// <list type="bullet">
    ///   <item>Pins and shafts</item>
    ///   <item>Mounting posts</item>
    ///   <item>Standoffs and spacers</item>
    ///   <item>Circular base features</item>
    ///   <item>Simple cylindrical parts</item>
    /// </list>
    /// 
    /// <para><strong>Example Usage:</strong></para>
    /// <code>
    /// var builder = new ExtrudedCylinderBuilder(swApp, logger);
    /// bool success = builder.CreateCylinderOnTopPlane(
    ///     model, 
    ///     diameterMm: 25.0,  // 25mm diameter
    ///     heightMm: 15.0     // 15mm tall
    /// );
    /// </code>
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
        /// 
        /// <para>
        /// This method creates a simple cylindrical feature by:
        /// 1. Selecting the "Top Plane"
        /// 2. Creating a sketch with a circle at the origin
        /// 3. Boss-extruding upward to the specified height
        /// </para>
        /// 
        /// <para><strong>Default Parameters:</strong></para>
        /// <list type="bullet">
        ///   <item>diameterMm: 20.0 mm (medium cylinder)</item>
        ///   <item>heightMm: 10.0 mm (short cylinder/disc)</item>
        /// </list>
        /// 
        /// <para><strong>What Gets Created:</strong></para>
        /// <code>
        /// └─ Part1
        ///     └─ Boss-Extrude1   ← NEW!
        ///         └─ Sketch1     ← Circle on Top Plane
        /// </code>
        /// 
        /// <para><strong>Typical Values:</strong></para>
        /// <list type="bullet">
        ///   <item>Small pin: diameter=5mm, height=10mm</item>
        ///   <item>Medium shaft: diameter=20mm, height=50mm</item>
        ///   <item>Large post: diameter=40mm, height=100mm</item>
        ///   <item>Thin disc: diameter=50mm, height=3mm</item>
        /// </list>
        /// </summary>
        /// 
        /// <param name="model">Active SolidWorks part document (must be Part type)</param>
        /// <param name="diameterMm">Cylinder diameter in millimeters (must be > 0). Default: 20mm</param>
        /// <param name="heightMm">Extrusion height in millimeters (must be > 0). Default: 10mm</param>
        /// 
        /// <returns>
        /// True if cylinder created successfully; False if validation fails or operation errors.
        /// </returns>
        /// 
        /// <exception cref="Exception">
        /// Caught internally. All exceptions are logged and method returns false.
        /// No exceptions are thrown to caller.
        /// </exception>
        /// 
        /// <remarks>
        /// <para><strong>Operation Flow:</strong></para>
        /// <code>
        /// 1. Validate parameters (diameter > 0, height > 0)
        /// 2. Check document type (must be Part)
        /// 3. Select "Top Plane"
        /// 4. Start sketch
        /// 5. Draw circle at origin (0, 0, 0)
        /// 6. Exit sketch
        /// 7. Boss-extrude (blind, upward direction)
        /// 8. Rebuild model
        /// 9. Commit UndoScope
        /// </code>
        /// 
        /// <para><strong>Error Handling:</strong></para>
        /// <list type="bullet">
        ///   <item>Invalid parameters → logs error, returns false</item>
        ///   <item>Wrong document type → logs error, returns false</item>
        ///   <item>Plane selection fails → logs error, returns false</item>
        ///   <item>Circle creation fails → logs error, rollback via UndoScope</item>
        ///   <item>Extrusion fails → logs error, rollback via UndoScope</item>
        /// </list>
        /// 
        /// <para><strong>Important Notes:</strong></para>
        /// <list type="bullet">
        ///   <item>Creates standalone cylinder - no requirement for existing bodies</item>
        ///   <item>Can be used on empty models</item>
        ///   <item>Can be combined with other features (base plates, holes, etc.)</item>
        ///   <item>Circle is centered at world origin (0, 0) on Top Plane</item>
        ///   <item>Extrusion direction is upward (+Z in SolidWorks default orientation)</item>
        /// </list>
        /// 
        /// <para><strong>Logging:</strong></para>
        /// Every step is logged for debugging:
        /// <code>
        /// [INFO] Creating cylinder on Top Plane:
        /// [INFO]   Diameter: 25 mm
        /// [INFO]   Height: 15 mm
        /// [INFO] Selecting Top Plane...
        /// [INFO] ✓ Top Plane selected
        /// [INFO] Starting sketch...
        /// [INFO] ✓ Sketch active
        /// [INFO] Creating circle at origin...
        /// [INFO] ✓ Circle created (radius=12.5 mm)
        /// [INFO] Exiting sketch...
        /// [INFO] Creating boss-extrude...
        /// [INFO] ✓ Cylinder created: 'Boss-Extrude1'
        /// [INFO] ✓ Cylinder created successfully!
        /// </code>
        /// </remarks>
        /// 
        /// <example>
        /// <para><strong>Basic Usage:</strong></para>
        /// <code>
        /// using TextToCad.SolidWorksAddin.Builders;
        /// using TextToCad.SolidWorksAddin.Utils;
        /// 
        /// // Create logger
        /// ILogger logger = new Logger(msg => Console.WriteLine(msg));
        /// 
        /// // Create builder
        /// var builder = new ExtrudedCylinderBuilder(swApp, logger);
        /// 
        /// // Create default cylinder (20mm × 10mm)
        /// bool success = builder.CreateCylinderOnTopPlane(model);
        /// 
        /// // Create custom cylinder
        /// success = builder.CreateCylinderOnTopPlane(
        ///     model,
        ///     diameterMm: 25.0,  // 25mm diameter
        ///     heightMm: 15.0     // 15mm height
        /// );
        /// </code>
        /// 
        /// <para><strong>Integration with Natural Language:</strong></para>
        /// <code>
        /// // User instruction: "create a cylinder 20mm diameter 30mm tall"
        /// 
        /// // Parse parameters from backend response
        /// double diameter = parsed.ParametersData?.DiameterMm ?? 20.0;
        /// double height = parsed.ParametersData?.HeightMm ?? 10.0;
        /// 
        /// // Create builder and execute
        /// var builder = new ExtrudedCylinderBuilder(swApp, logger);
        /// bool success = builder.CreateCylinderOnTopPlane(model, diameter, height);
        /// </code>
        /// </example>
        public bool CreateCylinderOnTopPlane(IModelDoc2 model, double diameterMm = 20.0, double heightMm = 10.0)
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

            // Check if model is a part document
            if (model.GetType() != (int)swDocumentTypes_e.swDocPART)
            {
                _log.Error("Active document is not a Part document");
                return false;
            }

            _log.Info("Creating cylinder on Top Plane:");
            _log.Info($"  Diameter: {diameterMm} mm");
            _log.Info($"  Height: {heightMm} mm");

            // Use UndoScope for safe rollback on failure
            using (var scope = new UndoScope(model, "Create Extruded Cylinder", _log))
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
                    _log.Info("✓ Top Plane selected");

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
                    _log.Info("✓ Sketch active and ready");

                    // Step 3: Create circle at origin
                    // Convert dimensions to meters for SolidWorks API
                    double radiusMm = diameterMm / 2.0;
                    double radiusM = Units.MmToM(radiusMm);

                    _log.Info($"Creating circle at origin (radius={radiusMm} mm)...");

                    // CreateCircle parameters:
                    // X, Y, Z of center point (origin)
                    // X, Y, Z of a point on the circumference
                    // Note: We use (radius, 0, 0) as the circumference point
                    object circleObj = model.SketchManager.CreateCircleByRadius(
                        0,        // X center (origin)
                        0,        // Y center (origin)
                        0,        // Z center (on plane)
                        radiusM   // Radius in meters
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

                    _log.Info($"✓ Circle created (radius={radiusMm} mm, diameter={diameterMm} mm)");

                    // Step 4: Exit sketch
                    _log.Info("Exiting sketch...");
                    model.SketchManager.InsertSketch(true);

                    // IMPORTANT: Do NOT clear selection here!
                    // InsertSketch(true) automatically selects the sketch we just exited,
                    // which is needed for the extrusion operation.
                    // Clearing selection here would cause the extrusion to fail or behave unpredictably.

                    // Step 5: Create boss-extrude feature
                    double heightM = Units.MmToM(heightMm);

                    _log.Info($"Creating boss-extrude (height={heightMm} mm)...");

                    // Use FeatureExtrusion method (20 parameters - same as BasePlateBuilder)
                    IFeature feature = model.FeatureManager.FeatureExtrusion(
                        true,              // SD: Single direction
                        false,             // Flip: Don't flip direction (extrude up)
                        false,             // Dir: Direction (not used for blind)
                        (int)swEndConditions_e.swEndCondBlind,  // T1: Blind extrusion
                        0,                 // T2: Not used (single direction)
                        heightM,           // D1: Depth in meters (height of cylinder)
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
                    _log.Info($"✓ Cylinder created: '{featureName}'");
                    _log.Info($"  Dimensions: {diameterMm}mm diameter × {heightMm}mm height");

                    // Step 6: Rebuild model
                    _log.Info("Rebuilding model...");
                    model.ForceRebuild3(false);

                    // Mark operation as successful
                    _log.Info("✓ Cylinder created successfully!");
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
