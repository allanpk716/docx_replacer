using System.Collections.Generic;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExcelToWordVerifier.Models;
using A = DocumentFormat.OpenXml.Drawing;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace ExcelToWordVerifier.Services;

/// <summary>
/// 使用 OpenXML SDK 写入 Word 文档并保留格式
/// </summary>
public class WordWriterService : IWordWriter
{
    public void Write(FormattedText formattedText, string outputPath)
    {
        WriteMany(new List<FormattedText> { formattedText }, outputPath);
    }

    public void WriteMany(List<FormattedText> formattedTexts, string outputPath)
    {
        // 创建 Word 文档
        using var wordDocument = WordprocessingDocument.Create(
            outputPath,
            WordprocessingDocumentType.Document);

        // 添加主文档部分
        var mainPart = wordDocument.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        // 为每个格式化文本创建一个段落
        foreach (var formattedText in formattedTexts)
        {
            var paragraph = body.AppendChild(new Paragraph());
            var runProperties = new RunProperties();

            // 为每个文本运行创建 Run 元素
            foreach (var textRun in formattedText.Runs)
            {
                var run = new Run();

                // 设置运行属性（格式）
                var rPr = new RunProperties();

                // 设置粗体
                if (textRun.IsBold)
                {
                    rPr.Bold = new Bold();
                }

                // 设置斜体
                if (textRun.IsItalic)
                {
                    rPr.Italic = new Italic();
                }

                // 设置下划线
                if (textRun.IsUnderline)
                {
                    rPr.Underline = new Underline() { Val = UnderlineValues.Single };
                }

                // 设置上标/下标
                if (textRun.IsSuperscript)
                {
                    rPr.VerticalTextAlignment = new VerticalTextAlignment() { Val = VerticalPositionValues.Superscript };
                }
                else if (textRun.IsSubscript)
                {
                    rPr.VerticalTextAlignment = new VerticalTextAlignment() { Val = VerticalPositionValues.Subscript };
                }

                run.AppendChild(rPr);
                run.AppendChild(new Text(textRun.Text) { Space = SpaceProcessingModeValues.Preserve });
                paragraph.AppendChild(run);
            }
        }

        mainPart.Document.Save();
    }
}
