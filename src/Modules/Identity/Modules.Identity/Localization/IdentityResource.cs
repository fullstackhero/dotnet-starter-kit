namespace FSH.Modules.Identity.Localization;

/// <summary>
/// Marker class for Identity module localization resources.
/// Pair this with IdentityResource.resx (and per-culture variants, e.g. IdentityResource.af-ZA.resx).
/// Inject as IStringLocalizer&lt;IdentityResource&gt; in Identity module components/services.
/// </summary>
/// <remarks>
/// Use this for Identity-specific strings like "UserCreated", "LoginSuccessful", etc.
/// For common strings (Required, Email, Password), use IStringLocalizer&lt;SharedResource&gt;.
/// </remarks>
#pragma warning disable S2094 // Intentionally empty marker class — pairs with IdentityResource.resx
public sealed class IdentityResource;
#pragma warning restore S2094
