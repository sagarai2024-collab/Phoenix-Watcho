namespace StudentLearningPortal.Models;

public class StudentDocument
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string DocumentType { get; set; } = "";  // Marksheet10, Marksheet12, Photo, Aadhaar
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

    public Student? Student { get; set; }
}
