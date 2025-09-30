using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DocuFiller.Utils;

namespace DocuFiller.Models
{
    /// <summary>
    /// JSON项目数据模型
    /// </summary>
    public class JsonProjectModel : INotifyPropertyChanged
    {
        private string _projectName = string.Empty;
        private ObservableCollection<JsonKeywordItem> _keywords = new ObservableCollection<JsonKeywordItem>();
        private DateTime _lastModified = DateTime.Now;
        private string _filePath = string.Empty;
        private bool _hasUnsavedChanges = false;

        /// <summary>
        /// 项目名称
        /// </summary>
        public string ProjectName
        {
            get => _projectName;
            set
            {
                if (_projectName != value)
                {
                    _projectName = value;
                    HasUnsavedChanges = true;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 关键词列表
        /// </summary>
        public ObservableCollection<JsonKeywordItem> Keywords
        {
            get => _keywords;
            set
            {
                if (_keywords != value)
                {
                    _keywords = value ?? new ObservableCollection<JsonKeywordItem>();
                    HasUnsavedChanges = true;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime LastModified
        {
            get => _lastModified;
            set
            {
                if (_lastModified != value)
                {
                    _lastModified = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath
        {
            get => _filePath;
            set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 是否有未保存的更改
        /// </summary>
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                if (_hasUnsavedChanges != value)
                {
                    _hasUnsavedChanges = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 关键词数量
        /// </summary>
        public int KeywordCount => Keywords?.Count ?? 0;

        /// <summary>
        /// 验证项目数据是否有效
        /// </summary>
        /// <returns>验证结果</returns>
        public ValidationResult Validate()
        {
            var result = new ValidationResult { IsValid = true };

            // 验证项目名称
            if (string.IsNullOrWhiteSpace(ProjectName))
            {
                result.AddError("项目名称不能为空");
            }
            else if (ProjectName.Length > 100)
            {
                result.AddError("项目名称长度不能超过100个字符");
            }

            // 验证关键词列表
            if (Keywords == null || Keywords.Count == 0)
            {
                result.AddError("至少需要包含一个关键词");
            }
            else
            {
                // 验证关键词唯一性
                var keySet = new HashSet<string>();
                for (int i = 0; i < Keywords.Count; i++)
                {
                    var keyword = Keywords[i];
                    var keywordResult = keyword.Validate();
                    
                    if (!keywordResult.IsValid)
                    {
                        foreach (var error in keywordResult.Errors)
                        {
                            result.AddError($"关键词 {i + 1}: {error}");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(keyword.Key))
                    {
                        if (keySet.Contains(keyword.Key))
                        {
                            result.AddError($"关键词 '{keyword.Key}' 重复");
                        }
                        else
                        {
                            keySet.Add(keyword.Key);
                        }
                    }
                }
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// 添加关键词
        /// </summary>
        /// <param name="keyword">关键词项</param>
        public void AddKeyword(JsonKeywordItem keyword)
        {
            if (keyword != null)
            {
                Keywords.Add(keyword);
                HasUnsavedChanges = true;
                OnPropertyChanged(nameof(Keywords));
                OnPropertyChanged(nameof(KeywordCount));
            }
        }

        /// <summary>
        /// 移除关键词
        /// </summary>
        /// <param name="keyword">关键词项</param>
        public bool RemoveKeyword(JsonKeywordItem keyword)
        {
            if (keyword != null && Keywords.Remove(keyword))
            {
                HasUnsavedChanges = true;
                OnPropertyChanged(nameof(Keywords));
                OnPropertyChanged(nameof(KeywordCount));
                return true;
            }
            return false;
        }

        /// <summary>
        /// 标记为已保存
        /// </summary>
        public void MarkAsSaved()
        {
            HasUnsavedChanges = false;
            LastModified = DateTime.Now;
        }

        /// <summary>
        /// 标记为已更改
        /// </summary>
        public void MarkAsChanged()
        {
            HasUnsavedChanges = true;
            LastModified = DateTime.Now;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}