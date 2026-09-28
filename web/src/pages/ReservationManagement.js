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
  // const [successMessage, setSuccessMessage] = useState(""); // Removed since unused

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
  const fmtDate = (d) => {
    if (!d) return "-";
    try { return new Date(d).toISOString().replace("T", " ").slice(0, 16) + " UTC"; } catch { return d; }
  };

  const getStatusBadgeClass = (status) => {
    switch (status) {
      case "Approved": return "bg-success bg-opacity-10 text-success border-success";
      case "Cancelled": return "bg-danger bg-opacity-10 text-danger border-danger";
      case "Completed": return "bg-primary bg-opacity-10 text-primary border-primary";
      default: return "bg-warning bg-opacity-10 text-warning border-warning"; // Pending
    }
  };

  const getStatusDotColor = (status) => {
    switch (status) {
      case "Approved": return "#198754"; // Bootstrap success
      case "Cancelled": return "#dc3545"; // Bootstrap danger
      case "Completed": return "#0d6efd"; // Bootstrap primary
      default: return "#ffc107"; // Bootstrap warning
    }
  };

  // ── Design tokens (Brand Colors) ───────────────────────────────
  const brand = {
    dark: "#0B3B2E",
    green: "#1E7A4D",
    sun: "#FFD54F",
    bg: "#F7F5F1",
  };

  // ================================================================
  return (
    <div className="container-fluid py-4" style={{ backgroundColor: brand.bg, minHeight: "100vh" }}>

      {/* HEADER */}
      <div className="row mb-4 align-items-start">
        <div className="col-12 col-md-8 mb-3 mb-md-0">
          <h1 className="h3 fw-bold" style={{ color: brand.dark }}>
            📋 Reservation Management
          </h1>
          <p className="text-muted mb-0">
            Monitor all prosumer energy booking reservations across all microgrid stations.
          </p>
        </div>
        <div className="col-12 col-md-4 text-md-end">
          <button
            type="button"
            className="btn btn-light shadow-sm fw-semibold"
            onClick={loadReservations}
          >
            ↻ Refresh
          </button>
        </div>
      </div>

      {/* BANNERS */}
      {error && (
        <div className="alert alert-danger d-flex align-items-center justify-content-between mb-4 py-2 shadow-sm" role="alert">
          <span><span className="fw-bold me-2">⚠</span> {error}</span>
          <button type="button" className="btn-close" onClick={() => setError("")} aria-label="Close"></button>
        </div>
      )}

      {/* KPI CARDS */}
      <div className="row g-3 mb-4">
        {[
          { label: "Total", value: stats.total, style: { backgroundColor: "#fff", color: "#1C1F1E" } },
          { label: "Pending", value: stats.pending, style: { backgroundColor: "#FEF7E0", color: "#B06000" } },
          { label: "Approved", value: stats.approved, style: { backgroundColor: "#E6F4EA", color: "#137333" } },
          { label: "Completed", value: stats.completed, style: { backgroundColor: "#E8F0FE", color: "#1A73E8" } },
          { label: "Cancelled", value: stats.cancelled, style: { backgroundColor: "#FCE8E6", color: "#C5221F" } },
        ].map((card) => (
          <div key={card.label} className="col-6 col-md-4 col-lg flex-grow-1">
            <div 
              className="card h-100 shadow-sm border-0" 
              style={{ backgroundColor: card.style.backgroundColor, cursor: "pointer" }}
              onClick={() => setSelectedStatus(card.label === "Total" ? "all" : card.label)}
            >
              <div className="card-body">
                <div className="text-uppercase fw-bold mb-1" style={{ fontSize: "0.7rem", color: card.style.color, letterSpacing: "0.5px" }}>{card.label}</div>
                <div className="display-6 fw-bold m-0" style={{ color: card.style.color }}>{card.value}</div>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* FILTERS */}
      <div className="card shadow-sm border-0 mb-4">
        <div className="card-body">
          <div className="row g-2 align-items-center">
            <div className="col-12 col-md-5 col-lg-4 position-relative">
              <span className="position-absolute top-50 translate-middle-y text-muted" style={{ left: "20px" }}>🔍</span>
              <input
                type="text"
                className="form-control"
                placeholder="Search by reservation ID, prosumer NIC, station or slot…"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                style={{ paddingLeft: "35px" }}
              />
            </div>
            <div className="col-12 col-md-4 col-lg-3">
              <select className="form-select" value={selectedStatus} onChange={(e) => setSelectedStatus(e.target.value)}>
                <option value="all">All Statuses</option>
                <option value="Pending">Pending</option>
                <option value="Approved">Approved</option>
                <option value="Completed">Completed</option>
                <option value="Cancelled">Cancelled</option>
              </select>
            </div>
            {(searchQuery || selectedStatus !== "all") && (
              <div className="col-12 col-md-3 col-lg-2">
                <button type="button" className="btn btn-light w-100 fw-semibold" onClick={() => { setSearchQuery(""); setSelectedStatus("all"); }}>
                  Clear Filters
                </button>
              </div>
            )}
            <div className="col-12 col-lg-3 ms-auto text-lg-end text-muted small fw-semibold">
              {filtered.length} of {reservations.length} reservations
            </div>
          </div>
        </div>
      </div>

      {/* LOADING */}
      {loading && (
        <div className="card shadow-sm border-0 text-center text-muted p-5">
          <div className="fs-1 mb-2">⏳</div>
          <div className="fw-bold">Loading Reservations…</div>
        </div>
      )}

      {/* EMPTY */}
      {!loading && filtered.length === 0 && (
        <div className="card shadow-sm border-0 text-center p-5">
          <div className="fs-1 mb-3">📋</div>
          <h3 className="h5 fw-bold" style={{ color: brand.dark }}>
            {reservations.length === 0 ? "No reservations yet" : "No reservations match the filter"}
          </h3>
          <p className="text-muted mb-0">
            {reservations.length === 0
              ? "Prosumers have not made any reservations yet."
              : "Try adjusting your search or filter."}
          </p>
        </div>
      )}

      {/* TABLE */}
      {!loading && filtered.length > 0 && (
        <div className="card shadow-sm border-0 overflow-hidden">
          <div className="table-responsive">
            <table className="table table-hover align-middle mb-0">
              <thead className="table-light text-muted text-uppercase" style={{ fontSize: "0.7rem", letterSpacing: "0.5px" }}>
                <tr>
                  <th className="py-3 px-4 text-nowrap">Reservation ID</th>
                  <th className="py-3 px-4 text-nowrap">Prosumer NIC</th>
                  <th className="py-3 px-4 text-nowrap">Station</th>
                  <th className="py-3 px-4 text-nowrap">Slot</th>
                  <th className="py-3 px-4 text-nowrap">Status</th>
                  <th className="py-3 px-4 text-nowrap">Created</th>
                  <th className="py-3 px-4 text-nowrap">Updated</th>
                  <th className="py-3 px-4 text-nowrap">Transaction Ref</th>
                </tr>
              </thead>
              <tbody className="border-top-0">
                {filtered.map((r, i) => {
                  return (
                    <tr key={r.id || r.reservationId || i}>
                      <td className="px-4 py-3">
                        <span className="badge bg-light text-dark font-monospace py-2 px-2 border" style={{ color: brand.dark }}>
                          {r.reservationId}
                        </span>
                      </td>
                      <td className="px-4 py-3 font-monospace" style={{ fontSize: "0.85rem", color: "#1C1F1E" }}>{r.prosumerNic}</td>
                      <td className="px-4 py-3 fw-bold" style={{ color: brand.dark }}>{r.stationId}</td>
                      <td className="px-4 py-3 font-monospace text-muted" style={{ fontSize: "0.85rem" }}>{r.slotId}</td>
                      <td className="px-4 py-3">
                        <span className={`badge rounded-pill ${getStatusBadgeClass(r.status)} px-3 py-2 border border-opacity-25`}>
                          <span className="d-inline-block rounded-circle me-2" style={{ width: "6px", height: "6px", backgroundColor: getStatusDotColor(r.status) }}></span>
                          {r.status}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-muted text-nowrap" style={{ fontSize: "0.8rem" }}>{fmtDate(r.createdAt)}</td>
                      <td className="px-4 py-3 text-muted text-nowrap" style={{ fontSize: "0.8rem" }}>{fmtDate(r.updatedAt)}</td>
                      <td className="px-4 py-3 text-muted font-monospace" style={{ fontSize: "0.8rem" }}>
                        {r.transactionReference || <span className="text-secondary">—</span>}
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
