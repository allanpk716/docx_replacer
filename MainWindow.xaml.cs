using Microsoft.Extensions.DependencyInjection;
using DocuFiller.ViewModels;
using DocuFiller.Models;
using System;
using System.Windows;
using System.Windows.Media;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace DocuFiller
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // 从依赖注入容器获取ViewModel
            var app = (App)Application.Current;
            DataContext = app.ServiceProvider.GetRequiredService<MainWindowViewModel>();
        }
        
        /// <summary>
        /// 关键词编辑器超链接点击事件
        /// </summary>
        private void KeywordEditorHyperlink_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                string url = "http://192.168.200.23:32200/";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 打开关键词编辑器: {url}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开关键词编辑器：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 打开关键词编辑器失败: {ex.Message}");
            }
        }

        /// <summary>
        /// JSON转Excel转换工具超链接点击事件
        /// </summary>
        private void ConverterHyperlink_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.OpenConverterCommand?.Execute(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开转换工具：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 检查更新超链接点击事件
        /// </summary>
        private void CheckForUpdateHyperlink_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.CheckForUpdateCommand?.Execute(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"检查更新失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 窗口关闭时的处理
        /// </summary>
        /// <param name="e">取消事件参数</param>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // 如果正在处理中，询问用户是否确认退出
            if (DataContext is MainWindowViewModel viewModel && viewModel.IsProcessing)
            {
                var result = MessageBox.Show(
                    "正在处理文档，确定要退出吗？",
                    "确认退出",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
                
                // 取消当前处理
                viewModel.CancelProcessCommand?.Execute(null);
            }
            
            base.OnClosing(e);
        }
        
        #region JSON数据文件拖拽事件处理
        
        /// <summary>
        /// JSON数据文件拖拽进入事件
        /// </summary>
        private void DataFileDropBorder_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1 && IsDataFile(files[0]))
                {
                    e.Effects = DragDropEffects.Copy;
                    // 添加视觉反馈
                    if (sender is System.Windows.Controls.Border border)
                    {
                        border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3)); // 蓝色
                        border.BorderThickness = new Thickness(3);
                        border.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x21, 0x96, 0xF3)); // 半透明蓝色
                    }
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 数据文件拖拽进入: {files[0]}");
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 非数据文件或多文件拖拽: {string.Join(", ", files)}");
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
                System.Diagnostics.Debug.WriteLine("[DEBUG] 拖拽数据不是文件格式");
            }
            e.Handled = true;
        }
        
        /// <summary>
        /// JSON数据文件拖拽离开事件
        /// </summary>
        private void DataFileDropBorder_DragLeave(object sender, DragEventArgs e)
        {
            // 恢复原始样式
            if (sender is System.Windows.Controls.Border border)
            {
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(0xBD, 0xC3, 0xC7)); // 原始灰色
                border.BorderThickness = new Thickness(2);
                border.Background = Brushes.Transparent;
            }
        }
        
        /// <summary>
        /// JSON数据文件拖拽悬停事件
        /// </summary>
        private void DataFileDropBorder_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1 && IsDataFile(files[0]))
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }
        
        /// <summary>
        /// JSON数据文件拖拽放置事件
        /// </summary>
        private void DataFileDropBorder_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0)
                    {
                        var filePath = files[0];
                        if (IsDataFile(filePath))
                        {
                            // 对于JSON文件，验证格式
                            if (IsJsonFile(filePath) && !IsValidJsonFile(filePath))
                            {
                                MessageBox.Show("所选文件不是有效的JSON格式！", "文件格式错误",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            // 设置数据路径并自动预览
                            if (DataContext is MainWindowViewModel viewModel)
                            {
                                Console.WriteLine($"[DEBUG] 数据文件拖放 - 设置DataPath: {filePath}");
                                viewModel.DataPath = filePath;
                                Console.WriteLine($"[DEBUG] 数据文件拖放 - DataPath已设置为: {viewModel.DataPath}");
                                // 自动触发预览
                                viewModel.PreviewDataCommand?.Execute(null);
                            }
                        }
                        else
                        {
                            MessageBox.Show("请拖拽JSON或Excel文件！", "文件类型错误",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"处理拖拽文件时发生错误：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 恢复原始样式
                if (sender is System.Windows.Controls.Border border)
                {
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(0xBD, 0xC3, 0xC7));
                    border.BorderThickness = new Thickness(2);
                    border.Background = Brushes.Transparent;
                }
            }
        }
        
        /// <summary>
        /// 检查是否为JSON文件
        /// </summary>
        private bool IsJsonFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".json";
        }

        /// <summary>
        /// 检查是否为Excel文件
        /// </summary>
        private bool IsExcelFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".xlsx";
        }

        /// <summary>
        /// 检查是否为支持的数据文件（JSON或Excel）
        /// </summary>
        private bool IsDataFile(string filePath)
        {
            return IsJsonFile(filePath) || IsExcelFile(filePath);
        }
        
        /// <summary>
        /// 验证JSON文件有效性
        /// </summary>
        private bool IsValidJsonFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;
                    
                var content = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(content))
                    return false;
                    
                // 简单的JSON格式验证
                content = content.Trim();
                return (content.StartsWith("[") && content.EndsWith("]")) || 
                       (content.StartsWith("{") && content.EndsWith("}"));
            }
            catch
            {
                return false;
            }
        }
        
        #endregion
        
        #region 文件夹拖拽事件处理

        /// <summary>
        /// 拖拽进入事件
        /// </summary>
        private void TemplateDropBorder_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var path = files[0];
                    bool isValid = false;
                    string hintText = string.Empty;

                    if (File.Exists(path) && IsDocxFile(path))
                    {
                        isValid = true;
                        hintText = $"可处理文件: {Path.GetFileName(path)}";
                    }
                    else if (Directory.Exists(path))
                    {
                        isValid = true;
                        hintText = $"可处理文件夹: {Path.GetFileName(path)} (包含子文件夹)";
                    }

                    if (isValid)
                    {
                        e.Effects = DragDropEffects.Copy;
                        UpdateBorderStyle(sender as System.Windows.Controls.Border, true);
                        UpdateHintText(hintText);
                    }
                    else
                    {
                        e.Effects = DragDropEffects.None;
                    }
                }
            }
            e.Handled = true;
        }

        /// <summary>
        /// 拖拽离开事件
        /// </summary>
        private void TemplateDropBorder_DragLeave(object sender, DragEventArgs e)
        {
            RestoreBorderStyle(sender as System.Windows.Controls.Border);
            UpdateHintText("拖拽单个 docx 文件或包含 docx 文件的文件夹到此处");
        }

        /// <summary>
        /// 拖拽悬停事件
        /// </summary>
        private void TemplateDropBorder_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var path = files[0];
                    if ((File.Exists(path) && IsDocxFile(path)) || Directory.Exists(path))
                    {
                        e.Effects = DragDropEffects.Copy;
                    }
                    else
                    {
                        e.Effects = DragDropEffects.None;
                    }
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        /// <summary>
        /// 模板文件/文件夹拖拽放置事件（统一处理）
        /// </summary>
        private async void TemplateDropBorder_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0)
                    {
                        var path = files[0];

                        if (DataContext is MainWindowViewModel viewModel)
                        {
                            // 判断是文件还是文件夹
                            if (File.Exists(path) && IsDocxFile(path))
                            {
                                // 单个文件处理
                                await viewModel.HandleSingleFileDropAsync(path);
                            }
                            else if (Directory.Exists(path))
                            {
                                // 文件夹处理（包含子文件夹）
                                await viewModel.HandleFolderDropAsync(path);
                            }
                            else
                            {
                                MessageBox.Show(
                                    "请拖拽 .docx/.dotx 文件或包含 .docx 文件的文件夹！",
                                    "文件类型错误",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"处理拖拽时发生错误：{ex.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                RestoreBorderStyle(sender as System.Windows.Controls.Border);
                UpdateHintText("拖拽单个 docx 文件或包含 docx 文件的文件夹到此处");
            }
        }
        
        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查是否为 docx 文件
        /// </summary>
        private bool IsDocxFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".docx" || extension == ".dotx";
        }

        /// <summary>
        /// 恢复边框样式
        /// </summary>
        private void RestoreBorderStyle(System.Windows.Controls.Border? border)
        {
            if (border != null)
            {
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(0xBD, 0xC3, 0xC7));
                border.BorderThickness = new Thickness(2);
                border.Background = Brushes.Transparent;
            }
        }

        /// <summary>
        /// 更新边框样式
        /// </summary>
        private void UpdateBorderStyle(System.Windows.Controls.Border? border, bool isActive)
        {
            if (border != null && isActive)
            {
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3));
                border.BorderThickness = new Thickness(3);
                border.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x21, 0x96, 0xF3));
            }
        }

        /// <summary>
        /// 更新提示文本
        /// </summary>
        private void UpdateHintText(string text)
        {
            // 使用 FindName 查找元素，避免直接引用未定义的名称
            var hintElement = FindName("TemplateDropHint") as System.Windows.Controls.TextBlock;
            if (hintElement != null)
            {
                hintElement.Text = text;
            }
        }

        #endregion

        #region 清理功能拖拽事件处理

        /// <summary>
        /// 清理功能拖拽进入事件
        /// </summary>
        private void CleanupDropZoneBorder_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                if (sender is System.Windows.Controls.Border border)
                {
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3));
                    border.BorderThickness = new Thickness(3);
                    border.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x21, 0x96, 0xF3));
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        /// <summary>
        /// 清理功能拖拽离开事件
        /// </summary>
        private void CleanupDropZoneBorder_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is System.Windows.Controls.Border border)
            {
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
                border.BorderThickness = new Thickness(2);
                border.Background = new SolidColorBrush(Color.FromRgb(0xF9, 0xF9, 0xF9));
            }
        }

        /// <summary>
        /// 清理功能拖拽悬停事件
        /// </summary>
        private void CleanupDropZoneBorder_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        /// <summary>
        /// 清理功能拖拽放置事件
        /// </summary>
        private void CleanupDropZoneBorder_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop) && DataContext is MainWindowViewModel viewModel)
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0)
                    {
                        // 处理每个文件或文件夹
                        foreach (var path in files)
                        {
                            if (File.Exists(path) && IsDocxFile(path))
                            {
                                // 单文件
                                AddCleanupFile(viewModel, path, InputSourceType.SingleFile);
                            }
                            else if (Directory.Exists(path))
                            {
                                // 文件夹
                                AddCleanupFolder(viewModel, path);
                            }
                        }

                        // 刷新 CanStartCleanup 属性
                        viewModel.OnPropertyChanged(nameof(viewModel.CanStartCleanup));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"处理拖拽文件时发生错误：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (sender is System.Windows.Controls.Border border)
                {
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
                    border.BorderThickness = new Thickness(2);
                    border.Background = new SolidColorBrush(Color.FromRgb(0xF9, 0xF9, 0xF9));
                }
            }
        }

        /// <summary>
        /// 添加清理文件到列表
        /// </summary>
        private void AddCleanupFile(MainWindowViewModel viewModel, string filePath, InputSourceType inputType)
        {
            // 检查重复
            if (viewModel.CleanupFileItems.Any(f => f.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
                return;

            var fileInfo = new System.IO.FileInfo(filePath);
            var fileItem = new DocuFiller.Models.CleanupFileItem
            {
                FilePath = filePath,
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                InputType = inputType
            };

            viewModel.CleanupFileItems.Add(fileItem);
        }

        /// <summary>
        /// 添加清理文件夹到列表
        /// </summary>
        private void AddCleanupFolder(MainWindowViewModel viewModel, string folderPath)
        {
            // 检查重复
            if (viewModel.CleanupFileItems.Any(f => f.FilePath.Equals(folderPath, StringComparison.OrdinalIgnoreCase)))
                return;

            var dirInfo = new DirectoryInfo(folderPath);
            var fileItem = new DocuFiller.Models.CleanupFileItem
            {
                FilePath = folderPath,
                FileName = dirInfo.Name,
                FileSize = 0,
                InputType = InputSourceType.Folder
            };

            viewModel.CleanupFileItems.Add(fileItem);
        }

        #endregion
    }
}