using System.ComponentModel.DataAnnotations;

namespace FSH.WebApi.Domain.Common.Contracts;

public interface IEntityTestable
{
    [StringLength(20)]
    string? InternalCode { get; set; }
    void SetInternalCode(int? num);
    void CleanInternalCode();
}
