// DocuFiller .NET Sidecar — HTTP API with SSE progress streaming.
// Listens on http://localhost:5000 and provides health check + simulated processing with SSE.

var builder = WebApplication.CreateBuilder(args);
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

// SSE progress streaming endpoint.
// Connects via GET /api/process/stream?filePath=<path>
// Streams progress events as SSE: data: {"step":"...","progress":N,"fileName":"..."}
app.MapGet("/api/process/stream", async (HttpContext context, string? filePath) =>
{
    context.Response.ContentType = "text/event-stream";
    context.Response.Headers["Cache-Control"] = "no-cache";
    context.Response.Headers["Connection"] = "keep-alive";
    context.Response.Headers["Access-Control-Allow-Origin"] = "*";

    var steps = new[]
    {
        new { Name = "Validating file", Weight = 20 },
        new { Name = "Parsing template", Weight = 20 },
        new { Name = "Filling data", Weight = 20 },
        new { Name = "Generating output", Weight = 20 },
        new { Name = "Finalizing", Weight = 20 }
    };

    var totalProgress = 0;
    var fileName = Path.GetFileName(filePath ?? "unknown.docx");

    // Initial event
    await WriteSseEvent(context, new
    {
        step = "Starting",
        progress = 0,
        fileName,
        filePath = filePath ?? ""
    });

    foreach (var step in steps)
    {
        await Task.Delay(600); // Simulate processing time

        totalProgress += step.Weight;
        await WriteSseEvent(context, new
        {
            step = step.Name,
            progress = totalProgress,
            fileName,
            filePath = filePath ?? ""
        });
    }

    // Completion event
    await WriteSseEvent(context, new
    {
        step = "Complete",
        progress = 100,
        fileName,
        filePath = filePath ?? "",
        output = $"{Path.GetFileNameWithoutExtension(fileName)}.filled{Path.GetExtension(fileName)}"
    });
});

app.Logger.LogInformation("DocuFiller Sidecar starting on http://localhost:5000");
app.Run();
return;

async Task WriteSseEvent(HttpContext context, object data)
{
    var json = System.Text.Json.JsonSerializer.Serialize(data);
    await context.Response.WriteAsync($"data: {json}\n\n");
    await context.Response.Body.FlushAsync();
}
