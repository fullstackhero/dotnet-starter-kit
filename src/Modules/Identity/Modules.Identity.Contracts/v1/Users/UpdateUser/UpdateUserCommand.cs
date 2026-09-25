using FSH.Framework.Shared.Storage;
using Mediator;
using System.Text.Json.Serialization;

namespace FSH.Modules.Identity.Contracts.v1.Users.UpdateUser;

public class UpdateUserCommand : ICommand<Unit>
{
    public string Id { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public FileUploadRequest? Image { get; set; }
    public bool DeleteCurrentImage { get; set; }

    /// <summary>
    /// Concurrency tokens the caller is willing to overwrite, taken from the request's
    /// <c>If-Match</c> header by the endpoint. <see langword="null"/> means the caller sent no
    /// precondition and accepts whatever version is stored; a non-null list means the update
    /// only proceeds when the stored token matches one of the entries.
    /// </summary>
    /// <remarks>
    /// Header-derived, never read from the request body — the endpoint always overwrites it.
    /// </remarks>
    [JsonIgnore]
    public IReadOnlyList<string>? ExpectedConcurrencyStamps { get; set; }
}