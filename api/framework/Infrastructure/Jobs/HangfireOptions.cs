namespace FSH.Framework.Infrastructure.Jobs;
public class HangfireOptions
{
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string Route { get; set; } = "/jobs";
}
