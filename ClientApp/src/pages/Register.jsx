import { useState, useEffect } from "react";

const COURSES = ["B.Tech CS", "B.Tech ECE", "B.Tech ME", "BCA", "BBA", "MBA"];
const empty = { fullName: "", email: "", password: "", phone: "", course: "", gender: "", dateOfBirth: "", address: "" };

const DOCUMENT_TYPES = [
  { key: "Marksheet10", label: "10th Marksheet *" },
  { key: "Marksheet12", label: "12th Marksheet *" },
  { key: "Photo",       label: "Passport Photo *" },
  { key: "Aadhaar",     label: "Aadhaar Card *"   },
];

function validate(form) {
  const errors = {};
  if (!form.fullName.trim())
    errors.fullName = "Full Name is required.";
  else if (form.fullName.trim().length < 3)
    errors.fullName = "Full Name must be at least 3 characters.";

  if (!form.email.trim())
    errors.email = "Email is required.";
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email))
    errors.email = "Enter a valid email address.";

  if (!form.password)
    errors.password = "Password is required.";
  else if (form.password.length < 6)
    errors.password = "Password must be at least 6 characters.";
  else if (!/^(?=.*[a-zA-Z])(?=.*[0-9])[a-zA-Z0-9]+$/.test(form.password))
    errors.password = "Password must contain letters and numbers only. e.g. Pass123";

  if (!form.phone.trim())
    errors.phone = "Phone number is required.";
  else if (!/^\d{10}$/.test(form.phone.trim()))
    errors.phone = "Enter a valid 10-digit phone number.";

  if (!form.course)   errors.course = "Please select a course.";
  if (!form.gender)   errors.gender = "Please select a gender.";
  if (!form.dateOfBirth) errors.dateOfBirth = "Date of Birth is required.";
  if (!form.address.trim()) errors.address = "Address is required.";

  return errors;
}

export default function Register({ onSwitch }) {
  const [form, setForm]           = useState(empty);
  const [errors, setErrors]       = useState({});
  const [serverError, setServerError] = useState(null);
  const [loading, setLoading]     = useState(false);
  const [popup, setPopup]         = useState(false);

  // After registration — document upload step
  const [studentId, setStudentId] = useState(null);
  const [documents, setDocuments] = useState({ Marksheet10: null, Marksheet12: null, Photo: null, Aadhaar: null });
  const [uploadStatus, setUploadStatus] = useState({});
  const [uploading, setUploading] = useState(false);
  const [allUploaded, setAllUploaded] = useState(false);

  const handleChange = (e) => {
    setForm((f) => ({ ...f, [e.target.name]: e.target.value }));
    setErrors((prev) => ({ ...prev, [e.target.name]: undefined }));
  };

  const handleFileChange = (docType, file) => {
    setDocuments((prev) => ({ ...prev, [docType]: file }));
    setUploadStatus((prev) => ({ ...prev, [docType]: undefined }));
  };

  useEffect(() => {
    if (!popup) return;
    const t = setTimeout(() => setPopup(false), 3000);
    return () => clearTimeout(t);
  }, [popup]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setServerError(null);
    const validationErrors = validate(form);
    if (Object.keys(validationErrors).length > 0) { setErrors(validationErrors); return; }

    setLoading(true);
    try {
      const res  = await fetch("/api/student/register", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ ...form, dateOfBirth: new Date(form.dateOfBirth) }),
      });
      const text = await res.text();
      const data = text ? JSON.parse(text) : {};
      if (!res.ok) throw new Error(typeof data === "string" ? data : data?.message || data?.title || "Registration failed");
      setStudentId(data.id);  // Move to document upload step
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
      formData.append("studentId", studentId);
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
    const allOk = Object.values(newStatus).every(s => s === "success");
    if (allOk) { setAllUploaded(true); setPopup(true); }
    setUploading(false);
  };

  // ── Document Upload Step ─────────────────────────────────────────────────
  if (studentId && !allUploaded) {
    return (
      <div className="form-card">
        {popup && (
          <div style={{ position:"fixed", top:"24px", right:"24px", zIndex:9999, background:"#22c55e", color:"#fff", padding:"14px 24px", borderRadius:"10px", boxShadow:"0 4px 16px rgba(0,0,0,0.15)", fontSize:"15px", fontWeight:600, animation:"slideIn 0.3s ease" }}>
            ✅ All documents uploaded successfully!
          </div>
        )}
        <h2>Upload Documents</h2>
        <p style={{ color:"#555", marginBottom:"1.2rem", fontSize:"14px" }}>
          Registration successful! Please upload your documents to complete the process.
        </p>

        {DOCUMENT_TYPES.map((doc) => (
          <div className="field" key={doc.key} style={{ marginBottom:"1rem" }}>
            <label>{doc.label}</label>
            <input type="file" accept=".jpg,.jpeg,.png,.pdf"
              onChange={(e) => handleFileChange(doc.key, e.target.files[0])}
              style={{ padding:"6px 0" }} />
            <span style={{ fontSize:"12px", color:"#888" }}>JPG, PNG or PDF — max 5MB</span>
            {uploadStatus[doc.key] === "success" && (
              <span style={{ color:"#22c55e", fontSize:"13px", display:"block" }}>✅ Uploaded</span>
            )}
            {uploadStatus[doc.key] && uploadStatus[doc.key] !== "success" && (
              <span style={{ color:"#e53e3e", fontSize:"13px", display:"block" }}>{uploadStatus[doc.key]}</span>
            )}
          </div>
        ))}

        <button onClick={handleUploadAll} disabled={uploading} className="btn-primary" style={{ marginTop:"0.5rem" }}>
          {uploading ? "Uploading..." : "Upload All Documents"}
        </button>
      </div>
    );
  }

  // ── Success Step ─────────────────────────────────────────────────────────
  if (allUploaded) {
    return (
      <div className="form-card" style={{ textAlign:"center" }}>
        <div style={{ fontSize:"48px", marginBottom:"1rem" }}>🎉</div>
        <h2 style={{ color:"#22c55e" }}>Registration Complete!</h2>
        <p style={{ color:"#555", margin:"1rem 0" }}>Your registration and documents have been submitted successfully.</p>
        <button className="btn-primary" onClick={onSwitch}>Go to Login</button>
      </div>
    );
  }

  // ── Registration Form ────────────────────────────────────────────────────
  return (
    <div className="form-card">
      {popup && (
        <div style={{ position:"fixed", top:"24px", right:"24px", zIndex:9999, background:"#22c55e", color:"#fff", padding:"14px 24px", borderRadius:"10px", boxShadow:"0 4px 16px rgba(0,0,0,0.15)", fontSize:"15px", fontWeight:600, animation:"slideIn 0.3s ease" }}>
          ✅ Registration Successful! Now upload your documents.
        </div>
      )}

      <h2>Student Registration</h2>
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
            <input type="email" name="email" value={form.email} onChange={handleChange} placeholder="Your Mail Address" />
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
            <label>Phone *</label>
            <input name="phone" value={form.phone} onChange={handleChange} placeholder="10-digit number" />
            {errors.phone && <span className="field-error">{errors.phone}</span>}
          </div>
        </div>

        <div className="form-row">
          <div className="field">
            <label>Course *</label>
            <select name="course" value={form.course} onChange={handleChange}>
              <option value="">Select course</option>
              {COURSES.map((c) => <option key={c}>{c}</option>)}
            </select>
            {errors.course && <span className="field-error">{errors.course}</span>}
          </div>
          <div className="field">
            <label>Gender *</label>
            <select name="gender" value={form.gender} onChange={handleChange}>
              <option value="">Select</option>
              <option>Male</option><option>Female</option><option>Other</option>
            </select>
            {errors.gender && <span className="field-error">{errors.gender}</span>}
          </div>
        </div>

        <div className="form-row">
          <div className="field">
            <label>Date of Birth *</label>
            <input type="date" name="dateOfBirth" value={form.dateOfBirth} onChange={handleChange} />
            {errors.dateOfBirth && <span className="field-error">{errors.dateOfBirth}</span>}
          </div>
          <div className="field">
            <label>Address *</label>
            <input name="address" value={form.address} onChange={handleChange} placeholder="Your Address" />
            {errors.address && <span className="field-error">{errors.address}</span>}
          </div>
        </div>

        <button type="submit" disabled={loading} className="btn-primary" style={{ marginTop:"1rem" }}>
          {loading ? "Registering..." : "Register & Continue"}
        </button>
      </form>
      <p className="switch-link">Already registered? <button onClick={onSwitch}>Login here</button></p>
    </div>
  );
}
