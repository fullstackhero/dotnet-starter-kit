namespace FSH.Modules.Multitenancy.Provisioning;

public enum TenantProvisioningStepName
{
    Database = 1,
    Migrations = 2,
    Seeding = 3,
    CacheWarm = 4
}
