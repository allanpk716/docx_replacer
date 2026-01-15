using System;

namespace DocuFiller.Models.Update
{
    public class VersionInfo
    {
        public string Version { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileHash { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public DateTime PublishDate { get; set; }
        public bool Mandatory { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
    }
}
