using System.IO;
using System.Windows;
using System.Windows.Controls;
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
            _viewModel = App.Current.Services.GetRequiredService<CleanupViewModel>();
            DataContext = _viewModel;
        }

        private void OnDrop(object sender, DragEventArgs e)
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

            ResetDropZone();
            e.Handled = true;
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                DropZoneBorder.Background = System.Windows.Media.Brushes.LightBlue;
                DropZoneBorder.BorderBrush = System.Windows.Media.Brushes.Blue;
            }
            e.Handled = true;
        }

        private void OnDragLeave(object sender, DragEventArgs e)
        {
            ResetDropZone();
            e.Handled = true;
        }

        private void ResetDropZone()
        {
            DropZoneBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 249, 249));
            DropZoneBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204));
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