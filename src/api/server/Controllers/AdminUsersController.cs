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

namespace FSH.Framework.Server.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = "admin,customer_admin")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminUsersController(IMediator mediator)
    {
        _mediator = mediator;
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
        // Validate all Value Objects
        var emailResult = Email.Create(request.Email);
        if (!emailResult.IsSuccess)
            return BadRequest(ApiResponse.FailureResult($"Email error: {emailResult.Error}"));

        var usernameResult = Username.Create(request.Username);
        if (!usernameResult.IsSuccess)
            return BadRequest(ApiResponse.FailureResult($"Username error: {usernameResult.Error}"));

        var phoneResult = PhoneNumber.Create(request.PhoneNumber);
        if (!phoneResult.IsSuccess)
            return BadRequest(ApiResponse.FailureResult($"Phone error: {phoneResult.Error}"));

        var tcknResult = Tckn.Create(request.Tckn);
        if (!tcknResult.IsSuccess)
            return BadRequest(ApiResponse.FailureResult($"TCKN error: {tcknResult.Error}"));

        var passwordResult = Password.Create(request.Password);
        if (!passwordResult.IsSuccess)
            return BadRequest(ApiResponse.FailureResult($"Password error: {passwordResult.Error}"));

        var firstNameResult = Name.Create(request.FirstName);
        if (!firstNameResult.IsSuccess)
            return BadRequest(ApiResponse.FailureResult($"First name error: {firstNameResult.Error}"));

        var lastNameResult = Name.Create(request.LastName);
        if (!lastNameResult.IsSuccess)
            return BadRequest(ApiResponse.FailureResult($"Last name error: {lastNameResult.Error}"));

        var professionResult = Profession.Create(request.Profession);
        if (!professionResult.IsSuccess)
            return BadRequest(ApiResponse.FailureResult($"Profession error: {professionResult.Error}"));

        var birthDateResult = BirthDate.Create(request.BirthDate);
        if (!birthDateResult.IsSuccess)
            return BadRequest(ApiResponse.FailureResult($"Birth date error: {birthDateResult.Error}"));

        var command = new CreateUserCommand
        {
            Email = emailResult.Value!,
            Username = usernameResult.Value!,
            PhoneNumber = phoneResult.Value!,
            Tckn = tcknResult.Value!,
            Password = passwordResult.Value!,
            FirstName = firstNameResult.Value!,
            LastName = lastNameResult.Value!,
            Profession = professionResult.Value,
            BirthDate = birthDateResult.Value!,
            Status = request.Status,
            IsIdentityVerified = request.IsIdentityVerified,
            IsPhoneVerified = request.IsPhoneVerified,
            IsEmailVerified = request.IsEmailVerified
        };

        var result = await _mediator.Send(command);
        if (result.IsSuccess && result.Value is not null)
        {
            return CreatedAtAction(
                nameof(GetUsersAsync),
                new { id = result.Value.UserId },
                ApiResponse<CreateUserResult>.SuccessResult(result.Value));
        }

        return BadRequest(ApiResponse.FailureResult(result.Error ?? "Failed to create user"));
    }

    [HttpPut("{id}")]
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

        var professionResult = Profession.Create(request.Profession);
        if (!professionResult.IsSuccess)
            return BadRequest(ApiResponse.FailureResult($"Profession error: {professionResult.Error}"));

        var command = new UpdateUserCommand
        {
            UserId = id,
            Email = emailResult.Value!,
            Username = usernameResult.Value!,
            FirstName = firstNameResult.Value!,
            LastName = lastNameResult.Value!,
            PhoneNumber = phoneResult.Value!,
            Profession = professionResult.Value,
            Status = request.Status,
            IsIdentityVerified = request.IsIdentityVerified,
            IsPhoneVerified = request.IsPhoneVerified,
            IsEmailVerified = request.IsEmailVerified
        };

        var result = await _mediator.Send(command);
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(ApiResponse<UpdateUserResult>.SuccessResult(result.Value));
        }

        return result.Error == "User not found"
            ? NotFound(ApiResponse.FailureResult(result.Error))
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
