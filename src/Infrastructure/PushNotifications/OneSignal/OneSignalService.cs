using FSH.WebApi.Application.Common.Interfaces;
using FSH.WebApi.Application.Common.PushNotifications;
using FSH.WebApi.Infrastructure.Multitenancy;
using Microsoft.Extensions.Logging;
using System.Text;

namespace FSH.WebApi.Infrastructure.PushNotifications.OneSignal;

// Maybe use template method pattern with a BaseClass? It could be usefull if we add another push notification provider.
public class OneSignalService : IPushNotificationsService
{
    private readonly TenantPushNotificationsSettings _tenantPushNotificationSettings;
    private readonly IPushNotificationsTemplateFactory _template;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OneSignalService> _logger;
    private readonly ISerializerService _serializer;

    public OneSignalService(
        FSHTenantInfo currentTenant,
        IPushNotificationsTemplateFactory template,
        IHttpClientFactory httpClientFactory,
        ILogger<OneSignalService> logger,
        ISerializerService serializer)
    {
        _tenantPushNotificationSettings = currentTenant.PushNotificationsSettings!; // null check is done in PushNotificationServiceFactory
        _template = template;

        _httpClient = httpClientFactory.CreateClient(PushNotificationsConstants.HttpClientName);
        _httpClient.BaseAddress = new Uri(OneSignalConstants.Url);
        _httpClient.DefaultRequestHeaders.Add(OneSignalConstants.AuthHeader, $"{_tenantPushNotificationSettings.AuthKey}");
        _logger = logger;
        _serializer = serializer;
    }

    public Task SendTo(string userId, ICollection<Message> messages)
    {
        string json = GenerateJson(
            messages,
            new KeyValuePair<string, ICollection<string>>(OneSignalConstants.IncludeExternalUserIds, new[] { userId }));

        return SendHttpRequest(json);
    }

    public Task SendTo(string userId, string templateName)
    {
        var template = _template.Create(templateName);
        return SendTo(userId, template.Messages);
    }

    public Task SendToAll(ICollection<Message> messages)
    {
        string json = GenerateJson(
            messages,
            new KeyValuePair<string, ICollection<string>>(OneSignalConstants.IncludedSegments, new[] { OneSignalConstants.AllUsers }));

        return SendHttpRequest(json);
    }

    public Task SendToAll(string templateName)
    {
        var template = _template.Create(templateName);
        return SendToAll(template.Messages);
    }

    public Task SendToActiveUsers(ICollection<Message> messages)
    {
        string json = GenerateJson(
            messages,
            new KeyValuePair<string, ICollection<string>>(OneSignalConstants.IncludedSegments, new[] { OneSignalConstants.ActiveUsers }));

        return SendHttpRequest(json);
    }

    public Task SendToActiveUsers(string templateName)
    {
        var template = _template.Create(templateName);
        return SendToActiveUsers(template.Messages);
    }

    public Task SendToInactiveUsers(ICollection<Message> messages)
    {
        string json = GenerateJson(
            messages,
            new KeyValuePair<string, ICollection<string>>(OneSignalConstants.IncludedSegments, new[] { OneSignalConstants.InactiveUsers }));

        return SendHttpRequest(json);
    }

    public Task SendToInactiveUsers(string templateName)
    {
        var template = _template.Create(templateName);
        return SendToInactiveUsers(template.Messages);
    }

    public async Task SendHttpRequest(string json)
    {
        try
        {
            _logger.LogDebug($"Sending push notification request to OneSignal with JSON: {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("", content);
            response.EnsureSuccessStatusCode();
            string responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug($"OneSignal's response: {responseContent}");

            if (responseContent.Contains("errors"))
            {
                throw new OneSignalException("OneSignal returned an error.", new List<string> { responseContent });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error while sending push notification request to OneSignal with JSON: {json}");

            throw;
        }
    }

    private string GenerateJson(ICollection<Message> messages, KeyValuePair<string, ICollection<string>> receiver)
    {
        var notification = new Dictionary<string, object>
        {
            { OneSignalConstants.AppId, _tenantPushNotificationSettings.AppId },
            { OneSignalConstants.AppName, _tenantPushNotificationSettings.Name },
            { receiver.Key, receiver.Value },
            { OneSignalConstants.Headings, new Dictionary<string, string>() },
            { OneSignalConstants.Contents, new Dictionary<string, string>() },
            { OneSignalConstants.LargeIcon, _tenantPushNotificationSettings.IconUrl }
        };

        var contentsEntry = (Dictionary<string, string>)notification[OneSignalConstants.Contents];
        var headingsEntry = (Dictionary<string, string>)notification[OneSignalConstants.Headings];

        foreach (Message message in messages)
        {
            contentsEntry.Add(message.Language, message.Content);
            headingsEntry.Add(message.Language, message.Heading);
        }

        return _serializer.Serialize(notification);
    }
}