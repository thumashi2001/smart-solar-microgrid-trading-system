import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../api";

function MicrogridNodes() {
  const navigate = useNavigate();

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
  // LOAD NODES
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
  // OPEN DEACTIVATE CONFIRMATION
  // =========================================================

  const openDeactivateModal = (node) => {
    setSuccessMessage("");
    setDeactivateError("");
    setNodeToDeactivate(node);
  };

  // =========================================================
  // CLOSE DEACTIVATE CONFIRMATION
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
    <div style={styles.page}>
      {/* =================================================== */}
      {/* HEADER */}
      {/* =================================================== */}

      <div style={styles.header}>
        <div>
          <h1 style={styles.title}>
            Microgrid Nodes
          </h1>

          <p style={styles.subtitle}>
            Manage solar microgrid nodes,
            capacity, battery slots and
            operating schedules.
          </p>
        </div>

        <button
          type="button"
          onClick={() =>
            navigate("/microgrid-nodes/add")
          }
          style={styles.addButton}
        >
          + Add Microgrid Node
        </button>
      </div>

      {/* =================================================== */}
      {/* SUCCESS MESSAGE */}
      {/* =================================================== */}

      {successMessage && (
        <div style={styles.successBox}>
          <strong>Success</strong>

          <div style={{ marginTop: "4px" }}>
            {successMessage}
          </div>
        </div>
      )}

      {/* =================================================== */}
      {/* ERROR MESSAGE */}
      {/* =================================================== */}

      {error && (
        <div style={styles.errorBox}>
          <strong>Error</strong>

          <div style={{ marginTop: "4px" }}>
            {error}
          </div>
        </div>
      )}

      {/* =================================================== */}
      {/* SUMMARY CARDS */}
      {/* =================================================== */}

      <div style={styles.summaryGrid}>
        <div style={styles.summaryCard}>
          <div style={styles.summaryLabel}>
            TOTAL NODES
          </div>

          <div style={styles.summaryValue}>
            {totalNodes}
          </div>
        </div>

        <div style={styles.summaryCard}>
          <div style={styles.summaryLabel}>
            ACTIVE NODES
          </div>

          <div style={styles.summaryValueGreen}>
            {activeNodes}
          </div>
        </div>

        <div style={styles.summaryCard}>
          <div style={styles.summaryLabel}>
            INACTIVE NODES
          </div>

          <div style={styles.summaryValue}>
            {inactiveNodes}
          </div>
        </div>
      </div>

      {/* =================================================== */}
      {/* SEARCH AND FILTER */}
      {/* =================================================== */}

      <div style={styles.toolbar}>
        <input
          type="text"
          value={searchText}
          onChange={(event) =>
            setSearchText(event.target.value)
          }
          placeholder="Search by node ID, name or location..."
          style={styles.searchInput}
        />

        <select
          value={statusFilter}
          onChange={(event) =>
            setStatusFilter(event.target.value)
          }
          style={styles.filterSelect}
        >
          <option value="all">
            All Status
          </option>

          <option value="active">
            Active
          </option>

          <option value="inactive">
            Inactive
          </option>
        </select>

        <button
          type="button"
          onClick={loadNodes}
          style={styles.refreshButton}
        >
          Refresh
        </button>
      </div>

      {/* =================================================== */}
      {/* TABLE */}
      {/* =================================================== */}

      <div style={styles.tableCard}>
        {loading ? (
          <div style={styles.emptyState}>
            Loading microgrid nodes...
          </div>
        ) : filteredNodes.length === 0 ? (
          <div style={styles.emptyState}>
            No microgrid nodes found.
          </div>
        ) : (
          <div style={styles.tableWrapper}>
            <table style={styles.table}>
              <thead>
                <tr>
                  <th style={styles.th}>
                    NODE ID
                  </th>

                  <th style={styles.th}>
                    NODE NAME
                  </th>

                  <th style={styles.th}>
                    LOCATION
                  </th>

                  <th style={styles.th}>
                    CAPACITY
                  </th>

                  <th style={styles.th}>
                    BATTERY SLOTS
                  </th>

                  <th style={styles.th}>
                    SCHEDULE
                  </th>

                  <th style={styles.th}>
                    STATUS
                  </th>

                  <th style={styles.th}>
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
                        <td style={styles.td}>
                          <strong>
                            {getNodeId(node)}
                          </strong>
                        </td>

                        <td style={styles.td}>
                          {getNodeName(node)}
                        </td>

                        <td style={styles.td}>
                          {getLocation(node)}
                        </td>

                        <td style={styles.td}>
                          {getCapacity(node)} kWh
                        </td>

                        <td style={styles.td}>
                          {getBatterySlots(node)}
                        </td>

                        <td style={styles.td}>
                          {getSchedule(node)}
                        </td>

                        <td style={styles.td}>
                          <span
                            style={
                              isActive
                                ? styles.activeBadge
                                : styles.inactiveBadge
                            }
                          >
                            {isActive
                              ? "Active"
                              : "Inactive"}
                          </span>
                        </td>

                        <td style={styles.td}>
                          <div
                            style={
                              styles.actionGroup
                            }
                          >
                            <button
                              type="button"
                              onClick={() =>
                                handleView(node)
                              }
                              style={
                                styles.viewButton
                              }
                            >
                              View
                            </button>

                            <button
                              type="button"
                              onClick={() =>
                                handleEdit(node)
                              }
                              style={
                                styles.editButton
                              }
                            >
                              Edit
                            </button>

                            {isActive ? (
                              <button
                                type="button"
                                onClick={() =>
                                  openDeactivateModal(
                                    node
                                  )
                                }
                                style={
                                  styles.deactivateButton
                                }
                              >
                                Deactivate
                              </button>
                            ) : (
                              <button
                                type="button"
                                disabled={
                                  isReactivating
                                }
                                onClick={() =>
                                  handleReactivate(
                                    node
                                  )
                                }
                                style={{
                                  ...styles.reactivateButton,
                                  opacity:
                                    isReactivating
                                      ? 0.6
                                      : 1,
                                }}
                              >
                                {isReactivating
                                  ? "Reactivating..."
                                  : "Reactivate"}
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

      {/* =================================================== */}
      {/* DEACTIVATE CONFIRMATION MODAL */}
      {/* =================================================== */}

      {nodeToDeactivate && (
        <div style={styles.modalOverlay}>
          <div style={styles.modal}>
            {/* Warning icon */}
            <div style={styles.warningIcon}>
              !
            </div>

            <h2 style={styles.modalTitle}>
              Deactivate Microgrid Node?
            </h2>

            <p style={styles.modalText}>
              You are about to deactivate{" "}
              <strong>
                {getNodeName(
                  nodeToDeactivate
                )}
              </strong>
              .
            </p>

            <p style={styles.modalText}>
              This node will no longer be
              available for new energy
              reservations.
            </p>

            {/* Assignment business-rule information */}
            <div style={styles.warningBox}>
              <strong>
                Important:
              </strong>{" "}
              A microgrid node cannot be
              deactivated when active energy
              reservations exist.
            </div>

            {/* API error */}
            {deactivateError && (
              <div style={styles.modalError}>
                <strong>
                  Unable to deactivate node
                </strong>

                <div
                  style={{
                    marginTop: "5px",
                  }}
                >
                  {deactivateError}
                </div>
              </div>
            )}

            <div style={styles.modalActions}>
              <button
                type="button"
                onClick={
                  closeDeactivateModal
                }
                disabled={deactivating}
                style={styles.cancelButton}
              >
                Cancel
              </button>

              <button
                type="button"
                onClick={
                  confirmDeactivate
                }
                disabled={deactivating}
                style={{
                  ...styles.confirmDeactivateButton,
                  opacity: deactivating
                    ? 0.65
                    : 1,
                  cursor: deactivating
                    ? "not-allowed"
                    : "pointer",
                }}
              >
                {deactivating
                  ? "Deactivating..."
                  : "Deactivate Node"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// =========================================================
// STYLES
// =========================================================

const styles = {
  page: {
    padding: "32px",
    background: "#F7F5F1",
    minHeight: "calc(100vh - 71px)",
    boxSizing: "border-box",
  },

  header: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "flex-start",
    gap: "20px",
    marginBottom: "24px",
  },

  title: {
    margin: 0,
    color: "#111827",
    fontSize: "30px",
    fontWeight: 700,
  },

  subtitle: {
    marginTop: "7px",
    marginBottom: 0,
    color: "#6B6862",
    fontSize: "14px",
  },

  addButton: {
    border: "none",
    borderRadius: "8px",
    padding: "11px 17px",
    background: "#1E7A4D",
    color: "#FFFFFF",
    cursor: "pointer",
    fontSize: "14px",
    fontWeight: 600,
  },

  summaryGrid: {
    display: "grid",
    gridTemplateColumns:
      "repeat(auto-fit, minmax(200px, 1fr))",
    gap: "16px",
    marginBottom: "20px",
  },

  summaryCard: {
    background: "#FFFFFF",
    border: "1px solid #DEDAD3",
    borderRadius: "10px",
    padding: "20px",
  },

  summaryLabel: {
    color: "#77736D",
    fontSize: "12px",
    fontWeight: 600,
  },

  summaryValue: {
    color: "#111827",
    fontSize: "27px",
    fontWeight: 700,
    marginTop: "7px",
  },

  summaryValueGreen: {
    color: "#1E7A4D",
    fontSize: "27px",
    fontWeight: 700,
    marginTop: "7px",
  },

  toolbar: {
    display: "flex",
    flexWrap: "wrap",
    gap: "10px",
    marginBottom: "16px",
  },

  searchInput: {
    flex: 1,
    minWidth: "280px",
    padding: "11px 13px",
    border: "1px solid #D7D3CB",
    borderRadius: "8px",
    background: "#FFFFFF",
    fontSize: "14px",
    outline: "none",
  },

  filterSelect: {
    minWidth: "145px",
    padding: "10px 12px",
    border: "1px solid #D7D3CB",
    borderRadius: "8px",
    background: "#FFFFFF",
    fontSize: "14px",
    outline: "none",
  },

  refreshButton: {
    padding: "10px 16px",
    border: "1px solid #D7D3CB",
    borderRadius: "8px",
    background: "#FFFFFF",
    cursor: "pointer",
    fontWeight: 600,
  },

  tableCard: {
    background: "#FFFFFF",
    border: "1px solid #DEDAD3",
    borderRadius: "12px",
    overflow: "hidden",
  },

  tableWrapper: {
    overflowX: "auto",
  },

  table: {
    width: "100%",
    borderCollapse: "collapse",
  },

  th: {
    padding: "14px 15px",
    textAlign: "left",
    background: "#F7F5F1",
    color: "#69655F",
    fontSize: "11px",
    fontWeight: 700,
    borderBottom: "1px solid #DEDAD3",
    whiteSpace: "nowrap",
  },

  td: {
    padding: "15px",
    color: "#2B2B2B",
    fontSize: "13px",
    borderBottom: "1px solid #EEEAE4",
    verticalAlign: "middle",
  },

  activeBadge: {
    display: "inline-block",
    padding: "5px 10px",
    borderRadius: "20px",
    background: "#E7F7ED",
    color: "#18794E",
    fontSize: "12px",
    fontWeight: 600,
  },

  inactiveBadge: {
    display: "inline-block",
    padding: "5px 10px",
    borderRadius: "20px",
    background: "#F1F1F1",
    color: "#6B6862",
    fontSize: "12px",
    fontWeight: 600,
  },

  actionGroup: {
    display: "flex",
    alignItems: "center",
    gap: "6px",
    whiteSpace: "nowrap",
  },

  viewButton: {
    padding: "7px 10px",
    border: "1px solid #D7D3CB",
    borderRadius: "6px",
    background: "#FFFFFF",
    color: "#333333",
    cursor: "pointer",
    fontSize: "12px",
    fontWeight: 600,
  },

  editButton: {
    padding: "7px 10px",
    border: "1px solid #BFDCCB",
    borderRadius: "6px",
    background: "#EEF8F2",
    color: "#176B45",
    cursor: "pointer",
    fontSize: "12px",
    fontWeight: 600,
  },

  deactivateButton: {
    padding: "7px 10px",
    border: "1px solid #F4B8B4",
    borderRadius: "6px",
    background: "#FFF5F4",
    color: "#B42318",
    cursor: "pointer",
    fontSize: "12px",
    fontWeight: 600,
  },

  reactivateButton: {
    padding: "7px 10px",
    border: "1px solid #BFDCCB",
    borderRadius: "6px",
    background: "#EEF8F2",
    color: "#176B45",
    cursor: "pointer",
    fontSize: "12px",
    fontWeight: 600,
  },

  emptyState: {
    padding: "45px",
    textAlign: "center",
    color: "#77736D",
    fontSize: "14px",
  },

  successBox: {
    padding: "13px 16px",
    marginBottom: "18px",
    border: "1px solid #BFDCCB",
    borderRadius: "8px",
    background: "#EEF8F2",
    color: "#176B45",
    fontSize: "13px",
  },

  errorBox: {
    padding: "13px 16px",
    marginBottom: "18px",
    border: "1px solid #FECACA",
    borderRadius: "8px",
    background: "#FEF3F2",
    color: "#B42318",
    fontSize: "13px",
  },

  // =======================================================
  // MODAL
  // =======================================================

  modalOverlay: {
    position: "fixed",
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    zIndex: 9999,
    background: "rgba(0, 0, 0, 0.45)",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    padding: "20px",
  },

  modal: {
    width: "100%",
    maxWidth: "500px",
    background: "#FFFFFF",
    borderRadius: "14px",
    padding: "28px",
    boxShadow:
      "0 20px 60px rgba(0, 0, 0, 0.25)",
  },

  warningIcon: {
    width: "46px",
    height: "46px",
    borderRadius: "50%",
    background: "#FFF0EE",
    color: "#B42318",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    fontSize: "25px",
    fontWeight: 800,
    marginBottom: "16px",
  },

  modalTitle: {
    marginTop: 0,
    marginBottom: "12px",
    color: "#111827",
    fontSize: "22px",
  },

  modalText: {
    color: "#5F5B55",
    fontSize: "14px",
    lineHeight: 1.6,
    marginTop: "7px",
    marginBottom: "7px",
  },

  warningBox: {
    marginTop: "18px",
    padding: "13px 14px",
    border: "1px solid #F4D5A6",
    borderRadius: "8px",
    background: "#FFF8E8",
    color: "#76520A",
    fontSize: "13px",
    lineHeight: 1.5,
  },

  modalError: {
    marginTop: "14px",
    padding: "13px 14px",
    border: "1px solid #FECACA",
    borderRadius: "8px",
    background: "#FEF3F2",
    color: "#B42318",
    fontSize: "13px",
  },

  modalActions: {
    display: "flex",
    justifyContent: "flex-end",
    gap: "10px",
    marginTop: "24px",
  },

  cancelButton: {
    padding: "10px 16px",
    border: "1px solid #D7D3CB",
    borderRadius: "8px",
    background: "#FFFFFF",
    color: "#333333",
    cursor: "pointer",
    fontWeight: 600,
  },

  confirmDeactivateButton: {
    padding: "10px 16px",
    border: "none",
    borderRadius: "8px",
    background: "#B42318",
    color: "#FFFFFF",
    cursor: "pointer",
    fontWeight: 600,
  },
};

export default MicrogridNodes;