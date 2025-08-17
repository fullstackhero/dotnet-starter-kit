using System.Threading.Tasks;
using FSH.Starter.Api.Dtos;

namespace FSH.Starter.Api.Services;
public interface IChatService
{
    Task<string> ProcessMessageAsync(WhatsAppMessageDto message);
}
