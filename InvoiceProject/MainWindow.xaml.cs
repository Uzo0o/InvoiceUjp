using System.Windows;

namespace InvoiceProject;

public partial class MainWindow : Window
{
    public MainWindow(ViewModels.InvoiceViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        if (this.WindowState == WindowState.Normal)
        {
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            this.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
            this.WindowState = WindowState.Maximized;
        }
        else
        {
            this.WindowState = WindowState.Normal;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}