using System.Text.Json.Serialization;

namespace InvoiceProject.Models;

public class CompanyDto
{
    [JsonPropertyName("taxNumber")] public string TaxNumber { get; set; } = string.Empty;
    [JsonPropertyName("vatNumber")] public string VatNumber { get; set; } = string.Empty;
    [JsonPropertyName("registrationNumber")] public string RegistrationNumber { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("address")] public AddressDto Address { get; set; } = new();
    [JsonPropertyName("countryCode")] public string CountryCode { get; set; } = "MK";
    [JsonPropertyName("municipality")] public string Municipality { get; set; } = string.Empty;
    [JsonPropertyName("einvoiceUser")] public bool EinvoiceUser { get; set; }
}