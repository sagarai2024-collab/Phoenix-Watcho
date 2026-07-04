import { useState } from "react";
import Register from "./pages/Register";
import Login from "./pages/Login";
import Dashboard from "./pages/Dashboard";
import Admin from "./pages/Admin";
import ChatAssistant from "./components/ChatAssistant";
import "./App.css";

export default function App() {
  const [page, setPage]       = useState("login");
  const [student, setStudent] = useState(null);

  const handleLogin  = (data) => { setStudent(data); setPage("dashboard"); };
  const handleLogout = () => { setStudent(null); setPage("login"); };

  return (
    <div className="app">
      <header className="app-header">
        <h1>GreenLand University</h1>
        <p>Student Learning Portal</p>
        {!student && page !== "admin" && (
          <nav className="header-nav">
            <button className={page === "login"    ? "nav-active" : ""} onClick={() => setPage("login")}>Login</button>
            <button className={page === "register" ? "nav-active" : ""} onClick={() => setPage("register")}>Register</button>
            <button className={page === "admin"    ? "nav-active" : ""} onClick={() => setPage("admin")}
              style={{ fontSize: "12px", opacity: 0.7 }}>
              Admin
            </button>
          </nav>
        )}
      </header>

      <main className="app-main">
        {page === "login"     && <Login     onSwitch={() => setPage("register")} onLogin={handleLogin} />}
        {page === "register"  && <Register  onSwitch={() => setPage("login")} />}
        {page === "dashboard" && <Dashboard student={student} onLogout={handleLogout} />}
        {page === "admin"     && <Admin     onBack={() => setPage("login")} />}
      </main>

      <ChatAssistant />
    </div>
  );
}
