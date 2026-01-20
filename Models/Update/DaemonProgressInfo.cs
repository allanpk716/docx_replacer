using System.Text.Json.Serialization;

namespace DocuFiller.Models.Update
{
    public class DaemonProgressInfo
    {
        [JsonPropertyName("downloaded")]
        public long Downloaded { get; set; }

        [JsonPropertyName("total")]
        public long Total { get; set; }

        [JsonPropertyName("percentage")]
        public double Percentage { get; set; }

        [JsonPropertyName("speed")]
        public long Speed { get; set; }
    }
}
