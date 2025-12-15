using System.Text.RegularExpressions;
using FluentValidation;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Contracts.v1.UpdateTenantTheme;

namespace FSH.Modules.Multitenancy.Features.v1.UpdateTenantTheme;

public partial class UpdateTenantThemeCommandValidator : AbstractValidator<UpdateTenantThemeCommand>
{
    public UpdateTenantThemeCommandValidator()
    {
        RuleFor(x => x.Theme)
            .NotNull()
            .WithMessage("Theme is required.");

        RuleFor(x => x.Theme.LightPalette)
            .NotNull()
            .SetValidator(new PaletteValidator());

        RuleFor(x => x.Theme.DarkPalette)
            .NotNull()
            .SetValidator(new PaletteValidator());

        RuleFor(x => x.Theme.Typography)
            .NotNull()
            .SetValidator(new TypographyValidator());

        RuleFor(x => x.Theme.Layout)
            .NotNull()
            .SetValidator(new LayoutValidator());
    }

    [GeneratedRegex("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})$")]
    private static partial Regex HexColorRegex();

    private sealed class PaletteValidator : AbstractValidator<PaletteDto>
    {
        public PaletteValidator()
        {
            RuleFor(x => x.Primary).Must(BeValidHexColor).WithMessage("Primary must be a valid hex color.");
            RuleFor(x => x.Secondary).Must(BeValidHexColor).WithMessage("Secondary must be a valid hex color.");
            RuleFor(x => x.Tertiary).Must(BeValidHexColor).WithMessage("Tertiary must be a valid hex color.");
            RuleFor(x => x.Background).Must(BeValidHexColor).WithMessage("Background must be a valid hex color.");
            RuleFor(x => x.Surface).Must(BeValidHexColor).WithMessage("Surface must be a valid hex color.");
            RuleFor(x => x.Error).Must(BeValidHexColor).WithMessage("Error must be a valid hex color.");
            RuleFor(x => x.Warning).Must(BeValidHexColor).WithMessage("Warning must be a valid hex color.");
            RuleFor(x => x.Success).Must(BeValidHexColor).WithMessage("Success must be a valid hex color.");
            RuleFor(x => x.Info).Must(BeValidHexColor).WithMessage("Info must be a valid hex color.");
        }

        private static bool BeValidHexColor(string color) =>
            !string.IsNullOrWhiteSpace(color) && HexColorRegex().IsMatch(color);
    }

    private sealed class TypographyValidator : AbstractValidator<TypographyDto>
    {
        public TypographyValidator()
        {
            RuleFor(x => x.FontFamily)
                .NotEmpty()
                .MaximumLength(200)
                .Must(BeValidFontFamily)
                .WithMessage("FontFamily must be a valid web-safe font.");

            RuleFor(x => x.HeadingFontFamily)
                .NotEmpty()
                .MaximumLength(200)
                .Must(BeValidFontFamily)
                .WithMessage("HeadingFontFamily must be a valid web-safe font.");

            RuleFor(x => x.FontSizeBase)
                .InclusiveBetween(10, 24)
                .WithMessage("FontSizeBase must be between 10 and 24.");

            RuleFor(x => x.LineHeightBase)
                .InclusiveBetween(1.0, 2.5)
                .WithMessage("LineHeightBase must be between 1.0 and 2.5.");
        }

        private static bool BeValidFontFamily(string fontFamily) =>
            TypographyDto.WebSafeFonts.Contains(fontFamily);
    }

    private sealed class LayoutValidator : AbstractValidator<LayoutDto>
    {
        public LayoutValidator()
        {
            RuleFor(x => x.BorderRadius)
                .NotEmpty()
                .MaximumLength(20)
                .Matches(@"^\d+(px|rem|em|%)$")
                .WithMessage("BorderRadius must be a valid CSS value (e.g., '4px', '0.5rem').");

            RuleFor(x => x.DefaultElevation)
                .InclusiveBetween(0, 24)
                .WithMessage("DefaultElevation must be between 0 and 24.");
        }
    }
}
