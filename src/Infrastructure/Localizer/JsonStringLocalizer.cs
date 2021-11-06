using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Infrastructure.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace DN.WebApi.Infrastructure.Localizer
{
    public class JsonStringLocalizer : IStringLocalizer
    {
        private string Localization => "Localization";

        private readonly ICacheService _cache;

        private readonly JsonSerializer _serializer = new JsonSerializer();

        public JsonStringLocalizer(ICacheService cache)
        {
            _cache = cache;
        }

        public LocalizedString this[string name]
        {
            get
            {
                string value = GetString(name);
                return new LocalizedString(name, value ?? $"{name} [{Thread.CurrentThread.CurrentCulture.Name}]", value == null);
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
            using (var str = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var sReader = new StreamReader(str))
            using (var reader = new JsonTextReader(sReader))
            {
                while (reader.Read())
                {
                    if (reader.TokenType != JsonToken.PropertyName)
                        continue;
                    string key = (string)reader.Value;
                    reader.Read();
                    string value = _serializer.Deserialize<string>(reader);
                    yield return new LocalizedString(key, value, false);
                }
            }
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            CultureInfo.DefaultThreadCurrentCulture = culture;
            return new JsonStringLocalizer(_cache);
        }

        private string GetString(string key)
        {
            string relativeFilePath = $"{Localization}/{Thread.CurrentThread.CurrentCulture.Name}.json";
            string fullFilePath = Path.GetFullPath(relativeFilePath);
            if (File.Exists(fullFilePath))
            {
                string cacheKey = $"locale_{Thread.CurrentThread.CurrentCulture.Name}_{key}";
                string cacheValue = _cache.GetString(cacheKey);
                if (!string.IsNullOrEmpty(cacheValue)) return cacheValue;
                string result = PullDeserialize<string>(key, Path.GetFullPath(relativeFilePath));
                if (!string.IsNullOrEmpty(result)) _cache.SetString(cacheKey, result);
                return result;
            }

            WriteEmptyKeys(new CultureInfo("en-US"), fullFilePath);
            return default(string);
        }

        private void WriteEmptyKeys(CultureInfo sourceCulture, string fullFilePath)
        {
            string sourceFilePath = $"{Localization}/{sourceCulture.Name}.json";
            if (!File.Exists(sourceFilePath)) return;
            using (var str = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var outStream = File.Create(fullFilePath))
            using (var sWriter = new StreamWriter(outStream))
            using (var writer = new JsonTextWriter(sWriter))
            using (var sReader = new StreamReader(str))
            using (var reader = new JsonTextReader(sReader))
            {
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
        }

        private T PullDeserialize<T>(string propertyName, string filePath)
        {
            if (propertyName == null) return default(T);
            if (filePath == null) return default(T);
            using (var str = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var sReader = new StreamReader(str))
            using (var reader = new JsonTextReader(sReader))
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.PropertyName && (string)reader.Value == propertyName)
                    {
                        reader.Read();
                        return _serializer.Deserialize<T>(reader);
                    }
                }

                return default(T);
            }
        }
    }
}