using System.Collections.Generic;
using Newtonsoft.Json;

namespace TextToCad.SolidWorksAddin.Models
{
    public class PlannerQuestion
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("prompt")]
        public string Prompt { get; set; }
    }

    public class PlannerResponse
    {
        [JsonProperty("schema_version")]
        public string SchemaVersion { get; set; }

        [JsonProperty("instruction")]
        public string Instruction { get; set; }

        [JsonProperty("state_id")]
        public string StateId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("plan")]
        public List<string> Plan { get; set; } = new List<string>();

        [JsonProperty("questions")]
        public List<PlannerQuestion> Questions { get; set; } = new List<PlannerQuestion>();

        [JsonProperty("answers")]
        public Dictionary<string, object> Answers { get; set; } = new Dictionary<string, object>();

        [JsonProperty("operations")]
        public List<ParsedParameters> Operations { get; set; } = new List<ParsedParameters>();

        [JsonProperty("notes")]
        public List<string> Notes { get; set; } = new List<string>();
    }
}
