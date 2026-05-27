using FSH.Framework.Core.Domain;
using NetArchTest.Rules;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// Tests to enforce DDD patterns for domain entities.
/// </summary>
public class DomainEntityTests
{
    private static readonly Assembly[] ModuleAssemblies = ModuleAssemblyDiscovery.GetModuleAssemblies();

    [Fact]
    public void Domain_Events_Should_Implement_IDomainEvent()
    {
        var failures = new List<string>();

        foreach (var module in ModuleAssemblies)
        {
            var eventTypes = module.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.Name.EndsWith("DomainEvent", StringComparison.Ordinal)
                         || t.Name.EndsWith("Event", StringComparison.Ordinal)
                             && t.Namespace?.Contains(".Domain", StringComparison.Ordinal) == true);

            foreach (var eventType in eventTypes)
            {
                if (!typeof(IDomainEvent).IsAssignableFrom(eventType))
                {
                    failures.Add($"{eventType.FullName} should implement IDomainEvent");
                }
            }
        }

        failures.ShouldBeEmpty(
            $"All domain events should implement IDomainEvent. " +
            $"Violations: {string.Join(", ", failures)}");
    }

    [Fact]
    public void Domain_Events_Should_Be_Sealed()
    {
        var failures = new List<string>();

        foreach (var module in ModuleAssemblies)
        {
            var eventTypes = module.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => typeof(IDomainEvent).IsAssignableFrom(t));

            foreach (var eventType in eventTypes)
            {
                // Check if it's a record (records can be sealed too)
                bool isRecord = eventType.GetProperty("EqualityContract",
                    BindingFlags.NonPublic | BindingFlags.Instance) != null;

                if (!eventType.IsSealed && !isRecord)
                {
                    failures.Add($"{eventType.FullName} should be sealed or a record");
                }
            }
        }

        failures.ShouldBeEmpty(
            $"Domain events should be sealed or records for immutability. " +
            $"Violations: {string.Join(", ", failures)}");
    }

    [Fact]
    public void Entities_In_Core_Namespace_Should_Implement_IEntity()
    {
        var failures = new List<string>();

        foreach (var module in ModuleAssemblies)
        {
            var entityTypes = module.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.Namespace?.Contains(".Core.", StringComparison.Ordinal) == true)
                .Where(t => t.Name.EndsWith("Entity", StringComparison.Ordinal)
                         || (t.Namespace?.Contains(".Domain", StringComparison.Ordinal) == true
                             && !t.Name.EndsWith("Event", StringComparison.Ordinal)
                             && !t.Name.EndsWith("Dto", StringComparison.Ordinal)
                             && !t.Name.EndsWith("Exception", StringComparison.Ordinal)));

            foreach (var entityType in entityTypes)
            {
                bool implementsIEntity = entityType.GetInterfaces()
                    .Any(i => i.IsGenericType &&
                              i.GetGenericTypeDefinition().Name.StartsWith("IEntity", StringComparison.Ordinal));

                bool inheritsBaseEntity = IsSubclassOfGeneric(entityType, typeof(BaseEntity<>));

                if (!implementsIEntity && !inheritsBaseEntity)
                {
                    failures.Add($"{entityType.FullName} should implement IEntity<T> or inherit BaseEntity<T>");
                }
            }
        }

        failures.ShouldBeEmpty(
            $"Entities in Core namespace should implement IEntity<T> or inherit BaseEntity<T>. " +
            $"Violations: {string.Join(", ", failures)}");
    }

    [Fact]
    public void Aggregate_Roots_Should_Not_Reference_Other_Aggregates_Directly()
    {
        // This is a soft check - aggregate roots should reference other aggregates by ID only
        // We check that aggregate properties don't expose other aggregate types directly
        var failures = new List<string>();

        foreach (var module in ModuleAssemblies)
        {
            var aggregateTypes = module.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => IsSubclassOfGeneric(t, typeof(AggregateRoot<>)));

            foreach (var aggregateType in aggregateTypes)
            {
                // Get all public properties
                var properties = aggregateType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    var propertyType = property.PropertyType;

                    // Skip collection types and check element type
                    if (propertyType.IsGenericType)
                    {
                        var genericArgs = propertyType.GetGenericArguments();
                        if (genericArgs.Length > 0)
                        {
                            propertyType = genericArgs[0];
                        }
                    }

                    // Check if property type is another aggregate root (excluding self-references)
                    if (propertyType != aggregateType &&
                        IsSubclassOfGeneric(propertyType, typeof(AggregateRoot<>)))
                    {
                        failures.Add(
                            $"{aggregateType.Name}.{property.Name} directly references aggregate {propertyType.Name}. " +
                            "Consider referencing by ID instead.");
                    }
                }
            }
        }

        failures.ShouldBeEmpty(
            $"Aggregate roots should not directly reference other aggregate roots — use ID references instead. " +
            $"Violations: {string.Join(", ", failures)}");
    }

    [Fact]
    public void Value_Objects_Should_Be_Immutable()
    {
        var failures = new List<string>();

        foreach (var module in ModuleAssemblies)
        {
            var valueObjectTypes = module.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.Name.EndsWith("ValueObject", StringComparison.Ordinal)
                         || t.BaseType?.Name == "ValueObject");

            foreach (var voType in valueObjectTypes)
            {
                // Check all public properties have no public setter
                var mutableProperties = voType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.SetMethod != null && p.SetMethod.IsPublic)
                    .ToArray();

                if (mutableProperties.Length > 0)
                {
                    failures.Add(
                        $"{voType.FullName} has mutable properties: " +
                        $"{string.Join(", ", mutableProperties.Select(p => p.Name))}");
                }
            }
        }

        failures.ShouldBeEmpty(
            $"Value objects should be immutable (no public setters). " +
            $"Violations: {string.Join(", ", failures)}");
    }

    private static bool IsSubclassOfGeneric(Type type, Type genericBase)
    {
        while (type != null && type != typeof(object))
        {
            var current = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            if (genericBase == current)
            {
                return true;
            }
            type = type.BaseType!;
        }
        return false;
    }
}