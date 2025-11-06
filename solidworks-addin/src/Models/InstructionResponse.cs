using Newtonsoft.Json;
using System.Collections.Generic;

namespace TextToCad.SolidWorksAddin.Models
{
    /// <summary>
    /// Response model from the backend API.
    /// Matches the FastAPI InstructionResponse schema with schema_version, source, plan, and parsed_parameters.
    /// </summary>
    public class InstructionResponse
    {
        /// <summary>
        /// API schema version for contract stability (currently "1.0")
        /// </summary>
        [JsonProperty("schema_version")]
        public string SchemaVersion { get; set; }

        /// <summary>
        /// Original instruction text echoed back
        /// </summary>
        [JsonProperty("instruction")]
        public string Instruction { get; set; }

        /// <summary>
        /// Parsing source: "ai" (OpenAI) or "rule" (regex-based)
        /// </summary>
        [JsonProperty("source")]
        public string Source { get; set; }

        /// <summary>
        /// Human-readable plan steps describing what will be executed
        /// </summary>
        [JsonProperty("plan")]
        public List<string> Plan { get; set; }

        /// <summary>
        /// Structured CAD parameters extracted from the instruction
        /// </summary>
        [JsonProperty("parsed_parameters")]
        public ParsedParameters ParsedParameters { get; set; }

        /// <summary>
        /// Check if the response used AI parsing
        /// </summary>
        public bool IsAIParsed => Source?.ToLower() == "ai";

        /// <summary>
        /// Get a formatted plan string for display
        /// </summary>
        public string GetFormattedPlan()
        {
            if (Plan == null || Plan.Count == 0)
                return "No plan available";

            return string.Join("\n", Plan.ConvertAll(p => $"• {p}"));
        }

        /// <summary>
        /// Get a summary of the response for logging
        /// </summary>
        public string GetSummary()
        {
            string source = IsAIParsed ? "AI" : "Rule-based";
            string action = ParsedParameters?.Action ?? "unknown";
            int planSteps = Plan?.Count ?? 0;
            
            return $"[{source}] Action: {action}, Plan steps: {planSteps}";
        }
    }
}
