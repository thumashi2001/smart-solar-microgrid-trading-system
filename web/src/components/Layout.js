import {
  Link,
  useLocation,
  useNavigate,
} from "react-router-dom";

const sidebarBg = "#0B3B2E";

function Layout({ children }) {
  const navigate = useNavigate();
  const location = useLocation();

  const fullName =
    localStorage.getItem("fullName") || "User";

  const role =
    localStorage.getItem("role") || "";

  const handleLogout = () => {
    localStorage.clear();
    navigate("/");
  };

  const navItem = (to, label, icon) => {
    const active =
      location.pathname === to ||
      (to !== "/" &&
        location.pathname.startsWith(`${to}/`));

    return (
      <Link
        to={to}
        style={{
          display: "flex",
          alignItems: "center",
          gap: "12px",
          padding: "12px 20px",
          margin: "4px 12px",
          borderRadius: "10px",
          color: "#fff",
          textDecoration: "none",
          fontSize: "14px",
          fontWeight: 500,
          background: active
            ? "rgba(255,255,255,0.15)"
            : "transparent",
          transition: "background 0.2s ease",
        }}
      >
        <span style={{ fontSize: "16px" }}>
          {icon}
        </span>

        {label}
      </Link>
    );
  };

  return (
    <div
      style={{
        display: "flex",
        minHeight: "100vh",
        fontFamily:
          "'Segoe UI', Arial, sans-serif",
      }}
    >
      {/* LEFT SIDEBAR */}
      <div
        style={{
          width: "260px",
          background: sidebarBg,
          display: "flex",
          flexDirection: "column",
          flexShrink: 0,
        }}
      >
        {/* LOGO */}
        <div
          style={{
            padding: "24px 20px",
            display: "flex",
            alignItems: "center",
            gap: "10px",
          }}
        >
          <span style={{ fontSize: "30px" }}>
            ☀️
          </span>

          <div>
            <div
              style={{
                color: "#fff",
                fontWeight: 700,
                fontSize: "17px",
                lineHeight: 1.2,
              }}
            >
              Smart Solar
            </div>

            <div
              style={{
                color:
                  "rgba(255,255,255,0.6)",
                fontSize: "11px",
              }}
            >
              Clean Energy Brighter Tomorrow
            </div>
          </div>
        </div>

        {/* NAVIGATION */}
        <div
          style={{
            marginTop: "16px",
            flexGrow: 1,
          }}
        >
          {navItem(
            "/dashboard",
            "Dashboard",
            "📊"
          )}

          {navItem(
            "/users",
            "Users",
            "👤"
          )}

          {navItem(
            "/prosumers",
            "Prosumers",
            "🔌"
          )}

          {navItem(
            "/microgrid-nodes",
            "Microgrid Nodes",
            "☀️"
          )}

          {navItem(
            "/energy-slots",
            "Energy Slots",
            "⚡"
          )}
        </div>

        {/* LOGOUT */}
        <div
          style={{
            padding: "20px",
            borderTop:
              "1px solid rgba(255,255,255,0.12)",
          }}
        >
          <button
            type="button"
            onClick={handleLogout}
            style={{
              width: "100%",
              background: "transparent",
              border:
                "1px solid rgba(255,255,255,0.35)",
              color: "#fff",
              borderRadius: "8px",
              padding: "10px",
              cursor: "pointer",
              fontSize: "14px",
              fontWeight: 500,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              gap: "8px",
            }}
          >
            <span style={{ fontSize: "16px" }}>
              🚪
            </span>
            Logout
          </button>
        </div>
      </div>

      {/* MAIN AREA */}
      <div
        style={{
          flexGrow: 1,
          background: "#F7F5F1",
          minWidth: 0,
        }}
      >
        {/* TOP BAR */}
        <div
          style={{
            background: "#fff",
            borderBottom:
              "1px solid #E4E1DA",
            padding: "16px 32px",
            display: "flex",
            justifyContent: "flex-end",
            alignItems: "center",
            gap: "12px",
          }}
        >
          <div style={{ textAlign: "right" }}>
            <div
              style={{
                fontWeight: 600,
                fontSize: "14px",
                color: "#1C1F1E",
              }}
            >
              {fullName}
            </div>

            <div
              style={{
                color: "#6B6862",
                fontSize: "12px",
              }}
            >
              {role}
            </div>
          </div>

          <div
            style={{
              width: "38px",
              height: "38px",
              borderRadius: "50%",
              background:
                "linear-gradient(135deg, #1E7A4D, #0B3B2E)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              fontWeight: 700,
              color: "#fff",
              fontSize: "15px",
              border: "2px solid #E4E1DA",
            }}
          >
            {fullName
              .charAt(0)
              .toUpperCase()}
          </div>
        </div>

        {/* PAGE CONTENT */}
        <div>{children}</div>
      </div>
    </div>
  );
}

export default Layout;