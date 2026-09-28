import { useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../api";

// Brand colours Bootstrap doesn't have. Everything else uses Bootstrap classes.
const brand = {
  dark: "#0B3B2E",
  green: "#1E7A4D",
  sun: "#FFD54F",
  page: "#F7F5F1",
};

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
    <div className="container-fluid">
      <div className="row min-vh-100">
        {/* Left panel: hidden below 768px (phones), visible on tablets and up */}
        <div
          className="col-md-5 d-none d-md-flex flex-column justify-content-center text-white position-relative overflow-hidden p-5"
          style={{ background: `linear-gradient(160deg, ${brand.dark} 0%, ${brand.green} 100%)` }}
        >
          <svg
            viewBox="0 0 200 200"
            className="position-absolute"
            style={{ top: "-40px", right: "-40px", width: "220px", opacity: 0.15 }}
          >
            <circle cx="100" cy="100" r="60" fill={brand.sun} />
            {[...Array(12)].map((_, i) => (
              <line
                key={i}
                x1="100"
                y1="100"
                x2={100 + 95 * Math.cos((i * Math.PI) / 6)}
                y2={100 + 95 * Math.sin((i * Math.PI) / 6)}
                stroke={brand.sun}
                strokeWidth="4"
              />
            ))}
          </svg>

          <div className="d-flex align-items-center gap-2 mb-5">
            <span className="fs-1">☀️</span>
            <div>
              <div className="fw-bold fs-5">Smart Solar</div>
              <div className="small opacity-75">Clean Energy, Brighter Tomorrow</div>
            </div>
          </div>

          <h1 className="display-6 fw-semibold mb-3">Powering a Sustainable Tomorrow</h1>

          <div className="d-flex flex-column gap-3 mt-3">
            <Feature icon="📡" title="Monitor Microgrid Nodes" desc="Track station status across the network" />
            <Feature icon="⚡" title="Manage Energy Transfers" desc="Coordinate prosumer bookings and slots" />
            <Feature icon="🌱" title="Support a Greener Future" desc="Enable clean, distributed energy trading" />
          </div>
        </div>

        {/* Right panel: login form */}
        <div
          className="col-12 col-md-7 d-flex align-items-center justify-content-center p-4"
          style={{ background: brand.page }}
        >
          <form
            onSubmit={handleLogin}
            className="bg-white p-4 p-md-5 rounded-4 shadow-sm w-100"
            style={{ maxWidth: "420px" }}
          >
            <h2 className="fw-bold mb-1">Welcome Back</h2>
            <p className="text-secondary small mb-4">Sign in to your account to continue</p>

            <div className="mb-3">
              <label htmlFor="identifier" className="form-label fw-semibold small">
                Email or NIC
              </label>
              <input
                id="identifier"
                type="text"
                className="form-control py-2"
                value={identifier}
                onChange={(e) => setIdentifier(e.target.value)}
                placeholder="name@example.com"
              />
            </div>

            <div className="mb-3">
              <label htmlFor="password" className="form-label fw-semibold small">
                Password
              </label>
              <input
                id="password"
                type="password"
                className="form-control py-2"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
              />
            </div>

            {error && (
              <div className="alert alert-danger py-2 small mb-0" role="alert">
                {error}
              </div>
            )}

            <button
              type="submit"
              className="btn w-100 mt-4 py-2 fw-semibold text-white"
              style={{ background: brand.green }}
            >
              Sign In
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}

function Feature({ icon, title, desc }) {
  return (
    <div className="d-flex gap-3 align-items-start">
      <div
        className="rounded-circle d-flex align-items-center justify-content-center flex-shrink-0"
        style={{ width: "36px", height: "36px", background: "rgba(255,255,255,0.15)" }}
      >
        {icon}
      </div>
      <div>
        <div className="fw-semibold small">{title}</div>
        <div className="small opacity-75">{desc}</div>
      </div>
    </div>
  );
}

export default Login;