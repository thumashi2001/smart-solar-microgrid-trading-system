import { useCallback, useEffect, useMemo, useState } from "react";
import api from "../api";

function MicrogridNodes() {
  const [nodes, setNodes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");

  // =========================
  // Load Microgrid Nodes
  // =========================
  const fetchNodes = useCallback(async () => {
    try {
      setLoading(true);
      setError("");

      const response = await api.get("/microgridnodes");

      // Supports either:
      // [ ... ]
      // or { data: [ ... ] }
      const nodeData = Array.isArray(response.data)
        ? response.data
        : response.data?.data || [];

      setNodes(nodeData);
    } catch (err) {
      console.error("Error loading microgrid nodes:", err);

      if (err.response) {
        setError(
          err.response.data?.message ||
            `Failed to load microgrid nodes. Server returned ${err.response.status}.`
        );
      } else if (err.request) {
        setError(
          "Cannot connect to the API. Make sure the C# Web API is running."
        );
      } else {
        setError("Failed to load microgrid nodes.");
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchNodes();
  }, [fetchNodes]);

  // =========================
  // Summary values
  // =========================
  const totalNodes = nodes.length;

  const activeNodes = nodes.filter(
    (node) => String(node.status || "").toLowerCase() === "active"
  ).length;

  const inactiveNodes = nodes.filter(
    (node) => String(node.status || "").toLowerCase() === "inactive"
  ).length;

  // =========================
  // Search + Filter
  // =========================
  const filteredNodes = useMemo(() => {
    const search = searchTerm.trim().toLowerCase();

    return nodes.filter((node) => {
      const status = String(node.status || "").toLowerCase();

      const matchesStatus =
        statusFilter === "all" || status === statusFilter;

      const matchesSearch =
        !search ||
        String(node.nodeId || "").toLowerCase().includes(search) ||
        String(node.nodeName || "").toLowerCase().includes(search) ||
        String(node.location || "").toLowerCase().includes(search);

      return matchesStatus && matchesSearch;
    });
  }, [nodes, searchTerm, statusFilter]);

  // =========================
  // UI Helpers
  // =========================
  const getStatusStyle = (status) => {
    const normalizedStatus = String(status || "").toLowerCase();

    if (normalizedStatus === "active") {
      return {
        background: "#DFF3E7",
        color: "#167345",
      };
    }

    return {
      background: "#FBE4E4",
      color: "#B42318",
    };
  };

  const cardStyle = {
    background: "#fff",
    border: "1px solid #E6E2DB",
    borderRadius: "12px",
    padding: "18px 20px",
    flex: 1,
    minWidth: "190px",
  };

  const actionButtonStyle = {
    background: "#fff",
    border: "1px solid #D7D3CC",
    borderRadius: "7px",
    padding: "7px 12px",
    cursor: "pointer",
    fontSize: "13px",
    fontWeight: 500,
    color: "#333",
  };

  return (
    <div
      style={{
        padding: "36px",
        color: "#1C1F1E",
      }}
    >
      {/* =========================
          Header
      ========================== */}
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "flex-start",
          gap: "20px",
          marginBottom: "28px",
        }}
      >
        <div>
          <h1
            style={{
              margin: 0,
              fontSize: "28px",
              fontWeight: 700,
            }}
          >
            Microgrid Nodes
          </h1>

          <p
            style={{
              margin: "7px 0 0",
              color: "#6B6862",
              fontSize: "14px",
            }}
          >
            Manage solar microgrid nodes, capacity, battery slots and
            operating schedules.
          </p>
        </div>

        <button
          type="button"
          style={{
            background: "#1E7A4D",
            color: "#fff",
            border: "none",
            borderRadius: "9px",
            padding: "11px 18px",
            fontSize: "14px",
            fontWeight: 600,
            cursor: "pointer",
          }}
          onClick={() => {
            alert("Add Microgrid Node form will be added next.");
          }}
        >
          + Add Microgrid Node
        </button>
      </div>

      {/* =========================
          Summary Cards
      ========================== */}
      <div
        style={{
          display: "flex",
          gap: "16px",
          flexWrap: "wrap",
          marginBottom: "26px",
        }}
      >
        <div style={{ ...cardStyle, background: "#EAF1F8" }}>
          <div
            style={{
              fontSize: "13px",
              color: "#5D6670",
              marginBottom: "7px",
            }}
          >
            Total Nodes
          </div>

          <div
            style={{
              fontSize: "25px",
              fontWeight: 700,
            }}
          >
            {totalNodes}
          </div>
        </div>

        <div style={{ ...cardStyle, background: "#E5F4EA" }}>
          <div
            style={{
              fontSize: "13px",
              color: "#52705D",
              marginBottom: "7px",
            }}
          >
            Active Nodes
          </div>

          <div
            style={{
              fontSize: "25px",
              fontWeight: 700,
              color: "#167345",
            }}
          >
            {activeNodes}
          </div>
        </div>

        <div style={{ ...cardStyle, background: "#FBE9E7" }}>
          <div
            style={{
              fontSize: "13px",
              color: "#7C5D59",
              marginBottom: "7px",
            }}
          >
            Inactive Nodes
          </div>

          <div
            style={{
              fontSize: "25px",
              fontWeight: 700,
              color: "#B42318",
            }}
          >
            {inactiveNodes}
          </div>
        </div>
      </div>

      {/* =========================
          Error Message
      ========================== */}
      {error && (
        <div
          style={{
            background: "#FDECEC",
            border: "1px solid #F2B8B5",
            color: "#B42318",
            padding: "14px 16px",
            borderRadius: "9px",
            marginBottom: "20px",
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
            gap: "15px",
          }}
        >
          <span>{error}</span>

          <button
            type="button"
            onClick={fetchNodes}
            style={{
              border: "1px solid #B42318",
              background: "#fff",
              color: "#B42318",
              borderRadius: "6px",
              padding: "6px 12px",
              cursor: "pointer",
              fontWeight: 600,
            }}
          >
            Retry
          </button>
        </div>
      )}

      {/* =========================
          Search / Filters
      ========================== */}
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          gap: "12px",
          flexWrap: "wrap",
          marginBottom: "16px",
        }}
      >
        <input
          type="text"
          placeholder="Search by node ID, name or location..."
          value={searchTerm}
          onChange={(event) => setSearchTerm(event.target.value)}
          style={{
            width: "330px",
            maxWidth: "100%",
            border: "1px solid #D7D3CC",
            borderRadius: "8px",
            padding: "10px 13px",
            outline: "none",
            background: "#fff",
            fontSize: "14px",
          }}
        />

        <div
          style={{
            display: "flex",
            gap: "10px",
          }}
        >
          <select
            value={statusFilter}
            onChange={(event) => setStatusFilter(event.target.value)}
            style={{
              border: "1px solid #D7D3CC",
              borderRadius: "8px",
              padding: "10px 13px",
              background: "#fff",
              fontSize: "14px",
              cursor: "pointer",
            }}
          >
            <option value="all">All Statuses</option>
            <option value="active">Active</option>
            <option value="inactive">Inactive</option>
          </select>

          <button
            type="button"
            onClick={fetchNodes}
            style={{
              background: "#fff",
              border: "1px solid #D7D3CC",
              borderRadius: "8px",
              padding: "9px 14px",
              cursor: "pointer",
              fontSize: "14px",
              fontWeight: 500,
            }}
          >
            Refresh
          </button>
        </div>
      </div>

      {/* =========================
          Table
      ========================== */}
      <div
        style={{
          background: "#fff",
          border: "1px solid #E4E1DA",
          borderRadius: "12px",
          overflowX: "auto",
        }}
      >
        {loading ? (
          <div
            style={{
              padding: "55px",
              textAlign: "center",
              color: "#6B6862",
            }}
          >
            Loading microgrid nodes...
          </div>
        ) : filteredNodes.length === 0 ? (
          <div
            style={{
              padding: "55px",
              textAlign: "center",
              color: "#6B6862",
            }}
          >
            {nodes.length === 0
              ? "No microgrid nodes found."
              : "No nodes match your search or filter."}
          </div>
        ) : (
          <table
            style={{
              width: "100%",
              borderCollapse: "collapse",
              minWidth: "1050px",
            }}
          >
            <thead>
              <tr
                style={{
                  background: "#F3F1EC",
                  textAlign: "left",
                }}
              >
                <th style={tableHeaderStyle}>NODE ID</th>
                <th style={tableHeaderStyle}>NODE NAME</th>
                <th style={tableHeaderStyle}>LOCATION</th>
                <th style={tableHeaderStyle}>CAPACITY</th>
                <th style={tableHeaderStyle}>BATTERY SLOTS</th>
                <th style={tableHeaderStyle}>SCHEDULE</th>
                <th style={tableHeaderStyle}>STATUS</th>
                <th style={tableHeaderStyle}>ACTIONS</th>
              </tr>
            </thead>

            <tbody>
              {filteredNodes.map((node) => (
                <tr
                  key={node.id || node._id || node.nodeId}
                  style={{
                    borderTop: "1px solid #ECE9E3",
                  }}
                >
                  <td style={tableCellStyle}>
                    <strong>{node.nodeId || "-"}</strong>
                  </td>

                  <td style={tableCellStyle}>
                    {node.nodeName || "-"}
                  </td>

                  <td style={tableCellStyle}>
                    {node.location || "-"}
                  </td>

                  <td style={tableCellStyle}>
                    {node.capacityKWh !== undefined &&
                    node.capacityKWh !== null
                      ? `${node.capacityKWh} kWh`
                      : "-"}
                  </td>

                  <td style={tableCellStyle}>
                    {node.batterySlots ?? "-"}
                  </td>

                  <td style={tableCellStyle}>
                    {node.schedule || "-"}
                  </td>

                  <td style={tableCellStyle}>
                    <span
                      style={{
                        ...getStatusStyle(node.status),
                        padding: "5px 10px",
                        borderRadius: "20px",
                        fontSize: "12px",
                        fontWeight: 600,
                        textTransform: "capitalize",
                      }}
                    >
                      {node.status || "Unknown"}
                    </span>
                  </td>

                  <td style={tableCellStyle}>
                    <div
                      style={{
                        display: "flex",
                        gap: "7px",
                        whiteSpace: "nowrap",
                      }}
                    >
                      <button
                        type="button"
                        style={actionButtonStyle}
                        onClick={() => {
                          alert(
                            `View node: ${
                              node.nodeName || node.nodeId
                            }`
                          );
                        }}
                      >
                        View
                      </button>

                      <button
                        type="button"
                        style={actionButtonStyle}
                        onClick={() => {
                          alert(
                            `Edit node: ${
                              node.nodeName || node.nodeId
                            }`
                          );
                        }}
                      >
                        Edit
                      </button>

                      {String(node.status || "").toLowerCase() ===
                      "active" ? (
                        <button
                          type="button"
                          style={{
                            ...actionButtonStyle,
                            color: "#B42318",
                            borderColor: "#E8B5B0",
                          }}
                          onClick={() => {
                            alert(
                              `Deactivate node: ${
                                node.nodeName || node.nodeId
                              }`
                            );
                          }}
                        >
                          Deactivate
                        </button>
                      ) : (
                        <button
                          type="button"
                          style={{
                            ...actionButtonStyle,
                            color: "#167345",
                            borderColor: "#A9D7BC",
                          }}
                          onClick={() => {
                            alert(
                              `Reactivate node: ${
                                node.nodeName || node.nodeId
                              }`
                            );
                          }}
                        >
                          Reactivate
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {!loading && !error && (
        <div
          style={{
            marginTop: "12px",
            color: "#77736D",
            fontSize: "13px",
          }}
        >
          Showing {filteredNodes.length} of {nodes.length} microgrid node
          {nodes.length === 1 ? "" : "s"}.
        </div>
      )}
    </div>
  );
}

const tableHeaderStyle = {
  padding: "14px 16px",
  fontSize: "12px",
  fontWeight: 700,
  color: "#5D5A55",
  whiteSpace: "nowrap",
};

const tableCellStyle = {
  padding: "15px 16px",
  fontSize: "13px",
  color: "#343735",
  verticalAlign: "middle",
};

export default MicrogridNodes;