using System.IO;
using DocuFiller.Models;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Cli.Commands;

/// <summary>
/// cleanup 子命令处理器：清理 Word 文档中的批注和内容控件，输出 JSONL 格式结果。
/// </summary>
public class CleanupCommand : ICliCommand
{
    private readonly IDocumentCleanupService _cleanupService;
    private readonly ILogger<CleanupCommand> _logger;

    public CleanupCommand(
        IDocumentCleanupService cleanupService,
        ILogger<CleanupCommand> logger)
    {
        _cleanupService = cleanupService;
        _logger = logger;
    }

    public string CommandName => "cleanup";

    public async Task<int> ExecuteAsync(Dictionary<string, string> options)
    {
        _logger.LogDebug("cleanup 子命令开始执行");

        // 验证必需参数
        if (!options.TryGetValue("input", out var inputPath) || string.IsNullOrWhiteSpace(inputPath))
        {
            JsonlOutput.WriteError("缺少必需参数: --input <path>", "MISSING_ARGUMENT");
            return 1;
        }

        // 确定输入类型
        bool isDirectory = Directory.Exists(inputPath);
        bool folderFlag = options.TryGetValue("folder", out var folderVal)
                          && folderVal.Equals("true", StringComparison.OrdinalIgnoreCase);
        bool isFolderMode = isDirectory || folderFlag;

        // 验证输入路径存在
        if (!isFolderMode && !File.Exists(inputPath))
        {
            JsonlOutput.WriteError($"输入文件不存在: {inputPath}", "FILE_NOT_FOUND");
            return 1;
        }

        if (isFolderMode && !isDirectory && !Directory.Exists(inputPath))
        {
            JsonlOutput.WriteError($"输入目录不存在: {inputPath}", "FILE_NOT_FOUND");
            return 1;
        }

        // 获取 --output（可选）
        string? outputDir = options.TryGetValue("output", out var od) ? od : null;
        bool hasOutput = !string.IsNullOrWhiteSpace(outputDir);

        try
        {
            CleanupResult result;

            if (hasOutput)
            {
                // 指定输出目录模式
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir!);
                    _logger.LogDebug("创建输出目录: {OutputDir}", outputDir);
                }

                var fileItem = new CleanupFileItem
                {
                    FilePath = inputPath,
                    FileName = Path.GetFileName(inputPath),
                    InputType = isFolderMode ? InputSourceType.Folder : InputSourceType.SingleFile,
                };

                _logger.LogDebug("开始文档清理（指定输出目录）: Input={Input}, Output={Output}",
                    inputPath, outputDir);

                result = await _cleanupService.CleanupAsync(fileItem, outputDir!);
            }
            else
            {
                // 原地清理模式（单文件）
                if (isFolderMode)
                {
                    // 文件夹原地清理：使用 CleanupFileItem
                    var fileItem = new CleanupFileItem
                    {
                        FilePath = inputPath,
                        FileName = Path.GetFileName(inputPath),
                        InputType = InputSourceType.Folder,
                    };

                    _logger.LogDebug("开始文档清理（文件夹原地）: Input={Input}", inputPath);

                    result = await _cleanupService.CleanupAsync(fileItem);
                }
                else
                {
                    _logger.LogDebug("开始文档清理（单文件原地）: Input={Input}", inputPath);

                    result = await _cleanupService.CleanupAsync(inputPath);
                }
            }

            if (result.Success)
            {
                // 输出清理结果详情
                JsonlOutput.WriteResult("result", new
                {
                    commentsRemoved = result.CommentsRemoved,
                    controlsUnwrapped = result.ControlsUnwrapped,
                    outputPath = result.OutputFilePath ?? result.OutputFolderPath,
                });

                // 输出汇总
                JsonlOutput.WriteSummary(new
                {
                    success = true,
                    commentsRemoved = result.CommentsRemoved,
                    controlsUnwrapped = result.ControlsUnwrapped,
                    message = result.Message,
                });

                _logger.LogDebug("cleanup 子命令完成: 批注 {CommentsRemoved}, 控件 {ControlsUnwrapped}",
                    result.CommentsRemoved, result.ControlsUnwrapped);

                return 0;
            }
            else
            {
                JsonlOutput.WriteError($"清理失败: {result.Message}", "CLEANUP_ERROR");
                return 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理过程发生异常");
            JsonlOutput.WriteError($"清理失败: {ex.Message}", "CLEANUP_ERROR");
            return 1;
        }
    }
}
