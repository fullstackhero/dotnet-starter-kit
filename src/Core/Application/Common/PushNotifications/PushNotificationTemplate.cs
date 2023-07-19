using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Application.Common.PushNotifications;

public sealed record PushNotificationTemplate(string Name, ICollection<Message> Messages);
public sealed record Message(string Language, string Heading, string Content);