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

  // ----------------------------------------------------------------
  // Load Stations (for dropdowns)
  // ----------------------------------------------------------------
  const loadStations = useCallback(async () => {
    try {
      const res = await api.get("/microgridnodes");
      const list = Array.isArray(res.data) ? res.data : res.data?.data || res.data?.nodes || [];
      setStations(list);
    } catch (err) {
      console.error("Failed to load stations:", err);
    }
  }, []);

  // ----------------------------------------------------------------
  // Load Slots (with optional server-side station + date filter)
  // ----------------------------------------------------------------
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

  // ----------------------------------------------------------------
  // Derived data
  // ----------------------------------------------------------------
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

  // ----------------------------------------------------------------
  // Helpers
  // ----------------------------------------------------------------
  const showSuccess = (msg) => {
    setSuccessMessage(msg);
    setTimeout(() => setSuccessMessage(""), 4000);
  };

  const fmtDate = (d) => {
    if (!d) return "-";
    try { return new Date(d).toISOString().split("T")[0]; } catch { return d; }
  };

  // ----------------------------------------------------------------
  // Create slot
  // ----------------------------------------------------------------
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

  // ----------------------------------------------------------------
  // Edit slot
  // ----------------------------------------------------------------
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

  // ----------------------------------------------------------------
  // Toggle status
  // ----------------------------------------------------------------
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

  // ----------------------------------------------------------------
  // Design tokens (Brand Colors)
  // ----------------------------------------------------------------
  const brand = {
    dark: "#0B3B2E",
    green: "#1E7A4D",
    sun: "#FFD54F",
    bg: "#F7F5F1",
  };

  // ================================================================
  // RENDER
  // ================================================================
  return (
    <div className="container-fluid py-4" style={{ backgroundColor: brand.bg, minHeight: "100vh" }}>
      {/* HEADER */}
      <div className="row mb-4 align-items-start">
        <div className="col-12 col-md-8 mb-3 mb-md-0">
          <h1 className="h3 fw-bold" style={{ color: brand.dark }}>
            ⚡ Energy Booking Slots
          </h1>
          <p className="text-muted mb-0">
            Manage microgrid node operating windows, capacity allocations and availability schedules.
          </p>
        </div>
        <div className="col-12 col-md-4 d-flex gap-2 justify-content-md-end align-items-center">
          {/* View toggle */}
          <div className="btn-group shadow-sm bg-white rounded" role="group">
            <button
              type="button"
              className={`btn btn-sm ${viewMode === "table" ? "btn-light fw-bold" : "btn-link text-decoration-none text-muted"}`}
              onClick={() => setViewMode("table")}
              style={viewMode === "table" ? { color: brand.dark, borderColor: "#E4E1DA" } : {}}
            >
              ☰ Table
            </button>
            <button
              type="button"
              className={`btn btn-sm ${viewMode === "schedule" ? "btn-light fw-bold" : "btn-link text-decoration-none text-muted"}`}
              onClick={() => setViewMode("schedule")}
              style={viewMode === "schedule" ? { color: brand.dark, borderColor: "#E4E1DA" } : {}}
            >
              📅 Schedule
            </button>
          </div>
          <button
            type="button"
            onClick={openCreateModal}
            className="btn text-white shadow-sm d-flex align-items-center gap-2"
            style={{ backgroundColor: brand.green }}
          >
            <span>+</span> Create Slot
          </button>
        </div>
      </div>

      {/* BANNERS */}
      {successMessage && (
        <div className="alert alert-success d-flex align-items-center mb-4 py-2" role="alert">
          <span className="me-2 fw-bold">✓</span> {successMessage}
        </div>
      )}
      {error && (
        <div className="alert alert-danger d-flex align-items-center justify-content-between mb-4 py-2" role="alert">
          <span><span className="fw-bold me-2">⚠</span> {error}</span>
          <button type="button" className="btn-close" onClick={() => setError("")} aria-label="Close"></button>
        </div>
      )}

      {/* KPI CARDS */}
      <div className="row g-3 mb-4">
        {[
          { label: "Total Slots", value: stats.total, color: brand.dark, sub: "Configured across nodes" },
          { label: "Available Slots", value: stats.available, color: brand.green, sub: "Open for booking" },
          { label: "Total Capacity", value: stats.totalCap, color: "#1A73E8", sub: "Max booking units" },
          { label: "Remaining Spaces", value: stats.totalAvail, color: "#D93025", sub: "Current availability" },
        ].map((card) => (
          <div key={card.label} className="col-12 col-sm-6 col-lg-3">
            <div className="card h-100 shadow-sm border-0">
              <div className="card-body">
                <div className="text-uppercase fw-bold mb-1" style={{ fontSize: "0.75rem", color: card.color, letterSpacing: "0.5px" }}>{card.label}</div>
                <div className="display-6 fw-bold mb-1" style={{ color: card.color }}>{card.value}</div>
                <div className="text-muted small">{card.sub}</div>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* FILTERS */}
      <div className="card shadow-sm border-0 mb-4">
        <div className="card-body">
          <div className="row g-2 align-items-center">
            <div className="col-12 col-md-4 col-lg-3 position-relative">
              <span className="position-absolute top-50 translate-middle-y text-muted" style={{ left: "20px" }}>🔍</span>
              <input 
                type="text" 
                className="form-control" 
                placeholder="Search slot ID or station..." 
                value={searchQuery} 
                onChange={(e) => setSearchQuery(e.target.value)} 
                style={{ paddingLeft: "35px" }} 
              />
            </div>
            <div className="col-12 col-md-3 col-lg-3">
              <select className="form-select" value={selectedStation} onChange={(e) => setSelectedStation(e.target.value)}>
                <option value="all">All Stations</option>
                {stations.map((st) => {
                  const id = st.nodeId || st.nodeID || st.NodeId || st.NodeID;
                  const name = st.nodeName || st.name || st.NodeName || id;
                  return <option key={id} value={id}>{name} ({id})</option>;
                })}
              </select>
            </div>
            <div className="col-12 col-md-3 col-lg-2">
              <input type="date" className="form-control" value={selectedDate} onChange={(e) => setSelectedDate(e.target.value)} />
            </div>
            <div className="col-12 col-md-2 col-lg-2">
              <select className="form-select" value={selectedStatus} onChange={(e) => setSelectedStatus(e.target.value)}>
                <option value="all">All Statuses</option>
                <option value="Available">Available</option>
                <option value="Unavailable">Unavailable</option>
              </select>
            </div>
            {(searchQuery || selectedStation !== "all" || selectedDate || selectedStatus !== "all") && (
              <div className="col-12 col-lg-2 text-end text-lg-start">
                <button type="button" className="btn btn-light w-100" onClick={() => { setSearchQuery(""); setSelectedStation("all"); setSelectedDate(""); setSelectedStatus("all"); }}>
                  Clear Filters
                </button>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* LOADING */}
      {loading && (
        <div className="card shadow-sm border-0 text-center text-muted p-5">
          <div className="fs-1 mb-2">⏳</div>
          <div className="fw-bold">Loading Energy Slots...</div>
        </div>
      )}

      {/* EMPTY STATE */}
      {!loading && filteredSlots.length === 0 && (
        <div className="card shadow-sm border-0 text-center p-5">
          <div className="fs-1 mb-3">⚡</div>
          <h3 className="h5 fw-bold" style={{ color: brand.dark }}>No energy booking slots found</h3>
          <p className="text-muted mb-4">
            {slots.length === 0
              ? "No slots exist yet. Create the first slot to enable prosumer bookings."
              : "No slots match the current filters."}
          </p>
          <div>
            <button type="button" className="btn text-white px-4" onClick={openCreateModal} style={{ backgroundColor: brand.green }}>+ Create New Slot</button>
          </div>
        </div>
      )}

      {/* TABLE VIEW */}
      {!loading && filteredSlots.length > 0 && viewMode === "table" && (
        <div className="card shadow-sm border-0 overflow-hidden">
          <div className="table-responsive">
            <table className="table table-hover align-middle mb-0">
              <thead className="table-light text-muted text-uppercase" style={{ fontSize: "0.75rem", letterSpacing: "0.5px" }}>
                <tr>
                  <th className="py-3 px-4">Slot ID</th>
                  <th className="py-3 px-4">Station / Node</th>
                  <th className="py-3 px-4">Date</th>
                  <th className="py-3 px-4">Time Window</th>
                  <th className="py-3 px-4">Availability</th>
                  <th className="py-3 px-4">Status</th>
                  <th className="py-3 px-4 text-end">Actions</th>
                </tr>
              </thead>
              <tbody className="border-top-0">
                {filteredSlots.map((slot, i) => {
                  const mongoId = slot.id || slot._id;
                  const isAvail = slot.status === "Available";
                  const pct = slot.capacity > 0 ? Math.round((slot.availability / slot.capacity) * 100) : 0;
                  const barColor = pct === 0 ? "bg-danger" : pct <= 40 ? "bg-warning" : "bg-success";
                  
                  return (
                    <tr key={mongoId || slot.slotId || i}>
                      <td className="px-4 py-3">
                        <span className="badge bg-light text-dark font-monospace py-2 px-2 border" style={{ color: brand.dark }}>
                          {slot.slotId}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <div className="fw-bold">{stationMap[slot.stationId] || slot.stationId}</div>
                        <div className="text-muted font-monospace" style={{ fontSize: "0.75rem" }}>{slot.stationId}</div>
                      </td>
                      <td className="px-4 py-3 fw-semibold">📅 {fmtDate(slot.date)}</td>
                      <td className="px-4 py-3 fw-bold">{slot.startTime} – {slot.endTime}</td>
                      <td className="px-4 py-3" style={{ minWidth: "150px" }}>
                        <div className="d-flex justify-content-between mb-1" style={{ fontSize: "0.75rem", fontWeight: 600 }}>
                          <span className={pct === 0 ? "text-danger" : pct <= 40 ? "text-warning" : "text-success"}>{slot.availability} avail</span>
                          <span className="text-muted">/{slot.capacity}</span>
                        </div>
                        <div className="progress" style={{ height: "6px" }}>
                          <div className={`progress-bar ${barColor}`} role="progressbar" style={{ width: `${pct}%` }} aria-valuenow={pct} aria-valuemin="0" aria-valuemax="100"></div>
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <span className={`badge rounded-pill ${isAvail ? 'bg-success bg-opacity-10 text-success' : 'bg-danger bg-opacity-10 text-danger'} px-3 py-2 border ${isAvail ? 'border-success' : 'border-danger'} border-opacity-25`}>
                          <span className="d-inline-block rounded-circle me-2" style={{ width: "6px", height: "6px", backgroundColor: "currentColor" }}></span>
                          {slot.status}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-end">
                        <div className="btn-group">
                          <button
                            type="button"
                            className={`btn btn-sm ${isAvail ? 'btn-outline-warning' : 'btn-outline-success'}`}
                            disabled={togglingId === mongoId}
                            onClick={() => handleToggle(slot)}
                          >
                            {togglingId === mongoId ? "..." : isAvail ? "Deactivate" : "Activate"}
                          </button>
                          <button type="button" className="btn btn-sm btn-outline-secondary" onClick={() => openEditModal(slot)}>
                            ✎ Edit
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* SCHEDULE VIEW — grouped by date */}
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
              <div key={dateStr} className="card shadow-sm border-0">
                <div className="card-header bg-white border-bottom-0 pt-3 pb-0 d-flex justify-content-between align-items-center">
                  <h5 className="mb-0 fw-bold" style={{ color: brand.dark }}>📅 {dateStr}</h5>
                  <span className="badge bg-light text-muted border">{daySlots.length} slot{daySlots.length !== 1 ? "s" : ""}</span>
                </div>
                <div className="card-body">
                  <div className="row g-3">
                    {daySlots.map((slot) => {
                      const isAvail = slot.status === "Available";
                      return (
                        <div key={slot.id || slot._id || slot.slotId} className="col-12 col-md-6 col-lg-4 col-xl-3">
                          <div className={`card h-100 ${isAvail ? 'bg-light' : ''} border`}>
                            <div className="card-body d-flex flex-column gap-2">
                              <div className="d-flex justify-content-between align-items-center">
                                <span className="font-monospace fw-bold small" style={{ color: brand.dark }}>{slot.slotId}</span>
                                <span className={`badge rounded-pill ${isAvail ? 'bg-success' : 'bg-danger'}`}>{slot.status}</span>
                              </div>
                              <div>
                                <div className="fw-bold">{slot.startTime} – {slot.endTime}</div>
                                <div className="text-muted small">📍 {stationMap[slot.stationId] || slot.stationId}</div>
                              </div>
                              <div className="d-flex justify-content-between small fw-bold pt-2 border-top mt-auto">
                                <span className={slot.availability > 0 ? "text-success" : "text-danger"}>{slot.availability} spaces avail</span>
                                <span className="text-muted">Cap: {slot.capacity}</span>
                              </div>
                              <div className="d-flex gap-2 mt-2">
                                <button type="button" className="btn btn-sm btn-outline-secondary flex-grow-1" onClick={() => openEditModal(slot)}>✎ Edit</button>
                                <button type="button" className={`btn btn-sm ${isAvail ? 'btn-outline-warning' : 'btn-outline-success'} flex-grow-1`} onClick={() => handleToggle(slot)}>
                                  {isAvail ? "Deactivate" : "Activate"}
                                </button>
                              </div>
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

      {/* ============================================================ */}
      {/* CREATE SLOT MODAL                                            */}
      {/* ============================================================ */}
      {showCreateModal && (
        <div className="modal d-block" tabIndex="-1" style={{ backgroundColor: "rgba(0,0,0,0.5)" }}>
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content shadow">
              <div className="modal-header">
                <h5 className="modal-title fw-bold" style={{ color: brand.dark }}>⚡ Create Booking Slot</h5>
                <button type="button" className="btn-close" onClick={() => setShowCreateModal(false)}></button>
              </div>
              <div className="modal-body">
                {createError && (
                  <div className="alert alert-danger py-2 px-3 fw-semibold small mb-3">⚠ {createError}</div>
                )}
                <form onSubmit={handleCreate}>
                  <div className="mb-3">
                    <label className="form-label fw-semibold small">Microgrid Station <span className="text-danger">*</span></label>
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
                    <label className="form-label fw-semibold small">Date <span className="text-danger">*</span></label>
                    <input type="date" className="form-control" value={createForm.date} onChange={(e) => setCreateForm({ ...createForm, date: e.target.value })} required />
                  </div>
                  <div className="row g-3 mb-3">
                    <div className="col-6">
                      <label className="form-label fw-semibold small">Start Time <span className="text-danger">*</span></label>
                      <input type="time" className="form-control" value={createForm.startTime} onChange={(e) => setCreateForm({ ...createForm, startTime: e.target.value })} required />
                    </div>
                    <div className="col-6">
                      <label className="form-label fw-semibold small">End Time <span className="text-danger">*</span></label>
                      <input type="time" className="form-control" value={createForm.endTime} onChange={(e) => setCreateForm({ ...createForm, endTime: e.target.value })} required />
                    </div>
                  </div>
                  <div className="mb-4">
                    <label className="form-label fw-semibold small">Capacity (Max Booking Units) <span className="text-danger">*</span></label>
                    <input type="number" className="form-control" min="1" max="100" value={createForm.capacity} onChange={(e) => setCreateForm({ ...createForm, capacity: e.target.value })} required />
                    <div className="form-text">Availability will initially equal capacity. It decrements as reservations are made.</div>
                  </div>
                  <div className="d-flex justify-content-end gap-2">
                    <button type="button" className="btn btn-light" onClick={() => setShowCreateModal(false)}>Cancel</button>
                    <button type="submit" className="btn text-white" disabled={creating} style={{ backgroundColor: brand.green }}>
                      {creating ? "Creating..." : "Save Slot"}
                    </button>
                  </div>
                </form>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ============================================================ */}
      {/* EDIT SLOT MODAL                                              */}
      {/* ============================================================ */}
      {editingSlot && (
        <div className="modal d-block" tabIndex="-1" style={{ backgroundColor: "rgba(0,0,0,0.5)" }}>
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content shadow">
              <div className="modal-header d-flex flex-column align-items-start position-relative">
                <button type="button" className="btn-close position-absolute top-0 end-0 m-3" onClick={() => setEditingSlot(null)}></button>
                <h5 className="modal-title fw-bold" style={{ color: brand.dark }}>✎ Edit Slot Schedule</h5>
                <div className="text-muted font-monospace small">{editingSlot.slotId} · {editingSlot.stationId}</div>
              </div>
              <div className="modal-body">
                {/* Capacity/Availability read-only notice */}
                <div className="bg-light border rounded p-3 mb-3 d-flex justify-content-between align-items-center">
                  <div>
                    <div className="text-uppercase fw-bold text-muted small" style={{ fontSize: "0.7rem", letterSpacing: "0.5px" }}>Capacity / Availability</div>
                    <div className="fw-bold">{editingSlot.availability} of {editingSlot.capacity} spaces available</div>
                  </div>
                  <span className="badge bg-secondary">🔒 API Protected</span>
                </div>
                
                {updateError && (
                  <div className="alert alert-danger py-2 px-3 fw-semibold small mb-3">⚠ {updateError}</div>
                )}
                
                <form onSubmit={handleUpdate}>
                  <div className="mb-3">
                    <label className="form-label fw-semibold small">Date <span className="text-danger">*</span></label>
                    <input type="date" className="form-control" value={editForm.date} onChange={(e) => setEditForm({ ...editForm, date: e.target.value })} required />
                  </div>
                  <div className="row g-3 mb-3">
                    <div className="col-6">
                      <label className="form-label fw-semibold small">Start Time <span className="text-danger">*</span></label>
                      <input type="time" className="form-control" value={editForm.startTime} onChange={(e) => setEditForm({ ...editForm, startTime: e.target.value })} required />
                    </div>
                    <div className="col-6">
                      <label className="form-label fw-semibold small">End Time <span className="text-danger">*</span></label>
                      <input type="time" className="form-control" value={editForm.endTime} onChange={(e) => setEditForm({ ...editForm, endTime: e.target.value })} required />
                    </div>
                  </div>
                  <div className="mb-4">
                    <label className="form-label fw-semibold small">Status <span className="text-danger">*</span></label>
                    <select className="form-select" value={editForm.status} onChange={(e) => setEditForm({ ...editForm, status: e.target.value })} required>
                      <option value="Available">Available (Open for booking)</option>
                      <option value="Unavailable">Unavailable (Disabled)</option>
                    </select>
                    <div className="form-text">Schedule changes require no active reservations on this slot.</div>
                  </div>
                  <div className="d-flex justify-content-end gap-2">
                    <button type="button" className="btn btn-light" onClick={() => setEditingSlot(null)}>Cancel</button>
                    <button type="submit" className="btn text-white" disabled={updating} style={{ backgroundColor: brand.green }}>
                      {updating ? "Saving..." : "Update Slot"}
                    </button>
                  </div>
                </form>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default EnergySlots;
