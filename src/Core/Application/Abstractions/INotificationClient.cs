using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DN.WebApi.Application.Abstractions
{
    public interface INotificationClient
    {
        Task ReceiveMessage(object message);
    }
}
