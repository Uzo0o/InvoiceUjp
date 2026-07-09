using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using InvoiceProject.Services;
using InvoiceProject.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceProject;


public partial class MainWindow : Window
{
    public MainWindow(InvoiceViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}