using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using Xunit;

namespace FSH.Starter.Tests.Architecture;

/// <summary>
/// Enterprise-level architecture validation tests.
/// These tests ensure architectural boundaries and dependencies are maintained.
/// </summary>
public class ArchitectureTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(FSH.Framework.Core.FshCore).Assembly,
            typeof(FSH.Framework.Infrastructure.FshInfrastructure).Assembly,
            typeof(FSH.Starter.WebApi.Host.Program).Assembly)
        .Build();

    /// <summary>
    /// Core layer should not depend on Infrastructure layer.
    /// This ensures clean architecture principles.
    /// </summary>
    [Fact]
    public void Core_Should_Not_Depend_On_Infrastructure()
    {
        var rule = ArchRuleDefinition.Types()
            .That().ResideInNamespace("FSH.Framework.Core")
            .Should().NotDependOnAny("FSH.Framework.Infrastructure");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Core layer should not depend on any external frameworks.
    /// Core should contain only business logic.
    /// </summary>
    [Fact]
    public void Core_Should_Not_Depend_On_External_Frameworks()
    {
        var rule = ArchRuleDefinition.Types()
            .That().ResideInNamespace("FSH.Framework.Core")
            .Should().NotDependOnAny("Microsoft.AspNetCore")
            .AndShould().NotDependOnAny("Microsoft.EntityFrameworkCore")
            .AndShould().NotDependOnAny("Dapper")
            .AndShould().NotDependOnAny("Npgsql");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Controllers should only be in the API layer.
    /// This prevents mixing of concerns.
    /// </summary>
    [Fact]
    public void Controllers_Should_Only_Be_In_Api_Layer()
    {
        var rule = ArchRuleDefinition.Classes()
            .That().HaveNameEndingWith("Controller")
            .Should().ResideInNamespace("FSH.Starter.WebApi.Host.Controllers");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Services should implement interfaces.
    /// This ensures dependency injection and testability.
    /// </summary>
    [Fact]
    public void Services_Should_Implement_Interfaces()
    {
        var rule = ArchRuleDefinition.Classes()
            .That().HaveNameEndingWith("Service")
            .And().AreNotInterfaces()
            .Should().ImplementInterface(@".*I.*Service.*");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Domain entities should not depend on infrastructure.
    /// Entities should be framework-agnostic.
    /// </summary>
    [Fact]
    public void Domain_Entities_Should_Not_Depend_On_Infrastructure()
    {
        var rule = ArchRuleDefinition.Classes()
            .That().ResideInNamespace("FSH.Framework.Core.Domain")
            .Should().NotDependOnAny("FSH.Framework.Infrastructure")
            .AndShould().NotDependOnAny("Microsoft.EntityFrameworkCore")
            .AndShould().NotDependOnAny("Dapper");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Repository implementations should be in Infrastructure layer.
    /// This follows the clean architecture pattern.
    /// </summary>
    [Fact]
    public void Repository_Implementations_Should_Be_In_Infrastructure()
    {
        var rule = ArchRuleDefinition.Classes()
            .That().HaveNameEndingWith("Repository")
            .And().AreNotInterfaces()
            .Should().ResideInNamespace("FSH.Framework.Infrastructure");

        rule.Check(Architecture);
    }

    /// <summary>
    /// Infrastructure should not contain business logic.
    /// Infrastructure should only handle technical concerns.
    /// </summary>
    [Fact]
    public void Infrastructure_Should_Not_Contain_Business_Logic()
    {
        var rule = ArchRuleDefinition.Types()
            .That().ResideInNamespace("FSH.Framework.Infrastructure")
            .Should().NotDependOnAny("FSH.Framework.Core.Features")
            .Because("Infrastructure should not contain business logic");

        rule.Check(Architecture);
    }

    /// <summary>
    /// API Controllers should not directly access repositories.
    /// Controllers should use services/mediator pattern.
    /// </summary>
    [Fact]
    public void Controllers_Should_Not_Directly_Access_Repositories()
    {
        var rule = ArchRuleDefinition.Classes()
            .That().HaveNameEndingWith("Controller")
            .Should().NotDependOnAny(".*Repository.*")
            .Because("Controllers should use services, not repositories directly");

        rule.Check(Architecture);
    }
} 