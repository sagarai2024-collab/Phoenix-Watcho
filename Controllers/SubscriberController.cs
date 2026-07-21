using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoenixWatcho.Data;
using PhoenixWatcho.Models;

namespace PhoenixWatcho.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriberController(AppDbContext db) : ControllerBase
{
    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }

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
            Password     = HashPassword(req.Password),
            Phone        = req.Phone,
            Address      = req.Address,
            City         = req.City,
            State        = req.State,
            StbNumber    = "STB-DTH-" + Random.Shared.Next(100000, 999999),
            RegisteredAt = DateTime.UtcNow
        };

        db.Subscribers.Add(subscriber);
        await db.SaveChangesAsync();

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

        var hashed = HashPassword(req.Password);
        var subscriber = await db.Subscribers
            .FirstOrDefaultAsync(s => s.VcNumber == req.VcNumber.Trim() && s.Password == hashed);

        if (subscriber is null)
            return Unauthorized("Invalid VC number or password.");

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
}

public record RegisterRequest(string FullName, string Email, string Password,
                              string Phone, string Address, string City, string State);
public record LoginRequest(string VcNumber, string Password);
