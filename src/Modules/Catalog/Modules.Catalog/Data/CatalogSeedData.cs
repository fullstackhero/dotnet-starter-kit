using FSH.Modules.Catalog.Domain;

namespace FSH.Modules.Catalog.Data;

/// <summary>
/// Demo seed data for the Catalog module. Wired in by <see cref="CatalogDbInitializer"/>
/// only when the host is running in the Development environment.
/// </summary>
internal static class CatalogSeedData
{
    public static IReadOnlyList<Brand> Brands { get; } =
    [
        Brand.Create("Acme Goods",      "Quality essentials for the modern home.",            null),
        Brand.Create("Northwind",       "Outdoor and adventure gear since 1985.",             null),
        Brand.Create("Contoso Studio",  "Design-forward furniture and lighting.",             null),
        Brand.Create("Fabrikam",        "Pro-grade tools for makers and builders.",           null),
    ];

    public static (IReadOnlyList<Category> Roots, IReadOnlyList<Category> Children) BuildCategories()
    {
        var apparel = Category.Create("Apparel", "Clothing and accessories.", null);
        var home = Category.Create("Home & Living", "Furniture, decor, and home essentials.", null);
        var outdoor = Category.Create("Outdoor", "Gear for the great outdoors.", null);
        var tools = Category.Create("Tools", "Power tools, hand tools, and accessories.", null);

        var roots = new[] { apparel, home, outdoor, tools };

        var children = new[]
        {
            Category.Create("Tops",         "Shirts, t-shirts, and tops.",          apparel.Id),
            Category.Create("Outerwear",    "Jackets, coats, and shells.",          apparel.Id),
            Category.Create("Furniture",    "Chairs, tables, and shelving.",        home.Id),
            Category.Create("Lighting",     "Lamps and lighting fixtures.",         home.Id),
            Category.Create("Camping",      "Tents, sleeping bags, and cookware.",  outdoor.Id),
            Category.Create("Hand Tools",   "Hammers, screwdrivers, wrenches.",     tools.Id),
            Category.Create("Power Tools",  "Drills, saws, sanders.",               tools.Id),
        };

        return (roots, children);
    }

    public static IReadOnlyList<Product> BuildProducts(
        IReadOnlyDictionary<string, Brand> brandsByName,
        IReadOnlyDictionary<string, Category> categoriesByName)
    {
        Brand B(string name) => brandsByName[name];
        Category C(string name) => categoriesByName[name];

        return
        [
            Product.Create("ACM-TS-001", "Classic Cotton Tee",      "100% organic cotton crew-neck.",      B("Acme Goods").Id,     C("Tops").Id,         new Money(24.00m,  "USD"),  150),
            Product.Create("ACM-HD-002", "Heavyweight Hoodie",      "450gsm fleece pullover hoodie.",      B("Acme Goods").Id,     C("Outerwear").Id,    new Money(68.00m,  "USD"),  60),
            Product.Create("CON-CH-101", "Walnut Lounge Chair",     "Mid-century walnut frame, linen seat.", B("Contoso Studio").Id, C("Furniture").Id,    new Money(489.00m, "USD"),  12),
            Product.Create("CON-LP-102", "Brass Pendant Lamp",      "Hand-finished brass dome pendant.",   B("Contoso Studio").Id, C("Lighting").Id,     new Money(189.00m, "USD"),  24),
            Product.Create("NW-TN-201",  "Trailhead 2P Tent",       "3-season backpacking tent, 2.4kg.",   B("Northwind").Id,      C("Camping").Id,      new Money(279.00m, "USD"),  35),
            Product.Create("NW-SB-202",  "Summit Sleeping Bag",     "Down-filled, comfort to -5°C.",       B("Northwind").Id,      C("Camping").Id,      new Money(219.00m, "USD"),  28),
            Product.Create("FAB-DR-301", "20V Cordless Drill",      "Brushless, 2-speed, 2x batteries.",   B("Fabrikam").Id,       C("Power Tools").Id,  new Money(159.00m, "USD"),  80),
            Product.Create("FAB-WS-302", "16-piece Wrench Set",     "Chrome vanadium, metric + imperial.", B("Fabrikam").Id,       C("Hand Tools").Id,   new Money(72.00m,  "USD"),  120),
            Product.Create("FAB-CS-303", "7-1/4\" Circular Saw",    "15-amp corded, 5800 RPM.",            B("Fabrikam").Id,       C("Power Tools").Id,  new Money(129.00m, "USD"),  45),
            Product.Create("ACM-JK-003", "All-Weather Shell",       "3-layer waterproof breathable shell.", B("Acme Goods").Id,    C("Outerwear").Id,    new Money(189.00m, "USD"),  40),
        ];
    }
}
