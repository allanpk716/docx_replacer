using System.Text.Json.Serialization;

namespace DocuFiller.Models.Update
{
    public class DownloadStatus
    {
        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("file")]
        public string File { get; set; } = string.Empty;

        [JsonPropertyName("progress")]
        public DaemonProgressInfo? Progress { get; set; }

        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;
    }
}
