using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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

        [ObservableProperty]
        private string? _importedFolderPath;

        public ObservableCollection<CleanupFileItem> FileItems { get; } = new();

        public bool CanStartCleanup => FileItems.Count > 0 && !IsProcessing;

        public CleanupViewModel(IDocumentCleanupService cleanupService, ILogger<CleanupViewModel> logger)
        {
            _cleanupService = cleanupService ?? throw new ArgumentNullException(nameof(cleanupService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _cleanupService.ProgressChanged += OnProgressChanged;
        }

        partial void OnIsProcessingChanged(bool value)
        {
            OnPropertyChanged(nameof(CanStartCleanup));
        }

        #region Commands

        [RelayCommand(CanExecute = nameof(CanStartCleanup))]
        public async Task StartCleanupAsync()
        {
            IsProcessing = true;
            ProgressStatus = "准备处理...";
            ProgressPercent = 0;

            var fileItemsSnapshot = FileItems.ToList();
            var isFolderMode = !string.IsNullOrEmpty(ImportedFolderPath);
            var importedFolder = ImportedFolderPath;
            var outputDir = OutputDirectory;
            var dispatcher = Application.Current.Dispatcher;

            int successCount = 0;
            int failureCount = 0;
            int skippedCount = 0;

            try
            {
                var useOutputDir = !string.IsNullOrWhiteSpace(outputDir);

                await Task.Run(() =>
                {
                    string? folderOutputRoot = null;
                    if (isFolderMode && useOutputDir)
                    {
                        string timestamp = DateTime.Now.ToString("yyyy年M月d日HHmmss");
                        string folderName = new DirectoryInfo(importedFolder!).Name;
                        folderOutputRoot = Path.Combine(outputDir, $"{folderName}_{timestamp}");
                        Directory.CreateDirectory(folderOutputRoot);
                        _logger.LogInformation("文件夹模式输出目录: {OutputRoot}", folderOutputRoot);
                    }

                    for (int i = 0; i < fileItemsSnapshot.Count; i++)
                    {
                        var fileItem = fileItemsSnapshot[i];

                        dispatcher.Invoke(() =>
                        {
                            fileItem.Status = CleanupFileStatus.Processing;
                            ProgressStatus = $"正在处理: {fileItem.FileName} ({i + 1}/{fileItemsSnapshot.Count})";
                            ProgressPercent = (int)((i / (double)fileItemsSnapshot.Count) * 100);
                        });

                        try
                        {
                            CleanupResult result;

                            if (isFolderMode && folderOutputRoot != null)
                            {
                                string relativePath = Path.GetRelativePath(importedFolder!, fileItem.FilePath);
                                string outputPath = Path.Combine(folderOutputRoot, relativePath);
                                string? outputSubDir = Path.GetDirectoryName(outputPath);
                                if (!string.IsNullOrEmpty(outputSubDir))
                                    Directory.CreateDirectory(outputSubDir);

                                File.Copy(fileItem.FilePath, outputPath, overwrite: true);

                                var cleanItem = new CleanupFileItem
                                {
                                    FilePath = outputPath,
                                    FileName = fileItem.FileName
                                };
                                result = _cleanupService.CleanupAsync(cleanItem).GetAwaiter().GetResult();
                            }
                            else if (useOutputDir)
                            {
                                var serviceItem = new CleanupFileItem
                                {
                                    FilePath = fileItem.FilePath,
                                    FileName = fileItem.FileName,
                                    FileSize = fileItem.FileSize
                                };
                                result = _cleanupService.CleanupAsync(serviceItem, outputDir).GetAwaiter().GetResult();
                            }
                            else
                            {
                                var cleanItem = new CleanupFileItem
                                {
                                    FilePath = fileItem.FilePath,
                                    FileName = fileItem.FileName
                                };
                                result = _cleanupService.CleanupAsync(cleanItem).GetAwaiter().GetResult();
                            }

                            dispatcher.Invoke(() =>
                            {
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
                                        fileItem.StatusMessage = isFolderMode
                                            ? result.Message
                                            : (result.OutputFilePath ?? result.Message);
                                        successCount++;
                                    }
                                }
                                else
                                {
                                    fileItem.Status = CleanupFileStatus.Failure;
                                    fileItem.StatusMessage = result.Message;
                                    failureCount++;
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "处理文件 {FileName} 时出错", fileItem.FileName);
                            dispatcher.Invoke(() =>
                            {
                                fileItem.Status = CleanupFileStatus.Failure;
                                fileItem.StatusMessage = $"处理失败: {ex.Message}";
                                failureCount++;
                            });
                        }
                    }
                });

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

        [RelayCommand]
        public void ClearList()
        {
            FileItems.Clear();
            ImportedFolderPath = null;
            ProgressStatus = "等待处理...";
            ProgressPercent = 0;
            OnPropertyChanged(nameof(CanStartCleanup));
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

        [RelayCommand]
        private void DropFiles(string[] paths)
        {
            if (paths == null || paths.Length == 0) return;

            var folders = paths.Where(p => Directory.Exists(p)).ToList();
            var docxFiles = paths.Where(p => File.Exists(p)
                && p.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)).ToList();

            if (folders.Count > 0 && docxFiles.Count > 0)
            {
                MessageBox.Show("不支持同时导入文件和文件夹，请分开操作。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (docxFiles.Count > 0)
            {
                AddFiles(docxFiles.ToArray());
            }

            foreach (var folder in folders)
            {
                AddFolder(folder);
            }

            CommandManager.InvalidateRequerySuggested();
        }

        #endregion

        #region Public Methods (called from code-behind)

        public void AddFiles(string[] filePaths)
        {
            if (!string.IsNullOrEmpty(ImportedFolderPath))
            {
                MessageBox.Show("当前已导入文件夹，请先清空列表再添加单独的文件。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AddFilesCore(filePaths);
        }

        private void AddFilesCore(string[] filePaths)
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
        }

        public void AddFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return;

            if (FileItems.Count > 0 && string.IsNullOrEmpty(ImportedFolderPath))
            {
                MessageBox.Show("当前已导入单独的文件，请先清空列表再导入文件夹。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(ImportedFolderPath)
                && !Path.GetFullPath(ImportedFolderPath)
                    .Equals(Path.GetFullPath(folderPath), StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("已导入其他文件夹，请先清空列表再导入新文件夹。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ImportedFolderPath = folderPath;
            var docxFiles = Directory.GetFiles(folderPath, "*.docx", SearchOption.AllDirectories);
            AddFilesCore(docxFiles);
        }

        public void RemoveFile(CleanupFileItem item)
        {
            FileItems.Remove(item);
            OnPropertyChanged(nameof(CanStartCleanup));
        }

        #endregion

        private void OnProgressChanged(object? sender, CleanupProgressEventArgs e)
        {
        }
    }
}
