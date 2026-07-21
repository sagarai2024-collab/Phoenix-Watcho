using PhoenixWatcho.Models;

namespace PhoenixWatcho.Services;

/// <summary>
/// Computes the itemized DishTV bill for a plan, the way a real DTH invoice reads:
///   base pack price  −  promotional discount  +  NCF  +  HD add-on  +  18% GST  =  total payable.
/// NCF (Network Capacity Fee) is the TRAI-mandated fee every DTH subscriber pays.
/// </summary>
public class BillingService
{
    public const decimal NcfBase   = 130m;  // NCF for up to 200 SD channels (before tax)
    public const decimal HdAddOn   = 50m;   // extra capacity charge for HD packs
    public const decimal GstRate   = 0.18m; // 18% GST

    public BillBreakdown ComputeBill(Plan plan)
    {
        var basePrice = plan.MonthlyPrice;
        var discount  = Math.Round(basePrice * plan.DiscountPercent / 100m, 2);
        var hdCharge  = plan.IsHd ? HdAddOn : 0m;

        var taxable   = basePrice - discount + NcfBase + hdCharge;
        var gst       = Math.Round(taxable * GstRate, 2);
        var total     = Math.Round(taxable + gst, 2);

        return new BillBreakdown(
            BasePrice:    basePrice,
            DiscountPct:  plan.DiscountPercent,
            Discount:     discount,
            Ncf:          NcfBase,
            HdCharge:     hdCharge,
            Taxable:      Math.Round(taxable, 2),
            GstRate:      GstRate * 100,
            Gst:          gst,
            TotalPayable: total
        );
    }
}

/// <summary>Itemized bill lines — all amounts in ₹.</summary>
public record BillBreakdown(
    decimal BasePrice,
    decimal DiscountPct,
    decimal Discount,
    decimal Ncf,
    decimal HdCharge,
    decimal Taxable,
    decimal GstRate,
    decimal Gst,
    decimal TotalPayable);
