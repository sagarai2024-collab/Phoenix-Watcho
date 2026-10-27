using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoenixWatcho.Data;
using PhoenixWatcho.Models;
using PhoenixWatcho.Services;

namespace PhoenixWatcho.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriberController(AppDbContext db, IPasswordHasher<Subscriber> hasher) : ControllerBase
{
    /// <summary>
    /// The unsalted SHA256 format used before PasswordHasher. Kept only so accounts
    /// created with it can still sign in; they are re-hashed on their next login.
    /// </summary>
    private static string LegacySha256(string password) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password))).ToLower();

    private bool VerifyAndUpgrade(Subscriber subscriber, string password)
    {
        // Legacy unsalted SHA256 hex: verify the old way, then upgrade the stored hash.
        if (subscriber.Password.Length == 64 && subscriber.Password.All(Uri.IsHexDigit))
        {
            var matches = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(subscriber.Password),
                Encoding.UTF8.GetBytes(LegacySha256(password)));
            if (matches)
                subscriber.Password = hasher.HashPassword(subscriber, password);
            return matches;
        }

        var result = hasher.VerifyHashedPassword(subscriber, subscriber.Password, password);
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            subscriber.Password = hasher.HashPassword(subscriber, password);
        return result != PasswordVerificationResult.Failed;
    }

    private Task SignInAsync(Subscriber subscriber) =>
        HttpContext.SignInAsync(AuthExtensions.ForSubscriber(subscriber),
                                new AuthenticationProperties { IsPersistent = true });

    /// <summary>Generate a unique 11-digit VC (Viewing Card) number.</summary>
    private async Task<string> GenerateVcNumberAsync()
    {
        string vc;
        do
        {
            vc = "0" + Random.Shared.NextInt64(1_000_000_000, 9_999_999_999).ToString();
        }
        while (await db.Subscribers.AnyAsync(s => s.VcNumber == vc));
        return vc;
    }

    [HttpGet]
    [Authorize(Roles = AuthExtensions.AdminRole)]
    public async Task<IActionResult> GetAll() =>
        Ok(await db.Subscribers
            .AsNoTracking()
            .Select(s => new { s.Id, s.VcNumber, s.FullName, s.Email, s.Phone, s.City, s.State, s.RegisteredAt })
            .ToListAsync());

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FullName))
            return BadRequest("Full Name is required.");
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest("Email is required.");
        if (string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Password is required.");
        if (string.IsNullOrWhiteSpace(req.Phone))
            return BadRequest("Mobile number is required.");
        if (string.IsNullOrWhiteSpace(req.Address))
            return BadRequest("Address is required.");
        if (string.IsNullOrWhiteSpace(req.City))
            return BadRequest("City is required.");
        if (string.IsNullOrWhiteSpace(req.State))
            return BadRequest("State is required.");

        if (await db.Subscribers.AnyAsync(s => s.Email == req.Email))
            return Conflict("This email is already registered.");

        var subscriber = new Subscriber
        {
            VcNumber     = await GenerateVcNumberAsync(),
            FullName     = req.FullName,
            Email        = req.Email,
            Password     = "",
            Phone        = req.Phone,
            Address      = req.Address,
            City         = req.City,
            State        = req.State,
            StbNumber    = "STB-DTH-" + Random.Shared.Next(100000, 999999),
            RegisteredAt = DateTime.UtcNow
        };

        subscriber.Password = hasher.HashPassword(subscriber, req.Password);

        db.Subscribers.Add(subscriber);
        await db.SaveChangesAsync();

        // Signed in straight away so the KYC upload step that follows is authorised.
        await SignInAsync(subscriber);

        return Ok(new
        {
            message  = "Registration successful! Your VC number is your login ID.",
            id       = subscriber.Id,
            vcNumber = subscriber.VcNumber
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.VcNumber) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("VC number and password are required.");

        var subscriber = await db.Subscribers
            .FirstOrDefaultAsync(s => s.VcNumber == req.VcNumber.Trim());

        if (subscriber is null || !VerifyAndUpgrade(subscriber, req.Password))
            return Unauthorized("Invalid VC number or password.");

        await db.SaveChangesAsync(); // persists a re-hashed password, if any
        await SignInAsync(subscriber);

        return Ok(new
        {
            message   = "Login successful!",
            id        = subscriber.Id,
            vcNumber  = subscriber.VcNumber,
            fullName  = subscriber.FullName,
            email     = subscriber.Email,
            phone     = subscriber.Phone,
            city      = subscriber.City,
            state     = subscriber.State,
            stbNumber = subscriber.StbNumber
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Ok();
    }
}

public record RegisterRequest(string FullName, string Email, string Password,
                              string Phone, string Address, string City, string State);
public record LoginRequest(string VcNumber, string Password);
