import { useEffect, useState } from "react";
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid } from "recharts";
import api from "../api";

const accent = "#1E7A4D";

function Prosumers() {
  const [prosumers, setProsumers] = useState([]);
  const [search, setSearch] = useState("");
  const [showModal, setShowModal] = useState(false);
  const [editingProsumer, setEditingProsumer] = useState(null);
  const [form, setForm] = useState({ nic: "", fullName: "", email: "", phone: "", passwordHash: "" });

  const loadProsumers = () => {
    api.get("/prosumers").then((res) => setProsumers(res.data));
  };

  useEffect(() => {
    loadProsumers();
  }, []);

  const openEdit = (p) => {
    setEditingProsumer(p);
    setForm({ nic: p.nic, fullName: p.fullName, email: p.email, phone: p.phone, passwordHash: "" });
    setShowModal(true);
  };

  const closeModal = () => setShowModal(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    await api.put(`/prosumers/${editingProsumer.nic}`, { ...form, status: editingProsumer.status });
    setShowModal(false);
    loadProsumers();
  };

  const advance = async (nic, currentStatus) => {
    const endpoint = currentStatus === "active" ? "deactivate" : "reactivate";
    await api.patch(`/prosumers/${nic}/${endpoint}`);
    loadProsumers();
  };

  const remove = async (nic) => {
    if (window.confirm("Delete this prosumer permanently? This cannot be undone.")) {
      await api.delete(`/prosumers/${nic}`);
      loadProsumers();
    }
  };

  const statusInfo = (status) => {
    if (status === "pendingActivation") return { label: "Pending", bg: "#FBF0DC", color: "#9A6B15" };
    if (status === "active") return { label: "Active", bg: "#E5F3EA", color: "#2E7D4F" };
    return { label: "Deactivated", bg: "#F7E7E5", color: "#B14A3C" };
  };

  const actionLabel = (status) => {
    if (status === "pendingActivation") return "Approve";
    if (status === "active") return "Deactivate";
    return "Reactivate";
  };

  const filteredProsumers = prosumers.filter((p) =>
    p.fullName.toLowerCase().includes(search.toLowerCase())
  );

  const totalCount = prosumers.length;
  const activeCount = prosumers.filter((p) => p.status === "active").length;
  const pendingCount = prosumers.filter((p) => p.status === "pendingActivation").length;
  const deactivatedCount = prosumers.filter((p) => p.status === "deactivated").length;

  const chartData = [
    { name: "Active", count: activeCount },
    { name: "Pending", count: pendingCount },
    { name: "Deactivated", count: deactivatedCount },
  ];

  return (
    <div style={{ padding: "40px", fontFamily: "'Segoe UI', Arial, sans-serif", background: "#F7F5F1", minHeight: "100vh" }}>
      <div style={{ marginBottom: "24px" }}>
        <h2 style={{ margin: 0, color: "#1C1F1E" }}>Prosumer Management</h2>
        <p style={{ color: "#6B6862", margin: "4px 0 0 0" }}>Approve, edit, deactivate, or reactivate prosumer accounts, keyed by NIC.</p>
      </div>

      {/* Summary cards */}
      <div style={{ display: "flex", gap: "16px", marginBottom: "20px" }}>
        <SummaryCard icon="🔌" label="Total Prosumers" value={totalCount} bg="#E7EEF5" />
        <SummaryCard icon="✅" label="Active" value={activeCount} bg="#E5F3EA" />
        <SummaryCard icon="⏳" label="Pending Approval" value={pendingCount} bg="#FBF0DC" />
        <SummaryCard icon="🚫" label="Deactivated" value={deactivatedCount} bg="#F7E7E5" />
      </div>

      {/* Chart */}
      <div style={{ background: "#fff", border: "1px solid #E4E1DA", borderRadius: "14px", padding: "20px", marginBottom: "20px" }}>
        <h4 style={{ margin: "0 0 11px 0", color: "#1C1F1E" }}>Prosumer Status Overview</h4>
        <ResponsiveContainer width="100%" height={160}>
          <BarChart data={chartData}>
            <CartesianGrid strokeDasharray="3 3" stroke="#EFEDE7" />
            <XAxis dataKey="name" tick={{ fontSize: 12 }} />
            <YAxis allowDecimals={false} tick={{ fontSize: 12 }} />
            <Tooltip />
            <Bar dataKey="count" fill={accent} radius={[6, 6, 0, 0]} />
          </BarChart>
        </ResponsiveContainer>
      </div>

      {/* Search bar */}
      <div style={{ marginBottom: "16px" }}>
        <input
          type="text"
          placeholder="Search by name..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          style={{ ...input, width: "320px" }}
        />
      </div>

      <div style={{ background: "#fff", border: "1px solid #E4E1DA", borderRadius: "14px", overflow: "hidden" }}>
        <table style={{ width: "100%", borderCollapse: "collapse" }}>
          <thead>
            <tr style={{ background: "#F3F1EC", textAlign: "left" }}>
              <th style={th}>NIC</th>
              <th style={th}>Full Name</th>
              <th style={th}>Email</th>
              <th style={th}>Phone</th>
              <th style={th}>Status</th>
              <th style={th}></th>
            </tr>
          </thead>
          <tbody>
            {filteredProsumers.map((p) => {
              const s = statusInfo(p.status);
              return (
                <tr key={p.nic} style={{ borderBottom: "1px solid #EFEDE7" }}>
                  <td style={{ ...td, fontFamily: "monospace" }}>{p.nic}</td>
                  <td style={td}>{p.fullName}</td>
                  <td style={{ ...td, color: "#6B6862" }}>{p.email}</td>
                  <td style={{ ...td, color: "#6B6862" }}>{p.phone}</td>
                  <td style={td}>
                    <span style={badge(s.bg, s.color)}>{s.label}</span>
                  </td>
                  <td style={{ ...td, textAlign: "right", whiteSpace: "nowrap" }}>
                    <button onClick={() => openEdit(p)} style={btnSmall}>Edit</button>
                    <button onClick={() => advance(p.nic, p.status)} style={btnSmall}>{actionLabel(p.status)}</button>
                    <button onClick={() => remove(p.nic)} style={{ ...btnSmall, color: "#B14A3C", borderColor: "#E8B4AC" }}>Delete</button>
                  </td>
                </tr>
              );
            })}
            {filteredProsumers.length === 0 && (
              <tr><td colSpan="6" style={{ ...td, textAlign: "center", color: "#6B6862" }}>No prosumers found.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {showModal && (
        <div style={overlay}>
          <form onSubmit={handleSubmit} style={modalCard}>
            <h3 style={{ margin: "0 0 6px 0" }}>Edit prosumer</h3>
            <p style={{ margin: "0 0 18px 0", color: "#6B6862", fontSize: "13px" }}>NIC: {form.nic}</p>

            <label style={label}>Full name</label>
            <input required value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} style={input} />

            <label style={label}>Email</label>
            <input required type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} style={input} />

            <label style={label}>Phone</label>
            <input required value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} style={input} />

            <label style={label}>New password (leave blank to keep current)</label>
            <input type="password" value={form.passwordHash} onChange={(e) => setForm({ ...form, passwordHash: e.target.value })} style={input} />

            <div style={{ display: "flex", gap: "12px", justifyContent: "flex-end", marginTop: "8px" }}>
              <button type="button" onClick={closeModal} style={btnSecondary}>Cancel</button>
              <button type="submit" style={btnPrimary}>Save changes</button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}

function SummaryCard({ icon, label, value, bg }) {
  return (
    <div style={{ flex: 1, background: bg, borderRadius: "10px", padding: "10px 14px", display: "flex", alignItems: "center", gap: "10px" }}>
      <div style={{ fontSize: "18px" }}>{icon}</div>
      <div>
        <div style={{ fontSize: "18px", fontWeight: 700, color: "#1C1F1E", lineHeight: 1.1 }}>{value}</div>
        <div style={{ fontSize: "11px", color: "#6B6862" }}>{label}</div>
      </div>
    </div>
  );
}
const th = { padding: "14px 20px", fontSize: "12px", fontWeight: 600, color: "#6B6862", textTransform: "uppercase", letterSpacing: "0.04em" };
const td = { padding: "14px 20px", fontSize: "14px" };
const badge = (bg, color) => ({ padding: "4px 10px", borderRadius: "999px", fontSize: "12px", fontWeight: 600, background: bg, color });
const btnPrimary = { background: accent, color: "#fff", border: "none", borderRadius: "10px", padding: "10px 18px", fontSize: "14px", fontWeight: 600, cursor: "pointer" };
const btnSecondary = { background: "none", border: "1px solid #D8D4CB", borderRadius: "10px", padding: "10px 18px", fontSize: "14px", fontWeight: 600, cursor: "pointer", color: "#3A3733" };
const btnSmall = { background: "none", border: "1px solid #D8D4CB", borderRadius: "8px", padding: "6px 12px", fontSize: "13px", fontWeight: 600, cursor: "pointer", color: "#3A3733", marginLeft: "6px" };
const overlay = { position: "fixed", inset: 0, background: "rgba(28,31,30,0.45)", display: "flex", alignItems: "center", justifyContent: "center" };
const modalCard = { width: "380px", background: "#fff", borderRadius: "16px", padding: "32px", boxSizing: "border-box" };
const label = { display: "block", fontSize: "13px", fontWeight: 600, color: "#3A3733", marginBottom: "6px", marginTop: "14px" };
const input = { width: "100%", boxSizing: "border-box", padding: "10px 12px", border: "1px solid #D8D4CB", borderRadius: "8px", fontSize: "14px", fontFamily: "inherit" };

export default Prosumers;