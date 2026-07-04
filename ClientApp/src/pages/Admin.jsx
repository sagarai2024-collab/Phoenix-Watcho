import { useState, useEffect, useCallback } from "react";

const ADMIN_PASSWORD = "admin123";

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

  const [analytics, setAnalytics] = useState(null);
  const [students, setStudents]   = useState([]);
  const [docs, setDocs]           = useState([]);
  const [logs, setLogs]           = useState([]);
  const [kb, setKb]               = useState([]);

  const [kbForm, setKbForm]       = useState({ id: 0, topic: "", question: "", answer: "", keywords: "" });
  const [kbEditing, setKbEditing] = useState(false);

  const load = useCallback(async (section) => {
    try {
      if (section === "analytics") {
        const r = await fetch("/api/admin/analytics");
        setAnalytics(await r.json());
      } else if (section === "students") {
        const r = await fetch("/api/admin/students");
        setStudents(await r.json());
      } else if (section === "documents") {
        const r = await fetch("/api/admin/documents");
        setDocs(await r.json());
      } else if (section === "chatlogs") {
        const r = await fetch("/api/admin/chatlogs");
        setLogs(await r.json());
      } else if (section === "kb") {
        const r = await fetch("/api/admin/kb");
        setKb(await r.json());
      }
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
      </div>
    );
  }

  const TABS = [
    { key: "analytics", label: "📊 Analytics" },
    { key: "students",  label: "👥 Students"  },
    { key: "documents", label: "📄 Documents" },
    { key: "chatlogs",  label: "💬 Chat Logs" },
    { key: "kb",        label: "🧠 Knowledge Base" },
  ];

  return (
    <div style={{ maxWidth: "900px", margin: "0 auto", padding: "0 1rem" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "1rem" }}>
        <h2 style={{ margin: 0 }}>Admin Panel</h2>
        <button className="btn-secondary" onClick={onBack}>← Back to Portal</button>
      </div>

      {/* ── Tabs ── */}
      <div style={{ display: "flex", gap: "6px", flexWrap: "wrap", marginBottom: "1.2rem" }}>
        {TABS.map(t => (
          <button key={t.key} onClick={() => setTab(t.key)}
            style={{ padding: "8px 16px", borderRadius: "8px", border: "none", cursor: "pointer", fontWeight: 600, fontSize: "13px",
              background: tab === t.key ? "#2563eb" : "#f1f5f9", color: tab === t.key ? "#fff" : "#374151" }}>
            {t.label}
          </button>
        ))}
      </div>

      {/* ── Analytics ── */}
      {tab === "analytics" && analytics && (
        <div>
          <div style={{ display: "grid", gridTemplateColumns: "repeat(3,1fr)", gap: "1rem", marginBottom: "1.2rem" }}>
            {[
              { icon: "💬", label: "Total Chats",    value: analytics.totalChats },
              { icon: "👥", label: "Total Students", value: analytics.totalStudents },
              { icon: "⏳", label: "Pending Docs",   value: analytics.pendingDocs },
            ].map(c => (
              <div key={c.label} className="form-card" style={{ textAlign: "center", padding: "1.2rem 0.8rem" }}>
                <div style={{ fontSize: "28px" }}>{c.icon}</div>
                <div style={{ fontSize: "12px", color: "#888", marginTop: "6px" }}>{c.label}</div>
                <div style={{ fontWeight: 700, fontSize: "22px", marginTop: "4px" }}>{c.value}</div>
              </div>
            ))}
          </div>
          <div className="form-card">
            <h3 style={{ marginTop: 0 }}>Most Asked Topics</h3>
            {analytics.topicCounts.map(t => (
              <div key={t.topic} style={{ display: "flex", alignItems: "center", gap: "12px", marginBottom: "10px" }}>
                <div style={{ width: "90px", fontSize: "13px", fontWeight: 600, color: "#374151", textTransform: "capitalize" }}>{t.topic}</div>
                <div style={{ flex: 1, background: "#f1f5f9", borderRadius: "6px", overflow: "hidden" }}>
                  <div style={{ height: "20px", background: "#2563eb", width: `${Math.min(100, (t.count / (analytics.totalChats || 1)) * 100 * 3)}%`, minWidth: "4px", borderRadius: "6px" }} />
                </div>
                <div style={{ width: "30px", fontSize: "13px", color: "#888" }}>{t.count}</div>
              </div>
            ))}
            {analytics.topicCounts.length === 0 && <p style={{ color: "#888" }}>No chat data yet.</p>}
          </div>
        </div>
      )}

      {/* ── Students ── */}
      {tab === "students" && (
        <div className="form-card" style={{ overflowX: "auto" }}>
          <h3 style={{ marginTop: 0 }}>Registered Students ({students.length})</h3>
          <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "13px" }}>
            <thead>
              <tr style={{ background: "#f8fafc" }}>
                {["Name","Email","Course","Phone","Registered","Docs"].map(h => (
                  <th key={h} style={{ padding: "10px 12px", textAlign: "left", borderBottom: "1px solid #e5e7eb", fontWeight: 700, color: "#374151" }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {students.map(s => (
                <tr key={s.id} style={{ borderBottom: "1px solid #f1f5f9" }}>
                  <td style={{ padding: "10px 12px", fontWeight: 600 }}>{s.fullName}</td>
                  <td style={{ padding: "10px 12px", color: "#6b7280" }}>{s.email}</td>
                  <td style={{ padding: "10px 12px" }}>{s.course}</td>
                  <td style={{ padding: "10px 12px" }}>{s.phone}</td>
                  <td style={{ padding: "10px 12px", color: "#6b7280" }}>{new Date(s.registeredAt).toLocaleDateString()}</td>
                  <td style={{ padding: "10px 12px" }}>
                    <span style={{ fontWeight: 600 }}>{s.approvedDocs}</span>
                    <span style={{ color: "#aaa" }}>/{s.documentCount}</span>
                  </td>
                </tr>
              ))}
              {students.length === 0 && (
                <tr><td colSpan={6} style={{ padding: "20px", textAlign: "center", color: "#888" }}>No students yet.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* ── Documents ── */}
      {tab === "documents" && (
        <div className="form-card" style={{ overflowX: "auto" }}>
          <h3 style={{ marginTop: 0 }}>Document Verification</h3>
          <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "13px" }}>
            <thead>
              <tr style={{ background: "#f8fafc" }}>
                {["Student","Type","File","Uploaded","Status","Actions"].map(h => (
                  <th key={h} style={{ padding: "10px 12px", textAlign: "left", borderBottom: "1px solid #e5e7eb", fontWeight: 700, color: "#374151" }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {docs.map(d => (
                <tr key={d.id} style={{ borderBottom: "1px solid #f1f5f9" }}>
                  <td style={{ padding: "10px 12px", fontWeight: 600 }}>{d.studentName}</td>
                  <td style={{ padding: "10px 12px" }}>{d.documentType}</td>
                  <td style={{ padding: "10px 12px", color: "#6b7280", fontSize: "12px" }}>{d.fileName}</td>
                  <td style={{ padding: "10px 12px", color: "#6b7280" }}>{new Date(d.uploadedAt).toLocaleDateString()}</td>
                  <td style={{ padding: "10px 12px" }}><Badge status={d.status} /></td>
                  <td style={{ padding: "10px 12px" }}>
                    <div style={{ display: "flex", gap: "6px" }}>
                      <button onClick={() => updateDocStatus(d.id, "Approved")}
                        style={{ background: "#dcfce7", color: "#16a34a", border: "none", borderRadius: "6px", padding: "4px 10px", fontSize: "12px", fontWeight: 600, cursor: "pointer" }}>
                        ✅ Approve
                      </button>
                      <button onClick={() => updateDocStatus(d.id, "Rejected")}
                        style={{ background: "#fee2e2", color: "#dc2626", border: "none", borderRadius: "6px", padding: "4px 10px", fontSize: "12px", fontWeight: 600, cursor: "pointer" }}>
                        ❌ Reject
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
              {docs.length === 0 && (
                <tr><td colSpan={6} style={{ padding: "20px", textAlign: "center", color: "#888" }}>No documents uploaded yet.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* ── Chat Logs ── */}
      {tab === "chatlogs" && (
        <div className="form-card" style={{ overflowX: "auto" }}>
          <h3 style={{ marginTop: 0 }}>Recent Chat Logs ({logs.length})</h3>
          <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "13px" }}>
            <thead>
              <tr style={{ background: "#f8fafc" }}>
                {["Time","Topic","Student Message","Bot Reply"].map(h => (
                  <th key={h} style={{ padding: "10px 12px", textAlign: "left", borderBottom: "1px solid #e5e7eb", fontWeight: 700, color: "#374151" }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {logs.map(l => (
                <tr key={l.id} style={{ borderBottom: "1px solid #f1f5f9" }}>
                  <td style={{ padding: "10px 12px", color: "#6b7280", whiteSpace: "nowrap" }}>{new Date(l.createdAt).toLocaleString()}</td>
                  <td style={{ padding: "10px 12px" }}>
                    <Badge status={l.topic.charAt(0).toUpperCase() + l.topic.slice(1)} />
                  </td>
                  <td style={{ padding: "10px 12px", maxWidth: "220px" }}>{l.userMessage}</td>
                  <td style={{ padding: "10px 12px", color: "#6b7280", maxWidth: "280px", whiteSpace: "pre-wrap", fontSize: "12px" }}>{l.botReply.slice(0, 120)}{l.botReply.length > 120 ? "…" : ""}</td>
                </tr>
              ))}
              {logs.length === 0 && (
                <tr><td colSpan={4} style={{ padding: "20px", textAlign: "center", color: "#888" }}>No chat logs yet.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* ── Knowledge Base ── */}
      {tab === "kb" && (
        <div>
          <div className="form-card" style={{ marginBottom: "1.2rem" }}>
            <h3 style={{ marginTop: 0 }}>{kbEditing ? "Edit Entry" : "Add New KB Entry"}</h3>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "0.8rem" }}>
              {[["topic","Topic (fee/course/eligibility…)"],["question","Question"]].map(([k,p]) => (
                <div className="field" key={k}>
                  <label style={{ textTransform: "capitalize" }}>{p}</label>
                  <input value={kbForm[k]} onChange={e => setKbForm(f => ({...f, [k]: e.target.value}))} placeholder={p} />
                </div>
              ))}
            </div>
            <div className="field">
              <label>Answer</label>
              <textarea value={kbForm.answer} onChange={e => setKbForm(f => ({...f, answer: e.target.value}))}
                placeholder="Full answer shown to student" rows={3}
                style={{ width: "100%", padding: "8px", borderRadius: "8px", border: "1px solid #d1d5db", resize: "vertical", fontSize: "14px" }} />
            </div>
            <div className="field">
              <label>Keywords (space separated)</label>
              <input value={kbForm.keywords} onChange={e => setKbForm(f => ({...f, keywords: e.target.value}))} placeholder="fee cost price pay" />
            </div>
            <div style={{ display: "flex", gap: "8px", marginTop: "0.5rem" }}>
              <button className="btn-primary" onClick={saveKb}>{kbEditing ? "Update Entry" : "Add Entry"}</button>
              {kbEditing && (
                <button className="btn-secondary" onClick={() => { setKbEditing(false); setKbForm({ id:0,topic:"",question:"",answer:"",keywords:"" }); }}>
                  Cancel
                </button>
              )}
            </div>
          </div>

          <div className="form-card" style={{ overflowX: "auto" }}>
            <h3 style={{ marginTop: 0 }}>Knowledge Base Entries ({kb.length})</h3>
            <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "13px" }}>
              <thead>
                <tr style={{ background: "#f8fafc" }}>
                  {["Topic","Question","Answer (preview)","Actions"].map(h => (
                    <th key={h} style={{ padding: "10px 12px", textAlign: "left", borderBottom: "1px solid #e5e7eb", fontWeight: 700, color: "#374151" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {kb.map(e => (
                  <tr key={e.id} style={{ borderBottom: "1px solid #f1f5f9" }}>
                    <td style={{ padding: "10px 12px" }}><Badge status={e.topic.charAt(0).toUpperCase() + e.topic.slice(1)} /></td>
                    <td style={{ padding: "10px 12px", fontWeight: 600 }}>{e.question}</td>
                    <td style={{ padding: "10px 12px", color: "#6b7280", maxWidth: "260px" }}>{e.answer.slice(0,80)}…</td>
                    <td style={{ padding: "10px 12px" }}>
                      <div style={{ display: "flex", gap: "6px" }}>
                        <button onClick={() => { setKbForm({ id:e.id, topic:e.topic, question:e.question, answer:e.answer, keywords:e.keywords }); setKbEditing(true); }}
                          style={{ background: "#dbeafe", color: "#1d4ed8", border: "none", borderRadius: "6px", padding: "4px 10px", fontSize: "12px", fontWeight: 600, cursor: "pointer" }}>
                          ✏️ Edit
                        </button>
                        <button onClick={() => deleteKb(e.id)}
                          style={{ background: "#fee2e2", color: "#dc2626", border: "none", borderRadius: "6px", padding: "4px 10px", fontSize: "12px", fontWeight: 600, cursor: "pointer" }}>
                          🗑️ Delete
                        </button>
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
