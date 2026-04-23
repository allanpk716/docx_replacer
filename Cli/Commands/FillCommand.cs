using System.IO;
using DocuFiller.Models;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Cli.Commands;

/// <summary>
/// fill 子命令处理器：使用 Excel 数据批量填充 Word 模板，输出 JSONL 格式结果。
/// </summary>
public class FillCommand : ICliCommand
{
    private readonly IDocumentProcessor _documentProcessor;
    private readonly IExcelDataParser _excelDataParser;
    private readonly ILogger<FillCommand> _logger;

    public FillCommand(
        IDocumentProcessor documentProcessor,
        IExcelDataParser excelDataParser,
        ILogger<FillCommand> logger)
    {
        _documentProcessor = documentProcessor;
        _excelDataParser = excelDataParser;
        _logger = logger;
    }

    public string CommandName => "fill";

    public async Task<int> ExecuteAsync(Dictionary<string, string> options)
    {
        _logger.LogDebug("fill 子命令开始执行");

        // 验证必需参数
        if (!options.TryGetValue("template", out var templatePath) || string.IsNullOrWhiteSpace(templatePath))
        {
            JsonlOutput.WriteError("缺少必需参数: --template <path>", "MISSING_ARGUMENT");
            return 1;
        }

        if (!options.TryGetValue("data", out var dataPath) || string.IsNullOrWhiteSpace(dataPath))
        {
            JsonlOutput.WriteError("缺少必需参数: --data <xlsx>", "MISSING_ARGUMENT");
            return 1;
        }

        if (!options.TryGetValue("output", out var outputDir) || string.IsNullOrWhiteSpace(outputDir))
        {
            JsonlOutput.WriteError("缺少必需参数: --output <dir>", "MISSING_ARGUMENT");
            return 1;
        }

        // 验证文件存在性
        if (!File.Exists(templatePath))
        {
            JsonlOutput.WriteError($"模板文件不存在: {templatePath}", "FILE_NOT_FOUND");
            return 1;
        }

        if (!File.Exists(dataPath))
        {
            JsonlOutput.WriteError($"数据文件不存在: {dataPath}", "FILE_NOT_FOUND");
            return 1;
        }

        // 创建输出目录（如果不存在）
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
            _logger.LogDebug("创建输出目录: {OutputDir}", outputDir);
        }

        // 解析 overwrite 标志
        bool overwrite = options.TryGetValue("overwrite", out var overwriteVal)
                         && overwriteVal.Equals("true", StringComparison.OrdinalIgnoreCase);

        // 验证 Excel 数据文件
        var excelValidation = await _excelDataParser.ValidateExcelFileAsync(dataPath);
        if (!excelValidation.IsValid)
        {
            string errorMsg = excelValidation.Errors.Count > 0
                ? string.Join("; ", excelValidation.Errors)
                : "Excel 数据文件验证失败";
            JsonlOutput.WriteError(errorMsg, "FILL_ERROR");
            return 1;
        }

        // 订阅进度事件
        _documentProcessor.ProgressUpdated += OnProgress;

        try
        {
            // 构建处理请求
            var request = new ProcessRequest
            {
                TemplateFilePath = templatePath,
                DataFilePath = dataPath,
                OutputDirectory = outputDir,
                OverwriteExisting = overwrite,
            };

            _logger.LogDebug("开始文档填充: Template={Template}, Data={Data}, Output={Output}",
                templatePath, dataPath, outputDir);

            // 执行文档填充
            var result = await _documentProcessor.ProcessDocumentsAsync(request);

            // 输出每个生成文件的结果
            foreach (var filePath in result.GeneratedFiles)
            {
                JsonlOutput.WriteResult("result", new { file = filePath });
            }

            // 输出失败文件信息
            foreach (var failedFile in result.FailedFiles)
            {
                JsonlOutput.WriteError($"文件处理失败: {failedFile}", "FILL_ERROR");
            }

            // 输出汇总
            JsonlOutput.WriteSummary(new
            {
                total = result.TotalRecords,
                success = result.SuccessfulRecords,
                failed = result.FailedRecords,
                duration = result.Duration.ToString(@"mm\:ss\.fff"),
            });

            _logger.LogDebug("fill 子命令完成: 成功 {Success}/{Total}",
                result.SuccessfulRecords, result.TotalRecords);

            return result.IsSuccess ? 0 : 1;
        }
        finally
        {
            _documentProcessor.ProgressUpdated -= OnProgress;
        }
    }

    private void OnProgress(object? sender, ProgressEventArgs e)
    {
        JsonlOutput.WriteResult("progress", new
        {
            current = e.CurrentIndex,
            total = e.TotalCount,
            percent = e.ProgressPercentage,
            message = e.StatusMessage,
        });
    }
}
