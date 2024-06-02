namespace FSH.Framework.Infrastructure.Jobs;
public class HangfireOptions
{
    public string UserName { get; set; } = "admin";
    public string Password { get; set; } = "Secure1234!Me";
    public string Route { get; set; } = "/jobs";
}
