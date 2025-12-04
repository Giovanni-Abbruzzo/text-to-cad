using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using TextToCad.SolidWorksAddin.Models;

namespace TextToCad.SolidWorksAddin
{
    /// <summary>
    /// Main UI control for the Task Pane.
    /// Provides interface for entering instructions and viewing results.
    /// </summary>
    [ComVisible(true)]
    public partial class TaskPaneControl : UserControl
    {
        #region Private Fields

        private bool isProcessing = false;
        private Addin _addin;

        #endregion

        #region Constructor

        public TaskPaneControl()
        {
            InitializeComponent();
            InitializeUI();
            Logger.Info("TaskPaneControl initialized");
        }

        /// <summary>
        /// Set the add-in reference for accessing SolidWorks API
        /// </summary>
        public void SetAddin(Addin addin)
        {
            _addin = addin;
        }

        #endregion

        #region UI Initialization

        /// <summary>
        /// Additional UI initialization beyond the designer
        /// </summary>
        private void InitializeUI()
        {
            // Set initial API URL from config
            txtApiBase.Text = ApiClient.GetBaseUrl();

            // Set placeholder text styling
            txtInstruction.ForeColor = SystemColors.GrayText;
            txtInstruction.Text = "Enter CAD instruction here... (e.g., 'create 4 holes in a circular pattern')";

            // Update connection status
            UpdateConnectionStatus();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle Preview button click - calls /dry_run endpoint
        /// </summary>
        private async void btnPreview_Click(object sender, EventArgs e)
        {
            // Validate instruction
            if (!ErrorHandler.ValidateInstruction(txtInstruction.Text, out string errorMessage))
            {
                AppendLog("❌ Validation Error", Color.Red);
                AppendLog(errorMessage, Color.Red);
                return;
            }

            if (isProcessing)
            {
                AppendLog("⏳ Already processing a request...", Color.Orange);
                return;
            }

            try
            {
                isProcessing = true;
                SetUIEnabled(false);

                AppendLog("🔍 Previewing instruction...", Color.Blue);
                Logger.Info($"Preview requested: '{txtInstruction.Text}'");

                // Create request
                var request = new InstructionRequest(txtInstruction.Text, chkUseAI.Checked);

                // Call API
                var response = await ApiClient.DryRunAsync(request);

                // Display results
                DisplayResponse(response, isPreview: true);

                AppendLog("✓ Preview complete", Color.Green);
            }
            catch (Exception ex)
            {
                string errorMsg = ErrorHandler.HandleException(ex, "Preview");
                AppendLog("❌ Preview Failed", Color.Red);
                AppendLog(errorMsg, Color.Red);
            }
            finally
            {
                isProcessing = false;
                SetUIEnabled(true);
            }
        }

        /// <summary>
        /// Handle Execute button click - calls /process_instruction endpoint
        /// </summary>
        private async void btnExecute_Click(object sender, EventArgs e)
        {
            // Validate instruction
            if (!ErrorHandler.ValidateInstruction(txtInstruction.Text, out string errorMessage))
            {
                AppendLog("❌ Validation Error", Color.Red);
                AppendLog(errorMessage, Color.Red);
                return;
            }

            if (isProcessing)
            {
                AppendLog("⏳ Already processing a request...", Color.Orange);
                return;
            }

            // Confirm execution
            if (!ErrorHandler.Confirm(
                $"Execute this instruction?\n\n\"{txtInstruction.Text}\"\n\n" +
                "This will save the command to the database.",
                "Confirm Execution"))
            {
                AppendLog("❌ Execution cancelled by user", Color.Orange);
                return;
            }

            try
            {
                isProcessing = true;
                SetUIEnabled(false);

                AppendLog("⚙️ Executing instruction...", Color.Blue);
                Logger.Info($"Execute requested: '{txtInstruction.Text}'");

                // Create request
                var request = new InstructionRequest(txtInstruction.Text, chkUseAI.Checked);

                // Call API
                var response = await ApiClient.ProcessInstructionAsync(request);

                // Display results
                DisplayResponse(response, isPreview: false);

                AppendLog("✓ Execution complete (saved to database)", Color.Green);

                // === NEW: Actually execute the CAD operation ===
                AppendLog("", Color.Black);
                AppendLog("🔧 Creating CAD geometry...", Color.Blue);
                
                bool geometryCreated = ExecuteCADOperation(response);
                
                if (geometryCreated)
                {
                    AppendLog("✓ CAD geometry created successfully!", Color.Green);
                }
                else
                {
                    AppendLog("⚠️ Geometry creation skipped or failed (see details above)", Color.Orange);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = ErrorHandler.HandleException(ex, "Execute");
                AppendLog("❌ Execution Failed", Color.Red);
                AppendLog(errorMsg, Color.Red);
            }
            finally
            {
                isProcessing = false;
                SetUIEnabled(true);
            }
        }

        /// <summary>
        /// Handle Clear Log button click
        /// </summary>
        private void btnClearLog_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
            txtPlan.Clear();
            lblStatus.Text = "Ready";
            lblStatus.ForeColor = SystemColors.ControlText;
            Logger.Debug("Log cleared");
        }

        /// <summary>
        /// Handle API URL change
        /// </summary>
        private void btnUpdateUrl_Click(object sender, EventArgs e)
        {
            try
            {
                string newUrl = txtApiBase.Text.Trim();
                ApiClient.SetBaseUrl(newUrl);
                AppendLog($"✓ API URL updated to: {newUrl}", Color.Green);
                UpdateConnectionStatus();
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Invalid URL: {ex.Message}", Color.Red);
            }
        }

        /// <summary>
        /// Handle Test Connection button click
        /// </summary>
        private async void btnTestConnection_Click(object sender, EventArgs e)
        {
            try
            {
                SetUIEnabled(false);
                AppendLog("🔌 Testing connection...", Color.Blue);

                bool connected = await ApiClient.TestConnectionAsync();

                if (connected)
                {
                    AppendLog("✓ Connection successful!", Color.Green);
                    UpdateConnectionStatus(true);
                }
                else
                {
                    AppendLog("❌ Connection failed - server not responding", Color.Red);
                    UpdateConnectionStatus(false);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Connection error: {ex.Message}", Color.Red);
                UpdateConnectionStatus(false);
            }
            finally
            {
                SetUIEnabled(true);
            }
        }

        /// <summary>
        /// Handle Open Logs button click
        /// </summary>
        private void btnOpenLogs_Click(object sender, EventArgs e)
        {
            Logger.OpenLogDirectory();
        }

        /// <summary>
        /// Test Units conversion utilities
        /// </summary>
        private void btnTestUnits_Click(object sender, EventArgs e)
        {
            AppendLog("\n═══ Testing Units Conversion ═══", Color.DarkBlue);
            
            double mm = 100.0;
            double m = Utils.Units.MmToM(mm);
            double backToMm = Utils.Units.MToMm(m);
            
            AppendLog($"100mm → {m}m → {backToMm}mm", Color.Black);
            
            if (Math.Abs(backToMm - mm) < 0.0001)
            {
                AppendLog("✓ Units conversion test PASSED", Color.Green);
            }
            else
            {
                AppendLog("✗ Units conversion test FAILED", Color.Red);
            }
        }

        /// <summary>
        /// Test plane selection utilities
        /// </summary>
        private void btnTestPlanes_Click(object sender, EventArgs e)
        {
            AppendLog("\n═══ Testing Plane Selection ═══", Color.DarkBlue);
            
            if (_addin == null)
            {
                AppendLog("✗ Add-in reference not set", Color.Red);
                return;
            }
            
            var model = _addin.SwApp.ActiveDoc as SolidWorks.Interop.sldworks.IModelDoc2;
            if (model == null)
            {
                AppendLog("✗ No active document. Please open a part.", Color.Orange);
                return;
            }

            var logger = new Utils.Logger(msg => AppendLog(msg, Color.Black));
            
            string[] planeNames = { "Top Plane", "Front Plane", "Right Plane" };
            foreach (var planeName in planeNames)
            {
                bool found = Utils.Selection.SelectPlaneByName(_addin.SwApp, model, planeName, false, logger);
                if (found)
                {
                    AppendLog($"✓ {planeName} selected - pausing to show...", Color.Green);
                    Application.DoEvents(); // Update UI
                    System.Threading.Thread.Sleep(1000); // Pause 1 second to see selection
                }
                else
                {
                    AppendLog($"✗ {planeName} not found", Color.Red);
                }
            }
            
            AppendLog("Plane selection test complete", Color.DarkBlue);
        }

        /// <summary>
        /// Test face selection utilities
        /// </summary>
        private void btnTestFaces_Click(object sender, EventArgs e)
        {
            AppendLog("\n═══ Testing Face Selection ═══", Color.DarkBlue);
            
            if (_addin == null)
            {
                AppendLog("✗ Add-in reference not set", Color.Red);
                return;
            }
            
            var model = _addin.SwApp.ActiveDoc as SolidWorks.Interop.sldworks.IModelDoc2;
            if (model == null)
            {
                AppendLog("✗ No active document. Please open a part with faces.", Color.Orange);
                return;
            }

            var logger = new Utils.Logger(msg => AppendLog(msg, Color.Black));
            
            // Test finding top planar face (by highest Y-coordinate = top/bottom plane)
            AppendLog("Searching for top-most planar face (highest Y = top)...", Color.Black);
            var topFace = Utils.Selection.GetTopMostPlanarFace(model, logger);
            if (topFace != null)
            {
                bool selected = Utils.Selection.SelectFace(model, topFace, false, logger);
                AppendLog(selected ? "✓ Top planar face (highest Y-coordinate) found and selected" : "✗ Face found but selection failed", 
                         selected ? Color.Green : Color.Orange);
                AppendLog("Note: Y-axis = top/bottom, X-axis = right/left, Z-axis = front/back", Color.DarkGray);
            }
            else
            {
                AppendLog("✗ No planar faces found (create a simple box to test)", Color.Orange);
            }
            
            int selCount = Utils.Selection.GetSelectionCount(model);
            AppendLog($"Current selection count: {selCount}", Color.DarkGray);
        }

        /// <summary>
        /// Test UndoScope utilities
        /// </summary>
        private void btnTestUndo_Click(object sender, EventArgs e)
        {
            AppendLog("\n═══ Testing Undo Scope ═══", Color.DarkBlue);
            
            if (_addin == null)
            {
                AppendLog("✗ Add-in reference not set", Color.Red);
                return;
            }
            
            var model = _addin.SwApp.ActiveDoc as SolidWorks.Interop.sldworks.IModelDoc2;
            if (model == null)
            {
                AppendLog("✗ No active document. Please open a part.", Color.Orange);
                return;
            }

            var logger = new Utils.Logger(msg => AppendLog(msg, Color.Black));
            
            AppendLog("⚠️ WARNING: EditRollback() behavior", Color.Orange);
            AppendLog("SolidWorks EditRollback() rolls back to before the SELECTED feature,", Color.DarkGray);
            AppendLog("not to a programmatic undo point. This is a SolidWorks API limitation.", Color.DarkGray);
            AppendLog("If a feature is selected, it will rollback to before that feature.\n", Color.DarkGray);
            
            // Test committed scope
            AppendLog("Test 1: Committed UndoScope (no rollback)", Color.DarkBlue);
            using (var scope = new Utils.UndoScope(model, "Test Operation", logger))
            {
                AppendLog("  Inside undo scope...", Color.Black);
                scope.Commit();
                AppendLog("  Scope committed - rollback prevented", Color.Black);
            }
            AppendLog("✓ Committed scope test complete\n", Color.Green);
            
            // Test uncommitted scope (will attempt rollback)
            AppendLog("Test 2: Uncommitted UndoScope (triggers rollback)", Color.DarkBlue);
            AppendLog("  Deselecting all features first...", Color.DarkGray);
            model.ClearSelection2(true);
            
            using (var scope = new Utils.UndoScope(model, "Rollback Test", logger))
            {
                AppendLog("  Inside undo scope...", Color.Black);
                // Not calling Commit() - should trigger rollback warning
                AppendLog("  (Not committing - will attempt rollback on dispose)", Color.DarkGray);
            }
            AppendLog("✓ Rollback scope test complete", Color.Green);
            AppendLog("\nNote: Actual rollback effect depends on feature selection state", Color.Orange);
        }

        /// <summary>
        /// Handle instruction textbox focus - clear placeholder
        /// </summary>
        private void txtInstruction_Enter(object sender, EventArgs e)
        {
            if (txtInstruction.ForeColor == SystemColors.GrayText)
            {
                txtInstruction.Text = "";
                txtInstruction.ForeColor = SystemColors.WindowText;
            }
        }

        /// <summary>
        /// Handle instruction textbox blur - restore placeholder if empty
        /// </summary>
        private void txtInstruction_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtInstruction.Text))
            {
                txtInstruction.ForeColor = SystemColors.GrayText;
                txtInstruction.Text = "Enter CAD instruction here... (e.g., 'create 4 holes in a circular pattern')";
            }
        }

        #endregion

        #region CAD Execution

        /// <summary>
        /// Execute the actual CAD operation based on parsed API response.
        /// This is where natural language gets converted to real geometry!
        /// Supports multi-operation instructions (multiple lines).
        /// </summary>
        /// <param name="response">Parsed instruction response from backend</param>
        /// <returns>True if all operations succeeded; false if any failed</returns>
        private bool ExecuteCADOperation(InstructionResponse response)
        {
            try
            {
                // Get SolidWorks application and active document
                if (_addin == null)
                {
                    AppendLog("❌ Add-in not initialized", Color.Red);
                    Logger.Error("ExecuteCADOperation: _addin is null");
                    return false;
                }

                var swApp = _addin.SwApp;
                if (swApp == null)
                {
                    AppendLog("❌ SolidWorks application not available", Color.Red);
                    Logger.Error("ExecuteCADOperation: SwApp is null");
                    return false;
                }

                var model = swApp.ActiveDoc as SolidWorks.Interop.sldworks.IModelDoc2;
                if (model == null)
                {
                    AppendLog("❌ No active SolidWorks document", Color.Red);
                    AppendLog("Please open a Part document first", Color.Orange);
                    System.Diagnostics.Debug.WriteLine("ExecuteCADOperation: No active document");
                    return false;
                }

                // Check document type
                if (model.GetType() != (int)SolidWorks.Interop.swconst.swDocumentTypes_e.swDocPART)
                {
                    AppendLog("❌ Active document is not a Part", Color.Red);
                    AppendLog("Please open a Part document (not Assembly or Drawing)", Color.Orange);
                    System.Diagnostics.Debug.WriteLine($"ExecuteCADOperation: Document type is {model.GetType()}, not Part");
                    return false;
                }

                // Create logger that forwards to UI
                var logger = new Utils.Logger(msg => AppendLog(msg, Color.DarkGray));

                // Check if this is a multi-operation instruction
                // Debug: Log operations array status
                if (response.Operations != null)
                {
                    AppendLog($"Operations array present: {response.Operations.Count} items", Color.DarkGray);
                }
                else
                {
                    AppendLog("Operations array is NULL", Color.Orange);
                }
                
                if (response.IsMultiOperation)
                {
                    AppendLog($"🔄 Multi-operation instruction: {response.Operations.Count} operations", Color.Blue);
                    
                    bool allSucceeded = true;
                    for (int i = 0; i < response.Operations.Count; i++)
                    {
                        var operation = response.Operations[i];
                        AppendLog($"\n▶ Operation {i + 1}/{response.Operations.Count}:", Color.DarkBlue);
                        
                        // Debug: Log operation details
                        if (operation != null)
                        {
                            AppendLog($"  Action: {operation.Action}, Shape: {operation.ParametersData?.Shape}", Color.DarkGray);
                        }
                        else
                        {
                            AppendLog("  ERROR: Operation is NULL", Color.Red);
                            continue;
                        }
                        
                        try
                        {
                            bool success = ExecuteSingleOperation(swApp, model, operation, logger);
                            
                            if (success)
                            {
                                AppendLog($"✓ Operation {i + 1} completed", Color.Green);
                            }
                            else
                            {
                                AppendLog($"✗ Operation {i + 1} failed", Color.Red);
                                allSucceeded = false;
                                // Continue with remaining operations even if one fails
                            }
                        }
                        catch (Exception opEx)
                        {
                            AppendLog($"✗ Operation {i + 1} threw exception: {opEx.Message}", Color.Red);
                            System.Diagnostics.Debug.WriteLine($"Operation {i + 1} exception: {opEx.Message}\n{opEx.StackTrace}");
                            allSucceeded = false;
                            // Continue with remaining operations even if one throws
                        }
                    }
                    
                    if (allSucceeded)
                    {
                        AppendLog($"\n✓ All {response.Operations.Count} operations completed successfully!", Color.Green);
                    }
                    else
                    {
                        AppendLog($"\n⚠️ Some operations failed - check log above", Color.Orange);
                    }
                    
                    return allSucceeded;
                }
                else
                {
                    // Single operation - use backward compatible path
                    var parsed = response.ParsedParameters;
                    if (parsed == null)
                    {
                        AppendLog("⚠️ No parameters parsed from instruction", Color.Orange);
                        System.Diagnostics.Debug.WriteLine("ExecuteCADOperation: ParsedParameters is null");
                        return false;
                    }
                    
                    return ExecuteSingleOperation(swApp, model, parsed, logger);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"❌ CAD execution error: {ex.Message}", Color.Red);
                System.Diagnostics.Debug.WriteLine($"ExecuteCADOperation exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Execute a single CAD operation by dispatching to the appropriate builder
        /// </summary>
        /// <param name="swApp">SolidWorks application</param>
        /// <param name="model">Active model document</param>
        /// <param name="parsed">Parsed parameters for this operation</param>
        /// <param name="logger">Logger instance</param>
        /// <returns>True if operation succeeded; false otherwise</returns>
        private bool ExecuteSingleOperation(
            SolidWorks.Interop.sldworks.ISldWorks swApp,
            SolidWorks.Interop.sldworks.IModelDoc2 model,
            ParsedParameters parsed,
            Interfaces.ILogger logger)
        {
            try
            {
                string action = parsed.Action?.ToLower() ?? "";
                string shape = parsed.ParametersData?.Shape?.ToLower() ?? "";

                AppendLog($"Action: {parsed.Action}, Shape: {parsed.ParametersData?.Shape}", Color.DarkGray);

                // === DISPATCH TO APPROPRIATE BUILDER ===

                // Base Plate Creation
                if (shape.Contains("base") || shape.Contains("plate") || shape.Contains("rectangular") || shape.Contains("base_plate"))
                {
                    AppendLog("Detected: Base Plate creation", Color.Blue);
                    return CreateBasePlate(swApp, model, parsed, logger);
                }
                
                // Cylinder Creation (Sprint SW-5)
                else if (shape.Contains("cylinder") || shape.Contains("cylindrical") || shape.Contains("circular"))
                {
                    AppendLog("Detected: Cylinder creation", Color.Blue);
                    return CreateCylinder(swApp, model, parsed, logger);
                }
                
                // Hole Pattern (Sprint SW-4)
                else if (shape.Contains("hole") || action.Contains("hole") || shape.Contains("pattern"))
                {
                    AppendLog("Detected: Circular hole pattern creation", Color.Blue);
                    return CreateCircularHoles(swApp, model, parsed, logger);
                }
                
                // Unknown operation
                else
                {
                    AppendLog($"⚠️ Unknown operation: {parsed.Action} / {parsed.ParametersData?.Shape}", Color.Orange);
                    AppendLog("Currently supported: base plates, cylinders, circular hole patterns", Color.Gray);
                    System.Diagnostics.Debug.WriteLine($"ExecuteSingleOperation: Unrecognized action/shape: {action}/{shape}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Operation error: {ex.Message}", Color.Red);
                System.Diagnostics.Debug.WriteLine($"ExecuteSingleOperation exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create a base plate using BasePlateBuilder
        /// </summary>
        private bool CreateBasePlate(
            SolidWorks.Interop.sldworks.ISldWorks swApp,
            SolidWorks.Interop.sldworks.IModelDoc2 model,
            ParsedParameters parsed,
            Interfaces.ILogger logger)
        {
            try
            {
                // Extract dimensions from parsed parameters
                // Note: Backend uses "diameter" for width sometimes, and "height" for extrusion depth
                double sizeMm = 80.0;  // Default
                double thicknessMm = 6.0;  // Default

                var data = parsed.ParametersData;
                if (data != null)
                {
                    // Try to get size from diameter field (backend uses this for width)
                    if (data.DiameterMm.HasValue && data.DiameterMm.Value > 0)
                    {
                        sizeMm = data.DiameterMm.Value;
                    }

                    // Try to get thickness from height
                    if (data.HeightMm.HasValue && data.HeightMm.Value > 0)
                    {
                        thicknessMm = data.HeightMm.Value;
                    }
                }

                AppendLog($"Creating base plate: {sizeMm}×{sizeMm}×{thicknessMm} mm", Color.Blue);

                // Create the builder
                var builder = new Builders.BasePlateBuilder(swApp, logger);

                // Execute!
                bool success = builder.EnsureBasePlate(model, sizeMm, thicknessMm);

                return success;
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Base plate creation failed: {ex.Message}", Color.Red);
                Logger.Error($"CreateBasePlate exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create a circular pattern of holes using CircularHolesBuilder
        /// </summary>
        private bool CreateCircularHoles(
            SolidWorks.Interop.sldworks.ISldWorks swApp,
            SolidWorks.Interop.sldworks.IModelDoc2 model,
            ParsedParameters parsed,
            Interfaces.ILogger logger)
        {
            try
            {
                // Extract parameters from parsed data
                var data = parsed.ParametersData;
                if (data == null)
                {
                    AppendLog("⚠️ No parameters data for hole pattern", Color.Orange);
                    return false;
                }

                // Required parameters - with fallbacks from Pattern property
                int count = data.Count ?? data.Pattern?.Count ?? 4;  // Default: 4 holes (square pattern)
                double diameterMm = data.DiameterMm ?? 5.0;  // Default: 5mm holes (M5 bolts)

                // Optional parameters - check both direct and Pattern properties
                double? angleDeg = data.AngleDeg ?? data.Pattern?.AngleDeg;  // null = full circle (360°)
                double? patternRadiusMm = data.RadiusMm;  // null = calculated from plate size
                double? plateSizeMm = data.WidthMm ?? 80.0;  // Default: 80mm plate

                AppendLog($"Creating circular hole pattern:", Color.Blue);
                AppendLog($"  Count: {count}, Diameter: {diameterMm}mm", Color.DarkGray);
                if (angleDeg.HasValue)
                    AppendLog($"  Angle: {angleDeg}°", Color.DarkGray);
                if (patternRadiusMm.HasValue)
                    AppendLog($"  Pattern radius: {patternRadiusMm}mm", Color.DarkGray);

                // Create the builder
                var builder = new Builders.CircularHolesBuilder(swApp, logger);

                // Execute!
                bool success = builder.CreatePatternOnTopFace(
                    model,
                    count,
                    diameterMm,
                    angleDeg,
                    patternRadiusMm,
                    plateSizeMm
                );

                return success;
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Circular hole pattern creation failed: {ex.Message}", Color.Red);
                System.Diagnostics.Debug.WriteLine($"CreateCircularHoles exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create an extruded cylinder using ExtrudedCylinderBuilder
        /// </summary>
        private bool CreateCylinder(
            SolidWorks.Interop.sldworks.ISldWorks swApp,
            SolidWorks.Interop.sldworks.IModelDoc2 model,
            ParsedParameters parsed,
            Interfaces.ILogger logger)
        {
            try
            {
                // Extract parameters from parsed data
                var data = parsed.ParametersData;
                if (data == null)
                {
                    AppendLog("⚠️ No parameters data for cylinder", Color.Orange);
                    return false;
                }

                // Extract dimensions with defaults
                double diameterMm = data.DiameterMm ?? 20.0;  // Default: 20mm diameter
                double heightMm = data.HeightMm ?? 10.0;      // Default: 10mm height

                AppendLog($"Creating cylinder:", Color.Blue);
                AppendLog($"  Diameter: {diameterMm}mm, Height: {heightMm}mm", Color.DarkGray);

                // Create the builder
                var builder = new Builders.ExtrudedCylinderBuilder(swApp, logger);

                // Execute!
                bool success = builder.CreateCylinderOnTopPlane(model, diameterMm, heightMm);

                return success;
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Cylinder creation failed: {ex.Message}", Color.Red);
                System.Diagnostics.Debug.WriteLine($"CreateCylinder exception: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Display API response in the UI
        /// </summary>
        private void DisplayResponse(InstructionResponse response, bool isPreview)
        {
            string mode = isPreview ? "PREVIEW" : "EXECUTE";
            string source = response.IsAIParsed ? "AI" : "Rule-based";

            AppendLog($"\n═══════════════════════════════════", Color.DarkBlue);
            AppendLog($"  {mode} RESULTS", Color.DarkBlue);
            AppendLog($"═══════════════════════════════════", Color.DarkBlue);
            AppendLog($"Source: {source} parsing", Color.DarkGray);
            AppendLog($"Schema Version: {response.SchemaVersion}", Color.DarkGray);
            AppendLog("", Color.Black);

            // Display plan
            AppendLog("📋 PLAN:", Color.DarkBlue);
            txtPlan.Clear();
            if (response.Plan != null && response.Plan.Count > 0)
            {
                foreach (var step in response.Plan)
                {
                    AppendLog($"  • {step}", Color.Black);
                    txtPlan.AppendText($"• {step}\r\n");
                }
            }
            else
            {
                AppendLog("  (No plan available)", Color.Gray);
            }

            AppendLog("", Color.Black);

            // Display parsed parameters
            if (response.ParsedParameters != null)
            {
                AppendLog("🔧 PARSED PARAMETERS:", Color.DarkBlue);
                AppendLog($"  Action: {response.ParsedParameters.GetActionDescription()}", Color.Black);
                AppendLog($"  {response.ParsedParameters.GetParametersSummary()}", Color.Black);
            }

            AppendLog($"═══════════════════════════════════\n", Color.DarkBlue);

            // Update status
            lblStatus.Text = $"{mode} Complete - {source}";
            lblStatus.ForeColor = Color.Green;
        }

        /// <summary>
        /// Append text to log with color
        /// </summary>
        private void AppendLog(string text, Color color)
        {
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;
            txtLog.SelectionColor = color;
            txtLog.AppendText(text + "\r\n");
            txtLog.SelectionColor = txtLog.ForeColor;
            txtLog.ScrollToCaret();
        }

        /// <summary>
        /// Enable/disable UI controls during processing
        /// </summary>
        private void SetUIEnabled(bool enabled)
        {
            btnPreview.Enabled = enabled;
            btnExecute.Enabled = enabled;
            btnUpdateUrl.Enabled = enabled;
            btnTestConnection.Enabled = enabled;
            txtInstruction.Enabled = enabled;
            txtApiBase.Enabled = enabled;
            chkUseAI.Enabled = enabled;

            if (!enabled)
            {
                lblStatus.Text = "Processing...";
                lblStatus.ForeColor = Color.Orange;
            }
        }

        /// <summary>
        /// Update connection status indicator
        /// </summary>
        private async void UpdateConnectionStatus(bool? forceStatus = null)
        {
            bool connected;
            
            if (forceStatus.HasValue)
            {
                connected = forceStatus.Value;
            }
            else
            {
                try
                {
                    connected = await ApiClient.TestConnectionAsync();
                }
                catch
                {
                    connected = false;
                }
            }

            if (connected)
            {
                lblConnectionStatus.Text = "● Connected";
                lblConnectionStatus.ForeColor = Color.Green;
            }
            else
            {
                lblConnectionStatus.Text = "● Disconnected";
                lblConnectionStatus.ForeColor = Color.Red;
            }
        }

        #endregion
    }
}
