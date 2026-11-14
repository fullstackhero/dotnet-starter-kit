using Microsoft.AspNetCore.SignalR.Client;

namespace Integration.Tests.Infrastructure.Extensions;

public static class HubConnectionExtensions
{
    /// <summary>
    /// Starts the connection and waits until the server has finished <c>AppHub.OnConnectedAsync</c>.
    /// </summary>
    /// <remarks>
    /// <c>StartAsync</c> returns once the handshake completes, but the server runs
    /// <c>OnConnectedAsync</c> (a DB lookup, then the <c>user:</c>/<c>tenant:</c>/<c>channel:</c>
    /// group joins) after that. A broadcast fired in that gap misses this connection, so tests that
    /// assert on group broadcasts were flaky. SignalR dispatches a connection's invocations only after
    /// <c>OnConnectedAsync</c> returns, so awaiting one invocation is a reliable barrier.
    /// <c>JoinChannel(Guid.Empty)</c> is a no-op: the membership check fails and no group is joined.
    /// </remarks>
    public static async Task StartAndWaitForServerAsync(this HubConnection connection, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        await connection.StartAsync(ct).ConfigureAwait(false);
        await connection.InvokeAsync("JoinChannel", Guid.Empty, ct).ConfigureAwait(false);
    }
}
