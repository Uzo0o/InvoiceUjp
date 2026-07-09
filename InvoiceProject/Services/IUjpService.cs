using InvoiceProject.Models;

namespace InvoiceProject.Services;

public interface IUjpService
{
    Task<CompanyDto> GetCompanyDetailsAsync(string edb);
    Task<string> GetServerTimestampAsync();
    Task<string> SubmitInvoiceAsync(Invoice invoice);
}