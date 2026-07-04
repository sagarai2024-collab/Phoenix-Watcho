using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentLearningPortal.Data;
using StudentLearningPortal.Models;

namespace StudentLearningPortal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentController(AppDbContext db) : ControllerBase
{
    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await db.Students
            .Select(s => new { s.Id, s.FullName, s.Email, s.Course, s.Phone, s.Gender, s.RegisteredAt })
            .ToListAsync());

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] Student student)
    {
        if (string.IsNullOrWhiteSpace(student.FullName))
            return BadRequest("Full Name is required.");
        if (string.IsNullOrWhiteSpace(student.Email))
            return BadRequest("Email is required.");
        if (string.IsNullOrWhiteSpace(student.Password))
            return BadRequest("Password is required.");
        if (string.IsNullOrWhiteSpace(student.Course))
            return BadRequest("Course is required.");
        if (string.IsNullOrWhiteSpace(student.Phone))
            return BadRequest("Phone is required.");
        if (string.IsNullOrWhiteSpace(student.Gender))
            return BadRequest("Gender is required.");
        if (student.DateOfBirth == null)
            return BadRequest("Date of Birth is required.");
        if (string.IsNullOrWhiteSpace(student.Address))
            return BadRequest("Address is required.");

        if (await db.Students.AnyAsync(s => s.Email == student.Email))
            return Conflict("Email already registered.");

        student.Password    = HashPassword(student.Password);
        student.RegisteredAt = DateTime.UtcNow;

        db.Students.Add(student);
        await db.SaveChangesAsync();

        return Ok(new { message = "Registration successful!", student.Id });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Email and Password are required.");

        var hashed  = HashPassword(req.Password);
        var student = await db.Students
            .FirstOrDefaultAsync(s => s.Email == req.Email && s.Password == hashed);

        if (student is null)
            return Unauthorized("Invalid email or password.");

        return Ok(new {
            message      = "Login successful!",
            id           = student.Id,
            fullName     = student.FullName,
            email        = student.Email,
            course       = student.Course,
            registeredAt = student.RegisteredAt
        });
    }
}

public record LoginRequest(string Email, string Password);
