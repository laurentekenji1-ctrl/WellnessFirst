using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using WellnessPlatform.Models;

namespace WellnessPlatform.Services;

public sealed class AccountEmailService(
    UserManager<AppUser> userManager,
    IPlatformEmailSender emailSender,
    IConfiguration configuration)
{
    public bool IsConfigured => emailSender.IsConfigured;

    public async Task SendConfirmationEmailAsync(AppUser user)
    {
        EnsureConfigured();
        var email = user.Email
            ?? throw new InvalidOperationException(
                "The account does not have an email address.");
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(token));
        var link = BuildLink(
            "/confirm-email",
            ("userId", user.Id.ToString()),
            ("token", encodedToken));
        var safeLink = HtmlEncoder.Default.Encode(link);

        await emailSender.SendAsync(
            email,
            "Verify your Wellness Platform email",
            $"Confirm your email address by opening this link: {link}",
            $"<p>Confirm your email address to activate your account.</p><p><a href=\"{safeLink}\">Verify email address</a></p>");
    }

    public async Task RequestConfirmationEmailAsync(string email)
    {
        EnsureConfigured();
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null || !user.Active || user.EmailConfirmed)
        {
            return;
        }

        await SendConfirmationEmailAsync(user);
    }

    public async Task RequestPasswordResetEmailAsync(string email)
    {
        EnsureConfigured();
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null || !user.Active || !user.EmailConfirmed)
        {
            return;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(token));
        var link = BuildLink(
            "/reset-password",
            ("userId", user.Id.ToString()),
            ("token", encodedToken));
        var safeLink = HtmlEncoder.Default.Encode(link);

        await emailSender.SendAsync(
            user.Email!,
            "Reset your Wellness Platform password",
            $"Reset your password by opening this link: {link}",
            $"<p>Use the link below to reset your Wellness Platform password.</p><p><a href=\"{safeLink}\">Reset password</a></p>");
    }

    public async Task<IdentityResult> ConfirmEmailAsync(
        string userId,
        string encodedToken)
    {
        if (!int.TryParse(userId, out var parsedUserId))
        {
            return InvalidTokenResult();
        }

        var user = await userManager.FindByIdAsync(parsedUserId.ToString());
        if (user is null ||
            !TryDecodeToken(encodedToken, out var token))
        {
            return InvalidTokenResult();
        }

        return await userManager.ConfirmEmailAsync(user, token);
    }

    public async Task<IdentityResult> ResetPasswordAsync(
        string userId,
        string encodedToken,
        string password)
    {
        if (!int.TryParse(userId, out var parsedUserId))
        {
            return InvalidTokenResult();
        }

        var user = await userManager.FindByIdAsync(parsedUserId.ToString());
        if (user is null ||
            !user.Active ||
            !user.EmailConfirmed ||
            !TryDecodeToken(encodedToken, out var token))
        {
            return InvalidTokenResult();
        }

        return await userManager.ResetPasswordAsync(user, token, password);
    }

    private string BuildLink(
        string path,
        params (string Key, string Value)[] parameters)
    {
        var baseUrl = configuration["Email:PublicBaseUrl"]?.TrimEnd('/')
            ?? throw new EmailDeliveryNotConfiguredException();
        var values = parameters.ToDictionary(
            parameter => parameter.Key,
            parameter => (string?)parameter.Value);
        return QueryHelpers.AddQueryString($"{baseUrl}{path}", values);
    }

    private void EnsureConfigured()
    {
        if (!emailSender.IsConfigured)
        {
            throw new EmailDeliveryNotConfiguredException();
        }
    }

    private static bool TryDecodeToken(
        string encodedToken,
        out string token)
    {
        token = "";
        try
        {
            token = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(encodedToken));
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static IdentityResult InvalidTokenResult()
    {
        return IdentityResult.Failed(new IdentityError
        {
            Description = "This link is invalid or has expired."
        });
    }
}
