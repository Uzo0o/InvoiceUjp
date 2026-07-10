namespace InvoiceProject.Models;

public class UserSettings
{
    public string CertPath { get; set; } = string.Empty;
    public string CertPassword { get; set; } = string.Empty; 
    public string CertThumbprint { get; set; } = string.Empty;
    public string EujpId { get; set; } = string.Empty;
    public string SellerEdb { get; set; } = string.Empty;
    
    // Cached UJP Data
    public string SellerName { get; set; } = string.Empty;
    public string SellerStreet { get; set; } = string.Empty;
    public string SellerNumber { get; set; } = string.Empty; // NEW
    public string SellerCity { get; set; } = string.Empty;
    public string SellerZip { get; set; } = string.Empty;    // NEW
}