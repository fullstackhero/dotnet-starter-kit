using DN.WebApi.Application.Abstractions.Services.General;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace DN.WebApi.Infrastructure.Services.General
{
    public class NewtonSoftService : ISerializerService
    {
        public T Deserialize<T>(string text)
        {
            return JsonConvert.DeserializeObject<T>(text);
        }

        public string Serialize<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        public string Serialize<T>(T obj, Type type)
        {
            return JsonConvert.SerializeObject(obj, type, new());
        }
    }
}