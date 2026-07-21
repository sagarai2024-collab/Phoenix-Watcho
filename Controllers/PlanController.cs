using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoenixWatcho.Data;
using PhoenixWatcho.Models;
using PhoenixWatcho.Services;

namespace PhoenixWatcho.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlanController(AppDbContext db, BillingService billing) : ControllerBase
{
    /// <summary>All available packs, each with its itemized bill.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await db.Plans.AsNoTracking().OrderBy(p => p.MonthlyPrice).ToListAsync();
        return Ok(plans.Select(ToPlanDto));
    }

    /// <summary>
    /// The subscriber's current plan with live expiry status + billing,
    /// plus the full catalogue of available plans to upgrade to.
    /// This is what the consumer dashboard reads.
    /// </summary>
    [HttpGet("subscriber/{vcNumber}")]
    public async Task<IActionResult> GetForSubscriber(string vcNumber)
    {
        var subscriber = await db.Subscribers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.VcNumber == vcNumber);

        if (subscriber is null)
            return NotFound("No subscriber found for this VC number.");

        // Latest subscription = the current one
        var current = await db.Subscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .Where(s => s.SubscriberId == subscriber.Id)
            .OrderByDescending(s => s.ExpiryDate)
            .FirstOrDefaultAsync();

        object? currentDto = null;
        if (current?.Plan is not null)
        {
            var bill = billing.ComputeBill(current.Plan);
            currentDto = new
            {
                subscriptionId = current.Id,
                plan           = ToPlanDto(current.Plan),
                startDate      = current.StartDate,
                expiryDate     = current.ExpiryDate,
                daysRemaining  = current.DaysRemaining,
                status         = current.Status,   // Active / Expiring / Expired
                bill
            };
        }

        var available = await db.Plans.AsNoTracking().OrderBy(p => p.MonthlyPrice).ToListAsync();

        return Ok(new
        {
            subscriber = new { subscriber.VcNumber, subscriber.FullName, subscriber.StbNumber, subscriber.City, subscriber.State },
            current = currentDto,
            availablePlans = available.Select(ToPlanDto)
        });
    }

    /// <summary>Subscribe / upgrade a VC to a plan. Creates a fresh validity window.</summary>
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest req)
    {
        var subscriber = await db.Subscribers.FirstOrDefaultAsync(s => s.VcNumber == req.VcNumber);
        if (subscriber is null) return NotFound("Subscriber not found.");

        var plan = await db.Plans.FindAsync(req.PlanId);
        if (plan is null) return NotFound("Plan not found.");

        var now = DateTime.UtcNow;
        var subscription = new Subscription
        {
            SubscriberId = subscriber.Id,
            PlanId       = plan.Id,
            StartDate    = now,
            ExpiryDate   = now.AddDays(plan.ValidityDays)
        };
        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();

        var bill = billing.ComputeBill(plan);
        return Ok(new
        {
            message    = $"'{plan.Name}' activated. Valid until {subscription.ExpiryDate:dd MMM yyyy}.",
            expiryDate = subscription.ExpiryDate,
            bill
        });
    }

    private object ToPlanDto(Plan p)
    {
        var bill = billing.ComputeBill(p);
        return new
        {
            p.Id, p.Code, p.Name, p.Tier, p.MonthlyPrice, p.ValidityDays,
            p.ChannelCount, p.IsHd, p.DiscountPercent, p.Description,
            bill
        };
    }
}

public record SubscribeRequest(string VcNumber, int PlanId);
