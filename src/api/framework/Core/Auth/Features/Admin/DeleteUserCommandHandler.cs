using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FSH.Framework.Core.Auth.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Core.Auth.Features.Admin;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, DeleteUserResult>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<DeleteUserCommandHandler> _logger;

    public DeleteUserCommandHandler(
        IUserRepository userRepository,
        ILogger<DeleteUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<DeleteUserResult> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        // Check if user exists
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {request.UserId.ToString()} not found");
        }

        // Check if user is an admin (prevent deleting admin users)
        var roles = await _userRepository.GetUserRolesAsync(request.UserId);
        if (roles.Contains("admin", StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Cannot delete admin users");
        }

        // Delete user
        await _userRepository.DeleteUserAsync(request.UserId);

        _logger.LogInformation("Admin deleted user with ID {UserId}", request.UserId);

        return new DeleteUserResult 
        { 
            UserId = request.UserId, 
            Message = "User deleted successfully" 
        };
    }
}