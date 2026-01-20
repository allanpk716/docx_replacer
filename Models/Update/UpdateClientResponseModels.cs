using System;
using System.Text.Json.Serialization;

namespace DocuFiller.Models.Update
{
    public class UpdateClientCheckResponse
    {
        [JsonPropertyName("hasUpdate")]
        public bool HasUpdate { get; set; }

        [JsonPropertyName("currentVersion")]
        public string CurrentVersion { get; set; } = string.Empty;

        [JsonPropertyName("latestVersion")]
        public string LatestVersion { get; set; } = string.Empty;

        [JsonPropertyName("downloadUrl")]
        public string DownloadUrl { get; set; } = string.Empty;

        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }

        [JsonPropertyName("releaseNotes")]
        public string ReleaseNotes { get; set; } = string.Empty;

        [JsonPropertyName("publishDate")]
        public DateTime PublishDate { get; set; }

        [JsonPropertyName("mandatory")]
        public bool Mandatory { get; set; }
    }

    public class UpdateClientDownloadResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("file")]
        public string File { get; set; } = string.Empty;

        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }

        [JsonPropertyName("decrypted")]
        public bool Decrypted { get; set; }
    }
}
