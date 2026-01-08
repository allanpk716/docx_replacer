using System;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Services
{
    /// <summary>
    /// 批注管理器
    /// </summary>
    public class CommentManager
    {
        private readonly ILogger<CommentManager> _logger;

        public CommentManager(ILogger<CommentManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 为Run元素添加批注
        /// </summary>
        public void AddCommentToElement(WordprocessingDocument document, Run targetRun, string commentText, string author, string tag)
        {
            try
            {
                _logger.LogDebug($"开始为Run元素添加批注，标签: '{tag}'");

                // 验证文档主体部分
                if (document.MainDocumentPart == null)
                {
                    _logger.LogError("文档主体部分为空，无法添加批注");
                    return;
                }

                // 获取或创建批注部分
                WordprocessingCommentsPart? commentsPart = GetOrCreateCommentsPart(document.MainDocumentPart);

                // 生成唯一ID
                string commentId = GenerateCommentId(commentsPart);

                // 创建批注内容
                Comment comment = CreateComment(commentId, commentText, author);

                // 保存批注
                SaveComment(commentsPart, comment);

                // 在文档中添加批注引用
                AddCommentReference(targetRun, commentId);

                _logger.LogInformation($"✓ 成功为Run元素添加批注，标签: '{tag}'，ID: {commentId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"为Run元素添加批注时发生异常，标签: '{tag}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 为多个连续的Run元素添加批注范围
        /// </summary>
        public void AddCommentToRunRange(WordprocessingDocument document, System.Collections.Generic.List<Run> targetRuns, string commentText, string author, string tag)
        {
            try
            {
                if (targetRuns == null || targetRuns.Count == 0)
                {
                    _logger.LogWarning($"目标Run列表为空，无法添加批注，标签: '{tag}'");
                    return;
                }

                _logger.LogDebug($"开始为 {targetRuns.Count} 个Run元素添加批注范围，标签: '{tag}'");

                // 验证文档主体部分
                if (document.MainDocumentPart == null)
                {
                    _logger.LogError("文档主体部分为空，无法添加批注");
                    return;
                }

                // 获取或创建批注部分
                WordprocessingCommentsPart? commentsPart = GetOrCreateCommentsPart(document.MainDocumentPart);

                // 生成唯一ID
                string commentId = GenerateCommentId(commentsPart);

                // 创建批注内容
                Comment comment = CreateComment(commentId, commentText, author);

                // 保存批注
                SaveComment(commentsPart, comment);

                // 在文档中添加批注范围引用
                AddCommentRangeReference(targetRuns, commentId);

                _logger.LogInformation($"✓ 成功为 {targetRuns.Count} 个Run元素添加批注范围，标签: '{tag}'，ID: {commentId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"为Run范围添加批注时发生异常，标签: '{tag}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取或创建批注部分
        /// </summary>
        private WordprocessingCommentsPart GetOrCreateCommentsPart(MainDocumentPart mainDocumentPart)
        {
            WordprocessingCommentsPart? commentsPart = mainDocumentPart.WordprocessingCommentsPart;

            if (commentsPart == null)
            {
                _logger.LogDebug("创建新的批注部分");
                commentsPart = mainDocumentPart.AddNewPart<WordprocessingCommentsPart>();
                commentsPart.Comments = new Comments();
            }

            return commentsPart;
        }

        /// <summary>
        /// 生成批注ID
        /// </summary>
        private string GenerateCommentId(WordprocessingCommentsPart commentsPart)
        {
            string id = "1";

            if (commentsPart.Comments != null && commentsPart.Comments.HasChildren)
            {
                // 找到当前最大的ID并加1
                int maxId = commentsPart.Comments.Descendants<Comment>()
                    .Select(c => int.TryParse(c.Id?.Value, out int commentId) ? commentId : 0)
                    .DefaultIfEmpty(0)
                    .Max();
                id = (maxId + 1).ToString();
            }

            _logger.LogDebug($"生成批注ID: {id}");
            return id;
        }

        /// <summary>
        /// 创建批注
        /// </summary>
        private Comment CreateComment(string id, string commentText, string author)
        {
            Paragraph paragraph = new(new Run(new Text(commentText)));

            Comment comment = new()
            {
                Id = id,
                Author = author,
                Date = DateTime.Now,
                Initials = author.Length >= 2 ? author[..2] : author
            };

            comment.Append(paragraph);
            return comment;
        }

        /// <summary>
        /// 保存批注
        /// </summary>
        private void SaveComment(WordprocessingCommentsPart commentsPart, Comment comment)
        {
            commentsPart.Comments?.Append(comment);
            commentsPart.Comments?.Save();
            _logger.LogDebug($"批注已保存到comments.xml，ID: {comment.Id}");
        }

        /// <summary>
        /// 添加批注引用
        /// </summary>
        private void AddCommentReference(Run targetRun, string commentId)
        {
            // 在被批注的元素前后插入范围标记
            targetRun.InsertBeforeSelf(new CommentRangeStart() { Id = commentId });
            targetRun.InsertAfterSelf(new CommentRangeEnd() { Id = commentId });

            // 在被批注的元素中添加引用标记
            targetRun.Append(new CommentReference() { Id = commentId });
        }

        /// <summary>
        /// 为Run范围添加批注引用
        /// </summary>
        private void AddCommentRangeReference(System.Collections.Generic.List<Run> targetRuns, string commentId)
        {
            if (targetRuns == null || targetRuns.Count == 0)
                return;

            Run firstRun = targetRuns[0];
            Run lastRun = targetRuns[targetRuns.Count - 1];

            // 在第一个Run之前插入批注范围开始标记
            firstRun.InsertBeforeSelf(new CommentRangeStart() { Id = commentId });

            // 在最后一个Run之后插入批注范围结束标记
            lastRun.InsertAfterSelf(new CommentRangeEnd() { Id = commentId });

            // 在最后一个Run中添加批注引用
            lastRun.Append(new CommentReference() { Id = commentId });
        }
    }
}