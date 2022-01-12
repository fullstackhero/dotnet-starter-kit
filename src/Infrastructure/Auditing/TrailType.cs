namespace FSH.WebApi.Infrastructure.Auditing;

public enum TrailType : byte
{
    None = 0,
    Create = 1,
    Update = 2,
    Delete = 3
}