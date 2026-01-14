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
    /// Service controller for orchestrating the Preview and Execute workflow.
    /// Handles API communication, response parsing, validation, and builder orchestration.
    /// </summary>
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
                        _log.Info($"Operation {i + 1}/{response.Operations.Count}: {operation.Action}");

                        bool success = ExecuteSingleOperation(model, operation);
                        if (!success)
                        {
                            _log.Error($"Operation {i + 1} failed");
                            allSucceeded = false;
                            // Continue with remaining operations
                        }
                        else
                        {
                            _log.Info($"Operation {i + 1} completed");
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
                bool hasChamferParams = data.ChamferDistanceMm.HasValue || !string.IsNullOrWhiteSpace(data.ChamferTarget);
                bool hasFilletParams = !string.IsNullOrWhiteSpace(data.FilletTarget);

                _log.Info($"Routing operation: action='{action}', shape='{shape}'");

                // Handle fillet operation
                if (action == "fillet" || shape == "fillet" || (hasFilletParams && string.IsNullOrEmpty(shape)))
                {
                    return ExecuteFillet(model, data);
                }

                // Handle base plate / block
                if (shape.Contains("base") || shape.Contains("plate") || shape.Contains("rectangular") ||
                    shape.Contains("block") || shape.Contains("box") || shape.Contains("cube") || shape == "base_plate")
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

                // Handle chamfer
                if (action.Contains("chamfer") || shape.Contains("chamfer") || (hasChamferParams && string.IsNullOrEmpty(shape)))
                {
                    return ExecuteChamfer(model, data);
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
                double widthMm = data.WidthMm ?? data.LengthMm ?? data.DiameterMm ?? 80.0;
                double lengthMm = data.LengthMm ?? data.WidthMm ?? data.DiameterMm ?? widthMm;
                double thicknessMm = data.HeightMm ?? data.DepthMm ?? 6.0;

                _log.Info($"Creating base plate: {lengthMm}x{widthMm}x{thicknessMm} mm");

                var builder = new BasePlateBuilder(_sw, _log);
                return builder.EnsureBasePlate(
                    model,
                    widthMm,
                    thicknessMm,
                    widthMm,
                    lengthMm,
                    data.DraftAngleDeg,
                    data.DraftOutward,
                    data.FlipDirection
                );
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
                double diameterMm = data.DiameterMm ?? (data.RadiusMm.HasValue ? data.RadiusMm.Value * 2.0 : 20.0);
                double heightMm = data.HeightMm ?? data.DepthMm ?? 30.0;

                _log.Info($"Creating cylinder: diameter={diameterMm} mm, height={heightMm} mm");

                var builder = new ExtrudedCylinderBuilder(_sw, _log);
                return builder.CreateCylinderOnTopPlane(
                    model,
                    diameterMm,
                    heightMm,
                    data.DraftAngleDeg,
                    data.DraftOutward,
                    data.FlipDirection
                );
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
                int count = data.Count ?? data.Pattern?.Count ?? 4;
                double diameterMm = data.DiameterMm ?? (data.RadiusMm.HasValue ? data.RadiusMm.Value * 2.0 : 5.0);
                double? angleDeg = data.Pattern?.AngleDeg ?? data.AngleDeg;
                double? patternRadiusMm = data.Pattern?.RadiusMm;
                double? depthMm = data.DepthMm ?? data.HeightMm;

                double plateSizeMm;
                if (data.WidthMm.HasValue || data.LengthMm.HasValue)
                {
                    if (data.WidthMm.HasValue && data.LengthMm.HasValue)
                        plateSizeMm = Math.Min(data.WidthMm.Value, data.LengthMm.Value);
                    else
                        plateSizeMm = data.WidthMm ?? data.LengthMm ?? 80.0;
                }
                else
                {
                    plateSizeMm = 80.0;
                    if (patternRadiusMm.HasValue)
                    {
                        double holeRadiusMm = diameterMm / 2.0;
                        double minPlateMm = 2.0 * (patternRadiusMm.Value + holeRadiusMm);
                        double paddedPlateMm = minPlateMm * 1.1;
                        if (paddedPlateMm > plateSizeMm)
                            plateSizeMm = paddedPlateMm;
                    }
                }

                _log.Info($"Creating {count} holes with diameter={diameterMm} mm");

                var builder = new CircularHolesBuilder(_sw, _log);
                return builder.CreatePatternOnTopFace(
                    model,
                    count,
                    diameterMm,
                    angleDeg,
                    patternRadiusMm,
                    plateSizeMm,
                    depthMm,
                    data.DraftAngleDeg,
                    data.DraftOutward,
                    data.FlipDirection
                );
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
                double? radiusMm = data.RadiusMm ?? data.DiameterMm;
                if (!radiusMm.HasValue || radiusMm.Value <= 0)
                {
                    _log.Error("Fillet requires a positive radius in mm (e.g., 'fillet radius 2 mm')");
                    return false;
                }

                string target = data.FilletTarget?.Trim().ToLowerInvariant();
                bool allEdges = target == "all_edges";

                _log.Info($"Creating fillet: radius={radiusMm.Value} mm, target={(allEdges ? "all edges" : "recent feature")}");

                var builder = new FilletBuilder(_sw, _log);

                if (allEdges)
                {
                    return builder.ApplyFilletToAllSharpEdges(model, radiusMm.Value);
                }
                else
                {
                    return builder.ApplyFilletToRecentEdges(model, radiusMm.Value);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Fillet execution failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Execute chamfer creation.
        /// </summary>
        private bool ExecuteChamfer(IModelDoc2 model, Parameters data)
        {
            try
            {
                double? distanceMm = data.ChamferDistanceMm;
                if (!distanceMm.HasValue || distanceMm.Value <= 0)
                {
                    _log.Error("Chamfer requires a positive distance in mm (e.g., 'chamfer 2 mm at 45 deg')");
                    return false;
                }

                double? angleDeg = data.AngleDeg;
                if (angleDeg.HasValue && (angleDeg.Value <= 0 || angleDeg.Value >= 180))
                {
                    _log.Error($"Invalid chamfer angle: {angleDeg.Value} deg (must be between 0 and 180)");
                    return false;
                }

                string target = data.ChamferTarget?.Trim().ToLowerInvariant();
                bool allEdges = target == "all_edges";

                _log.Info($"Creating chamfer: distance={distanceMm.Value} mm, target={(allEdges ? "all edges" : "recent feature")}");
                if (angleDeg.HasValue)
                    _log.Info($"  Angle: {angleDeg.Value} deg");

                var builder = new ChamferBuilder(_sw, _log);

                if (allEdges)
                {
                    return builder.ApplyChamferToAllSharpEdges(model, distanceMm.Value, angleDeg);
                }
                else
                {
                    return builder.ApplyChamferToRecentEdges(model, distanceMm.Value, angleDeg);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Chamfer execution failed: {ex.Message}");
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

            if (data.WidthMm.HasValue && data.WidthMm.Value <= 0)
            {
                _log.Error($"Invalid width: {data.WidthMm.Value} mm (must be > 0)");
                return false;
            }

            if (data.LengthMm.HasValue && data.LengthMm.Value <= 0)
            {
                _log.Error($"Invalid length: {data.LengthMm.Value} mm (must be > 0)");
                return false;
            }

            if (data.DepthMm.HasValue && data.DepthMm.Value <= 0)
            {
                _log.Error($"Invalid depth: {data.DepthMm.Value} mm (must be > 0)");
                return false;
            }

            if (data.RadiusMm.HasValue && data.RadiusMm.Value <= 0)
            {
                _log.Error($"Invalid radius: {data.RadiusMm.Value} mm (must be > 0)");
                return false;
            }

            if (data.ChamferDistanceMm.HasValue && data.ChamferDistanceMm.Value <= 0)
            {
                _log.Error($"Invalid chamfer distance: {data.ChamferDistanceMm.Value} mm (must be > 0)");
                return false;
            }

            if (data.DraftAngleDeg.HasValue && data.DraftAngleDeg.Value <= 0)
            {
                _log.Error($"Invalid draft angle: {data.DraftAngleDeg.Value} deg (must be > 0)");
                return false;
            }

            // Validate count (if present)
            if (data.Count.HasValue && data.Count.Value < 0)
            {
                _log.Error($"Invalid count: {data.Count.Value} (must be >= 0)");
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
