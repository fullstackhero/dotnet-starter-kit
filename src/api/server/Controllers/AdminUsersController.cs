using System.Net;
using Asp.Versioning;
using FSH.Framework.Core.Auth.Dtos;
using FSH.Framework.Core.Auth.Features.Admin;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using FSH.Framework.Core.Common.Models;
using FSH.Starter.WebApi.Contracts.Admin;
using FSH.Starter.WebApi.Contracts.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace FSH.Framework.Server.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = "admin,customer_admin")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(IMediator mediator, ILogger<AdminUsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserListItemDto>>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetUsersAsync()
    {
        var users = await _mediator.Send(new GetUsersQuery());
        return Ok(ApiResponse<IReadOnlyList<UserListItemDto>>.SuccessResult(users));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateUserResult>), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> CreateUserAsync([FromBody] AdminCreateUserCommand request)
    {
        _logger.LogInformation("Attempting to create user with email {email}", request.Email);

        var emailResult = Email.Create(request.Email);
        var usernameResult = Username.Create(request.Username);
        var phoneResult = PhoneNumber.Create(request.PhoneNumber);
        var tcknResult = Tckn.Create(request.Tckn);
        var passwordResult = Password.Create(request.Password);
        var firstNameResult = Name.Create(request.FirstName);
        var lastNameResult = Name.Create(request.LastName);
        var birthDateResult = BirthDate.Create(request.BirthDate);

        var errors = new List<string>();
        if (!emailResult.IsSuccess) errors.Add(emailResult.Error!);
        if (!usernameResult.IsSuccess) errors.Add(usernameResult.Error!);
        if (!phoneResult.IsSuccess) errors.Add(phoneResult.Error!);
        if (!tcknResult.IsSuccess) errors.Add(tcknResult.Error!);
        if (!passwordResult.IsSuccess) errors.Add(passwordResult.Error!);
        if (!firstNameResult.IsSuccess) errors.Add(firstNameResult.Error!);
        if (!lastNameResult.IsSuccess) errors.Add(lastNameResult.Error!);
        if (!birthDateResult.IsSuccess) errors.Add(birthDateResult.Error!);

        if (errors.Any())
        {
            _logger.LogWarning("User creation validation failed: {errors}", string.Join(", ", errors));
            return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
        }

        var command = new CreateUserCommand
        {
            Email = emailResult.Value!,
            Username = usernameResult.Value!,
            PhoneNumber = phoneResult.Value!,
            Tckn = tcknResult.Value!,
            Password = passwordResult.Value!,
            FirstName = firstNameResult.Value!,
            LastName = lastNameResult.Value!,
            BirthDate = birthDateResult.Value!,
            IsEmailVerified = request.IsEmailVerified,
            ProfessionId = request.ProfessionId,
            Status = request.Status
        };

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetUsersAsync), new { id = result.Value!.UserId }, ApiResponse<CreateUserResult>.SuccessResult(result.Value!));
        }

        return BadRequest(ApiResponse.FailureResult(result.Error!));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateUserResult>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> UpdateUserAsync(Guid id, [FromBody] AdminUpdateUserCommand request)
    {
        // Validate all Value Objects
        var emailResult = Email.Create(request.Email);
        if (!emailResult.IsSuccess)
            return BadRequest(ApiResponse.FailureResult($"Email error: {emailResult.Error}"));

        var usernameResult = Username.Create(request.Username);
        if (!usernameResult.IsSuccess)
            return BadRequest(ApiResponse.FailureResult($"Username error: {usernameResult.Error}"));

        var firstNameResult = Name.Create(request.FirstName);
        if (!firstNameResult.IsSuccess)
            return BadRequest(ApiResponse.FailureResult($"First name error: {firstNameResult.Error}"));

        var lastNameResult = Name.Create(request.LastName);
        if (!lastNameResult.IsSuccess)
            return BadRequest(ApiResponse.FailureResult($"Last name error: {lastNameResult.Error}"));

        var phoneResult = PhoneNumber.Create(request.PhoneNumber);
        if (!phoneResult.IsSuccess)
            return BadRequest(ApiResponse.FailureResult($"Phone error: {phoneResult.Error}"));

        var command = new UpdateUserCommand
        {
            UserId = id,
            Email = emailResult.Value!,
            Username = usernameResult.Value!,
            FirstName = firstNameResult.Value!,
            LastName = lastNameResult.Value!,
            PhoneNumber = phoneResult.Value!,
            ProfessionId = request.ProfessionId,
            Status = request.Status,
            IsEmailVerified = request.IsEmailVerified
        };

        var result = await _mediator.Send(command);
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(ApiResponse<UpdateUserResult>.SuccessResult(result.Value));
        }

        return string.Equals(result.Error, "User not found", StringComparison.Ordinal)
            ? NotFound(ApiResponse.FailureResult(result.Error ?? "User not found"))
            : BadRequest(ApiResponse.FailureResult(result.Error ?? "Failed to update user"));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DeleteUserResult>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> DeleteUserAsync(Guid id)
    {
        var command = new DeleteUserCommand { UserId = id };
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<DeleteUserResult>.SuccessResult(result));
    }

    [HttpPost("{id}/roles")]
    [ProducesResponseType(typeof(ApiResponse<AssignRoleResult>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> AssignRoleAsync(Guid id, [FromBody] AdminAssignRoleRequest request)
    {
        var assignCommand = new FSH.Framework.Core.Auth.Features.Admin.AssignRoleCommand
        {
            UserId = id,
            Role = request.Role
        };

        var result = await _mediator.Send(assignCommand);
        return Ok(ApiResponse<AssignRoleResult>.SuccessResult(result));
    }
}

public sealed class AdminAssignRoleRequest
{
    public string Role { get; init; } = default!;
}
