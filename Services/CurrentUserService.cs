using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using WellnessPlatform.Models;

namespace WellnessPlatform.Services;

public class CurrentUserService(AuthenticationStateProvider authenticationStateProvider)
{
    public const string PermissionClaimType = "wellness.permission";

    public AppUser? User { get; private set; }

    public bool IsAuthenticated => User is not null;

    public bool IsClient => User?.Role == UserRole.Client;

    public bool IsStaff => User?.Role == UserRole.Staff;

    public bool IsAdmin => User?.Role == UserRole.Admin;

    public async Task InitializeAsync()
    {
        var principal =
            (await authenticationStateProvider.GetAuthenticationStateAsync())
                .User;

        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleValue = principal.FindFirstValue(ClaimTypes.Role);

        if (principal.Identity?.IsAuthenticated != true ||
            !int.TryParse(idValue, out var userId) ||
            !Enum.TryParse<UserRole>(roleValue, out var role))
        {
            User = null;
            return;
        }

        User = new AppUser
        {
            Id = userId,
            Email = principal.FindFirstValue(ClaimTypes.Email),
            FullName = principal.FindFirstValue(ClaimTypes.Name) ?? "",
            Role = role,
            Active = true,
            EmailConfirmed = true,
            UserPermissions = principal
                .FindAll(PermissionClaimType)
                .Select(claim => new UserPermission
                {
                    Permission = new Permission { Key = claim.Value }
                })
                .ToList()
        };
    }

    public bool HasPermission(string permissionKey)
    {
        if (User is null)
        {
            return false;
        }

        if (User.Role == UserRole.Admin)
        {
            return true;
        }

        return User.UserPermissions.Any(permission =>
            permission.Permission?.Key == permissionKey);
    }
}
