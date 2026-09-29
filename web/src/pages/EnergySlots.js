import { useCallback, useEffect, useMemo, useState } from "react";
import api from "../api";

/* ================================================================
   EnergySlots — Component 2: Energy Reservation & Slot Management
   Handles: slot listing, create, edit schedule, activate/deactivate
================================================================ */
function EnergySlots() {
  const [slots, setSlots] = useState([]);
  const [stations, setStations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  // Filters
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedStation, setSelectedStation] = useState("all");
  const [selectedDate, setSelectedDate] = useState("");
  const [selectedStatus, setSelectedStatus] = useState("all");
  const [viewMode, setViewMode] = useState("table"); // "table" | "schedule"

  // Create modal
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [createForm, setCreateForm] = useState({ stationId: "", date: "", startTime: "09:00", endTime: "12:00", capacity: 5 });
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState("");

  // Edit modal
  const [editingSlot, setEditingSlot] = useState(null);
  const [editForm, setEditForm] = useState({ date: "", startTime: "", endTime: "", status: "Available" });
  const [updating, setUpdating] = useState(false);
  const [updateError, setUpdateError] = useState("");

  // Quick status toggle
  const [togglingId, setTogglingId] = useState(null);

  const loadStations = useCallback(async () => {
    try {
      const res = await api.get("/microgridnodes");
      const list = Array.isArray(res.data) ? res.data : res.data?.data || res.data?.nodes || [];
      setStations(list);
    } catch (err) {
      console.error("Failed to load stations:", err);
    }
  }, []);

  const loadSlots = useCallback(async () => {
    try {
      setLoading(true);
      setError("");
      const params = {};
      if (selectedStation !== "all") params.stationId = selectedStation;
      if (selectedDate) params.date = selectedDate;
      const res = await api.get("/slots", { params });
      setSlots(Array.isArray(res.data) ? res.data : []);
    } catch (err) {
      console.error("Failed to load slots:", err);
      setError(
        err.response?.status === 401
          ? "Not authorized. Please log in again."
          : "Failed to load energy slots. Verify the API is running."
      );
    } finally {
      setLoading(false);
    }
  }, [selectedStation, selectedDate]);

  useEffect(() => { loadStations(); }, [loadStations]);
  useEffect(() => { loadSlots(); }, [loadSlots]);

  const stationMap = useMemo(() => {
    const m = {};
    stations.forEach((st) => {
      const id = st.nodeId || st.nodeID || st.NodeId || st.NodeID;
      const name = st.nodeName || st.name || st.NodeName || id;
      if (id) m[id] = name;
    });
    return m;
  }, [stations]);

  const filteredSlots = useMemo(
    () =>
      slots.filter((s) => {
        if (selectedStatus !== "all" && s.status !== selectedStatus) return false;
        if (searchQuery.trim()) {
          const q = searchQuery.toLowerCase();
          const sid = (s.slotId || "").toLowerCase();
          const stId = (s.stationId || "").toLowerCase();
          const stName = (stationMap[s.stationId] || "").toLowerCase();
          if (!sid.includes(q) && !stId.includes(q) && !stName.includes(q)) return false;
        }
        return true;
      }),
    [slots, selectedStatus, searchQuery, stationMap]
  );

  const stats = useMemo(
    () => ({
      total: slots.length,
      available: slots.filter((s) => s.status === "Available").length,
      totalCap: slots.reduce((a, s) => a + (s.capacity || 0), 0),
      totalAvail: slots.reduce((a, s) => a + (s.availability || 0), 0),
    }),
    [slots]
  );

  const showSuccess = (msg) => {
    setSuccessMessage(msg);
    setTimeout(() => setSuccessMessage(""), 4000);
  };

  const fmtDate = (d) => {
    if (!d) return "-";
    try { return new Date(d).toISOString().split("T")[0]; } catch { return d; }
  };

  const openCreateModal = () => {
    const firstActive = stations.find((s) => (s.status || "").toLowerCase() === "active");
    const defId =
      firstActive?.nodeId ||
      firstActive?.nodeID ||
      stations[0]?.nodeId ||
      stations[0]?.nodeID ||
      "";
    setCreateForm({
      stationId: defId,
      date: new Date().toISOString().split("T")[0],
      startTime: "09:00",
      endTime: "12:00",
      capacity: 5,
    });
    setCreateError("");
    setShowCreateModal(true);
  };

  const handleCreate = async (e) => {
    e.preventDefault();
    setCreateError("");
    if (!createForm.stationId) return setCreateError("Please select a microgrid station.");
    if (!createForm.date) return setCreateError("Date is required.");
    if (!createForm.startTime || !createForm.endTime) return setCreateError("Start and end time are required.");
    if (createForm.endTime <= createForm.startTime) return setCreateError("End time must be after start time.");
    if (!createForm.capacity || Number(createForm.capacity) <= 0) return setCreateError("Capacity must be greater than 0.");
    try {
      setCreating(true);
      await api.post("/slots", {
        stationId: createForm.stationId,
        date: createForm.date,
        startTime: createForm.startTime,
        endTime: createForm.endTime,
        capacity: Number(createForm.capacity),
      });
      setShowCreateModal(false);
      showSuccess("Energy booking slot created successfully!");
      await loadSlots();
    } catch (err) {
      setCreateError(
        err.response?.data?.message || err.response?.data?.title || "Failed to create slot. Check inputs."
      );
    } finally {
      setCreating(false);
    }
  };

  const openEditModal = (slot) => {
    setEditingSlot(slot);
    setEditForm({
      date: fmtDate(slot.date),
      startTime: slot.startTime || "09:00",
      endTime: slot.endTime || "12:00",
      status: slot.status || "Available",
    });
    setUpdateError("");
  };

  const handleUpdate = async (e) => {
    e.preventDefault();
    setUpdateError("");
    if (!editForm.startTime || !editForm.endTime) return setUpdateError("Start and end time are required.");
    if (editForm.endTime <= editForm.startTime) return setUpdateError("End time must be after start time.");
    try {
      setUpdating(true);
      const slotMongoId = editingSlot.id || editingSlot._id;
      await api.put(`/slots/${slotMongoId}`, {
        date: editForm.date || undefined,
        startTime: editForm.startTime,
        endTime: editForm.endTime,
        status: editForm.status,
      });
      setEditingSlot(null);
      showSuccess("Slot updated successfully!");
      await loadSlots();
    } catch (err) {
      setUpdateError(
        err.response?.data?.message || err.response?.data?.title || "Failed to update slot."
      );
    } finally {
      setUpdating(false);
    }
  };

  const handleToggle = async (slot) => {
    const slotMongoId = slot.id || slot._id;
    const newStatus = slot.status === "Available" ? "Unavailable" : "Available";
    try {
      setTogglingId(slotMongoId);
      await api.put(`/slots/${slotMongoId}`, { status: newStatus });
      showSuccess(`Slot ${slot.slotId} is now ${newStatus}.`);
      await loadSlots();
    } catch (err) {
      setError(err.response?.data?.message || "Failed to update slot status.");
    } finally {
      setTogglingId(null);
    }
  };

  return (
    <div className="container-fluid p-4 p-md-5">
      {/* Header */}
      <div className="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-3">
        <div>
          <h2 className="h3 mb-1">⚡ Energy Booking Slots</h2>
          <p className="text-secondary mb-0">
            Manage microgrid node operating windows, capacity allocations and availability schedules.
          </p>
        </div>
        <div className="d-flex gap-2 align-items-center">
          <div className="btn-group" role="group">
            <input type="radio" className="btn-check" name="btnradio" id="btnradio1" autoComplete="off" checked={viewMode === "table"} onChange={() => setViewMode("table")} />
            <label className="btn btn-outline-secondary" htmlFor="btnradio1">☰ Table</label>

            <input type="radio" className="btn-check" name="btnradio" id="btnradio2" autoComplete="off" checked={viewMode === "schedule"} onChange={() => setViewMode("schedule")} />
            <label className="btn btn-outline-secondary" htmlFor="btnradio2">📅 Schedule</label>
          </div>
          <button onClick={openCreateModal} className="btn btn-success fw-semibold">
            <i className="bi bi-plus-lg me-1"></i> Create Slot
          </button>
        </div>
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
          { label: "Total Slots", value: stats.total, sub: "Configured across nodes", bg: "bg-light", icon: "bi-calendar-range" },
          { label: "Available Slots", value: stats.available, sub: "Open for booking", bg: "bg-success-subtle", icon: "bi-calendar-check" },
          { label: "Total Capacity", value: stats.totalCap, sub: "Max booking units", bg: "bg-primary-subtle", icon: "bi-battery-charging" },
          { label: "Remaining Spaces", value: stats.totalAvail, sub: "Current availability", bg: "bg-warning-subtle", icon: "bi-battery-half" },
        ].map((card) => (
          <div key={card.label} className="col-12 col-md-6 col-lg-3">
            <SummaryCard icon={card.icon} label={card.label} value={card.value} bg={card.bg} />
          </div>
        ))}
      </div>

      {/* Filters */}
      <div className="card shadow-sm mb-4">
        <div className="card-body d-flex flex-wrap gap-3 align-items-center">
          <div className="flex-grow-1" style={{ minWidth: "200px" }}>
            <input
              type="text"
              className="form-control"
              placeholder="Search slot ID or station..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
          </div>
          <select
            className="form-select w-auto"
            value={selectedStation}
            onChange={(e) => setSelectedStation(e.target.value)}
          >
            <option value="all">All Stations</option>
            {stations.map((st) => {
              const id = st.nodeId || st.nodeID || st.NodeId || st.NodeID;
              const name = st.nodeName || st.name || st.NodeName || id;
              return <option key={id} value={id}>{name} ({id})</option>;
            })}
          </select>
          <input
            type="date"
            className="form-control w-auto"
            value={selectedDate}
            onChange={(e) => setSelectedDate(e.target.value)}
          />
          <select
            className="form-select w-auto"
            value={selectedStatus}
            onChange={(e) => setSelectedStatus(e.target.value)}
          >
            <option value="all">All Statuses</option>
            <option value="Available">Available</option>
            <option value="Unavailable">Unavailable</option>
          </select>
          {(searchQuery || selectedStation !== "all" || selectedDate || selectedStatus !== "all") && (
            <button
              type="button"
              className="btn btn-outline-secondary"
              onClick={() => { setSearchQuery(""); setSelectedStation("all"); setSelectedDate(""); setSelectedStatus("all"); }}
            >
              Clear Filters
            </button>
          )}
        </div>
      </div>

      {/* Loading State */}
      {loading && (
        <div className="text-center p-5 text-secondary">
          <div className="spinner-border mb-3" role="status"></div>
          <div className="fw-semibold">Loading Energy Slots…</div>
        </div>
      )}

      {/* Empty State */}
      {!loading && filteredSlots.length === 0 && (
        <div className="card shadow-sm p-5 text-center">
          <div className="fs-1 mb-3">⚡</div>
          <h4 className="h5 fw-bold mb-2">No energy booking slots found</h4>
          <p className="text-secondary mb-4">
            {slots.length === 0
              ? "No slots exist yet. Create the first slot to enable prosumer bookings."
              : "No slots match the current filters."}
          </p>
          <div>
            <button type="button" onClick={openCreateModal} className="btn btn-success fw-semibold">
              <i className="bi bi-plus-lg me-1"></i> Create New Slot
            </button>
          </div>
        </div>
      )}

      {/* Table View */}
      {!loading && filteredSlots.length > 0 && viewMode === "table" && (
        <div className="card shadow-sm">
          <div className="table-responsive">
            <table className="table table-hover align-middle mb-0">
              <thead className="table-light">
                <tr className="small text-uppercase text-secondary">
                  <th className="px-4 py-3">Slot ID</th>
                  <th className="px-4 py-3">Station / Node</th>
                  <th className="px-4 py-3">Date</th>
                  <th className="px-4 py-3">Time Window</th>
                  <th className="px-4 py-3">Availability</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3 text-end">Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredSlots.map((slot) => {
                  const mongoId = slot.id || slot._id;
                  const isAvail = slot.status === "Available";
                  const pct = slot.capacity > 0 ? Math.round((slot.availability / slot.capacity) * 100) : 0;
                  const barColorClass = pct === 0 ? "bg-danger" : pct <= 40 ? "bg-warning" : "bg-success";

                  return (
                    <tr key={mongoId || slot.slotId}>
                      <td className="px-4 py-3">
                        <span className="badge bg-light text-dark border font-monospace">
                          {slot.slotId}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <div className="fw-semibold">{stationMap[slot.stationId] || slot.stationId}</div>
                        <div className="small text-secondary font-monospace">{slot.stationId}</div>
                      </td>
                      <td className="px-4 py-3 fw-medium">📅 {fmtDate(slot.date)}</td>
                      <td className="px-4 py-3 fw-semibold">{slot.startTime} – {slot.endTime}</td>
                      <td className="px-4 py-3" style={{ minWidth: "150px" }}>
                        <div className="d-flex justify-content-between small fw-bold mb-1">
                          <span className={pct === 0 ? "text-danger" : pct <= 40 ? "text-warning" : "text-success"}>
                            {slot.availability} avail
                          </span>
                          <span className="text-secondary">/{slot.capacity}</span>
                        </div>
                        <div className="progress" style={{ height: "6px" }}>
                          <div className={`progress-bar ${barColorClass}`} style={{ width: `${pct}%` }}></div>
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <span className={`badge rounded-pill ${isAvail ? "text-bg-success" : "text-bg-danger"}`}>
                          {slot.status}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-end">
                        <button
                          className="btn btn-sm btn-outline-secondary me-2"
                          onClick={() => handleToggle(slot)}
                          disabled={togglingId === mongoId}
                        >
                          {togglingId === mongoId ? "..." : isAvail ? "Deactivate" : "Activate"}
                        </button>
                        <button
                          className="btn btn-sm btn-outline-primary"
                          onClick={() => openEditModal(slot)}
                        >
                          ✎ Edit
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Schedule View */}
      {!loading && filteredSlots.length > 0 && viewMode === "schedule" && (
        <div className="d-flex flex-column gap-4">
          {Object.entries(
            filteredSlots.reduce((acc, s) => {
              const dk = fmtDate(s.date);
              if (!acc[dk]) acc[dk] = [];
              acc[dk].push(s);
              return acc;
            }, {})
          )
            .sort()
            .map(([dateStr, daySlots]) => (
              <div key={dateStr} className="card shadow-sm">
                <div className="card-header bg-white d-flex justify-content-between align-items-center py-3">
                  <h5 className="mb-0 fw-bold text-success">📅 {dateStr}</h5>
                  <span className="badge bg-secondary rounded-pill">{daySlots.length} slot{daySlots.length !== 1 ? "s" : ""}</span>
                </div>
                <div className="card-body">
                  <div className="row g-3">
                    {daySlots.map((slot) => {
                      const isAvail = slot.status === "Available";
                      return (
                        <div key={slot.id || slot._id || slot.slotId} className="col-12 col-md-6 col-lg-4 col-xl-3">
                          <div className={`card h-100 ${isAvail ? "border-success-subtle" : "border-danger-subtle bg-light"}`}>
                            <div className="card-body d-flex flex-column gap-2">
                              <div className="d-flex justify-content-between align-items-center">
                                <span className="badge bg-light text-dark border font-monospace">{slot.slotId}</span>
                                <span className={`badge ${isAvail ? "text-bg-success" : "text-bg-danger"}`}>{slot.status}</span>
                              </div>
                              <div>
                                <div className="fs-5 fw-bold">{slot.startTime} – {slot.endTime}</div>
                                <div className="small text-secondary text-truncate">📍 {stationMap[slot.stationId] || slot.stationId}</div>
                              </div>
                              <div className="d-flex justify-content-between small fw-bold mt-2 pt-2 border-top border-dashed">
                                <span className={slot.availability > 0 ? "text-success" : "text-danger"}>{slot.availability} spaces avail</span>
                                <span className="text-secondary">Cap: {slot.capacity}</span>
                              </div>
                            </div>
                            <div className="card-footer bg-transparent d-flex gap-2 border-0 pt-0">
                              <button className="btn btn-sm btn-outline-primary flex-grow-1" onClick={() => openEditModal(slot)}>✎ Edit</button>
                              <button className="btn btn-sm btn-outline-secondary" onClick={() => handleToggle(slot)}>
                                {isAvail ? "Deactivate" : "Activate"}
                              </button>
                            </div>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </div>
              </div>
            ))}
        </div>
      )}

      {/* Create Modal */}
      {showCreateModal && (
        <>
          <div className="modal d-block" tabIndex="-1" role="dialog" style={{ background: "rgba(0,0,0,0.5)" }}>
            <div className="modal-dialog modal-dialog-centered">
              <form onSubmit={handleCreate} className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title fw-bold text-success">⚡ Create Booking Slot</h5>
                  <button type="button" className="btn-close" onClick={() => setShowCreateModal(false)}></button>
                </div>
                <div className="modal-body">
                  {createError && (
                    <div className="alert alert-danger py-2 small fw-semibold">⚠ {createError}</div>
                  )}
                  <div className="mb-3">
                    <label className="form-label small fw-bold">Microgrid Station *</label>
                    <select className="form-select" value={createForm.stationId} onChange={(e) => setCreateForm({ ...createForm, stationId: e.target.value })} required>
                      <option value="">Select a station...</option>
                      {stations.map((st) => {
                        const id = st.nodeId || st.nodeID || st.NodeId || st.NodeID;
                        const name = st.nodeName || st.name || st.NodeName || id;
                        const isActive = (st.status || "").toLowerCase() === "active";
                        return (
                          <option key={id} value={id} disabled={!isActive}>
                            {name} ({id}){!isActive ? " — Inactive" : ""}
                          </option>
                        );
                      })}
                    </select>
                    <div className="form-text">Only active stations are available for scheduling.</div>
                  </div>
                  <div className="mb-3">
                    <label className="form-label small fw-bold">Date *</label>
                    <input type="date" className="form-control" value={createForm.date} onChange={(e) => setCreateForm({ ...createForm, date: e.target.value })} required />
                  </div>
                  <div className="row g-3 mb-3">
                    <div className="col">
                      <label className="form-label small fw-bold">Start Time *</label>
                      <input type="time" className="form-control" value={createForm.startTime} onChange={(e) => setCreateForm({ ...createForm, startTime: e.target.value })} required />
                    </div>
                    <div className="col">
                      <label className="form-label small fw-bold">End Time *</label>
                      <input type="time" className="form-control" value={createForm.endTime} onChange={(e) => setCreateForm({ ...createForm, endTime: e.target.value })} required />
                    </div>
                  </div>
                  <div className="mb-3">
                    <label className="form-label small fw-bold">Capacity (Max Booking Units) *</label>
                    <input type="number" className="form-control" min="1" max="100" value={createForm.capacity} onChange={(e) => setCreateForm({ ...createForm, capacity: e.target.value })} required />
                    <div className="form-text">Availability will initially equal capacity.</div>
                  </div>
                </div>
                <div className="modal-footer border-top-0">
                  <button type="button" className="btn btn-outline-secondary" onClick={() => setShowCreateModal(false)}>Cancel</button>
                  <button type="submit" className="btn btn-success fw-semibold" disabled={creating}>
                    {creating ? "Creating..." : "Save Slot"}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </>
      )}

      {/* Edit Modal */}
      {editingSlot && (
        <>
          <div className="modal d-block" tabIndex="-1" role="dialog" style={{ background: "rgba(0,0,0,0.5)" }}>
            <div className="modal-dialog modal-dialog-centered">
              <form onSubmit={handleUpdate} className="modal-content">
                <div className="modal-header d-flex justify-content-between align-items-start">
                  <div>
                    <h5 className="modal-title fw-bold text-success mb-1">✎ Edit Slot Schedule</h5>
                    <div className="font-monospace small text-secondary">{editingSlot.slotId} · {editingSlot.stationId}</div>
                  </div>
                  <button type="button" className="btn-close" onClick={() => setEditingSlot(null)}></button>
                </div>
                <div className="modal-body">
                  <div className="alert alert-light border d-flex justify-content-between align-items-center mb-4">
                    <div>
                      <div className="small fw-bold text-secondary text-uppercase mb-1">Capacity / Availability</div>
                      <div className="fw-bold">{editingSlot.availability} of {editingSlot.capacity} spaces available</div>
                    </div>
                    <span className="badge bg-light text-secondary border">🔒 API Protected</span>
                  </div>

                  {updateError && (
                    <div className="alert alert-danger py-2 small fw-semibold">⚠ {updateError}</div>
                  )}

                  <div className="mb-3">
                    <label className="form-label small fw-bold">Date *</label>
                    <input type="date" className="form-control" value={editForm.date} onChange={(e) => setEditForm({ ...editForm, date: e.target.value })} required />
                  </div>
                  <div className="row g-3 mb-3">
                    <div className="col">
                      <label className="form-label small fw-bold">Start Time *</label>
                      <input type="time" className="form-control" value={editForm.startTime} onChange={(e) => setEditForm({ ...editForm, startTime: e.target.value })} required />
                    </div>
                    <div className="col">
                      <label className="form-label small fw-bold">End Time *</label>
                      <input type="time" className="form-control" value={editForm.endTime} onChange={(e) => setEditForm({ ...editForm, endTime: e.target.value })} required />
                    </div>
                  </div>
                  <div className="mb-3">
                    <label className="form-label small fw-bold">Status *</label>
                    <select className="form-select" value={editForm.status} onChange={(e) => setEditForm({ ...editForm, status: e.target.value })} required>
                      <option value="Available">Available (Open for booking)</option>
                      <option value="Unavailable">Unavailable (Disabled)</option>
                    </select>
                    <div className="form-text">Schedule changes require no active reservations on this slot.</div>
                  </div>
                </div>
                <div className="modal-footer border-top-0">
                  <button type="button" className="btn btn-outline-secondary" onClick={() => setEditingSlot(null)}>Cancel</button>
                  <button type="submit" className="btn btn-success fw-semibold" disabled={updating}>
                    {updating ? "Saving..." : "Update Slot"}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

export default EnergySlots;

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
