using FSH.Modules.Multitenancy.Contracts.Dtos;

namespace Integration.Tests.Infrastructure;

/// <summary>
/// Polls a tenant's provisioning status until the whole run finishes.
///
/// The status body also lists each step, and a finished step reads "Completed" while later steps
/// (seeding the tenant admin) are still running. Matching "Completed" anywhere in the body returns
/// after the first step, so only the overall <c>Status</c> field is trusted.
/// </summary>
internal static class TenantProvisioningWait
{
    public static async Task WaitForProvisioningAsync(HttpClient client, string tenantId, int maxRetries = 60)
    {
        ArgumentNullException.ThrowIfNull(client);

        TenantProvisioningStatusDto? status = null;
        for (int i = 0; i < maxRetries; i++)
        {
            var statusResponse = await client.GetAsync(
                $"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");

            if (statusResponse.IsSuccessStatusCode)
            {
                status = await statusResponse.Content.ReadFromJsonAsync<TenantProvisioningStatusDto>();
                if (string.Equals(status?.Status, "Completed", StringComparison.Ordinal))
                {
                    return;
                }

                if (string.Equals(status?.Status, "Failed", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Tenant {tenantId} provisioning failed at {status?.CurrentStep}: {status?.Error}");
                }
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException(
            $"Tenant {tenantId} provisioning did not complete within {maxRetries} seconds. " +
            $"Last status: {status?.Status ?? "<none>"} (step {status?.CurrentStep ?? "<none>"}).");
    }
}
