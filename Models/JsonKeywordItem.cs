using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DocuFiller.Utils;

namespace DocuFiller.Models
{
    /// <summary>
    /// JSON关键词项数据模型
    /// </summary>
    public class JsonKeywordItem : INotifyPropertyChanged
    {
        private string _key = string.Empty;
        private string _value = string.Empty;
        private string _sourceFile = string.Empty;
        private DateTime _createdTime = DateTime.Now;
        private DateTime _modifiedTime = DateTime.Now;

        /// <summary>
        /// 关键词键名
        /// </summary>
        public string Key
        {
            get => _key;
            set
            {
                if (_key != value)
                {
                    _key = value;
                    ModifiedTime = DateTime.Now;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 关键词值
        /// </summary>
        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    ModifiedTime = DateTime.Now;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 来源文件
        /// </summary>
        public string SourceFile
        {
            get => _sourceFile;
            set
            {
                if (_sourceFile != value)
                {
                    _sourceFile = value;
                    ModifiedTime = DateTime.Now;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime
        {
            get => _createdTime;
            set
            {
                if (_createdTime != value)
                {
                    _createdTime = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime ModifiedTime
        {
            get => _modifiedTime;
            set
            {
                if (_modifiedTime != value)
                {
                    _modifiedTime = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 是否为多行文本
        /// </summary>
        public bool IsMultiLine => !string.IsNullOrEmpty(Value) && Value.Contains("\n");

        /// <summary>
        /// 值的行数
        /// </summary>
        public int LineCount
        {
            get
            {
                if (string.IsNullOrEmpty(Value))
                    return 0;
                return Value.Split('\n').Length;
            }
        }

        /// <summary>
        /// 验证关键词项是否有效
        /// </summary>
        /// <returns>验证结果</returns>
        public ValidationResult Validate()
        {
            var result = new ValidationResult { IsValid = true };

            // 验证Key
            if (string.IsNullOrWhiteSpace(Key))
            {
                result.AddError("关键词键名不能为空");
            }
            else
            {
                if (Key.Length > 100)
                {
                    result.AddError("关键词键名长度不能超过100个字符");
                }

                // 建议使用#包围的格式
                if (!Key.StartsWith("#") || !Key.EndsWith("#"))
                {
                    result.AddWarning("建议关键词键名使用#包围的格式，如：#关键词#");
                }
            }

            // 验证Value
            if (string.IsNullOrWhiteSpace(Value))
            {
                result.AddError("关键词值不能为空");
            }
            else if (Value.Length > 10000)
            {
                result.AddError("关键词值长度不能超过10000个字符");
            }

            // 验证SourceFile
            if (!string.IsNullOrWhiteSpace(SourceFile))
            {
                if (SourceFile.Length > 200)
                {
                    result.AddError("来源文件名长度不能超过200个字符");
                }

                // 检查文件名是否包含无效字符
                var invalidChars = new char[] { '<', '>', ':', '"', '|', '?', '*' };
                foreach (var invalidChar in invalidChars)
                {
                    if (SourceFile.Contains(invalidChar))
                    {
                        result.AddError($"来源文件名包含无效字符: {invalidChar}");
                        break;
                    }
                }
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// 创建关键词项的副本
        /// </summary>
        /// <returns>关键词项副本</returns>
        public JsonKeywordItem Clone()
        {
            return new JsonKeywordItem
            {
                Key = this.Key,
                Value = this.Value,
                SourceFile = this.SourceFile,
                CreatedTime = this.CreatedTime,
                ModifiedTime = this.ModifiedTime
            };
        }

        /// <summary>
        /// 获取显示用的简短值
        /// </summary>
        /// <param name="maxLength">最大长度</param>
        /// <returns>简短值</returns>
        public string GetShortValue(int maxLength = 50)
        {
            if (string.IsNullOrEmpty(Value))
                return string.Empty;

            if (Value.Length <= maxLength)
                return Value;

            // 如果是多行文本，只显示第一行
            if (IsMultiLine)
            {
                var firstLine = Value.Split('\n')[0];
                if (firstLine.Length <= maxLength)
                    return firstLine + "...";
                return firstLine.Substring(0, maxLength - 3) + "...";
            }

            return Value.Substring(0, maxLength - 3) + "...";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"{Key} = {GetShortValue()}";
        }
    }
}