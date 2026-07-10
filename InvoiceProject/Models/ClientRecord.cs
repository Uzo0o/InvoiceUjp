using SQLite;

namespace InvoiceProject.Models;

public class ClientRecord
{
    [PrimaryKey, MaxLength(15)]
    public string Edb { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty; // NEW
    public string City { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;        // NEW
    public string CountryCode { get; set; } = "MK";        // NEW
}