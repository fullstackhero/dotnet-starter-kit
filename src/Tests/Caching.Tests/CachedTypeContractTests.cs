using System.ComponentModel;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Identity;
using FSH.Modules.Multitenancy.Contracts.Dtos;

namespace Caching.Tests;

/// <summary>
/// Verifies that every type stored in HybridCache is <c>sealed</c> AND carries
/// <see cref="ImmutableObjectAttribute"/>(true), which unlocks L1 object reuse and
/// avoids per-read JSON deserialization. Removing either silently regresses performance
/// — the test locks this in at build time.
/// </summary>
/// <remarks>
/// When you add a new type to a <c>HybridCache.GetOrCreateAsync&lt;T&gt;</c> or
/// <c>HybridCache.SetAsync&lt;T&gt;</c> call, add it to <see cref="CachedTypes"/>.
/// Internal types (like <c>PermissionSet</c> in the Identity module) are loaded via
/// reflection so the test doesn't require <c>InternalsVisibleTo</c>.
/// </remarks>
public sealed class CachedTypeContractTests
{
    public static TheoryData<Type> CachedTypes
    {
        get
        {
            var data = new TheoryData<Type>
            {
                typeof(TenantThemeDto),
                typeof(PaletteDto),
                typeof(BrandAssetsDto),
                typeof(TypographyDto),
                typeof(LayoutDto),
                typeof(CachedIdempotentResponse),
            };

            // Reach into the Identity runtime assembly for the internal PermissionSet type.
            var permissionSet = typeof(IdentityModule).Assembly
                .GetType("FSH.Modules.Identity.Caching.PermissionSet", throwOnError: false);
            if (permissionSet is not null)
            {
                data.Add(permissionSet);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(CachedTypes))]
    public void CachedType_Should_BeSealed(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        type.IsSealed.ShouldBeTrue(
            $"{type.FullName} is stored in HybridCache and must be sealed to unlock L1 object reuse.");
    }

    [Theory]
    [MemberData(nameof(CachedTypes))]
    public void CachedType_Should_HaveImmutableObjectAttribute(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var attr = type.GetCustomAttributes(typeof(ImmutableObjectAttribute), inherit: false)
            .Cast<ImmutableObjectAttribute>()
            .FirstOrDefault();

        attr.ShouldNotBeNull(
            $"{type.FullName} is stored in HybridCache and must have [ImmutableObject(true)] so the runtime can return the same reference across L1 hits instead of re-deserializing.");
        attr!.Immutable.ShouldBeTrue(
            $"{type.FullName} has [ImmutableObject(false)] — change to [ImmutableObject(true)] or stop caching it.");
    }
}
