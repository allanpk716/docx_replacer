using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DocuFiller.ViewModels;
using DocuFiller.Models;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Linq;

namespace DocuFiller
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;

        public MainWindow(ILogger<MainWindow> logger)
        {
            InitializeComponent();
            _logger = logger;
            
            // 从依赖注入容器获取ViewModel
            var app = (App)Application.Current;
            DataContext = app.ServiceProvider.GetRequiredService<MainWindowViewModel>();
            
            // 绑定 IsTopmost 到 Window.Topmost
            if (DataContext is MainWindowViewModel vm)
            {
                vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(MainWindowViewModel.IsTopmost))
                    {
                        Topmost = vm.IsTopmost;
                        UpdatePinButtonVisual(vm.IsTopmost);
                    }
                };
            }
        }

        private void UpdatePinButtonVisual(bool isTopmost)
        {
            if (PinIcon != null)
            {
                PinIcon.Text = isTopmost ? "📌" : "📍";
                PinIcon.Opacity = isTopmost ? 1.0 : 0.5;
            }
            if (PinButton != null)
            {
                PinButton.ToolTip = isTopmost ? "取消置顶" : "置顶窗口";
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        /// <summary>
        /// 窗口级 PreviewDragOver 处理器：当窗口未激活时自动调用 Activate()，
        /// 使子控件的拖放事件处理器能正常接收 OLE 拖放消息。
        /// </summary>
        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (!IsActive)
            {
                Activate();
                _logger.LogInformation("Window activated for drag-drop");
            }
            e.Handled = false;
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
        
        #region 数据文件拖拽事件处理

        /// <summary>
        /// 数据文件拖拽进入事件
        /// </summary>
        private void DataPathTextBox_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1 && IsDataFile(files[0]))
                {
                    e.Effects = DragDropEffects.Copy;
                    if (sender is System.Windows.Controls.TextBox textBox)
                    {
                        textBox.BorderBrush = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3));
                        textBox.BorderThickness = new Thickness(2);
                        textBox.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x21, 0x96, 0xF3));
                    }
                    _logger.LogDebug("数据文件拖拽进入: {FilePath}", files[0]);
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                    _logger.LogDebug("非数据文件或多文件拖拽: {Files}", string.Join(", ", files));
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
                _logger.LogDebug("拖拽数据不是文件格式");
            }
            e.Handled = true;
        }

        /// <summary>
        /// 数据文件拖拽离开事件
        /// </summary>
        private void DataPathTextBox_PreviewDragLeave(object sender, DragEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                textBox.BorderBrush = new SolidColorBrush(Color.FromRgb(0xBD, 0xC3, 0xC7));
                textBox.BorderThickness = new Thickness(1);
                textBox.Background = Brushes.White;
            }
        }

        /// <summary>
        /// 数据文件拖拽悬停事件
        /// </summary>
        private void DataPathTextBox_PreviewDragOver(object sender, DragEventArgs e)
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
        /// 数据文件拖拽放置事件
        /// </summary>
        private void DataPathTextBox_PreviewDrop(object sender, DragEventArgs e)
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
                            if (DataContext is MainWindowViewModel viewModel)
                            {
                                _logger.LogDebug("数据文件拖放 - 设置DataPath: {FilePath}", filePath);
                                viewModel.DataPath = filePath;
                                _logger.LogDebug("数据文件拖放 - DataPath已设置为: {DataPath}", viewModel.DataPath);
                                viewModel.PreviewDataCommand?.Execute(null);
                            }
                        }
                        else
                        {
                            MessageBox.Show("请拖拽 Excel (.xlsx) 文件！", "文件类型错误",
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
                if (sender is System.Windows.Controls.TextBox textBox)
                {
                    textBox.BorderBrush = new SolidColorBrush(Color.FromRgb(0xBD, 0xC3, 0xC7));
                    textBox.BorderThickness = new Thickness(1);
                    textBox.Background = Brushes.White;
                }
            }
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
        /// 检查是否为支持的数据文件（Excel）
        /// </summary>
        private bool IsDataFile(string filePath)
        {
            return IsExcelFile(filePath);
        }

        #endregion

        #region 模板文件拖拽事件处理

        /// <summary>
        /// 模板文件拖拽进入事件
        /// </summary>
        private void TemplatePathTextBox_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var path = files[0];
                    bool isValid = false;

                    if (File.Exists(path) && IsDocxFile(path))
                    {
                        isValid = true;
                    }
                    else if (Directory.Exists(path))
                    {
                        isValid = true;
                    }

                    if (isValid)
                    {
                        e.Effects = DragDropEffects.Copy;
                        if (sender is System.Windows.Controls.TextBox textBox)
                        {
                            textBox.BorderBrush = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3));
                            textBox.BorderThickness = new Thickness(2);
                            textBox.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x21, 0x96, 0xF3));
                        }
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
        /// 模板文件拖拽离开事件
        /// </summary>
        private void TemplatePathTextBox_PreviewDragLeave(object sender, DragEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                textBox.BorderBrush = new SolidColorBrush(Color.FromRgb(0xBD, 0xC3, 0xC7));
                textBox.BorderThickness = new Thickness(1);
                textBox.Background = Brushes.White;
            }
        }

        /// <summary>
        /// 模板文件拖拽悬停事件
        /// </summary>
        private void TemplatePathTextBox_PreviewDragOver(object sender, DragEventArgs e)
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
        /// 模板文件拖拽放置事件
        /// </summary>
        private async void TemplatePathTextBox_PreviewDrop(object sender, DragEventArgs e)
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
                            if (File.Exists(path) && IsDocxFile(path))
                            {
                                await viewModel.HandleSingleFileDropAsync(path);
                            }
                            else if (Directory.Exists(path))
                            {
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
                if (sender is System.Windows.Controls.TextBox textBox)
                {
                    textBox.BorderBrush = new SolidColorBrush(Color.FromRgb(0xBD, 0xC3, 0xC7));
                    textBox.BorderThickness = new Thickness(1);
                    textBox.Background = Brushes.White;
                }
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
        /// 恢复边框样式（供清理功能拖放区域使用）
        /// </summary>
        private void RestoreBorderStyle(System.Windows.Controls.Border? border)
        {
            if (border != null)
            {
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
                border.BorderThickness = new Thickness(2);
                border.Background = new SolidColorBrush(Color.FromRgb(0xF9, 0xF9, 0xF9));
            }
        }

        /// <summary>
        /// 更新边框样式（供清理功能拖放区域使用）
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
                        CommandManager.InvalidateRequerySuggested();
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