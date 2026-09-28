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

  const closeModal = () => {
    setShowCreateModal(false);
    setEditingSlot(null);
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
      closeModal();
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
      closeModal();
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

  // ================================================================
  // RENDER
  // ================================================================
  return (
    <div className="container-fluid p-4 p-md-5">
      {/* HEADER */}
      <div className="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-3">
        <div>
          <h2 className="h3 mb-1">Energy Booking Slots</h2>
          <p className="text-secondary mb-0">
            Manage microgrid node operating windows, capacity allocations and availability schedules.
          </p>
        </div>
        <div className="d-flex gap-2">
          <div className="btn-group" role="group">
            <button
              type="button"
              className={`btn btn-sm ${viewMode === "table" ? "btn-secondary" : "btn-outline-secondary"}`}
              onClick={() => setViewMode("table")}
            >
              <i className="bi bi-table me-1"></i> Table
            </button>
            <button
              type="button"
              className={`btn btn-sm ${viewMode === "schedule" ? "btn-secondary" : "btn-outline-secondary"}`}
              onClick={() => setViewMode("schedule")}
            >
              <i className="bi bi-calendar3 me-1"></i> Schedule
            </button>
          </div>
          <button onClick={openCreateModal} className="btn btn-success fw-semibold">
            <i className="bi bi-plus-lg me-1"></i>
            Create Slot
          </button>
        </div>
      </div>

      {/* BANNERS */}
      {successMessage && (
        <div className="alert alert-success d-flex align-items-center mb-4 py-2" role="alert">
          <span>{successMessage}</span>
        </div>
      )}
      {error && (
        <div className="alert alert-danger d-flex align-items-center justify-content-between mb-4 py-2" role="alert">
          <span>{error}</span>
          <button type="button" className="btn-close" onClick={() => setError("")}></button>
        </div>
      )}

      {/* KPI CARDS */}
      <div className="row g-3 mb-4">
        <div className="col-6 col-md-3">
          <SummaryCard icon="bi-lightning-charge" label="Total Slots" value={stats.total} bg="bg-primary-subtle" />
        </div>
        <div className="col-6 col-md-3">
          <SummaryCard icon="bi-check-circle" label="Available Slots" value={stats.available} bg="bg-success-subtle" />
        </div>
        <div className="col-6 col-md-3">
          <SummaryCard icon="bi-battery-full" label="Total Capacity" value={stats.totalCap} bg="bg-info-subtle" />
        </div>
        <div className="col-6 col-md-3">
          <SummaryCard icon="bi-battery-half" label="Remaining Spaces" value={stats.totalAvail} bg="bg-warning-subtle" />
        </div>
      </div>

      {/* FILTERS */}
      <div className="d-flex flex-wrap gap-2 mb-3">
        <div style={{ maxWidth: "240px", flexGrow: 1 }}>
          <input
            type="text"
            className="form-control"
            placeholder="Search slot ID or station..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
        </div>
        <div style={{ maxWidth: "200px" }}>
          <select className="form-select" value={selectedStation} onChange={(e) => setSelectedStation(e.target.value)}>
            <option value="all">All Stations</option>
            {stations.map((st) => {
              const id = st.nodeId || st.nodeID || st.NodeId || st.NodeID;
              const name = st.nodeName || st.name || st.NodeName || id;
              return <option key={id} value={id}>{name}</option>;
            })}
          </select>
        </div>
        <div style={{ maxWidth: "150px" }}>
          <input type="date" className="form-control" value={selectedDate} onChange={(e) => setSelectedDate(e.target.value)} />
        </div>
        <div style={{ maxWidth: "160px" }}>
          <select className="form-select" value={selectedStatus} onChange={(e) => setSelectedStatus(e.target.value)}>
            <option value="all">All Statuses</option>
            <option value="Available">Available</option>
            <option value="Unavailable">Unavailable</option>
          </select>
        </div>
        {(searchQuery || selectedStation !== "all" || selectedDate || selectedStatus !== "all") && (
          <button type="button" className="btn btn-light text-secondary" onClick={() => { setSearchQuery(""); setSelectedStation("all"); setSelectedDate(""); setSelectedStatus("all"); }}>
            Clear Filters
          </button>
        )}
      </div>

      {/* TABLE VIEW */}
      {viewMode === "table" && (
        <div className="card shadow-sm">
          <div className="table-responsive">
            <table className="table table-hover align-middle mb-0">
              <thead className="table-light">
                <tr className="small text-uppercase text-secondary">
                  <th className="px-4 py-3 text-nowrap">Slot ID</th>
                  <th className="px-4 py-3">Station / Node</th>
                  <th className="px-4 py-3">Date</th>
                  <th className="px-4 py-3">Time Window</th>
                  <th className="px-4 py-3">Availability</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3"></th>
                </tr>
              </thead>
              <tbody>
                {loading && (
                  <tr>
                    <td colSpan="7" className="text-center text-secondary py-4">
                      Loading Energy Slots...
                    </td>
                  </tr>
                )}
                {!loading && filteredSlots.length === 0 && (
                  <tr>
                    <td colSpan="7" className="text-center text-secondary py-4">
                      {slots.length === 0 ? "No slots exist yet." : "No slots match the filters."}
                    </td>
                  </tr>
                )}
                {!loading && filteredSlots.map((slot) => {
                  const mongoId = slot.id || slot._id;
                  const isAvail = slot.status === "Available";
                  return (
                    <tr key={mongoId || slot.slotId}>
                      <td className="px-4 py-3 fw-semibold text-nowrap">{slot.slotId}</td>
                      <td className="px-4 py-3">
                        <div>{stationMap[slot.stationId] || slot.stationId}</div>
                      </td>
                      <td className="px-4 py-3 text-nowrap">{fmtDate(slot.date)}</td>
                      <td className="px-4 py-3 text-nowrap">{slot.startTime} – {slot.endTime}</td>
                      <td className="px-4 py-3 text-nowrap">
                        {slot.availability} / {slot.capacity}
                      </td>
                      <td className="px-4 py-3">
                        <span className={`badge rounded-pill ${isAvail ? "text-bg-success" : "text-bg-danger"}`}>
                          {isAvail ? "Available" : "Unavailable"}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-end text-nowrap">
                        <button
                          onClick={() => openEditModal(slot)}
                          className="btn btn-sm btn-outline-secondary ms-1"
                        >
                          Edit
                        </button>
                        <button
                          disabled={togglingId === mongoId}
                          onClick={() => handleToggle(slot)}
                          className={`btn btn-sm ${isAvail ? "btn-outline-warning" : "btn-outline-success"} ms-1`}
                        >
                          {togglingId === mongoId ? "..." : isAvail ? "Deactivate" : "Activate"}
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
              <div key={dateStr} className="card shadow-sm">
                <div className="card-header bg-white border-bottom-0 pt-3 pb-0 d-flex justify-content-between align-items-center">
                  <h5 className="mb-0 h6 fw-bold">📅 {dateStr}</h5>
                  <span className="badge text-bg-light text-secondary border">{daySlots.length} slots</span>
                </div>
                <div className="card-body">
                  <div className="row g-3">
                    {daySlots.map((slot) => {
                      const isAvail = slot.status === "Available";
                      return (
                        <div key={slot.id || slot._id || slot.slotId} className="col-12 col-md-6 col-lg-4 col-xl-3">
                          <div className={`card h-100 ${isAvail ? 'bg-light' : ''}`}>
                            <div className="card-body d-flex flex-column gap-2">
                              <div className="d-flex justify-content-between align-items-center">
                                <span className="fw-semibold small">{slot.slotId}</span>
                                <span className={`badge rounded-pill ${isAvail ? 'text-bg-success' : 'text-bg-danger'}`}>{slot.status}</span>
                              </div>
                              <div>
                                <div className="fw-bold">{slot.startTime} – {slot.endTime}</div>
                                <div className="text-secondary small">{stationMap[slot.stationId] || slot.stationId}</div>
                              </div>
                              <div className="d-flex justify-content-between small fw-semibold pt-2 border-top mt-auto">
                                <span>{slot.availability} avail</span>
                                <span className="text-secondary">Cap: {slot.capacity}</span>
                              </div>
                              <div className="d-flex gap-2 mt-2">
                                <button type="button" className="btn btn-sm btn-outline-secondary flex-grow-1" onClick={() => openEditModal(slot)}>Edit</button>
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

      {/* CREATE / EDIT MODAL */}
      {(showCreateModal || editingSlot) && (
        <>
          <div className="modal d-block" tabIndex="-1" role="dialog">
            <div className="modal-dialog modal-dialog-centered">
              <form onSubmit={editingSlot ? handleUpdate : handleCreate} className="modal-content">
                <div className="modal-header">
                  <div>
                    <h5 className="modal-title">{editingSlot ? "Edit slot schedule" : "Create booking slot"}</h5>
                    {editingSlot && (
                      <div className="small text-secondary">{editingSlot.slotId} · {editingSlot.stationId}</div>
                    )}
                  </div>
                  <button type="button" className="btn-close" aria-label="Close" onClick={closeModal}></button>
                </div>

                <div className="modal-body">
                  {editingSlot && (
                    <div className="alert alert-secondary py-2 mb-3 d-flex justify-content-between align-items-center">
                      <div>
                        <div className="small fw-semibold text-uppercase">Capacity / Availability</div>
                        <div>{editingSlot.availability} of {editingSlot.capacity} spaces available</div>
                      </div>
                      <span className="badge text-bg-secondary">🔒 API Protected</span>
                    </div>
                  )}
                  
                  {(createError || updateError) && (
                    <div className="alert alert-danger py-2 px-3 fw-semibold small mb-3">
                      {createError || updateError}
                    </div>
                  )}
                  
                  {!editingSlot && (
                    <div className="mb-3">
                      <label className="form-label fw-semibold small">Microgrid Station</label>
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
                    </div>
                  )}

                  <div className="mb-3">
                    <label className="form-label fw-semibold small">Date</label>
                    <input 
                      type="date" 
                      className="form-control" 
                      value={editingSlot ? editForm.date : createForm.date} 
                      onChange={(e) => editingSlot ? setEditForm({ ...editForm, date: e.target.value }) : setCreateForm({ ...createForm, date: e.target.value })} 
                      required 
                    />
                  </div>

                  <div className="row g-3 mb-3">
                    <div className="col-6">
                      <label className="form-label fw-semibold small">Start Time</label>
                      <input 
                        type="time" 
                        className="form-control" 
                        value={editingSlot ? editForm.startTime : createForm.startTime} 
                        onChange={(e) => editingSlot ? setEditForm({ ...editForm, startTime: e.target.value }) : setCreateForm({ ...createForm, startTime: e.target.value })} 
                        required 
                      />
                    </div>
                    <div className="col-6">
                      <label className="form-label fw-semibold small">End Time</label>
                      <input 
                        type="time" 
                        className="form-control" 
                        value={editingSlot ? editForm.endTime : createForm.endTime} 
                        onChange={(e) => editingSlot ? setEditForm({ ...editForm, endTime: e.target.value }) : setCreateForm({ ...createForm, endTime: e.target.value })} 
                        required 
                      />
                    </div>
                  </div>

                  {!editingSlot && (
                    <div className="mb-1">
                      <label className="form-label fw-semibold small">Capacity (Max Booking Units)</label>
                      <input 
                        type="number" 
                        className="form-control" 
                        min="1" max="100" 
                        value={createForm.capacity} 
                        onChange={(e) => setCreateForm({ ...createForm, capacity: e.target.value })} 
                        required 
                      />
                    </div>
                  )}

                  {editingSlot && (
                    <div className="mb-1">
                      <label className="form-label fw-semibold small">Status</label>
                      <select className="form-select" value={editForm.status} onChange={(e) => setEditForm({ ...editForm, status: e.target.value })} required>
                        <option value="Available">Available (Open for booking)</option>
                        <option value="Unavailable">Unavailable (Disabled)</option>
                      </select>
                    </div>
                  )}
                </div>

                <div className="modal-footer">
                  <button type="button" onClick={closeModal} className="btn btn-outline-secondary">
                    Cancel
                  </button>
                  <button type="submit" className="btn btn-success fw-semibold" disabled={creating || updating}>
                    {editingSlot ? (updating ? "Saving..." : "Save changes") : (creating ? "Creating..." : "Create slot")}
                  </button>
                </div>
              </form>
            </div>
          </div>
          <div className="modal-backdrop show"></div>
        </>
      )}
    </div>
  );
}

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

export default EnergySlots;
