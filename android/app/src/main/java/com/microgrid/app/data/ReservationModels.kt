package com.microgrid.app.data

import com.google.gson.annotations.SerializedName

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
    @SerializedName("status") val status: String = "Available"
)

// ──────────────────────────────────────────────
// Reservation models
// ──────────────────────────────────────────────

data class Reservation(
    @SerializedName("id") val id: String = "",
    @SerializedName("reservationId") val reservationId: String = "",
    @SerializedName("slotId") val slotId: String = "",
    @SerializedName("stationId") val stationId: String = "",
    @SerializedName("prosumerNic") val prosumerNic: String = "",
    @SerializedName("date") val date: String = "",
    @SerializedName("startTime") val startTime: String = "",
    @SerializedName("endTime") val endTime: String = "",
    @SerializedName("status") val status: String = "Pending",
    @SerializedName("energyAmount") val energyAmount: Double = 0.0,
    @SerializedName("notes") val notes: String = "",
    @SerializedName("createdAt") val createdAt: String = ""
)

data class CreateReservationRequest(
    @SerializedName("slotId") val slotId: String,
    @SerializedName("prosumerNic") val prosumerNic: String,
    @SerializedName("energyAmount") val energyAmount: Double,
    @SerializedName("notes") val notes: String = ""
)

data class UpdateReservationRequest(
    @SerializedName("energyAmount") val energyAmount: Double? = null,
    @SerializedName("notes") val notes: String? = null,
    @SerializedName("status") val status: String? = null
)
