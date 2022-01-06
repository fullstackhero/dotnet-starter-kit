using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DN.WebApi.Application.Multitenancy;
using DN.WebApi.Infrastructure.Multitenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Infrastructure.Test.Multitenancy.Fixtures;

public class TestFixture : TestBedFixture
{
    protected override void AddServices(IServiceCollection services, IConfiguration? configuration)
        => services
            .AddTransient<IMakeSecureConnectionString, MakeSecureConnectionString>();

    protected override ValueTask DisposeAsyncCore()
        => new();

    protected override IEnumerable<string> GetConfigurationFiles()
    {
        yield return "appsettings.json";
    }
}