import { useState, useRef, useEffect } from "react";

export default function ChatAssistant() {
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState([
    { role: "assistant", content: "Hi! I'm the DishTV / Watcho assistant. Ask me about plans, recharge, expiry or charges — or type your VC number to check your plan." },
  ]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const bottomRef = useRef(null);

  useEffect(() => { bottomRef.current?.scrollIntoView({ behavior: "smooth" }); }, [messages]);

  const send = async () => {
    const msg = input.trim();
    if (!msg || loading) return;

    const history = messages.map((m) => ({ role: m.role, content: m.content }));
    setMessages((p) => [...p, { role: "user", content: msg }]);
    setInput("");
    setLoading(true);

    try {
      const res = await fetch("/api/chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ message: msg, history }),
      });

      if (!res.ok) {
        const errText = await res.text();
        throw new Error(`Server error ${res.status}: ${errText}`);
      }

      const data = await res.json();
      setMessages((p) => [...p, { role: "assistant", content: data.reply }]);
    } catch (err) {
      const errorMsg = err.message.includes("Failed to fetch")
        ? "Cannot reach the server. Make sure the backend (dotnet run) is running."
        : `Error: ${err.message}`;
      setMessages((p) => [...p, { role: "assistant", content: errorMsg }]);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="chat-wrapper">
      {open && (
        <div className="chat-box">
          <div className="chat-header">
            <span>DishTV Assistant</span>
            <button onClick={() => setOpen(false)} className="chat-close">✕</button>
          </div>
          <div className="chat-messages">
            {messages.map((m, i) => (
              <div key={i} className={`chat-bubble ${m.role}`}>{m.content}</div>
            ))}
            {loading && <div className="chat-bubble assistant typing">Wait ...I am Typing...</div>}
            <div ref={bottomRef} />
          </div>
          <div className="chat-input-row">
            <input value={input} onChange={(e) => setInput(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && send()}
              placeholder="Ask about plans, recharge, expiry..." disabled={loading} />
            <button onClick={send} disabled={loading || !input.trim()} className="btn-send">Send</button>
          </div>
        </div>
      )}
      <button className="chat-fab" onClick={() => setOpen((o) => !o)}>{open ? "✕" : "💬"}</button>
    </div>
  );
}
