using Microsoft.AspNetCore.Identity;
using WellnessPlatform.Models;

namespace WellnessPlatform.Services;

public sealed class LegacyAwarePasswordHasher(PasswordService legacyPasswordService)
    : IPasswordHasher<AppUser>
{
    private readonly PasswordHasher<AppUser> identityHasher = new();

    public string HashPassword(AppUser user, string password)
    {
        return identityHasher.HashPassword(user, password);
    }

    public PasswordVerificationResult VerifyHashedPassword(
        AppUser user,
        string hashedPassword,
        string providedPassword)
    {
        try
        {
            var result = identityHasher.VerifyHashedPassword(
                user,
                hashedPassword,
                providedPassword);

            if (result != PasswordVerificationResult.Failed)
            {
                return result;
            }
        }
        catch (FormatException)
        {
            // Older accounts use the app's original salt.hash format.
        }

        return legacyPasswordService.VerifyPassword(
            providedPassword,
            hashedPassword)
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Failed;
    }
}
