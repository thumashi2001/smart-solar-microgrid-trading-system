package com.smartsolar.microgrid.ui.map

data class MapStationModel(
    val nodeId: String,
    val nodeName: String,
    val location: String,
    val latitude: Double,
    val longitude: Double,
    val capacityKWh: Double,
    val batterySlots: Int,
    val status: String,
    val distanceKm: Double?,
)
