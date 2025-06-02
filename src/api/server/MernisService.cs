using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Host;

public interface IMernisService
{
    Task<bool> VerifyIdentityAsync(string tckn, string firstName, string lastName, int birthYear);
}

public class MernisService : IMernisService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MernisService> _logger;
    private readonly bool _isEnabled;
    private readonly Uri _mernisUri;
    private readonly string _soapAction;

    public MernisService(HttpClient httpClient, ILogger<MernisService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _isEnabled = configuration.GetValue<bool>("MernisVerification:Enabled", false);
        
        var mernisUrl = configuration.GetValue<string>("MernisVerification:ServiceUrl", "https://tckimlik.nvi.gov.tr/Service/KPSPublic.asmx");
        _mernisUri = new Uri(mernisUrl);
        _soapAction = configuration.GetValue<string>("MernisVerification:SoapAction", "http://tckimlik.nvi.gov.tr/WS/TCKimlikNoDogrula");
        
        Console.WriteLine($"[DEBUG] MernisService created. Enabled: {_isEnabled}");
        _logger.LogInformation("MernisService initialized. Enabled: {IsEnabled}", _isEnabled);
    }

    public async Task<bool> VerifyIdentityAsync(string tckn, string firstName, string lastName, int birthYear)
    {
        Console.WriteLine($"[DEBUG] MernisService.VerifyIdentityAsync called. Enabled: {_isEnabled}");
        Console.WriteLine($"[DEBUG] Input parameters - TCKN: {tckn}, FirstName: '{firstName}', LastName: '{lastName}', BirthYear: {birthYear}");
        
        if (!_isEnabled)
        {
            Console.WriteLine("[DEBUG] MERNİS verification is disabled. Returning true.");
            _logger.LogWarning("MERNİS verification is disabled. Skipping identity verification.");
            return true; // Return true when disabled for testing
        }

        try
        {
            var soapRequest = CreateSoapRequest(tckn, firstName, lastName, birthYear);
            Console.WriteLine($"[DEBUG] Complete SOAP Request being sent to MERNİS:");
            Console.WriteLine(soapRequest);
            
            using var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", _soapAction);

            _logger.LogInformation("Verifying identity for TCKN: {TCKN}", tckn);

            var response = await _httpClient.PostAsync(_mernisUri, content);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[DEBUG] MERNİS API request failed with status: {response.StatusCode}");
                _logger.LogError("MERNİS API request failed with status: {StatusCode}", response.StatusCode);
                return false;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[DEBUG] MERNİS API Response:");
            Console.WriteLine(responseContent);
            
            var isVerified = ParseSoapResponse(responseContent);

            Console.WriteLine($"[DEBUG] MERNİS verification result for TCKN {tckn}: {isVerified}");
            _logger.LogInformation("MERNİS verification result for TCKN {TCKN}: {Result}", tckn, isVerified);
            
            return isVerified;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] MERNİS verification exception: {ex.Message}");
            _logger.LogError(ex, "Error occurred while verifying identity for TCKN: {TCKN}. Exception: {Message}", tckn, ex.Message);
            return false; // Fail safely - require manual verification if API fails
        }
    }

    private static string CreateSoapRequest(string tckn, string firstName, string lastName, int birthYear)
    {
        // Use Turkish culture for proper character conversion (i -> İ, not I)
        var turkishCulture = CultureInfo.GetCultureInfo("tr-TR");
        
        var firstNameUpper = firstName.ToUpper(turkishCulture);
        var lastNameUpper = lastName.ToUpper(turkishCulture);

        // Debug logging for Turkish character conversion
        Console.WriteLine($"[DEBUG] Turkish Character Conversion:");
        Console.WriteLine($"[DEBUG] Original firstName: '{firstName}' -> Upper: '{firstNameUpper}'");
        Console.WriteLine($"[DEBUG] Original lastName: '{lastName}' -> Upper: '{lastNameUpper}'");
        
#pragma warning disable S1075 // Hard-coded URIs are required for SOAP namespace definitions
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
               xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
               xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <TCKimlikNoDogrula xmlns=""http://tckimlik.nvi.gov.tr/WS"">
      <TCKimlikNo>{tckn}</TCKimlikNo>
      <Ad>{firstNameUpper}</Ad>
      <Soyad>{lastNameUpper}</Soyad>
      <DogumYili>{birthYear}</DogumYili>
    </TCKimlikNoDogrula>
  </soap:Body>
</soap:Envelope>";
#pragma warning restore S1075
    }

    private bool ParseSoapResponse(string responseContent)
    {
        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(responseContent);

            var namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
#pragma warning disable S1075 // Hard-coded URIs are required for SOAP namespace definitions
            namespaceManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            namespaceManager.AddNamespace("tckimlik", "http://tckimlik.nvi.gov.tr/WS");
#pragma warning restore S1075

            var resultNode = xmlDoc.SelectSingleNode("//tckimlik:TCKimlikNoDogrulaResult", namespaceManager);
            
            if (resultNode != null && bool.TryParse(resultNode.InnerText, out var result))
            {
                return result;
            }

            _logger.LogWarning("Could not parse MERNİS response: {Response}", responseContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing MERNİS response: {Response}", responseContent);
            return false;
        }
    }
} 
