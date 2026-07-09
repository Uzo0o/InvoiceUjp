using System.IO;
using System.Text.Json;
using InvoiceProject.Models;

namespace InvoiceProject.Services;

public class UserSettingsService : IUserSettingsService
{
    private readonly string _settingsFilePath;
    public UserSettings CurrentSettings { get; private set; }

    public UserSettingsService()
    {
        // This gets the C:\Users\Username\AppData\Local folder
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        
        // Create a dedicated folder for your software
        string myAppFolder = Path.Combine(appDataFolder, "IntegritiEFakturi");
        Directory.CreateDirectory(myAppFolder); // Creates it if it doesn't exist
        
        _settingsFilePath = Path.Combine(myAppFolder, "user-settings.json");
        LoadSettings();
    }

    private void LoadSettings()
    {
        if (File.Exists(_settingsFilePath))
        {
            string json = File.ReadAllText(_settingsFilePath);
            CurrentSettings = JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
        }
        else
        {
            CurrentSettings = new UserSettings();
        }
    }

    public void SaveSettings(UserSettings settings)
    {
        CurrentSettings = settings;
        string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsFilePath, json);
    }

    // Helper to check if the user needs to see the setup screen on startup
    public bool IsConfigured()
    {
        // Check if they have EITHER a File OR a USB Thumbprint
        bool hasCert = !string.IsNullOrWhiteSpace(CurrentSettings.CertPath) || 
                       !string.IsNullOrWhiteSpace(CurrentSettings.CertThumbprint);
                       
        // They must have a cert AND an EDB
        return hasCert && !string.IsNullOrWhiteSpace(CurrentSettings.SellerEdb);
    }
}