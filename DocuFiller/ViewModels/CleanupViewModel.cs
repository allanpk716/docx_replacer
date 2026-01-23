using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using DocuFiller.Models;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocuFiller.ViewModels
{
    public class CleanupViewModel : ObservableObject
    {
        private readonly IDocumentCleanupService _cleanupService;
        private readonly ILogger<CleanupViewModel> _logger;
        private bool _isProcessing;
        private string _progressStatus = "等待处理...";
        private int _progressPercent;

        public ObservableCollection<CleanupFileItem> FileItems { get; } = new();

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanStartCleanup)); }
        }

        public string ProgressStatus
        {
            get => _progressStatus;
            set { _progressStatus = value; OnPropertyChanged(); }
        }

        public int ProgressPercent
        {
            get => _progressPercent;
            set { _progressPercent = value; OnPropertyChanged(); }
        }

        public bool CanStartCleanup => FileItems.Count > 0 && !IsProcessing;
        public bool CanClearList => FileItems.Count > 0 && !IsProcessing;

        public CleanupViewModel(IDocumentCleanupService cleanupService, ILogger<CleanupViewModel> logger)
        {
            _cleanupService = cleanupService ?? throw new ArgumentNullException(nameof(cleanupService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _cleanupService.ProgressChanged += OnProgressChanged;
        }

        public void AddFiles(string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                if (!File.Exists(filePath))
                    continue;

                if (!filePath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                    continue;

                // 检查重复
                if (FileItems.Any(f => f.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var fileInfo = new System.IO.FileInfo(filePath);
                var fileItem = new CleanupFileItem
                {
                    FilePath = filePath,
                    FileName = fileInfo.Name,
                    FileSize = fileInfo.Length
                };

                FileItems.Add(fileItem);
            }

            OnPropertyChanged(nameof(CanStartCleanup));
            OnPropertyChanged(nameof(CanClearList));
        }

        public void AddFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return;

            var docxFiles = Directory.GetFiles(folderPath, "*.docx", SearchOption.AllDirectories);
            AddFiles(docxFiles);
        }

        public void RemoveFile(CleanupFileItem item)
        {
            FileItems.Remove(item);
            OnPropertyChanged(nameof(CanStartCleanup));
            OnPropertyChanged(nameof(CanClearList));
        }

        public void ClearList()
        {
            FileItems.Clear();
            ProgressStatus = "等待处理...";
            ProgressPercent = 0;
            OnPropertyChanged(nameof(CanStartCleanup));
            OnPropertyChanged(nameof(CanClearList));
        }

        public async Task StartCleanupAsync()
        {
            IsProcessing = true;
            ProgressStatus = "准备处理...";
            ProgressPercent = 0;

            int successCount = 0;
            int failureCount = 0;
            int skippedCount = 0;

            try
            {
                for (int i = 0; i < FileItems.Count; i++)
                {
                    var fileItem = FileItems[i];
                    fileItem.Status = CleanupFileStatus.Processing;
                    ProgressStatus = $"正在处理: {fileItem.FileName} ({i + 1}/{FileItems.Count})";
                    ProgressPercent = (int)((i / (double)FileItems.Count) * 100);

                    var result = await _cleanupService.CleanupAsync(fileItem);

                    if (result.Success)
                    {
                        if (result.Message.Contains("无需处理"))
                        {
                            fileItem.Status = CleanupFileStatus.Skipped;
                            fileItem.StatusMessage = result.Message;
                            skippedCount++;
                        }
                        else
                        {
                            fileItem.Status = CleanupFileStatus.Success;
                            fileItem.StatusMessage = result.Message;
                            successCount++;
                        }
                    }
                    else
                    {
                        fileItem.Status = CleanupFileStatus.Failure;
                        fileItem.StatusMessage = result.Message;
                        failureCount++;
                    }
                }

                ProgressPercent = 100;
                ProgressStatus = $"处理完成: {successCount} 成功, {failureCount} 失败, {skippedCount} 跳过";

                _logger.LogInformation($"批量清理完成: {successCount} 成功, {failureCount} 失败, {skippedCount} 跳过");

                MessageBox.Show(
                    $"清理完成！\n\n成功: {successCount}\n失败: {failureCount}\n跳过: {skippedCount}",
                    "处理完成",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量清理时发生异常");
                MessageBox.Show($"处理过程中发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void OnProgressChanged(object? sender, CleanupProgressEventArgs e)
        {
            // 可用于更细粒度的进度更新
        }
    }
}