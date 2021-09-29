using System.ComponentModel;

namespace DN.WebApi.Domain.Constants
{
    public class Permissions
    {
        [DisplayName("Identity")]
        [Description("Identity Permissions")]
        public static class Identity
        {
            public const string Register = "Permissions.Identity.Register";
        }

        [DisplayName("Roles")]
        [Description("Roles Permissions")]
        public static class Roles
        {
            public const string View = "Permissions.Roles.View";
            public const string ListAll = "Permissions.Roles.ViewAll";
            public const string Register = "Permissions.Roles.Register";
            public const string Update = "Permissions.Roles.Update";
            public const string Remove = "Permissions.Roles.Remove";
        }

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

        [DisplayName("Brands")]
        [Description("Brands Permissions")]
        public static class Brands
        {
            public const string View = "Permissions.Brands.View";
            public const string ListAll = "Permissions.Brands.ViewAll";
            public const string Register = "Permissions.Brands.Register";
            public const string Update = "Permissions.Brands.Update";
            public const string Remove = "Permissions.Brands.Remove";
        }
    }
}