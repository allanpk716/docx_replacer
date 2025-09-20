using System;

namespace DocuFiller.Models
{
    /// <summary>
    /// 文件信息
    /// </summary>
    public class FileInfo
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// 文件大小文本
        /// </summary>
        public string SizeText
        {
            get
            {
                if (Size < 1024)
                    return $"{Size} B";
                else if (Size < 1024 * 1024)
                    return $"{Size / 1024.0:F1} KB";
                else if (Size < 1024 * 1024 * 1024)
                    return $"{Size / (1024.0 * 1024.0):F1} MB";
                else
                    return $"{Size / (1024.0 * 1024.0 * 1024.0):F1} GB";
            }
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// 文件扩展名
        /// </summary>
        public string Extension { get; set; } = string.Empty;

        /// <summary>
        /// 是否只读
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// 相对于基础目录的路径
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;

        /// <summary>
        /// 文件所在目录路径
        /// </summary>
        public string DirectoryPath { get; set; } = string.Empty;

        /// <summary>
        /// 文件的相对目录路径
        /// </summary>
        public string RelativeDirectoryPath { get; set; } = string.Empty;

        /// <summary>
        /// 是否为docx文件
        /// </summary>
        public bool IsDocxFile => Extension.Equals(".docx", StringComparison.OrdinalIgnoreCase);
    }
}