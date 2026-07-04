import { useState, useEffect } from "react";

const STATUS_COLORS = {
  Approved: { bg: "#dcfce7", color: "#16a34a", label: "✅ Approved" },
  Rejected: { bg: "#fee2e2", color: "#dc2626", label: "❌ Rejected" },
  Pending:  { bg: "#fef9c3", color: "#ca8a04", label: "⏳ Pending"  },
};

export default function Dashboard({ student, onLogout }) {
  const [docs, setDocs]       = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch(`/api/document/${student.id}`)
      .then(r => r.json())
      .then(setDocs)
      .catch(() => setDocs([]))
      .finally(() => setLoading(false));
  }, [student.id]);

  const allApproved  = docs.length === 4 && docs.every(d => d.status === "Approved");
  const approvedCount = docs.filter(d => d.status === "Approved").length;

  return (
    <div style={{ maxWidth: "760px", margin: "0 auto", padding: "0 1rem" }}>

      {/* ── Welcome banner ── */}
      <div className="form-card" style={{ marginBottom: "1.2rem", background: "linear-gradient(135deg,#2563eb,#1d4ed8)", color: "#fff", borderRadius: "14px" }}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", flexWrap: "wrap", gap: "1rem" }}>
          <div>
            <div style={{ fontSize: "28px", marginBottom: "4px" }}>👋</div>
            <h2 style={{ margin: 0, color: "#fff" }}>Welcome, {student.fullName}!</h2>
            <p style={{ margin: "6px 0 0", opacity: 0.85, fontSize: "14px" }}>Greenland University Student Portal</p>
          </div>
          <button className="btn-secondary" onClick={onLogout}
            style={{ background: "rgba(255,255,255,0.15)", color: "#fff", border: "1px solid rgba(255,255,255,0.4)" }}>
            Logout
          </button>
        </div>
      </div>

      {/* ── Stats row ── */}
      <div style={{ display: "grid", gridTemplateColumns: "repeat(3,1fr)", gap: "1rem", marginBottom: "1.2rem" }}>
        {[
          { icon: "🎓", label: "Course",    value: student.course },
          { icon: "📅", label: "Registered", value: student.registeredAt ? new Date(student.registeredAt).toLocaleDateString("en-IN",{ day:"2-digit",month:"short",year:"numeric" }) : "—" },
          { icon: "📄", label: "Docs Approved", value: loading ? "…" : `${approvedCount} / 4` },
        ].map(card => (
          <div key={card.label} className="form-card" style={{ textAlign: "center", padding: "1.2rem 0.8rem" }}>
            <div style={{ fontSize: "28px" }}>{card.icon}</div>
            <div style={{ fontSize: "12px", color: "#888", marginTop: "6px" }}>{card.label}</div>
            <div style={{ fontWeight: 700, fontSize: "15px", marginTop: "4px" }}>{card.value}</div>
          </div>
        ))}
      </div>

      {/* ── Profile info ── */}
      <div className="form-card" style={{ marginBottom: "1.2rem" }}>
        <h3 style={{ marginTop: 0, marginBottom: "1rem" }}>Profile Information</h3>
        <div className="dashboard-info">
          {[
            ["Email",  student.email],
            ["Course", student.course],
            ["Status", allApproved ? "✅ Documents Verified" : "⏳ Verification Pending"],
          ].map(([label, val]) => (
            <div className="info-row" key={label}>
              <span>{label}</span>
              <strong>{val}</strong>
            </div>
          ))}
        </div>
      </div>

      {/* ── Documents ── */}
      <div className="form-card">
        <h3 style={{ marginTop: 0, marginBottom: "1rem" }}>Uploaded Documents</h3>

        {loading && <p style={{ color: "#888" }}>Loading documents…</p>}

        {!loading && docs.length === 0 && (
          <p style={{ color: "#e53e3e" }}>No documents uploaded yet. Please complete the registration process.</p>
        )}

        {!loading && docs.length > 0 && (
          <div style={{ display: "flex", flexDirection: "column", gap: "0.75rem" }}>
            {docs.map(doc => {
              const s = STATUS_COLORS[doc.status] ?? STATUS_COLORS.Pending;
              return (
                <div key={doc.id} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: "0.75rem 1rem", borderRadius: "8px", border: "1px solid #e5e7eb", background: "#fafafa" }}>
                  <div>
                    <div style={{ fontWeight: 600, fontSize: "14px" }}>{doc.documentType.replace(/(\d+)/, ' $1')}</div>
                    <div style={{ fontSize: "12px", color: "#888", marginTop: "2px" }}>{doc.fileName}</div>
                    <div style={{ fontSize: "11px", color: "#aaa" }}>Uploaded: {new Date(doc.uploadedAt).toLocaleDateString()}</div>
                  </div>
                  <span style={{ background: s.bg, color: s.color, padding: "4px 12px", borderRadius: "20px", fontSize: "12px", fontWeight: 600 }}>
                    {s.label}
                  </span>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
