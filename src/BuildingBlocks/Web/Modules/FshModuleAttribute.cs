using System;

namespace FSH.Framework.Web.Modules;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class FshModuleAttribute(Type moduleType, int order = 0) : Attribute
{
    public Type ModuleType { get; } = moduleType ?? throw new ArgumentNullException(nameof(moduleType));

    /// <summary>
    /// Optional ordering hint that allows hosts to control module startup sequencing.
    /// Lower numbers execute first.
    /// </summary>
    public int Order { get; } = order;
}