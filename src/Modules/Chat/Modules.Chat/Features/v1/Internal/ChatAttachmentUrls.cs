using FSH.Modules.Chat.Contracts.v1.DTOs;
using FSH.Modules.Files.Contracts.v1.Queries;
using Mediator;

namespace FSH.Modules.Chat.Features.v1.Internal;

/// <summary>
/// Resolves message attachment URLs at <b>read</b> time. Chat files are uploaded Private, so the URL
/// persisted on a <see cref="MessageAttachmentDto"/> is a short-lived presigned URL captured at send
/// time — it expires, breaking historical images. For every attachment that carries a
/// <c>FileAssetId</c> we mint a fresh presigned URL via <see cref="GetFileDownloadUrlQuery"/> (which
/// also enforces the file's access policy), so the link is always valid when the page is fetched.
/// If a file can't be resolved (deleted / access denied) the stored URL is kept as a best-effort
/// fallback rather than failing the whole page.
/// </summary>
internal static class ChatAttachmentUrls
{
    public static async Task<List<MessageDto>> ResolveAsync(
        IReadOnlyList<MessageDto> messages,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(mediator);

        var ids = messages
            .SelectMany(m => m.Attachments)
            .Where(a => a.FileAssetId is { } id && id != Guid.Empty)
            .Select(a => a.FileAssetId!.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return [.. messages];
        }

        // Sequential by design: each Send resolves through the Files module's scoped DbContext, so
        // firing them concurrently would race that single context. The set is small (images per page).
        var resolved = new Dictionary<Guid, string>(ids.Count);
        foreach (var id in ids)
        {
            try
            {
                var presigned = await mediator
                    .Send(new GetFileDownloadUrlQuery(id, Inline: true), cancellationToken)
                    .ConfigureAwait(false);
                resolved[id] = presigned.Url.ToString();
            }
#pragma warning disable CA1031 // a missing/forbidden file must not break the whole message page
            catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
            {
                // Keep the stored URL as a best-effort fallback.
            }
        }

        return [.. messages.Select(m => m with
        {
            Attachments = [.. m.Attachments.Select(a =>
                a.FileAssetId is { } fid && resolved.TryGetValue(fid, out var url)
                    ? a with { Url = url }
                    : a)],
        })];
    }
}
