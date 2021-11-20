using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DN.WebApi.Application.Abstractions.Services.General
{
    public interface INotificationService : ITransientService
    {
        Task BroadcastExceptMessageAsync(object message, IEnumerable<string> excludedConnectionIds);
        Task BroadcastMessageAsync(object message);
        Task SendMessageAsync(object message);
        Task SendMessageExceptAsync(object message, IEnumerable<string> excludedConnectionIds);
        Task SendMessageToGroupAsync(object message, string group);
        Task SendMessageToGroupExceptAsync(object message, string group, IEnumerable<string> excludedConnectionIds);
        Task SendMessageToGroupsAsync(object message, IEnumerable<string> groupNames);
        Task SendMessageToUserAsync(string user, object message);
        Task SendMessageToUsersAsync(IEnumerable<string> userIds, object message);
    }
}
