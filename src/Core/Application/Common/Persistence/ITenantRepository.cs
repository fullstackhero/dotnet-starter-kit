namespace FSH.WebApi.Application.Common.Persistence;

// The Repository for the TenantManagement Db
// I(Read)RepositoryBase<T> is from Ardalis.Specification

public interface ITenantRepository : IRepositoryBase<Tenant>
{
}

public interface ITenantReadRepository : IReadRepositoryBase<Tenant>
{
}