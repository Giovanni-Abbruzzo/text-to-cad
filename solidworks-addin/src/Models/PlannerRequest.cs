using System.Collections.Generic;
using Newtonsoft.Json;

namespace TextToCad.SolidWorksAddin.Models
{
    public class PlannerRequest
    {
        [JsonProperty("instruction")]
        public string Instruction { get; set; }

        [JsonProperty("state_id")]
        public string StateId { get; set; }

        [JsonProperty("answers")]
        public Dictionary<string, object> Answers { get; set; }

        [JsonProperty("use_ai")]
        public bool UseAI { get; set; }
    }
}
