using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Users.SetProfileImage;

/// <summary>
/// Sets the authenticated user's avatar URL — typically a durable <c>publicUrl</c> returned
/// from the Files module's presigned-upload flow. Pass a null/empty URL to clear the image.
/// The endpoint forces the target id to the authenticated user; the caller cannot set someone
/// else's avatar.
/// </summary>
public sealed record SetProfileImageCommand(string? ImageUrl) : ICommand<Unit>;
