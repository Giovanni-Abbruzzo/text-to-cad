using Newtonsoft.Json;

namespace TextToCad.SolidWorksAddin.Models
{
    /// <summary>
    /// Request model for sending instructions to the backend API.
    /// Matches the FastAPI InstructionRequest schema.
    /// </summary>
    public class InstructionRequest
    {
        /// <summary>
        /// Natural language CAD instruction (minimum 3 characters)
        /// </summary>
        [JsonProperty("instruction")]
        public string Instruction { get; set; }

        /// <summary>
        /// Whether to use AI parsing (true) or rule-based parsing (false)
        /// </summary>
        [JsonProperty("use_ai")]
        public bool UseAI { get; set; }

        /// <summary>
        /// Create a new instruction request
        /// </summary>
        public InstructionRequest(string instruction, bool useAI = false)
        {
            Instruction = instruction;
            UseAI = useAI;
        }
    }
}
