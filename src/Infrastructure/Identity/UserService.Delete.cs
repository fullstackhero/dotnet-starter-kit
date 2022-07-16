using FSH.WebApi.Application.Common.Exceptions;
using FSH.WebApi.Domain.Identity;

namespace FSH.WebApi.Infrastructure.Identity;

internal partial class UserService
{
    public async Task<string> DeleteAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);

        _ = user ?? throw new NotFoundException(_t["User Not Found."]);

        string currentImage = user.ImageUrl ?? string.Empty;
        if (!string.IsNullOrEmpty(currentImage))
        {
            string root = Directory.GetCurrentDirectory();
            _fileStorage.Remove(Path.Combine(root, currentImage));
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        var result = await _userManager.RemoveFromRolesAsync(user, userRoles);

        if (!result.Succeeded)
        {
            throw new InternalServerException(_t["Remove role(s) failed."], result.GetErrors(_t));
        }

        result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            throw new InternalServerException(_t["Remove user failed."], result.GetErrors(_t));
        }

        await _events.PublishAsync(new ApplicationUserDeletedEvent(user.Id));
        return string.Format(_t["{0} succesfully deleted."], user.UserName);
    }
}
