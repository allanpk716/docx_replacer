using System.Windows;
using DocuFiller.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Views
{
    public partial class UpdateSettingsWindow : Window
    {
        private readonly UpdateSettingsViewModel _viewModel;
        private readonly ILogger<UpdateSettingsWindow> _logger;

        public UpdateSettingsWindow(ILogger<UpdateSettingsWindow> logger)
        {
            InitializeComponent();
            _logger = logger;

            _viewModel = App.Current.ServiceProvider.GetRequiredService<UpdateSettingsViewModel>();
            DataContext = _viewModel;

            // 注入关闭回调，让 ViewModel 的 Save/Cancel 命令可以设置 DialogResult 并关闭窗口
            _viewModel.CloseCallback = (dialogResult) =>
            {
                DialogResult = dialogResult;
                Close();
            };
        }
    }
}
