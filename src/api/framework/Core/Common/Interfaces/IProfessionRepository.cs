using FSH.Framework.Core.Common.Models;

namespace FSH.Framework.Core.Common.Interfaces;

public interface IProfessionRepository
{
    Task<IReadOnlyList<ProfessionDto>> GetAllActiveProfessionsAsync();
    Task<ProfessionDto?> GetByIdAsync(int id);
}

public class ProfessionDto
{
    public int Id { get; init; }
    public string Name { get; init; } = default!;
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
} 