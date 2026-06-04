using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Services;
using Shouldly;
using Xunit;

namespace Multitenancy.Tests.Services;

public sealed class FinbuckleEventTenantScopeTests
{
    private readonly StubAccessor _accessor = new();

    [Fact]
    public void Begin_Should_SetTenantContext_When_TenantIdProvided()
    {
        var sut = new FinbuckleEventTenantScope(_accessor, _accessor);

        using (sut.Begin("acme"))
        {
            _accessor.MultiTenantContext.TenantInfo.ShouldNotBeNull();
            _accessor.MultiTenantContext.TenantInfo!.Id.ShouldBe("acme");
            _accessor.MultiTenantContext.TenantInfo.Identifier.ShouldBe("acme");
        }
    }

    [Fact]
    public void Begin_Should_RestorePreviousContext_When_Disposed()
    {
        // Seed a pre-existing ambient tenant.
        ((IMultiTenantContextSetter)_accessor).MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(new AppTenantInfo("root", "root"));
        var sut = new FinbuckleEventTenantScope(_accessor, _accessor);

        using (sut.Begin("acme"))
        {
            _accessor.MultiTenantContext.TenantInfo!.Id.ShouldBe("acme");
        }

        _accessor.MultiTenantContext.TenantInfo!.Id.ShouldBe("root");
    }

    [Fact]
    public void Begin_Should_LeaveContextUntouched_When_TenantIdNullOrWhitespace()
    {
        // Seed a known tenant so we can prove Begin(null/"") does not replace it.
        ((IMultiTenantContextSetter)_accessor).MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(new AppTenantInfo("root", "root"));
        var sut = new FinbuckleEventTenantScope(_accessor, _accessor);

        using (sut.Begin(null))
        {
            _accessor.MultiTenantContext.TenantInfo!.Id.ShouldBe("root");
        }

        using (sut.Begin("   "))
        {
            _accessor.MultiTenantContext.TenantInfo!.Id.ShouldBe("root");
        }
    }

#pragma warning disable S2376 // Finbuckle's IMultiTenantContextSetter is a set-only contract.
    private sealed class StubAccessor : IMultiTenantContextAccessor<AppTenantInfo>, IMultiTenantContextSetter
    {
        private IMultiTenantContext<AppTenantInfo> _context =
            new MultiTenantContext<AppTenantInfo>(new AppTenantInfo());

        public IMultiTenantContext<AppTenantInfo> MultiTenantContext => _context;

        IMultiTenantContext IMultiTenantContextAccessor.MultiTenantContext => _context;

        IMultiTenantContext IMultiTenantContextSetter.MultiTenantContext
        {
            set => _context = (IMultiTenantContext<AppTenantInfo>)value;
        }
    }
#pragma warning restore S2376
}
