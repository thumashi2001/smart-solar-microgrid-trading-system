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

  // Loads all prosumers from the API.
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

  // Saves the edited prosumer, keyed by NIC.
  const handleSubmit = async (e) => {
    e.preventDefault();
    await api.put(`/prosumers/${editingProsumer.nic}`, { ...form, status: editingProsumer.status });
    setShowModal(false);
    loadProsumers();
  };

  // Approves a pending prosumer, or toggles active and deactivated.
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

  // Label and Bootstrap badge colour for each status.
  const statusInfo = (status) => {
    if (status === "pendingActivation") return { label: "Pending", badge: "text-bg-warning" };
    if (status === "active") return { label: "Active", badge: "text-bg-success" };
    return { label: "Deactivated", badge: "text-bg-danger" };
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
    <div className="container-fluid p-4 p-md-5">
      {/* Header */}
      <div className="mb-4">
        <h2 className="h3 mb-1">Prosumer Management</h2>
        <p className="text-secondary mb-0">
          Approve, edit, deactivate, or reactivate prosumer accounts, keyed by NIC.
        </p>
      </div>

      {/* Summary cards */}
      <div className="row g-3 mb-4">
        <div className="col-6 col-lg-3">
          <SummaryCard icon="bi-plug" label="Total Prosumers" value={totalCount} bg="bg-primary-subtle" />
        </div>
        <div className="col-6 col-lg-3">
          <SummaryCard icon="bi-check-circle" label="Active" value={activeCount} bg="bg-success-subtle" />
        </div>
        <div className="col-6 col-lg-3">
          <SummaryCard icon="bi-hourglass-split" label="Pending Approval" value={pendingCount} bg="bg-warning-subtle" />
        </div>
        <div className="col-6 col-lg-3">
          <SummaryCard icon="bi-slash-circle" label="Deactivated" value={deactivatedCount} bg="bg-danger-subtle" />
        </div>
      </div>

      {/* Chart */}
      <div className="card shadow-sm mb-4">
        <div className="card-body">
          <h5 className="card-title mb-3">Prosumer Status Overview</h5>
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

      {/* Prosumers table */}
      <div className="card shadow-sm">
        <div className="table-responsive">
          <table className="table table-hover align-middle mb-0">
            <thead className="table-light">
              <tr className="small text-uppercase text-secondary">
                <th className="px-4 py-3">NIC</th>
                <th className="px-4 py-3">Full Name</th>
                <th className="px-4 py-3">Email</th>
                <th className="px-4 py-3">Phone</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody>
              {filteredProsumers.map((p) => {
                const s = statusInfo(p.status);
                return (
                  <tr key={p.nic}>
                    <td className="px-4 py-3 font-monospace">{p.nic}</td>
                    <td className="px-4 py-3">{p.fullName}</td>
                    <td className="px-4 py-3 text-secondary">{p.email}</td>
                    <td className="px-4 py-3 text-secondary">{p.phone}</td>
                    <td className="px-4 py-3">
                      <span className={`badge rounded-pill ${s.badge}`}>{s.label}</span>
                    </td>
                    <td className="px-4 py-3 text-end text-nowrap">
                      <button onClick={() => openEdit(p)} className="btn btn-sm btn-outline-secondary ms-1">
                        Edit
                      </button>
                      <button
                        onClick={() => advance(p.nic, p.status)}
                        className="btn btn-sm btn-outline-secondary ms-1"
                      >
                        {actionLabel(p.status)}
                      </button>
                      <button onClick={() => remove(p.nic)} className="btn btn-sm btn-outline-danger ms-1">
                        Delete
                      </button>
                    </td>
                  </tr>
                );
              })}
              {filteredProsumers.length === 0 && (
                <tr>
                  <td colSpan="6" className="text-center text-secondary py-4">
                    No prosumers found.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Edit modal */}
      {showModal && (
        <>
          <div className="modal d-block" tabIndex="-1" role="dialog">
            <div className="modal-dialog modal-dialog-centered">
              <form onSubmit={handleSubmit} className="modal-content">
                <div className="modal-header">
                  <div>
                    <h5 className="modal-title">Edit prosumer</h5>
                    <div className="small text-secondary">NIC: {form.nic}</div>
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
                    <label className="form-label fw-semibold small">Phone</label>
                    <input
                      required
                      className="form-control"
                      value={form.phone}
                      onChange={(e) => setForm({ ...form, phone: e.target.value })}
                    />
                  </div>

                  <div className="mb-1">
                    <label className="form-label fw-semibold small">
                      New password (leave blank to keep current)
                    </label>
                    <input
                      type="password"
                      className="form-control"
                      value={form.passwordHash}
                      onChange={(e) => setForm({ ...form, passwordHash: e.target.value })}
                    />
                  </div>
                </div>

                <div className="modal-footer">
                  <button type="button" onClick={closeModal} className="btn btn-outline-secondary">
                    Cancel
                  </button>
                  <button type="submit" className="btn btn-success fw-semibold">
                    Save changes
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

export default Prosumers;