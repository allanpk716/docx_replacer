using System;
using System.ComponentModel;
using System.Windows;
using DocuFiller.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DocuFiller.Views
{
    /// <summary>
    /// JsonEditorWindow.xaml 的交互逻辑
    /// </summary>
    public partial class JsonEditorWindow : Window
    {
        private readonly JsonEditorViewModel _viewModel;
        
        public JsonEditorWindow(JsonEditorViewModel viewModel)
        {
            InitializeComponent();
            
            // 使用依赖注入的ViewModel
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;
            
            // 订阅ViewModel事件
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            
            // 窗口加载时的处理
            Loaded += OnWindowLoaded;
        }
        
        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 处理ViewModel属性变化
            if (e.PropertyName == nameof(JsonEditorViewModel.HasUnsavedChanges))
            {
                // 更新窗口标题显示未保存状态
                UpdateWindowTitle();
            }
        }
        
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // 窗口加载完成后的初始化
            _viewModel?.Initialize();
        }
        
        private void UpdateWindowTitle()
        {
            var baseTitle = "JSON关键词编辑器";
            if (_viewModel?.HasUnsavedChanges == true)
            {
                Title = $"{baseTitle} *";
            }
            else
            {
                Title = baseTitle;
            }
        }
        
        protected override void OnClosing(CancelEventArgs e)
        {
            // 检查是否有未保存的更改
            if (_viewModel?.HasUnsavedChanges == true)
            {
                var result = MessageBox.Show(
                    "有未保存的更改，是否保存？",
                    "确认",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    // 执行保存操作
                    _viewModel.SaveCommand?.Execute(null);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    // 取消关闭
                    e.Cancel = true;
                    return;
                }
            }
            
            // 清理资源
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel.Dispose();
            }
            
            base.OnClosing(e);
        }
    }
}