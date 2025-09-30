using System.Collections.Generic;
using DocuFiller.Models;
using DocuFiller.Utils;
using System.Collections;

namespace DocuFiller.Services.Interfaces
{
    /// <summary>
    /// 关键词验证服务接口
    /// </summary>
    public interface IKeywordValidationService
    {
        /// <summary>
        /// 验证单个关键词
        /// </summary>
        /// <param name="keyword">关键词项</param>
        /// <returns>验证结果</returns>
        ValidationResult ValidateKeyword(JsonKeywordItem keyword);

        /// <summary>
        /// 验证关键词列表
        /// </summary>
        /// <param name="keywords">关键词列表</param>
        /// <returns>验证结果</returns>
        ValidationResult ValidateKeywordList(IEnumerable<JsonKeywordItem> keywords);

        /// <summary>
        /// 检查关键词是否重复
        /// </summary>
        /// <param name="key">关键词键名</param>
        /// <param name="keywords">关键词列表</param>
        /// <param name="excludeIndex">排除的索引（用于编辑时排除自身）</param>
        /// <returns>是否重复</returns>
        bool IsKeywordDuplicate(string key, List<JsonKeywordItem> keywords, int excludeIndex = -1);

        /// <summary>
        /// 验证关键词键名格式
        /// </summary>
        /// <param name="key">关键词键名</param>
        /// <returns>验证结果</returns>
        ValidationResult ValidateKeyFormat(string key);

        /// <summary>
        /// 验证关键词值
        /// </summary>
        /// <param name="value">关键词值</param>
        /// <returns>验证结果</returns>
        ValidationResult ValidateKeyValue(string value);

        /// <summary>
        /// 验证来源文件名
        /// </summary>
        /// <param name="sourceFile">来源文件名</param>
        /// <returns>验证结果</returns>
        ValidationResult ValidateSourceFile(string sourceFile);

        /// <summary>
        /// 获取关键词建议
        /// </summary>
        /// <param name="partialKey">部分关键词</param>
        /// <param name="existingKeywords">已存在的关键词列表</param>
        /// <returns>建议的关键词列表</returns>
        List<string> GetKeywordSuggestions(string partialKey, List<JsonKeywordItem> existingKeywords);

        /// <summary>
        /// 格式化关键词键名
        /// </summary>
        /// <param name="key">原始键名</param>
        /// <returns>格式化后的键名</returns>
        string FormatKeyName(string key);

        /// <summary>
        /// 检查关键词值是否包含潜在问题
        /// </summary>
        /// <param name="value">关键词值</param>
        /// <returns>检查结果</returns>
        ValidationResult CheckValueIssues(string value);
    }
}