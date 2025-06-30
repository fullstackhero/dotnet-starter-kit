using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FSH.Framework.Core.Auth.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Core.Auth.Features.Admin;

public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, AssignRoleResult>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AssignRoleCommandHandler> _logger;

    public AssignRoleCommandHandler(
        IUserRepository userRepository,
        ILogger<AssignRoleCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<AssignRoleResult> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        // Check if user exists
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {request.UserId} not found");
        }

        // Get current roles
        var currentRoles = await _userRepository.GetUserRolesAsync(request.UserId);

        // If role is already assigned, return success
        if (currentRoles.Contains(request.Role, StringComparer.Ordinal))
        {
            return new AssignRoleResult
            {
                UserId = request.UserId,
                Role = request.Role,
                Message = "Role was already assigned to the user"
            };
        }

        // Assign new role
        await _userRepository.AssignRoleAsync(request.UserId, request.Role);

        _logger.LogInformation("Admin assigned role {Role} to user {UserId}", request.Role, request.UserId);

        return new AssignRoleResult
        {
            UserId = request.UserId,
            Role = request.Role,
            Message = "Role assigned successfully"
        };
    }
}