using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoenixWatcho.Data;
using PhoenixWatcho.Models;

namespace PhoenixWatcho.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    private static readonly string[] AllowedTypes = [".jpg", ".jpeg", ".png", ".pdf"];
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] int subscriberId,
                                            [FromForm] string documentType,
                                            IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (file.Length > MaxFileSize)
            return BadRequest("File size must be under 5 MB.");

        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!AllowedTypes.Contains(ext))
            return BadRequest("Only JPG, PNG and PDF files are allowed.");

        var subscriberExists = await db.Subscribers.AnyAsync(s => s.Id == subscriberId);
        if (!subscriberExists)
            return NotFound("Subscriber not found.");

        var uploadFolder = Path.Combine(env.WebRootPath ?? "wwwroot", "uploads", subscriberId.ToString());
        Directory.CreateDirectory(uploadFolder);

        var uniqueName = $"{documentType}_{Guid.NewGuid()}{ext}";
        var filePath   = Path.Combine(uploadFolder, uniqueName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var doc = new KycDocument
        {
            SubscriberId = subscriberId,
            DocumentType = documentType,
            FileName     = file.FileName,
            FilePath     = $"/uploads/{subscriberId}/{uniqueName}",
            UploadedAt   = DateTime.UtcNow
        };

        db.KycDocuments.Add(doc);
        await db.SaveChangesAsync();

        return Ok(new { message = "Document uploaded successfully!", filePath = doc.FilePath });
    }

    [HttpGet("{subscriberId}")]
    public async Task<IActionResult> GetDocuments(int subscriberId)
    {
        var docs = await db.KycDocuments
            .Where(d => d.SubscriberId == subscriberId)
            .Select(d => new { d.Id, d.DocumentType, d.FileName, d.FilePath, d.UploadedAt, d.Status })
            .ToListAsync();

        return Ok(docs);
    }
}
