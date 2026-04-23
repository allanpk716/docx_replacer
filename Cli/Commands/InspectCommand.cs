using System.IO;
using DocuFiller.Services.Interfaces;

namespace DocuFiller.Cli.Commands;

/// <summary>
/// inspect 子命令处理器：检查模板文件中的内容控件并输出 JSONL 格式结果。
/// </summary>
public class InspectCommand : ICliCommand
{
    private readonly IDocumentProcessor _documentProcessor;

    public InspectCommand(IDocumentProcessor documentProcessor)
    {
        _documentProcessor = documentProcessor;
    }

    public string CommandName => "inspect";

    public async Task<int> ExecuteAsync(Dictionary<string, string> options)
    {
        // 验证 --template 参数
        if (!options.TryGetValue("template", out var templatePath) || string.IsNullOrWhiteSpace(templatePath))
        {
            JsonlOutput.WriteError("缺少必需参数: --template <path>", "MISSING_ARGUMENT");
            return 1;
        }

        // 验证文件存在
        if (!File.Exists(templatePath))
        {
            JsonlOutput.WriteError($"模板文件不存在: {templatePath}", "FILE_NOT_FOUND");
            return 1;
        }

        try
        {
            // 获取内容控件
            var controls = await _documentProcessor.GetContentControlsAsync(templatePath);

            // 输出每个控件的信息
            foreach (var control in controls)
            {
                JsonlOutput.WriteResult("control", new
                {
                    tag = control.Tag,
                    title = control.Title,
                    contentType = control.Type.ToString(),
                    location = control.Location.ToString(),
                });
            }

            // 输出汇总行
            JsonlOutput.WriteSummary(new
            {
                totalControls = controls.Count,
            });

            return 0;
        }
        catch (Exception ex)
        {
            JsonlOutput.WriteError($"检查模板控件失败: {ex.Message}", "INSPECT_ERROR");
            return 1;
        }
    }
}
