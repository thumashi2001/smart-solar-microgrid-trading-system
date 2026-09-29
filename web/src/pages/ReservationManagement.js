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
  const [processingId, setProcessingId] = useState(null);

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

  const handleApprove = async (id) => {
    try {
      setProcessingId(id);
      setError("");
      await api.patch(`/reservations/${id}/approve`);
      showSuccess("Reservation approved successfully!");
      await loadReservations();
    } catch (err) {
      setError(err.response?.data?.message || "Failed to approve reservation.");
    } finally {
      setProcessingId(null);
    }
  };

  const showSuccess = (msg) => {
    setSuccessMessage(msg);
    setTimeout(() => setSuccessMessage(""), 4000);
  };

  const fmtDate = (d) => {
    if (!d) return "-";
    try { return new Date(d).toISOString().replace("T", " ").slice(0, 16) + " UTC"; } catch { return d; }
  };

  const statusBadge = (status) => {
    switch (status) {
      case "Approved": return "text-bg-success";
      case "Cancelled": return "text-bg-danger";
      case "Completed": return "text-bg-primary";
      default: return "text-bg-warning";
    }
  };

  const stats = useMemo(() => ({
    total: reservations.length,
    pending: reservations.filter((r) => r.status === "Pending").length,
    approved: reservations.filter((r) => r.status === "Approved").length,
    cancelled: reservations.filter((r) => r.status === "Cancelled").length,
    completed: reservations.filter((r) => r.status === "Completed").length,
  }), [reservations]);

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

  return (
    <div className="container-fluid p-4 p-md-5">
      {/* Header */}
      <div className="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-3">
        <div>
          <h2 className="h3 mb-1">📋 Reservation Management</h2>
          <p className="text-secondary mb-0">
            Monitor all prosumer energy booking reservations across all microgrid stations.
          </p>
        </div>
        <button onClick={loadReservations} className="btn btn-outline-secondary fw-semibold">
          <i className="bi bi-arrow-clockwise me-1"></i> Refresh
        </button>
      </div>

      {/* Alerts */}
      {successMessage && (
        <div className="alert alert-success fw-semibold" role="alert">
          ✓ {successMessage}
        </div>
      )}
      {error && (
        <div className="alert alert-danger d-flex justify-content-between fw-semibold" role="alert">
          <span>⚠ {error}</span>
          <button type="button" className="btn-close" onClick={() => setError("")}></button>
        </div>
      )}

      {/* KPI Cards */}
      <div className="row g-3 mb-4">
        {[
          { label: "Total", value: stats.total, bg: "bg-light", icon: "bi-list-ul" },
          { label: "Pending", value: stats.pending, bg: "bg-warning-subtle", icon: "bi-hourglass-split" },
          { label: "Approved", value: stats.approved, bg: "bg-success-subtle", icon: "bi-check-circle" },
          { label: "Completed", value: stats.completed, bg: "bg-primary-subtle", icon: "bi-flag" },
          { label: "Cancelled", value: stats.cancelled, bg: "bg-danger-subtle", icon: "bi-x-circle" },
        ].map((card) => (
          <div key={card.label} className="col-12 col-md" onClick={() => setSelectedStatus(card.label === "Total" ? "all" : card.label)} style={{ cursor: "pointer" }}>
            <SummaryCard icon={card.icon} label={card.label} value={card.value} bg={card.bg} />
          </div>
        ))}
      </div>

      {/* Filters */}
      <div className="card shadow-sm mb-4">
        <div className="card-body d-flex flex-wrap gap-3 align-items-center">
          <div className="flex-grow-1" style={{ minWidth: "240px" }}>
            <input
              type="text"
              className="form-control"
              placeholder="Search by reservation ID, prosumer NIC, station or slot…"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
          </div>
          <select
            className="form-select w-auto"
            value={selectedStatus}
            onChange={(e) => setSelectedStatus(e.target.value)}
          >
            <option value="all">All Statuses</option>
            <option value="Pending">Pending</option>
            <option value="Approved">Approved</option>
            <option value="Completed">Completed</option>
            <option value="Cancelled">Cancelled</option>
          </select>
          {(searchQuery || selectedStatus !== "all") && (
            <button
              type="button"
              className="btn btn-outline-secondary"
              onClick={() => { setSearchQuery(""); setSelectedStatus("all"); }}
            >
              Clear
            </button>
          )}
          <span className="small text-secondary ms-auto">
            {filtered.length} of {reservations.length} reservations
          </span>
        </div>
      </div>

      {/* Loading State */}
      {loading && (
        <div className="text-center p-5 text-secondary">
          <div className="spinner-border mb-3" role="status"></div>
          <div className="fw-semibold">Loading Reservations…</div>
        </div>
      )}

      {/* Empty State */}
      {!loading && filtered.length === 0 && (
        <div className="card shadow-sm p-5 text-center">
          <div className="fs-1 mb-3">📋</div>
          <h4 className="h5 fw-bold mb-2">
            {reservations.length === 0 ? "No reservations yet" : "No reservations match the filter"}
          </h4>
          <p className="text-secondary mb-0">
            {reservations.length === 0
              ? "Prosumers have not made any reservations yet."
              : "Try adjusting your search or filter."}
          </p>
        </div>
      )}

      {/* Table */}
      {!loading && filtered.length > 0 && (
        <div className="card shadow-sm">
          <div className="table-responsive">
            <table className="table table-sm table-hover align-middle mb-0">
              <thead className="table-light">
                <tr className="small text-uppercase text-secondary">
                  <th className="px-2 py-2">Reservation ID</th>
                  <th className="px-2 py-2">Prosumer NIC</th>
                  <th className="px-2 py-2">Station</th>
                  <th className="px-2 py-2">Slot</th>
                  <th className="px-2 py-2">Status</th>
                  <th className="px-2 py-2">Created</th>
                  <th className="px-2 py-2">Updated</th>
                  <th className="px-2 py-2">Transaction Ref</th>
                  <th className="px-2 py-2 text-end text-nowrap">Actions</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((r) => {
                  const mongoId = r.id || r._id;
                  return (
                    <tr key={mongoId || r.reservationId}>
                      <td className="px-2 py-2">
                        <span className="badge bg-light text-dark border font-monospace">
                          {r.reservationId}
                        </span>
                      </td>
                      <td className="px-2 py-2 font-monospace small">{r.prosumerNic}</td>
                      <td className="px-2 py-2 fw-semibold text-primary">{r.stationId}</td>
                      <td className="px-2 py-2 font-monospace small">{r.slotId}</td>
                      <td className="px-2 py-2">
                        <span className={`badge rounded-pill ${statusBadge(r.status)}`}>
                          {r.status}
                        </span>
                      </td>
                      <td className="px-2 py-2 small text-secondary">{fmtDate(r.createdAt)}</td>
                      <td className="px-2 py-2 small text-secondary">{fmtDate(r.updatedAt)}</td>
                      <td 
                        className="px-2 py-2 small font-monospace text-secondary text-truncate" 
                        style={{ maxWidth: "120px" }} 
                        title={r.transactionReference}
                      >
                        {r.transactionReference || "—"}
                      </td>
                      <td className="px-2 py-2 text-end text-nowrap">
                        {r.status === "Pending" && (
                          <button
                            className="btn btn-sm btn-success fw-semibold"
                            onClick={() => handleApprove(mongoId)}
                            disabled={processingId === mongoId}
                          >
                            {processingId === mongoId ? "Approving..." : "Approve"}
                          </button>
                        )}
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

// One coloured statistic card in the summary row.
function SummaryCard({ icon, label, value, bg }) {
  return (
    <div className={`rounded-3 p-3 d-flex align-items-center gap-3 h-100 ${bg}`}>
      <i className={`bi ${icon} fs-4`}></i>
      <div>
        <div className="fs-4 fw-bold lh-1">{value}</div>
        <div className="small text-secondary">{label}</div>
      </div>
    </div>
  );
}
