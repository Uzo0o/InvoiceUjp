using System.IO;
using InvoiceProject.Models;
using SQLite;

namespace InvoiceProject.Services;

public class DatabaseService : IDatabaseService
{
    private readonly SQLiteAsyncConnection _db;

    public DatabaseService()
    {
        // Save the database in the exact same IntegritiEFakturi AppData folder
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string myAppFolder = Path.Combine(appDataFolder, "IntegritiEFakturi");
        Directory.CreateDirectory(myAppFolder);
        
        string dbPath = Path.Combine(myAppFolder, "efakturi-data.db");
        
        // Initialize connection
        _db = new SQLiteAsyncConnection(dbPath);
        
        // This automatically creates the tables if they don't exist yet!
        _db.CreateTableAsync<ClientRecord>().Wait();
    }

    public async Task SaveClientAsync(ClientRecord client)
    {
        // InsertOrReplace ensures if the EDB already exists, it updates the address instead of crashing
        await _db.InsertOrReplaceAsync(client);
    }

    public async Task<List<ClientRecord>> SearchClientsByNameAsync(string searchQuery)
    {
        // Perform a case-insensitive SQL LIKE search
        return await _db.Table<ClientRecord>()
            .Where(c => c.Name.ToLower().Contains(searchQuery.ToLower()))
            .ToListAsync();
    }

    public async Task<ClientRecord> GetClientByEdbAsync(string edb)
    {
        return await _db.Table<ClientRecord>()
            .FirstOrDefaultAsync(c => c.Edb == edb);
    }
}