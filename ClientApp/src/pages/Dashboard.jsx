import { useState, useEffect, useCallback } from "react";

const rupee = (n) => "₹" + Number(n).toLocaleString("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const dateFmt = (d) => new Date(d).toLocaleDateString("en-IN", { day: "2-digit", month: "short", year: "numeric" });

const BANNER = {
  Active:   { cls: "expiry-active",   icon: "🟢", title: (d) => `Your plan is active — ${d} days remaining`, sub: "Enjoy uninterrupted viewing." },
  Expiring: { cls: "expiry-expiring", icon: "🟠", title: (d) => `Plan expiring in ${d} day${d === 1 ? "" : "s"}`, sub: "Recharge now to avoid any interruption." },
  Expired:  { cls: "expiry-expired",  icon: "🔴", title: (d) => `Plan expired ${Math.abs(d)} day${Math.abs(d) === 1 ? "" : "s"} ago`, sub: "Recharge to resume your DishTV service." },
};

function Bill({ bill }) {
  return (
    <div className="bill">
      <div className="bill-row"><span>Base pack price</span><span className="rupee">{rupee(bill.basePrice)}</span></div>
      {bill.discount > 0 && (
        <div className="bill-row discount"><span>Discount ({bill.discountPct}% off)</span><span className="rupee">− {rupee(bill.discount)}</span></div>
      )}
      <div className="bill-row muted"><span>NCF (Network Capacity Fee)</span><span className="rupee">+ {rupee(bill.ncf)}</span></div>
      {bill.hdCharge > 0 && (
        <div className="bill-row muted"><span>HD add-on charge</span><span className="rupee">+ {rupee(bill.hdCharge)}</span></div>
      )}
      <div className="bill-row muted"><span>GST ({bill.gstRate}%)</span><span className="rupee">+ {rupee(bill.gst)}</span></div>
      <div className="bill-row total"><span>Total payable</span><span className="rupee">{rupee(bill.totalPayable)}</span></div>
    </div>
  );
}

function PlanCard({ plan, currentPlanId, onSubscribe, busy }) {
  const owned = plan.id === currentPlanId;
  const hasDiscount = plan.discountPercent > 0;
  return (
    <div className={`plan-card ${owned ? "current" : ""}`}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <span className="plan-tier">{plan.tier}</span>
        {plan.isHd && <span className="plan-tier" style={{ background: "#e6f0ff", color: "#2563eb" }}>HD</span>}
      </div>
      <div className="plan-name">{plan.name}</div>
      <div className="plan-price">
        {rupee(plan.bill.totalPayable)} <small>/ {plan.validityDays} days</small>
      </div>
      {hasDiscount && (
        <div><span className="plan-off">{plan.discountPercent}% OFF</span> <span className="plan-strike">base {rupee(plan.monthlyPrice)}</span></div>
      )}
      <div className="plan-meta">{plan.channelCount} channels · incl. NCF & GST</div>
      <div className="plan-meta" style={{ fontSize: "0.78rem" }}>{plan.description}</div>
      <button className={`plan-btn ${owned ? "owned" : ""}`} disabled={owned || busy}
        onClick={() => onSubscribe(plan)}>
        {owned ? "Current Plan" : busy ? "…" : "Choose Plan"}
      </button>
    </div>
  );
}

export default function Dashboard({ subscriber, onLogout }) {
  const [data, setData]   = useState(null);
  const [loading, setLoading] = useState(true);
  const [busyPlan, setBusyPlan] = useState(null);
  const [toast, setToast] = useState(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const r = await fetch(`/api/plan/subscriber/${subscriber.vcNumber}`);
      setData(await r.json());
    } catch {
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [subscriber.vcNumber]);

  useEffect(() => { load(); }, [load]);

  const subscribe = async (plan) => {
    setBusyPlan(plan.id);
    try {
      const r = await fetch("/api/plan/subscribe", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ vcNumber: subscriber.vcNumber, planId: plan.id }),
      });
      const res = await r.json();
      if (r.ok) { setToast(res.message); await load(); }
    } finally {
      setBusyPlan(null);
      setTimeout(() => setToast(null), 4000);
    }
  };

  if (loading) return <div className="form-card"><p style={{ color: "#888" }}>Loading your account…</p></div>;

  const current = data?.current;
  const banner  = current ? BANNER[current.status] : null;

  return (
    <div style={{ maxWidth: "820px", margin: "0 auto", padding: "0 0.5rem", width: "100%" }}>
      {toast && (
        <div style={{ position:"fixed", top:"20px", right:"20px", zIndex:9999, background:"#16a34a", color:"#fff",
          padding:"14px 22px", borderRadius:"10px", boxShadow:"0 4px 16px rgba(0,0,0,0.15)", fontWeight:600, animation:"slideIn 0.3s ease" }}>
          ✅ {toast}
        </div>
      )}

      {/* Header */}
      <div className="form-card" style={{ marginBottom: "1.2rem", background: "linear-gradient(135deg,#e2231a,#b3140d)", color: "#fff" }}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", flexWrap: "wrap", gap: "1rem" }}>
          <div>
            <div style={{ fontSize: "26px" }}>📺</div>
            <h2 style={{ margin: "4px 0 0", color: "#fff" }}>Hi, {subscriber.fullName}</h2>
            <p style={{ margin: "6px 0 0", opacity: 0.9, fontSize: "14px" }}>
              VC: <strong>{subscriber.vcNumber}</strong> · STB: {subscriber.stbNumber || "—"}
            </p>
          </div>
          <button className="btn-secondary" onClick={onLogout}
            style={{ background: "rgba(255,255,255,0.15)", color: "#fff", border: "1px solid rgba(255,255,255,0.5)", marginTop: 0 }}>
            Logout
          </button>
        </div>
      </div>

      {/* Expiry notification */}
      {banner && (
        <div className={`expiry-banner ${banner.cls}`}>
          <div>
            <div className="msg-title">{banner.icon} {banner.title(current.daysRemaining)}</div>
            <div className="msg-sub">{banner.sub} Valid till {dateFmt(current.expiryDate)}.</div>
          </div>
          {current.status !== "Active" && (
            <button className="recharge-btn" onClick={() => current.plan && subscribe(current.plan)}>Recharge Now</button>
          )}
        </div>
      )}

      {!current && (
        <div className="alert alert-error" style={{ marginBottom: "1.2rem" }}>
          No active plan on this VC number yet. Choose a plan below to get started.
        </div>
      )}

      {/* Current plan + bill */}
      {current && (
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1.2rem", marginBottom: "1.2rem" }}>
          <div className="form-card">
            <h3 style={{ marginTop: 0, marginBottom: "0.8rem" }}>Current Plan</h3>
            <div className="plan-name" style={{ fontSize: "1.3rem" }}>{current.plan.name}</div>
            <span className="plan-tier">{current.plan.tier}</span>{" "}
            {current.plan.isHd && <span className="plan-tier" style={{ background: "#e6f0ff", color: "#2563eb" }}>HD</span>}
            <div className="dashboard-info" style={{ marginTop: "1rem" }}>
              <div className="info-row"><span>Channels</span><strong>{current.plan.channelCount}</strong></div>
              <div className="info-row"><span>Started</span><strong>{dateFmt(current.startDate)}</strong></div>
              <div className="info-row"><span>Expires</span><strong>{dateFmt(current.expiryDate)}</strong></div>
              <div className="info-row"><span>Status</span><strong>{current.status}</strong></div>
            </div>
          </div>
          <div className="form-card">
            <h3 style={{ marginTop: 0, marginBottom: "0.8rem" }}>Bill Breakdown</h3>
            <Bill bill={current.plan.bill} />
          </div>
        </div>
      )}

      {/* Available plans */}
      <div className="form-card">
        <h3 style={{ marginTop: 0, marginBottom: "1rem" }}>Available Plans</h3>
        <div className="plan-grid">
          {data?.availablePlans?.map((p) => (
            <PlanCard key={p.id} plan={p} currentPlanId={current?.plan?.id}
              onSubscribe={subscribe} busy={busyPlan === p.id} />
          ))}
        </div>
      </div>
    </div>
  );
}
