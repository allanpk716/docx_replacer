using System.Windows;
using DocuFiller.ViewModels.Update;

namespace DocuFiller.Views.Update
{
    /// <summary>
    /// UpdateWindow.xaml 的交互逻辑
    /// </summary>
    public partial class UpdateWindow : Window
    {
        /// <summary>
        /// 初始化 UpdateWindow 的新实例
        /// </summary>
        /// <param name="viewModel">更新窗口 ViewModel</param>
        public UpdateWindow(UpdateViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));

            // 设置 ViewModel 关闭窗口的回调
            viewModel.RequestClose = () =>
            {
                Close();
            };
        }

        /// <summary>
        /// 窗口关闭时清理资源
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // 清理 ViewModel
            if (DataContext is UpdateViewModel viewModel)
            {
                viewModel.RequestClose = null;
            }
        }
    }
}
