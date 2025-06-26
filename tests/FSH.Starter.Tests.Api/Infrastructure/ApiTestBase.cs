using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;
using FluentAssertions;
using Xunit.Abstractions;

namespace FSH.Starter.Tests.Api.Infrastructure;

public abstract class ApiTestBase : IDisposable
{
    protected readonly HttpClient HttpClient;
    protected readonly TestDataManager TestDataManager;
    protected readonly ITestOutputHelper Output;

    protected ApiTestBase(ITestOutputHelper output)
    {
        Output = output;
        
        // Load test configuration
        var configuration = LoadTestConfiguration();
        
        // Create HTTP client
        var baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
        HttpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        
        var timeout = int.Parse(configuration["ApiSettings:RequestTimeoutSeconds"] ?? "30");
        HttpClient.Timeout = TimeSpan.FromSeconds(timeout);
        
        // Initialize test data manager
        TestDataManager = new TestDataManager(configuration);
        
        Output.WriteLine($"🚀 API Test initialized - Base URL: {baseUrl}");
    }

    private static IConfiguration LoadTestConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("testsettings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables();

        return builder.Build();
    }

    protected async Task<HttpResponseMessage> PostJsonAsync<T>(string endpoint, T data)
    {
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        Output.WriteLine($"📤 POST {endpoint}");
        Output.WriteLine($"📝 Request Body: {json}");
        
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await HttpClient.PostAsync(endpoint, content);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        Output.WriteLine($"📥 Response ({response.StatusCode}): {responseContent}");
        
        return response;
    }

    protected async Task<HttpResponseMessage> GetAsync(string endpoint)
    {
        Output.WriteLine($"📤 GET {endpoint}");
        
        var response = await HttpClient.GetAsync(endpoint);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        Output.WriteLine($"📥 Response ({response.StatusCode}): {responseContent}");
        
        return response;
    }

    protected async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(content);
    }

    protected static void LogTestStart(ITestOutputHelper output, string testName)
    {
        output.WriteLine("");
        output.WriteLine($"🧪 === {testName} ===");
        output.WriteLine($"⏰ Started at: {DateTime.Now:HH:mm:ss.fff}");
        output.WriteLine("");
    }

    protected static void LogTestEnd(ITestOutputHelper output, string testName, bool success)
    {
        output.WriteLine("");
        output.WriteLine($"✅ {testName} - {(success ? "PASSED" : "FAILED")}");
        output.WriteLine($"⏰ Ended at: {DateTime.Now:HH:mm:ss.fff}");
        output.WriteLine("");
    }

    public virtual void Dispose()
    {
        HttpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
} 