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

        /// <summary>
        /// 将控件 sdtPr/rPr 中定义的 rStyle 应用到内容中的所有 Run，
        /// 并移除旧程序遗留的直接格式覆盖（如标红色），确保控件预期的样式生效。
        /// </summary>
        /// <param name="control">内容控件元素</param>
        /// <param name="logger">日志记录器（可选）</param>
        public static void ApplyControlStyleToRuns(SdtElement control, ILogger? logger = null)
        {
            var sdtPr = control.SdtProperties;
            if (sdtPr == null) return;

            var controlRPr = sdtPr.GetFirstChild<RunProperties>();
            if (controlRPr == null) return;

            // 提取控件级别的 rStyle（字符样式引用，如"样式1 Char"）
            var controlRunStyle = controlRPr.GetFirstChild<RunStyle>();
            if (controlRunStyle?.Val?.Value == null) return;

            string rStyle = controlRunStyle.Val.Value;
            string tag = sdtPr.GetFirstChild<Tag>()?.Val?.Value ?? "unknown";
            logger?.LogInformation($"[ApplyControlStyle] 控件 '{tag}' 检测到 rStyle='{rStyle}'，开始确认控件样式");

            // 找到内容容器中的所有 Run
            var contentContainer = FindContentContainer(control);
            if (contentContainer == null) return;

            var runs = contentContainer.Descendants<Run>()
                .Where(r => GetNearestAncestorSdt(r) == control)
                .ToList();

            foreach (var run in runs)
            {
                var runProps = run.RunProperties;
                if (runProps == null)
                {
                    runProps = new RunProperties();
                    run.InsertAt(runProps, 0);
                }

                var existingStyle = runProps.GetFirstChild<RunStyle>();
                if (existingStyle == null || existingStyle.Val?.Value != rStyle)
                {
                    if (existingStyle != null)
                    {
                        existingStyle.Val = rStyle;
                    }
                    else
                    {
                        runProps.InsertAt(new RunStyle() { Val = rStyle }, 0);
                    }

                    // 移除旧程序遗留的直接格式覆盖
                    RemoveDirectFormatOverrides(runProps);

                    logger?.LogDebug($"[ApplyControlStyle] 已为控件 '{tag}' 的 Run 应用 rStyle='{rStyle}' 并清除直接格式覆盖");
                }
            }
        }

        /// <summary>
        /// 移除 Run 中可能由旧替换程序遗留的直接格式覆盖（如标红色），
        /// 这些格式会阻止控件的 rStyle 样式生效。
        /// </summary>
        private static void RemoveDirectFormatOverrides(RunProperties runProps)
        {
            var color = runProps.GetFirstChild<Color>();
            if (color != null && color.Val?.Value != null && color.Val.Value != "auto")
            {
                color.Remove();
            }
        }

        /// <summary>
        /// 获取最近的祖先 SdtElement
        /// </summary>
        private static SdtElement? GetNearestAncestorSdt(OpenXmlElement element)
        {
            var current = element.Parent;
            while (current != null)
            {
                if (current is SdtElement sdt)
                {
                    return sdt;
                }
                current = current.Parent;
            }
            return null;
        }
    }
}
