namespace FSH.WebApi.Application.Identity.Users;

public class UpdateUserRequest : IRequest
{
    public string Id { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public FileUploadRequest? Image { get; set; }
    public bool DeleteCurrentImage { get; set; } = false;
}

public class UpdateUserRequestHandler : AsyncRequestHandler<UpdateUserRequest>
{
    private readonly IUserService _userService;
    public UpdateUserRequestHandler(IUserService userService) => _userService = userService;

    protected override Task Handle(UpdateUserRequest req, CancellationToken ct) =>
        _userService.UpdateAsync(req, ct);
}