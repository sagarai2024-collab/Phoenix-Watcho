namespace PhoenixWatcho.Models;

public class KnowledgeBase
{
    public int Id { get; set; }
    public string Topic { get; set; } = "";      // plan, recharge, expiry, hd, ncf, etc.
    public string Question { get; set; } = "";   // "How do I recharge my DishTV?"
    public string Answer { get; set; } = "";     // "You can recharge via the Watcho app..."
    public string Keywords { get; set; } = "";   // "recharge pay renew balance top up"
}
