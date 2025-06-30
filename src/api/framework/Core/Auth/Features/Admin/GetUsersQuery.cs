using System.Collections.Generic;
using MediatR;
using FSH.Framework.Core.Auth.Dtos;

namespace FSH.Framework.Core.Auth.Features.Admin;

public record GetUsersQuery : IRequest<IReadOnlyList<UserListItemDto>>;