using System.Net;
using Microsoft.AspNetCore.Identity;
using ProjectME_BE.Domain.Model.User;

namespace ProjectME_BE.Application.Services;

public class NoOpEmailSender
{
    public Task SendConfirmationLinkAsync(UserAggregate user, string email, string confirmationLink)
    {
        // HTML-decode the link (&amp; -> &)
        var decodedLink = WebUtility.HtmlDecode(confirmationLink);

        Console.WriteLine("========================================");
        Console.WriteLine($"[EMAIL] Confirmation link for: {email}");
        Console.WriteLine($"[LINK] {decodedLink}");
        Console.WriteLine("========================================");
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(UserAggregate user, string email, string resetLink)
    {
        var decodedLink = WebUtility.HtmlDecode(resetLink);

        Console.WriteLine("========================================");
        Console.WriteLine($"[EMAIL] Password reset link for: {email}");
        Console.WriteLine($"[LINK] {decodedLink}");
        Console.WriteLine("========================================");
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(UserAggregate user, string email, string resetCode)
    {
        Console.WriteLine("========================================");
        Console.WriteLine($"[EMAIL] Password reset code for: {email}");
        Console.WriteLine($"[CODE] {resetCode}");
        Console.WriteLine("========================================");
        return Task.CompletedTask;
    }
}
