namespace InvoiceProject.Models;

public class UserSettings
{
    public string CertPath { get; set; } = string.Empty;
    
    // Note: In a banking/military app, you'd encrypt this password. 
    // For now, plain text in AppData is standard for desktop tools.
    public string CertPassword { get; set; } = string.Empty; 
    
    public string EujpId { get; set; } = string.Empty;
    public string SellerEdb { get; set; } = string.Empty;
    
    // Cached UJP Data so we don't have to query it every time the app opens
    public string SellerName { get; set; } = string.Empty;
    public string SellerStreet { get; set; } = string.Empty;
    public string SellerCity { get; set; } = string.Empty;
    
    public string CertThumbprint { get; set; } = string.Empty;
}