import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import api from "../api";

function MicrogridNodeDetails() {
  const navigate = useNavigate();
  const { id } = useParams();

  const [node, setNode] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const loadNode = async () => {
      try {
        setLoading(true);
        setError("");

        // Use the existing working list endpoint.
        const response = await api.get("/microgridnodes");

        const nodes = Array.isArray(response.data)
          ? response.data
          : response.data?.data || response.data?.nodes || [];

        // Support the common ID field names used by the current project.
        const selectedNode = nodes.find((item) => {
          const itemId =
            item.id ??
            item._id ??
            item.nodeId ??
            item.nodeID ??
            item.NodeId ??
            item.NodeID;

          return String(itemId) === String(id);
        });

        if (!selectedNode) {
          setError("Microgrid node could not be found.");
          setNode(null);
          return;
        }

        setNode(selectedNode);
      } catch (err) {
        console.error("Load microgrid node error:", err);

        if (err.response?.status === 401) {
          setError("You are not authorized. Please log in again.");
        } else if (err.response?.status === 403) {
          setError("You do not have permission to view this node.");
        } else {
          setError(
            "Unable to load microgrid node details. Please make sure the API is running."
          );
        }
      } finally {
        setLoading(false);
      }
    };

    loadNode();
  }, [id]);

  const getValue = (...values) => {
    const value = values.find(
      (item) => item !== undefined && item !== null && item !== ""
    );

    return value ?? "—";
  };

  const nodeId = node
    ? getValue(
        node.nodeId,
        node.nodeID,
        node.NodeId,
        node.NodeID,
        node.id,
        node._id
      )
    : "";

  const nodeName = node
    ? getValue(node.nodeName, node.name, node.NodeName)
    : "";

  const description = node
    ? getValue(node.description, node.Description)
    : "";

  const location = node
    ? getValue(node.location, node.Location)
    : "";

  const latitude = node
    ? getValue(node.latitude, node.Latitude)
    : "";

  const longitude = node
    ? getValue(node.longitude, node.Longitude)
    : "";

  const capacity = node
    ? getValue(
        node.capacityKWh,
        node.capacity,
        node.CapacityKWh,
        node.Capacity
      )
    : "";

  const batterySlots = node
    ? getValue(
        node.batterySlots,
        node.BatterySlots,
        node.availableBatterySlots
      )
    : "";

  const schedule = node
    ? getValue(
        node.schedule,
        node.Schedule,
        node.operatingSchedule,
        node.OperatingSchedule
      )
    : "";

  const status = node
    ? getValue(node.status, node.Status)
    : "";

  const isActive =
    String(status).toLowerCase() === "active";

  const pageStyle = {
    padding: "32px",
    maxWidth: "1200px",
    margin: "0 auto",
  };

  const cardStyle = {
    background: "#FFFFFF",
    border: "1px solid #E3E0D9",
    borderRadius: "12px",
    padding: "24px",
  };

  const labelStyle = {
    fontSize: "12px",
    color: "#77736D",
    marginBottom: "6px",
    fontWeight: "600",
    textTransform: "uppercase",
    letterSpacing: "0.3px",
  };

  const valueStyle = {
    fontSize: "15px",
    color: "#1C1F1E",
    fontWeight: "600",
    wordBreak: "break-word",
  };

  const infoBoxStyle = {
    background: "#FAF9F6",
    border: "1px solid #ECE9E2",
    borderRadius: "9px",
    padding: "16px",
  };

  if (loading) {
    return (
      <div style={pageStyle}>
        <div
          style={{
            ...cardStyle,
            textAlign: "center",
            padding: "50px",
          }}
        >
          <div
            style={{
              fontSize: "18px",
              fontWeight: "600",
              marginBottom: "8px",
            }}
          >
            Loading microgrid node...
          </div>

          <div
            style={{
              color: "#77736D",
              fontSize: "14px",
            }}
          >
            Please wait while the node information is retrieved.
          </div>
        </div>
      </div>
    );
  }

  if (error || !node) {
    return (
      <div style={pageStyle}>
        <div
          style={{
            background: "#FDECEC",
            color: "#B42318",
            border: "1px solid #F5C2C0",
            padding: "16px",
            borderRadius: "10px",
            marginBottom: "20px",
          }}
        >
          {error || "Microgrid node could not be found."}
        </div>

        <button
          type="button"
          onClick={() => navigate("/microgrid-nodes")}
          style={{
            background: "#1E7A4D",
            color: "#FFFFFF",
            border: "none",
            borderRadius: "8px",
            padding: "11px 18px",
            cursor: "pointer",
            fontWeight: "600",
          }}
        >
          ← Back to Microgrid Nodes
        </button>
      </div>
    );
  }

  return (
    <div style={pageStyle}>
      {/* Breadcrumb */}
      <div
        style={{
          color: "#77736D",
          fontSize: "13px",
          marginBottom: "10px",
        }}
      >
        Microgrid Nodes / Node Details
      </div>

      {/* Page Header */}
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
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: "12px",
              marginBottom: "7px",
            }}
          >
            <h1
              style={{
                margin: 0,
                fontSize: "30px",
                color: "#111111",
              }}
            >
              {nodeName}
            </h1>

            <span
              style={{
                background: isActive ? "#E1F3E8" : "#FDE8E7",
                color: isActive ? "#157347" : "#B42318",
                padding: "5px 11px",
                borderRadius: "20px",
                fontSize: "12px",
                fontWeight: "700",
              }}
            >
              {String(status).charAt(0).toUpperCase() +
                String(status).slice(1)}
            </span>
          </div>

          <p
            style={{
              margin: 0,
              color: "#6B6862",
              fontSize: "14px",
            }}
          >
            View complete information about this solar microgrid node.
          </p>
        </div>

        <div
          style={{
            display: "flex",
            gap: "10px",
          }}
        >
          <button
            type="button"
            onClick={() => navigate("/microgrid-nodes")}
            style={{
              background: "#FFFFFF",
              border: "1px solid #D5D1C9",
              color: "#333333",
              borderRadius: "8px",
              padding: "10px 16px",
              cursor: "pointer",
              fontWeight: "600",
            }}
          >
            ← Back to Nodes
          </button>

          <button
            type="button"
            onClick={() =>
              navigate(`/microgrid-nodes/${id}/edit`)
            }
            style={{
              background: "#1E7A4D",
              border: "none",
              color: "#FFFFFF",
              borderRadius: "8px",
              padding: "10px 18px",
              cursor: "pointer",
              fontWeight: "600",
            }}
          >
            Edit Node
          </button>
        </div>
      </div>

      {/* Overview */}
      <div
        style={{
          ...cardStyle,
          marginBottom: "20px",
        }}
      >
        <h2
          style={{
            margin: "0 0 6px 0",
            fontSize: "18px",
            color: "#1C1F1E",
          }}
        >
          Node Overview
        </h2>

        <p
          style={{
            margin: "0 0 22px 0",
            color: "#77736D",
            fontSize: "13px",
          }}
        >
          Basic identification and location information.
        </p>

        <div
          style={{
            display: "grid",
            gridTemplateColumns:
              "repeat(auto-fit, minmax(220px, 1fr))",
            gap: "16px",
          }}
        >
          <div style={infoBoxStyle}>
            <div style={labelStyle}>Node ID</div>
            <div style={valueStyle}>{nodeId}</div>
          </div>

          <div style={infoBoxStyle}>
            <div style={labelStyle}>Node Name</div>
            <div style={valueStyle}>{nodeName}</div>
          </div>

          <div style={infoBoxStyle}>
            <div style={labelStyle}>Location</div>
            <div style={valueStyle}>{location}</div>
          </div>

          <div style={infoBoxStyle}>
            <div style={labelStyle}>Status</div>

            <div
              style={{
                ...valueStyle,
                color: isActive ? "#157347" : "#B42318",
              }}
            >
              {String(status).charAt(0).toUpperCase() +
                String(status).slice(1)}
            </div>
          </div>
        </div>

        <div
          style={{
            marginTop: "18px",
          }}
        >
          <div style={labelStyle}>Description</div>

          <div
            style={{
              background: "#FAF9F6",
              border: "1px solid #ECE9E2",
              borderRadius: "9px",
              padding: "16px",
              color: "#454843",
              fontSize: "14px",
              lineHeight: "1.6",
            }}
          >
            {description}
          </div>
        </div>
      </div>

      {/* Technical Information */}
      <div
        style={{
          ...cardStyle,
          marginBottom: "20px",
        }}
      >
        <h2
          style={{
            margin: "0 0 6px 0",
            fontSize: "18px",
            color: "#1C1F1E",
          }}
        >
          Capacity & Storage
        </h2>

        <p
          style={{
            margin: "0 0 22px 0",
            color: "#77736D",
            fontSize: "13px",
          }}
        >
          Energy capacity and battery storage information.
        </p>

        <div
          style={{
            display: "grid",
            gridTemplateColumns: "1fr 1fr",
            gap: "18px",
          }}
        >
          <div style={infoBoxStyle}>
            <div style={labelStyle}>Energy Capacity</div>

            <div
              style={{
                fontSize: "24px",
                color: "#1E7A4D",
                fontWeight: "700",
              }}
            >
              {capacity} kWh
            </div>
          </div>

          <div style={infoBoxStyle}>
            <div style={labelStyle}>Battery Slots</div>

            <div
              style={{
                fontSize: "24px",
                color: "#1E7A4D",
                fontWeight: "700",
              }}
            >
              {batterySlots}
            </div>
          </div>
        </div>
      </div>

      {/* GPS Information */}
      <div
        style={{
          ...cardStyle,
          marginBottom: "20px",
        }}
      >
        <h2
          style={{
            margin: "0 0 6px 0",
            fontSize: "18px",
            color: "#1C1F1E",
          }}
        >
          GPS Location
        </h2>

        <p
          style={{
            margin: "0 0 22px 0",
            color: "#77736D",
            fontSize: "13px",
          }}
        >
          Geographic coordinates stored for this microgrid node.
        </p>

        <div
          style={{
            display: "grid",
            gridTemplateColumns: "1fr 1fr",
            gap: "18px",
          }}
        >
          <div style={infoBoxStyle}>
            <div style={labelStyle}>Latitude</div>
            <div style={valueStyle}>{latitude}</div>
          </div>

          <div style={infoBoxStyle}>
            <div style={labelStyle}>Longitude</div>
            <div style={valueStyle}>{longitude}</div>
          </div>
        </div>

        <div
          style={{
            marginTop: "18px",
            padding: "14px 16px",
            background: "#EEF7F1",
            border: "1px solid #CFE6D6",
            borderRadius: "9px",
            color: "#275A3B",
            fontSize: "13px",
          }}
        >
          <strong>Location data:</strong> These coordinates can be used by
          the mobile application when displaying nearby microgrid nodes.
        </div>
      </div>

      {/* Operating Information */}
      <div
        style={{
          ...cardStyle,
          marginBottom: "24px",
        }}
      >
        <h2
          style={{
            margin: "0 0 6px 0",
            fontSize: "18px",
            color: "#1C1F1E",
          }}
        >
          Operating Information
        </h2>

        <p
          style={{
            margin: "0 0 22px 0",
            color: "#77736D",
            fontSize: "13px",
          }}
        >
          Current operating schedule and node availability.
        </p>

        <div
          style={{
            display: "grid",
            gridTemplateColumns: "1fr 1fr",
            gap: "18px",
          }}
        >
          <div style={infoBoxStyle}>
            <div style={labelStyle}>Operating Schedule</div>
            <div style={valueStyle}>{schedule}</div>
          </div>

          <div style={infoBoxStyle}>
            <div style={labelStyle}>Current Status</div>

            <div
              style={{
                ...valueStyle,
                color: isActive ? "#157347" : "#B42318",
              }}
            >
              {isActive ? "Active" : "Inactive"}
            </div>
          </div>
        </div>
      </div>

      {/* Bottom actions */}
      <div
        style={{
          display: "flex",
          justifyContent: "flex-end",
          gap: "12px",
          paddingBottom: "30px",
        }}
      >
        <button
          type="button"
          onClick={() => navigate("/microgrid-nodes")}
          style={{
            background: "#FFFFFF",
            color: "#333333",
            border: "1px solid #D5D1C9",
            borderRadius: "8px",
            padding: "11px 20px",
            cursor: "pointer",
            fontWeight: "600",
          }}
        >
          Back
        </button>

        <button
          type="button"
          onClick={() =>
            navigate(`/microgrid-nodes/${id}/edit`)
          }
          style={{
            background: "#1E7A4D",
            color: "#FFFFFF",
            border: "none",
            borderRadius: "8px",
            padding: "11px 22px",
            cursor: "pointer",
            fontWeight: "600",
          }}
        >
          Edit Microgrid Node
        </button>
      </div>
    </div>
  );
}

export default MicrogridNodeDetails;