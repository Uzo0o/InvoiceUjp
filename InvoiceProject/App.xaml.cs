using System.Configuration;
using System.Data;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using InvoiceProject.Services;
using InvoiceProject.ViewModels;
using InvoiceProject.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvoiceProject;


public partial class App : Application
{
    public static IHost AppHost { get; private set; }

    public App()
    {
        AppHost = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, builder) => 
            {
                builder.AddJsonFile("appsettings.json", optional: false);
            })
            .ConfigureServices((context, services) =>
            {
                string certPath = context.Configuration["UjpSettings:CertPath"];
                string certPass = context.Configuration["UjpSettings:CertPassword"];
                var certificate = new X509Certificate2(certPath, certPass, X509KeyStorageFlags.Exportable);
                
                services.AddSingleton<SettingsWindow>();
                services.AddSingleton<SettingsViewModel>();
                
                services.AddHttpClient<IUjpService, UjpService>();
                
                services.AddSingleton(certificate);
                services.AddSingleton<IUserSettingsService, UserSettingsService>();
                
                services.AddSingleton<IDatabaseService, DatabaseService>();

                
    
                // 3. Register your UI (Singleton means only one instance exists for the app)
                services.AddSingleton<MainWindow>();
                services.AddSingleton<InvoiceViewModel>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        // 1. Tell WPF: "Do not close the app just because a window closed!"
        this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        await AppHost.StartAsync();
        var settingsService = AppHost.Services.GetRequiredService<IUserSettingsService>();

        if (settingsService.IsConfigured())
        {
            var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            // 2. Now that the main window is up, return to normal behavior
            this.ShutdownMode = ShutdownMode.OnMainWindowClose; 
        }
        else
        {
            var settingsWindow = AppHost.Services.GetRequiredService<SettingsWindow>();
            settingsWindow.Show();
        
            settingsWindow.Closed += (s, args) => 
            {
                if (settingsService.IsConfigured())
                {
                    var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
                    mainWindow.Show();
                    // Return to normal behavior
                    this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                }
                else
                {
                    Shutdown(); // They exited without finishing setup
                }
            };
        }
    }
}