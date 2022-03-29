using Finbuckle.MultiTenant.EntityFrameworkCore;
using FSH.WebApi.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Infrastructure.Persistence.Configuration;
public class GameTypeConfig : IEntityTypeConfiguration<GameType>
{
    public void Configure(EntityTypeBuilder<GameType> builder)
    {
        builder.IsMultiTenant();
        builder.Property(x => x.Name)
            .HasMaxLength(256);
        
    }
}