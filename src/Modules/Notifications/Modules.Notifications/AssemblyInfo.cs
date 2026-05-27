using System.Runtime.CompilerServices;
using FSH.Framework.Web.Modules;

[assembly: FshModule(typeof(FSH.Modules.Notifications.NotificationsModule), 750)]
[assembly: InternalsVisibleTo("Notifications.Tests")]
[assembly: InternalsVisibleTo("Integration.Tests")]
