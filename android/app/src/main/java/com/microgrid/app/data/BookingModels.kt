package com.microgrid.app.data

data class ReservationResponse(
    val id: String,
    val reservationId: String,
    val prosumerNic: String,
    val stationId: String,
    val slotId: String,
    val status: String,
    val createdAt: String,
    val updatedAt: String,
    val transactionReference: String
)

data class SlotResponse(
    val id: String,
    val slotId: String,
    val stationId: String,
    val date: String,
    val startTime: String,
    val endTime: String,
    val capacity: Int,
    val availability: Int,
    val status: String,
    val createdAt: String,
    val updatedAt: String
)

data class MicrogridNodeResponse(
    val id: String,
    val nodeId: String,
    val nodeName: String,
    val description: String,
    val location: String,
    val latitude: Double,
    val longitude: Double,
    val capacityKWh: Int,
    val batterySlots: Int,
    val schedule: String,
    val status: String,
    val createdAt: String,
    val updatedAt: String
)