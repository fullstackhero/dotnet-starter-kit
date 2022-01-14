using System.Globalization;
using FSH.WebApi.Application.Common.Caching;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FSH.WebApi.Infrastructure.Localization;

public class JsonStringLocalizer : IStringLocalizer
{
    private string Localization => "Localization";

    private string DefaultCulture => "en-US";

    private readonly ICacheService _cache;

    private readonly JsonSerializer _serializer = new();

    public JsonStringLocalizer(ICacheService cache)
    {
        _cache = cache;
    }

    public LocalizedString this[string name]
    {
        get
        {
            string? value = GetString(name);
            return new LocalizedString(name, value ?? $"{name}", value == null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var actualValue = this[name];
            return !actualValue.ResourceNotFound
                ? new LocalizedString(name, string.Format(actualValue.Value, arguments), false)
                : actualValue;
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        string filePath = $"{Localization}/{Thread.CurrentThread.CurrentCulture.Name}.json";
        using var str = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var sReader = new StreamReader(str);
        using var reader = new JsonTextReader(sReader);
        while (reader.Read())
        {
            if (reader.TokenType != JsonToken.PropertyName || reader.Value is null)
                continue;
            string key = (string)reader.Value;
            reader.Read();
            string? value = _serializer.Deserialize<string>(reader);
            if (value is not null)
            {
                yield return new LocalizedString(key, value, false);
            }
        }
    }

    private string? GetString(string key)
    {
        string? stringCulture = GetSpecificCulture(key);
        if (!string.IsNullOrEmpty(stringCulture)) return stringCulture;
        stringCulture = GetNaturalCulture(key);
        if (!string.IsNullOrEmpty(stringCulture)) return stringCulture;
        stringCulture = GetDefaultCulture(key);
        if (!string.IsNullOrEmpty(stringCulture)) return stringCulture;

        return default;
    }

    private string? ValidateCulture(string key, string culture)
    {
        string relativeFilePath = $"{Localization}/{culture}.json";
        string fullFilePath = Path.GetFullPath(relativeFilePath);
        if (File.Exists(fullFilePath))
        {
            return _cache.GetOrSet(
                $"locale_{culture}_{key}",
                () => PullDeserialize<string>(key, Path.GetFullPath(relativeFilePath)));
        }

        WriteEmptyKeys(new CultureInfo("en-US"), fullFilePath);
        return default;
    }

    private string? GetSpecificCulture(string key) =>
        ValidateCulture(key, Thread.CurrentThread.CurrentCulture.Name);

    private string? GetNaturalCulture(string key) =>
        ValidateCulture(key, Thread.CurrentThread.CurrentCulture.Name.Split("-")[0]);

    private string? GetDefaultCulture(string key) =>
        ValidateCulture(key, DefaultCulture);

    private void WriteEmptyKeys(CultureInfo sourceCulture, string fullFilePath)
    {
        string sourceFilePath = $"{Localization}/{sourceCulture.Name}.json";
        if (!File.Exists(sourceFilePath)) return;
        using var str = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var outStream = File.Create(fullFilePath);
        using var sWriter = new StreamWriter(outStream);
        using var writer = new JsonTextWriter(sWriter);
        using var sReader = new StreamReader(str);
        using var reader = new JsonTextReader(sReader);
        writer.Formatting = Formatting.Indented;
        var job = JObject.Load(reader);
        writer.WriteStartObject();
        foreach (var property in job.Properties())
        {
            writer.WritePropertyName(property.Name);
            writer.WriteNull();
        }

        writer.WriteEndObject();
    }

    private T? PullDeserialize<T>(string propertyName, string filePath)
    {
        if (propertyName == null) return default;
        if (filePath == null) return default;
        using var str = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var sReader = new StreamReader(str);
        using var reader = new JsonTextReader(sReader);
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.PropertyName && (string?)reader.Value == propertyName)
            {
                reader.Read();
                return _serializer.Deserialize<T>(reader);
            }
        }

        return default;
    }
}