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

        [JsonProperty("radius_mm")]
        public double? RadiusMm { get; set; }

        [JsonProperty("center_x_mm")]
        public double? CenterXmm { get; set; }

        [JsonProperty("center_y_mm")]
        public double? CenterYmm { get; set; }

        [JsonProperty("center_z_mm")]
        public double? CenterZmm { get; set; }
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

        [JsonProperty("length_mm")]
        public double? LengthMm { get; set; }

        [JsonProperty("radius_mm")]
        public double? RadiusMm { get; set; }

        [JsonProperty("center_x_mm")]
        public double? CenterXmm { get; set; }

        [JsonProperty("center_y_mm")]
        public double? CenterYmm { get; set; }

        [JsonProperty("center_z_mm")]
        public double? CenterZmm { get; set; }

        [JsonProperty("axis")]
        public string Axis { get; set; }

        [JsonProperty("use_top_face")]
        public bool? UseTopFace { get; set; }

        [JsonProperty("extrude_midplane")]
        public bool? ExtrudeMidplane { get; set; }

        [JsonProperty("depth_mm")]
        public double? DepthMm { get; set; }

        [JsonProperty("angle_deg")]
        public double? AngleDeg { get; set; }

        [JsonProperty("draft_angle_deg")]
        public double? DraftAngleDeg { get; set; }

        [JsonProperty("draft_outward")]
        public bool? DraftOutward { get; set; }

        [JsonProperty("flip_direction")]
        public bool? FlipDirection { get; set; }

        [JsonProperty("fillet_target")]
        public string FilletTarget { get; set; }

        [JsonProperty("chamfer_distance_mm")]
        public double? ChamferDistanceMm { get; set; }

        [JsonProperty("chamfer_target")]
        public string ChamferTarget { get; set; }

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
        /// CAD action to perform (extrude, create_hole, fillet, chamfer, pattern, create_feature)
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
                case "chamfer":
                    return "Apply Chamfer";
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

            if (ParametersData.LengthMm.HasValue)
                parts.Add($"Length: {ParametersData.LengthMm.Value} mm");

            if (ParametersData.WidthMm.HasValue)
                parts.Add($"Width: {ParametersData.WidthMm.Value} mm");

            if (ParametersData.DepthMm.HasValue)
                parts.Add($"Depth: {ParametersData.DepthMm.Value} mm");

            if (ParametersData.RadiusMm.HasValue)
                parts.Add($"Radius: {ParametersData.RadiusMm.Value} mm");

            if (ParametersData.CenterXmm.HasValue || ParametersData.CenterYmm.HasValue || ParametersData.CenterZmm.HasValue)
            {
                string cx = ParametersData.CenterXmm.HasValue ? ParametersData.CenterXmm.Value.ToString() : "?";
                string cy = ParametersData.CenterYmm.HasValue ? ParametersData.CenterYmm.Value.ToString() : "?";
                string cz = ParametersData.CenterZmm.HasValue ? ParametersData.CenterZmm.Value.ToString() : "?";
                parts.Add($"Center: ({cx}, {cy}, {cz}) mm");
            }

            if (!string.IsNullOrWhiteSpace(ParametersData.Axis))
                parts.Add($"Axis: {ParametersData.Axis}");

            if (ParametersData.UseTopFace.HasValue)
                parts.Add($"Use Top Face: {ParametersData.UseTopFace.Value}");

            if (ParametersData.ExtrudeMidplane.HasValue)
                parts.Add($"Midplane: {ParametersData.ExtrudeMidplane.Value}");

            if (ParametersData.AngleDeg.HasValue)
                parts.Add($"Angle: {ParametersData.AngleDeg.Value} deg");

            if (ParametersData.DraftAngleDeg.HasValue)
                parts.Add($"Draft: {ParametersData.DraftAngleDeg.Value} deg");

            if (ParametersData.DraftOutward.HasValue)
                parts.Add($"Draft Outward: {ParametersData.DraftOutward.Value}");

            if (ParametersData.FlipDirection.HasValue)
                parts.Add($"Flip Direction: {ParametersData.FlipDirection.Value}");

            if (!string.IsNullOrWhiteSpace(ParametersData.FilletTarget))
                parts.Add($"Fillet Target: {ParametersData.FilletTarget}");

            if (ParametersData.ChamferDistanceMm.HasValue)
                parts.Add($"Chamfer Distance: {ParametersData.ChamferDistanceMm.Value} mm");

            if (!string.IsNullOrWhiteSpace(ParametersData.ChamferTarget))
                parts.Add($"Chamfer Target: {ParametersData.ChamferTarget}");

            if (ParametersData.Count.HasValue)
                parts.Add($"Count: {ParametersData.Count.Value}");

            if (ParametersData.Pattern != null && ParametersData.Pattern.Type != null)
            {
                parts.Add($"Pattern: {ParametersData.Pattern.Type}");
                if (ParametersData.Pattern.Count.HasValue)
                    parts.Add($"Pattern Count: {ParametersData.Pattern.Count.Value}");
                if (ParametersData.Pattern.AngleDeg.HasValue)
                    parts.Add($"Angle: {ParametersData.Pattern.AngleDeg.Value} deg");
                if (ParametersData.Pattern.RadiusMm.HasValue)
                    parts.Add($"Pattern Radius: {ParametersData.Pattern.RadiusMm.Value} mm");
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "No parameters detected";
        }
    }
}
