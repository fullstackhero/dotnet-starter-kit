using System.ComponentModel;

namespace DN.WebApi.Domain.Constants
{
    public class Permissions
    {
        [DisplayName("Products")]
        [Description("Products Permissions")]
        public static class Products
        {
            public const string View = "Permissions.Products.View";
            public const string ListAll = "Permissions.Products.ViewAll";
            public const string Register = "Permissions.Products.Register";
            public const string Update = "Permissions.Products.Update";
            public const string Remove = "Permissions.Products.Remove";
        }

        public static class Tenants
        {
            public const string View = "Permissions.Tenants.View";
            public const string ListAll = "Permissions.Tenants.ViewAll";
            public const string Create = "Permissions.Tenants.Register";
            public const string Update = "Permissions.Tenants.Update";
            public const string Remove = "Permissions.Tenants.Remove";
        }
    }
}