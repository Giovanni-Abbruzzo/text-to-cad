using System;
using System.Threading.Tasks;
using SolidWorks.Interop.sldworks;
using TextToCad.SolidWorksAddin.Builders;
using TextToCad.SolidWorksAddin.Interfaces;
using TextToCad.SolidWorksAddin.Models;
using TextToCad.SolidWorksAddin.Utils;

namespace TextToCad.SolidWorksAddin.Controllers
{
    /// <summary>
    /// Service controller for orchestrating the Preview → Execute workflow.
    /// Handles API communication, response parsing, validation, and builder orchestration.
    /// </summary>
    /// <remarks>
    /// This controller provides a clean separation between:
    /// - UI layer (TaskPaneControl)
    /// - API communication (ApiClient)
    /// - Business logic (Validation and orchestration)
    /// - CAD operations (Builders)
    /// 
    /// The Preview → Execute pattern:
    /// 1. Preview: Call /dry_run to show user what will happen (no side effects)
    /// 2. User reviews and confirms
    /// 3. Execute: Call /process_instruction and execute CAD operations
    /// 
    /// USAGE:
    /// var controller = new ExecuteController(swApp, logger);
    /// string preview = await controller.PreviewAsync("create 100mm plate 5mm thick");
    /// bool success = await controller.ExecuteAsync("create 100mm plate 5mm thick");
    /// </remarks>
    public class ExecuteController
    {
        private readonly ISldWorks _sw;
        private readonly ILogger _log;

        /// <summary>
        /// Create a new execute controller.
        /// </summary>
        /// <param name="sw">SolidWorks application instance</param>
        /// <param name="log">Logger for operation tracking</param>
        /// <exception cref="ArgumentNullException">If sw or log is null</exception>
        public ExecuteController(ISldWorks sw, ILogger log)
        {
            _sw = sw ?? throw new ArgumentNullException(nameof(sw));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>
        /// Preview an instruction without executing (calls /dry_run endpoint).
        /// </summary>
        /// <param name="instruction">Natural language CAD instruction</param>
        /// <param name="useAI">Whether to use AI parsing (requires API key)</param>
        /// <returns>Raw JSON response from backend for display/review</returns>
        /// <remarks>
        /// This method has no side effects:
        /// - Does not save to database
        /// - Does not create CAD geometry
        /// - Does not modify the model
        /// 
        /// Use this to show users what will happen before they commit to execution.
        /// </remarks>
        /// <example>
        /// <code>
        /// string preview = await controller.PreviewAsync("create a 20mm cylinder 40mm tall");
        /// // Display preview to user
        /// // User reviews and decides whether to execute
        /// </code>
        /// </example>
        public async Task<string> PreviewAsync(string instruction, bool useAI = false)
        {
            try
            {
                _log.Info($"Preview requested: '{instruction}' (use_ai={useAI})");

                // Validate input
                if (!ValidateInstruction(instruction))
                    return CreateErrorResponse("Invalid instruction: must be at least 3 characters");

                // Call dry run endpoint
                var request = new InstructionRequest(instruction, useAI);

                InstructionResponse response = await ApiClient.DryRunAsync(request);

                if (response == null)
                    return CreateErrorResponse("No response from backend");

                // Return formatted JSON for display
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.Indented);
                _log.Info("Preview successful");
                return json;
            }
            catch (Exception ex)
            {
                _log.Error($"Preview failed: {ex.Message}");
                return CreateErrorResponse($"Preview failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute an instruction (calls /process_instruction endpoint and creates CAD geometry).
        /// </summary>
        /// <param name="instruction">Natural language CAD instruction</param>
        /// <param name="useAI">Whether to use AI parsing (requires API key)</param>
        /// <param name="model">Optional: Specific model to execute on (uses active model if null)</param>
        /// <returns>True if execution succeeded; false otherwise</returns>
        /// <remarks>
        /// This method has side effects:
        /// - Saves instruction to database
        /// - Creates CAD geometry in the model
        /// - Modifies the active document
        /// 
        /// All operations are wrapped in UndoScope for safe rollback on failure.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Preview first (recommended)
        /// string preview = await controller.PreviewAsync("create 100mm plate 5mm thick");
        /// // Show preview to user...
        /// 
        /// // Execute if user confirms
        /// bool success = await controller.ExecuteAsync("create 100mm plate 5mm thick");
        /// </code>
        /// </example>
        public async Task<bool> ExecuteAsync(string instruction, bool useAI = false, IModelDoc2 model = null)
        {
            try
            {
                _log.Info($"Execute requested: '{instruction}' (use_ai={useAI})");

                // Validate input
                if (!ValidateInstruction(instruction))
                {
                    _log.Error("Invalid instruction");
                    return false;
                }

                // Get target model
                IModelDoc2 targetModel = model ?? (IModelDoc2)_sw.ActiveDoc;
                if (targetModel == null)
                {
                    _log.Error("No active model document");
                    return false;
                }

                // Ensure it's a Part document
                if (targetModel.GetType() != (int)SolidWorks.Interop.swconst.swDocumentTypes_e.swDocPART)
                {
                    _log.Error("Active document is not a Part");
                    return false;
                }

                // Call process instruction endpoint
                var request = new InstructionRequest(instruction, useAI);

                InstructionResponse response = await ApiClient.ProcessInstructionAsync(request);

                if (response == null)
                {
                    _log.Error("No response from backend");
                    return false;
                }

                _log.Info($"Backend response received: {response.GetSummary()}");

                // Execute the parsed operations
                return ExecuteResponse(targetModel, response);
            }
            catch (Exception ex)
            {
                _log.Error($"Execute failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Execute a parsed instruction response (handles single or multi-operation).
        /// </summary>
        /// <param name="model">Model to execute on</param>
        /// <param name="response">Parsed instruction response from backend</param>
        /// <returns>True if all operations succeeded; false if any failed</returns>
        private bool ExecuteResponse(IModelDoc2 model, InstructionResponse response)
        {
            try
            {
                // Check for multi-operation
                if (response.IsMultiOperation)
                {
                    _log.Info($"Executing {response.Operations.Count} operations...");

                    bool allSucceeded = true;
                    for (int i = 0; i < response.Operations.Count; i++)
                    {
                        var operation = response.Operations[i];
                        _log.Info($"▶ Operation {i + 1}/{response.Operations.Count}: {operation.Action}");

                        bool success = ExecuteSingleOperation(model, operation);
                        if (!success)
                        {
                            _log.Error($"✗ Operation {i + 1} failed");
                            allSucceeded = false;
                            // Continue with remaining operations
                        }
                        else
                        {
                            _log.Info($"✓ Operation {i + 1} completed");
                        }
                    }

                    return allSucceeded;
                }
                else
                {
                    // Single operation
                    _log.Info("Executing single operation...");
                    return ExecuteSingleOperation(model, response.ParsedParameters);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"ExecuteResponse failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Execute a single parsed operation.
        /// </summary>
        /// <param name="model">Model to execute on</param>
        /// <param name="parsed">Parsed parameters</param>
        /// <returns>True if successful; false otherwise</returns>
        private bool ExecuteSingleOperation(IModelDoc2 model, ParsedParameters parsed)
        {
            try
            {
                if (parsed == null || parsed.ParametersData == null)
                {
                    _log.Error("Invalid parsed parameters");
                    return false;
                }

                var data = parsed.ParametersData;

                // Validate parameters
                if (!ValidateParameters(data))
                {
                    _log.Error("Parameter validation failed");
                    return false;
                }

                // Route to appropriate builder based on action and shape
                string action = parsed.Action?.ToLowerInvariant() ?? "";
                string shape = data.Shape?.ToLowerInvariant() ?? "";

                _log.Info($"Routing operation: action='{action}', shape='{shape}'");

                // Handle fillet operation
                if (action == "fillet" || shape == "fillet")
                {
                    return ExecuteFillet(model, data);
                }

                // Handle base plate
                if (shape.Contains("base") || shape.Contains("plate") || shape.Contains("rectangular") || shape == "base_plate")
                {
                    return ExecuteBasePlate(model, data);
                }

                // Handle cylinder
                if (shape == "cylinder" || shape == "cylindrical")
                {
                    return ExecuteCylinder(model, data);
                }

                // Handle holes
                if (action.Contains("hole") || data.Pattern != null)
                {
                    return ExecuteHoles(model, data);
                }

                _log.Warn($"Unhandled operation: action='{action}', shape='{shape}'");
                return false;
            }
            catch (Exception ex)
            {
                _log.Error($"ExecuteSingleOperation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Execute base plate creation.
        /// </summary>
        private bool ExecuteBasePlate(IModelDoc2 model, Parameters data)
        {
            try
            {
                double sizeMm = data.DiameterMm ?? 80.0;
                double thicknessMm = data.HeightMm ?? 6.0;

                _log.Info($"Creating base plate: {sizeMm}×{sizeMm}×{thicknessMm} mm");

                var builder = new BasePlateBuilder(_sw, _log);
                return builder.EnsureBasePlate(model, sizeMm, thicknessMm);
            }
            catch (Exception ex)
            {
                _log.Error($"Base plate execution failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Execute cylinder creation.
        /// </summary>
        private bool ExecuteCylinder(IModelDoc2 model, Parameters data)
        {
            try
            {
                double diameterMm = data.DiameterMm ?? 20.0;
                double heightMm = data.HeightMm ?? 30.0;

                _log.Info($"Creating cylinder: Ø{diameterMm} mm × {heightMm} mm");

                var builder = new ExtrudedCylinderBuilder(_sw, _log);
                return builder.CreateCylinderOnTopPlane(model, diameterMm, heightMm);
            }
            catch (Exception ex)
            {
                _log.Error($"Cylinder execution failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Execute hole pattern creation.
        /// </summary>
        private bool ExecuteHoles(IModelDoc2 model, Parameters data)
        {
            try
            {
                int count = data.Count ?? 4;
                double diameterMm = data.DiameterMm ?? 5.0;

                _log.Info($"Creating {count} holes with Ø{diameterMm} mm");

                var builder = new CircularHolesBuilder(_sw, _log);
                return builder.CreatePatternOnTopFace(model, count, diameterMm);
            }
            catch (Exception ex)
            {
                _log.Error($"Holes execution failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Execute fillet creation.
        /// </summary>
        private bool ExecuteFillet(IModelDoc2 model, Parameters data)
        {
            try
            {
                double radiusMm = data.DiameterMm ?? 2.0; // Use diameter field for radius
                bool allEdges = data.Count.HasValue && data.Count.Value == 0; // Count=0 means all edges

                _log.Info($"Creating fillet: radius={radiusMm} mm, target={(allEdges ? "all edges" : "recent feature")}");

                var builder = new FilletBuilder(_sw, _log);

                if (allEdges)
                {
                    return builder.ApplyFilletToAllSharpEdges(model, radiusMm);
                }
                else
                {
                    return builder.ApplyFilletToRecentEdges(model, radiusMm);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Fillet execution failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validate instruction string.
        /// </summary>
        private bool ValidateInstruction(string instruction)
        {
            if (string.IsNullOrWhiteSpace(instruction))
            {
                _log.Error("Instruction is empty");
                return false;
            }

            if (instruction.Trim().Length < 3)
            {
                _log.Error($"Instruction too short: '{instruction}' (minimum 3 characters)");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validate parameter values (dimensions must be > 0).
        /// </summary>
        private bool ValidateParameters(Parameters data)
        {
            if (data == null)
            {
                _log.Error("Parameters data is null");
                return false;
            }

            // Validate diameter (if present)
            if (data.DiameterMm.HasValue && data.DiameterMm.Value <= 0)
            {
                _log.Error($"Invalid diameter: {data.DiameterMm.Value} mm (must be > 0)");
                return false;
            }

            // Validate height (if present)
            if (data.HeightMm.HasValue && data.HeightMm.Value <= 0)
            {
                _log.Error($"Invalid height: {data.HeightMm.Value} mm (must be > 0)");
                return false;
            }

            // Validate count (if present)
            if (data.Count.HasValue && data.Count.Value < 0)
            {
                _log.Error($"Invalid count: {data.Count.Value} (must be ≥ 0)");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Create an error response JSON string.
        /// </summary>
        private string CreateErrorResponse(string message)
        {
            return $"{{\"error\": \"{message}\"}}";
        }
    }
}
