import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../api";

function BackofficeDashboard() {
  const navigate = useNavigate();

  const [nodes, setNodes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  // Reservation values will be connected to Viman's API later.
  const [pendingReservations] = useState(0);
  const [approvedFutureReservations] = useState(0);

  useEffect(() => {
    loadDashboardData();
  }, []);

  const loadDashboardData = async () => {
    try {
      setLoading(true);
      setError("");

      const nodeResponse = await api.get("/microgridnodes");

      setNodes(
        Array.isArray(nodeResponse.data)
          ? nodeResponse.data
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

  const cardStyle = {
    background: "#fff",
    border: "1px solid #E3E0DA",
    borderRadius: "12px",
    padding: "22px",
    minHeight: "105px",
  };

  const cardLabelStyle = {
    color: "#6B6862",
    fontSize: "12px",
    fontWeight: 600,
    textTransform: "uppercase",
    marginBottom: "10px",
  };

  const cardValueStyle = {
    fontSize: "28px",
    fontWeight: 700,
    color: "#111827",
  };

  const actionButtonStyle = {
    border: "none",
    background: "#177A4B",
    color: "#fff",
    padding: "10px 16px",
    borderRadius: "8px",
    cursor: "pointer",
    fontWeight: 600,
  };

  if (loading) {
    return (
      <div style={{ padding: "40px" }}>
        <h2>Loading Dashboard...</h2>
        <p style={{ color: "#6B6862" }}>
          Retrieving Smart Solar system information.
        </p>
      </div>
    );
  }

  return (
    <div
      style={{
        padding: "36px",
        maxWidth: "1500px",
        margin: "0 auto",
      }}
    >
      {/* Page Header */}
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          marginBottom: "28px",
        }}
      >
        <div>
          <h1
            style={{
              margin: 0,
              fontSize: "30px",
              color: "#111827",
            }}
          >
            Backoffice Dashboard
          </h1>

          <p
            style={{
              marginTop: "8px",
              color: "#6B6862",
            }}
          >
            Overview of the Smart Solar Microgrid
            Trading System.
          </p>
        </div>

        <button
          type="button"
          onClick={loadDashboardData}
          style={{
            background: "#fff",
            border: "1px solid #D6D3CD",
            padding: "10px 18px",
            borderRadius: "8px",
            cursor: "pointer",
            fontWeight: 600,
          }}
        >
          ↻ Refresh
        </button>
      </div>

      {/* Error */}
      {error && (
        <div
          style={{
            background: "#FDECEC",
            border: "1px solid #F5B7B1",
            color: "#B42318",
            padding: "14px 18px",
            borderRadius: "8px",
            marginBottom: "22px",
          }}
        >
          {error}
        </div>
      )}

      {/* Main Statistics */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns:
            "repeat(auto-fit, minmax(210px, 1fr))",
          gap: "16px",
          marginBottom: "28px",
        }}
      >
        <div style={cardStyle}>
          <div style={cardLabelStyle}>
            Total Nodes
          </div>

          <div style={cardValueStyle}>
            {totalNodes}
          </div>

          <div
            style={{
              color: "#6B6862",
              fontSize: "13px",
              marginTop: "5px",
            }}
          >
            Registered microgrid nodes
          </div>
        </div>

        <div style={cardStyle}>
          <div style={cardLabelStyle}>
            Active Nodes
          </div>

          <div
            style={{
              ...cardValueStyle,
              color: "#177A4B",
            }}
          >
            {activeNodes}
          </div>

          <div
            style={{
              color: "#6B6862",
              fontSize: "13px",
              marginTop: "5px",
            }}
          >
            Currently operational
          </div>
        </div>

        <div style={cardStyle}>
          <div style={cardLabelStyle}>
            Inactive Nodes
          </div>

          <div style={cardValueStyle}>
            {inactiveNodes}
          </div>

          <div
            style={{
              color: "#6B6862",
              fontSize: "13px",
              marginTop: "5px",
            }}
          >
            Currently unavailable
          </div>
        </div>

        <div style={cardStyle}>
          <div style={cardLabelStyle}>
            Pending Reservations
          </div>

          <div
            style={{
              ...cardValueStyle,
              color: "#B7791F",
            }}
          >
            {pendingReservations}
          </div>

          <div
            style={{
              color: "#6B6862",
              fontSize: "13px",
              marginTop: "5px",
            }}
          >
            Waiting for processing
          </div>
        </div>

        <div style={cardStyle}>
          <div style={cardLabelStyle}>
            Approved Future Reservations
          </div>

          <div
            style={{
              ...cardValueStyle,
              color: "#177A4B",
            }}
          >
            {approvedFutureReservations}
          </div>

          <div
            style={{
              color: "#6B6862",
              fontSize: "13px",
              marginTop: "5px",
            }}
          >
            Upcoming approved bookings
          </div>
        </div>
      </div>

      {/* Capacity Overview */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns:
            "repeat(auto-fit, minmax(280px, 1fr))",
          gap: "16px",
          marginBottom: "28px",
        }}
      >
        <div style={cardStyle}>
          <div style={cardLabelStyle}>
            Total Energy Capacity
          </div>

          <div
            style={{
              fontSize: "26px",
              fontWeight: 700,
              color: "#177A4B",
            }}
          >
            {totalCapacity.toLocaleString()} kWh
          </div>

          <p
            style={{
              color: "#6B6862",
              fontSize: "13px",
              marginBottom: 0,
            }}
          >
            Combined capacity of all registered
            microgrid nodes.
          </p>
        </div>

        <div style={cardStyle}>
          <div style={cardLabelStyle}>
            Total Battery Slots
          </div>

          <div
            style={{
              fontSize: "26px",
              fontWeight: 700,
              color: "#177A4B",
            }}
          >
            {totalBatterySlots}
          </div>

          <p
            style={{
              color: "#6B6862",
              fontSize: "13px",
              marginBottom: 0,
            }}
          >
            Combined battery storage slots across
            all nodes.
          </p>
        </div>
      </div>

      {/* Quick Actions */}
      <div
        style={{
          background: "#fff",
          border: "1px solid #E3E0DA",
          borderRadius: "12px",
          padding: "22px",
          marginBottom: "28px",
        }}
      >
        <h2
          style={{
            marginTop: 0,
            marginBottom: "6px",
            fontSize: "19px",
          }}
        >
          Quick Actions
        </h2>

        <p
          style={{
            color: "#6B6862",
            marginTop: 0,
            marginBottom: "18px",
            fontSize: "14px",
          }}
        >
          Common Backoffice management operations.
        </p>

        <div
          style={{
            display: "flex",
            gap: "12px",
            flexWrap: "wrap",
          }}
        >
          <button
            type="button"
            onClick={() =>
              navigate("/microgrid-nodes")
            }
            style={actionButtonStyle}
          >
            Manage Microgrid Nodes
          </button>

          <button
            type="button"
            onClick={() =>
              navigate("/microgrid-nodes/add")
            }
            style={actionButtonStyle}
          >
            + Add Microgrid Node
          </button>

          <button
            type="button"
            onClick={() => navigate("/users")}
            style={actionButtonStyle}
          >
            Manage Users
          </button>

          <button
            type="button"
            onClick={() =>
              navigate("/prosumers")
            }
            style={actionButtonStyle}
          >
            Manage Prosumers
          </button>
        </div>
      </div>

      {/* Microgrid Node Overview */}
      <div
        style={{
          background: "#fff",
          border: "1px solid #E3E0DA",
          borderRadius: "12px",
          overflow: "hidden",
        }}
      >
        <div
          style={{
            padding: "22px",
            borderBottom: "1px solid #E3E0DA",
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
          }}
        >
          <div>
            <h2
              style={{
                margin: 0,
                fontSize: "19px",
              }}
            >
              Microgrid Node Overview
            </h2>

            <p
              style={{
                margin: "6px 0 0",
                color: "#6B6862",
                fontSize: "13px",
              }}
            >
              Current status of registered solar
              microgrid nodes.
            </p>
          </div>

          <button
            type="button"
            onClick={() =>
              navigate("/microgrid-nodes")
            }
            style={{
              background: "#fff",
              border: "1px solid #D6D3CD",
              padding: "9px 14px",
              borderRadius: "7px",
              cursor: "pointer",
              fontWeight: 600,
            }}
          >
            View All
          </button>
        </div>

        {nodes.length === 0 ? (
          <div
            style={{
              padding: "35px",
              textAlign: "center",
              color: "#6B6862",
            }}
          >
            No microgrid nodes are currently
            registered.
          </div>
        ) : (
          <div style={{ overflowX: "auto" }}>
            <table
              style={{
                width: "100%",
                borderCollapse: "collapse",
              }}
            >
              <thead>
                <tr
                  style={{
                    background: "#F7F5F1",
                    textAlign: "left",
                  }}
                >
                  {[
                    "Node ID",
                    "Node Name",
                    "Location",
                    "Capacity",
                    "Battery Slots",
                    "Status",
                    "Action",
                  ].map((heading) => (
                    <th
                      key={heading}
                      style={{
                        padding: "14px 16px",
                        fontSize: "12px",
                        color: "#5F5B55",
                        textTransform: "uppercase",
                        borderBottom:
                          "1px solid #E3E0DA",
                      }}
                    >
                      {heading}
                    </th>
                  ))}
                </tr>
              </thead>

              <tbody>
                {nodes.slice(0, 5).map((node) => {
                  const active =
                    node.status?.toLowerCase() ===
                    "active";

                  return (
                    <tr
                      key={node.id}
                      style={{
                        borderBottom:
                          "1px solid #EEEAE4",
                      }}
                    >
                      <td
                        style={{
                          padding: "15px 16px",
                          fontWeight: 600,
                        }}
                      >
                        {node.nodeId || "-"}
                      </td>

                      <td
                        style={{
                          padding: "15px 16px",
                        }}
                      >
                        {node.nodeName || "-"}
                      </td>

                      <td
                        style={{
                          padding: "15px 16px",
                        }}
                      >
                        {node.location || "-"}
                      </td>

                      <td
                        style={{
                          padding: "15px 16px",
                        }}
                      >
                        {node.capacityKWh || 0} kWh
                      </td>

                      <td
                        style={{
                          padding: "15px 16px",
                        }}
                      >
                        {node.batterySlots || 0}
                      </td>

                      <td
                        style={{
                          padding: "15px 16px",
                        }}
                      >
                        <span
                          style={{
                            display:
                              "inline-block",
                            padding: "5px 10px",
                            borderRadius: "20px",
                            background: active
                              ? "#E5F5EC"
                              : "#EFEFEF",
                            color: active
                              ? "#177A4B"
                              : "#5F5B55",
                            fontSize: "12px",
                            fontWeight: 600,
                          }}
                        >
                          {active
                            ? "Active"
                            : "Inactive"}
                        </span>
                      </td>

                      <td
                        style={{
                          padding: "15px 16px",
                        }}
                      >
                        <button
                          type="button"
                          onClick={() =>
                            navigate(
                              `/microgrid-nodes/${node.id}`
                            )
                          }
                          style={{
                            background: "#fff",
                            border:
                              "1px solid #D6D3CD",
                            padding: "7px 12px",
                            borderRadius: "6px",
                            cursor: "pointer",
                          }}
                        >
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