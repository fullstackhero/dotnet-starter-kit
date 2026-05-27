using System.Runtime.CompilerServices;
using FSH.Framework.Web.Modules;

[assembly: FshModule(typeof(FSH.Modules.Chat.ChatModule), 800)]
[assembly: InternalsVisibleTo("Chat.Tests")]
[assembly: InternalsVisibleTo("Integration.Tests")]
