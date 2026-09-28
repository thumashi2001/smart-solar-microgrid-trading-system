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

  // Loads all Backoffice and Grid Operator users from the API.
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

  // Creates a new user or saves changes to the one being edited.
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
    <div className="container-fluid p-4 p-md-5">
      {/* Header */}
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h2 className="h3 mb-1">User Management</h2>
          <p className="text-secondary mb-0">Manage Backoffice and Grid Operator accounts.</p>
        </div>
        <button onClick={openAdd} className="btn btn-success fw-semibold">
          <i className="bi bi-plus-lg me-1"></i>
          Add User
        </button>
      </div>

      {/* Summary cards */}
      <div className="row g-3 mb-4">
        <div className="col-12 col-md-4">
          <SummaryCard icon="bi-people" label="Total Users" value={totalCount} bg="bg-primary-subtle" />
        </div>
        <div className="col-12 col-md-4">
          <SummaryCard icon="bi-check-circle" label="Active Users" value={activeCount} bg="bg-success-subtle" />
        </div>
        <div className="col-12 col-md-4">
          <SummaryCard icon="bi-slash-circle" label="Deactivated" value={deactivatedCount} bg="bg-danger-subtle" />
        </div>
      </div>

      {/* Chart */}
      <div className="card shadow-sm mb-4">
        <div className="card-body">
          <h5 className="card-title mb-3">User Status Overview</h5>
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
      </div>

      {/* Search bar */}
      <div className="mb-3" style={{ maxWidth: "320px" }}>
        <input
          type="text"
          className="form-control"
          placeholder="Search by name..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      {/* Users table */}
      <div className="card shadow-sm">
        <div className="table-responsive">
          <table className="table table-hover align-middle mb-0">
            <thead className="table-light">
              <tr className="small text-uppercase text-secondary">
                <th className="px-4 py-3">Full Name</th>
                <th className="px-4 py-3">Email</th>
                <th className="px-4 py-3">Role</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody>
              {filteredUsers.map((u) => (
                <tr key={u.id}>
                  <td className="px-4 py-3">{u.fullName}</td>
                  <td className="px-4 py-3 text-secondary">{u.email}</td>
                  <td className="px-4 py-3">
                    <span className={`badge rounded-pill ${u.role === "Backoffice" ? "text-bg-warning" : "text-bg-info"}`}>
                      {u.role}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <span className={`badge rounded-pill ${u.status === "active" ? "text-bg-success" : "text-bg-danger"}`}>
                      {u.status === "active" ? "Active" : "Deactivated"}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-end text-nowrap">
                    <button onClick={() => openEdit(u)} className="btn btn-sm btn-outline-secondary ms-1">
                      Edit
                    </button>
                    {u.status === "active" ? (
                      <button onClick={() => deactivate(u.id)} className="btn btn-sm btn-outline-secondary ms-1">
                        Deactivate
                      </button>
                    ) : (
                      <button onClick={() => reactivate(u.id)} className="btn btn-sm btn-outline-secondary ms-1">
                        Reactivate
                      </button>
                    )}
                    <button onClick={() => remove(u.id)} className="btn btn-sm btn-outline-danger ms-1">
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
              {filteredUsers.length === 0 && (
                <tr>
                  <td colSpan="5" className="text-center text-secondary py-4">
                    No users found.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Add / Edit modal */}
      {showModal && (
        <>
          <div className="modal d-block" tabIndex="-1" role="dialog">
            <div className="modal-dialog modal-dialog-centered">
              <form onSubmit={handleSubmit} className="modal-content">
                <div className="modal-header">
                  <div>
                    <h5 className="modal-title">{editingUser ? "Edit user" : "Create user"}</h5>
                    <div className="small text-secondary">Backoffice and Grid Operator accounts only</div>
                  </div>
                  <button type="button" className="btn-close" aria-label="Close" onClick={closeModal}></button>
                </div>

                <div className="modal-body">
                  <div className="mb-3">
                    <label className="form-label fw-semibold small">Full name</label>
                    <input
                      required
                      className="form-control"
                      value={form.fullName}
                      onChange={(e) => setForm({ ...form, fullName: e.target.value })}
                    />
                  </div>

                  <div className="mb-3">
                    <label className="form-label fw-semibold small">Email</label>
                    <input
                      required
                      type="email"
                      className="form-control"
                      value={form.email}
                      onChange={(e) => setForm({ ...form, email: e.target.value })}
                    />
                  </div>

                  <div className="mb-3">
                    <label className="form-label fw-semibold small">Role</label>
                    <select
                      className="form-select"
                      value={form.role}
                      onChange={(e) => setForm({ ...form, role: e.target.value })}
                    >
                      <option>Backoffice</option>
                      <option>GridOperator</option>
                    </select>
                  </div>

                  <div className="mb-1">
                    <label className="form-label fw-semibold small">
                      {editingUser ? "New password (leave blank to keep current)" : "Temporary password"}
                    </label>
                    <input
                      type="password"
                      className="form-control"
                      value={form.passwordHash}
                      onChange={(e) => setForm({ ...form, passwordHash: e.target.value })}
                      required={!editingUser}
                    />
                  </div>
                </div>

                <div className="modal-footer">
                  <button type="button" onClick={closeModal} className="btn btn-outline-secondary">
                    Cancel
                  </button>
                  <button type="submit" className="btn btn-success fw-semibold">
                    {editingUser ? "Save changes" : "Create user"}
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

export default Users;