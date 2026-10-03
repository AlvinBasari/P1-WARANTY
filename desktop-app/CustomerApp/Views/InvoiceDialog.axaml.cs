using Avalonia.Controls;
using CustomerApp.ViewModels;

namespace CustomerApp.Views
{
    public partial class InvoiceDialog : Window
    {
        public InvoiceDialog()
        {
            InitializeComponent();
        }

        public InvoiceDialog(InvoiceViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
