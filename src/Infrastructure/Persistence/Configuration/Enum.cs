using FL_CRMS_ERP_WEBAPI.Domain.Catalog;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using Finbuckle.MultiTenant.EntityFrameworkCore;

namespace FL_CRMS_ERP_WEBAPI.Infrastructure.Persistence.Configuration;
public class InvoiceStatusConfig : IEntityTypeConfiguration<InvoiceStatusModel>
{
    public void Configure(EntityTypeBuilder<InvoiceStatusModel> builder)
    {
        builder
           .ToTable("InvoiceStatus", SchemaNames.Enum)
           .IsMultiTenant();

    }
}
