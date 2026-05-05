using Microsoft.AspNetCore.Mvc;
using ElectronNET.API;
using ElectronNET.API.Entities;
using ElectronNetDocufiller.Services;

namespace ElectronNetDocufiller.Controllers;

/// <summary>
/// API controller demonstrating Electron.NET native dialog integration
/// and simulated document processing with progress reporting.
/// </summary>
[ApiController]
[Route("api")]
public class ProcessingController : ControllerBase
{
    private readonly IProcessingService _processingService;
    private readonly ILogger<ProcessingController> _logger;

    public ProcessingController(IProcessingService processingService, ILogger<ProcessingController> logger)
    {
        _processingService = processingService;
        _logger = logger;
    }

    /// <summary>
    /// Opens native file dialog using Electron.NET API and returns selected file path.
    /// Demonstrates Electron native integration (not HTML input[type=file]).
    /// </summary>
    [HttpGet("select-file")]
    public async Task<IActionResult> SelectFile()
    {
        _logger.LogInformation("[API] select-file endpoint called");

        if (!HybridSupport.IsElectronActive)
        {
            _logger.LogWarning("[API] Electron not active — returning fallback response");
            return Ok(new { path = (string?)null, message = "Electron not active. Running in browser-only mode." });
        }

        try
        {
            var mainWindow = Electron.WindowManager.BrowserWindows.FirstOrDefault();
            if (mainWindow == null)
            {
                _logger.LogWarning("[API] No Electron window found");
                return Ok(new { path = (string?)null, message = "No Electron window available" });
            }

            var options = new OpenDialogOptions
            {
                Title = "选择要处理的文档",
                Properties = new[] { OpenDialogProperty.openFile },
                Filters = new[]
                {
                    new FileFilter
                    {
                        Name = "文档文件",
                        Extensions = new[] { "docx", "xlsx", "pdf", "txt" }
                    },
                    new FileFilter
                    {
                        Name = "所有文件",
                        Extensions = new[] { "*" }
                    }
                }
            };

            _logger.LogInformation("[API] Opening native file dialog...");
            var filePaths = await Electron.Dialog.ShowOpenDialogAsync(mainWindow, options);
            var filePath = filePaths?.FirstOrDefault();

            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogInformation("[API] User cancelled file selection");
                return Ok(new { path = (string?)null, message = "未选择文件" });
            }

            _logger.LogInformation("[API] File selected: {FilePath}", filePath);
            return Ok(new { path = filePath, message = "文件已选择" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[API] Error opening file dialog");
            return Ok(new { path = (string?)null, message = $"错误: {ex.Message}" });
        }
    }

    /// <summary>
    /// Starts simulated processing and streams progress updates via SSE.
    /// Demonstrates backend-to-frontend progress communication.
    /// </summary>
    [HttpGet("process")]
    public async Task ProcessFile([FromQuery] string filePath, CancellationToken ct)
    {
        _logger.LogInformation("[API] process endpoint called with file: {FilePath}", filePath);

        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        if (string.IsNullOrEmpty(filePath))
        {
            await SendSseEvent(new { type = "error", message = "No file path provided" });
            return;
        }

        var progress = new Progress<int>(async percent =>
        {
            try
            {
                await SendSseEvent(new { type = "progress", percent, filePath });
                _logger.LogInformation("[API] SSE progress: {Percent}%", percent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[API] Failed to send SSE progress event");
            }
        });

        var result = await _processingService.ProcessFileAsync(filePath, progress, ct);

        await SendSseEvent(new
        {
            type = "complete",
            result.FilePath,
            result.FileName,
            result.FileSizeBytes,
            DurationMs = result.Duration.TotalMilliseconds,
            result.TotalSteps,
            result.Success,
            result.ErrorMessage
        });

        _logger.LogInformation("[API] Processing complete: {Success}", result.Success);
    }

    /// <summary>
    /// IPC endpoint demonstrating Electron IPC main-on handler registration.
    /// Called from renderer process via Electron IPC bridge.
    /// </summary>
    [HttpGet("ipc/status")]
    public IActionResult GetIpcStatus()
    {
        return Ok(new
        {
            electronActive = HybridSupport.IsElectronActive,
            timestamp = DateTime.UtcNow,
            version = typeof(Electron).Assembly.GetName().Version?.ToString()
        });
    }

    private async Task SendSseEvent(object data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        var message = $"data: {json}\n\n";
        await Response.WriteAsync(message);
        await Response.Body.FlushAsync();
    }
}
