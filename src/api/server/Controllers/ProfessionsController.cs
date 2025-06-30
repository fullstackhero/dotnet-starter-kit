using Microsoft.AspNetCore.Mvc;
using FSH.Framework.Core.Common.Interfaces;
using FSH.Starter.WebApi.Contracts.Common;

namespace FSH.Starter.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProfessionsController : ControllerBase
{
    private readonly IProfessionRepository _professionRepository;

    public ProfessionsController(IProfessionRepository professionRepository)
    {
        _professionRepository = professionRepository;
    }

    /// <summary>
    /// Get all active professions for dropdown list
    /// </summary>
    /// <returns>List of active professions</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProfessionDto>>), 200)]
    public async Task<IActionResult> GetAllProfessionsAsync()
    {
        try
        {
            var professions = await _professionRepository.GetAllActiveProfessionsAsync();
            return Ok(ApiResponse<IReadOnlyList<ProfessionDto>>.SuccessResult(professions));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<IReadOnlyList<ProfessionDto>>.FailureResult($"Error getting professions: {ex}"));
        }
    }

    /// <summary>
    /// Get profession by ID
    /// </summary>
    /// <param name="id">Profession ID</param>
    /// <returns>Profession details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ProfessionDto>), 200)]
    public async Task<IActionResult> GetProfessionByIdAsync(int id)
    {
        try
        {
            var profession = await _professionRepository.GetByIdAsync(id);
            if (profession == null)
            {
                return Ok(ApiResponse<ProfessionDto>.FailureResult("Profession not found"));
            }

            return Ok(ApiResponse<ProfessionDto>.SuccessResult(profession));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<ProfessionDto>.FailureResult($"Error getting profession: {ex}"));
        }
    }
}