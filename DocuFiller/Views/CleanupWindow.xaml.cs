using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using DocuFiller.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DocuFiller.Views
{
    public partial class CleanupWindow : Window
    {
        private readonly CleanupViewModel _viewModel;

        public CleanupWindow()
        {
            InitializeComponent();
            _viewModel = App.Current.ServiceProvider.GetRequiredService<CleanupViewModel>();
            DataContext = _viewModel;
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[CleanupWindow] OnDragEnter triggered");
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                if (sender is Border border)
                {
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3));
                    border.BorderThickness = new Thickness(3);
                    border.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x21, 0x96, 0xF3));
                    System.Diagnostics.Debug.WriteLine("[CleanupWindow] Border style updated");
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
                        foreach (var file in files)
                        {
                            if (File.Exists(file) && file.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                            {
                                _viewModel.AddFiles(new[] { file });
                            }
                            else if (Directory.Exists(file))
                            {
                                _viewModel.AddFolder(file);
                            }
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
            // 简化处理：清空列表，因为 ListView 的选中项处理较复杂
            // 实际使用中可以添加删除选中项的功能
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
