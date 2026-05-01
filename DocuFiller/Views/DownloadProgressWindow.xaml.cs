using System.Windows;
using DocuFiller.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Views
{
    public partial class DownloadProgressWindow : Window
    {
        private DownloadProgressViewModel? _viewModel;
        private readonly ILogger<DownloadProgressWindow> _logger;

        public DownloadProgressWindow(ILogger<DownloadProgressWindow> logger)
        {
            InitializeComponent();
            _logger = logger;

            Owner = Application.Current.MainWindow;

            // ViewModel will be created by MainWindowViewModel with proper parameters,
            // then set as DataContext before showing this window.
            // This window only provides the close callback pattern.
        }

        /// <summary>
        /// Set the ViewModel and wire up the close callback.
        /// Called by MainWindowViewModel after creating the ViewModel with required parameters.
        /// </summary>
        public void SetViewModel(DownloadProgressViewModel viewModel)
        {
            _viewModel = viewModel;
            DataContext = _viewModel;

            // Inject close callback so ViewModel can close this window.
            // Must dispatch to UI thread because CloseCallback is invoked from Task.Run background thread.
            _viewModel.CloseCallback = (dialogResult) =>
            {
                Dispatcher.Invoke(() =>
                {
                    DialogResult = dialogResult;
                    Close();
                });
            };
        }

        /// <summary>
        /// Prevent closing via X button during active download — route through CancelCommand instead.
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_viewModel?.IsDownloading == true)
            {
                // Don't allow direct window close; user must use Cancel button
                _viewModel.CancelCommand.Execute(null);
                e.Cancel = true;
                return;
            }

            base.OnClosing(e);
        }
    }
}
