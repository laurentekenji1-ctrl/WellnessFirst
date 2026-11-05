using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using WellnessPlatform.Data;
using WellnessPlatform.Models;

namespace WellnessPlatform.Services;

public sealed class AppUserClaimsPrincipalFactory(
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole<int>> roleManager,
    IOptions<IdentityOptions> options,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : UserClaimsPrincipalFactory<AppUser, IdentityRole<int>>(
        userManager,
        roleManager,
        options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        var nameClaimType = Options.ClaimsIdentity.UserNameClaimType;

        foreach (var claim in identity.FindAll(nameClaimType).ToList())
        {
            identity.RemoveClaim(claim);
        }

        identity.AddClaim(new Claim(nameClaimType, user.FullName));
        identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));

        await using var db = await dbContextFactory.CreateDbContextAsync();
        var permissionKeys = await db.UserPermissions
            .Where(userPermission => userPermission.UserId == user.Id)
            .Select(userPermission => userPermission.Permission!.Key)
            .ToListAsync();

        foreach (var permissionKey in permissionKeys)
        {
            identity.AddClaim(new Claim(
                CurrentUserService.PermissionClaimType,
                permissionKey));
        }

        return identity;
    }
}
