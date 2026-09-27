package com.smartsolar.microgrid.data.api.dto

import com.google.gson.annotations.SerializedName

data class LoginRequest(
    @SerializedName("identifier") val identifier: String,
    @SerializedName("password") val password: String,
)

data class LoginResponse(
    @SerializedName("token") val token: String,
    @SerializedName("role") val role: String,
    @SerializedName("fullName") val fullName: String,
    @SerializedName("nic") val nic: String? = null,
)

data class VerifyTransferRequest(
    @SerializedName("transactionReference") val transactionReference: String,
)

data class VerifyTransferResponse(
    @SerializedName("eligible") val eligible: Boolean,
    @SerializedName("reservationId") val reservationId: String,
    @SerializedName("transactionReference") val transactionReference: String,
    @SerializedName("status") val status: String,
    @SerializedName("stationId") val stationId: String,
    @SerializedName("stationName") val stationName: String?,
    @SerializedName("stationLocation") val stationLocation: String?,
    @SerializedName("slotId") val slotId: String,
    @SerializedName("prosumerNicMasked") val prosumerNicMasked: String,
    @SerializedName("message") val message: String,
)

data class CompleteTransferResponse(
    @SerializedName("alreadyCompleted") val alreadyCompleted: Boolean,
    @SerializedName("reservationId") val reservationId: String,
    @SerializedName("status") val status: String,
    @SerializedName("completedAt") val completedAt: String?,
    @SerializedName("completedByOperatorId") val completedByOperatorId: String?,
    @SerializedName("completedByOperatorName") val completedByOperatorName: String?,
    @SerializedName("message") val message: String,
)

data class QrCodeResponse(
    @SerializedName("reservationId") val reservationId: String,
    @SerializedName("transactionReference") val transactionReference: String,
    @SerializedName("status") val status: String,
    @SerializedName("issuedAt") val issuedAt: String?,
    @SerializedName("payload") val payload: String,
    @SerializedName("reusedExistingReference") val reusedExistingReference: Boolean,
)

data class MicrogridNodeDto(
    @SerializedName("id") val id: String?,
    @SerializedName("nodeId") val nodeId: String,
    @SerializedName("nodeName") val nodeName: String,
    @SerializedName("location") val location: String,
    @SerializedName("latitude") val latitude: Double,
    @SerializedName("longitude") val longitude: Double,
    @SerializedName("capacityKWh") val capacityKWh: Double,
    @SerializedName("batterySlots") val batterySlots: Int,
    @SerializedName("status") val status: String,
)

data class NearbyStationDto(
    @SerializedName("id") val id: String?,
    @SerializedName("nodeId") val nodeId: String,
    @SerializedName("nodeName") val nodeName: String,
    @SerializedName("location") val location: String,
    @SerializedName("latitude") val latitude: Double,
    @SerializedName("longitude") val longitude: Double,
    @SerializedName("capacityKWh") val capacityKWh: Double,
    @SerializedName("batterySlots") val batterySlots: Int,
    @SerializedName("status") val status: String,
    @SerializedName("distanceKm") val distanceKm: Double,
)

data class ApiMessageResponse(
    @SerializedName("message") val message: String?,
)
