using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;

namespace FL_CRMS_ERP_WEBAPI.Infrastructure.Persistence.Configuration;
public class LeadConfig : IEntityTypeConfiguration<LeadDetailsModel>
{
    public void Configure(EntityTypeBuilder<LeadDetailsModel> builder)
    {
        //builder.IsMultiTenant();
        builder
             .ToTable("LeadDetailsInfo", SchemaNames.LeadData)
             .IsMultiTenant();  
    }
}

public class AccountConfig : IEntityTypeConfiguration<AccountDetailsModel>
{
    public void Configure(EntityTypeBuilder<AccountDetailsModel> builder)
    {
        //builder.IsMultiTenant();
        builder
             .ToTable("AccountDetailsInfo", SchemaNames.LeadData)
             .IsMultiTenant();
    }
}

public class ContactConfig : IEntityTypeConfiguration<ContactDetailsModel>
{
    public void Configure(EntityTypeBuilder<ContactDetailsModel> builder)
    {
        builder
             .ToTable("ContactDetailsInfo", SchemaNames.LeadData)
             .IsMultiTenant();
    }
}

public class QuotationConfig : IEntityTypeConfiguration<QuotationDetailsModel>
{
    public void Configure(EntityTypeBuilder<QuotationDetailsModel> builder)
    {
        builder
             .ToTable("QuotationDetailsInfo", SchemaNames.LeadData)
             .IsMultiTenant();
    }
}

public class InvoiceConfig : IEntityTypeConfiguration<InvoiceDetailsModel>
{
    public void Configure(EntityTypeBuilder<InvoiceDetailsModel> builder)
    {
        builder
             .ToTable("InvoiceDetailsInfo", SchemaNames.LeadData)
             .IsMultiTenant();
        builder.Property(i => i.InvoiceId).ValueGeneratedOnAddOrUpdate();
    }
}

public class CustomerConfig : IEntityTypeConfiguration<CustomerDetailsModel>
{
    public void Configure(EntityTypeBuilder<CustomerDetailsModel> builder)
    {
        builder
             .ToTable("CustomerDetailsInfo", SchemaNames.LeadData)
             .IsMultiTenant();
    }
}

public class ProductDetailsConfig : IEntityTypeConfiguration<ProductDetailsModel>
{
    public void Configure(EntityTypeBuilder<ProductDetailsModel> builder)
    {
        builder
             .ToTable("ProductDetailsInfo", SchemaNames.LeadData)
             .IsMultiTenant();
    }
}

public class DocumentConfig : IEntityTypeConfiguration<DocumentModel>
{
    public void Configure(EntityTypeBuilder<DocumentModel> builder)
    {
        builder
             .ToTable("Documents", SchemaNames.LeadData)
             .IsMultiTenant();
    }
}

public class DocumentTypeConfig : IEntityTypeConfiguration<DocumentTypeModel>
{
    public void Configure(EntityTypeBuilder<DocumentTypeModel> builder)
    {
        builder
             .ToTable("DocumentTypes", SchemaNames.LeadData)
             .IsMultiTenant();
    }
}
