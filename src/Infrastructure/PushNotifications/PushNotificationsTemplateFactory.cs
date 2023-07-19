using FSH.WebApi.Application.Common.Interfaces;
using FSH.WebApi.Application.Common.PushNotifications;
using Microsoft.Extensions.Logging;

namespace FSH.WebApi.Infrastructure.PushNotifications;

public class PushNotificationsTemplateFactory : IPushNotificationsTemplateFactory
{
    private readonly ISerializerService _serializer;
    private readonly ILogger<PushNotificationsTemplateFactory> _logger;

    public PushNotificationsTemplateFactory(ISerializerService serializer, ILogger<PushNotificationsTemplateFactory> logger)
    {
        _serializer = serializer;
        _logger = logger;
    }

    public PushNotificationTemplate Create(string templateName)
    {
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string tmplFolder = Path.Combine(baseDirectory, "PushNotifications Templates");
        string filePath = Path.Combine(tmplFolder, $"{templateName}.json");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Template file not found.", filePath);
        }

        string json = File.ReadAllText(filePath);

        try
        {
            return _serializer.Deserialize<PushNotificationTemplate>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deserializing template file: {filePath}");
            throw;
        }
    }
}