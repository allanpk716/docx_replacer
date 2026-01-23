using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DocuFiller.Models
{
    /// <summary>
    /// 清理文件项模型
    /// </summary>
    public class CleanupFileItem : INotifyPropertyChanged
    {
        private string _filePath = string.Empty;
        private string _fileName = string.Empty;
        private long _fileSize;
        private CleanupFileStatus _status = CleanupFileStatus.Pending;
        private string _statusMessage = "待处理";

        /// <summary>
        /// 文件完整路径
        /// </summary>
        public string FilePath
        {
            get => _filePath;
            set { _filePath = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName
        {
            get => _fileName;
            set { _fileName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize
        {
            get => _fileSize;
            set { _fileSize = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 文件大小显示文本（KB）
        /// </summary>
        public string FileSizeDisplay => _fileSize > 0 ? $"{_fileSize / 1024} KB" : "-";

        /// <summary>
        /// 文件状态
        /// </summary>
        public CleanupFileStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusDisplay)); }
        }

        /// <summary>
        /// 状态显示文本
        /// </summary>
        public string StatusDisplay => _status switch
        {
            CleanupFileStatus.Pending => "待处理",
            CleanupFileStatus.Processing => "处理中...",
            CleanupFileStatus.Success => "处理成功",
            CleanupFileStatus.Failure => "处理失败",
            CleanupFileStatus.Skipped => "无需处理",
            _ => "未知"
        };

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private string _outputPath = string.Empty;
        private InputSourceType _inputType = InputSourceType.SingleFile;

        /// <summary>
        /// 处理后的输出路径（单文件时为文件路径，文件夹时为文件夹路径）
        /// </summary>
        public string OutputPath
        {
            get => _outputPath;
            set { _outputPath = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 输入类型标识
        /// </summary>
        public InputSourceType InputType
        {
            get => _inputType;
            set { _inputType = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 属性更改事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发属性更改通知
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 清理文件状态枚举
    /// </summary>
    public enum CleanupFileStatus
    {
        /// <summary>
        /// 待处理
        /// </summary>
        Pending,

        /// <summary>
        /// 处理中
        /// </summary>
        Processing,

        /// <summary>
        /// 处理成功
        /// </summary>
        Success,

        /// <summary>
        /// 处理失败
        /// </summary>
        Failure,

        /// <summary>
        /// 无需处理（跳过）
        /// </summary>
        Skipped
    }
}
