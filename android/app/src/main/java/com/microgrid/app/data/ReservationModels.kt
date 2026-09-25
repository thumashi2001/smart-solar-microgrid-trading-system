package com.microgrid.app.data

import com.google.gson.annotations.SerializedName

// ──────────────────────────────────────────────
// Microgrid Node (Station) model — for station selection
// ──────────────────────────────────────────────

data class MicrogridNode(
    @SerializedName("id") val id: String = "",
    @SerializedName("nodeId") val nodeId: String = "",
    @SerializedName("nodeName") val nodeName: String = "",
    @SerializedName("location") val location: String = "",
    @SerializedName("status") val status: String = "",
    @SerializedName("capacity") val capacity: Double = 0.0
)

// ──────────────────────────────────────────────
// Slot models
// ──────────────────────────────────────────────

data class Slot(
    @SerializedName("id") val id: String = "",
    @SerializedName("slotId") val slotId: String = "",
    @SerializedName("stationId") val stationId: String = "",
    @SerializedName("date") val date: String = "",
    @SerializedName("startTime") val startTime: String = "",
    @SerializedName("endTime") val endTime: String = "",
    @SerializedName("capacity") val capacity: Int = 0,
    @SerializedName("availability") val availability: Int = 0,
    @SerializedName("status") val status: String = "Available",
    @SerializedName("createdAt") val createdAt: String = "",
    @SerializedName("updatedAt") val updatedAt: String = ""
)

// ──────────────────────────────────────────────
// Reservation models
// ──────────────────────────────────────────────

data class Reservation(
    @SerializedName("id") val id: String = "",
    @SerializedName("reservationId") val reservationId: String = "",
    // Backend field names are camelCase — Gson deserialises them automatically
    @SerializedName("prosumerNic") val prosumerNic: String = "",
    @SerializedName("stationId") val stationId: String = "",
    @SerializedName("slotId") val slotId: String = "",
    @SerializedName("status") val status: String = "Pending",
    @SerializedName("createdAt") val createdAt: String = "",
    @SerializedName("updatedAt") val updatedAt: String = "",
    @SerializedName("transactionReference") val transactionReference: String = ""
)

/**
 * POST /api/reservations
 *
 * Backend DTO (CreateReservationRequest.cs):
 *   public string ProsumerNic { get; set; }
 *   public string StationId   { get; set; }
 *   public string SlotId      { get; set; }
 *
 * NO energyAmount. NO notes. Do not add unsupported fields.
 */
data class CreateReservationRequest(
    @SerializedName("prosumerNic") val prosumerNic: String,
    @SerializedName("stationId") val stationId: String,
    @SerializedName("slotId") val slotId: String
)

/**
 * PUT /api/reservations/{id}
 *
 * Backend DTO (UpdateReservationRequest.cs):
 *   public string StationId { get; set; }
 *   public string SlotId    { get; set; }
 *
 * This moves the reservation to a DIFFERENT slot on the same or a different station.
 * It is NOT a status-change endpoint.
 * Cancellation uses DELETE /api/reservations/{id}.
 */
data class UpdateReservationRequest(
    @SerializedName("stationId") val stationId: String,
    @SerializedName("slotId") val slotId: String
)
