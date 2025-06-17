using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using FSH.Framework.Core.Common.Interfaces;
using FSH.Framework.Core.Common.Exceptions;

namespace FSH.Framework.Infrastructure.Common;

public sealed class DapperProfessionRepository : IProfessionRepository
{
    private readonly IDbConnection _connection;
    private readonly ILogger<DapperProfessionRepository> _logger;

    public DapperProfessionRepository(IDbConnection connection, ILogger<DapperProfessionRepository> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<ProfessionDto>> GetAllActiveProfessionsAsync()
    {
        try
        {
            const string sql = @"
                SELECT id, name, is_active as IsActive, sort_order as SortOrder
                FROM professions 
                WHERE is_active = TRUE 
                ORDER BY sort_order ASC, name ASC";

            var professions = await _connection.QueryAsync<ProfessionDto>(sql);
            return professions.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active professions");
            throw new FshException("Error getting active professions", ex);
        }
    }

    public async Task<ProfessionDto?> GetByIdAsync(int id)
    {
        try
        {
            const string sql = @"
                SELECT id, name, is_active as IsActive, sort_order as SortOrder
                FROM professions 
                WHERE id = @Id";

            var profession = await _connection.QueryFirstOrDefaultAsync<ProfessionDto>(sql, new { Id = id });
            return profession;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profession by id {ProfessionId}", id);
            throw new FshException($"Error getting profession by id {id}", ex);
        }
    }
} 