import { useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../api";

function Login() {
  const [identifier, setIdentifier] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const navigate = useNavigate();

 const handleLogin = async (e) => {
    e.preventDefault();
    setError("");
    try {
      const res = await api.post("/auth/login", { identifier, password });

      if (res.data.role === "Prosumer") {
        setError("Prosumer accounts must use the mobile app to log in.");
        return;
      }

      localStorage.setItem("token", res.data.token);
      localStorage.setItem("role", res.data.role);
      localStorage.setItem("fullName", res.data.fullName);

      if (res.data.role === "Backoffice") navigate("/dashboard");
      else if (res.data.role === "GridOperator") navigate("/operator");
    } catch (err) {
      setError("Invalid email/NIC or password.");
    }
  };

  return (
    <div style={{ display: "flex", height: "100vh", fontFamily: "'Segoe UI', Arial, sans-serif" }}>
      <div
        style={{
          width: "42%",
          background: "linear-gradient(160deg, #0B3B2E 0%, #1E7A4D 100%)",
          color: "#fff",
          padding: "60px",
          boxSizing: "border-box",
          display: "flex",
          flexDirection: "column",
          justifyContent: "center",
          position: "relative",
          overflow: "hidden",
        }}
      >
        <svg
          viewBox="0 0 200 200"
          style={{ position: "absolute", top: "-40px", right: "-40px", width: "220px", opacity: 0.15 }}
        >
          <circle cx="100" cy="100" r="60" fill="#FFD54F" />
          {[...Array(12)].map((_, i) => (
            <line
              key={i}
              x1="100" y1="100"
              x2={100 + 95 * Math.cos((i * Math.PI) / 6)}
              y2={100 + 95 * Math.sin((i * Math.PI) / 6)}
              stroke="#FFD54F"
              strokeWidth="4"
            />
          ))}
        </svg>

        <div style={{ display: "flex", alignItems: "center", gap: "10px", marginBottom: "40px" }}>
          <span style={{ fontSize: "36px" }}>☀️</span>
          <div>
            <div style={{ fontWeight: 700, fontSize: "20px" }}>Smart Solar</div>
            <div style={{ fontSize: "12px", opacity: 0.75 }}>Clean Energy, Brighter Tomorrow</div>
          </div>
        </div>

        <h1 style={{ fontSize: "34px", marginBottom: "16px", lineHeight: 1.25 }}>
          Powering a Sustainable Tomorrow
        </h1>

        <div style={{ display: "flex", flexDirection: "column", gap: "20px", marginTop: "16px" }}>
          <Feature icon="📡" title="Monitor Microgrid Nodes" desc="Track station status across the network" />
          <Feature icon="⚡" title="Manage Energy Transfers" desc="Coordinate prosumer bookings and slots" />
          <Feature icon="🌱" title="Support a Greener Future" desc="Enable clean, distributed energy trading" />
        </div>
      </div>

      <div style={{ width: "58%", display: "flex", alignItems: "center", justifyContent: "center", background: "#F7F5F1" }}>
        <form
          onSubmit={handleLogin}
          style={{
            width: "380px",
            background: "#fff",
            padding: "44px",
            borderRadius: "16px",
            boxShadow: "0 8px 30px rgba(0,0,0,0.08)",
          }}
        >
          <h2 style={{ margin: "0 0 6px 0", fontSize: "28px" }}>Welcome Back</h2>
          <p style={{ color: "#6B6862", marginBottom: "28px", fontSize: "14px" }}>
            Sign in to your account to continue
          </p>

          <label style={labelStyle}>Email or NIC</label>
          <input
            type="text"
            value={identifier}
            onChange={(e) => setIdentifier(e.target.value)}
            placeholder="name@example.com"
            style={inputStyle}
          />

          <label style={labelStyle}>Password</label>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="••••••••"
            style={inputStyle}
          />

          {error && (
  <p style={{
    color: "#B14A3C",
    background: "#F7E7E5",
    padding: "10px 12px",
    borderRadius: "8px",
    fontSize: "13px",
    marginTop: "16px",
    marginBottom: "0",
  }}>
    {error}
  </p>
)}

          <button type="submit" style={submitStyle}>Sign In</button>
        </form>
      </div>
    </div>
  );
}

function Feature({ icon, title, desc }) {
  return (
    <div style={{ display: "flex", gap: "12px", alignItems: "flex-start" }}>
      <div style={{ background: "rgba(255,255,255,0.15)", borderRadius: "50%", width: "36px", height: "36px", display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
        {icon}
      </div>
      <div>
        <div style={{ fontWeight: 600, fontSize: "14px" }}>{title}</div>
        <div style={{ fontSize: "12px", opacity: 0.8 }}>{desc}</div>
      </div>
    </div>
  );
}

const labelStyle = { display: "block", fontSize: "13px", fontWeight: 600, marginBottom: "6px", marginTop: "16px", color: "#3A3733" };
const inputStyle = { width: "100%", boxSizing: "border-box", padding: "12px 14px", border: "1px solid #D8D4CB", borderRadius: "10px", fontSize: "14px" };
const submitStyle = { width: "100%", marginTop: "26px", padding: "14px", background: "#1E7A4D", color: "#fff", border: "none", borderRadius: "10px", fontSize: "15px", fontWeight: 600, cursor: "pointer" };

export default Login;