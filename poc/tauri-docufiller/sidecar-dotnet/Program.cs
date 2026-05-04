// Minimal Kestrel HTTP server for DocuFiller sidecar PoC.
// Listens on http://localhost:5000 and provides a health endpoint.

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on a specific port
builder.WebHost.UseUrls("http://localhost:5000");

var app = builder.Build();

// Health check endpoint — verifies the sidecar is alive.
app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        status = "healthy",
        service = "sidecar-dotnet",
        version = "0.1.0",
        timestamp = DateTime.UtcNow.ToString("O")
    });
});

// Future endpoints:
// POST /api/fill-template — accept template + data, return filled document
// GET  /api/progress/{jobId} — SSE stream for progress updates

app.Logger.LogInformation("Sidecar starting on http://localhost:5000");

app.Run();
