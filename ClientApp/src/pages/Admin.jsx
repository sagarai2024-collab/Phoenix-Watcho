import { useState, useEffect, useCallback } from "react";

const ADMIN_PASSWORD = "admin123";
const rupee = (n) => "₹" + Number(n).toLocaleString("en-IN");

const STATUS_BADGE = {
  Approved: { bg: "#dcfce7", color: "#16a34a" },
  Rejected: { bg: "#fee2e2", color: "#dc2626" },
  Pending:  { bg: "#fef9c3", color: "#ca8a04" },
};

function Badge({ status }) {
  const s = STATUS_BADGE[status] ?? STATUS_BADGE.Pending;
  return (
    <span style={{ background: s.bg, color: s.color, padding: "3px 10px", borderRadius: "20px", fontSize: "12px", fontWeight: 600 }}>
      {status}
    </span>
  );
}

export default function Admin({ onBack }) {
  const [authed, setAuthed]   = useState(false);
  const [pwd, setPwd]         = useState("");
  const [pwdErr, setPwdErr]   = useState("");
  const [tab, setTab]         = useState("analytics");

  const [analytics, setAnalytics]     = useState(null);
  const [subscribers, setSubscribers] = useState([]);
  const [plans, setPlans]             = useState([]);
  const [docs, setDocs]               = useState([]);
  const [logs, setLogs]               = useState([]);
  const [kb, setKb]                   = useState([]);

  const [kbForm, setKbForm]       = useState({ id: 0, topic: "", question: "", answer: "", keywords: "" });
  const [kbEditing, setKbEditing] = useState(false);

  const load = useCallback(async (section) => {
    try {
      const map = {
        analytics:   ["/api/admin/analytics",   setAnalytics],
        subscribers: ["/api/admin/subscribers",  setSubscribers],
        plans:       ["/api/admin/plans",        setPlans],
        documents:   ["/api/admin/documents",    setDocs],
        chatlogs:    ["/api/admin/chatlogs",     setLogs],
        kb:          ["/api/admin/kb",           setKb],
      };
      const entry = map[section];
      if (!entry) return;
      const r = await fetch(entry[0]);
      entry[1](await r.json());
    } catch { /* ignore */ }
  }, []);

  useEffect(() => { if (authed) load(tab); }, [authed, tab, load]);

  const handleLogin = () => {
    if (pwd === ADMIN_PASSWORD) { setAuthed(true); setPwdErr(""); }
    else setPwdErr("Wrong password.");
  };

  const updateDocStatus = async (id, status) => {
    await fetch(`/api/admin/document/${id}/status`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ status }),
    });
    load("documents");
  };

  const saveKb = async () => {
    const method = kbEditing ? "PUT" : "POST";
    const url    = kbEditing ? `/api/admin/kb/${kbForm.id}` : "/api/admin/kb";
    await fetch(url, { method, headers: { "Content-Type": "application/json" }, body: JSON.stringify(kbForm) });
    setKbForm({ id: 0, topic: "", question: "", answer: "", keywords: "" });
    setKbEditing(false);
    load("kb");
  };

  const deleteKb = async (id) => {
    if (!window.confirm("Delete this entry?")) return;
    await fetch(`/api/admin/kb/${id}`, { method: "DELETE" });
    load("kb");
  };

  const th = { padding: "10px 12px", textAlign: "left", borderBottom: "1px solid #e5e7eb", fontWeight: 700, color: "#374151" };
  const td = { padding: "10px 12px" };

  // ── Password gate ─────────────────────────────────────────────────────────
  if (!authed) {
    return (
      <div className="form-card" style={{ maxWidth: "360px", margin: "0 auto", textAlign: "center" }}>
        <div style={{ fontSize: "36px", marginBottom: "0.5rem" }}>🔐</div>
        <h2>Admin Panel</h2>
        <p style={{ color: "#888", fontSize: "14px", marginBottom: "1.2rem" }}>Enter admin password to continue</p>
        <div className="field" style={{ textAlign: "left" }}>
          <label>Password</label>
          <input type="password" value={pwd} onChange={e => setPwd(e.target.value)}
            onKeyDown={e => e.key === "Enter" && handleLogin()} placeholder="Admin password" />
          {pwdErr && <span className="field-error">{pwdErr}</span>}
        </div>
        <button className="btn-primary" style={{ marginTop: "0.5rem" }} onClick={handleLogin}>Login</button>
        <p className="switch-link" style={{ marginTop: "1rem" }}>
          <button onClick={onBack}>← Back to Portal</button>
        </p>
        <p style={{ fontSize: "12px", color: "#a0aec0", marginTop: "0.5rem" }}>Demo password: <strong>admin123</strong></p>
      </div>
    );
  }

  const TABS = [
    { key: "analytics",   label: "📊 Analytics" },
    { key: "subscribers", label: "👥 Subscribers" },
    { key: "plans",       label: "📦 Plans" },
    { key: "documents",   label: "📄 KYC" },
    { key: "chatlogs",    label: "💬 Chat Logs" },
    { key: "kb",          label: "🧠 Knowledge Base" },
  ];

  return (
    <div style={{ maxWidth: "960px", margin: "0 auto", padding: "0 1rem", width: "100%" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "1rem" }}>
        <h2 style={{ margin: 0 }}>Admin Panel</h2>
        <button className="btn-secondary" style={{ marginTop: 0 }} onClick={onBack}>← Back to Portal</button>
      </div>

      <div style={{ display: "flex", gap: "6px", flexWrap: "wrap", marginBottom: "1.2rem" }}>
        {TABS.map(t => (
          <button key={t.key} onClick={() => setTab(t.key)}
            style={{ padding: "8px 16px", borderRadius: "8px", border: "none", cursor: "pointer", fontWeight: 600, fontSize: "13px",
              background: tab === t.key ? "#e2231a" : "#f1f5f9", color: tab === t.key ? "#fff" : "#374151" }}>
            {t.label}
          </button>
        ))}
      </div>

      {/* Analytics */}
      {tab === "analytics" && analytics && (
        <div>
          <div className="stat-grid">
            {[
              { icon: "💬", label: "Total Chats",       value: analytics.totalChats },
              { icon: "👥", label: "Total Subscribers", value: analytics.totalSubscribers },
              { icon: "🔴", label: "Expired Plans",     value: analytics.expiredCount },
            ].map(c => (
              <div key={c.label} className="stat-card">
                <div className="stat-icon">{c.icon}</div>
                <div className="stat-label">{c.label}</div>
                <div className="stat-value" style={{ fontSize: "22px" }}>{c.value}</div>
              </div>
            ))}
          </div>
          <div className="form-card">
            <h3 style={{ marginTop: 0 }}>Most Asked Topics</h3>
            {analytics.topicCounts.map(t => (
              <div key={t.topic} style={{ display: "flex", alignItems: "center", gap: "12px", marginBottom: "10px" }}>
                <div style={{ width: "90px", fontSize: "13px", fontWeight: 600, color: "#374151", textTransform: "capitalize" }}>{t.topic}</div>
                <div style={{ flex: 1, background: "#f1f5f9", borderRadius: "6px", overflow: "hidden" }}>
                  <div style={{ height: "20px", background: "#e2231a", width: `${Math.min(100, (t.count / (analytics.totalChats || 1)) * 100 * 3)}%`, minWidth: "4px", borderRadius: "6px" }} />
                </div>
                <div style={{ width: "30px", fontSize: "13px", color: "#888" }}>{t.count}</div>
              </div>
            ))}
            {analytics.topicCounts.length === 0 && <p style={{ color: "#888" }}>No chat data yet.</p>}
          </div>
        </div>
      )}

      {/* Subscribers */}
      {tab === "subscribers" && (
        <div className="form-card" style={{ overflowX: "auto" }}>
          <h3 style={{ marginTop: 0 }}>Subscribers ({subscribers.length})</h3>
          <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "13px" }}>
            <thead><tr style={{ background: "#f8fafc" }}>
              {["VC Number","Name","Current Plan","Expires","City","Pending KYC"].map(h => <th key={h} style={th}>{h}</th>)}
            </tr></thead>
            <tbody>
              {subscribers.map(s => (
                <tr key={s.id} style={{ borderBottom: "1px solid #f1f5f9" }}>
                  <td style={{ ...td, fontWeight: 600, fontVariantNumeric: "tabular-nums" }}>{s.vcNumber}</td>
                  <td style={td}>{s.fullName}</td>
                  <td style={td}>{s.currentPlan || "—"}</td>
                  <td style={{ ...td, color: "#6b7280" }}>{s.expiryDate ? new Date(s.expiryDate).toLocaleDateString("en-IN") : "—"}</td>
                  <td style={td}>{s.city}</td>
                  <td style={td}>{s.pendingDocs}</td>
                </tr>
              ))}
              {subscribers.length === 0 && <tr><td colSpan={6} style={{ padding: "20px", textAlign: "center", color: "#888" }}>No subscribers yet.</td></tr>}
            </tbody>
          </table>
        </div>
      )}

      {/* Plans */}
      {tab === "plans" && (
        <div className="form-card" style={{ overflowX: "auto" }}>
          <h3 style={{ marginTop: 0 }}>Plans ({plans.length})</h3>
          <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "13px" }}>
            <thead><tr style={{ background: "#f8fafc" }}>
              {["Code","Name","Tier","Base Price","Discount","Channels","HD","Validity"].map(h => <th key={h} style={th}>{h}</th>)}
            </tr></thead>
            <tbody>
              {plans.map(p => (
                <tr key={p.id} style={{ borderBottom: "1px solid #f1f5f9" }}>
                  <td style={{ ...td, fontWeight: 600 }}>{p.code}</td>
                  <td style={td}>{p.name}</td>
                  <td style={td}>{p.tier}</td>
                  <td style={td}>{rupee(p.monthlyPrice)}</td>
                  <td style={td}>{p.discountPercent}%</td>
                  <td style={td}>{p.channelCount}</td>
                  <td style={td}>{p.isHd ? "✅" : "—"}</td>
                  <td style={td}>{p.validityDays}d</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* KYC Documents */}
      {tab === "documents" && (
        <div className="form-card" style={{ overflowX: "auto" }}>
          <h3 style={{ marginTop: 0 }}>KYC Verification</h3>
          <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "13px" }}>
            <thead><tr style={{ background: "#f8fafc" }}>
              {["Subscriber","VC Number","Type","File","Status","Actions"].map(h => <th key={h} style={th}>{h}</th>)}
            </tr></thead>
            <tbody>
              {docs.map(d => (
                <tr key={d.id} style={{ borderBottom: "1px solid #f1f5f9" }}>
                  <td style={{ ...td, fontWeight: 600 }}>{d.subscriberName}</td>
                  <td style={{ ...td, fontVariantNumeric: "tabular-nums" }}>{d.vcNumber}</td>
                  <td style={td}>{d.documentType}</td>
                  <td style={{ ...td, color: "#6b7280", fontSize: "12px" }}>{d.fileName}</td>
                  <td style={td}><Badge status={d.status} /></td>
                  <td style={td}>
                    <div style={{ display: "flex", gap: "6px" }}>
                      <button onClick={() => updateDocStatus(d.id, "Approved")}
                        style={{ background: "#dcfce7", color: "#16a34a", border: "none", borderRadius: "6px", padding: "4px 10px", fontSize: "12px", fontWeight: 600, cursor: "pointer" }}>✅ Approve</button>
                      <button onClick={() => updateDocStatus(d.id, "Rejected")}
                        style={{ background: "#fee2e2", color: "#dc2626", border: "none", borderRadius: "6px", padding: "4px 10px", fontSize: "12px", fontWeight: 600, cursor: "pointer" }}>❌ Reject</button>
                    </div>
                  </td>
                </tr>
              ))}
              {docs.length === 0 && <tr><td colSpan={6} style={{ padding: "20px", textAlign: "center", color: "#888" }}>No KYC documents uploaded yet.</td></tr>}
            </tbody>
          </table>
        </div>
      )}

      {/* Chat Logs */}
      {tab === "chatlogs" && (
        <div className="form-card" style={{ overflowX: "auto" }}>
          <h3 style={{ marginTop: 0 }}>Recent Chat Logs ({logs.length})</h3>
          <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "13px" }}>
            <thead><tr style={{ background: "#f8fafc" }}>
              {["Time","Topic","User Message","Bot Reply"].map(h => <th key={h} style={th}>{h}</th>)}
            </tr></thead>
            <tbody>
              {logs.map(l => (
                <tr key={l.id} style={{ borderBottom: "1px solid #f1f5f9" }}>
                  <td style={{ ...td, color: "#6b7280", whiteSpace: "nowrap" }}>{new Date(l.createdAt).toLocaleString()}</td>
                  <td style={td}><Badge status={l.topic.charAt(0).toUpperCase() + l.topic.slice(1)} /></td>
                  <td style={{ ...td, maxWidth: "220px" }}>{l.userMessage}</td>
                  <td style={{ ...td, color: "#6b7280", maxWidth: "280px", whiteSpace: "pre-wrap", fontSize: "12px" }}>{l.botReply.slice(0, 120)}{l.botReply.length > 120 ? "…" : ""}</td>
                </tr>
              ))}
              {logs.length === 0 && <tr><td colSpan={4} style={{ padding: "20px", textAlign: "center", color: "#888" }}>No chat logs yet.</td></tr>}
            </tbody>
          </table>
        </div>
      )}

      {/* Knowledge Base */}
      {tab === "kb" && (
        <div>
          <div className="form-card" style={{ marginBottom: "1.2rem" }}>
            <h3 style={{ marginTop: 0 }}>{kbEditing ? "Edit Entry" : "Add New KB Entry"}</h3>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "0.8rem" }}>
              {[["topic","Topic (plan/recharge/expiry…)"],["question","Question"]].map(([k,p]) => (
                <div className="field" key={k}>
                  <label style={{ textTransform: "capitalize" }}>{p}</label>
                  <input value={kbForm[k]} onChange={e => setKbForm(f => ({...f, [k]: e.target.value}))} placeholder={p} />
                </div>
              ))}
            </div>
            <div className="field">
              <label>Answer</label>
              <textarea value={kbForm.answer} onChange={e => setKbForm(f => ({...f, answer: e.target.value}))}
                placeholder="Full answer shown to the subscriber" rows={3}
                style={{ width: "100%", padding: "8px", borderRadius: "8px", border: "1px solid #d1d5db", resize: "vertical", fontSize: "14px" }} />
            </div>
            <div className="field">
              <label>Keywords (space separated)</label>
              <input value={kbForm.keywords} onChange={e => setKbForm(f => ({...f, keywords: e.target.value}))} placeholder="recharge pay renew balance" />
            </div>
            <div style={{ display: "flex", gap: "8px", marginTop: "0.5rem" }}>
              <button className="btn-primary" style={{ width: "auto", padding: "0.6rem 1.5rem" }} onClick={saveKb}>{kbEditing ? "Update Entry" : "Add Entry"}</button>
              {kbEditing && (
                <button className="btn-secondary" style={{ marginTop: 0 }} onClick={() => { setKbEditing(false); setKbForm({ id:0,topic:"",question:"",answer:"",keywords:"" }); }}>Cancel</button>
              )}
            </div>
          </div>

          <div className="form-card" style={{ overflowX: "auto" }}>
            <h3 style={{ marginTop: 0 }}>Knowledge Base Entries ({kb.length})</h3>
            <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "13px" }}>
              <thead><tr style={{ background: "#f8fafc" }}>
                {["Topic","Question","Answer (preview)","Actions"].map(h => <th key={h} style={th}>{h}</th>)}
              </tr></thead>
              <tbody>
                {kb.map(e => (
                  <tr key={e.id} style={{ borderBottom: "1px solid #f1f5f9" }}>
                    <td style={td}><Badge status={e.topic.charAt(0).toUpperCase() + e.topic.slice(1)} /></td>
                    <td style={{ ...td, fontWeight: 600 }}>{e.question}</td>
                    <td style={{ ...td, color: "#6b7280", maxWidth: "260px" }}>{e.answer.slice(0,80)}…</td>
                    <td style={td}>
                      <div style={{ display: "flex", gap: "6px" }}>
                        <button onClick={() => { setKbForm({ id:e.id, topic:e.topic, question:e.question, answer:e.answer, keywords:e.keywords }); setKbEditing(true); }}
                          style={{ background: "#dbeafe", color: "#1d4ed8", border: "none", borderRadius: "6px", padding: "4px 10px", fontSize: "12px", fontWeight: 600, cursor: "pointer" }}>✏️ Edit</button>
                        <button onClick={() => deleteKb(e.id)}
                          style={{ background: "#fee2e2", color: "#dc2626", border: "none", borderRadius: "6px", padding: "4px 10px", fontSize: "12px", fontWeight: 600, cursor: "pointer" }}>🗑️ Delete</button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
