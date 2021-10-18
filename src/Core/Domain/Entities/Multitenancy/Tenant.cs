using DN.WebApi.Domain.Contracts;
using System;
using System.Collections.Generic;

namespace DN.WebApi.Domain.Entities.Multitenancy
{
    public class Tenant : AuditableEntity
    {
        public string Name { get; private set; }
        public string AdminEmail { get; private set; }
        public string ConnectionString { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime ValidUpto { get; private set; }
        public Guid? ParentTenantId { get; set; }

        #region Navigation
        public virtual Tenant ParentTenant { get; set; }
        public virtual ICollection<Tenant> SubTenants { get; set; }
        #endregion

        public Tenant(string name, string key, string adminEmail, Guid? parentTenantId, Guid createdBy, string connectionString)
        {
            Name = name;
            Referral = key;
            AdminEmail = adminEmail;
            ConnectionString = connectionString;
            ParentTenantId = parentTenantId;
            CreatedBy = createdBy;
            CreatedOn = DateTime.Now;
            IsActive = true;
            ConcurrencyStamp = Guid.NewGuid();

            // Add Default 1 Month Validity for all new tenants. Something like a DEMO period for tenants.
            ValidUpto = DateTime.UtcNow.AddMonths(1);
        }

        public Tenant()
        {

        }

        public void AddValidity(int months)
        {
            this.ValidUpto = this.ValidUpto.AddMonths(months);
        }

        public void Activate()
        {
            this.IsActive = true;
        }

        public void Deactivate()
        {
            this.IsActive = false;
        }
    }
}