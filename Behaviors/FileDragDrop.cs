using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DocuFiller.Behaviors
{
    /// <summary>
    /// 文件类型过滤器，用于拖放验证
    /// </summary>
    public enum FileFilter
    {
        /// <summary>.docx/.dotx 文件或文件夹（模板拖放区）</summary>
        DocxOrFolder,
        /// <summary>单个 .xlsx 文件（数据文件拖放区）</summary>
        ExcelFile,
        /// <summary>.docx 文件或文件夹（清理功能拖放区）</summary>
        DocxFile
    }

    /// <summary>
    /// 文件拖放 AttachedProperty Behavior，封装文件类型验证、视觉效果反馈和 Drop 回调。
    /// TextBox 目标自动使用 Preview 隧道事件（绕过内置拖放拦截），其他元素使用冒泡事件。
    /// </summary>
    public static class FileDragDrop
    {
        #region IsEnabled

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(FileDragDrop),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement element) return;

            var enabled = (bool)e.NewValue;
            element.AllowDrop = enabled;

            if (element is TextBox)
            {
                if (enabled)
                {
                    element.PreviewDragEnter += OnPreviewDragEnter;
                    element.PreviewDragLeave += OnPreviewDragLeave;
                    element.PreviewDragOver += OnPreviewDragOver;
                    element.PreviewDrop += OnPreviewDrop;
                }
                else
                {
                    element.PreviewDragEnter -= OnPreviewDragEnter;
                    element.PreviewDragLeave -= OnPreviewDragLeave;
                    element.PreviewDragOver -= OnPreviewDragOver;
                    element.PreviewDrop -= OnPreviewDrop;
                }
            }
            else
            {
                if (enabled)
                {
                    element.DragEnter += OnDragEnter;
                    element.DragLeave += OnDragLeave;
                    element.DragOver += OnDragOver;
                    element.Drop += OnDrop;
                }
                else
                {
                    element.DragEnter -= OnDragEnter;
                    element.DragLeave -= OnDragLeave;
                    element.DragOver -= OnDragOver;
                    element.Drop -= OnDrop;
                }
            }
        }

        #endregion

        #region Filter

        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.RegisterAttached(
                "Filter",
                typeof(FileFilter),
                typeof(FileDragDrop),
                new PropertyMetadata(FileFilter.DocxOrFolder));

        public static FileFilter GetFilter(DependencyObject obj) => (FileFilter)obj.GetValue(FilterProperty);
        public static void SetFilter(DependencyObject obj, FileFilter value) => obj.SetValue(FilterProperty, value);

        #endregion

        #region DropCommand

        public static readonly DependencyProperty DropCommandProperty =
            DependencyProperty.RegisterAttached(
                "DropCommand",
                typeof(ICommand),
                typeof(FileDragDrop),
                new PropertyMetadata(null));

        public static ICommand GetDropCommand(DependencyObject obj) => (ICommand)obj.GetValue(DropCommandProperty);
        public static void SetDropCommand(DependencyObject obj, ICommand value) => obj.SetValue(DropCommandProperty, value);

        #endregion

        #region Validation

        /// <summary>
        /// 验证拖入文件是否符合过滤器要求。用于 DragEnter/DragOver 控制光标和高亮。
        /// </summary>
        private static bool ShouldAcceptDrag(string[]? files, FileFilter filter)
        {
            if (files == null || files.Length == 0) return false;

            return filter switch
            {
                FileFilter.ExcelFile => files.Length == 1 && IsExcelFile(files[0]),
                FileFilter.DocxOrFolder => (File.Exists(files[0]) && IsDocxFile(files[0]))
                                           || Directory.Exists(files[0]),
                // DocxFile（清理）：宽松验证，只检查是否有文件，具体过滤由 Command 负责
                FileFilter.DocxFile => true,
                _ => false
            };
        }

        private static bool IsDocxFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".docx" || ext == ".dotx";
        }

        private static bool IsExcelFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return Path.GetExtension(path).ToLowerInvariant() == ".xlsx";
        }

        #endregion

        #region Visual Feedback

        private static readonly SolidColorBrush _highlightBorder = new(Color.FromRgb(0x21, 0x96, 0xF3));
        private static readonly SolidColorBrush _highlightBackground = new(Color.FromArgb(0x20, 0x21, 0x96, 0xF3));
        private static readonly SolidColorBrush _textBoxRestoreBorder = new(Color.FromRgb(0xBD, 0xC3, 0xC7));
        private static readonly SolidColorBrush _borderRestoreBorder = new(Color.FromRgb(0xCC, 0xCC, 0xCC));
        private static readonly SolidColorBrush _borderRestoreBackground = new(Color.FromRgb(0xF9, 0xF9, 0xF9));

        private static void ApplyHighlight(UIElement element)
        {
            if (element is TextBox textBox)
            {
                textBox.BorderBrush = _highlightBorder;
                textBox.BorderThickness = new Thickness(2);
                textBox.Background = _highlightBackground;
            }
            else if (element is Border border)
            {
                border.BorderBrush = _highlightBorder;
                border.BorderThickness = new Thickness(3);
                border.Background = _highlightBackground;
            }
        }

        private static void RestoreStyle(UIElement element)
        {
            if (element is TextBox textBox)
            {
                textBox.BorderBrush = _textBoxRestoreBorder;
                textBox.BorderThickness = new Thickness(1);
                textBox.Background = Brushes.White;
            }
            else if (element is Border border)
            {
                border.BorderBrush = _borderRestoreBorder;
                border.BorderThickness = new Thickness(2);
                border.Background = _borderRestoreBackground;
            }
        }

        #endregion

        #region Preview Event Handlers (TextBox targets)

        private static void OnPreviewDragEnter(object sender, DragEventArgs e) => HandleDragEnter((UIElement)sender, e);
        private static void OnPreviewDragLeave(object sender, DragEventArgs e) => RestoreStyle((UIElement)sender);
        private static void OnPreviewDragOver(object sender, DragEventArgs e) => HandleDragOver((UIElement)sender, e);
        private static void OnPreviewDrop(object sender, DragEventArgs e) => HandleDrop((UIElement)sender, e);

        #endregion

        #region Bubbling Event Handlers (Border / other targets)

        private static void OnDragEnter(object sender, DragEventArgs e) => HandleDragEnter((UIElement)sender, e);
        private static void OnDragLeave(object sender, DragEventArgs e) => RestoreStyle((UIElement)sender);
        private static void OnDragOver(object sender, DragEventArgs e) => HandleDragOver((UIElement)sender, e);
        private static void OnDrop(object sender, DragEventArgs e) => HandleDrop((UIElement)sender, e);

        #endregion

        #region Shared Handler Logic

        private static void HandleDragEnter(UIElement element, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                var filter = GetFilter(element);

                if (ShouldAcceptDrag(files, filter))
                {
                    e.Effects = DragDropEffects.Copy;
                    ApplyHighlight(element);
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

        private static void HandleDragOver(UIElement element, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                var filter = GetFilter(element);
                e.Effects = ShouldAcceptDrag(files, filter) ? DragDropEffects.Copy : DragDropEffects.None;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private static void HandleDrop(UIElement element, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0)
                    {
                        var command = GetDropCommand(element);
                        if (command?.CanExecute(files) == true)
                        {
                            command.Execute(files);
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
                RestoreStyle(element);
            }
        }

        #endregion
    }
}
