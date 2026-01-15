namespace DocuFiller.Models.Update
{
    public class DownloadProgress
    {
        public long BytesReceived { get; set; }
        public long TotalBytes { get; set; }
        public int ProgressPercentage { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
