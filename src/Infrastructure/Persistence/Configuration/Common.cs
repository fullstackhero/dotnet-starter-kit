using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Reflection.Emit;

namespace FL_CRMS_ERP_WEBAPI.Infrastructure.Persistence.Configuration;
public class NotesConfig : IEntityTypeConfiguration<NotesModel>
{
    public void Configure(EntityTypeBuilder<NotesModel> builder)
    {
        //builder.IsMultiTenant();
        builder
             .ToTable("NotesDetailsInfo", SchemaNames.Common)
             .IsMultiTenant();
        builder
           .Property(e => e.CreatedOn)
           .HasColumnType("timestamp with time zone");
        builder
           .Property(e => e.LastModifiedOn)
           .HasColumnType("timestamp with time zone");
        //builder
        //    .Property(b => b.LeadOwner)
        //        .HasMaxLength(1024);

        //builder
        //    .Property(p => p.ImagePath)
        //        .HasMaxLength(2048);
    }

}
public class TaskConfig : IEntityTypeConfiguration<TaskModel>
{
    public void Configure(EntityTypeBuilder<TaskModel> builder)
    {
        //builder.IsMultiTenant();
        builder
             .ToTable("TaskDetailsInfo", SchemaNames.Common)
             .IsMultiTenant();
        builder
           .Property(e => e.DueDate)
           .HasColumnType("timestamp with time zone");
        builder
          .Property(e => e.CreatedOn)
          .HasColumnType("timestamp with time zone");
        builder
           .Property(e => e.LastModifiedOn)
           .HasColumnType("timestamp with time zone");
        //builder
        //    .Property(e => e.ToDate)
        //    .HasColumnType("timestamp with time zone");
    }
}

public class MeetingConfig : IEntityTypeConfiguration<MeetingModel>
{
    public void Configure(EntityTypeBuilder<MeetingModel> builder)
    {
        //builder.IsMultiTenant();
        builder
             .ToTable("MeetingDetailsInfo", SchemaNames.Common)
             .IsMultiTenant();
        builder
            .Property(e => e.FromDate)
            .HasColumnType("timestamp with time zone");
        builder
            .Property(e => e.ToDate)
            .HasColumnType("timestamp with time zone");
        builder
          .Property(e => e.CreatedOn)
          .HasColumnType("timestamp with time zone");
        builder
           .Property(e => e.LastModifiedOn)
           .HasColumnType("timestamp with time zone");
    }
}

public class CallsConfig : IEntityTypeConfiguration<CallsModel>
{
    public void Configure(EntityTypeBuilder<CallsModel> builder)
    {
        //builder.IsMultiTenant();
        builder
             .ToTable("CallDetailsInfo", SchemaNames.Common)
             .IsMultiTenant();
        builder
            .Property(e => e.CallStartTime)
            .HasColumnType("timestamp with time zone");
        //builder.Property(e => e.CallStartTime)
        //.HasConversion(new ValueConverter<DateTime?, DateTime?>(
        //    v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v,
        //    v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v
        //));
    }
}