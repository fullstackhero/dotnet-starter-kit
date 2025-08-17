using System.Threading.Tasks;
namespace FSH.Starter.Api.Services;
public interface ILlmService
{
    Task<string> AnswerAsync(string userMessage, string context);
}
