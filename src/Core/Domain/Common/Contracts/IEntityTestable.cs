using System.ComponentModel.DataAnnotations;

namespace FSH.WebApi.Domain.Common.Contracts;
/// <summary>
/// Interface used only test case.
/// </summary>
public interface IEntityTestable
{
    [StringLength(20)]
    string? InternalCode { get; set; }
    void SetInternalCode(int? num);
    void CleanInternalCode();
}
