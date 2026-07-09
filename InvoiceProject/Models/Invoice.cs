using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using InvoiceProject.Models;

public class Invoice
{
    [JsonPropertyName("requestTimestamp")] public string RequestTimestamp { get; set; }
    [JsonPropertyName("document")] public InvoiceDocument Document { get; set; }
}

public class InvoiceDocument
{
    [JsonPropertyName("docHeader")] public InvoiceHeader Header { get; set; }
    [JsonPropertyName("docSeller")] public CompanyInfo Seller { get; set; }
    [JsonPropertyName("docBuyer")] public CompanyInfo Buyer { get; set; }
    
    [JsonPropertyName("docPayment")] public DocPaymentDto Payment { get; set; } // NEW
    [JsonPropertyName("docItems")] public List<DocItemDto> DocItems { get; set; }
    [JsonPropertyName("docTotals")] public DocTotals DocTotals { get; set; }
    [JsonPropertyName("vatTotals")] public List<VatTotalDto> VatTotals { get; set; } // NEW
}

public class InvoiceHeader
{
    [JsonPropertyName("docStorno")] public int DocStorno { get; set; }
    [JsonPropertyName("docType")] public string DocType { get; set; }
    [JsonPropertyName("docTypeName")] public string DocTypeName { get; set; }
    [JsonPropertyName("docDate")] public string DocDate { get; set; }
    [JsonPropertyName("docTurnoverDate")] public string DocTurnoverDate { get; set; }
    [JsonPropertyName("docNumber")] public string DocNumber { get; set; }
    [JsonPropertyName("docId")] public string DocId { get; set; }
}

public class CompanyInfo
{
    [JsonPropertyName("sellerCCode")] public string CCode { get; set; }
    [JsonPropertyName("sellerCName")] public string CName { get; set; }
    [JsonPropertyName("sellerTin")] public string Tin { get; set; }
    [JsonPropertyName("sellerVatNumber")] public string VatNumber { get; set; }
    [JsonPropertyName("sellerName")] public string Name { get; set; }
    [JsonPropertyName("sellerAddress")] public AddressDto Address { get; set; }
}

public partial class DocItem : ObservableObject
{
    [ObservableProperty] private int _lineNo;
    [ObservableProperty] private string _desc = string.Empty;

    // When these change, we tell the UI that the Totals changed too!
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowNetTotal))]
    [NotifyPropertyChangedFor(nameof(RowVatAmount))]
    [NotifyPropertyChangedFor(nameof(RowGrossTotal))]
    private decimal _qty = 1.0m;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowNetTotal))]
    [NotifyPropertyChangedFor(nameof(RowVatAmount))]
    [NotifyPropertyChangedFor(nameof(RowGrossTotal))]
    private decimal _unitPrice;

    // The user will pick this from a dropdown (e.g., "18", "5", "0")
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VatPercent))]
    [NotifyPropertyChangedFor(nameof(RowVatAmount))]
    [NotifyPropertyChangedFor(nameof(RowGrossTotal))]
    private string _taxIndicator = "DDV-G"; 

    public decimal RowNetTotal => Qty * UnitPrice;

    // Translate the indicator into actual math
    public decimal VatPercent => TaxIndicator switch
    {
        "18%" => 0.18m,
        "5%" => 0.05m,
        _ => 0.0m // DDV-G or others
    };

    public decimal RowVatAmount => RowNetTotal * VatPercent;
    public decimal RowGrossTotal => RowNetTotal + RowVatAmount;
}

public class DocTotals
{
    [JsonPropertyName("docNetAmount")] public decimal NetAmount { get; set; }
    [JsonPropertyName("docVatAmount")] public decimal VatAmount { get; set; }
    [JsonPropertyName("docGrossAmount")] public decimal GrossAmount { get; set; }
    [JsonPropertyName("docGrossAmountR")] public decimal GrossAmountR { get; set; }
    [JsonPropertyName("docFinalAmount")] public decimal FinalAmount { get; set; } // NEW
}

public class VatTotal
{
    [JsonPropertyName("vatTaxIndicator")] public string TaxIndicator { get; set; }
    [JsonPropertyName("vatPercent")] public decimal Percent { get; set; }
    [JsonPropertyName("vatAmount")] public decimal Amount { get; set; }
}

public class DocPayment
{
    [JsonPropertyName("docPaymentTypeCode")] public string PaymentTypeCode { get; set; }
    [JsonPropertyName("docPaymentTypeDesc")] public string PaymentTypeDesc { get; set; }
    [JsonPropertyName("docCurrency")] public string Currency { get; set; }
    [JsonPropertyName("docCurrencyCode")] public string CurrencyCode { get; set; }
    [JsonPropertyName("docCurrencyDate")] public string CurrencyDate { get; set; }
    [JsonPropertyName("docCurrencyExchRate")] public decimal CurrencyExchRate { get; set; }
}

public class DocItemDto
{
    [JsonPropertyName("docItemLineNo")] public int LineNo { get; set; }
    [JsonPropertyName("docItemSku")] public string Sku { get; set; } = "SKU-001"; // Or map from UI
    [JsonPropertyName("docItemDesc")] public string Desc { get; set; }
    [JsonPropertyName("docItemMUnit")] public string MUnit { get; set; } = "pcs";
    [JsonPropertyName("docItemQty")] public decimal Qty { get; set; }
    
    [JsonPropertyName("docItemUnitOriginalPriceWoVat")] public decimal UnitOriginalPriceWoVat { get; set; }
    [JsonPropertyName("docItemUnitPriceWoVat")] public decimal UnitPriceWoVat { get; set; }
    
    [JsonPropertyName("docItemUnitVat")] public decimal UnitVat { get; set; }
    [JsonPropertyName("docItemVat")] public decimal Vat { get; set; }
    [JsonPropertyName("docItemVatGroup")] public string VatGroup { get; set; }
    
    [JsonPropertyName("docItemTotalOriginalPriceWoVat")] public decimal TotalOriginalPriceWoVat { get; set; }
    [JsonPropertyName("docItemTotalPriceWoVat")] public decimal TotalPriceWoVat { get; set; }
    [JsonPropertyName("docItemTotalVat")] public decimal TotalVat { get; set; }
    [JsonPropertyName("docItemTotalPriceWVat")] public decimal TotalPriceWVat { get; set; }
    [JsonPropertyName("docItemTaxIndicator")] public string TaxIndicator { get; set; }
}

public class DocPaymentDto
{
    [JsonPropertyName("docPaymentTypeCode")] public string TypeCode { get; set; } = "P11";
    [JsonPropertyName("docPaymentTypeDesc")] public string TypeDesc { get; set; } = "Плаќање со картичка";
    [JsonPropertyName("docCurrency")] public string Currency { get; set; } = "MKD";
    [JsonPropertyName("docCurrencyCode")] public string CurrencyCode { get; set; } = "MKD";
    [JsonPropertyName("docCurrencyDate")] public string CurrencyDate { get; set; }
    [JsonPropertyName("docCurrencyExchRate")] public decimal ExchRate { get; set; } = 1;
}

public class VatTotalDto
{
    [JsonPropertyName("vatTaxIndicator")] public string TaxIndicator { get; set; }
    [JsonPropertyName("vatCode")] public string VatCode { get; set; }
    [JsonPropertyName("vatPercent")] public decimal VatPercent { get; set; }
    [JsonPropertyName("vatTaxableAmount")] public decimal TaxableAmount { get; set; }
    [JsonPropertyName("vatAmount")] public decimal Amount { get; set; }
    [JsonPropertyName("vatTotalAmount")] public decimal TotalAmount { get; set; }
}