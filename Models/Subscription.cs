namespace PhoenixWatcho.Models;

/// <summary>
/// Links a <see cref="Subscriber"/> to a <see cref="Plan"/> for a validity window.
/// The live status (Active / Expiring / Expired) is derived from <see cref="ExpiryDate"/>.
/// </summary>
public class Subscription
{
    public int Id { get; set; }
    public int SubscriberId { get; set; }
    public int PlanId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime ExpiryDate { get; set; }

    public Subscriber? Subscriber { get; set; }
    public Plan? Plan { get; set; }

    // ── Derived (not mapped) helpers ──────────────────────────────────────────
    public int DaysRemaining => (int)Math.Ceiling((ExpiryDate - DateTime.UtcNow).TotalDays);

    /// <summary>Active, Expiring (≤5 days left) or Expired.</summary>
    public string Status =>
        DaysRemaining < 0 ? "Expired" :
        DaysRemaining <= 5 ? "Expiring" :
        "Active";
}
