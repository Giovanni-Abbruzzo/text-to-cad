using System.Collections.Generic;
using Newtonsoft.Json;

namespace TextToCad.SolidWorksAddin.Models
{
    public class ReplayModelInfo
    {
        [JsonProperty("document_title")]
        public string DocumentTitle { get; set; }

        [JsonProperty("document_path")]
        public string DocumentPath { get; set; }

        [JsonProperty("units")]
        public string Units { get; set; }
    }

    public class ReplayResult
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }
    }

    public class ReplayEntry
    {
        [JsonProperty("schema_version")]
        public string SchemaVersion { get; set; }

        [JsonProperty("session_id")]
        public string SessionId { get; set; }

        [JsonProperty("sequence")]
        public int Sequence { get; set; }

        [JsonProperty("timestamp_utc")]
        public string TimestampUtc { get; set; }

        [JsonProperty("instruction")]
        public string Instruction { get; set; }

        [JsonProperty("use_ai")]
        public bool UseAI { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("operation_index")]
        public int OperationIndex { get; set; }

        [JsonProperty("operation_count")]
        public int OperationCount { get; set; }

        [JsonProperty("operation")]
        public ParsedParameters Operation { get; set; }

        [JsonProperty("plan")]
        public List<string> Plan { get; set; }

        [JsonProperty("model")]
        public ReplayModelInfo Model { get; set; }

        [JsonProperty("result")]
        public ReplayResult Result { get; set; }
    }
}
