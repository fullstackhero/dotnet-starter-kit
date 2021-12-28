using System.ComponentModel;

namespace DN.WebApi.Domain.Constants;

public partial class PermissionConstants
{
    [DisplayName("Products")]
    [Description("Products Permissions")]
    public static class Products
    {
        public const string View = "Permissions.Products.View";
        public const string Search = "Permissions.Products.Search";
        public const string Register = "Permissions.Products.Register";
        public const string Update = "Permissions.Products.Update";
        public const string Remove = "Permissions.Products.Remove";
    }

    [DisplayName("Brands")]
    [Description("Brands Permissions")]
    public static class Brands
    {
        public const string View = "Permissions.Brands.View";
        public const string Search = "Permissions.Brands.Search";
        public const string Register = "Permissions.Brands.Register";
        public const string Update = "Permissions.Brands.Update";
        public const string Remove = "Permissions.Brands.Remove";
        public const string Generate = "Permissions.Brands.Generate";
        public const string Clean = "Permissions.Brands.Clean";
    }
}