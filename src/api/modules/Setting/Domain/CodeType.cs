using System.Text.Json.Serialization;

namespace FSH.Starter.WebApi.Setting.Domain;
// [Newtonsoft.Json.JsonConverter(typeof(JsonStringEnumConverter))]
public enum CodeType
{
    All,
    MasterData,
    Transaction,
    FastTransaction,
    IntCode
}
