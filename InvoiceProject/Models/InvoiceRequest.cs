using System.Text.Json.Serialization;

namespace InvoiceProject.Models;

public class InvoiceRequest
{
    [JsonPropertyName("jws")] public string Jws { get; set; } // The wrapper required by the API
}