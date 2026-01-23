using System;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Services
{
    /// <summary>
    /// 批注清理处理器
    /// </summary>
    /// <remarks>
    /// 负责清理 Word 文档中的批注，包括：
    /// 1. 将被批注标记的文本颜色改为黑色
    /// 2. 删除批注标记元素（CommentRangeStart、CommentRangeEnd、CommentReference）
    /// 3. 删除批注内容部分（WordprocessingCommentsPart）
    /// </remarks>
    public class CleanupCommentProcessor
    {
        private readonly ILogger<CleanupCommentProcessor> _logger;

        public CleanupCommentProcessor(ILogger<CleanupCommentProcessor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 处理并清理文档中的所有批注
        /// </summary>
        /// <param name="document">Word 文档对象</param>
        /// <returns>已清理的批注数量</returns>
        public int ProcessComments(WordprocessingDocument document)
        {
            int commentsRemoved = 0;

            if (document.MainDocumentPart?.WordprocessingCommentsPart == null)
            {
                _logger.LogDebug("文档没有批注部分，跳过批注清理");
                return 0;
            }

            // 1. 收集所有批注的 ID
            var commentIds = document.MainDocumentPart.WordprocessingCommentsPart.Comments
                ?.Descendants<Comment>()
                .Select(c => c.Id?.Value)
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList();

            if (commentIds == null || commentIds.Count == 0)
            {
                _logger.LogDebug("文档没有批注，跳过批注清理");
                return 0;
            }

            _logger.LogInformation("找到 {Count} 个批注", commentIds.Count);

            // 2. 对每个批注 ID，找到被批注的 Run 并修改颜色为黑色
            foreach (var commentId in commentIds)
            {
                ChangeCommentedRunsColorToBlack(document, commentId);
            }

            // 3. 删除批注标记元素
            RemoveCommentMarkers(document, commentIds);

            // 4. 删除批注内容部分
            RemoveCommentsPart(document);

            commentsRemoved = commentIds.Count;
            _logger.LogInformation("已清理 {Count} 个批注", commentsRemoved);

            return commentsRemoved;
        }

        /// <summary>
        /// 将被批注的 Run 元素的颜色改为黑色
        /// </summary>
        /// <param name="document">Word 文档对象</param>
        /// <param name="commentId">批注 ID</param>
        private void ChangeCommentedRunsColorToBlack(WordprocessingDocument document, string commentId)
        {
            if (document.MainDocumentPart?.Document == null)
                return;

            // 找到所有批注范围开始标记
            var rangeStarts = document.MainDocumentPart.Document.Descendants<CommentRangeStart>()
                .Where(rs => rs.Id?.Value == commentId)
                .ToList();

            foreach (var rangeStart in rangeStarts)
            {
                // 找到对应的范围结束标记
                var rangeEnd = rangeStart.ElementsAfter().OfType<CommentRangeEnd>()
                    .FirstOrDefault(re => re.Id?.Value == commentId);

                if (rangeEnd == null)
                    continue;

                // 收集两者之间的所有 Run
                var runsInRange = GetRunsBetween(rangeStart, rangeEnd);

                // 将这些 Run 的颜色改为黑色
                foreach (var run in runsInRange)
                {
                    SetRunColorToBlack(run);
                }
            }
        }

        /// <summary>
        /// 获取两个元素之间的所有 Run 元素
        /// </summary>
        /// <param name="start">起始元素</param>
        /// <param name="end">结束元素</param>
        /// <returns>Run 元素列表</returns>
        private System.Collections.Generic.List<Run> GetRunsBetween(OpenXmlElement start, OpenXmlElement end)
        {
            var runs = new System.Collections.Generic.List<Run>();
            var current = start.NextSibling();

            while (current != null && current != end)
            {
                if (current is Run run)
                {
                    runs.Add(run);
                }
                // 递归查找子元素中的 Run
                runs.AddRange(current.Descendants<Run>().ToList());

                current = current.NextSibling();
            }

            return runs;
        }

        /// <summary>
        /// 将 Run 元素的颜色设置为黑色
        /// </summary>
        /// <param name="run">Run 元素</param>
        private void SetRunColorToBlack(Run run)
        {
            var runProperties = run.RunProperties;
            if (runProperties == null)
            {
                runProperties = new RunProperties();
                run.InsertAt(runProperties, 0);
            }

            var color = runProperties.GetFirstChild<Color>();
            if (color == null)
            {
                color = new Color();
                runProperties.AppendChild(color);
            }

            color.Val = "000000"; // 黑色
        }

        /// <summary>
        /// 删除文档中的所有批注标记元素
        /// </summary>
        /// <param name="document">Word 文档对象</param>
        /// <param name="commentIds">批注 ID 列表</param>
        private void RemoveCommentMarkers(WordprocessingDocument document, System.Collections.Generic.List<string?> commentIds)
        {
            if (document.MainDocumentPart?.Document == null)
                return;

            // 删除批注范围开始标记
            var rangeStarts = document.MainDocumentPart.Document.Descendants<CommentRangeStart>()
                .Where(rs => commentIds.Contains(rs.Id?.Value))
                .ToList();
            foreach (var rs in rangeStarts)
            {
                rs.Remove();
            }

            // 删除批注范围结束标记
            var rangeEnds = document.MainDocumentPart.Document.Descendants<CommentRangeEnd>()
                .Where(re => commentIds.Contains(re.Id?.Value))
                .ToList();
            foreach (var re in rangeEnds)
            {
                re.Remove();
            }

            // 删除批注引用标记
            var references = document.MainDocumentPart.Document.Descendants<CommentReference>()
                .Where(cr => commentIds.Contains(cr.Id?.Value))
                .ToList();
            foreach (var cr in references)
            {
                cr.Remove();
            }

            _logger.LogDebug("已删除 {StartCount} 个范围开始标记, {EndCount} 个范围结束标记, {RefCount} 个引用标记",
                rangeStarts.Count, rangeEnds.Count, references.Count);
        }

        /// <summary>
        /// 删除批注内容部分
        /// </summary>
        /// <param name="document">Word 文档对象</param>
        private void RemoveCommentsPart(WordprocessingDocument document)
        {
            if (document.MainDocumentPart?.WordprocessingCommentsPart != null)
            {
                document.MainDocumentPart.DeletePart(document.MainDocumentPart.WordprocessingCommentsPart);
                _logger.LogDebug("已删除批注部分");
            }
        }
    }
}
