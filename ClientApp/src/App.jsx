import { useState } from "react";
import Register from "./pages/Register";
import Login from "./pages/Login";
import Dashboard from "./pages/Dashboard";
import Admin from "./pages/Admin";
import ChatAssistant from "./components/ChatAssistant";
import "./App.css";

export default function App() {
  const [page, setPage] = useState("login");
  const [subscriber, setSubscriber] = useState(null);

  const handleLogin  = (data) => { setSubscriber(data); setPage("dashboard"); };
  const handleLogout = () => { setSubscriber(null); setPage("login"); };

  return (
    <div className="app">
      <header className="app-header">
        <h1>Dish<span className="brand-mark">TV</span> · Watcho</h1>
        <p>Consumer Self-Care Portal</p>
        {!subscriber && page !== "admin" && (
          <nav className="header-nav">
            <button className={page === "login"    ? "nav-active" : ""} onClick={() => setPage("login")}>Login</button>
            <button className={page === "register" ? "nav-active" : ""} onClick={() => setPage("register")}>New Connection</button>
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
        {page === "dashboard" && <Dashboard subscriber={subscriber} onLogout={handleLogout} />}
        {page === "admin"     && <Admin     onBack={() => setPage("login")} />}
      </main>

      <ChatAssistant />
    </div>
  );
}
