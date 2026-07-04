using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentLearningPortal.Data;
using StudentLearningPortal.Models;

namespace StudentLearningPortal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    private static readonly string[] AllowedTypes = [".jpg", ".jpeg", ".png", ".pdf"];
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] int studentId,
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

        var studentExists = await db.Students.AnyAsync(s => s.Id == studentId);
        if (!studentExists)
            return NotFound("Student not found.");

        // Save file to wwwroot/uploads/{studentId}/
        var uploadFolder = Path.Combine(env.WebRootPath ?? "wwwroot", "uploads", studentId.ToString());
        Directory.CreateDirectory(uploadFolder);

        var uniqueName = $"{documentType}_{Guid.NewGuid()}{ext}";
        var filePath   = Path.Combine(uploadFolder, uniqueName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        // Save record to DB
        var doc = new StudentDocument
        {
            StudentId    = studentId,
            DocumentType = documentType,
            FileName     = file.FileName,
            FilePath     = $"/uploads/{studentId}/{uniqueName}",
            UploadedAt   = DateTime.UtcNow
        };

        db.StudentDocuments.Add(doc);
        await db.SaveChangesAsync();

        return Ok(new { message = "Document uploaded successfully!", filePath = doc.FilePath });
    }

    [HttpGet("{studentId}")]
    public async Task<IActionResult> GetDocuments(int studentId)
    {
        var docs = await db.StudentDocuments
            .Where(d => d.StudentId == studentId)
            .Select(d => new { d.Id, d.DocumentType, d.FileName, d.FilePath, d.UploadedAt })
            .ToListAsync();

        return Ok(docs);
    }
}
