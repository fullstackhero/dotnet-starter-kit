using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DN.WebApi.Shared.DTOs
{
    public interface INotificationMessage
    {
        public string MessageType { get; set; }

        public string Message { get; set; }
    }
}
