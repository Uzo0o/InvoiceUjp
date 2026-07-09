using System.Text.Json.Serialization;

namespace InvoiceProject.Models;

public class AddressDto
{
    [JsonPropertyName("street")]
    public string Street { get; set; }
    
    [JsonPropertyName("number")]
    public string Number { get; set; }
    
    [JsonPropertyName("city")]
    public string City { get; set; }
    
    [JsonPropertyName("zip")]
    public string Zip { get; set; }
}