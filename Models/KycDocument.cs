namespace PhoenixWatcho.Models;

/// <summary>
/// KYC document uploaded by a subscriber for identity/address verification.
/// </summary>
public class KycDocument
{
    public int Id { get; set; }
    public int SubscriberId { get; set; }
    public string DocumentType { get; set; } = "";  // Aadhaar, AddressProof, Photo
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

    public Subscriber? Subscriber { get; set; }
}
