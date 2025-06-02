namespace FSH.Starter.WebApi.Host;

public static class RoleConstants
{
    public const string Admin = "admin";
    public const string CustomerAdmin = "customer_admin";
    public const string CustomerSupport = "customer_support";
    public const string BaseUser = "base_user";

    public static readonly string[] AllRoles = 
    { 
        Admin, 
        CustomerAdmin, 
        CustomerSupport, 
        BaseUser 
    };

    public static readonly string[] AdminRoles = 
    { 
        Admin, 
        CustomerAdmin 
    };

    public static readonly string[] SupportRoles = 
    { 
        Admin, 
        CustomerAdmin, 
        CustomerSupport 
    };
} 
