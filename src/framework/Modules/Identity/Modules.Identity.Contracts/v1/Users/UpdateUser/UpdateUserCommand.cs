﻿using FSH.Framework.Core.Storage;

namespace FSH.Framework.Identity.Contracts.v1.Users.UpdateUser;
public class UpdateUserCommand
{
    public string Id { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public FileUploadRequest? Image { get; set; }
    public bool DeleteCurrentImage { get; set; }
}