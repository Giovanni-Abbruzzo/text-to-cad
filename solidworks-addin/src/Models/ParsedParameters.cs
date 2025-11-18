using Newtonsoft.Json;
using System.Collections.Generic;

namespace TextToCad.SolidWorksAddin.Models
{
    /// <summary>
    /// Pattern information for CAD operations
    /// </summary>
    public class PatternInfo
    {
        [JsonProperty("type")]
        public string Type { get; set; } // "circular" or "linear"

        [JsonProperty("count")]
        public int? Count { get; set; }

        [JsonProperty("angle_deg")]
        public double? AngleDeg { get; set; }
    }

    /// <summary>
    /// Parameters extracted from the instruction
    /// </summary>
    public class Parameters
    {
        [JsonProperty("count")]
        public int? Count { get; set; }

        [JsonProperty("diameter_mm")]
        public double? DiameterMm { get; set; }

        [JsonProperty("height_mm")]
        public double? HeightMm { get; set; }

        [JsonProperty("width_mm")]
        public double? WidthMm { get; set; }

        [JsonProperty("radius_mm")]
        public double? RadiusMm { get; set; }

        [JsonProperty("angle_deg")]
        public double? AngleDeg { get; set; }

        [JsonProperty("shape")]
        public string Shape { get; set; }

        [JsonProperty("pattern")]
        public PatternInfo Pattern { get; set; }
    }

    /// <summary>
    /// Parsed parameters from the backend.
    /// Matches the FastAPI ParsedParameters schema.
    /// </summary>
    public class ParsedParameters
    {
        /// <summary>
        /// CAD action to perform (extrude, create_hole, fillet, pattern, create_feature)
        /// </summary>
        [JsonProperty("action")]
        public string Action { get; set; }

        /// <summary>
        /// Extracted parameters (all fields always present, null if not detected)
        /// </summary>
        [JsonProperty("parameters")]
        public Parameters ParametersData { get; set; }

        /// <summary>
        /// Get a human-readable description of the action
        /// </summary>
        public string GetActionDescription()
        {
            switch (Action?.ToLower())
            {
                case "extrude":
                    return "Extrude Feature";
                case "create_hole":
                    return "Create Hole";
                case "fillet":
                    return "Apply Fillet";
                case "pattern":
                    return "Create Pattern";
                case "create_feature":
                    return "Create Feature";
                default:
                    return Action ?? "Unknown Action";
            }
        }

        /// <summary>
        /// Get a formatted string of all parameters
        /// </summary>
        public string GetParametersSummary()
        {
            var parts = new List<string>();

            if (ParametersData == null)
                return "No parameters";

            if (ParametersData.Shape != null)
                parts.Add($"Shape: {ParametersData.Shape}");

            if (ParametersData.DiameterMm.HasValue)
                parts.Add($"Diameter: {ParametersData.DiameterMm.Value} mm");

            if (ParametersData.HeightMm.HasValue)
                parts.Add($"Height: {ParametersData.HeightMm.Value} mm");

            if (ParametersData.Count.HasValue)
                parts.Add($"Count: {ParametersData.Count.Value}");

            if (ParametersData.Pattern != null && ParametersData.Pattern.Type != null)
            {
                parts.Add($"Pattern: {ParametersData.Pattern.Type}");
                if (ParametersData.Pattern.Count.HasValue)
                    parts.Add($"Pattern Count: {ParametersData.Pattern.Count.Value}");
                if (ParametersData.Pattern.AngleDeg.HasValue)
                    parts.Add($"Angle: {ParametersData.Pattern.AngleDeg.Value}°");
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "No parameters detected";
        }
    }
}
