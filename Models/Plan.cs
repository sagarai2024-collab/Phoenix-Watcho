namespace PhoenixWatcho.Models;

/// <summary>
/// A DishTV / Watcho subscription pack (e.g. "Sports HD 799").
/// Pricing is the base pack price; NCF, HD add-on and GST are added at billing time
/// (see <see cref="PhoenixWatcho.Services.BillingService"/>).
/// </summary>
public class Plan
{
    public int Id { get; set; }
    public string Code { get; set; } = "";        // e.g. SPORTSHD799
    public string Name { get; set; } = "";        // e.g. Sports HD Pack
    public string Tier { get; set; } = "";        // Base, Value, Regional, Sports, Premium
    public decimal MonthlyPrice { get; set; }     // base pack price (before NCF / HD / GST)
    public int ValidityDays { get; set; } = 30;
    public int ChannelCount { get; set; }
    public bool IsHd { get; set; }                // HD pack → HD add-on charge applies
    public decimal DiscountPercent { get; set; }  // promotional discount on the base price
    public string Description { get; set; } = "";
}
