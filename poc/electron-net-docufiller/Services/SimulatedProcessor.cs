namespace ElectronNetDocufiller.Services;

using System.Diagnostics;

/// <summary>
/// Interface for simulated document processing with progress reporting.
/// </summary>
public interface IProcessingService
{
    /// <summary>
    /// Simulates processing a document file, reporting progress via callback.
    /// </summary>
    Task<ProcessingResult> ProcessFileAsync(string filePath, IProgress<int>? progress = null, CancellationToken ct = default);
}

/// <summary>
/// Result of simulated document processing.
/// </summary>
public record ProcessingResult(
    string FilePath,
    string FileName,
    long FileSizeBytes,
    TimeSpan Duration,
    int TotalSteps,
    bool Success,
    string? ErrorMessage = null
);

/// <summary>
/// Mock processor that simulates document processing with progress updates.
/// Simulates the core DocuFiller workflow: read file → parse → fill template → save.
/// </summary>
public class SimulatedProcessor : IProcessingService
{
    private readonly ILogger<SimulatedProcessor> _logger;

    public SimulatedProcessor(ILogger<SimulatedProcessor> logger)
    {
        _logger = logger;
    }

    public async Task<ProcessingResult> ProcessFileAsync(string filePath, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("[SimulatedProcessor] Starting processing: {FilePath}", filePath);

        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("[SimulatedProcessor] File not found: {FilePath}", filePath);
                return new ProcessingResult(filePath, Path.GetFileName(filePath), 0, sw.Elapsed, 0, false, "File not found");
            }

            var fileInfo = new FileInfo(filePath);
            var fileSize = fileInfo.Length;
            var fileName = fileInfo.Name;

            // Simulate 5-step processing pipeline (matching DocuFiller's real flow):
            // Step 1: Read file (0-20%)
            // Step 2: Parse document (20-40%)
            // Step 3: Fill template (40-60%)
            // Step 4: Validate output (60-80%)
            // Step 5: Save result (80-100%)
            var steps = new[] { "读取文件", "解析文档", "填充模板", "验证输出", "保存结果" };

            for (int i = 0; i < steps.Length; i++)
            {
                ct.ThrowIfCancellationRequested();

                var progressPercent = (i + 1) * 100 / steps.Length;
                _logger.LogInformation(
                    "[SimulatedProcessor] Step {Step}/{Total}: {StepName} ({Progress}%) — {FileName}",
                    i + 1, steps.Length, steps[i], progressPercent, fileName);

                progress?.Report(progressPercent);

                // Simulate work — larger files take longer
                var delayMs = Math.Max(200, Math.Min(800, (int)(fileSize / 1024)));
                await Task.Delay(delayMs, ct);
            }

            sw.Stop();
            _logger.LogInformation(
                "[SimulatedProcessor] Completed processing: {FileName} in {Duration}ms",
                fileName, sw.ElapsedMilliseconds);

            return new ProcessingResult(filePath, fileName, fileSize, sw.Elapsed, steps.Length, true);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            _logger.LogWarning("[SimulatedProcessor] Processing cancelled: {FilePath}", filePath);
            return new ProcessingResult(filePath, Path.GetFileName(filePath), 0, sw.Elapsed, 0, false, "Cancelled by user");
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[SimulatedProcessor] Error processing: {FilePath}", filePath);
            return new ProcessingResult(filePath, Path.GetFileName(filePath), 0, sw.Elapsed, 0, false, ex.Message);
        }
    }
}
