using System.Windows;
using System.Windows.Controls;

namespace DocuFiller.Views
{
    /// <summary>
    /// UpdateBanner.xaml 的交互逻辑
    /// </summary>
    public partial class UpdateBannerView : UserControl
    {
        public UpdateBannerView()
        {
            InitializeComponent();
        }

        private void RemindLater_Click(object sender, RoutedEventArgs e)
        {
            // 处理"稍后提醒"按钮点击事件
            // 这里应该调用 ViewModel 中的相关命令
            // 使用反射来检查和执行命令，避免编译时依赖
            if (DataContext != null)
            {
                var commandProperty = DataContext.GetType().GetProperty("RemindLaterCommand");
                if (commandProperty != null)
                {
                    var command = commandProperty.GetValue(DataContext);
                    if (command != null)
                    {
                        var canExecuteMethod = command.GetType().GetMethod("CanExecute", new[] { typeof(object) });
                        var executeMethod = command.GetType().GetMethod("Execute", new[] { typeof(object) });

                        if (canExecuteMethod != null && executeMethod != null)
                        {
                            var canExecute = (bool)canExecuteMethod.Invoke(command, new object[] { null });
                            if (canExecute)
                            {
                                executeMethod.Invoke(command, new object[] { null });
                            }
                        }
                    }
                }
            }
        }
    }
}