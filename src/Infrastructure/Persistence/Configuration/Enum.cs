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

public class LineOfBusinessConfig : IEntityTypeConfiguration<LineOfBusinessModel>
{
    public void Configure(EntityTypeBuilder<LineOfBusinessModel> builder)
    {
        builder
           .ToTable("LineOfBusiness", SchemaNames.Enum)
           .IsMultiTenant();
    }
}

public class CustomerCompanyNameConfig : IEntityTypeConfiguration<CustomerCompanyNameModel>
{
    public void Configure(EntityTypeBuilder<CustomerCompanyNameModel> builder)
    {
        builder
           .ToTable("CustomerCompanyName", SchemaNames.Enum)
           .IsMultiTenant();
    }
}

public class CustomerModeOfPaymentConfig : IEntityTypeConfiguration<CustomerModeOfPaymentModel>
{
    public void Configure(EntityTypeBuilder<CustomerModeOfPaymentModel> builder)
    {
        builder
           .ToTable("CustomerModeOfPayment", SchemaNames.Enum)
           .IsMultiTenant();
    }
}

public class CustomerNumberOfLivesConfig : IEntityTypeConfiguration<CustomerNumberOfLivesModel>
{
    public void Configure(EntityTypeBuilder<CustomerNumberOfLivesModel> builder)
    {
        builder
           .ToTable("CustomerNumberOfLives", SchemaNames.Enum)
           .IsMultiTenant();
    }
}

public class CustomerProductConfig : IEntityTypeConfiguration<CustomerProductModel>
{
    public void Configure(EntityTypeBuilder<CustomerProductModel> builder)
    {
        builder
           .ToTable("CustomerProduct", SchemaNames.Enum)
           .IsMultiTenant();
    }
}

public class CustomerPolicyStatusConfig : IEntityTypeConfiguration<CustomerPolicyStatusModel>
{
    public void Configure(EntityTypeBuilder<CustomerPolicyStatusModel> builder)
    {
        builder
           .ToTable("CustomerPolicyStatus", SchemaNames.Enum)
           .IsMultiTenant();
    }
}
