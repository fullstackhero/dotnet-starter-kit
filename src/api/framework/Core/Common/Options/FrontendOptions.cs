using System;

namespace FSH.Framework.Core.Common.Options;

public sealed class FrontendOptions
{
    public Uri BaseUrl { get; set; } = new Uri("http://localhost:3001");
}