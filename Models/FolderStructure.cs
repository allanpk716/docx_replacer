using System;
using System.Collections.Generic;

namespace DocuFiller.Models
{
    /// <summary>
    /// 文件夹结构信息
    /// </summary>
    public class FolderStructure
    {
        /// <summary>
        /// 文件夹名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 文件夹完整路径
        /// </summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>
        /// 相对于根目录的路径
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;

        /// <summary>
        /// 文件夹中的docx文件列表
        /// </summary>
        public List<FileInfo> DocxFiles { get; set; } = new List<FileInfo>();

        /// <summary>
        /// 子文件夹列表
        /// </summary>
        public List<FolderStructure> SubFolders { get; set; } = new List<FolderStructure>();

        /// <summary>
        /// 文件夹创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 文件夹修改时间
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// 文件夹中docx文件总数（包括子文件夹）
        /// </summary>
        public int TotalDocxCount
        {
            get
            {
                int count = DocxFiles.Count;
                foreach (var subFolder in SubFolders)
                {
                    count += subFolder.TotalDocxCount;
                }
                return count;
            }
        }

        /// <summary>
        /// 是否为空文件夹（不包含任何docx文件）
        /// </summary>
        public bool IsEmpty => TotalDocxCount == 0;
    }
}