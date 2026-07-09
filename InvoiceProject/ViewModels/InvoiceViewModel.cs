using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoiceProject.Models;
using InvoiceProject.Services;

namespace InvoiceProject.ViewModels;

public partial class InvoiceViewModel : ObservableObject
{
    private readonly IUjpService _ujpService;
    private readonly IDatabaseService _databaseService;
    private readonly IUserSettingsService _settingsService;

    public ObservableCollection<ClientRecord> SearchResults { get; } = new();
    
    [ObservableProperty] private string _searchQuery;
    [ObservableProperty] private ClientRecord _selectedClient;

    // These properties map directly to your XAML
    [ObservableProperty] private string _buyerEdb;

    [ObservableProperty] private string _buyerName;

    [ObservableProperty] private string _statusMessage; 
    
    [ObservableProperty] private string _invoiceNumber;

    [ObservableProperty] private DateTime _invoiceDate = DateTime.Today;
    [ObservableProperty] private DateTime _turnoverDate = DateTime.Today;

    [ObservableProperty] private string _selectedDocumentType = "100";
    
    [ObservableProperty] private decimal _netAmount;
    [ObservableProperty] private decimal _vatAmount;
    [ObservableProperty] private decimal _grossAmount;
    
    // Run this inside your ViewModel constructor to generate a unique test number
    private void InitializeHeaderDefaults()
    {
        // Production tip: Real systems track sequential numbers in a database, 
        // but a timestamp-based string works perfectly for sandbox testing.
        InvoiceNumber = $"INV-{DateTime.Now:yyyyMMddHHmmss}";
    }
    
    // The DI container automatically hands this ViewModel the UjpService!
    public InvoiceViewModel(IUjpService ujpService, IDatabaseService databaseService,
        IUserSettingsService settingsService)
    {
        _ujpService = ujpService;
        _databaseService = databaseService;
        _settingsService = settingsService;
        InitializeHeaderDefaults();
    }
    [RelayCommand]
    private async Task SearchLocalClientsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            SearchResults.Clear();
            return;
        }

        var results = await _databaseService.SearchClientsByNameAsync(SearchQuery);
        
        SearchResults.Clear();
        foreach (var client in results)
        {
            SearchResults.Add(client);
        }
    }

    // 2. The Auto-Fill Magic
    // CommunityToolkit automatically calls this method when _selectedClient changes!
    partial void OnSelectedClientChanged(ClientRecord value)
    {
        if (value != null)
        {
            BuyerEdb = value.Edb;
            BuyerName = value.Name;
            StatusMessage = "Loaded from Local Address Book.";
        }
    }

    // 3. Update your EXISTING LookupCompanyAsync to SAVE to the database
    [RelayCommand]
    private async Task LookupCompanyAsync()
    {
        if (string.IsNullOrWhiteSpace(BuyerEdb)) return;

        try
        {
            StatusMessage = "Looking up company in UJP Database...";
            var company = await _ujpService.GetCompanyDetailsAsync(BuyerEdb);
            
            if (company != null)
            {
                BuyerName = company.Name;
                StatusMessage = "Company found and saved to Address Book!";

                // SILENTLY SAVE TO SQLITE!
                await _databaseService.SaveClientAsync(new ClientRecord 
                {
                    Edb = BuyerEdb,
                    Name = company.Name,
                    Street = company.Address?.Street ?? "",
                    City = company.Address?.City ?? ""
                });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    public ObservableCollection<DocItem> InvoiceItems { get; } = new();

    [RelayCommand]
    private void AddBlankItem()
    {
        var newItem = new DocItem 
        { 
            LineNo = InvoiceItems.Count + 1,
            Desc = "New Item",
            Qty = 1.0m,
            UnitPrice = 0.0m
        };

        // THE MAGIC: Subscribe to the item's property changes
        newItem.PropertyChanged += (sender, args) =>
        {
            // If the user changes Qty or UnitPrice, RowTotal updates, so we recount everything
            if (args.PropertyName == nameof(DocItem.TaxIndicator))
            {
                RecalculateTotals();
            }
        };

        InvoiceItems.Add(newItem);
        RecalculateTotals(); // Calculate immediately when added
    }
    
    
    private void RecalculateTotals()
    {
        decimal currentNet = 0;
        decimal currentVat = 0;
        
        foreach (var item in InvoiceItems)
        {
            currentNet += item.RowNetTotal;
            currentVat += item.RowVatAmount;
        }

        NetAmount = currentNet;
        VatAmount = currentVat; 
        GrossAmount = NetAmount + VatAmount;
    }
    
    [RelayCommand]
    private async Task SubmitInvoiceAsync()
    {
        if (InvoiceItems.Count == 0)
        {
            StatusMessage = "Cannot submit an empty invoice!";
            return;
        }

        try
        {
            StatusMessage = "Signing and Submitting to UJP...";
            string officialTimestamp = await _ujpService.GetServerTimestampAsync();
            
            // 1. Get the permanent Seller Settings we saved earlier
            var seller = _settingsService.CurrentSettings; // Assuming you made the settings accessible

            // 2. Build the Payload matching the API Schema exactly
            var invoicePayload = new Invoice
            {
                RequestTimestamp = officialTimestamp,
                Document = new InvoiceDocument
                {
                    Header = new InvoiceHeader
                    {
                        DocType = SelectedDocumentType,
                        DocTypeName = "Фактура", // Added from console
                        DocDate = InvoiceDate.ToString("yyyy-MM-dd"),
                        DocTurnoverDate = TurnoverDate.ToString("yyyy-MM-dd"),
                        DocNumber = InvoiceNumber,
                        DocStorno = 0
                    },
                    Seller = new CompanyInfo
                    {
                        CCode = "MK", CName = "Северна Македонија",
                        Tin = seller.SellerEdb, Name = seller.SellerName,
                        Address = new AddressDto { Street = seller.SellerStreet, City = seller.SellerCity }
                    },
                    Buyer = new CompanyInfo
                    {
                        CCode = "MK", CName = "Северна Македонија",
                        Tin = BuyerEdb, Name = BuyerName,
                        Address = new AddressDto { Street = "Unknown", City = "Unknown" } // Update based on your DB if you save this
                    },
                    Payment = new DocPaymentDto
                    {
                        CurrencyDate = InvoiceDate.ToString("yyyy-MM-dd")
                    },
                    DocTotals = new DocTotals
                    {
                        NetAmount = NetAmount,
                        VatAmount = VatAmount,
                        GrossAmount = GrossAmount,
                        GrossAmountR = Math.Round(GrossAmount, 0),
                        FinalAmount = Math.Round(GrossAmount, 0)
                    },
                    VatTotals = new List<VatTotalDto>
                    {
                        // For DDV-G (No VAT). If you have mixed rates, you'll need to group by TaxIndicator
                        new VatTotalDto
                        {
                            TaxIndicator = "DDV-G",
                            VatCode = "DDV-G",
                            VatPercent = 0.0m,
                            TaxableAmount = NetAmount,
                            Amount = 0.0m,
                            TotalAmount = NetAmount
                        }
                    },
                    DocItems = InvoiceItems.Select(item => new DocItemDto
                    {
                        LineNo = item.LineNo,
                        Desc = item.Desc,
                        Qty = item.Qty,
                        
                        // Map the console app's required pricing fields
                        UnitOriginalPriceWoVat = item.UnitPrice,
                        UnitPriceWoVat = item.UnitPrice,
                        UnitVat = item.RowVatAmount > 0 ? (item.RowVatAmount / item.Qty) : 0,
                        Vat = item.RowVatAmount > 0 ? (item.RowVatAmount / item.Qty) : 0,
                        VatGroup = item.TaxIndicator,
                        
                        TotalOriginalPriceWoVat = item.RowNetTotal,
                        TotalPriceWoVat = item.RowNetTotal,
                        TotalVat = item.RowVatAmount,
                        TotalPriceWVat = item.RowGrossTotal,
                        TaxIndicator = item.TaxIndicator
                    }).ToList()
                }
            };
            
            // 3. Send it through the encrypted pipeline
            string result = await _ujpService.SubmitInvoiceAsync(invoicePayload);
            
            StatusMessage = $"SUCCESS! Invoice Registered. UJP Response: {result}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"SUBMISSION FAILED: {ex.Message}";
        }
    }
}