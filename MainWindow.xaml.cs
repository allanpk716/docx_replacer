using Microsoft.Extensions.DependencyInjection;
using DocuFiller.ViewModels;
using System;
using System.Windows;
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
        public MainWindow()
        {
            InitializeComponent();
            
            // 从依赖注入容器获取ViewModel
            var app = (App)Application.Current;
            DataContext = app.ServiceProvider.GetRequiredService<MainWindowViewModel>();
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
                if (files.Length == 1 && IsJsonFile(files[0]))
                {
                    e.Effects = DragDropEffects.Copy;
                    // 添加视觉反馈
                    if (sender is System.Windows.Controls.Border border)
                    {
                        border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3)); // 蓝色
                        border.BorderThickness = new Thickness(3);
                        border.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x21, 0x96, 0xF3)); // 半透明蓝色
                    }
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] JSON文件拖拽进入: {files[0]}");
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 非JSON文件或多文件拖拽: {string.Join(", ", files)}");
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
                if (files.Length == 1 && IsJsonFile(files[0]))
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
                        if (IsJsonFile(filePath))
                        {
                            // 验证JSON文件有效性
                            if (IsValidJsonFile(filePath))
                            {
                                // 设置数据路径并自动预览
                                if (DataContext is MainWindowViewModel viewModel)
                                {
                                    Console.WriteLine($"[DEBUG] JSON文件拖放 - 设置DataPath: {filePath}");
                                    viewModel.DataPath = filePath;
                                    Console.WriteLine($"[DEBUG] JSON文件拖放 - DataPath已设置为: {viewModel.DataPath}");
                                    // 自动触发预览
                                    viewModel.PreviewDataCommand?.Execute(null);
                                }
                            }
                            else
                            {
                                MessageBox.Show("所选文件不是有效的JSON格式！", "文件格式错误", 
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                        else
                        {
                            MessageBox.Show("请拖拽JSON文件！", "文件类型错误", 
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
        /// 文件夹拖拽进入事件
        /// </summary>
        private void TemplateFolderDropBorder_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0 && Directory.Exists(files[0]))
                {
                    e.Effects = DragDropEffects.Copy;
                    // 添加视觉反馈
                    if (sender is System.Windows.Controls.Border border)
                    {
                        border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3)); // 蓝色
                        border.BorderThickness = new Thickness(3);
                        border.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x21, 0x96, 0xF3)); // 半透明蓝色
                    }
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
        }
        
        /// <summary>
        /// 文件夹拖拽离开事件
        /// </summary>
        private void TemplateFolderDropBorder_DragLeave(object sender, DragEventArgs e)
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
        /// 文件夹拖拽悬停事件
        /// </summary>
        private void TemplateFolderDropBorder_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0 && Directory.Exists(files[0]))
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
        /// 文件夹拖拽放置事件
        /// </summary>
        private async void TemplateFolderDropBorder_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0)
                    {
                        var folderPath = files[0];
                        if (Directory.Exists(folderPath))
                        {
                            // 设置文件夹路径并扫描docx文件
                            if (DataContext is MainWindowViewModel viewModel)
                            {
                                await viewModel.HandleFolderDropAsync(folderPath);
                            }
                        }
                        else
                        {
                            MessageBox.Show("请拖拽文件夹！", "文件类型错误", 
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"处理拖拽文件夹时发生错误：{ex.Message}", "错误", 
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
        
        #endregion
    }
}