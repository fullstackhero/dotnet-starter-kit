using System.Threading.Tasks;
namespace FSH.Starter.Api.Services;
public interface IWhatsAppService
{
    Task SendMessageAsync(string to, string body);
}
