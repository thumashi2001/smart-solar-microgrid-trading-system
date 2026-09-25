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
  // Design tokens
  // ----------------------------------------------------------------
  const c = {
    primary: "#0B3B2E",
    accent: "#1E7A4D",
    bg: "#F7F5F1",
    card: "#fff",
    border: "#E4E1DA",
    textMain: "#1C1F1E",
    textMuted: "#6B6862",
    green: "#137333",
    red: "#C5221F",
    blue: "#1A73E8",
  };

  const inputStyle = {
    width: "100%",
    padding: "10px 12px",
    borderRadius: "8px",
    border: `1px solid ${c.border}`,
    fontSize: "14px",
    outline: "none",
    boxSizing: "border-box",
    background: "#fff",
    color: c.textMain,
  };
  const labelStyle = {
    display: "block",
    fontSize: "13px",
    fontWeight: 600,
    color: c.textMain,
    marginBottom: "6px",
  };
  const btnPrimary = {
    padding: "10px 22px",
    borderRadius: "8px",
    border: "none",
    background: "linear-gradient(135deg, #1E7A4D, #0B3B2E)",
    color: "#fff",
    fontSize: "14px",
    fontWeight: 600,
    cursor: "pointer",
  };
  const btnSecondary = {
    padding: "10px 18px",
    borderRadius: "8px",
    border: `1px solid ${c.border}`,
    background: "#fff",
    color: c.textMuted,
    fontSize: "14px",
    fontWeight: 600,
    cursor: "pointer",
  };

  // ================================================================
  // RENDER
  // ================================================================
  return (
    <div style={{ padding: "32px", maxWidth: "1400px", margin: "0 auto", fontFamily: "'Segoe UI', Arial, sans-serif" }}>
      {/* HEADER */}
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: "24px", flexWrap: "wrap", gap: "16px" }}>
        <div>
          <h1 style={{ fontSize: "26px", fontWeight: 700, color: c.textMain, margin: "0 0 6px 0" }}>
            ⚡ Energy Booking Slots
          </h1>
          <p style={{ margin: 0, color: c.textMuted, fontSize: "14px" }}>
            Manage microgrid node operating windows, capacity allocations and availability schedules.
          </p>
        </div>
        <div style={{ display: "flex", gap: "12px", alignItems: "center" }}>
          {/* View toggle */}
          <div style={{ display: "flex", background: "#EBE8E1", borderRadius: "8px", padding: "3px" }}>
            {["table", "schedule"].map((mode) => (
              <button
                key={mode}
                type="button"
                onClick={() => setViewMode(mode)}
                style={{
                  border: "none",
                  background: viewMode === mode ? "#fff" : "transparent",
                  color: viewMode === mode ? c.primary : c.textMuted,
                  fontWeight: viewMode === mode ? 600 : 500,
                  padding: "6px 14px",
                  borderRadius: "6px",
                  fontSize: "13px",
                  cursor: "pointer",
                  boxShadow: viewMode === mode ? "0 1px 3px rgba(0,0,0,0.1)" : "none",
                }}
              >
                {mode === "table" ? "☰ Table" : "📅 Schedule"}
              </button>
            ))}
          </div>
          <button
            type="button"
            onClick={openCreateModal}
            style={{ ...btnPrimary, display: "flex", alignItems: "center", gap: "8px", boxShadow: "0 2px 6px rgba(11,59,46,0.25)" }}
          >
            <span style={{ fontSize: "18px", lineHeight: 1 }}>+</span> Create Slot
          </button>
        </div>
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
      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(200px, 1fr))", gap: "16px", marginBottom: "24px" }}>
        {[
          { label: "Total Slots", value: stats.total, color: c.textMain, sub: "Configured across nodes" },
          { label: "Available Slots", value: stats.available, color: c.accent, sub: "Open for booking" },
          { label: "Total Capacity", value: stats.totalCap, color: c.blue, sub: "Max booking units" },
          { label: "Remaining Spaces", value: stats.totalAvail, color: "#D93025", sub: "Current availability" },
        ].map((card) => (
          <div key={card.label} style={{ background: c.card, borderRadius: "12px", padding: "18px 20px", border: `1px solid ${c.border}`, boxShadow: "0 1px 3px rgba(0,0,0,0.04)" }}>
            <div style={{ fontSize: "11px", fontWeight: 700, textTransform: "uppercase", color: card.color, letterSpacing: "0.5px" }}>{card.label}</div>
            <div style={{ fontSize: "30px", fontWeight: 700, color: card.color, margin: "4px 0" }}>{card.value}</div>
            <div style={{ fontSize: "12px", color: c.textMuted }}>{card.sub}</div>
          </div>
        ))}
      </div>

      {/* FILTERS */}
      <div style={{ background: c.card, borderRadius: "12px", padding: "16px 20px", border: `1px solid ${c.border}`, marginBottom: "24px", display: "flex", flexWrap: "wrap", gap: "12px", alignItems: "center", justifyContent: "space-between" }}>
        <div style={{ display: "flex", flexWrap: "wrap", gap: "10px", flexGrow: 1 }}>
          <div style={{ position: "relative", minWidth: "200px", flexGrow: 1 }}>
            <span style={{ position: "absolute", left: "12px", top: "50%", transform: "translateY(-50%)", color: "#9E9A90", fontSize: "14px" }}>🔍</span>
            <input type="text" placeholder="Search slot ID or station..." value={searchQuery} onChange={(e) => setSearchQuery(e.target.value)} style={{ ...inputStyle, paddingLeft: "34px" }} />
          </div>
          <select value={selectedStation} onChange={(e) => setSelectedStation(e.target.value)} style={{ ...inputStyle, width: "auto", minWidth: "180px" }}>
            <option value="all">All Stations</option>
            {stations.map((st) => {
              const id = st.nodeId || st.nodeID || st.NodeId || st.NodeID;
              const name = st.nodeName || st.name || st.NodeName || id;
              return <option key={id} value={id}>{name} ({id})</option>;
            })}
          </select>
          <input type="date" value={selectedDate} onChange={(e) => setSelectedDate(e.target.value)} style={{ ...inputStyle, width: "auto" }} />
          <select value={selectedStatus} onChange={(e) => setSelectedStatus(e.target.value)} style={{ ...inputStyle, width: "auto" }}>
            <option value="all">All Statuses</option>
            <option value="Available">Available</option>
            <option value="Unavailable">Unavailable</option>
          </select>
        </div>
        {(searchQuery || selectedStation !== "all" || selectedDate || selectedStatus !== "all") && (
          <button type="button" onClick={() => { setSearchQuery(""); setSelectedStation("all"); setSelectedDate(""); setSelectedStatus("all"); }} style={{ background: "#F0ECE1", border: "none", color: c.textMuted, padding: "9px 14px", borderRadius: "8px", fontSize: "13px", cursor: "pointer" }}>
            Clear Filters
          </button>
        )}
      </div>

      {/* LOADING */}
      {loading && (
        <div style={{ background: c.card, borderRadius: "12px", padding: "48px", textAlign: "center", border: `1px solid ${c.border}`, color: c.textMuted }}>
          <div style={{ fontSize: "28px", marginBottom: "8px" }}>⏳</div>
          <div style={{ fontSize: "16px", fontWeight: 600 }}>Loading Energy Slots...</div>
        </div>
      )}

      {/* EMPTY STATE */}
      {!loading && filteredSlots.length === 0 && (
        <div style={{ background: c.card, borderRadius: "12px", padding: "48px 24px", textAlign: "center", border: `1px solid ${c.border}` }}>
          <div style={{ fontSize: "36px", marginBottom: "12px" }}>⚡</div>
          <h3 style={{ fontSize: "18px", fontWeight: 700, color: c.textMain, margin: "0 0 6px 0" }}>No energy booking slots found</h3>
          <p style={{ color: c.textMuted, fontSize: "14px", margin: "0 0 18px 0" }}>
            {slots.length === 0
              ? "No slots exist yet. Create the first slot to enable prosumer bookings."
              : "No slots match the current filters."}
          </p>
          <button type="button" onClick={openCreateModal} style={btnPrimary}>+ Create New Slot</button>
        </div>
      )}

      {/* TABLE VIEW */}
      {!loading && filteredSlots.length > 0 && viewMode === "table" && (
        <div style={{ background: c.card, borderRadius: "12px", border: `1px solid ${c.border}`, overflow: "hidden", boxShadow: "0 1px 3px rgba(0,0,0,0.04)" }}>
          <div style={{ overflowX: "auto" }}>
            <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "14px", textAlign: "left" }}>
              <thead>
                <tr style={{ background: "#F7F5F1", borderBottom: `1px solid ${c.border}`, color: c.textMuted, fontSize: "11px", textTransform: "uppercase", fontWeight: 700 }}>
                  {["Slot ID", "Station / Node", "Date", "Time Window", "Availability", "Status", "Actions"].map((h, i) => (
                    <th key={h} style={{ padding: "14px 16px", textAlign: i === 6 ? "right" : "left" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {filteredSlots.map((slot, i) => {
                  const mongoId = slot.id || slot._id;
                  const isAvail = slot.status === "Available";
                  const pct = slot.capacity > 0 ? Math.round((slot.availability / slot.capacity) * 100) : 0;
                  const barColor = pct === 0 ? c.red : pct <= 40 ? "#F9AB00" : c.accent;
                  return (
                    <tr
                      key={mongoId || slot.slotId || i}
                      style={{ borderBottom: i === filteredSlots.length - 1 ? "none" : `1px solid #F0ECE1` }}
                      onMouseEnter={(e) => (e.currentTarget.style.background = "#FAF8F5")}
                      onMouseLeave={(e) => (e.currentTarget.style.background = "#fff")}
                    >
                      <td style={{ padding: "16px" }}>
                        <span style={{ background: "#EBE8E1", padding: "4px 8px", borderRadius: "6px", fontFamily: "monospace", fontSize: "12px", fontWeight: 700, color: c.primary }}>
                          {slot.slotId}
                        </span>
                      </td>
                      <td style={{ padding: "16px" }}>
                        <div style={{ fontWeight: 600 }}>{stationMap[slot.stationId] || slot.stationId}</div>
                        <div style={{ fontSize: "11px", color: c.textMuted, fontFamily: "monospace" }}>{slot.stationId}</div>
                      </td>
                      <td style={{ padding: "16px", fontWeight: 500 }}>📅 {fmtDate(slot.date)}</td>
                      <td style={{ padding: "16px", fontWeight: 600 }}>{slot.startTime} – {slot.endTime}</td>
                      <td style={{ padding: "16px", minWidth: "150px" }}>
                        <div style={{ display: "flex", justifyContent: "space-between", fontSize: "12px", fontWeight: 600, marginBottom: "4px" }}>
                          <span style={{ color: barColor }}>{slot.availability} avail</span>
                          <span style={{ color: c.textMuted }}>/{slot.capacity}</span>
                        </div>
                        <div style={{ height: "6px", background: "#EBE8E1", borderRadius: "3px", overflow: "hidden" }}>
                          <div style={{ width: `${pct}%`, height: "100%", background: barColor, borderRadius: "3px", transition: "width 0.3s" }} />
                        </div>
                      </td>
                      <td style={{ padding: "16px" }}>
                        <span style={{ display: "inline-flex", alignItems: "center", gap: "5px", padding: "4px 10px", borderRadius: "12px", fontSize: "12px", fontWeight: 600, background: isAvail ? "#E6F4EA" : "#FCE8E6", color: isAvail ? c.green : c.red }}>
                          <span style={{ width: "6px", height: "6px", borderRadius: "50%", background: isAvail ? c.green : c.red }} />
                          {slot.status}
                        </span>
                      </td>
                      <td style={{ padding: "16px", textAlign: "right" }}>
                        <div style={{ display: "inline-flex", gap: "8px" }}>
                          <button
                            type="button"
                            disabled={togglingId === mongoId}
                            onClick={() => handleToggle(slot)}
                            style={{ border: `1px solid ${c.border}`, background: isAvail ? "#FFF8F0" : "#F0F8F4", color: isAvail ? "#B06000" : c.accent, borderRadius: "6px", padding: "6px 10px", fontSize: "12px", fontWeight: 600, cursor: togglingId === mongoId ? "wait" : "pointer" }}
                          >
                            {togglingId === mongoId ? "..." : isAvail ? "Deactivate" : "Activate"}
                          </button>
                          <button type="button" onClick={() => openEditModal(slot)} style={{ border: `1px solid ${c.border}`, background: "#fff", color: c.textMain, borderRadius: "6px", padding: "6px 10px", fontSize: "12px", fontWeight: 600, cursor: "pointer" }}>
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
        <div style={{ display: "flex", flexDirection: "column", gap: "24px" }}>
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
              <div key={dateStr} style={{ background: c.card, borderRadius: "12px", border: `1px solid ${c.border}`, padding: "20px" }}>
                <div style={{ fontSize: "16px", fontWeight: 700, color: c.primary, borderBottom: `1px solid ${c.border}`, paddingBottom: "10px", marginBottom: "16px", display: "flex", justifyContent: "space-between" }}>
                  <span>📅 {dateStr}</span>
                  <span style={{ fontSize: "13px", color: c.textMuted, fontWeight: 500 }}>{daySlots.length} slot{daySlots.length !== 1 ? "s" : ""}</span>
                </div>
                <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(260px, 1fr))", gap: "14px" }}>
                  {daySlots.map((slot) => {
                    const isAvail = slot.status === "Available";
                    return (
                      <div key={slot.id || slot._id || slot.slotId} style={{ border: `1px solid ${c.border}`, borderRadius: "10px", padding: "16px", background: isAvail ? "#FAFAF8" : "#FFF7F7", display: "flex", flexDirection: "column", gap: "10px" }}>
                        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                          <span style={{ fontWeight: 700, fontFamily: "monospace", fontSize: "12px", color: c.primary }}>{slot.slotId}</span>
                          <span style={{ fontSize: "11px", fontWeight: 600, padding: "3px 8px", borderRadius: "10px", background: isAvail ? "#E6F4EA" : "#FCE8E6", color: isAvail ? c.green : c.red }}>{slot.status}</span>
                        </div>
                        <div>
                          <div style={{ fontSize: "15px", fontWeight: 600, color: c.textMain }}>{slot.startTime} – {slot.endTime}</div>
                          <div style={{ fontSize: "12px", color: c.textMuted }}>📍 {stationMap[slot.stationId] || slot.stationId}</div>
                        </div>
                        <div style={{ display: "flex", justifyContent: "space-between", fontSize: "12px", fontWeight: 600, paddingTop: "6px", borderTop: "1px dashed #E4E1DA" }}>
                          <span style={{ color: slot.availability > 0 ? c.accent : c.red }}>{slot.availability} spaces avail</span>
                          <span style={{ color: c.textMuted }}>Cap: {slot.capacity}</span>
                        </div>
                        <div style={{ display: "flex", gap: "8px" }}>
                          <button type="button" onClick={() => openEditModal(slot)} style={{ flex: 1, padding: "7px", fontSize: "12px", fontWeight: 600, borderRadius: "6px", border: `1px solid ${c.border}`, background: "#fff", cursor: "pointer" }}>✎ Edit</button>
                          <button type="button" onClick={() => handleToggle(slot)} style={{ padding: "7px 12px", fontSize: "12px", fontWeight: 600, borderRadius: "6px", border: `1px solid ${c.border}`, background: isAvail ? "#FFF8F0" : "#F0F8F4", color: isAvail ? "#B06000" : c.accent, cursor: "pointer" }}>
                            {isAvail ? "Deactivate" : "Activate"}
                          </button>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            ))}
        </div>
      )}

      {/* ============================================================ */}
      {/* CREATE SLOT MODAL                                            */}
      {/* ============================================================ */}
      {showCreateModal && (
        <div style={{ position: "fixed", inset: 0, background: "rgba(0,0,0,0.5)", display: "flex", alignItems: "center", justifyContent: "center", zIndex: 1000, padding: "20px" }}>
          <div style={{ background: "#fff", borderRadius: "14px", padding: "28px", width: "100%", maxWidth: "520px", boxShadow: "0 20px 40px rgba(0,0,0,0.2)", maxHeight: "90vh", overflowY: "auto" }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "20px", borderBottom: `1px solid ${c.border}`, paddingBottom: "12px" }}>
              <h2 style={{ fontSize: "20px", fontWeight: 700, color: c.primary, margin: 0 }}>⚡ Create Booking Slot</h2>
              <button type="button" onClick={() => setShowCreateModal(false)} style={{ background: "none", border: "none", fontSize: "20px", color: c.textMuted, cursor: "pointer" }}>✕</button>
            </div>
            {createError && (
              <div style={{ background: "#FCE8E6", color: c.red, padding: "10px 14px", borderRadius: "8px", fontSize: "13px", marginBottom: "16px", fontWeight: 500 }}>⚠ {createError}</div>
            )}
            <form onSubmit={handleCreate}>
              <div style={{ marginBottom: "16px" }}>
                <label style={labelStyle}>Microgrid Station *</label>
                <select value={createForm.stationId} onChange={(e) => setCreateForm({ ...createForm, stationId: e.target.value })} required style={inputStyle}>
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
                <span style={{ fontSize: "11px", color: c.textMuted }}>Only active stations are available for scheduling.</span>
              </div>
              <div style={{ marginBottom: "16px" }}>
                <label style={labelStyle}>Date *</label>
                <input type="date" value={createForm.date} onChange={(e) => setCreateForm({ ...createForm, date: e.target.value })} required style={inputStyle} />
              </div>
              <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "14px", marginBottom: "16px" }}>
                <div>
                  <label style={labelStyle}>Start Time *</label>
                  <input type="time" value={createForm.startTime} onChange={(e) => setCreateForm({ ...createForm, startTime: e.target.value })} required style={inputStyle} />
                </div>
                <div>
                  <label style={labelStyle}>End Time *</label>
                  <input type="time" value={createForm.endTime} onChange={(e) => setCreateForm({ ...createForm, endTime: e.target.value })} required style={inputStyle} />
                </div>
              </div>
              <div style={{ marginBottom: "24px" }}>
                <label style={labelStyle}>Capacity (Max Booking Units) *</label>
                <input type="number" min="1" max="100" value={createForm.capacity} onChange={(e) => setCreateForm({ ...createForm, capacity: e.target.value })} required style={inputStyle} />
                <span style={{ fontSize: "11px", color: c.textMuted }}>Availability will initially equal capacity. It decrements as reservations are made.</span>
              </div>
              <div style={{ display: "flex", justifyContent: "flex-end", gap: "10px" }}>
                <button type="button" onClick={() => setShowCreateModal(false)} style={btnSecondary}>Cancel</button>
                <button type="submit" disabled={creating} style={{ ...btnPrimary, opacity: creating ? 0.7 : 1 }}>{creating ? "Creating..." : "Save Slot"}</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ============================================================ */}
      {/* EDIT SLOT MODAL                                              */}
      {/* ============================================================ */}
      {editingSlot && (
        <div style={{ position: "fixed", inset: 0, background: "rgba(0,0,0,0.5)", display: "flex", alignItems: "center", justifyContent: "center", zIndex: 1000, padding: "20px" }}>
          <div style={{ background: "#fff", borderRadius: "14px", padding: "28px", width: "100%", maxWidth: "520px", boxShadow: "0 20px 40px rgba(0,0,0,0.2)", maxHeight: "90vh", overflowY: "auto" }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "20px", borderBottom: `1px solid ${c.border}`, paddingBottom: "12px" }}>
              <div>
                <h2 style={{ fontSize: "20px", fontWeight: 700, color: c.primary, margin: "0 0 2px 0" }}>✎ Edit Slot Schedule</h2>
                <span style={{ fontFamily: "monospace", fontSize: "12px", color: c.textMuted }}>{editingSlot.slotId} · {editingSlot.stationId}</span>
              </div>
              <button type="button" onClick={() => setEditingSlot(null)} style={{ background: "none", border: "none", fontSize: "20px", color: c.textMuted, cursor: "pointer" }}>✕</button>
            </div>
            {/* Capacity/Availability read-only notice */}
            <div style={{ background: "#F7F5F1", border: `1px solid ${c.border}`, borderRadius: "8px", padding: "12px 16px", marginBottom: "18px", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <div>
                <div style={{ fontSize: "11px", fontWeight: 700, color: c.textMuted, textTransform: "uppercase" }}>Capacity / Availability</div>
                <div style={{ fontSize: "15px", fontWeight: 700, color: c.textMain }}>{editingSlot.availability} of {editingSlot.capacity} spaces available</div>
              </div>
              <span style={{ fontSize: "11px", background: "#EBE8E1", color: c.textMuted, padding: "4px 8px", borderRadius: "4px", fontWeight: 600 }}>🔒 API Protected</span>
            </div>
            {updateError && (
              <div style={{ background: "#FCE8E6", color: c.red, padding: "10px 14px", borderRadius: "8px", fontSize: "13px", marginBottom: "16px", fontWeight: 500 }}>⚠ {updateError}</div>
            )}
            <form onSubmit={handleUpdate}>
              <div style={{ marginBottom: "16px" }}>
                <label style={labelStyle}>Date *</label>
                <input type="date" value={editForm.date} onChange={(e) => setEditForm({ ...editForm, date: e.target.value })} required style={inputStyle} />
              </div>
              <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "14px", marginBottom: "16px" }}>
                <div>
                  <label style={labelStyle}>Start Time *</label>
                  <input type="time" value={editForm.startTime} onChange={(e) => setEditForm({ ...editForm, startTime: e.target.value })} required style={inputStyle} />
                </div>
                <div>
                  <label style={labelStyle}>End Time *</label>
                  <input type="time" value={editForm.endTime} onChange={(e) => setEditForm({ ...editForm, endTime: e.target.value })} required style={inputStyle} />
                </div>
              </div>
              <div style={{ marginBottom: "24px" }}>
                <label style={labelStyle}>Status *</label>
                <select value={editForm.status} onChange={(e) => setEditForm({ ...editForm, status: e.target.value })} required style={inputStyle}>
                  <option value="Available">Available (Open for booking)</option>
                  <option value="Unavailable">Unavailable (Disabled)</option>
                </select>
                <span style={{ fontSize: "11px", color: c.textMuted }}>Schedule changes require no active reservations on this slot.</span>
              </div>
              <div style={{ display: "flex", justifyContent: "flex-end", gap: "10px" }}>
                <button type="button" onClick={() => setEditingSlot(null)} style={btnSecondary}>Cancel</button>
                <button type="submit" disabled={updating} style={{ ...btnPrimary, opacity: updating ? 0.7 : 1 }}>{updating ? "Saving..." : "Update Slot"}</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

export default EnergySlots;
