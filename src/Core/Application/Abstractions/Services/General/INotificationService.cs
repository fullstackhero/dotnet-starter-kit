using DN.WebApi.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DN.WebApi.Application.Abstractions.Services.General
{
    public interface INotificationService : ITransientService
    {
        Task BroadcastExceptMessageAsync(INotificationMessage notification, IEnumerable<string> excludedConnectionIds);
        Task BroadcastMessageAsync(INotificationMessage notification);
        Task SendMessageAsync(INotificationMessage notification);
        Task SendMessageExceptAsync(INotificationMessage notification, IEnumerable<string> excludedConnectionIds);
        Task SendMessageToGroupAsync(INotificationMessage notification, string group);
        Task SendMessageToGroupExceptAsync(INotificationMessage notification, string group, IEnumerable<string> excludedConnectionIds);
        Task SendMessageToGroupsAsync(INotificationMessage notification, IEnumerable<string> groupNames);
        Task SendMessageToUserAsync(string user, INotificationMessage notification);
        Task SendMessageToUsersAsync(IEnumerable<string> userIds, INotificationMessage notification);
    }
}
