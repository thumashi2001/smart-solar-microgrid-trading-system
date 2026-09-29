import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../api";

function BackofficeDashboard() {
  const navigate = useNavigate();

  const [nodes, setNodes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  // Reservation values will be connected to the reservation API later
  const [pendingReservations] = useState(0);
  const [approvedFutureReservations] = useState(0);

  useEffect(() => {
    loadDashboardData();
  }, []);

  const loadDashboardData = async () => {
    try {
      setLoading(true);
      setError("");

      const response = await api.get("/microgridnodes");

      setNodes(
        Array.isArray(response.data)
          ? response.data
          : []
      );
    } catch (err) {
      console.error("Dashboard loading error:", err);

      setError(
        err.response?.data?.message ||
          "Unable to load dashboard data."
      );
    } finally {
      setLoading(false);
    }
  };

  // =========================
  // DASHBOARD CALCULATIONS
  // =========================

  const totalNodes = nodes.length;

  const activeNodes = nodes.filter(
    (node) =>
      node.status?.toLowerCase() === "active"
  ).length;

  const inactiveNodes = nodes.filter(
    (node) =>
      node.status?.toLowerCase() === "inactive"
  ).length;

  const totalCapacity = nodes.reduce(
    (total, node) =>
      total + Number(node.capacityKWh || 0),
    0
  );

  const totalBatterySlots = nodes.reduce(
    (total, node) =>
      total + Number(node.batterySlots || 0),
    0
  );

  // =========================
  // LOADING
  // =========================

  if (loading) {
    return (
      <div className="container-fluid py-5">
        <div className="text-center py-5">
          <div
            className="spinner-border text-success mb-3"
            role="status"
          >
            <span className="visually-hidden">
              Loading...
            </span>
          </div>

          <h4 className="fw-semibold">
            Loading Dashboard...
          </h4>

          <p className="text-muted">
            Retrieving Smart Solar system information.
          </p>
        </div>
      </div>
    );
  }

  // =========================
  // PAGE
  // =========================

  return (
    <div
      className="container-fluid px-4 px-lg-5 py-4"
      style={{
        backgroundColor: "#f7f6f2",
        minHeight: "100vh",
      }}
    >
      {/* =========================
          HEADER
      ========================= */}

      <div className="d-flex flex-column flex-md-row justify-content-between align-items-md-center gap-3 mb-4">
        <div>
          <h2
            className="fw-bold mb-1"
            style={{ color: "#102033" }}
          >
            Backoffice Dashboard
          </h2>

          <p className="text-muted mb-0">
            Overview of the Smart Solar Microgrid Trading
            System.
          </p>
        </div>

        <button
          type="button"
          className="btn btn-outline-secondary px-4 py-2"
          onClick={loadDashboardData}
        >
          <i className="bi bi-arrow-clockwise me-2"></i>
          Refresh
        </button>
      </div>

      {/* =========================
          ERROR MESSAGE
      ========================= */}

      {error && (
        <div
          className="alert alert-danger alert-dismissible fade show"
          role="alert"
        >
          <i className="bi bi-exclamation-triangle-fill me-2"></i>

          {error}

          <button
            type="button"
            className="btn-close"
            onClick={() => setError("")}
          ></button>
        </div>
      )}

      {/* =========================
          TOP STATISTIC CARDS
      ========================= */}

      <div className="row g-3 mb-4">

        {/* TOTAL NODES */}
        <div className="col-12 col-md-6 col-xl">
          <div
            className="card border-0 h-100"
            style={{
              backgroundColor: "#cfe2ff",
              borderRadius: "10px",
            }}
          >
            <div className="card-body p-4">
              <div className="d-flex align-items-center">

                <div
                  className="d-flex justify-content-center align-items-center me-3"
                  style={{
                    width: "50px",
                    height: "50px",
                    borderRadius: "10px",
                    backgroundColor: "rgba(255,255,255,0.55)",
                  }}
                >
                  <i
                    className="bi bi-diagram-3 fs-4"
                    style={{ color: "#0d3b66" }}
                  ></i>
                </div>

                <div>
                  <h2
                    className="fw-bold mb-0"
                    style={{ color: "#102033" }}
                  >
                    {totalNodes}
                  </h2>

                  <p className="text-muted mb-0">
                    Total Nodes
                  </p>
                </div>

              </div>
            </div>
          </div>
        </div>

        {/* ACTIVE NODES */}
        <div className="col-12 col-md-6 col-xl">
          <div
            className="card border-0 h-100"
            style={{
              backgroundColor: "#d1e7dd",
              borderRadius: "10px",
            }}
          >
            <div className="card-body p-4">
              <div className="d-flex align-items-center">

                <div
                  className="d-flex justify-content-center align-items-center me-3"
                  style={{
                    width: "50px",
                    height: "50px",
                    borderRadius: "10px",
                    backgroundColor: "rgba(255,255,255,0.55)",
                  }}
                >
                  <i
                    className="bi bi-check-circle fs-4"
                    style={{ color: "#087f5b" }}
                  ></i>
                </div>

                <div>
                  <h2
                    className="fw-bold mb-0"
                    style={{ color: "#087f5b" }}
                  >
                    {activeNodes}
                  </h2>

                  <p className="text-muted mb-0">
                    Active Nodes
                  </p>
                </div>

              </div>
            </div>
          </div>
        </div>

        {/* INACTIVE NODES */}
        <div className="col-12 col-md-6 col-xl">
          <div
            className="card border-0 h-100"
            style={{
              backgroundColor: "#f8d7da",
              borderRadius: "10px",
            }}
          >
            <div className="card-body p-4">
              <div className="d-flex align-items-center">

                <div
                  className="d-flex justify-content-center align-items-center me-3"
                  style={{
                    width: "50px",
                    height: "50px",
                    borderRadius: "10px",
                    backgroundColor: "rgba(255,255,255,0.55)",
                  }}
                >
                  <i
                    className="bi bi-slash-circle fs-4"
                    style={{ color: "#b02a37" }}
                  ></i>
                </div>

                <div>
                  <h2
                    className="fw-bold mb-0"
                    style={{ color: "#b02a37" }}
                  >
                    {inactiveNodes}
                  </h2>

                  <p className="text-muted mb-0">
                    Inactive Nodes
                  </p>
                </div>

              </div>
            </div>
          </div>
        </div>

        {/* PENDING RESERVATIONS */}
        <div className="col-12 col-md-6 col-xl">
          <div
            className="card border-0 h-100"
            style={{
              backgroundColor: "#fff3cd",
              borderRadius: "10px",
            }}
          >
            <div className="card-body p-4">
              <div className="d-flex align-items-center">

                <div
                  className="d-flex justify-content-center align-items-center me-3"
                  style={{
                    width: "50px",
                    height: "50px",
                    borderRadius: "10px",
                    backgroundColor: "rgba(255,255,255,0.55)",
                  }}
                >
                  <i
                    className="bi bi-clock fs-4"
                    style={{ color: "#997404" }}
                  ></i>
                </div>

                <div>
                  <h2
                    className="fw-bold mb-0"
                    style={{ color: "#997404" }}
                  >
                    {pendingReservations}
                  </h2>

                  <p className="text-muted mb-0">
                    Pending Reservations
                  </p>
                </div>

              </div>
            </div>
          </div>
        </div>

        {/* APPROVED RESERVATIONS */}
        <div className="col-12 col-md-6 col-xl">
          <div
            className="card border-0 h-100"
            style={{
              backgroundColor: "#cff4fc",
              borderRadius: "10px",
            }}
          >
            <div className="card-body p-4">
              <div className="d-flex align-items-center">

                <div
                  className="d-flex justify-content-center align-items-center me-3"
                  style={{
                    width: "50px",
                    height: "50px",
                    borderRadius: "10px",
                    backgroundColor: "rgba(255,255,255,0.55)",
                  }}
                >
                  <i
                    className="bi bi-calendar-check fs-4"
                    style={{ color: "#087990" }}
                  ></i>
                </div>

                <div>
                  <h2
                    className="fw-bold mb-0"
                    style={{ color: "#087990" }}
                  >
                    {approvedFutureReservations}
                  </h2>

                  <p className="text-muted mb-0">
                    Approved Reservations
                  </p>
                </div>

              </div>
            </div>
          </div>
        </div>

      </div>

      {/* =========================
          ENERGY INFORMATION
      ========================= */}

      <div className="row g-3 mb-4">

        {/* ENERGY CAPACITY */}
        <div className="col-12 col-lg-6">
          <div
            className="card border shadow-sm h-100"
            style={{ borderRadius: "10px" }}
          >
            <div className="card-body p-4">
              <div className="d-flex align-items-center">

                <div
                  className="d-flex align-items-center justify-content-center me-4"
                  style={{
                    width: "64px",
                    height: "64px",
                    borderRadius: "12px",
                    backgroundColor: "#e5f5ec",
                  }}
                >
                  <i
                    className="bi bi-lightning-charge-fill fs-2"
                    style={{ color: "#198754" }}
                  ></i>
                </div>

                <div>
                  <p className="text-muted small text-uppercase fw-semibold mb-1">
                    Total Energy Capacity
                  </p>

                  <h2
                    className="fw-bold mb-1"
                    style={{ color: "#087f5b" }}
                  >
                    {totalCapacity.toLocaleString()} kWh
                  </h2>

                  <small className="text-muted">
                    Combined capacity of all registered
                    microgrid nodes.
                  </small>
                </div>

              </div>
            </div>
          </div>
        </div>

        {/* BATTERY SLOTS */}
        <div className="col-12 col-lg-6">
          <div
            className="card border shadow-sm h-100"
            style={{ borderRadius: "10px" }}
          >
            <div className="card-body p-4">
              <div className="d-flex align-items-center">

                <div
                  className="d-flex align-items-center justify-content-center me-4"
                  style={{
                    width: "64px",
                    height: "64px",
                    borderRadius: "12px",
                    backgroundColor: "#e5f5ec",
                  }}
                >
                  <i
                    className="bi bi-battery-charging fs-2"
                    style={{ color: "#198754" }}
                  ></i>
                </div>

                <div>
                  <p className="text-muted small text-uppercase fw-semibold mb-1">
                    Total Battery Slots
                  </p>

                  <h2
                    className="fw-bold mb-1"
                    style={{ color: "#087f5b" }}
                  >
                    {totalBatterySlots}
                  </h2>

                  <small className="text-muted">
                    Combined battery storage slots across all
                    nodes.
                  </small>
                </div>

              </div>
            </div>
          </div>
        </div>

      </div>

      {/* =========================
          QUICK ACTIONS
      ========================= */}

      <div
        className="card border shadow-sm mb-4"
        style={{ borderRadius: "10px" }}
      >
        <div className="card-body p-4">

          <div className="d-flex align-items-center mb-2">
            <i
              className="bi bi-lightning-charge-fill me-2"
              style={{ color: "#198754" }}
            ></i>

            <h5 className="fw-bold mb-0">
              Quick Actions
            </h5>
          </div>

          <p className="text-muted small mb-3">
            Common Backoffice management operations.
          </p>

          <div className="d-flex flex-wrap gap-2">

            <button
              type="button"
              className="btn btn-success px-3"
              onClick={() =>
                navigate("/microgrid-nodes")
              }
            >
              <i className="bi bi-diagram-3 me-2"></i>
              Manage Microgrid Nodes
            </button>

            <button
              type="button"
              className="btn btn-success px-3"
              onClick={() =>
                navigate("/microgrid-nodes/add")
              }
            >
              <i className="bi bi-plus-lg me-2"></i>
              Add Microgrid Node
            </button>

            <button
              type="button"
              className="btn btn-success px-3"
              onClick={() => navigate("/users")}
            >
              <i className="bi bi-people me-2"></i>
              Manage Users
            </button>

            <button
              type="button"
              className="btn btn-success px-3"
              onClick={() => navigate("/prosumers")}
            >
              <i className="bi bi-person-check me-2"></i>
              Manage Prosumers
            </button>

          </div>
        </div>
      </div>

      {/* =========================
          MICROGRID NODE OVERVIEW
      ========================= */}

      <div
        className="card border shadow-sm"
        style={{ borderRadius: "10px" }}
      >

        {/* TABLE HEADER */}
        <div className="card-header bg-white p-4">
          <div className="d-flex flex-column flex-sm-row justify-content-between align-items-sm-center gap-3">

            <div>
              <div className="d-flex align-items-center mb-1">
                <i
                  className="bi bi-diagram-3 me-2"
                  style={{ color: "#198754" }}
                ></i>

                <h5 className="fw-bold mb-0">
                  Microgrid Node Overview
                </h5>
              </div>

              <p className="text-muted small mb-0">
                Current status of registered solar microgrid
                nodes.
              </p>
            </div>

            <button
              type="button"
              className="btn btn-outline-success px-4"
              onClick={() =>
                navigate("/microgrid-nodes")
              }
            >
              View All
            </button>

          </div>
        </div>

        {/* NO DATA */}
        {nodes.length === 0 ? (
          <div className="text-center py-5">

            <i
              className="bi bi-inbox fs-1"
              style={{ color: "#198754" }}
            ></i>

            <h6 className="fw-semibold mt-3">
              No Microgrid Nodes
            </h6>

            <p className="text-muted mb-3">
              No microgrid nodes are currently registered.
            </p>

            <button
              type="button"
              className="btn btn-success"
              onClick={() =>
                navigate("/microgrid-nodes/add")
              }
            >
              <i className="bi bi-plus-lg me-2"></i>
              Add Microgrid Node
            </button>

          </div>
        ) : (
          <div className="table-responsive">

            <table className="table table-hover align-middle mb-0">

              <thead className="table-light">
                <tr>
                  <th className="px-4 py-3">
                    NODE ID
                  </th>

                  <th className="py-3">
                    NODE NAME
                  </th>

                  <th className="py-3">
                    LOCATION
                  </th>

                  <th className="py-3">
                    CAPACITY
                  </th>

                  <th className="py-3">
                    BATTERY SLOTS
                  </th>

                  <th className="py-3">
                    STATUS
                  </th>

                  <th className="py-3">
                    ACTION
                  </th>
                </tr>
              </thead>

              <tbody>

                {nodes.slice(0, 5).map((node) => {

                  const active =
                    node.status?.toLowerCase() ===
                    "active";

                  return (
                    <tr key={node.id}>

                      {/* NODE ID */}
                      <td className="px-4 fw-semibold">
                        {node.nodeId || "-"}
                      </td>

                      {/* NAME */}
                      <td className="fw-medium">
                        {node.nodeName || "-"}
                      </td>

                      {/* LOCATION */}
                      <td>
                        <i
                          className="bi bi-geo-alt me-2"
                          style={{ color: "#198754" }}
                        ></i>

                        {node.location || "-"}
                      </td>

                      {/* CAPACITY */}
                      <td>
                        <i
                          className="bi bi-lightning-charge me-1"
                          style={{ color: "#198754" }}
                        ></i>

                        {node.capacityKWh || 0} kWh
                      </td>

                      {/* BATTERY */}
                      <td>
                        <i
                          className="bi bi-battery-half me-2"
                          style={{ color: "#198754" }}
                        ></i>

                        {node.batterySlots || 0}
                      </td>

                      {/* STATUS */}
                      <td>
                        <span
                          className={
                            active
                              ? "badge rounded-pill bg-success px-3 py-2"
                              : "badge rounded-pill bg-secondary px-3 py-2"
                          }
                        >
                          {active
                            ? "Active"
                            : "Inactive"}
                        </span>
                      </td>

                      {/* ACTION */}
                      <td>
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-success"
                          onClick={() =>
                            navigate(
                              `/microgrid-nodes/${node.id}`
                            )
                          }
                        >
                          <i className="bi bi-eye me-1"></i>
                          View
                        </button>
                      </td>

                    </tr>
                  );
                })}

              </tbody>
            </table>

          </div>
        )}

      </div>
    </div>
  );
}

export default BackofficeDashboard;