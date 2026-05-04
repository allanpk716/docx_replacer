using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DocuFiller.ViewModels;
using System;
using System.Windows;

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
    }
}
