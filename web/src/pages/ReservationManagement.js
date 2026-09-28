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
      case "Approved": return "text-bg-success";
      case "Cancelled": return "text-bg-danger";
      case "Completed": return "text-bg-info";
      default: return "text-bg-warning"; // Pending
    }
  };

  // ================================================================
  return (
    <div className="container-fluid p-4 p-md-5">

      {/* HEADER */}
      <div className="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-3">
        <div>
          <h2 className="h3 mb-1">Reservation Management</h2>
          <p className="text-secondary mb-0">
            Monitor all prosumer energy booking reservations across all microgrid stations.
          </p>
        </div>
        <button onClick={loadReservations} className="btn btn-outline-secondary fw-semibold">
          <i className="bi bi-arrow-clockwise me-1"></i>
          Refresh
        </button>
      </div>

      {/* BANNERS */}
      {error && (
        <div className="alert alert-danger d-flex align-items-center justify-content-between mb-4 py-2" role="alert">
          <span>{error}</span>
          <button type="button" className="btn-close" onClick={() => setError("")}></button>
        </div>
      )}

      {/* KPI CARDS */}
      <div className="row g-3 mb-4">
        <div className="col-6 col-md-4 col-lg flex-grow-1">
          <SummaryCard 
            icon="bi-list-ul" 
            label="Total" 
            value={stats.total} 
            bg="bg-primary-subtle" 
            onClick={() => setSelectedStatus("all")} 
          />
        </div>
        <div className="col-6 col-md-4 col-lg flex-grow-1">
          <SummaryCard 
            icon="bi-hourglass-split" 
            label="Pending" 
            value={stats.pending} 
            bg="bg-warning-subtle" 
            onClick={() => setSelectedStatus("Pending")} 
          />
        </div>
        <div className="col-6 col-md-4 col-lg flex-grow-1">
          <SummaryCard 
            icon="bi-check-circle" 
            label="Approved" 
            value={stats.approved} 
            bg="bg-success-subtle" 
            onClick={() => setSelectedStatus("Approved")} 
          />
        </div>
        <div className="col-6 col-md-6 col-lg flex-grow-1">
          <SummaryCard 
            icon="bi-check-all" 
            label="Completed" 
            value={stats.completed} 
            bg="bg-info-subtle" 
            onClick={() => setSelectedStatus("Completed")} 
          />
        </div>
        <div className="col-12 col-md-6 col-lg flex-grow-1">
          <SummaryCard 
            icon="bi-slash-circle" 
            label="Cancelled" 
            value={stats.cancelled} 
            bg="bg-danger-subtle" 
            onClick={() => setSelectedStatus("Cancelled")} 
          />
        </div>
      </div>

      {/* FILTERS */}
      <div className="d-flex flex-wrap gap-2 mb-3">
        <div style={{ maxWidth: "320px", flexGrow: 1 }}>
          <input
            type="text"
            className="form-control"
            placeholder="Search reservation ID, NIC, station, slot..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
        </div>
        <div style={{ maxWidth: "200px" }}>
          <select className="form-select" value={selectedStatus} onChange={(e) => setSelectedStatus(e.target.value)}>
            <option value="all">All Statuses</option>
            <option value="Pending">Pending</option>
            <option value="Approved">Approved</option>
            <option value="Completed">Completed</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>
        {(searchQuery || selectedStatus !== "all") && (
          <button type="button" className="btn btn-light text-secondary" onClick={() => { setSearchQuery(""); setSelectedStatus("all"); }}>
            Clear Filters
          </button>
        )}
      </div>

      {/* TABLE */}
      <div className="card shadow-sm">
        <div className="table-responsive">
          <table className="table table-hover align-middle mb-0">
            <thead className="table-light">
              <tr className="small text-uppercase text-secondary">
                <th className="px-4 py-3 text-nowrap">Reservation ID</th>
                <th className="px-4 py-3 text-nowrap">Prosumer NIC</th>
                <th className="px-4 py-3 text-nowrap">Station</th>
                <th className="px-4 py-3 text-nowrap">Slot</th>
                <th className="px-4 py-3 text-nowrap">Status</th>
                <th className="px-4 py-3 text-nowrap">Created</th>
                <th className="px-4 py-3 text-nowrap">Updated</th>
                <th className="px-4 py-3 text-nowrap">Transaction Ref</th>
              </tr>
            </thead>
            <tbody>
              {loading && (
                <tr>
                  <td colSpan="8" className="text-center text-secondary py-4">
                    Loading Reservations...
                  </td>
                </tr>
              )}
              {!loading && filtered.length === 0 && (
                <tr>
                  <td colSpan="8" className="text-center text-secondary py-4">
                    {reservations.length === 0 ? "No reservations found." : "No reservations match the filter."}
                  </td>
                </tr>
              )}
              {!loading && filtered.map((r) => (
                <tr key={r.id || r.reservationId}>
                  <td className="px-4 py-3 text-nowrap fw-semibold">{r.reservationId}</td>
                  <td className="px-4 py-3">{r.prosumerNic}</td>
                  <td className="px-4 py-3">{r.stationId}</td>
                  <td className="px-4 py-3 text-secondary">{r.slotId}</td>
                  <td className="px-4 py-3">
                    <span className={`badge rounded-pill ${getStatusBadgeClass(r.status)}`}>
                      {r.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-secondary text-nowrap">{fmtDate(r.createdAt)}</td>
                  <td className="px-4 py-3 text-secondary text-nowrap">{fmtDate(r.updatedAt)}</td>
                  <td className="px-4 py-3 text-secondary">
                    {r.transactionReference || "—"}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

// One coloured statistic card in the summary row.
function SummaryCard({ icon, label, value, bg, onClick }) {
  return (
    <div 
      className={`rounded-3 p-3 d-flex align-items-center gap-3 h-100 ${bg}`}
      style={{ cursor: onClick ? "pointer" : "default" }}
      onClick={onClick}
    >
      <i className={`bi ${icon} fs-4`}></i>
      <div>
        <div className="fs-4 fw-bold lh-1">{value}</div>
        <div className="small text-secondary">{label}</div>
      </div>
    </div>
  );
}

export default ReservationManagement;
