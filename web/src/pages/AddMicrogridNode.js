import { useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../api";

function AddMicrogridNode() {
  const navigate = useNavigate();

  const [formData, setFormData] = useState({
    nodeName: "",
    description: "",
    location: "",
    latitude: "",
    longitude: "",
    capacityKWh: "",
    batterySlots: "",
    schedule: "",
    status: "active",
  });

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  // Submit new microgrid node
  const handleSubmit = async (event) => {
    event.preventDefault();
    setError("");

    // Validation
    if (formData.nodeName.trim() === "") {
      setError("Node name is required.");
      return;
    }

    if (formData.location.trim() === "") {
      setError("Location is required.");
      return;
    }

    if (formData.latitude === "") {
      setError("Latitude is required.");
      return;
    }

    if (
      Number(formData.latitude) < -90 ||
      Number(formData.latitude) > 90
    ) {
      setError("Latitude must be between -90 and 90.");
      return;
    }

    if (formData.longitude === "") {
      setError("Longitude is required.");
      return;
    }

    if (
      Number(formData.longitude) < -180 ||
      Number(formData.longitude) > 180
    ) {
      setError("Longitude must be between -180 and 180.");
      return;
    }

    if (
      formData.capacityKWh === "" ||
      Number(formData.capacityKWh) <= 0
    ) {
      setError("Capacity must be greater than 0.");
      return;
    }

    if (
      formData.batterySlots === "" ||
      Number(formData.batterySlots) < 0
    ) {
      setError("Battery slots cannot be negative.");
      return;
    }

    if (formData.schedule.trim() === "") {
      setError("Operating schedule is required.");
      return;
    }

    // Object sent to the C# Web API
    const newNode = {
      nodeName: formData.nodeName.trim(),
      description: formData.description.trim(),
      location: formData.location.trim(),
      latitude: Number(formData.latitude),
      longitude: Number(formData.longitude),
      capacityKWh: Number(formData.capacityKWh),
      batterySlots: Number(formData.batterySlots),
      schedule: formData.schedule.trim(),
      status: formData.status,
    };

    try {
      setLoading(true);

      await api.post("/microgridnodes", newNode);

      alert("Microgrid node created successfully!");

      navigate("/microgrid-nodes");
    } catch (err) {
      console.error("Create microgrid node error:", err);

      if (err.response?.data?.message) {
        setError(err.response.data.message);
      } else if (typeof err.response?.data === "string") {
        setError(err.response.data);
      } else if (err.response?.status === 400) {
        setError(
          "The server rejected the data. Please check the entered values."
        );
      } else if (err.response?.status === 401) {
        setError("You are not authorized. Please log in again.");
      } else if (err.response?.status === 403) {
        setError("You do not have permission to create a microgrid node.");
      } else if (err.response?.status === 404) {
        setError("Microgrid node API endpoint was not found.");
      } else {
        setError(
          "Unable to create the microgrid node. Please check that the API is running."
        );
      }
    } finally {
      setLoading(false);
    }
  };

  const inputStyle = {
    width: "100%",
    boxSizing: "border-box",
    padding: "12px 14px",
    border: "1px solid #D9D6CF",
    borderRadius: "8px",
    background: "#FFFFFF",
    fontSize: "14px",
    color: "#1C1F1E",
    outline: "none",
  };

  const labelStyle = {
    display: "block",
    marginBottom: "7px",
    fontSize: "13px",
    fontWeight: "600",
    color: "#343734",
  };

  const cardStyle = {
    background: "#FFFFFF",
    border: "1px solid #E3E0D9",
    borderRadius: "12px",
    padding: "24px",
    marginBottom: "20px",
  };

  const sectionTitleStyle = {
    marginTop: "0",
    marginBottom: "6px",
    fontSize: "18px",
    color: "#1C1F1E",
  };

  const sectionDescriptionStyle = {
    marginTop: "0",
    marginBottom: "24px",
    color: "#77736D",
    fontSize: "13px",
  };

  return (
    <div
      style={{
        padding: "32px",
        maxWidth: "1200px",
        margin: "0 auto",
      }}
    >
      {/* Header */}
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "flex-start",
          marginBottom: "28px",
        }}
      >
        <div>
          <div
            style={{
              fontSize: "13px",
              color: "#77736D",
              marginBottom: "8px",
            }}
          >
            Microgrid Nodes / Add Node
          </div>

          <h1
            style={{
              margin: "0",
              fontSize: "30px",
              color: "#111111",
            }}
          >
            Add Microgrid Node
          </h1>

          <p
            style={{
              color: "#6B6862",
              marginTop: "8px",
              marginBottom: "0",
              fontSize: "14px",
            }}
          >
            Register a new solar microgrid node in the system.
          </p>
        </div>

        <button
          type="button"
          onClick={() => navigate("/microgrid-nodes")}
          style={{
            background: "#FFFFFF",
            border: "1px solid #D5D1C9",
            color: "#333333",
            padding: "10px 18px",
            borderRadius: "8px",
            cursor: "pointer",
            fontWeight: "600",
          }}
        >
          ← Back to Nodes
        </button>
      </div>

      {/* Error message */}
      {error && (
        <div
          style={{
            marginBottom: "20px",
            background: "#FDECEC",
            color: "#B42318",
            border: "1px solid #F5C2C0",
            padding: "13px 16px",
            borderRadius: "8px",
            fontSize: "14px",
          }}
        >
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit}>
        {/* Basic Information */}
        <div style={cardStyle}>
          <h2 style={sectionTitleStyle}>Basic Information</h2>

          <p style={sectionDescriptionStyle}>
            Enter the basic information of the solar microgrid node.
          </p>

          {/* Node Name */}
          <div style={{ marginBottom: "20px" }}>
            <label style={labelStyle}>
              Node Name <span style={{ color: "#C62828" }}>*</span>
            </label>

            <input
              type="text"
              value={formData.nodeName}
              onChange={(event) => {
                const value = event.target.value;

                setFormData((previousData) => ({
                  ...previousData,
                  nodeName: value,
                }));
              }}
              placeholder="e.g. Malabe Solar Hub"
              style={inputStyle}
              required
            />
          </div>

          {/* Description */}
          <div style={{ marginBottom: "20px" }}>
            <label style={labelStyle}>Description</label>

            <textarea
              value={formData.description}
              onChange={(event) => {
                const value = event.target.value;

                setFormData((previousData) => ({
                  ...previousData,
                  description: value,
                }));
              }}
              placeholder="Enter a short description..."
              rows={4}
              style={{
                ...inputStyle,
                resize: "vertical",
              }}
            />
          </div>

          {/* Location */}
          <div>
            <label style={labelStyle}>
              Location <span style={{ color: "#C62828" }}>*</span>
            </label>

            <input
              type="text"
              value={formData.location}
              onChange={(event) => {
                const value = event.target.value;

                setFormData((previousData) => ({
                  ...previousData,
                  location: value,
                }));
              }}
              placeholder="e.g. Malabe"
              style={inputStyle}
              required
            />
          </div>
        </div>

        {/* GPS Location */}
        <div style={cardStyle}>
          <h2 style={sectionTitleStyle}>GPS Location</h2>

          <p style={sectionDescriptionStyle}>
            Enter the geographic coordinates of the microgrid node.
          </p>

          <div
            style={{
              display: "grid",
              gridTemplateColumns: "1fr 1fr",
              gap: "20px",
            }}
          >
            {/* Latitude */}
            <div>
              <label style={labelStyle}>
                Latitude <span style={{ color: "#C62828" }}>*</span>
              </label>

              <input
                type="number"
                value={formData.latitude}
                onChange={(event) => {
                  const value = event.target.value;

                  setFormData((previousData) => ({
                    ...previousData,
                    latitude: value,
                  }));
                }}
                step="any"
                min="-90"
                max="90"
                placeholder="e.g. 6.9271"
                style={inputStyle}
                required
              />
            </div>

            {/* Longitude */}
            <div>
              <label style={labelStyle}>
                Longitude <span style={{ color: "#C62828" }}>*</span>
              </label>

              <input
                type="number"
                value={formData.longitude}
                onChange={(event) => {
                  const value = event.target.value;

                  setFormData((previousData) => ({
                    ...previousData,
                    longitude: value,
                  }));
                }}
                step="any"
                min="-180"
                max="180"
                placeholder="e.g. 79.8612"
                style={inputStyle}
                required
              />
            </div>
          </div>
        </div>

        {/* Capacity & Storage */}
        <div style={cardStyle}>
          <h2 style={sectionTitleStyle}>Capacity & Storage</h2>

          <p style={sectionDescriptionStyle}>
            Configure the energy capacity and available battery storage slots.
          </p>

          <div
            style={{
              display: "grid",
              gridTemplateColumns: "1fr 1fr",
              gap: "20px",
            }}
          >
            {/* Capacity */}
            <div>
              <label style={labelStyle}>
                Capacity (kWh){" "}
                <span style={{ color: "#C62828" }}>*</span>
              </label>

              <input
                type="number"
                value={formData.capacityKWh}
                onChange={(event) => {
                  const value = event.target.value;

                  setFormData((previousData) => ({
                    ...previousData,
                    capacityKWh: value,
                  }));
                }}
                min="0"
                step="0.01"
                placeholder="e.g. 450"
                style={inputStyle}
                required
              />
            </div>

            {/* Battery Slots */}
            <div>
              <label style={labelStyle}>
                Battery Slots{" "}
                <span style={{ color: "#C62828" }}>*</span>
              </label>

              <input
                type="number"
                value={formData.batterySlots}
                onChange={(event) => {
                  const value = event.target.value;

                  setFormData((previousData) => ({
                    ...previousData,
                    batterySlots: value,
                  }));
                }}
                min="0"
                step="1"
                placeholder="e.g. 12"
                style={inputStyle}
                required
              />
            </div>
          </div>
        </div>

        {/* Operational Information */}
        <div style={cardStyle}>
          <h2 style={sectionTitleStyle}>Operational Information</h2>

          <p style={sectionDescriptionStyle}>
            Configure the operating schedule and initial node status.
          </p>

          <div
            style={{
              display: "grid",
              gridTemplateColumns: "1fr 1fr",
              gap: "20px",
            }}
          >
            {/* Schedule */}
            <div>
              <label style={labelStyle}>
                Operating Schedule{" "}
                <span style={{ color: "#C62828" }}>*</span>
              </label>

              <input
                type="text"
                value={formData.schedule}
                onChange={(event) => {
                  const value = event.target.value;

                  setFormData((previousData) => ({
                    ...previousData,
                    schedule: value,
                  }));
                }}
                placeholder="e.g. 06:00 - 19:00"
                style={inputStyle}
                required
              />
            </div>

            {/* Status */}
            <div>
              <label style={labelStyle}>
                Status <span style={{ color: "#C62828" }}>*</span>
              </label>

              <select
                value={formData.status}
                onChange={(event) => {
                  const value = event.target.value;

                  setFormData((previousData) => ({
                    ...previousData,
                    status: value,
                  }));
                }}
                style={inputStyle}
              >
                <option value="active">Active</option>
                <option value="inactive">Inactive</option>
              </select>
            </div>
          </div>
        </div>

        {/* GPS Information */}
        <div
          style={{
            background: "#EEF7F1",
            border: "1px solid #CFE6D6",
            borderRadius: "10px",
            padding: "15px 18px",
            marginBottom: "24px",
            fontSize: "13px",
            color: "#275A3B",
          }}
        >
          <strong>GPS coordinates:</strong> Latitude and longitude stored for
          this node can later be used by the mobile application to display
          nearby microgrid nodes.
        </div>

        {/* Buttons */}
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
            disabled={loading}
            style={{
              background: "#FFFFFF",
              color: "#333333",
              border: "1px solid #D5D1C9",
              borderRadius: "8px",
              padding: "12px 22px",
              fontWeight: "600",
              cursor: loading ? "not-allowed" : "pointer",
            }}
          >
            Cancel
          </button>

          <button
            type="submit"
            disabled={loading}
            style={{
              background: loading ? "#83AE94" : "#1E7A4D",
              color: "#FFFFFF",
              border: "none",
              borderRadius: "8px",
              padding: "12px 24px",
              fontWeight: "600",
              cursor: loading ? "not-allowed" : "pointer",
            }}
          >
            {loading ? "Creating..." : "+ Create Microgrid Node"}
          </button>
        </div>
      </form>
    </div>
  );
}

export default AddMicrogridNode;