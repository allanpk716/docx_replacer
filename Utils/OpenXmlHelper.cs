using System;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Models;
using DocuFiller.Services;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Utils
{
    /// <summary>
    /// OpenXML 内容控件共享工具方法
    /// 从 DocumentProcessorService 和 ContentControlProcessor 提取的公共逻辑
    /// </summary>
    public static class OpenXmlHelper
    {
        /// <summary>
        /// 获取内容控件标签
        /// </summary>
        public static string? GetControlTag(SdtElement control)
        {
            return control.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
        }

        /// <summary>
        /// 提取现有文本内容（用于批注记录旧值）
        /// </summary>
        public static string ExtractExistingText(SdtElement control)
        {
            List<Text> existingTextElements = control.Descendants<Text>().ToList();
            return existingTextElements.Any() ? string.Join("", existingTextElements.Select(static t => t.Text)) : string.Empty;
        }

        /// <summary>
        /// 查找内容控件的内容容器（SdtContentRun / SdtContentBlock / SdtContentCell）
        /// </summary>
        public static OpenXmlElement? FindContentContainer(SdtElement control)
        {
            return control.Descendants<SdtContentRun>().FirstOrDefault() ??
                   control.Descendants<SdtContentBlock>().FirstOrDefault() as OpenXmlElement ??
                   control.Descendants<SdtContentCell>().FirstOrDefault();
        }

        /// <summary>
        /// 查找内容控件中所有相关的 Run 元素
        /// </summary>
        public static List<Run> FindAllTargetRuns(SdtElement control)
        {
            List<Run> runs = new List<Run>();

            OpenXmlElement? content = FindContentContainer(control);
            if (content != null)
            {
                // 块级控件或单元格控件：获取段落中的所有 Run
                // 行内控件：获取所有 Run
                runs = content.Descendants<Run>().ToList();
            }
            else
            {
                // 直接从控件中查找 Run
                runs = control.Descendants<Run>().ToList();
            }

            return runs;
        }

        /// <summary>
        /// 添加处理批注（记录字段替换信息）
        /// </summary>
        public static void AddProcessingComment(
            WordprocessingDocument document,
            SdtElement control,
            string tag,
            string newValue,
            string oldValue,
            ContentControlLocation location,
            CommentManager commentManager,
            ILogger logger)
        {
            List<Run> targetRuns = FindAllTargetRuns(control);

            if (targetRuns.Count == 0)
            {
                logger.LogWarning("未找到目标Run元素，跳过批注添加，标签: '{Tag}'", tag);
                return;
            }

            string currentTime = DateTime.Now.ToString("yyyy年M月d日 HH:mm:ss");
            string locationText = location switch
            {
                ContentControlLocation.Header => "页眉",
                ContentControlLocation.Footer => "页脚",
                _ => "正文"
            };
            string commentText = $"此字段（{locationText}）已于 {currentTime} 更新。标签：{tag}，旧值：[{oldValue}]，新值：{newValue}";

            if (targetRuns.Count == 1)
            {
                commentManager.AddCommentToElement(document, targetRuns[0], commentText, "DocuFiller系统", tag);
            }
            else
            {
                commentManager.AddCommentToRunRange(document, targetRuns, commentText, "DocuFiller系统", tag);
            }
        }

        /// <summary>
        /// 检查控件是否存在具有相同标签的祖先控件（用于跳过嵌套控件的重复处理）
        /// </summary>
        public static bool HasAncestorWithSameTag(SdtElement control, string tag)
        {
            var normalizedTag = tag.Trim();
            return control.Ancestors<SdtElement>()
                .Select(static c => c.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value)
                .Any(t => !string.IsNullOrWhiteSpace(t) &&
                          string.Equals(t!.Trim(), normalizedTag, StringComparison.OrdinalIgnoreCase));
        }
    }
}
