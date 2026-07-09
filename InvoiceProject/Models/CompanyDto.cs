using System.Text.Json.Serialization;

namespace InvoiceProject.Models;

public class CompanyDto
{
    [JsonPropertyName("taxNumber")] public string TaxNumber { get; set; }
    [JsonPropertyName("vatNumber")] public string VatNumber { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("address")] public AddressDto Address { get; set; }
}