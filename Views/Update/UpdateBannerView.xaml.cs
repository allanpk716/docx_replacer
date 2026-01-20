using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using DocuFiller.ViewModels.Update;

namespace DocuFiller.Views.Update
{
    /// <summary>
    /// 更新通知横幅视图
    /// </summary>
    public partial class UpdateBannerView : Window
    {
        public UpdateBannerView(UpdateBannerViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // 设置窗口属性
            WindowStartupLocation = WindowStartupLocation.Manual;

            // 允许拖动窗口
            MouseDown += Window_MouseDown;
        }

        /// <summary>
        /// 窗口鼠标按下事件 - 用于拖动窗口
        /// </summary>
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        /// <summary>
        /// 窗口加载时设置位置
        /// </summary>
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            // 将窗口定位到屏幕右上角
            var workingArea = SystemParameters.WorkArea;
            this.Left = workingArea.Right - this.Width - 20;
            this.Top = workingArea.Top + 20;
        }
    }
}