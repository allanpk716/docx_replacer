using System.Collections.Generic;
using System.Linq;
using DocuFiller.Utils;

namespace DocuFiller.Models
{
    /// <summary>
    /// 内容控件数据模型
    /// </summary>
    public class ContentControlData
    {
        /// <summary>
        /// 内容控件的Tag标识
        /// </summary>
        public string Tag { get; set; } = string.Empty;

        /// <summary>
        /// 内容控件的标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 要填充的值
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 内容控件类型
        /// </summary>
        public ContentControlType Type { get; set; } = ContentControlType.Text;

        /// <summary>
        /// 是否为必填项
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// 默认值
        /// </summary>
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>
        /// 验证规则
        /// </summary>
        public string ValidationPattern { get; set; } = string.Empty;

        /// <summary>
        /// 获取实际要填充的值
        /// </summary>
        /// <returns>填充值</returns>
        public string GetFillValue()
        {
            if (!string.IsNullOrWhiteSpace(Value))
                return Value;

            if (!string.IsNullOrWhiteSpace(DefaultValue))
                return DefaultValue;

            return string.Empty;
        }

        /// <summary>
        /// 验证数据是否有效
        /// </summary>
        /// <returns>验证结果</returns>
        public ValidationResult Validate()
        {
            var result = new ValidationResult { IsValid = true };

            // 检查必填项
            if (IsRequired && string.IsNullOrWhiteSpace(GetFillValue()))
            {
                result.AddError($"内容控件 '{Tag}' 是必填项，但未提供值");
                return result;
            }

            // 检查验证规则
            if (!string.IsNullOrWhiteSpace(ValidationPattern) && !string.IsNullOrWhiteSpace(GetFillValue()))
            {
                try
                {
                    var regex = new System.Text.RegularExpressions.Regex(ValidationPattern);
                    if (!regex.IsMatch(GetFillValue()))
                    {
                        result.AddError($"内容控件 '{Tag}' 的值不符合验证规则");
                        return result;
                    }
                }
                catch (System.Exception ex)
                {
                    result.AddError($"内容控件 '{Tag}' 的验证规则无效: {ex.Message}");
                    return result;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// 内容控件类型枚举
    /// </summary>
    public enum ContentControlType
    {
        /// <summary>
        /// 文本
        /// </summary>
        Text,
        /// <summary>
        /// 富文本
        /// </summary>
        RichText,
        /// <summary>
        /// 图片
        /// </summary>
        Picture,
        /// <summary>
        /// 日期
        /// </summary>
        Date,
        /// <summary>
        /// 下拉列表
        /// </summary>
        DropDownList,
        /// <summary>
        /// 组合框
        /// </summary>
        ComboBox,
        /// <summary>
        /// 复选框
        /// </summary>
        CheckBox
    }


}