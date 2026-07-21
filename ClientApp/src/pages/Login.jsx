import { useState } from "react";

export default function Login({ onSwitch, onLogin }) {
  const [form, setForm] = useState({ vcNumber: "", password: "" });
  const [status, setStatus] = useState(null);
  const [loading, setLoading] = useState(false);

  const handleChange = (e) => setForm((f) => ({ ...f, [e.target.name]: e.target.value }));

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setStatus(null);
    try {
      const res = await fetch("/api/subscriber/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(form),
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data || "Login failed");
      onLogin(data);
    } catch (err) {
      setStatus({ type: "error", msg: err.message });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="form-card">
      <h2>Subscriber Login</h2>
      {status && <div className={`alert alert-${status.type}`}>{status.msg}</div>}
      <form onSubmit={handleSubmit}>
        <div className="field" style={{ marginBottom: "1rem" }}>
          <label>VC Number *</label>
          <input name="vcNumber" value={form.vcNumber} onChange={handleChange} required
            placeholder="11-digit Viewing Card number" inputMode="numeric" />
          <span style={{ fontSize: "12px", color: "#718096" }}>
            Find it on the smart card in your set-top box, or on-screen under System Info.
          </span>
        </div>
        <div className="field" style={{ marginBottom: "1.5rem" }}>
          <label>Password *</label>
          <input type="password" name="password" value={form.password} onChange={handleChange} required placeholder="Your password" />
        </div>
        <button type="submit" disabled={loading} className="btn-primary">
          {loading ? "Signing in..." : "Login"}
        </button>
      </form>
      <p className="switch-link">New subscriber? <button onClick={onSwitch}>Get a new connection</button></p>
      <p style={{ textAlign: "center", marginTop: "0.8rem", fontSize: "12px", color: "#a0aec0" }}>
        Demo VC: <strong>01234567890</strong> · password <strong>Pass123</strong>
      </p>
    </div>
  );
}
