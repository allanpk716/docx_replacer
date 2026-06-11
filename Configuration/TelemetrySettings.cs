namespace DocuFiller.Configuration;

public class TelemetrySettings
{
    public const string SectionName = "Telemetry";

    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 50;
    public int FlushIntervalSeconds { get; set; } = 60;
}
