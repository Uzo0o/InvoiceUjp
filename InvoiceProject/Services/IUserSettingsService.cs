using InvoiceProject.Models;

namespace InvoiceProject.Services;

public interface IUserSettingsService
{
    UserSettings CurrentSettings { get; }
    void SaveSettings(UserSettings settings);
    bool IsConfigured();
}