using DocuFiller.Services.Interfaces;

namespace DocuFiller.Services;

public sealed class NullTelemetryService : ITelemetryService
{
    public void TrackEvent(string eventName, Dictionary<string, object>? properties = null) { }
    public Task FlushAsync(TimeSpan? timeout = null) => Task.CompletedTask;
    public void Dispose() { }
}
