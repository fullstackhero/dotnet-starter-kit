namespace FSH.Framework.Shared.Localization;

/// <summary>
/// Marker class for shared localization resources.
/// Pair this with SharedResource.resx (and per-culture variants, e.g. SharedResource.af-ZA.resx).
/// Inject as IStringLocalizer&lt;SharedResource&gt; wherever shared strings are needed.
/// </summary>
#pragma warning disable S2094 // Intentionally empty marker class — pairs with SharedResource.resx
public sealed class SharedResource;
#pragma warning restore S2094
