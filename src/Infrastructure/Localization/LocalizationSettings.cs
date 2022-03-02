using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Infrastructure.Localization;
public class LocalizationSettings
{
    public string[]? SupportedCultures { get; set; }
    public string? ResourcesPath { get; set; }
    public string? DefaultRequestCulture { get; set; }
    public bool? EnableLocalization { get; set; }
}
