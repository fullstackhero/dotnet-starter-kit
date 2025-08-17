using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace FSH.Starter.Api.Services;

public class OpenAiLlmService : ILlmService
{
    private readonly OpenAiOptions _options;
    private readonly HttpClient _http;

    public OpenAiLlmService(IOptions<OpenAiOptions> options, HttpClient http)
    {
        _options = options.Value;
        _http = http;
    }

    public async Task<string> AnswerAsync(string userMessage, string context)
    {
        var url = "https://api.openai.com/v1/chat/completions";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var payload = new
        {
            model = _options.Model,
            messages = new object[]
            {
                new { role = "system", content = "You are a helpful assistant for SMBs. Prefer answers from tenant FAQs context; ask clarifying questions if unsure." },
                new { role = "user", content = $"Context (FAQs):\n{context}\n\nUser: {userMessage}" }
            },
            temperature = 0.2
        };

        var json = JsonSerializer.Serialize(payload);
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        using var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        using var stream = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}
