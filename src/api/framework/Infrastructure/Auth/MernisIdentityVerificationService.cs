using System.Globalization;
using System.Text;
using System.Xml;
using FSH.Framework.Core.Auth.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Infrastructure.Auth;

public sealed class MernisIdentityVerificationService : IIdentityVerificationService
{
    private readonly ILogger<MernisIdentityVerificationService> _logger;
    private readonly HttpClient _httpClient;
    private readonly MernisServiceOptions _options;

    public MernisIdentityVerificationService(
        ILogger<MernisIdentityVerificationService> logger,
        IConfiguration configuration,
        HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        
        _options = configuration.GetSection("MernisService").Get<MernisServiceOptions>() 
            ?? throw new InvalidOperationException("MernisService configuration section is missing");
    }

    public async Task<bool> VerifyIdentityAsync(string tckn, string firstName, string lastName, int birthYear)
    {
        try
        {
            if (_options.UseDevelopmentMode)
            {
                _logger.LogInformation("MERNİS Development Mode: Simulating identity verification for TCKN: {Tckn}", tckn);
                return await SimulateMernisVerification(tckn, firstName, lastName, birthYear);
            }

            _logger.LogInformation("Calling real MERNİS service for identity verification");
            return await CallRealMernisService(tckn, firstName, lastName, birthYear);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during MERNİS identity verification for TCKN: {Tckn}", tckn);
            return false;
        }
    }

    private async Task<bool> SimulateMernisVerification(string tckn, string firstName, string lastName, int birthYear)
    {
        await Task.Delay(_options.SimulationDelayMs);
        
        if (!IsValidTCKN(tckn))
        {
            _logger.LogWarning("Invalid TCKN format: {Tckn}", tckn);
            return false;
        }

        if (_options.TestTcknFailures.Contains(tckn))
        {
            _logger.LogInformation("Development Mode: Simulating MERNİS verification failure for test TCKN");
            return false;
        }

        _logger.LogInformation("Development Mode: MERNİS verification successful for TCKN: {Tckn}, Name: {FirstName} {LastName}, Birth Year: {BirthYear}", 
            tckn, firstName, lastName, birthYear);
        
        return true;
    }

    private async Task<bool> CallRealMernisService(string tckn, string firstName, string lastName, int birthYear)
    {
        try
        {
            var soapRequest = CreateMernisSoapRequest(tckn, firstName, lastName, birthYear);
            
            _logger.LogInformation("MERNİS SOAP Request: {Request}", soapRequest);
            
            using var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", _options.SoapAction);

            var response = await _httpClient.PostAsync(_options.ServiceUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("MERNİS service returned error: {StatusCode}", response.StatusCode);
                return false;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("MERNİS Response: {Response}", responseContent);
            
            var result = ParseMernisResponse(responseContent);
            _logger.LogInformation("MERNİS Verification Result: {Result} for TCKN: {Tckn}, Name: {FirstName} {LastName}, Birth Year: {BirthYear}", 
                result, tckn, firstName, lastName, birthYear);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling MERNİS service");
            return false;
        }
    }

    private static string CreateMernisSoapRequest(string tckn, string firstName, string lastName, int birthYear)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
               xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
               xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <TCKimlikNoDogrula xmlns=""http://tckimlik.nvi.gov.tr/WS"">
      <TCKimlikNo>{tckn}</TCKimlikNo>
      <Ad>{firstName.ToUpper(CultureInfo.GetCultureInfo("tr-TR"))}</Ad>
      <Soyad>{lastName.ToUpper(CultureInfo.GetCultureInfo("tr-TR"))}</Soyad>
      <DogumYili>{birthYear.ToString(CultureInfo.InvariantCulture)}</DogumYili>
    </TCKimlikNoDogrula>
  </soap:Body>
</soap:Envelope>";
    }

    private bool ParseMernisResponse(string responseXml)
    {
        try
        {
            var pattern = @"<TCKimlikNoDogrulaResult>(true|false)</TCKimlikNoDogrulaResult>";
            var match = System.Text.RegularExpressions.Regex.Match(responseXml, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
            
            return match.Success && bool.Parse(match.Groups[1].Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing MERNİS response: {Message}", ex.Message);
            return false;
        }
    }
    
    private static bool IsValidTCKN(string tckn)
    {
        if (string.IsNullOrWhiteSpace(tckn) || tckn.Length != 11 || !tckn.All(char.IsDigit) || tckn[0] == '0')
        {
            return false;
        }

        var digits = tckn.Select(c => int.Parse(c.ToString(), CultureInfo.InvariantCulture)).ToArray();

        var sumOdd = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var sumEven = digits[1] + digits[3] + digits[5] + digits[7];

        var check1 = ((sumOdd * 7) - sumEven) % 10;
        var check2 = (sumOdd + sumEven + check1) % 10;

        return check1 == digits[9] && check2 == digits[10];
    }
}

public sealed class MernisServiceOptions
{
    public bool UseDevelopmentMode { get; init; }
    public Uri ServiceUrl { get; init; } = new Uri("http://localhost");
    public string SoapAction { get; init; } = string.Empty;
    public int SimulationDelayMs { get; init; } = 200;
    public HashSet<string> TestTcknFailures { get; init; } = new(StringComparer.Ordinal) { "12345678901" };
}