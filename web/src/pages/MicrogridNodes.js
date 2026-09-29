import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";

import { useNavigate } from "react-router-dom";
import api from "../api";

function MicrogridNodes() {
  const navigate = useNavigate();

  // =========================================================
  // STATE
  // =========================================================

  const [nodes, setNodes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [searchText, setSearchText] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");

  // Deactivate modal
  const [nodeToDeactivate, setNodeToDeactivate] = useState(null);
  const [deactivating, setDeactivating] = useState(false);
  const [deactivateError, setDeactivateError] = useState("");

  // Reactivate
  const [reactivatingId, setReactivatingId] = useState(null);

  // Success message
  const [successMessage, setSuccessMessage] = useState("");

  // =========================================================
  // HELPERS
  // =========================================================

  const getMongoId = (node) => {
    return (
      node?.id ??
      node?._id ??
      node?.Id ??
      ""
    );
  };

  const getNodeId = (node) => {
    return (
      node?.nodeId ??
      node?.nodeID ??
      node?.NodeId ??
      node?.NodeID ??
      "-"
    );
  };

  const getNodeName = (node) => {
    return (
      node?.nodeName ??
      node?.name ??
      node?.NodeName ??
      "-"
    );
  };

  const getLocation = (node) => {
    return (
      node?.location ??
      node?.Location ??
      "-"
    );
  };

  const getCapacity = (node) => {
    return (
      node?.capacityKWh ??
      node?.capacity ??
      node?.CapacityKWh ??
      node?.Capacity ??
      "-"
    );
  };

  const getBatterySlots = (node) => {
    return (
      node?.batterySlots ??
      node?.BatterySlots ??
      node?.availableBatterySlots ??
      "-"
    );
  };

  const getSchedule = (node) => {
    return (
      node?.schedule ??
      node?.Schedule ??
      node?.operatingSchedule ??
      node?.OperatingSchedule ??
      "-"
    );
  };

  const getStatus = (node) => {
    return String(
      node?.status ??
        node?.Status ??
        "active"
    ).toLowerCase();
  };

  // =========================================================
  // LOAD MICROGRID NODES
  // =========================================================

  const loadNodes = useCallback(async () => {
    try {
      setLoading(true);
      setError("");

      const response = await api.get("/microgridnodes");

      const nodeData = Array.isArray(response.data)
        ? response.data
        : response.data?.data ||
          response.data?.nodes ||
          [];

      setNodes(nodeData);
    } catch (err) {
      console.error(
        "Failed to load microgrid nodes:",
        err
      );

      if (err.response?.status === 401) {
        setError(
          "You are not authorized. Please log in again."
        );
      } else if (err.response?.status === 403) {
        setError(
          "You do not have permission to view microgrid nodes."
        );
      } else {
        setError(
          "Failed to load microgrid nodes. Please make sure the API is running."
        );
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadNodes();
  }, [loadNodes]);

  // =========================================================
  // SEARCH + FILTER
  // =========================================================

  const filteredNodes = useMemo(() => {
    const search = searchText
      .trim()
      .toLowerCase();

    return nodes.filter((node) => {
      const nodeId = String(
        getNodeId(node)
      ).toLowerCase();

      const nodeName = String(
        getNodeName(node)
      ).toLowerCase();

      const location = String(
        getLocation(node)
      ).toLowerCase();

      const status = getStatus(node);

      const matchesSearch =
        !search ||
        nodeId.includes(search) ||
        nodeName.includes(search) ||
        location.includes(search);

      const matchesStatus =
        statusFilter === "all" ||
        status === statusFilter;

      return matchesSearch && matchesStatus;
    });
  }, [nodes, searchText, statusFilter]);

  // =========================================================
  // SUMMARY COUNTS
  // =========================================================

  const totalNodes = nodes.length;

  const activeNodes = nodes.filter(
    (node) => getStatus(node) === "active"
  ).length;

  const inactiveNodes = nodes.filter(
    (node) => getStatus(node) === "inactive"
  ).length;

  // =========================================================
  // OPEN DEACTIVATE MODAL
  // =========================================================

  const openDeactivateModal = (node) => {
    setSuccessMessage("");
    setDeactivateError("");
    setNodeToDeactivate(node);
  };

  // =========================================================
  // CLOSE DEACTIVATE MODAL
  // =========================================================

  const closeDeactivateModal = () => {
    if (deactivating) {
      return;
    }

    setNodeToDeactivate(null);
    setDeactivateError("");
  };

  // =========================================================
  // CONFIRM DEACTIVATION
  // =========================================================

  const confirmDeactivate = async () => {
    if (!nodeToDeactivate) {
      return;
    }

    const id = getMongoId(nodeToDeactivate);

    if (!id) {
      setDeactivateError(
        "Unable to determine the microgrid node ID."
      );
      return;
    }

    try {
      setDeactivating(true);
      setDeactivateError("");
      setSuccessMessage("");

      await api.patch(
        `/microgridnodes/${encodeURIComponent(
          id
        )}/deactivate`
      );

      const nodeName =
        getNodeName(nodeToDeactivate);

      setNodeToDeactivate(null);

      setSuccessMessage(
        `${nodeName} was deactivated successfully.`
      );

      await loadNodes();
    } catch (err) {
      console.error(
        "Failed to deactivate microgrid node:",
        err
      );

      if (err.response?.status === 409) {
        setDeactivateError(
          err.response?.data?.message ||
            "This microgrid node has active energy reservations and cannot be deactivated."
        );
      } else if (err.response?.status === 404) {
        setDeactivateError(
          err.response?.data?.message ||
            "Microgrid node not found."
        );
      } else if (err.response?.status === 400) {
        setDeactivateError(
          err.response?.data?.message ||
            "This microgrid node cannot be deactivated."
        );
      } else if (err.response?.status === 401) {
        setDeactivateError(
          "You are not authorized. Please log in again."
        );
      } else if (err.response?.status === 403) {
        setDeactivateError(
          "You do not have permission to deactivate this microgrid node."
        );
      } else {
        setDeactivateError(
          err.response?.data?.message ||
            "Failed to deactivate microgrid node. Please try again."
        );
      }
    } finally {
      setDeactivating(false);
    }
  };

  // =========================================================
  // REACTIVATE NODE
  // =========================================================

  const handleReactivate = async (node) => {
    const id = getMongoId(node);

    if (!id) {
      setError(
        "Unable to determine the microgrid node ID."
      );
      return;
    }

    try {
      setReactivatingId(id);
      setError("");
      setSuccessMessage("");

      await api.patch(
        `/microgridnodes/${encodeURIComponent(
          id
        )}/reactivate`
      );

      setSuccessMessage(
        `${getNodeName(
          node
        )} was reactivated successfully.`
      );

      await loadNodes();
    } catch (err) {
      console.error(
        "Failed to reactivate microgrid node:",
        err
      );

      setError(
        err.response?.data?.message ||
          "Failed to reactivate microgrid node."
      );
    } finally {
      setReactivatingId(null);
    }
  };

  // =========================================================
  // NAVIGATION
  // =========================================================

  const handleView = (node) => {
    const id = getMongoId(node);

    if (!id) {
      return;
    }

    navigate(
      `/microgrid-nodes/${encodeURIComponent(id)}`
    );
  };

  const handleEdit = (node) => {
    const id = getMongoId(node);

    if (!id) {
      return;
    }

    navigate(
      `/microgrid-nodes/${encodeURIComponent(
        id
      )}/edit`
    );
  };

  // =========================================================
  // UI
  // =========================================================

  return (
    <div
      className="container-fluid py-4 px-4"
      style={{
        backgroundColor: "#f7f5f1",
        minHeight: "calc(100vh - 70px)",
      }}
    >
      {/* =====================================================
          PAGE HEADER
      ====================================================== */}

      <div className="d-flex flex-wrap justify-content-between align-items-start gap-3 mb-4">
        <div>
          <h2 className="fw-bold mb-1">
            Microgrid Node Management
          </h2>

          <p className="text-secondary mb-0">
            Manage solar microgrid nodes, capacity,
            battery slots and operating schedules.
          </p>
        </div>

        <button
          type="button"
          className="btn btn-success d-flex align-items-center gap-2"
          onClick={() =>
            navigate("/microgrid-nodes/add")
          }
        >
          <i className="bi bi-plus-lg"></i>
          Add Microgrid Node
        </button>
      </div>

      {/* =====================================================
          SUCCESS MESSAGE
      ====================================================== */}

      {successMessage && (
        <div
          className="alert alert-success alert-dismissible fade show"
          role="alert"
        >
          <i className="bi bi-check-circle-fill me-2"></i>

          <strong>Success!</strong>{" "}
          {successMessage}

          <button
            type="button"
            className="btn-close"
            onClick={() =>
              setSuccessMessage("")
            }
            aria-label="Close"
          ></button>
        </div>
      )}

      {/* =====================================================
          ERROR MESSAGE
      ====================================================== */}

      {error && (
        <div
          className="alert alert-danger alert-dismissible fade show"
          role="alert"
        >
          <i className="bi bi-exclamation-triangle-fill me-2"></i>

          <strong>Error!</strong>{" "}
          {error}

          <button
            type="button"
            className="btn-close"
            onClick={() => setError("")}
            aria-label="Close"
          ></button>
        </div>
      )}

      {/* =====================================================
          SUMMARY CARDS
      ====================================================== */}

      <div className="row g-3 mb-4">
        {/* Total Nodes */}

        <div className="col-12 col-md-4">
          <div
            className="card border-0 h-100"
            style={{
              backgroundColor: "#cfe2ff",
            }}
          >
            <div className="card-body d-flex align-items-center gap-3 py-4">
              <div
                className="d-flex align-items-center justify-content-center"
                style={{
                  width: "42px",
                  height: "42px",
                  fontSize: "26px",
                }}
              >
                <i className="bi bi-diagram-3"></i>
              </div>

              <div>
                <h3 className="fw-bold mb-0">
                  {totalNodes}
                </h3>

                <div className="text-secondary">
                  Total Nodes
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Active Nodes */}

        <div className="col-12 col-md-4">
          <div
            className="card border-0 h-100"
            style={{
              backgroundColor: "#d1e7dd",
            }}
          >
            <div className="card-body d-flex align-items-center gap-3 py-4">
              <div
                className="d-flex align-items-center justify-content-center"
                style={{
                  width: "42px",
                  height: "42px",
                  fontSize: "26px",
                }}
              >
                <i className="bi bi-check-circle"></i>
              </div>

              <div>
                <h3 className="fw-bold mb-0">
                  {activeNodes}
                </h3>

                <div className="text-secondary">
                  Active Nodes
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Inactive Nodes */}

        <div className="col-12 col-md-4">
          <div
            className="card border-0 h-100"
            style={{
              backgroundColor: "#f8d7da",
            }}
          >
            <div className="card-body d-flex align-items-center gap-3 py-4">
              <div
                className="d-flex align-items-center justify-content-center"
                style={{
                  width: "42px",
                  height: "42px",
                  fontSize: "26px",
                }}
              >
                <i className="bi bi-slash-circle"></i>
              </div>

              <div>
                <h3 className="fw-bold mb-0">
                  {inactiveNodes}
                </h3>

                <div className="text-secondary">
                  Deactivated
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* =====================================================
          NODE STATUS OVERVIEW
      ====================================================== */}

      <div className="card shadow-sm border mb-4">
        <div className="card-body">
          <h5 className="fw-bold mb-4">
            Node Status Overview
          </h5>

          <div className="row g-4">
            {/* Active */}

            <div className="col-md-6">
              <div className="d-flex justify-content-between mb-2">
                <span className="fw-semibold">
                  Active Nodes
                </span>

                <span className="fw-bold">
                  {activeNodes}
                </span>
              </div>

              <div
                className="progress"
                style={{ height: "22px" }}
              >
                <div
                  className="progress-bar bg-success"
                  role="progressbar"
                  style={{
                    width:
                      totalNodes === 0
                        ? "0%"
                        : `${
                            (activeNodes /
                              totalNodes) *
                            100
                          }%`,
                  }}
                  aria-valuenow={activeNodes}
                  aria-valuemin="0"
                  aria-valuemax={totalNodes}
                >
                  {activeNodes}
                </div>
              </div>
            </div>

            {/* Inactive */}

            <div className="col-md-6">
              <div className="d-flex justify-content-between mb-2">
                <span className="fw-semibold">
                  Deactivated Nodes
                </span>

                <span className="fw-bold">
                  {inactiveNodes}
                </span>
              </div>

              <div
                className="progress"
                style={{ height: "22px" }}
              >
                <div
                  className="progress-bar bg-danger"
                  role="progressbar"
                  style={{
                    width:
                      totalNodes === 0
                        ? "0%"
                        : `${
                            (inactiveNodes /
                              totalNodes) *
                            100
                          }%`,
                  }}
                  aria-valuenow={inactiveNodes}
                  aria-valuemin="0"
                  aria-valuemax={totalNodes}
                >
                  {inactiveNodes}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* =====================================================
          SEARCH AND FILTER
      ====================================================== */}

      <div className="row g-2 mb-3">
        <div className="col-12 col-md-6 col-lg-5">
          <div className="input-group">
            <span className="input-group-text bg-white">
              <i className="bi bi-search"></i>
            </span>

            <input
              type="text"
              className="form-control"
              value={searchText}
              onChange={(event) =>
                setSearchText(event.target.value)
              }
              placeholder="Search by node ID, name or location..."
            />
          </div>
        </div>

        <div className="col-6 col-md-3 col-lg-2">
          <select
            className="form-select"
            value={statusFilter}
            onChange={(event) =>
              setStatusFilter(event.target.value)
            }
          >
            <option value="all">
              All Status
            </option>

            <option value="active">
              Active
            </option>

            <option value="inactive">
              Deactivated
            </option>
          </select>
        </div>

        <div className="col-6 col-md-3 col-lg-2">
          <button
            type="button"
            className="btn btn-outline-secondary w-100"
            onClick={loadNodes}
          >
            <i className="bi bi-arrow-clockwise me-2"></i>
            Refresh
          </button>
        </div>
      </div>

      {/* =====================================================
          TABLE
      ====================================================== */}

      <div className="card shadow-sm border">
        <div className="card-body p-0">
          {loading ? (
            <div className="text-center py-5">
              <div
                className="spinner-border text-success mb-3"
                role="status"
              >
                <span className="visually-hidden">
                  Loading...
                </span>
              </div>

              <p className="text-secondary mb-0">
                Loading microgrid nodes...
              </p>
            </div>
          ) : filteredNodes.length === 0 ? (
            <div className="text-center py-5">
              <i
                className="bi bi-inbox text-secondary"
                style={{
                  fontSize: "40px",
                }}
              ></i>

              <h5 className="mt-3">
                No microgrid nodes found
              </h5>

              <p className="text-secondary mb-0">
                Try changing your search or
                status filter.
              </p>
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
                      SCHEDULE
                    </th>

                    <th className="py-3">
                      STATUS
                    </th>

                    <th className="py-3 text-end pe-4">
                      ACTIONS
                    </th>
                  </tr>
                </thead>

                <tbody>
                  {filteredNodes.map(
                    (node, index) => {
                      const mongoId =
                        getMongoId(node);

                      const status =
                        getStatus(node);

                      const isActive =
                        status === "active";

                      const isReactivating =
                        reactivatingId ===
                        mongoId;

                      return (
                        <tr
                          key={
                            mongoId ||
                            getNodeId(node) ||
                            index
                          }
                        >
                          {/* Node ID */}

                          <td className="px-4 py-3">
                            <span className="fw-semibold">
                              {getNodeId(node)}
                            </span>
                          </td>

                          {/* Node Name */}

                          <td className="py-3">
                            <div className="d-flex align-items-center gap-2">
                              <div
                                className="d-flex align-items-center justify-content-center rounded-circle bg-success-subtle text-success"
                                style={{
                                  width: "34px",
                                  height: "34px",
                                  minWidth: "34px",
                                }}
                              >
                                <i className="bi bi-sun-fill"></i>
                              </div>

                              <span className="fw-medium">
                                {getNodeName(
                                  node
                                )}
                              </span>
                            </div>
                          </td>

                          {/* Location */}

                          <td className="py-3">
                            <i className="bi bi-geo-alt me-1 text-secondary"></i>

                            {getLocation(node)}
                          </td>

                          {/* Capacity */}

                          <td className="py-3">
                            <span className="fw-medium">
                              {getCapacity(node)}
                            </span>{" "}
                            kWh
                          </td>

                          {/* Battery Slots */}

                          <td className="py-3">
                            <span className="badge text-bg-light border text-dark">
                              <i className="bi bi-battery-charging me-1"></i>

                              {getBatterySlots(
                                node
                              )}
                            </span>
                          </td>

                          {/* Schedule */}

                          <td className="py-3">
                            <i className="bi bi-clock me-1 text-secondary"></i>

                            {getSchedule(node)}
                          </td>

                          {/* Status */}

                          <td className="py-3">
                            {isActive ? (
                              <span className="badge rounded-pill text-bg-success">
                                Active
                              </span>
                            ) : (
                              <span className="badge rounded-pill text-bg-secondary">
                                Deactivated
                              </span>
                            )}
                          </td>

                          {/* Actions */}

                          <td className="py-3 pe-4">
                            <div className="d-flex justify-content-end flex-wrap gap-1">
                              <button
                                type="button"
                                className="btn btn-sm btn-outline-secondary"
                                onClick={() =>
                                  handleView(
                                    node
                                  )
                                }
                              >
                                <i className="bi bi-eye me-1"></i>
                                View
                              </button>

                              <button
                                type="button"
                                className="btn btn-sm btn-outline-primary"
                                onClick={() =>
                                  handleEdit(
                                    node
                                  )
                                }
                              >
                                <i className="bi bi-pencil me-1"></i>
                                Edit
                              </button>

                              {isActive ? (
                                <button
                                  type="button"
                                  className="btn btn-sm btn-outline-danger"
                                  onClick={() =>
                                    openDeactivateModal(
                                      node
                                    )
                                  }
                                >
                                  <i className="bi bi-slash-circle me-1"></i>
                                  Deactivate
                                </button>
                              ) : (
                                <button
                                  type="button"
                                  className="btn btn-sm btn-outline-success"
                                  disabled={
                                    isReactivating
                                  }
                                  onClick={() =>
                                    handleReactivate(
                                      node
                                    )
                                  }
                                >
                                  {isReactivating ? (
                                    <>
                                      <span
                                        className="spinner-border spinner-border-sm me-1"
                                        aria-hidden="true"
                                      ></span>

                                      Reactivating...
                                    </>
                                  ) : (
                                    <>
                                      <i className="bi bi-arrow-counterclockwise me-1"></i>
                                      Reactivate
                                    </>
                                  )}
                                </button>
                              )}
                            </div>
                          </td>
                        </tr>
                      );
                    }
                  )}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      {/* =====================================================
          RESULT COUNT
      ====================================================== */}

      {!loading && (
        <div className="d-flex justify-content-between align-items-center mt-3">
          <small className="text-secondary">
            Showing {filteredNodes.length} of{" "}
            {totalNodes} microgrid nodes
          </small>
        </div>
      )}

      {/* =====================================================
          DEACTIVATE CONFIRMATION MODAL
      ====================================================== */}

      {nodeToDeactivate && (
        <>
          <div
            className="modal fade show"
            style={{
              display: "block",
              backgroundColor:
                "rgba(0, 0, 0, 0.45)",
            }}
            tabIndex="-1"
            role="dialog"
          >
            <div className="modal-dialog modal-dialog-centered">
              <div className="modal-content border-0 shadow">
                {/* Modal Header */}

                <div className="modal-header border-0 pb-0">
                  <div>
                    <div
                      className="d-flex align-items-center justify-content-center rounded-circle bg-danger-subtle text-danger mb-3"
                      style={{
                        width: "48px",
                        height: "48px",
                        fontSize: "22px",
                      }}
                    >
                      <i className="bi bi-exclamation-triangle-fill"></i>
                    </div>

                    <h4 className="modal-title fw-bold">
                      Deactivate Microgrid
                      Node?
                    </h4>
                  </div>

                  <button
                    type="button"
                    className="btn-close align-self-start"
                    onClick={
                      closeDeactivateModal
                    }
                    disabled={deactivating}
                    aria-label="Close"
                  ></button>
                </div>

                {/* Modal Body */}

                <div className="modal-body pt-3">
                  <p className="text-secondary">
                    You are about to deactivate{" "}
                    <strong className="text-dark">
                      {getNodeName(
                        nodeToDeactivate
                      )}
                    </strong>
                    .
                  </p>

                  <p className="text-secondary">
                    This node will no longer be
                    available for new energy
                    reservations.
                  </p>

                  <div
                    className="alert alert-warning"
                    role="alert"
                  >
                    <div className="d-flex gap-2">
                      <i className="bi bi-exclamation-circle-fill"></i>

                      <div>
                        <strong>
                          Important:
                        </strong>

                        <div className="mt-1">
                          A microgrid node
                          cannot be deactivated
                          when active energy
                          reservations exist.
                        </div>
                      </div>
                    </div>
                  </div>

                  {deactivateError && (
                    <div
                      className="alert alert-danger"
                      role="alert"
                    >
                      <strong>
                        Unable to deactivate
                        node
                      </strong>

                      <div className="mt-1">
                        {deactivateError}
                      </div>
                    </div>
                  )}
                </div>

                {/* Modal Footer */}

                <div className="modal-footer border-0">
                  <button
                    type="button"
                    className="btn btn-outline-secondary"
                    onClick={
                      closeDeactivateModal
                    }
                    disabled={deactivating}
                  >
                    Cancel
                  </button>

                  <button
                    type="button"
                    className="btn btn-danger"
                    onClick={
                      confirmDeactivate
                    }
                    disabled={deactivating}
                  >
                    {deactivating ? (
                      <>
                        <span
                          className="spinner-border spinner-border-sm me-2"
                          aria-hidden="true"
                        ></span>

                        Deactivating...
                      </>
                    ) : (
                      <>
                        <i className="bi bi-slash-circle me-2"></i>
                        Deactivate Node
                      </>
                    )}
                  </button>
                </div>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

export default MicrogridNodes;