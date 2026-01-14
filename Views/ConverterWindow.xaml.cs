using System.Windows;
using DocuFiller.ViewModels;

namespace DocuFiller.Views
{
    public partial class ConverterWindow : Window
    {
        public ConverterWindow(ConverterWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
