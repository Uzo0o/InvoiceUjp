using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;
using InvoiceProject.Models;
using Jose;
using Microsoft.Extensions.Configuration;

namespace InvoiceProject.Services;

public class UjpService : IUjpService
{
    private readonly HttpClient _baseHttpClient; // Used for non-mTLS lookups
    private readonly IConfiguration _config;
    private readonly IUserSettingsService _settingsService;

    public UjpService(HttpClient httpClient, IConfiguration config, IUserSettingsService settingsService)
    {
        _baseHttpClient = httpClient;
        _config = config;
        _settingsService = settingsService;
        _baseHttpClient.BaseAddress = new Uri(_config["UjpSettings:BaseUrl"] ?? "https://efakturatest.ujp.gov.mk");
    }
    
    public async Task<string> GetServerTimestampAsync()
    {
        
        var response = await _baseHttpClient.GetAsync("einvoice_api/api/v1/server-time");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ServerTimeResponse>();
        
        if (result == null || string.IsNullOrWhiteSpace(result.Timestamp))
        {
            throw new Exception("Failed to parse the server timestamp from UJP.");
        }

        return result.Timestamp;
    }

    // Add this helper class at the bottom near your CompanyResponse class
    internal class ServerTimeResponse
    {
        [JsonPropertyName("timestamp")] 
        public string Timestamp { get; set; }
    }

    // Company lookup works over standard HTTP without transport certs
    public async Task<CompanyDto> GetCompanyDetailsAsync(string edb)
    {
        var response = await _baseHttpClient.GetAsync($"einvoice_api/api/v1/companies/{edb}");
        response.EnsureSuccessStatusCode();

        var wrapper = await response.Content.ReadFromJsonAsync<CompanyResponse>();
        return wrapper.Company;
    }

    public async Task<string> SubmitInvoiceAsync(Invoice invoice)
    {
        try
        {
            var settings = _settingsService.CurrentSettings;
            
            // 1. Serialize the inner document data
            var jsonOptions = new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };
            string jsonString = JsonSerializer.Serialize(invoice, jsonOptions);

            // 2. Cryptographically sign the document string
            string signedJws = SignPayload(jsonString);

            // 3. Resolve the user's specific certificate (File or USB Token)
            using X509Certificate2 cert = GetUserCertificate();

            // 4. Construct an isolated network handler for Mutual TLS (mTLS)
            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(cert);

            // 5. Build an isolated HttpClient bound to this specific certificate channel
            string baseUrl = _config["UjpSettings:BaseUrl"] ?? "https://efakturatest.ujp.gov.mk";
            using var client = new HttpClient(handler);
            client.BaseAddress = new Uri(baseUrl);

            // 6. Inject the exact custom headers from your working console prototype
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("X-EDB", settings.SellerEdb);
            client.DefaultRequestHeaders.Add("X-EUJP-ID", settings.EujpId);
            client.DefaultRequestHeaders.Add("X-SERIAL-NUMBER", cert.SerialNumber);

            // 7. Encase the signed JWS string inside the required outer schema wrapper
            var wrapper = new { jws = signedJws };
            string payloadJson = JsonSerializer.Serialize(wrapper);
            var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

            string endpoint = "/JSONReceiver/api/v1/sales-invoices/send";
            
            // Transmit the payload over the secured mTLS channel
            var response = await client.PostAsync(endpoint, content);
            string responseContent = await response.Content.ReadAsStringAsync();

            // Force evaluation trace directly to the compiler terminal window
            System.Diagnostics.Debug.WriteLine("================= UJP RAW RESPONSE START =================");
            System.Diagnostics.Debug.WriteLine($"HTTP STATUS: {(int)response.StatusCode} ({response.StatusCode})");
            System.Diagnostics.Debug.WriteLine($"JSON BODY  : {responseContent}");
            System.Diagnostics.Debug.WriteLine("================= UJP RAW RESPONSE END ===================");

            Console.WriteLine($"[UJP API OUT] Status: {(int)response.StatusCode} | Body: {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"UJP Gateway validation failure: {(int)response.StatusCode}");
            }

            return responseContent;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CRITICAL EXCEPTION IN SUBMIT]: {ex.Message}");
            Console.WriteLine($"[CRITICAL EXCEPTION IN SUBMIT]: {ex.Message}");
            throw;
        }
    }

    private X509Certificate2 GetUserCertificate()
    {
        var settings = _settingsService.CurrentSettings;

        if (!string.IsNullOrWhiteSpace(settings.CertThumbprint))
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(X509FindType.FindByThumbprint, settings.CertThumbprint, false);
            if (certs.Count > 0) return new X509Certificate2(certs[0]);
            
            throw new Exception("USB Hardware Token missing from the local system store.");
        }

        if (!string.IsNullOrWhiteSpace(settings.CertPath))
        {
            return new X509Certificate2(settings.CertPath, settings.CertPassword);
        }

        throw new Exception("No active digital certificate identity configured.");
    }

    private string SignPayload(string jsonContent)
    {
        var headers = new Dictionary<string, object> { { "alg", "RS256" }, { "typ", "JWT" } };
        using X509Certificate2 cert = GetUserCertificate();
        using RSA privateKey = cert.GetRSAPrivateKey();
        
        if (privateKey == null)
        {
            throw new Exception("The configured certificate does not contain an exportable or accessible RSA private key resource.");
        }

        return JWT.Encode(jsonContent, privateKey, JwsAlgorithm.RS256, extraHeaders: headers);
    }

    internal class CompanyResponse
    {
        [JsonPropertyName("company")] public CompanyDto Company { get; set; }
    }
}