using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using FSH.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Identity.Data;

public class ApplicationUserConfig : IEntityTypeConfiguration<FshUser>
{
    public void Configure(EntityTypeBuilder<FshUser> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .ToTable("Users", IdentityModuleConstants.SchemaName)
            .IsMultiTenant();

        builder
            .Property(u => u.ObjectId)
                .HasMaxLength(256);
    }
}

public class ApplicationRoleConfig : IEntityTypeConfiguration<FshRole>
{
    public void Configure(EntityTypeBuilder<FshRole> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .ToTable("Roles", IdentityModuleConstants.SchemaName)
            .IsMultiTenant()
                .AdjustUniqueIndexes();
    }
}

public class ApplicationRoleClaimConfig : IEntityTypeConfiguration<FshRoleClaim>
{
    public void Configure(EntityTypeBuilder<FshRoleClaim> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .ToTable("RoleClaims", IdentityModuleConstants.SchemaName)
            .IsMultiTenant();
    }
}

public class IdentityUserRoleConfig : IEntityTypeConfiguration<IdentityUserRole<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<string>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .ToTable("UserRoles", IdentityModuleConstants.SchemaName)
            .IsMultiTenant();
    }
}

public class IdentityUserClaimConfig : IEntityTypeConfiguration<IdentityUserClaim<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<string>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .ToTable("UserClaims", IdentityModuleConstants.SchemaName)
            .IsMultiTenant();
    }
}

public class IdentityUserLoginConfig : IEntityTypeConfiguration<IdentityUserLogin<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<string>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .ToTable("UserLogins", IdentityModuleConstants.SchemaName)
            .IsMultiTenant();
    }
}

public class IdentityUserTokenConfig : IEntityTypeConfiguration<IdentityUserToken<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<string>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .ToTable("UserTokens", IdentityModuleConstants.SchemaName)
            .IsMultiTenant();
    }
}