namespace FSH.Framework.Infrastructure.Cors;
using System.Collections.ObjectModel;

public class CorsOptions
{
    public CorsOptions()
    {
        AllowedOrigins = [];
    }

    public Collection<string> AllowedOrigins { get; }
}
