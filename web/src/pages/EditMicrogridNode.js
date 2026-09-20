import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import api from "../api";

function EditMicrogridNode() {
  const { id } = useParams();
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

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  // =========================================================
  // LOAD EXISTING NODE
  // =========================================================
  useEffect(() => {
    const loadNode = async () => {
      try {
        setLoading(true);
        setError("");

        // Same working endpoint used by the Node Details page
        const response = await api.get("/microgridnodes");

        const nodes = Array.isArray(response.data)
          ? response.data
          : response.data?.data ||
            response.data?.nodes ||
            [];

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
          setError("Microgrid node not found.");
          return;
        }

        setFormData({
          nodeName: String(
            selectedNode.nodeName ??
              selectedNode.name ??
              selectedNode.NodeName ??
              ""
          ),

          description: String(
            selectedNode.description ??
              selectedNode.Description ??
              ""
          ),

          location: String(
            selectedNode.location ??
              selectedNode.Location ??
              ""
          ),

          latitude: String(
            selectedNode.latitude ??
              selectedNode.Latitude ??
              ""
          ),

          longitude: String(
            selectedNode.longitude ??
              selectedNode.Longitude ??
              ""
          ),

          capacityKWh: String(
            selectedNode.capacityKWh ??
              selectedNode.capacity ??
              selectedNode.CapacityKWh ??
              selectedNode.Capacity ??
              ""
          ),

          batterySlots: String(
            selectedNode.batterySlots ??
              selectedNode.BatterySlots ??
              selectedNode.availableBatterySlots ??
              ""
          ),

          schedule: String(
            selectedNode.schedule ??
              selectedNode.Schedule ??
              selectedNode.operatingSchedule ??
              selectedNode.OperatingSchedule ??
              ""
          ),

          status: String(
            selectedNode.status ??
              selectedNode.Status ??
              "active"
          ).toLowerCase(),
        });
      } catch (err) {
        console.error(
          "Failed to load microgrid node:",
          err
        );

        if (err.response?.status === 401) {
          setError(
            "You are not authorized. Please log in again."
          );
        } else if (err.response?.status === 403) {
          setError(
            "You do not have permission to edit this microgrid node."
          );
        } else {
          setError(
            "Failed to load microgrid node. Please make sure the API is running."
          );
        }
      } finally {
        setLoading(false);
      }
    };

    if (id) {
      loadNode();
    } else {
      setError("Invalid microgrid node ID.");
      setLoading(false);
    }
  }, [id]);

  // =========================================================
  // UPDATE ONE FIELD
  // =========================================================
  const updateField = (field, value) => {
    setFormData((previousData) => ({
      ...previousData,
      [field]: value,
    }));
  };

  // =========================================================
  // VALIDATION
  // =========================================================
  const validateForm = () => {
    if (!formData.nodeName.trim()) {
      alert("Please enter the node name.");
      return false;
    }

    if (!formData.location.trim()) {
      alert("Please enter the location.");
      return false;
    }

    if (formData.latitude === "") {
      alert("Please enter the latitude.");
      return false;
    }

    if (formData.longitude === "") {
      alert("Please enter the longitude.");
      return false;
    }

    const latitude = Number(formData.latitude);
    const longitude = Number(formData.longitude);
    const capacity = Number(formData.capacityKWh);
    const slots = Number(formData.batterySlots);

    if (
      Number.isNaN(latitude) ||
      latitude < -90 ||
      latitude > 90
    ) {
      alert(
        "Latitude must be a number between -90 and 90."
      );
      return false;
    }

    if (
      Number.isNaN(longitude) ||
      longitude < -180 ||
      longitude > 180
    ) {
      alert(
        "Longitude must be a number between -180 and 180."
      );
      return false;
    }

    if (
      formData.capacityKWh === "" ||
      Number.isNaN(capacity) ||
      capacity <= 0
    ) {
      alert("Capacity must be greater than 0.");
      return false;
    }

    if (
      formData.batterySlots === "" ||
      Number.isNaN(slots) ||
      slots < 0 ||
      !Number.isInteger(slots)
    ) {
      alert(
        "Battery slots must be a whole number of 0 or greater."
      );
      return false;
    }

    if (!formData.schedule.trim()) {
      alert("Please enter the operating schedule.");
      return false;
    }

    return true;
  };

  // =========================================================
  // SAVE CHANGES
  // =========================================================
  const handleSubmit = async (event) => {
    event.preventDefault();

    if (!validateForm()) {
      return;
    }

    try {
      setSaving(true);
      setError("");

      const updatedNode = {
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

      await api.put(
        `/microgridnodes/${encodeURIComponent(id)}`,
        updatedNode
      );

      alert("Microgrid node updated successfully!");

      navigate(
        `/microgrid-nodes/${encodeURIComponent(id)}`
      );
    } catch (err) {
      console.error(
        "Failed to update microgrid node:",
        err
      );

      if (err.response?.status === 404) {
        setError(
          "The update API endpoint was not found. Please check the PUT route in the C# API."
        );
      } else if (err.response?.status === 401) {
        setError(
          "You are not authorized. Please log in again."
        );
      } else if (err.response?.status === 403) {
        setError(
          "You do not have permission to update this microgrid node."
        );
      } else {
        const message =
          err.response?.data?.message ||
          err.response?.data?.error ||
          "Failed to update microgrid node. Please try again.";

        setError(message);
      }
    } finally {
      setSaving(false);
    }
  };

  // =========================================================
  // NAVIGATION
  // =========================================================
  const handleCancel = () => {
    navigate(
      `/microgrid-nodes/${encodeURIComponent(id)}`
    );
  };

  // =========================================================
  // LOADING
  // =========================================================
  if (loading) {
    return (
      <div style={styles.page}>
        <div style={styles.loadingCard}>
          <h2 style={{ marginTop: 0 }}>
            Loading Microgrid Node...
          </h2>

          <p style={styles.muted}>
            Please wait while the node information is
            retrieved.
          </p>
        </div>
      </div>
    );
  }

  // =========================================================
  // LOAD ERROR
  // =========================================================
  if (error && !formData.nodeName) {
    return (
      <div style={styles.page}>
        <div style={styles.loadingCard}>
          <h2 style={{ marginTop: 0 }}>
            Unable to Load Node
          </h2>

          <p style={styles.errorText}>
            {error}
          </p>

          <button
            type="button"
            onClick={() =>
              navigate("/microgrid-nodes")
            }
            style={styles.secondaryButton}
          >
            ← Back to Nodes
          </button>
        </div>
      </div>
    );
  }

  // =========================================================
  // EDIT FORM
  // =========================================================
  return (
    <div style={styles.page}>
      <div style={styles.container}>
        {/* Breadcrumb */}
        <div style={styles.breadcrumb}>
          Microgrid Nodes / Node Details / Edit Node
        </div>

        {/* Header */}
        <div style={styles.header}>
          <div>
            <h1 style={styles.title}>
              Edit Microgrid Node
            </h1>

            <p style={styles.subtitle}>
              Update the information for{" "}
              <strong>
                {formData.nodeName}
              </strong>
              .
            </p>
          </div>

          <button
            type="button"
            onClick={handleCancel}
            style={styles.secondaryButton}
          >
            ← Back to Node Details
          </button>
        </div>

        {/* Save Error */}
        {error && (
          <div style={styles.errorBox}>
            <strong>
              Unable to save changes.
            </strong>

            <div style={{ marginTop: "5px" }}>
              {error}
            </div>
          </div>
        )}

        <form onSubmit={handleSubmit}>
          {/* ================================================= */}
          {/* BASIC INFORMATION */}
          {/* ================================================= */}

          <section style={styles.card}>
            <h2 style={styles.sectionTitle}>
              Basic Information
            </h2>

            <p style={styles.sectionDescription}>
              Update the general information of the
              microgrid node.
            </p>

            {/* Node Name */}
            <div style={styles.formGroup}>
              <label style={styles.label}>
                Node Name{" "}
                <span style={styles.required}>
                  *
                </span>
              </label>

              <input
                type="text"
                value={formData.nodeName}
                onChange={(event) =>
                  updateField(
                    "nodeName",
                    event.target.value
                  )
                }
                placeholder="e.g. Colombo Solar Hub"
                style={styles.input}
                required
              />
            </div>

            {/* Description */}
            <div style={styles.formGroup}>
              <label style={styles.label}>
                Description
              </label>

              <textarea
                value={formData.description}
                onChange={(event) =>
                  updateField(
                    "description",
                    event.target.value
                  )
                }
                placeholder="Enter a short description of this microgrid node"
                rows={4}
                style={styles.textarea}
              />
            </div>

            {/* Location */}
            <div style={styles.formGroup}>
              <label style={styles.label}>
                Location{" "}
                <span style={styles.required}>
                  *
                </span>
              </label>

              <input
                type="text"
                value={formData.location}
                onChange={(event) =>
                  updateField(
                    "location",
                    event.target.value
                  )
                }
                placeholder="e.g. Colombo"
                style={styles.input}
                required
              />
            </div>
          </section>

          {/* ================================================= */}
          {/* GPS */}
          {/* ================================================= */}

          <section style={styles.card}>
            <h2 style={styles.sectionTitle}>
              GPS Location
            </h2>

            <p style={styles.sectionDescription}>
              Update the geographic coordinates of the
              microgrid node.
            </p>

            <div style={styles.twoColumns}>
              {/* Latitude */}
              <div style={styles.formGroup}>
                <label style={styles.label}>
                  Latitude{" "}
                  <span style={styles.required}>
                    *
                  </span>
                </label>

                <input
                  type="number"
                  value={formData.latitude}
                  onChange={(event) =>
                    updateField(
                      "latitude",
                      event.target.value
                    )
                  }
                  placeholder="e.g. 6.9271"
                  step="any"
                  min="-90"
                  max="90"
                  style={styles.input}
                  required
                />
              </div>

              {/* Longitude */}
              <div style={styles.formGroup}>
                <label style={styles.label}>
                  Longitude{" "}
                  <span style={styles.required}>
                    *
                  </span>
                </label>

                <input
                  type="number"
                  value={formData.longitude}
                  onChange={(event) =>
                    updateField(
                      "longitude",
                      event.target.value
                    )
                  }
                  placeholder="e.g. 79.8612"
                  step="any"
                  min="-180"
                  max="180"
                  style={styles.input}
                  required
                />
              </div>
            </div>
          </section>

          {/* ================================================= */}
          {/* CAPACITY */}
          {/* ================================================= */}

          <section style={styles.card}>
            <h2 style={styles.sectionTitle}>
              Capacity & Storage
            </h2>

            <p style={styles.sectionDescription}>
              Update the energy capacity and available
              battery storage slots.
            </p>

            <div style={styles.twoColumns}>
              {/* Capacity */}
              <div style={styles.formGroup}>
                <label style={styles.label}>
                  Capacity (kWh){" "}
                  <span style={styles.required}>
                    *
                  </span>
                </label>

                <input
                  type="number"
                  value={formData.capacityKWh}
                  onChange={(event) =>
                    updateField(
                      "capacityKWh",
                      event.target.value
                    )
                  }
                  placeholder="e.g. 500"
                  min="0.01"
                  step="0.01"
                  style={styles.input}
                  required
                />
              </div>

              {/* Battery Slots */}
              <div style={styles.formGroup}>
                <label style={styles.label}>
                  Battery Slots{" "}
                  <span style={styles.required}>
                    *
                  </span>
                </label>

                <input
                  type="number"
                  value={formData.batterySlots}
                  onChange={(event) =>
                    updateField(
                      "batterySlots",
                      event.target.value
                    )
                  }
                  placeholder="e.g. 15"
                  min="0"
                  step="1"
                  style={styles.input}
                  required
                />
              </div>
            </div>
          </section>

          {/* ================================================= */}
          {/* OPERATIONAL INFORMATION */}
          {/* ================================================= */}

          <section style={styles.card}>
            <h2 style={styles.sectionTitle}>
              Operational Information
            </h2>

            <p style={styles.sectionDescription}>
              Update the operating schedule and current
              node status.
            </p>

            <div style={styles.twoColumns}>
              {/* Schedule */}
              <div style={styles.formGroup}>
                <label style={styles.label}>
                  Operating Schedule{" "}
                  <span style={styles.required}>
                    *
                  </span>
                </label>

                <input
                  type="text"
                  value={formData.schedule}
                  onChange={(event) =>
                    updateField(
                      "schedule",
                      event.target.value
                    )
                  }
                  placeholder="e.g. 06:00 - 20:00"
                  style={styles.input}
                  required
                />
              </div>

              {/* Status */}
              <div style={styles.formGroup}>
                <label style={styles.label}>
                  Status{" "}
                  <span style={styles.required}>
                    *
                  </span>
                </label>

                <select
                  value={formData.status}
                  onChange={(event) =>
                    updateField(
                      "status",
                      event.target.value
                    )
                  }
                  style={styles.input}
                >
                  <option value="active">
                    Active
                  </option>

                  <option value="inactive">
                    Inactive
                  </option>
                </select>
              </div>
            </div>
          </section>

          {/* GPS Note */}
          <div style={styles.infoBox}>
            <strong>
              GPS coordinates:
            </strong>{" "}
            Latitude and longitude stored for this
            node can be used by the mobile
            application to display nearby microgrid
            nodes.
          </div>

          {/* Buttons */}
          <div style={styles.actions}>
            <button
              type="button"
              onClick={handleCancel}
              disabled={saving}
              style={styles.secondaryButton}
            >
              Cancel
            </button>

            <button
              type="submit"
              disabled={saving}
              style={{
                ...styles.primaryButton,
                opacity: saving ? 0.65 : 1,
                cursor: saving
                  ? "not-allowed"
                  : "pointer",
              }}
            >
              {saving
                ? "Saving Changes..."
                : "Save Changes"}
            </button>
          </div>
        </form>
      </div>
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

  container: {
    maxWidth: "1120px",
    margin: "0 auto",
  },

  breadcrumb: {
    color: "#77736D",
    fontSize: "13px",
    marginBottom: "12px",
  },

  header: {
    display: "flex",
    alignItems: "flex-start",
    justifyContent: "space-between",
    gap: "20px",
    marginBottom: "25px",
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

  card: {
    background: "#FFFFFF",
    border: "1px solid #DEDAD3",
    borderRadius: "12px",
    padding: "24px",
    marginBottom: "18px",
  },

  sectionTitle: {
    margin: 0,
    color: "#111827",
    fontSize: "18px",
    fontWeight: 700,
  },

  sectionDescription: {
    marginTop: "6px",
    marginBottom: "22px",
    color: "#77736D",
    fontSize: "13px",
  },

  twoColumns: {
    display: "grid",
    gridTemplateColumns:
      "repeat(auto-fit, minmax(280px, 1fr))",
    gap: "18px",
  },

  formGroup: {
    display: "flex",
    flexDirection: "column",
    gap: "7px",
    marginBottom: "17px",
  },

  label: {
    color: "#333333",
    fontSize: "13px",
    fontWeight: 600,
  },

  required: {
    color: "#B42318",
  },

  input: {
    width: "100%",
    boxSizing: "border-box",
    padding: "12px 13px",
    border: "1px solid #D7D3CB",
    borderRadius: "8px",
    background: "#FFFFFF",
    color: "#222222",
    fontSize: "14px",
    outline: "none",
  },

  textarea: {
    width: "100%",
    boxSizing: "border-box",
    padding: "12px 13px",
    border: "1px solid #D7D3CB",
    borderRadius: "8px",
    background: "#FFFFFF",
    color: "#222222",
    fontSize: "14px",
    outline: "none",
    resize: "vertical",
    minHeight: "90px",
    fontFamily: "inherit",
  },

  primaryButton: {
    background: "#1E7A4D",
    color: "#FFFFFF",
    border: "none",
    borderRadius: "8px",
    padding: "11px 20px",
    fontSize: "14px",
    fontWeight: 600,
  },

  secondaryButton: {
    background: "#FFFFFF",
    color: "#222222",
    border: "1px solid #D7D3CB",
    borderRadius: "8px",
    padding: "10px 17px",
    cursor: "pointer",
    fontSize: "14px",
    fontWeight: 600,
  },

  actions: {
    display: "flex",
    justifyContent: "flex-end",
    gap: "10px",
    marginTop: "20px",
    paddingBottom: "35px",
  },

  infoBox: {
    padding: "14px 16px",
    background: "#EEF8F2",
    border: "1px solid #CCE5D5",
    borderRadius: "8px",
    color: "#285943",
    fontSize: "13px",
    lineHeight: 1.5,
  },

  errorBox: {
    padding: "14px 16px",
    marginBottom: "18px",
    background: "#FEF3F2",
    border: "1px solid #FECACA",
    borderRadius: "8px",
    color: "#B42318",
    fontSize: "13px",
  },

  errorText: {
    color: "#B42318",
    marginBottom: "20px",
  },

  loadingCard: {
    maxWidth: "700px",
    margin: "40px auto",
    padding: "28px",
    background: "#FFFFFF",
    border: "1px solid #DEDAD3",
    borderRadius: "12px",
  },

  muted: {
    color: "#6B6862",
  },
};

export default EditMicrogridNode;