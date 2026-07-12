using InvoiceProject.Models;

namespace InvoiceProject.Services;

public interface IDatabaseService
{
    Task SaveClientAsync(ClientRecord client);
    Task<List<ClientRecord>> SearchClientsByNameAsync(string searchQuery);
    Task<ClientRecord> GetClientByEdbAsync(string edb);
    
    Task<List<ClientRecord>> GetAllClientsAsync();
}