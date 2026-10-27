using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using PhoenixWatcho.Models;

namespace PhoenixWatcho.Services;

/// <summary>
/// Cookie-auth claims for the two kinds of user: a subscriber (identified by Id and
/// VC number) and the admin. Controllers use these helpers to check that a subscriber
/// only reads or changes their own account.
/// </summary>
public static class AuthExtensions
{
    public const string AdminRole      = "Admin";
    public const string SubscriberRole = "Subscriber";
    private const string VcClaim       = "vc";

    public static ClaimsPrincipal ForSubscriber(Subscriber s) =>
        new(new ClaimsIdentity(
        new[]
        {
            new Claim(ClaimTypes.NameIdentifier, s.Id.ToString()),
            new Claim(ClaimTypes.Name, s.FullName),
            new Claim(VcClaim, s.VcNumber),
            new Claim(ClaimTypes.Role, SubscriberRole)
        }, CookieAuthenticationDefaults.AuthenticationScheme));

    public static ClaimsPrincipal ForAdmin() =>
        new(new ClaimsIdentity(
        new[]
        {
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, AdminRole)
        }, CookieAuthenticationDefaults.AuthenticationScheme));

    /// <summary>The signed-in subscriber's VC number, or null for anyone else.</summary>
    public static string? VcNumber(this ClaimsPrincipal user) =>
        user.IsInRole(SubscriberRole) ? user.FindFirst(VcClaim)?.Value : null;

    /// <summary>The signed-in subscriber's Id, or null for anyone else.</summary>
    public static int? SubscriberId(this ClaimsPrincipal user) =>
        user.IsInRole(SubscriberRole) && int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
            ? id
            : null;

    /// <summary>True for the admin, or for the subscriber who owns this VC number.</summary>
    public static bool CanAccessVc(this ClaimsPrincipal user, string vcNumber) =>
        user.IsInRole(AdminRole) || user.VcNumber() == vcNumber;

    /// <summary>True for the admin, or for the subscriber with this Id.</summary>
    public static bool CanAccessSubscriber(this ClaimsPrincipal user, int subscriberId) =>
        user.IsInRole(AdminRole) || user.SubscriberId() == subscriberId;
}
