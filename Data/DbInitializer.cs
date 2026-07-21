using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PhoenixWatcho.Models;

namespace PhoenixWatcho.Data;

/// <summary>
/// Runtime seeding. Reference data (plans, KB) and demo subscribers are inserted only
/// when their tables are empty. Demo subscription dates are computed relative to "today"
/// so the Active / Expiring / Expired states are always visible on the dashboard.
/// </summary>
public static class DbInitializer
{
    private static string Hash(string password) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password))).ToLower();

    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedPlansAsync(db);
        await SeedKnowledgeBaseAsync(db);
        await SeedDemoSubscribersAsync(db);
    }

    private static async Task SeedPlansAsync(AppDbContext db)
    {
        if (await db.Plans.AnyAsync()) return;

        db.Plans.AddRange(
            new Plan { Code = "BASE199",     Name = "My Base Pack",        Tier = "Base",     MonthlyPrice = 199,  ValidityDays = 30, ChannelCount = 100, IsHd = false, DiscountPercent = 0,  Description = "100 essential SD channels — news, entertainment & infotainment." },
            new Plan { Code = "HINDIVALUE299", Name = "Hindi Value Pack",  Tier = "Value",    MonthlyPrice = 299,  ValidityDays = 30, ChannelCount = 160, IsHd = false, DiscountPercent = 10, Description = "160 popular Hindi SD channels with 10% launch discount." },
            new Plan { Code = "REGIONAL349", Name = "Regional Super Pack",  Tier = "Regional", MonthlyPrice = 349,  ValidityDays = 30, ChannelCount = 180, IsHd = false, DiscountPercent = 5,  Description = "Regional languages + national favourites, 180 channels." },
            new Plan { Code = "SPORTSHD799", Name = "Sports HD Pack",       Tier = "Sports",   MonthlyPrice = 799,  ValidityDays = 30, ChannelCount = 250, IsHd = true,  DiscountPercent = 15, Description = "All sports + HD entertainment, 250 channels. Best value for fans." },
            new Plan { Code = "PREMIUMHD1199", Name = "Premium HD Pack",    Tier = "Premium",  MonthlyPrice = 1199, ValidityDays = 30, ChannelCount = 350, IsHd = true,  DiscountPercent = 20, Description = "The complete HD experience — 350 channels, every genre." }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedKnowledgeBaseAsync(AppDbContext db)
    {
        if (await db.KnowledgeBase.AnyAsync()) return;

        db.KnowledgeBase.AddRange(
            new KnowledgeBase { Topic = "recharge", Question = "How do I recharge my DishTV connection?", Answer = "You can recharge instantly via the Watcho app, dishtv.in, UPI, or your nearest retailer. Just enter your VC number and the amount for your pack.", Keywords = "recharge pay renew balance top up payment money" },
            new KnowledgeBase { Topic = "plan", Question = "What plans are available?", Answer = "DishTV offers Base (₹199), Hindi Value (₹299), Regional (₹349), Sports HD (₹799) and Premium HD (₹1199) packs. Log in with your VC number to see prices and offers for your account.", Keywords = "plan pack price subscription options list available packs" },
            new KnowledgeBase { Topic = "expiry", Question = "When does my plan expire?", Answer = "Your plan validity is shown on your dashboard with the exact expiry date and days remaining. We notify you before it expires so your service is not interrupted.", Keywords = "expire expiry validity end date renew due when finish" },
            new KnowledgeBase { Topic = "vc", Question = "Where do I find my VC number?", Answer = "Your VC (Viewing Card) number is the 11-digit number on the smart card inside your set-top box. It is also shown on your on-screen menu under System Info.", Keywords = "vc viewing card number find where smartcard stb id account" },
            new KnowledgeBase { Topic = "ncf", Question = "What is NCF in my bill?", Answer = "NCF (Network Capacity Fee) is a TRAI-mandated fee of ₹130 + GST for up to 200 channels. It is a standard charge on every DTH bill, separate from your pack price.", Keywords = "ncf network capacity fee charge extra trai bill why" },
            new KnowledgeBase { Topic = "hd", Question = "Is there an extra charge for HD channels?", Answer = "HD packs carry a small HD add-on capacity charge in addition to the pack price. The exact amount is shown itemized on your dashboard bill.", Keywords = "hd high definition charge extra cost add-on quality" },
            new KnowledgeBase { Topic = "discount", Question = "Do you have any discounts or offers?", Answer = "Yes — several packs carry a promotional discount (for example Sports HD has 15% off and Premium HD 20% off). The discount is applied automatically and shown on your bill breakdown.", Keywords = "discount offer deal promo cashback save cheaper reduce" },
            new KnowledgeBase { Topic = "kyc", Question = "What KYC documents are required?", Answer = "For a new connection you need: Aadhaar card, an address proof, and a passport-size photo. You can upload them during registration; our team verifies them within 24 hours.", Keywords = "kyc document upload aadhaar address proof photo verification id" },
            new KnowledgeBase { Topic = "support", Question = "How do I contact DishTV support?", Answer = "Call our helpline on 1800-200-8080 (toll-free), use the Watcho app chat, or ask me here. Support is available 24x7.", Keywords = "contact support helpline call number customer care help reach" },
            new KnowledgeBase { Topic = "upgrade", Question = "How do I upgrade or change my plan?", Answer = "Open your dashboard, pick a new pack from the Available Plans grid, and confirm. The change applies immediately and the new price reflects on your next bill.", Keywords = "upgrade change switch plan pack modify downgrade different" }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedDemoSubscribersAsync(AppDbContext db)
    {
        if (await db.Subscribers.AnyAsync()) return;

        var now = DateTime.UtcNow;
        var sportsHd = await db.Plans.FirstAsync(p => p.Code == "SPORTSHD799");
        var hindi    = await db.Plans.FirstAsync(p => p.Code == "HINDIVALUE299");
        var premium  = await db.Plans.FirstAsync(p => p.Code == "PREMIUMHD1199");

        // 1) Active subscriber — plan valid for ~25 more days
        var active = new Subscriber { VcNumber = "01234567890", FullName = "Rahul Sharma", Email = "rahul.sharma@example.com", Password = Hash("Pass123"), Phone = "9876543210", StbNumber = "STB-DTH-100045", Address = "12 MG Road", City = "Pune", State = "Maharashtra", RegisteredAt = now.AddMonths(-8) };

        // 2) Expiring subscriber — only 3 days left (amber warning)
        var expiring = new Subscriber { VcNumber = "01234500001", FullName = "Priya Nair", Email = "priya.nair@example.com", Password = Hash("Pass123"), Phone = "9812345678", StbNumber = "STB-DTH-100091", Address = "44 Residency Rd", City = "Bengaluru", State = "Karnataka", RegisteredAt = now.AddMonths(-14) };

        // 3) Expired subscriber — lapsed 6 days ago (red recharge banner)
        var expired = new Subscriber { VcNumber = "01234500002", FullName = "Amit Verma", Email = "amit.verma@example.com", Password = Hash("Pass123"), Phone = "9800011222", StbNumber = "STB-DTH-100120", Address = "9 Park Street", City = "Kolkata", State = "West Bengal", RegisteredAt = now.AddMonths(-20) };

        db.Subscribers.AddRange(active, expiring, expired);
        await db.SaveChangesAsync();

        db.Subscriptions.AddRange(
            new Subscription { SubscriberId = active.Id,   PlanId = sportsHd.Id, StartDate = now.AddDays(-5),  ExpiryDate = now.AddDays(25) },
            new Subscription { SubscriberId = expiring.Id, PlanId = hindi.Id,    StartDate = now.AddDays(-27), ExpiryDate = now.AddDays(3)  },
            new Subscription { SubscriberId = expired.Id,  PlanId = premium.Id,  StartDate = now.AddDays(-36), ExpiryDate = now.AddDays(-6) }
        );
        await db.SaveChangesAsync();
    }
}
