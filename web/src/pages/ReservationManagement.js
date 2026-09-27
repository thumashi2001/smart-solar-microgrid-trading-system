import { useCallback, useEffect, useMemo, useState } from "react";
import api from "../api";

/* ================================================================
   ReservationManagement — Component 2
   Backoffice view: monitors all reservations via GET /api/reservations
   Displays: reservationId, prosumerNic, stationId, slotId, status,
             createdAt, updatedAt, transactionReference
   Does NOT aggregate data (that belongs to Nethasa's dashboard).
================================================================ */
function ReservationManagement() {
  const [reservations, setReservations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedStatus, setSelectedStatus] = useState("all");
  const [successMessage, setSuccessMessage] = useState("");

  const loadReservations = useCallback(async () => {
    try {
      setLoading(true);
      setError("");
      const res = await api.get("/reservations");
      setReservations(Array.isArray(res.data) ? res.data : []);
    } catch (err) {
      console.error("Failed to load reservations:", err);
      setError(
        err.response?.status === 401
          ? "Not authorized. Please log in again."
          : "Failed to load reservations. Verify the API is running."
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { loadReservations(); }, [loadReservations]);

  // ── Stats ──────────────────────────────────────────────────────
  const stats = useMemo(() => ({
    total: reservations.length,
    pending: reservations.filter((r) => r.status === "Pending").length,
    approved: reservations.filter((r) => r.status === "Approved").length,
    cancelled: reservations.filter((r) => r.status === "Cancelled").length,
    completed: reservations.filter((r) => r.status === "Completed").length,
  }), [reservations]);

  // ── Filter ─────────────────────────────────────────────────────
  const filtered = useMemo(() => reservations.filter((r) => {
    if (selectedStatus !== "all" && r.status !== selectedStatus) return false;
    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase();
      return (
        (r.reservationId || "").toLowerCase().includes(q) ||
        (r.prosumerNic || "").toLowerCase().includes(q) ||
        (r.stationId || "").toLowerCase().includes(q) ||
        (r.slotId || "").toLowerCase().includes(q)
      );
    }
    return true;
  }), [reservations, selectedStatus, searchQuery]);

  // ── Helpers ────────────────────────────────────────────────────
  const showSuccess = (msg) => {
    setSuccessMessage(msg);
    setTimeout(() => setSuccessMessage(""), 4000);
  };

  const fmtDate = (d) => {
    if (!d) return "-";
    try { return new Date(d).toISOString().replace("T", " ").slice(0, 16) + " UTC"; } catch { return d; }
  };

  const statusStyle = (status) => {
    switch (status) {
      case "Approved": return { bg: "#E6F4EA", color: "#137333" };
      case "Cancelled": return { bg: "#FCE8E6", color: "#C5221F" };
      case "Completed": return { bg: "#E8F0FE", color: "#1A73E8" };
      default: return { bg: "#FEF7E0", color: "#B06000" }; // Pending
    }
  };

  // ── Design tokens ──────────────────────────────────────────────
  const c = {
    primary: "#0B3B2E", accent: "#1E7A4D", bg: "#F7F5F1",
    card: "#fff", border: "#E4E1DA", textMain: "#1C1F1E", textMuted: "#6B6862",
    green: "#137333", red: "#C5221F",
  };
  const inputStyle = {
    width: "100%", padding: "10px 12px", borderRadius: "8px",
    border: `1px solid ${c.border}`, fontSize: "14px", outline: "none",
    boxSizing: "border-box", background: "#fff", color: c.textMain,
  };

  // ================================================================
  return (
    <div style={{ padding: "32px", maxWidth: "1400px", margin: "0 auto", fontFamily: "'Segoe UI', Arial, sans-serif" }}>

      {/* HEADER */}
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: "24px", flexWrap: "wrap", gap: "16px" }}>
        <div>
          <h1 style={{ fontSize: "26px", fontWeight: 700, color: c.textMain, margin: "0 0 6px 0" }}>
            📋 Reservation Management
          </h1>
          <p style={{ margin: 0, color: c.textMuted, fontSize: "14px" }}>
            Monitor all prosumer energy booking reservations across all microgrid stations.
          </p>
        </div>
        <button
          type="button"
          onClick={loadReservations}
          style={{ padding: "10px 18px", borderRadius: "8px", border: `1px solid ${c.border}`, background: "#fff", color: c.textMain, fontSize: "14px", fontWeight: 600, cursor: "pointer" }}
        >
          ↻ Refresh
        </button>
      </div>

      {/* BANNERS */}
      {successMessage && (
        <div style={{ background: "#E6F4EA", border: "1px solid #CEEAD6", color: c.green, padding: "12px 18px", borderRadius: "8px", marginBottom: "20px", fontSize: "14px", fontWeight: 500 }}>
          ✓ {successMessage}
        </div>
      )}
      {error && (
        <div style={{ background: "#FCE8E6", border: "1px solid #FAD2CF", color: c.red, padding: "12px 18px", borderRadius: "8px", marginBottom: "20px", fontSize: "14px", fontWeight: 500, display: "flex", justifyContent: "space-between" }}>
          <span>⚠ {error}</span>
          <button type="button" onClick={() => setError("")} style={{ background: "none", border: "none", color: c.red, cursor: "pointer", fontWeight: 700 }}>✕</button>
        </div>
      )}

      {/* KPI CARDS */}
      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(160px, 1fr))", gap: "16px", marginBottom: "24px" }}>
        {[
          { label: "Total", value: stats.total, bg: "#F7F5F1", color: c.textMain },
          { label: "Pending", value: stats.pending, bg: "#FEF7E0", color: "#B06000" },
          { label: "Approved", value: stats.approved, bg: "#E6F4EA", color: c.green },
          { label: "Completed", value: stats.completed, bg: "#E8F0FE", color: "#1A73E8" },
          { label: "Cancelled", value: stats.cancelled, bg: "#FCE8E6", color: c.red },
        ].map((card) => (
          <div key={card.label} style={{ background: card.bg, borderRadius: "12px", padding: "16px", border: `1px solid ${c.border}`, cursor: "pointer" }}
            onClick={() => setSelectedStatus(card.label === "Total" ? "all" : card.label)}>
            <div style={{ fontSize: "11px", fontWeight: 700, textTransform: "uppercase", color: card.color, letterSpacing: "0.5px" }}>{card.label}</div>
            <div style={{ fontSize: "28px", fontWeight: 700, color: card.color, margin: "4px 0" }}>{card.value}</div>
          </div>
        ))}
      </div>

      {/* FILTERS */}
      <div style={{ background: c.card, borderRadius: "12px", padding: "16px 20px", border: `1px solid ${c.border}`, marginBottom: "24px", display: "flex", flexWrap: "wrap", gap: "12px", alignItems: "center" }}>
        <div style={{ position: "relative", minWidth: "240px", flexGrow: 1 }}>
          <span style={{ position: "absolute", left: "12px", top: "50%", transform: "translateY(-50%)", color: "#9E9A90", fontSize: "14px" }}>🔍</span>
          <input
            type="text"
            placeholder="Search by reservation ID, prosumer NIC, station or slot…"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            style={{ ...inputStyle, paddingLeft: "34px" }}
          />
        </div>
        <select value={selectedStatus} onChange={(e) => setSelectedStatus(e.target.value)} style={{ ...inputStyle, width: "auto" }}>
          <option value="all">All Statuses</option>
          <option value="Pending">Pending</option>
          <option value="Approved">Approved</option>
          <option value="Completed">Completed</option>
          <option value="Cancelled">Cancelled</option>
        </select>
        {(searchQuery || selectedStatus !== "all") && (
          <button type="button" onClick={() => { setSearchQuery(""); setSelectedStatus("all"); }}
            style={{ background: "#F0ECE1", border: "none", color: c.textMuted, padding: "9px 14px", borderRadius: "8px", fontSize: "13px", cursor: "pointer" }}>
            Clear
          </button>
        )}
        <span style={{ fontSize: "12px", color: c.textMuted, marginLeft: "auto" }}>
          {filtered.length} of {reservations.length} reservations
        </span>
      </div>

      {/* LOADING */}
      {loading && (
        <div style={{ background: c.card, borderRadius: "12px", padding: "48px", textAlign: "center", border: `1px solid ${c.border}`, color: c.textMuted }}>
          <div style={{ fontSize: "28px", marginBottom: "8px" }}>⏳</div>
          <div style={{ fontSize: "16px", fontWeight: 600 }}>Loading Reservations…</div>
        </div>
      )}

      {/* EMPTY */}
      {!loading && filtered.length === 0 && (
        <div style={{ background: c.card, borderRadius: "12px", padding: "48px 24px", textAlign: "center", border: `1px solid ${c.border}` }}>
          <div style={{ fontSize: "36px", marginBottom: "12px" }}>📋</div>
          <h3 style={{ fontSize: "18px", fontWeight: 700, color: c.textMain, margin: "0 0 6px 0" }}>
            {reservations.length === 0 ? "No reservations yet" : "No reservations match the filter"}
          </h3>
          <p style={{ color: c.textMuted, fontSize: "14px", margin: 0 }}>
            {reservations.length === 0
              ? "Prosumers have not made any reservations yet."
              : "Try adjusting your search or filter."}
          </p>
        </div>
      )}

      {/* TABLE */}
      {!loading && filtered.length > 0 && (
        <div style={{ background: c.card, borderRadius: "12px", border: `1px solid ${c.border}`, overflow: "hidden", boxShadow: "0 1px 3px rgba(0,0,0,0.04)" }}>
          <div style={{ overflowX: "auto" }}>
            <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "13px", textAlign: "left" }}>
              <thead>
                <tr style={{ background: "#F7F5F1", borderBottom: `1px solid ${c.border}`, color: c.textMuted, fontSize: "11px", textTransform: "uppercase", fontWeight: 700 }}>
                  {["Reservation ID", "Prosumer NIC", "Station", "Slot", "Status", "Created", "Updated", "Transaction Ref"].map((h) => (
                    <th key={h} style={{ padding: "13px 14px", whiteSpace: "nowrap" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {filtered.map((r, i) => {
                  const ss = statusStyle(r.status);
                  return (
                    <tr
                      key={r.id || r.reservationId || i}
                      style={{ borderBottom: i === filtered.length - 1 ? "none" : `1px solid #F0ECE1` }}
                      onMouseEnter={(e) => (e.currentTarget.style.background = "#FAF8F5")}
                      onMouseLeave={(e) => (e.currentTarget.style.background = "#fff")}
                    >
                      <td style={{ padding: "14px" }}>
                        <span style={{ background: "#EBE8E1", padding: "3px 7px", borderRadius: "5px", fontFamily: "monospace", fontSize: "11px", fontWeight: 700, color: c.primary }}>
                          {r.reservationId}
                        </span>
                      </td>
                      <td style={{ padding: "14px", fontFamily: "monospace", fontSize: "12px", color: c.textMain }}>{r.prosumerNic}</td>
                      <td style={{ padding: "14px", fontWeight: 600, color: c.primary }}>{r.stationId}</td>
                      <td style={{ padding: "14px", fontFamily: "monospace", fontSize: "12px" }}>{r.slotId}</td>
                      <td style={{ padding: "14px" }}>
                        <span style={{ display: "inline-flex", alignItems: "center", gap: "5px", padding: "3px 9px", borderRadius: "10px", fontSize: "11px", fontWeight: 700, background: ss.bg, color: ss.color }}>
                          <span style={{ width: "5px", height: "5px", borderRadius: "50%", background: ss.color }} />
                          {r.status}
                        </span>
                      </td>
                      <td style={{ padding: "14px", fontSize: "11px", color: c.textMuted, whiteSpace: "nowrap" }}>{fmtDate(r.createdAt)}</td>
                      <td style={{ padding: "14px", fontSize: "11px", color: c.textMuted, whiteSpace: "nowrap" }}>{fmtDate(r.updatedAt)}</td>
                      <td style={{ padding: "14px", fontSize: "11px", color: c.textMuted, fontFamily: "monospace" }}>
                        {r.transactionReference || <span style={{ color: "#CCCCCC" }}>—</span>}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}

export default ReservationManagement;
