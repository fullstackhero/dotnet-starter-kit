using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using FSH.Framework.Core.Auth.Repositories;
using FSH.Framework.Core.Auth.Dtos;

namespace FSH.Framework.Core.Auth.Features.Admin;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IReadOnlyList<UserListItemDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUsersQueryHandler> _logger;

    public GetUsersQueryHandler(
        IUserRepository userRepository,
        ILogger<GetUsersQueryHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<UserListItemDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Getting all users");

        var users = await _userRepository.GetAllAsync()
            .ConfigureAwait(false);

        var userDtos = users.Select(u => new UserListItemDto
        {
            Id = u.Id,
            Email = u.Email.Value,
            Username = u.Username,
            FirstName = u.FirstName,
            LastName = u.LastName,
            PhoneNumber = u.PhoneNumber.Value,
            IsActive = string.Equals(u.Status, "ACTIVE", StringComparison.Ordinal)
        }).ToList();

        _logger.LogInformation("Retrieved {Count} users", userDtos.Count);

        return userDtos;
    }
}