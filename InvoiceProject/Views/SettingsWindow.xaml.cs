using System.Windows;
using InvoiceProject.ViewModels;

namespace InvoiceProject.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // This tells the ViewModel how to close this specific window when it finishes saving
        if (viewModel.CloseAction == null)
        {
            viewModel.CloseAction = new System.Action(this.Close);
        }
    }
}