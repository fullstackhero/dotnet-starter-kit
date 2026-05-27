using FSH.Framework.Web.Modules;

namespace FSH.Modules.Identity;

public sealed class IdentityModuleConstants : IModuleConstants
{
    public string ModuleId => "Identity";

    public string ModuleName => "Identity";

    public string ApiPrefix => "identity";
    public const string SchemaName = "identity";
    public const int PasswordLength = 10;
}