namespace PhoenixWatcho.Models;

/// <summary>
/// A DishTV / Watcho consumer. Identified by their VC (Viewing Card) number —
/// the number printed on the smart card inside the set-top box, and the login identity.
/// </summary>
public class Subscriber
{
    public int Id { get; set; }
    public string VcNumber { get; set; } = "";   // Viewing Card number — unique login id (e.g. 01234567890)
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";    // SHA256 hashed
    public string? Phone { get; set; }            // RMN — Registered Mobile Number
    public string? StbNumber { get; set; }        // Set-Top Box serial number
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public List<Subscription> Subscriptions { get; set; } = new();
}
