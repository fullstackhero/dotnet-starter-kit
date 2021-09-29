using DN.WebApi.Domain.Contracts;
using System;

namespace DN.WebApi.Domain.Entities.Catalog
{
    public class Product : AuditableEntity, IMustHaveTenant
    {
        public Product(string name, string description, decimal rate, Guid brandId, string imagePath)
        {
            Name = name;
            Description = description;
            Rate = rate;
            ImagePath = imagePath;
            BrandId = brandId;
        }

        protected Product()
        {
        }

        public Product Update(string name, string description, decimal rate, string imagePath)
        {
            Name = name;
            Description = description;
            Rate = rate;
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
        public Guid BrandId { get; set; }
        public virtual Brand Brand { get; set; }
    }
}