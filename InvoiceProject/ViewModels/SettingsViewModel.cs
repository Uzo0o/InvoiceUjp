using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows;
using InvoiceProject.Services;
using InvoiceProject.Models;

namespace InvoiceProject.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IUserSettingsService _settingsService;
    private readonly IUjpService _ujpService;
    public Action CloseAction { get; set; }

    [ObservableProperty] private string _certPath;
    [ObservableProperty] private string _certPassword;
    [ObservableProperty] private string _certThumbprint; // NEW
    [ObservableProperty] private string _eujpId;
    [ObservableProperty] private string _sellerEdb;
    [ObservableProperty] private string _statusMessage;

    public SettingsViewModel(IUserSettingsService settingsService, IUjpService ujpService)
    {
        _settingsService = settingsService;
        _ujpService = ujpService;
        
        var current = _settingsService.CurrentSettings;
        CertPath = current.CertPath;
        CertPassword = current.CertPassword;
        CertThumbprint = current.CertThumbprint; // NEW
        EujpId = current.EujpId;
        SellerEdb = current.SellerEdb;
    }

    [RelayCommand]
    private void SelectUsbCertificate()
    {
        try
        {
            using X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            X509Certificate2Collection selectedCerts = X509Certificate2UI.SelectFromCollection(
                store.Certificates, 
                "Select your Certificate", 
                "Please select your KIBS/Telekom USB Certificate from the list.", 
                X509SelectionFlag.SingleSelection);

            if (selectedCerts.Count > 0)
            {
                // Save the thumbprint and clear the file path so the app knows which one to use
                CertThumbprint = selectedCerts[0].Thumbprint;
                CertPath = string.Empty; 
                CertPassword = string.Empty; // USB handles its own PIN, no password needed here
                StatusMessage = "USB Certificate linked successfully!";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"USB Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void BrowseCertificate()
    {
        var dialog = new OpenFileDialog { Filter = "Certificates (*.pfx)|*.pfx" };
        if (dialog.ShowDialog() == true)
        {
            CertPath = dialog.FileName;
            CertThumbprint = string.Empty; // Clear USB if they pick a file
        }
    }

    [RelayCommand]
    private async Task SaveAndVerifyAsync()
    {
        // Validation: Must have EDB and EITHER a File OR a Thumbprint
        bool hasCert = !string.IsNullOrWhiteSpace(CertPath) || !string.IsNullOrWhiteSpace(CertThumbprint);
        
        if (!hasCert || string.IsNullOrWhiteSpace(SellerEdb))
        {
            StatusMessage = "Please provide a certificate and your EDB.";
            return;
        }

        try
        {
            StatusMessage = "Verifying company with UJP...";
            var company = await _ujpService.GetCompanyDetailsAsync(SellerEdb);

            var newSettings = new UserSettings
            {
                CertPath = CertPath,
                CertPassword = CertPassword,
                CertThumbprint = CertThumbprint, // Save the thumbprint
                EujpId = EujpId,
                SellerEdb = SellerEdb,
                SellerName = company?.Name ?? "Unknown",
                SellerStreet = company?.Address?.Street ?? "",
                SellerCity = company?.Address?.City ?? ""
            };
            
            _settingsService.SaveSettings(newSettings);

            CloseAction?.Invoke();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Verification Failed: {ex.Message}";
        }
    }
}