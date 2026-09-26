using FluentValidation;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Contracts.v1.UpdateTenantTheme;
using FSH.Modules.Multitenancy.Localization;
using Microsoft.Extensions.Localization;
using System.Text.RegularExpressions;

namespace FSH.Modules.Multitenancy.Features.v1.UpdateTenantTheme;

public partial class UpdateTenantThemeCommandValidator : AbstractValidator<UpdateTenantThemeCommand>
{
    public UpdateTenantThemeCommandValidator(IStringLocalizer<MultitenancyResources> localizer)
    {
        RuleFor(x => x.Theme)
            .NotNull()
            .WithMessage(_ => localizer["Validation.ThemeRequired"]);

        RuleFor(x => x.Theme.LightPalette)
            .NotNull()
            .SetValidator(new PaletteValidator(localizer));

        RuleFor(x => x.Theme.DarkPalette)
            .NotNull()
            .SetValidator(new PaletteValidator(localizer));

        RuleFor(x => x.Theme.Typography)
            .NotNull()
            .SetValidator(new TypographyValidator(localizer));

        RuleFor(x => x.Theme.Layout)
            .NotNull()
            .SetValidator(new LayoutValidator(localizer));
    }

    [GeneratedRegex("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})$")]
    private static partial Regex HexColorRegex();

    private sealed class PaletteValidator : AbstractValidator<PaletteDto>
    {
        public PaletteValidator(IStringLocalizer<MultitenancyResources> localizer)
        {
            RuleFor(x => x.Primary).Must(BeValidHexColor).WithMessage(_ => localizer["Validation.ColorMustBeHex", "Primary"]);
            RuleFor(x => x.Secondary).Must(BeValidHexColor).WithMessage(_ => localizer["Validation.ColorMustBeHex", "Secondary"]);
            RuleFor(x => x.Tertiary).Must(BeValidHexColor).WithMessage(_ => localizer["Validation.ColorMustBeHex", "Tertiary"]);
            RuleFor(x => x.Background).Must(BeValidHexColor).WithMessage(_ => localizer["Validation.ColorMustBeHex", "Background"]);
            RuleFor(x => x.Surface).Must(BeValidHexColor).WithMessage(_ => localizer["Validation.ColorMustBeHex", "Surface"]);
            RuleFor(x => x.Error).Must(BeValidHexColor).WithMessage(_ => localizer["Validation.ColorMustBeHex", "Error"]);
            RuleFor(x => x.Warning).Must(BeValidHexColor).WithMessage(_ => localizer["Validation.ColorMustBeHex", "Warning"]);
            RuleFor(x => x.Success).Must(BeValidHexColor).WithMessage(_ => localizer["Validation.ColorMustBeHex", "Success"]);
            RuleFor(x => x.Info).Must(BeValidHexColor).WithMessage(_ => localizer["Validation.ColorMustBeHex", "Info"]);
        }

        private static bool BeValidHexColor(string color) =>
            !string.IsNullOrWhiteSpace(color) && HexColorRegex().IsMatch(color);
    }

    private sealed class TypographyValidator : AbstractValidator<TypographyDto>
    {
        public TypographyValidator(IStringLocalizer<MultitenancyResources> localizer)
        {
            RuleFor(x => x.FontFamily)
                .NotEmpty()
                .MaximumLength(200)
                .Must(BeValidFontFamily)
                .WithMessage(_ => localizer["Validation.FontFamilyWebSafe", "FontFamily"]);

            RuleFor(x => x.HeadingFontFamily)
                .NotEmpty()
                .MaximumLength(200)
                .Must(BeValidFontFamily)
                .WithMessage(_ => localizer["Validation.FontFamilyWebSafe", "HeadingFontFamily"]);

            RuleFor(x => x.FontSizeBase)
                .InclusiveBetween(10, 24)
                .WithMessage(_ => localizer["Validation.FontSizeBaseRange"]);

            RuleFor(x => x.LineHeightBase)
                .InclusiveBetween(1.0, 2.5)
                .WithMessage(_ => localizer["Validation.LineHeightBaseRange"]);
        }

        private static bool BeValidFontFamily(string fontFamily) =>
            TypographyDto.WebSafeFonts.Contains(fontFamily);
    }

    private sealed class LayoutValidator : AbstractValidator<LayoutDto>
    {
        public LayoutValidator(IStringLocalizer<MultitenancyResources> localizer)
        {
            RuleFor(x => x.BorderRadius)
                .NotEmpty()
                .MaximumLength(20)
                .Matches(@"^\d+(px|rem|em|%)$")
                .WithMessage(_ => localizer["Validation.BorderRadiusInvalid"]);

            RuleFor(x => x.DefaultElevation)
                .InclusiveBetween(0, 24)
                .WithMessage(_ => localizer["Validation.DefaultElevationRange"]);
        }
    }
}