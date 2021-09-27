using DN.WebApi.Domain.Contracts;

namespace DN.WebApi.Domain.Entities.Catalog
{
    public class Product : AuditableEntity, IMustHaveTenant
    {
        public Product(string name, string description, decimal rate, string imagePath)
        {
            Name = name;
            Description = description;
            Rate = rate;
            ImagePath = imagePath;
        }

        protected Product()
        {
        }

        public Product Update(string name, string description, decimal rate, string imagePath)
        {
            Name = name;
            Description = description;
            Rate = rate;

            // Update image path if one is provided
            if (!string.IsNullOrEmpty(imagePath))
            {
                ImagePath = imagePath;
            }

            return this;
        }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public decimal Rate { get; private set; }

        public string TenantKey { get; set; }
        public string ImagePath { get; set; }
    }
}