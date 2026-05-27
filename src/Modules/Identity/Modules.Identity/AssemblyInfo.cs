using FSH.Framework.Web.Modules;
using System.Runtime.CompilerServices;

[assembly: FshModule(typeof(FSH.Modules.Identity.IdentityModule), 100)]
[assembly: InternalsVisibleTo("Identity.Tests")]