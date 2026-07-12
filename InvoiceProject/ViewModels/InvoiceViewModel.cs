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
    public ObservableCollection<ClientRecord> AllClients { get; } = new();
    
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

        _ = LoadAllClientsAsync();
    }

    [RelayCommand]
    public async Task LoadAllClients()
    {
        var clients = await _databaseService.GetAllClientsAsync();
        AllClients.Clear();
        foreach(var c in clients) AllClients.Add(c);
    }
    private async Task LoadAllClientsAsync()
    {
        var clients = await _databaseService.GetAllClientsAsync();
        AllClients.Clear();
        foreach (var c in clients)
        {
            AllClients.Add(c);
        }
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
                    VatNumber = company.VatNumber ?? "", // SAVED
                    Name = company.Name,
                    Street = company.Address?.Street ?? "",
                    Number = company.Address?.Number ?? "-", 
                    City = company.Address?.City ?? "",
                    Zip = company.Address?.Zip ?? "1000",
                    CountryCode = company.CountryCode ?? "MK"
                });
                await  LoadAllClientsAsync();
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

        // THE FIX: Listen to the Qty and UnitPrice so the ViewModel actually does the math!
        newItem.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(DocItem.TaxIndicator) ||
                args.PropertyName == nameof(DocItem.UnitPrice) ||
                args.PropertyName == nameof(DocItem.Qty))
            {
                RecalculateTotals();
            }
        };

        InvoiceItems.Add(newItem);
        RecalculateTotals(); 
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

        if (string.IsNullOrWhiteSpace(BuyerEdb))
        {
            StatusMessage = "Please select a buyer first!";
            return;
        }

        try
        {
            StatusMessage = "Preparing submission...";
            var buyerRecord = await _databaseService.GetClientByEdbAsync(BuyerEdb);
            
            if (buyerRecord == null)
            {
                StatusMessage = "Buyer address not found. Please click 'Lookup in UJP API' first to save their details.";
                return;
            }

            string officialTimestamp = await _ujpService.GetServerTimestampAsync();
            var seller = _settingsService.CurrentSettings;

            // 1. THE FIX: Helper to translate your UI Dropdown into UJP API Codes
            string GetApiTaxCode(string uiTaxIndicator) => uiTaxIndicator switch
            {
                "18%" => "DDV-A",
                "5%" => "DDV-B",
                _ => "DDV-G"
            };

            // 2. THE FIX: Dynamically build the VAT Totals block by grouping the items
            var calculatedVatTotals = InvoiceItems
                .GroupBy(i => GetApiTaxCode(i.TaxIndicator))
                .Select(group => new
                {
                    vatTaxIndicator = group.Key,
                    vatCode = group.Key,
                    vatPercent = group.Key == "DDV-A" ? 18.0m : (group.Key == "DDV-B" ? 5.0m : 0.0m),
                    
                    // FIX: Wrapped in Math.Round(..., 2)
                    vatTaxableAmount = Math.Round(group.Sum(i => i.RowNetTotal), 2),
                    vatAmount = Math.Round(group.Sum(i => i.RowVatAmount), 2),
                    vatTotalAmount = Math.Round(group.Sum(i => i.RowGrossTotal), 2)
                }).ToArray();
            
            RecalculateTotals();

            // 3. Build the Payload using the translated codes
            var payload = new
            {
                requestTimestamp = officialTimestamp,
                document = new
                {
                    header = new
                    {
                        docStorno = 0,
                        docType = SelectedDocumentType,
                        docTypeName = "Фактура",
                        docDate = InvoiceDate.ToString("yyyy-MM-dd"),
                        docTurnoverDate = TurnoverDate.ToString("yyyy-MM-dd"),
                        docNumber = InvoiceNumber,
                        docId = Guid.NewGuid().ToString("N").Substring(0, 10) 
                    },
                    seller = new
                    {
                        sellerCCode = "MK",
                        sellerCName = "Северна Македонија",
                        sellerTin = seller.SellerEdb,
                        
                        sellerVatNumber = seller.SellerVatNumber, // INJECTED
                        
                        sellerName = seller.SellerName,
                        sellerAddress = new { streetAddress = seller.SellerStreet, streetNumber = seller.SellerNumber, postalCode = seller.SellerZip, city = seller.SellerCity }
                    },
                    buyer = new
                    {
                        buyerCCode = "MK",
                        buyerCName = "Северна Македонија",
                        buyerTin = buyerRecord.Edb,
                        
                        buyerVatNumber = buyerRecord.VatNumber, // INJECTED
                        
                        buyerName = buyerRecord.Name,
                        buyerAddress = new 
                        { 
                            streetAddress = string.IsNullOrWhiteSpace(buyerRecord.Street) ? "Б.Б." : buyerRecord.Street, 
                            streetNumber = string.IsNullOrWhiteSpace(buyerRecord.Number) ? "-" : buyerRecord.Number, 
                            postalCode = string.IsNullOrWhiteSpace(buyerRecord.Zip) ? "1000" : buyerRecord.Zip, 
                            city = buyerRecord.City 
                        }
                    },
                    docPayment = new
                    {
                        docPaymentTypeCode = "P11",
                        docPaymentTypeDesc = "Плаќање со картичка",
                        docCurrency = "MKD",
                        docCurrencyCode = "MKD",
                        docCurrencyDate = InvoiceDate.ToString("yyyy-MM-dd"),
                        docCurrencyExchRate = 1
                    },
                    docItems = InvoiceItems.Select(item => new
                    {
                        docItemLineNo = item.LineNo,
                        docItemSku = "SKU-" + item.LineNo,
                        docItemDesc = item.Desc,
                        docItemMUnit = "pcs",
                        docItemQty = Math.Round(item.Qty, 3), 
                        
                        docItemUnitOriginalPriceWoVat = Math.Round(item.UnitPrice, 2),
                        docItemUnitDiscountAmount = 0, 
                        docItemUnitPriceWoVat = Math.Round(item.UnitPrice, 2),
                        
                        // THE CRITICAL FIX: 
                        // UJP expects the Tax PERCENTAGE here (18.0, 5.0, 0.0), NOT the currency amount!
                        docItemUnitVat = item.TaxIndicator == "18%" ? 18.0m : (item.TaxIndicator == "5%" ? 5.0m : 0.0m),
                        docItemVat = item.TaxIndicator == "18%" ? 18.0m : (item.TaxIndicator == "5%" ? 5.0m : 0.0m),
                        
                        docItemVatGroup = GetApiTaxCode(item.TaxIndicator),
                        
                        docItemTotalOriginalPriceWoVat = Math.Round(item.RowNetTotal, 2),
                        docItemTotalPriceWoVat = Math.Round(item.RowNetTotal, 2),
                        
                        // THIS is where the actual currency amount goes
                        docItemTotalVat = Math.Round(item.RowVatAmount, 2), 
                        
                        docItemTotalPriceWVat = Math.Round(item.RowGrossTotal, 2),
                        docItemTaxIndicator = GetApiTaxCode(item.TaxIndicator),
                        docItemDomesticProduct = (string)null 
                    }).ToArray(),
                    docTotals = new
                    {
                        docNetAmount = Math.Round(NetAmount, 2),
                        docDiscountAmount = 0,
                        docNetAmountDisc = Math.Round(NetAmount, 2),
                        docVatAmount = Math.Round(VatAmount, 2),
                        docGrossAmount = Math.Round(GrossAmount, 2),
                        // ОВА Е КЛУЧНО: Мора да биде цел број (int) без децимали
                        docGrossAmountR = (int)Math.Round(GrossAmount, MidpointRounding.AwayFromZero),
                        docAvansAmount = 0,
                        docFinalAmount = Math.Round(GrossAmount, 2)
                    },
                    // Plug in the dynamic calculation we built at the top!
                    vatTotals = calculatedVatTotals
                }
            };
            var debugOptions = new System.Text.Json.JsonSerializerOptions 
            { 
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                WriteIndented = true 
            };
            string debugJson = System.Text.Json.JsonSerializer.Serialize(payload, debugOptions);
            
            Console.WriteLine("\n================= OUTGOING UJP PAYLOAD =================");
            Console.WriteLine(debugJson);
            
            System.Diagnostics.Debug.WriteLine("\n================= OUTGOING UJP PAYLOAD =================");
            System.Diagnostics.Debug.WriteLine(debugJson);
            // --- END DEBUG PRINT BLOCK ---

            // Now send it...
            string result = await _ujpService.SubmitInvoiceAsync(payload);
            StatusMessage = $"SUCCESS! Invoice Registered. UJP Response: {result}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"SUBMISSION FAILED: {ex.Message}";
        }
    }
}