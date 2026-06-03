using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using DocuFiller.Models;
using DocuFiller.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Views
{
    public partial class CleanupWindow : Window
    {
        private readonly CleanupViewModel _viewModel;
        private readonly ILogger<CleanupWindow> _logger;

        public CleanupWindow(ILogger<CleanupWindow> logger)
        {
            InitializeComponent();
            _logger = logger;
            _viewModel = App.Current.ServiceProvider.GetRequiredService<CleanupViewModel>();
            _viewModel.OutputDirectory = string.Empty;
            DataContext = _viewModel;
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            _logger.LogDebug("CleanupWindow OnDragEnter triggered");
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                if (sender is Border border)
                {
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3));
                    border.BorderThickness = new Thickness(3);
                    border.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x21, 0x96, 0xF3));
                    _logger.LogDebug("CleanupWindow Border style updated");
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnDragLeave(object sender, DragEventArgs e)
        {
            if (sender is Border border)
            {
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
                border.BorderThickness = new Thickness(2);
                border.Background = new SolidColorBrush(Color.FromRgb(0xF9, 0xF9, 0xF9));
            }
            e.Handled = true;
        }

        private void OnDragOver(object sender, DragEventArgs e)
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

        private void OnDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0)
                    {
                        var folders = files.Where(p => Directory.Exists(p)).ToList();
                        var docxFiles = files.Where(p => File.Exists(p)
                            && p.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)).ToList();

                        if (folders.Count > 0 && docxFiles.Count > 0)
                        {
                            MessageBox.Show("不支持同时导入文件和文件夹，请分开操作。", "提示",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (docxFiles.Count > 0)
                        {
                            _viewModel.AddFiles(docxFiles.ToArray());
                        }

                        foreach (var folder in folders)
                        {
                            _viewModel.AddFolder(folder);
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
                if (sender is Border border)
                {
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
                    border.BorderThickness = new Thickness(2);
                    border.Background = new SolidColorBrush(Color.FromRgb(0xF9, 0xF9, 0xF9));
                }
            }
            e.Handled = true;
        }

        private void OnRemoveSelectedClick(object sender, RoutedEventArgs e)
        {
            var selected = FileListView.SelectedItems.Cast<CleanupFileItem>().ToList();
            foreach (var item in selected)
            {
                _viewModel.RemoveFile(item);
            }
        }

        private void OnClearListClick(object sender, RoutedEventArgs e)
        {
            _viewModel.ClearList();
        }

        private async void OnStartCleanupClick(object sender, RoutedEventArgs e)
        {
            await _viewModel.StartCleanupAsync();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
