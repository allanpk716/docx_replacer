using System;

namespace DocuFiller.Models.Update
{
    public class UpdateConfig
    {
        public string ServerUrl { get; set; } = "http://192.168.1.100:8080";
        public string Channel { get; set; } = "stable";
        public bool CheckOnStartup { get; set; } = true;
        public bool AutoDownload { get; set; } = true;
        public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(24);
    }
}
