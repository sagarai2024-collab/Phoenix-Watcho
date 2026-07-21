import { useState } from "react";

const empty = { fullName: "", email: "", password: "", phone: "", address: "", city: "", state: "" };

const DOCUMENT_TYPES = [
  { key: "Aadhaar",      label: "Aadhaar Card *"   },
  { key: "AddressProof", label: "Address Proof *"  },
  { key: "Photo",        label: "Passport Photo *" },
];

function validate(form) {
  const errors = {};
  if (!form.fullName.trim())        errors.fullName = "Full Name is required.";
  else if (form.fullName.trim().length < 3) errors.fullName = "Full Name must be at least 3 characters.";

  if (!form.email.trim())           errors.email = "Email is required.";
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) errors.email = "Enter a valid email address.";

  if (!form.password)               errors.password = "Password is required.";
  else if (form.password.length < 6) errors.password = "Password must be at least 6 characters.";
  else if (!/^(?=.*[a-zA-Z])(?=.*[0-9])[a-zA-Z0-9]+$/.test(form.password))
    errors.password = "Password must contain letters and numbers. e.g. Pass123";

  if (!form.phone.trim())           errors.phone = "Mobile number is required.";
  else if (!/^\d{10}$/.test(form.phone.trim())) errors.phone = "Enter a valid 10-digit mobile number.";

  if (!form.address.trim()) errors.address = "Address is required.";
  if (!form.city.trim())    errors.city = "City is required.";
  if (!form.state.trim())   errors.state = "State is required.";
  return errors;
}

export default function Register({ onSwitch }) {
  const [form, setForm]           = useState(empty);
  const [errors, setErrors]       = useState({});
  const [serverError, setServerError] = useState(null);
  const [loading, setLoading]     = useState(false);

  // After registration — VC + KYC upload step
  const [subscriberId, setSubscriberId] = useState(null);
  const [vcNumber, setVcNumber]   = useState(null);
  const [documents, setDocuments] = useState({ Aadhaar: null, AddressProof: null, Photo: null });
  const [uploadStatus, setUploadStatus] = useState({});
  const [uploading, setUploading] = useState(false);
  const [done, setDone]           = useState(false);

  const handleChange = (e) => {
    setForm((f) => ({ ...f, [e.target.name]: e.target.value }));
    setErrors((prev) => ({ ...prev, [e.target.name]: undefined }));
  };

  const handleFileChange = (docType, file) => {
    setDocuments((prev) => ({ ...prev, [docType]: file }));
    setUploadStatus((prev) => ({ ...prev, [docType]: undefined }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setServerError(null);
    const validationErrors = validate(form);
    if (Object.keys(validationErrors).length > 0) { setErrors(validationErrors); return; }

    setLoading(true);
    try {
      const res  = await fetch("/api/subscriber/register", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(form),
      });
      const text = await res.text();
      const data = text ? JSON.parse(text) : {};
      if (!res.ok) throw new Error(typeof data === "string" ? data : data?.message || "Registration failed");
      setSubscriberId(data.id);
      setVcNumber(data.vcNumber);
      setForm(empty);
      setErrors({});
    } catch (err) {
      setServerError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleUploadAll = async () => {
    const missing = DOCUMENT_TYPES.filter(d => !documents[d.key]);
    if (missing.length > 0) {
      alert(`Please select files for: ${missing.map(d => d.label).join(", ")}`);
      return;
    }

    setUploading(true);
    const newStatus = {};
    for (const doc of DOCUMENT_TYPES) {
      const formData = new FormData();
      formData.append("subscriberId", subscriberId);
      formData.append("documentType", doc.key);
      formData.append("file", documents[doc.key]);
      try {
        const res = await fetch("/api/document/upload", { method: "POST", body: formData });
        const data = await res.json();
        newStatus[doc.key] = res.ok ? "success" : `Error: ${data}`;
      } catch {
        newStatus[doc.key] = "Upload failed";
      }
    }
    setUploadStatus(newStatus);
    if (Object.values(newStatus).every(s => s === "success")) setDone(true);
    setUploading(false);
  };

  // ── KYC Upload Step ─────────────────────────────────────────────────────
  if (subscriberId && !done) {
    return (
      <div className="form-card">
        <h2>Welcome aboard! 📺</h2>
        <div className="alert alert-success" style={{ fontSize: "0.95rem" }}>
          Your connection is created. Your <strong>VC Number</strong> is{" "}
          <strong style={{ fontSize: "1.1rem" }}>{vcNumber}</strong> — use it to log in.
        </div>
        <p style={{ color:"#555", margin:"0.5rem 0 1.2rem", fontSize:"14px" }}>
          Please upload your KYC documents to complete verification.
        </p>

        {DOCUMENT_TYPES.map((doc) => (
          <div className="field" key={doc.key} style={{ marginBottom:"1rem" }}>
            <label>{doc.label}</label>
            <input type="file" accept=".jpg,.jpeg,.png,.pdf"
              onChange={(e) => handleFileChange(doc.key, e.target.files[0])} style={{ padding:"6px 0" }} />
            <span style={{ fontSize:"12px", color:"#888" }}>JPG, PNG or PDF — max 5MB</span>
            {uploadStatus[doc.key] === "success" && (
              <span style={{ color:"#16a34a", fontSize:"13px", display:"block" }}>✅ Uploaded</span>
            )}
            {uploadStatus[doc.key] && uploadStatus[doc.key] !== "success" && (
              <span style={{ color:"#e53e3e", fontSize:"13px", display:"block" }}>{uploadStatus[doc.key]}</span>
            )}
          </div>
        ))}

        <button onClick={handleUploadAll} disabled={uploading} className="btn-primary" style={{ marginTop:"0.5rem" }}>
          {uploading ? "Uploading..." : "Upload KYC Documents"}
        </button>
      </div>
    );
  }

  // ── Success Step ─────────────────────────────────────────────────────────
  if (done) {
    return (
      <div className="form-card" style={{ textAlign:"center" }}>
        <div style={{ fontSize:"48px", marginBottom:"1rem" }}>🎉</div>
        <h2 style={{ color:"#16a34a" }}>All Set!</h2>
        <p style={{ color:"#555", margin:"1rem 0" }}>
          Your DishTV connection is registered and documents submitted.<br/>
          Log in with VC Number <strong>{vcNumber}</strong> to choose a plan.
        </p>
        <button className="btn-primary" onClick={onSwitch}>Go to Login</button>
      </div>
    );
  }

  // ── Registration Form ────────────────────────────────────────────────────
  return (
    <div className="form-card">
      <h2>New DishTV Connection</h2>
      {serverError && <div className="alert alert-error">{serverError}</div>}

      <form onSubmit={handleSubmit} noValidate>
        <div className="form-row">
          <div className="field">
            <label>Full Name *</label>
            <input name="fullName" value={form.fullName} onChange={handleChange} placeholder="Your Name" />
            {errors.fullName && <span className="field-error">{errors.fullName}</span>}
          </div>
          <div className="field">
            <label>Email *</label>
            <input type="email" name="email" value={form.email} onChange={handleChange} placeholder="you@example.com" />
            {errors.email && <span className="field-error">{errors.email}</span>}
          </div>
        </div>

        <div className="form-row">
          <div className="field">
            <label>Password *</label>
            <input type="password" name="password" value={form.password} onChange={handleChange} placeholder="Alphanumeric, min 6 chars" />
            {errors.password && <span className="field-error">{errors.password}</span>}
          </div>
          <div className="field">
            <label>Mobile (RMN) *</label>
            <input name="phone" value={form.phone} onChange={handleChange} placeholder="10-digit mobile" />
            {errors.phone && <span className="field-error">{errors.phone}</span>}
          </div>
        </div>

        <div className="field" style={{ marginBottom: "1rem" }}>
          <label>Installation Address *</label>
          <input name="address" value={form.address} onChange={handleChange} placeholder="House / street / area" />
          {errors.address && <span className="field-error">{errors.address}</span>}
        </div>

        <div className="form-row">
          <div className="field">
            <label>City *</label>
            <input name="city" value={form.city} onChange={handleChange} placeholder="City" />
            {errors.city && <span className="field-error">{errors.city}</span>}
          </div>
          <div className="field">
            <label>State *</label>
            <input name="state" value={form.state} onChange={handleChange} placeholder="State" />
            {errors.state && <span className="field-error">{errors.state}</span>}
          </div>
        </div>

        <button type="submit" disabled={loading} className="btn-primary" style={{ marginTop:"1rem" }}>
          {loading ? "Creating connection..." : "Create Connection"}
        </button>
      </form>
      <p className="switch-link">Already a subscriber? <button onClick={onSwitch}>Login here</button></p>
    </div>
  );
}
