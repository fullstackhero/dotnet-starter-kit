using System;

namespace DN.WebApi.Application.Abstractions.Services.General
{
    public interface ISerializerService : ITransientService
    {
        string Serialize<T>(T obj);

        string Serialize<T>(T obj, Type type);

        T Deserialize<T>(string text);
    }
}