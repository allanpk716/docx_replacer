namespace DocuFiller.Services.Interfaces;

public interface ITelemetryService : IDisposable
{
    void TrackEvent(string eventName, Dictionary<string, object>? properties = null);
    Task FlushAsync(TimeSpan? timeout = null);
}
