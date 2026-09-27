package com.smartsolar.microgrid.data.db

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "cached_stations")
data class StationEntity(
    @PrimaryKey val nodeId: String,
    val nodeName: String,
    val location: String,
    val latitude: Double,
    val longitude: Double,
    val capacityKWh: Double,
    val batterySlots: Int,
    val status: String,
    val cachedAt: Long,
)
