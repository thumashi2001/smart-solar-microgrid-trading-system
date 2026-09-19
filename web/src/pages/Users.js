import { useEffect, useState } from "react";
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid } from "recharts";
import api from "../api";

const accent = "#1E7A4D";

function Users() {
  const [users, setUsers] = useState([]);
  const [search, setSearch] = useState("");
  const [showModal, setShowModal] = useState(false);
  const [editingUser, setEditingUser] = useState(null);
  const [form, setForm] = useState({ email: "", passwordHash: "", fullName: "", role: "Backoffice" });

  const loadUsers = () => {
    api.get("/users").then((res) => setUsers(res.data));
  };

  useEffect(() => {
    loadUsers();
  }, []);

  const openAdd = () => {
    setEditingUser(null);
    setForm({ email: "", passwordHash: "", fullName: "", role: "Backoffice" });
    setShowModal(true);
  };

  const openEdit = (user) => {
    setEditingUser(user);
    setForm({ email: user.email, passwordHash: "", fullName: user.fullName, role: user.role });
    setShowModal(true);
  };

  const closeModal = () => setShowModal(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (editingUser) {
      await api.put(`/users/${editingUser.id}`, { ...form, status: editingUser.status });
    } else {
      await api.post("/users", { ...form, status: "active" });
    }
    setShowModal(false);
    loadUsers();
  };

  const deactivate = async (id) => {
    await api.patch(`/users/${id}/deactivate`);
    loadUsers();
  };

  const reactivate = async (id) => {
    await api.patch(`/users/${id}/reactivate`);
    loadUsers();
  };

  const remove = async (id) => {
    if (window.confirm("Delete this user permanently? This cannot be undone.")) {
      await api.delete(`/users/${id}`);
      loadUsers();
    }
  };

  const filteredUsers = users.filter((u) =>
    u.fullName.toLowerCase().includes(search.toLowerCase())
  );

  const totalCount = users.length;
  const activeCount = users.filter((u) => u.status === "active").length;
  const deactivatedCount = users.filter((u) => u.status === "deactivated").length;

  const chartData = [
    { name: "Active", count: activeCount },
    { name: "Deactivated", count: deactivatedCount },
  ];

  return (
    <div style={{ padding: "40px", fontFamily: "'Segoe UI', Arial, sans-serif", background: "#F7F5F1", minHeight: "100vh" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "24px" }}>
        <div>
          <h2 style={{ margin: 0, color: "#1C1F1E" }}>User Management</h2>
          <p style={{ color: "#6B6862", margin: "4px 0 0 0" }}>Manage Backoffice and Grid Operator accounts.</p>
        </div>
        <button onClick={openAdd} style={btnPrimary}>+ Add User</button>
      </div>

      {/* Summary cards */}
      <div style={{ display: "flex", gap: "16px", marginBottom: "20px" }}>
        <SummaryCard icon="👥" label="Total Users" value={totalCount} bg="#E7EEF5" />
        <SummaryCard icon="✅" label="Active Users" value={activeCount} bg="#E5F3EA" />
        <SummaryCard icon="🚫" label="Deactivated" value={deactivatedCount} bg="#F7E7E5" />
      </div>

      {/* Chart */}
      <div style={{ background: "#fff", border: "1px solid #E4E1DA", borderRadius: "14px", padding: "20px", marginBottom: "20px" }}>
        <h4 style={{ margin: "0 0 11px 0", color: "#1C1F1E" }}>User Status Overview</h4>
        <ResponsiveContainer width="100%" height={170}>
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
              <th style={th}>Full Name</th>
              <th style={th}>Email</th>
              <th style={th}>Role</th>
              <th style={th}>Status</th>
              <th style={th}></th>
            </tr>
          </thead>
          <tbody>
            {filteredUsers.map((u) => (
              <tr key={u.id} style={{ borderBottom: "1px solid #EFEDE7" }}>
                <td style={td}>{u.fullName}</td>
                <td style={{ ...td, color: "#6B6862" }}>{u.email}</td>
                <td style={td}>
                  <span style={badge(u.role === "Backoffice" ? "#FBEDE3" : "#E7EEF5", u.role === "Backoffice" ? "#9A5B2E" : "#33587A")}>
                    {u.role}
                  </span>
                </td>
                <td style={td}>
                  <span style={badge(u.status === "active" ? "#E5F3EA" : "#F7E7E5", u.status === "active" ? "#2E7D4F" : "#B14A3C")}>
                    {u.status === "active" ? "Active" : "Deactivated"}
                  </span>
                </td>
                <td style={{ ...td, textAlign: "right", whiteSpace: "nowrap" }}>
                  <button onClick={() => openEdit(u)} style={btnSmall}>Edit</button>
                  {u.status === "active" ? (
                    <button onClick={() => deactivate(u.id)} style={btnSmall}>Deactivate</button>
                  ) : (
                    <button onClick={() => reactivate(u.id)} style={btnSmall}>Reactivate</button>
                  )}
                  <button onClick={() => remove(u.id)} style={{ ...btnSmall, color: "#B14A3C", borderColor: "#E8B4AC" }}>Delete</button>
                </td>
              </tr>
            ))}
            {filteredUsers.length === 0 && (
              <tr><td colSpan="5" style={{ ...td, textAlign: "center", color: "#6B6862" }}>No users found.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {showModal && (
        <div style={overlay}>
          <form onSubmit={handleSubmit} style={modalCard}>
            <h3 style={{ margin: "0 0 6px 0" }}>{editingUser ? "Edit user" : "Create user"}</h3>
            <p style={{ margin: "0 0 18px 0", color: "#6B6862", fontSize: "13px" }}>Backoffice and Grid Operator accounts only</p>

            <label style={label}>Full name</label>
            <input required value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} style={input} />

            <label style={label}>Email</label>
            <input required type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} style={input} />

            <label style={label}>Role</label>
            <select value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value })} style={input}>
              <option>Backoffice</option>
              <option>GridOperator</option>
            </select>

            <label style={label}>{editingUser ? "New password (leave blank to keep current)" : "Temporary password"}</label>
            <input type="password" value={form.passwordHash} onChange={(e) => setForm({ ...form, passwordHash: e.target.value })} style={input} required={!editingUser} />

            <div style={{ display: "flex", gap: "12px", justifyContent: "flex-end", marginTop: "8px" }}>
              <button type="button" onClick={closeModal} style={btnSecondary}>Cancel</button>
              <button type="submit" style={btnPrimary}>{editingUser ? "Save changes" : "Create user"}</button>
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

export default Users;