using Amazon.Runtime.Internal.Util;
using DocumentFormat.OpenXml.Spreadsheet;
using Finbuckle.MultiTenant;
using FSH.WebApi.Application.Common.Interfaces;
using FSH.WebApi.Application.Common.PushNotifications;
using FSH.WebApi.Application.Common.PushNotifications.OneSignal;
using FSH.WebApi.Application.Multitenancy;
using FSH.WebApi.Infrastructure.Multitenancy;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Infrastructure.PushNotifications.OneSignal;

// Use via IPushNotificationServiceFactory.Create(). Do not use directly.

// Maybe use template method pattern? It could be usefull if we add another push notification provider.
public class OneSignalService : IOneSignalService
{
    private readonly TenantPushNotificationInfo _tenantPushNotificationInfo;
    private readonly IPushNotificationTemplateFactory _template;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OneSignalService> _logger;
    private readonly ISerializerService _serializer;

    public OneSignalService(
        FSHTenantInfo currentTenant,
        IPushNotificationTemplateFactory template,
        IHttpClientFactory httpClientFactory,
        ILogger<OneSignalService> logger,
        ISerializerService serializer)
    {
        _tenantPushNotificationInfo = currentTenant.PushNotificationInfo!; // null check is done in PushNotificationServiceFactory
        _template = template;

        _httpClient = httpClientFactory.CreateClient(PushNotificationsConstants.HttpClientName);
        _httpClient.BaseAddress = new Uri("https://onesignal.com/api/v1/notifications");
        _httpClient.DefaultRequestHeaders.Add(OneSignalConstants.AuthHeader, $"{_tenantPushNotificationInfo.AuthKey}");
        _logger = logger;
        _serializer = serializer;
    }

    public async Task SendTo(string userId, PushNotificationType notificationType)
        => await SendTo(new string[] { userId }, notificationType);

    public async Task SendTo(ICollection<string> userIds, PushNotificationType notificationType)
    {
        string json = GenerateJson(
            new Dictionary<string, ICollection<string>>
            {
                {
                    OneSignalConstants.IncludeExternalUserIds, userIds
                }
            },
            notificationType);

        await SendHttpRequest(json);
    }

    public Task SendToAll(PushNotificationType notificationType)
    {
        string json = GenerateJson(
            new Dictionary<string, ICollection<string>>
            {
                {
                 OneSignalConstants.IncludedSegments, new[] { OneSignalConstants.AllUsers }
                }
            },
            notificationType);

        return SendHttpRequest(json);
    }

    public Task SendToActiveUsers(PushNotificationType notificationType)
    {
        string json = GenerateJson(
            new Dictionary<string, ICollection<string>>
            {
                {
                 OneSignalConstants.IncludedSegments, new[] { OneSignalConstants.ActiveUsers }
                }
            },
            notificationType);

        return SendHttpRequest(json);
    }

    public Task SendToInactiveUsers(PushNotificationType notificationType)
    {
        string json = GenerateJson(
            new Dictionary<string, ICollection<string>>
            {
                {
                 OneSignalConstants.IncludedSegments, new[] { OneSignalConstants.InactiveUsers }
                }
            },
            notificationType);

        return SendHttpRequest(json);
    }

    public async Task SendHttpRequest(string json)
    {
        try
        {
            _logger.LogDebug($"Sending push notification request to OneSignal with JSON: {json}");

            string response = await (await _httpClient.PostAsync("", new StringContent(json, Encoding.UTF8, "application/json")))
                        .Content.ReadAsStringAsync();

            _logger.LogDebug($"OneSignal response: {response}");

            if (response.Contains("errors"))
            {
                throw new OneSignalException("OneSignal returned an error.", new List<string> { response });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error while sending push notification request to OneSignal with JSON: {json}");

            throw;
        }
    }

    private string GenerateJson(Dictionary<string, ICollection<string>> receiverInfo, PushNotificationType notificationType)
    {
        var (headingEN, contentEN, headingTR, contentTR) = _template.Create(notificationType);

        var notification = new
        {
            app_id = _tenantPushNotificationInfo.AppId,
            app_name = _tenantPushNotificationInfo.Name,
            receiverInfo,
            headings = new Dictionary<string, string>
        {
            { "en", headingEN },
            { "tr", headingTR }
        },
            contents = new Dictionary<string, string>
        {
            { "en", contentEN },
            { "tr", contentTR }
        },
            large_icon = _tenantPushNotificationInfo.IconUrl
        };

        return _serializer.Serialize(notification);
    }
}