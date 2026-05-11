using Mediator;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// Tests to ensure every command/query handler has a corresponding validator.
/// This ensures validation coverage across all features.
/// </summary>
public class HandlerValidatorPairingTests
{
    private static readonly Assembly[] ModuleAssemblies = ModuleAssemblyDiscovery.GetModuleAssemblies();
    
    // Known missing validators (to be implemented)
    private static readonly string[] KnownMissingCommandHandlers = [
        "FSH.Modules.Billing.Features.v1.Invoices.VoidInvoice.VoidInvoiceCommandHandler",
        "FSH.Modules.Billing.Features.v1.Invoices.MarkInvoicePaid.MarkInvoicePaidCommandHandler",
        "FSH.Modules.Billing.Features.v1.Invoices.IssueInvoice.IssueInvoiceCommandHandler",
        "FSH.Modules.Catalog.Features.v1.Products.RestoreProduct.RestoreProductCommandHandler",
        "FSH.Modules.Catalog.Features.v1.Products.DeleteProduct.DeleteProductCommandHandler",
        "FSH.Modules.Catalog.Features.v1.Categories.RestoreCategory.RestoreCategoryCommandHandler",
        "FSH.Modules.Catalog.Features.v1.Categories.DeleteCategory.DeleteCategoryCommandHandler",
        "FSH.Modules.Catalog.Features.v1.Brands.RestoreBrand.RestoreBrandCommandHandler",
        "FSH.Modules.Catalog.Features.v1.Brands.DeleteBrand.DeleteBrandCommandHandler",
        "FSH.Modules.Identity.Features.v1.TwoFactor.Enroll.EnrollTwoFactorCommandHandler",
        "FSH.Modules.Identity.Features.v1.Impersonation.EndImpersonation.EndImpersonationCommandHandler",
        "FSH.Modules.Multitenancy.Features.v1.TenantProvisioning.RetryTenantProvisioning.RetryTenantProvisioningCommandHandler",
        "FSH.Modules.Multitenancy.Features.v1.ResetTenantTheme.ResetTenantThemeCommandHandler",
        "FSH.Modules.Tickets.Features.v1.Tickets.RestoreTicket.RestoreTicketCommandHandler",
        "FSH.Modules.Tickets.Features.v1.Tickets.ResolveTicket.ResolveTicketCommandHandler",
        "FSH.Modules.Tickets.Features.v1.Tickets.ReopenTicket.ReopenTicketCommandHandler",
        "FSH.Modules.Tickets.Features.v1.Tickets.AssignTicket.AssignTicketCommandHandler"
    ];

    private static readonly string[] KnownMissingQueryHandlers = [
        "FSH.Modules.Billing.Features.v1.Invoices.GetMyInvoices.GetMyInvoicesQueryHandler",
        "FSH.Modules.Billing.Features.v1.Invoices.GetInvoices.GetInvoicesQueryHandler",
        "FSH.Modules.Catalog.Features.v1.Products.SearchProducts.SearchProductsQueryHandler",
        "FSH.Modules.Catalog.Features.v1.Products.ListTrashedProducts.ListTrashedProductsQueryHandler",
        "FSH.Modules.Catalog.Features.v1.Categories.SearchCategories.SearchCategoriesQueryHandler",
        "FSH.Modules.Catalog.Features.v1.Categories.ListTrashedCategories.ListTrashedCategoriesQueryHandler",
        "FSH.Modules.Catalog.Features.v1.Brands.SearchBrands.SearchBrandsQueryHandler",
        "FSH.Modules.Catalog.Features.v1.Brands.ListTrashedBrands.ListTrashedBrandsQueryHandler",
        "FSH.Modules.Identity.Features.v1.Sessions.GetTenantSessions.GetTenantSessionsQueryHandler",
        "FSH.Modules.Tickets.Features.v1.Tickets.SearchTickets.SearchTicketsQueryHandler",
        "FSH.Modules.Tickets.Features.v1.Tickets.ListTrashedTickets.ListTrashedTicketsQueryHandler",
        "FSH.Modules.Webhooks.Features.v1.GetWebhookSubscriptions.GetWebhookSubscriptionsQueryHandler",
        "FSH.Modules.Webhooks.Features.v1.GetWebhookDeliveries.GetWebhookDeliveriesQueryHandler"
    ];

    [Fact]
    public void CommandHandlers_Should_Have_Corresponding_Validators()
    {
        var missingValidators = new List<string>();

        foreach (var module in ModuleAssemblies)
        {
            // Find all command handler types
            var commandHandlerTypes = module.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
                     i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))));

            foreach (var handlerType in commandHandlerTypes)
            {
                if (KnownMissingCommandHandlers.Contains(handlerType.FullName)) continue;
                // Extract the command type from the handler interface
                var handlerInterface = handlerType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType &&
                        (i.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
                         i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)));

                if (handlerInterface == null) continue;

                var commandType = handlerInterface.GetGenericArguments()[0];
                var commandName = commandType.Name;

                // Look for a validator in the same namespace or nearby
                var expectedValidatorName = commandName + "Validator";

                // Check for validator in the same assembly
                var validatorExists = module.GetTypes()
                    .Any(t => t.Name == expectedValidatorName ||
                              t.Name == commandName.Replace("Command", "", StringComparison.Ordinal) + "CommandValidator" ||
                              t.Name == commandName.Replace("Command", "", StringComparison.Ordinal) + "Validator");

                if (!validatorExists)
                {
                    // Check if the command type itself might have validation attributes (acceptable alternative)
                    var hasValidationAttributes = commandType
                        .GetProperties()
                        .Any(p => p.GetCustomAttributes()
                            .Any(a => a.GetType().Name.Contains("Required", StringComparison.Ordinal) ||
                                     a.GetType().Name.Contains("Range", StringComparison.Ordinal) ||
                                     a.GetType().Name.Contains("StringLength", StringComparison.Ordinal)));

                    if (!hasValidationAttributes)
                    {
                        missingValidators.Add($"{handlerType.FullName} -> missing {expectedValidatorName}");
                    }
                }
            }
        }

        missingValidators.ShouldBeEmpty(
            $"Found {missingValidators.Count} command handler(s) without validators. " +
            $"Every command handler must have a corresponding FluentValidation validator. " +
            $"Missing: {string.Join(", ", missingValidators)}");
    }

    [Fact]
    public void QueryHandlers_With_Pagination_Should_Have_Validators()
    {
        var missingValidators = new List<string>();

        foreach (var module in ModuleAssemblies)
        {
            // Find all query handler types
            var queryHandlerTypes = module.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)));

            foreach (var handlerType in queryHandlerTypes)
            {
                if (KnownMissingQueryHandlers.Contains(handlerType.FullName)) continue;
                // Extract the query type from the handler interface
                var handlerInterface = handlerType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));

                if (handlerInterface == null) continue;

                var queryType = handlerInterface.GetGenericArguments()[0];

                // Check if this query has pagination properties (PageNumber, PageSize, etc.)
                var hasPagination = queryType.GetProperties()
                    .Any(p => p.Name.Equals("PageNumber", StringComparison.OrdinalIgnoreCase) ||
                              p.Name.Equals("PageSize", StringComparison.OrdinalIgnoreCase) ||
                              p.Name.Equals("Skip", StringComparison.OrdinalIgnoreCase) ||
                              p.Name.Equals("Take", StringComparison.OrdinalIgnoreCase));

                if (hasPagination)
                {
                    var queryName = queryType.Name;
                    var expectedValidatorName = queryName + "Validator";

                    // Check for validator in the same assembly
                    var validatorExists = module.GetTypes()
                        .Any(t => t.Name == expectedValidatorName ||
                                  t.Name == queryName.Replace("Query", "", StringComparison.Ordinal) + "QueryValidator" ||
                                  t.Name == queryName.Replace("Query", "", StringComparison.Ordinal) + "Validator");

                    if (!validatorExists)
                    {
                        missingValidators.Add(
                            $"{handlerType.FullName} handles paginated query but has no validator");
                    }
                }
            }
        }

        missingValidators.ShouldBeEmpty(
            $"Paginated queries should have validators to validate PageNumber/PageSize bounds. " +
            $"Missing: {string.Join(", ", missingValidators)}");
    }

    [Fact]
    public void Validators_Should_Match_Command_Or_Query_Types()
    {
        var orphanedValidators = new List<string>();

        foreach (var module in ModuleAssemblies)
        {
            // Find all validators (classes inheriting from AbstractValidator<T>)
            var validatorTypes = module.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.BaseType != null &&
                           t.BaseType.IsGenericType &&
                           t.BaseType.GetGenericTypeDefinition().Name.Contains("AbstractValidator", StringComparison.Ordinal));

            foreach (var validatorType in validatorTypes)
            {
                // Skip nested validators (like LayoutValidator inside UpdateTenantThemeCommandValidator)
                if (validatorType.IsNested) continue;

                // Get the validated type
                var validatedType = validatorType.BaseType?.GetGenericArguments().FirstOrDefault();
                if (validatedType == null) continue;

                // Check if the validated type is a Command or Query
                bool isCommand = validatedType.Name.EndsWith("Command", StringComparison.Ordinal);
                bool isQuery = validatedType.Name.EndsWith("Query", StringComparison.Ordinal);

                if (!isCommand && !isQuery)
                {
                    // Allow validators for other types (like DTOs) but note them
                    continue;
                }

                // Check validator naming follows convention
                var expectedName = validatedType.Name + "Validator";
                if (!validatorType.Name.Equals(expectedName, StringComparison.Ordinal))
                {
                    // Allow some flexibility in naming
                    var altName = validatedType.Name.Replace("Command", "", StringComparison.Ordinal).Replace("Query", "", StringComparison.Ordinal) +
                                  (isCommand ? "CommandValidator" : "QueryValidator");
                    var altName2 = validatedType.Name.Replace("Command", "", StringComparison.Ordinal).Replace("Query", "", StringComparison.Ordinal) + "Validator";
                    if (!validatorType.Name.Equals(altName, StringComparison.Ordinal) && !validatorType.Name.Equals(altName2, StringComparison.Ordinal))
                    {
                        orphanedValidators.Add(
                            $"{validatorType.FullName} validates {validatedType.Name} but naming doesn't follow convention");
                    }
                }
            }
        }

        orphanedValidators.ShouldBeEmpty(
            $"Found {orphanedValidators.Count} validator(s) with incorrect naming. " +
            $"Validators must be named {{CommandName}}Validator or {{CommandName}}CommandValidator. " +
            $"Violations: {string.Join(", ", orphanedValidators)}");
    }
}