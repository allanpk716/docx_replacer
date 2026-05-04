using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocuFiller.Models;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace DocuFiller.ViewModels
{
    public partial class CleanupViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        private readonly IDocumentCleanupService _cleanupService;
        private readonly ILogger<CleanupViewModel> _logger;

        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private string _progressStatus = "等待处理...";

        [ObservableProperty]
        private int _progressPercent;

        [ObservableProperty]
        private string _outputDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DocuFiller输出",
            "清理");

        public ObservableCollection<CleanupFileItem> FileItems { get; } = new();

        public bool CanStartCleanup => FileItems.Count > 0 && !IsProcessing;
        public bool CanClearList => FileItems.Count > 0 && !IsProcessing;

        public CleanupViewModel(IDocumentCleanupService cleanupService, ILogger<CleanupViewModel> logger)
        {
            _cleanupService = cleanupService ?? throw new ArgumentNullException(nameof(cleanupService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _cleanupService.ProgressChanged += OnProgressChanged;
        }

        partial void OnIsProcessingChanged(bool value)
        {
            OnPropertyChanged(nameof(CanStartCleanup));
            OnPropertyChanged(nameof(CanClearList));
        }

        #region Commands

        [RelayCommand(CanExecute = nameof(CanStartCleanup))]
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
                var useOutputDir = !string.IsNullOrWhiteSpace(OutputDirectory);

                for (int i = 0; i < FileItems.Count; i++)
                {
                    var fileItem = FileItems[i];
                    fileItem.Status = CleanupFileStatus.Processing;
                    ProgressStatus = $"正在处理: {fileItem.FileName} ({i + 1}/{FileItems.Count})";
                    ProgressPercent = (int)((i / (double)FileItems.Count) * 100);

                    CleanupResult result;

                    if (useOutputDir)
                    {
                        Directory.CreateDirectory(OutputDirectory);
                        result = await _cleanupService.CleanupAsync(fileItem, OutputDirectory);
                    }
                    else
                    {
                        result = await _cleanupService.CleanupAsync(fileItem);
                    }

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
                            fileItem.StatusMessage = useOutputDir
                                ? $"{result.Message} → {result.OutputFilePath}"
                                : result.Message;
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

                _logger.LogInformation("批量清理完成: {Success} 成功, {Failure} 失败, {Skipped} 跳过",
                    successCount, failureCount, skippedCount);

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

        [RelayCommand(CanExecute = nameof(CanClearList))]
        public void ClearList()
        {
            FileItems.Clear();
            ProgressStatus = "等待处理...";
            ProgressPercent = 0;
        }

        [RelayCommand]
        private void RemoveSelectedFiles()
        {
            _logger.LogDebug("RemoveSelectedFiles called — stub, not yet connected to UI selection");
        }

        [RelayCommand]
        private void BrowseOutputDirectory()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择清理输出目录",
                DefaultDirectory = OutputDirectory
            };

            if (dialog.ShowDialog() == true)
            {
                OutputDirectory = dialog.FolderName;
            }
        }

        [RelayCommand]
        private void OpenOutputFolder()
        {
            var path = OutputDirectory;
            if (!string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path)
                    {
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "无法打开输出文件夹: {Path}", path);
                }
            }
        }

        /// <summary>
        /// 拖放文件命令：遍历路径，.docx 文件调用 AddFiles，文件夹调用 AddFolder。
        /// 由 FileDragDrop Behavior 通过 DropCommand 附加属性调用。
        /// </summary>
        [RelayCommand]
        private void DropFiles(string[] paths)
        {
            if (paths == null || paths.Length == 0) return;

            foreach (var path in paths)
            {
                if (File.Exists(path) && path.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                {
                    AddFiles(new[] { path });
                }
                else if (Directory.Exists(path))
                {
                    AddFolder(path);
                }
            }

            CommandManager.InvalidateRequerySuggested();
        }

        #endregion

        #region Public Methods (called from code-behind)

        public void AddFiles(string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                if (!File.Exists(filePath))
                    continue;

                if (!filePath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                    continue;

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

        #endregion

        private void OnProgressChanged(object? sender, CleanupProgressEventArgs e)
        {
            // 可用于更细粒度的进度更新
        }
    }
}
