using System;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Models;
using DocuFiller.Services.Interfaces;
using DocuFiller.Utils;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Services
{
    /// <summary>
    /// 内容控件处理器
    /// </summary>
    public class ContentControlProcessor
    {
        private readonly ILogger<ContentControlProcessor> _logger;
        private readonly CommentManager _commentManager;
        private readonly ISafeTextReplacer _safeTextReplacer;

        public ContentControlProcessor(ILogger<ContentControlProcessor> logger, CommentManager commentManager, ISafeTextReplacer safeTextReplacer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commentManager = commentManager ?? throw new ArgumentNullException(nameof(commentManager));
            _safeTextReplacer = safeTextReplacer ?? throw new ArgumentNullException(nameof(safeTextReplacer));
        }

        /// <summary>
        /// 处理单个内容控件
        /// </summary>
        public void ProcessContentControl(SdtElement control, Dictionary<string, object> data, WordprocessingDocument document, ContentControlLocation location = ContentControlLocation.Body)
        {
            try
            {
                // 获取控件标签
                string? tag = OpenXmlHelper.GetControlTag(control);
                if (string.IsNullOrWhiteSpace(tag))
                {
                    _logger.LogWarning("内容控件标签为空，跳过处理");
                    return;
                }

                // 验证数据是否存在
                if (!data.ContainsKey(tag))
                {
                    _logger.LogWarning($"数据中未找到标签 '{tag}' 对应的值，跳过处理");
                    return;
                }

                string value = data[tag]?.ToString() ?? string.Empty;
                _logger.LogDebug($"找到匹配数据: '{tag}' -> '{value}'");

                // 记录旧值
                string oldValue = OpenXmlHelper.ExtractExistingText(control);

                // 处理内容替换
                ProcessContentReplacement(control, value);

                // 添加批注(仅正文区域支持,页眉页脚不支持批注)
                if (location == ContentControlLocation.Body)
                {
                    OpenXmlHelper.AddProcessingComment(document, control, tag, value, oldValue, location, _commentManager, _logger);
                }
                else
                {
                    _logger.LogDebug($"跳过批注添加(页眉页脚不支持批注功能),标签: '{tag}', 位置: {location}");
                }

                _logger.LogInformation($"✓ 成功替换内容控件 '{tag}' ({location}) 为 '{value}'");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"处理内容控件时发生异常: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 处理文档中的所有内容控件（包括页眉页脚）
        /// </summary>
        public void ProcessContentControlsInDocument(
            WordprocessingDocument document,
            Dictionary<string, object> data,
            CancellationToken cancellationToken)
        {
            if (document.MainDocumentPart == null)
            {
                _logger.LogError("文档主体部分不存在");
                return;
            }

            _logger.LogInformation("开始处理文档中的所有内容控件");

            // 处理文档主体
            ProcessControlsInPart(
                document.MainDocumentPart.Document,
                data,
                document,
                ContentControlLocation.Body,
                cancellationToken);

            // 处理所有页眉
            foreach (var headerPart in document.MainDocumentPart.HeaderParts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ProcessControlsInHeaderPart(headerPart, data, document, cancellationToken);
            }

            // 处理所有页脚
            foreach (var footerPart in document.MainDocumentPart.FooterParts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ProcessControlsInFooterPart(footerPart, data, document, cancellationToken);
            }

            _logger.LogInformation("文档内容控件处理完成");
        }

        /// <summary>
        /// 处理文档部分中的内容控件
        /// </summary>
        private void ProcessControlsInPart(
            OpenXmlPartRootElement partRoot,
            Dictionary<string, object> data,
            WordprocessingDocument document,
            ContentControlLocation location,
            CancellationToken cancellationToken)
        {
            var allControls = partRoot.Descendants<SdtElement>().ToList();
            var taggedControls = allControls
                .Select(c => new { Control = c, Tag = OpenXmlHelper.GetControlTag(c) })
                .Where(x => !string.IsNullOrWhiteSpace(x.Tag))
                .ToList();

            var contentControls = taggedControls
                .Where(x => !OpenXmlHelper.HasAncestorWithSameTag(x.Control, x.Tag!))
                .Select(x => x.Control)
                .ToList();

            _logger.LogDebug($"在 {location} 中找到 {allControls.Count} 个内容控件，需处理 {contentControls.Count} 个带标签控件");

            foreach (var control in contentControls)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ProcessContentControl(control, data, document, location);
            }
        }

        /// <summary>
        /// 处理页眉中的内容控件
        /// </summary>
        private void ProcessControlsInHeaderPart(
            HeaderPart headerPart,
            Dictionary<string, object> data,
            WordprocessingDocument document,
            CancellationToken cancellationToken)
        {
            if (headerPart.Header == null)
            {
                _logger.LogDebug("页眉部分为空，跳过处理");
                return;
            }

            _logger.LogDebug("开始处理页眉中的内容控件");
            ProcessControlsInPart(
                headerPart.Header,
                data,
                document,
                ContentControlLocation.Header,
                cancellationToken);
        }

        /// <summary>
        /// 处理页脚中的内容控件
        /// </summary>
        private void ProcessControlsInFooterPart(
            FooterPart footerPart,
            Dictionary<string, object> data,
            WordprocessingDocument document,
            CancellationToken cancellationToken)
        {
            if (footerPart.Footer == null)
            {
                _logger.LogDebug("页脚部分为空，跳过处理");
                return;
            }

            _logger.LogDebug("开始处理页脚中的内容控件");
            ProcessControlsInPart(
                footerPart.Footer,
                data,
                document,
                ContentControlLocation.Footer,
                cancellationToken);
        }

        /// <summary>
        /// 处理内容替换
        /// </summary>
        private void ProcessContentReplacement(SdtElement control, string value)
        {
            // 使用安全文本替换服务
            _safeTextReplacer.ReplaceTextInControl(control, value);

            _logger.LogDebug($"使用安全文本替换服务替换内容: '{value}'");
        }

    }
}
