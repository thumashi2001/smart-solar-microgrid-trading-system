import { Link, useLocation, useNavigate } from "react-router-dom";

// Brand colours Bootstrap doesn't have
const sidebarBg = "#0B3B2E";
const pageBg = "#F7F5F1";

function Layout({ children }) {
  const navigate = useNavigate();
  const location = useLocation();

  const fullName = localStorage.getItem("fullName") || "User";
  const role = localStorage.getItem("role") || "";

  // Clears the saved login and returns to the login page.
  const handleLogout = () => {
    localStorage.clear();
    navigate("/");
  };

  // One sidebar link; highlighted when the current path matches.
  const navItem = (to, label, icon) => {
    const active =
      location.pathname === to ||
      (to !== "/" && location.pathname.startsWith(`${to}/`));

    return (
      <Link
        to={to}
        className={`d-flex align-items-center gap-3 text-white text-decoration-none rounded-3 px-3 py-2 mx-3 my-1 small fw-medium ${
          active ? "bg-white bg-opacity-25" : ""
        }`}
      >
        <i className={`bi ${icon} fs-6`}></i>
        {label}
      </Link>
    );
  };

  return (
    <div className="d-flex min-vh-100">
      {/* LEFT SIDEBAR */}
      <div
        className="d-flex flex-column flex-shrink-0"
        style={{ width: "260px", background: sidebarBg }}
      >
        {/* LOGO */}
        <div className="d-flex align-items-center gap-2 px-3 py-4">
          <i className="bi bi-brightness-high-fill fs-2 text-warning"></i>
          <div>
            <div className="text-white fw-bold lh-sm">Smart Solar</div>
            <div className="text-white text-opacity-75" style={{ fontSize: "11px" }}>
              Clean Energy Brighter Tomorrow
            </div>
          </div>
        </div>

        {/* NAVIGATION */}
        <div className="mt-3 flex-grow-1">
          {navItem("/dashboard", "Dashboard", "bi-bar-chart-line")}
          {navItem("/users", "Users", "bi-person")}
          {navItem("/prosumers", "Prosumers", "bi-plug")}
          {navItem("/microgrid-nodes", "Microgrid Nodes", "bi-sun")}
          {navItem("/energy-slots", "Energy Slots", "bi-lightning-charge")}
          {navItem("/reservations", "Reservations", "bi-clipboard-check")}
        </div>

        {/* LOGOUT */}
        <div className="p-3 border-top border-light border-opacity-25">
          <button
            type="button"
            onClick={handleLogout}
            className="btn btn-outline-light w-100 d-flex align-items-center justify-content-center gap-2"
          >
            <i className="bi bi-box-arrow-right"></i>
            Logout
          </button>
        </div>
      </div>

      {/* MAIN AREA */}
      <div className="flex-grow-1" style={{ background: pageBg, minWidth: 0 }}>
        {/* TOP BAR */}
        <div className="bg-white border-bottom d-flex justify-content-end align-items-center gap-3 px-4 py-3">
          <div className="text-end">
            <div className="fw-semibold small">{fullName}</div>
            <div className="text-secondary" style={{ fontSize: "12px" }}>
              {role}
            </div>
          </div>

          <div
            className="rounded-circle d-flex align-items-center justify-content-center fw-bold text-white border border-2"
            style={{
              width: "38px",
              height: "38px",
              background: "linear-gradient(135deg, #1E7A4D, #0B3B2E)",
            }}
          >
            {fullName.charAt(0).toUpperCase()}
          </div>
        </div>

        {/* PAGE CONTENT */}
        <div>{children}</div>
      </div>
    </div>
  );
}

export default Layout;