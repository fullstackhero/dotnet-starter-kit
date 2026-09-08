# Localization (i18n)

`src/BuildingBlocks/Core/Localization/` + per-module `Localization/` folders. Read before adding any user-facing message (exception, validation, API error). **The client's culture decides which words the client reads; it never decides how the API formats numbers or dates.**

## Culture negotiation (already wired — don't re-add)

`AddHeroLocalization()` / `UseHeroLocalization()` (`BuildingBlocks/Web/Localization/`) negotiate the request **UI** culture in this order: `?culture=` query → `locale` JWT claim (`UserLocaleRequestCultureProvider`) → `Accept-Language` → configured default → `en-US`. Supported tags live in `SupportedCultures.Tags`. The culture is set before endpoints and the exception handler run, so any `IStringLocalizer` resolved downstream picks up the request culture automatically.

**`CultureInfo.CurrentUICulture` only — `CurrentCulture` stays invariant.** An API that emits JSON must not shift `ToString()`, `Parse()` or interpolation per request; both React apps format at the presentation layer. `RequestLocalizationMiddleware` assigns both cultures unconditionally, so the culture half is pinned rather than left alone: `DefaultRequestCulture` carries `(InvariantCulture, configured default)` and `SupportedCultures` is `null` so the middleware skips culture filtering. Do **not** "fix" this by adding `AddSupportedCultures(...)`; `Formatting_culture_stays_invariant_while_ui_culture_negotiates` fails if you do.

`SupportedCultures.Tags` is **specific tags only**, no neutrals. A request asking for a bare `pt`, or for an unsupported variant like `pt-PT`, resolves to the configured default rather than being served a language it was not translated into. The React apps canonicalize variants onto supported tags (`CANON` in `clients/*/src/i18n.ts`) before calling the API, so app traffic is unaffected; a hand-rolled client sending bare `pt` gets the default. Adding a language means: add its specific tag to `Tags`, add a `*.{tag}.resx` per catalog, add its JSON catalogs to both apps, and remove it from `CANON` if it was being folded into another tag.

## Catalogs — hybrid, one marker per catalog

- **Core (`SharedResources`)** — generic / cross-cutting messages: ProblemDetails titles (`Error.*`), cross-module errors (`Error.TenantContextRequired`, `Error.NoCurrentUser`, …), and shared validation (`Validation.*`).
- **Per module (`<Module>Resources`)** — domain-specific messages owned by the module: `src/Modules/<Module>/Modules.<Module>/Localization/<Module>Resources.cs` (marker `public sealed class <Module>Resources;`) + co-located `<Module>Resources.resx` (neutral / en-US) + `<Module>Resources.pt-BR.resx`. `ResourcesPath = ""` (co-located), so the resx manifest name must equal the marker's full type name.

Catalogs are named for **specific** cultures (`.pt-BR`, never a neutral `.pt`), matching the front-end catalog folders. The neutral, un-suffixed `.resx` is the en-US / ultimate-fallback catalog.

Key naming: `Error.<Module>.<Case>` for domain messages (`Catalog.ProductNotFound`), `Error.<CrossCutting>` / `Validation.<Case>` for Core. PascalCase. Placeholders are `{0}`, `{1}` (`string.Format` via the localizer) — **not** the frontend's `{{name}}`.

**Placeholder arguments must be culture-insensitive.** The localizer's indexer calls `string.Format` under `CurrentCulture`, which is invariant (above). Pass `int`/`long`/`string`/enum — never a `double`, `decimal`, `DateTime` or `TimeSpan.TotalX`, which would render with an invariant separator instead of the reader's. Where a count is conceptually whole, expose it as an `int` at the source rather than converting at the call site (see `GetAuditsQueryHandler.MaxWindowDays` next to `MaxWindow`). Money and dates belong in structured response fields formatted by the client, not interpolated into a message.

## Exceptions — localize at the boundary, log stays English

Throw with the **English message** as `Exception.Message` (used for logs and fallback) plus the resource key metadata. **Never** pre-localize the message at the throw site.

```csharp
// domain message -> module catalog
throw new NotFoundException($"Product {id} not found.")
{
    MessageKey = "Catalog.ProductNotFound",
    MessageArgs = [id],
    ResourceSource = typeof(CatalogResources),
};

// cross-cutting message -> Core catalog (ResourceSource omitted = SharedResources)
throw new UnauthorizedException("Tenant context is required.")
{
    MessageKey = "Error.TenantContextRequired",
};
```

`GlobalExceptionHandler` resolves `Title` (by status) and `Detail` (via `MessageKey` + `ResourceSource`) under the request culture, and falls back to `Exception.Message` when the key is missing (`ResourceNotFound`) or malformed (`FormatException`). Migration is therefore incremental: an un-migrated `throw new NotFoundException("...")` still renders its English literal.

**Do NOT** set `ProblemDetails` from a localized string in logs — the handler logs `Exception.Message` (English) and the type name, never the translated body.

## Validators — inject the localizer, defer resolution

```csharp
public sealed class XCommandValidator : AbstractValidator<XCommand>
{
    public XCommandValidator(IStringLocalizer<SharedResources> localizer)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => localizer["Validation.NameRequired"]);
    }
}
```

Always the `.WithMessage(_ => localizer["Key"])` lambda (resolution is deferred to `Validate()`, under the request culture) — never `.WithMessage(localizer["Key"])`. **Catalog choice:** inject `IStringLocalizer<SharedResources>` for genuinely shared/generic validation (`Validation.*` already in Core, reuse them), or `IStringLocalizer<<Module>Resources>` for module-specific validation messages kept in the module's own catalog. DI provides the localizer automatically (`AddValidatorsFromAssembly` + `AddHeroLocalization` + the module's own `AddLocalization`); nested validators (`Include(new PagedQueryValidator<T>(localizer))`) receive it from the parent.

## Known behaviour (documented, not bugs)

- **The `locale` claim lags a language switch by one token.** The culture provider reads the JWT `locale` claim, so a switch does not reach the API until the next token issue. The front-end persists the choice to the profile and re-mints, so it converges; in the window between, the shell can be in the new language while an API error still comes back in the old one. Deliberate: the alternative is a per-request DB read on every authenticated call.
- **Impersonation carries the operator's language, not the target's.** `StartImpersonationCommandHandler` strips the target's `locale` claim so the operator keeps reading in their own language, and the cross-app handoff URL carries `locale` because the dashboard is normally on a different origin and cannot read admin's `i18nextLng`. During impersonation the switcher is client-side only — it must not PUT onto the impersonated user's profile.
- **SignalR does not carry the app locale.** The hub client builds its own requests instead of going through `apiFetch`, so `Accept-Language` on the negotiate is the browser's. Applies to every session. `handoff-locale.spec.ts` names the exception explicitly so any *other* channel that stops carrying the locale fails the test.

## Tests (required with every catalog change)

- **Parity** — every key present in both the neutral and the `pt-BR` catalog, for Core and every `<Module>Resources`. Per-catalog tests live in each module's test project; `CatalogParityTests` in `Architecture.Tests` enumerates every module catalog generically, so a **new** module catalog is covered without adding a test.
- **Code → resx guard** — every referenced key (`MessageKey`, `localizer["…"]`) must exist in its catalog, or the build fails. This is what catches a forgotten/typo `ResourceSource` (which would otherwise fall back silently).
- Build validators/handlers with a real localizer from the embedded catalog via `SharedResourcesLocalizerFactory.Create()` (test-project `Support/` helper), not a stub.

## Emails / background handlers

Integration-event handlers run without an HTTP request, so there is no negotiated culture. Localizing outbound emails needs the recipient's stored locale propagated to the handler — **not yet implemented** (tracked for a future PR); email bodies stay English for now.
