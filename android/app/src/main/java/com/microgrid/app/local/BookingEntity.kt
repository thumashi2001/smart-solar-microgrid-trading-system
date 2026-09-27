package com.microgrid.app.local

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "bookings")
data class BookingEntity(

    @PrimaryKey
    val reservationId: String,

    val prosumerNic: String,

    val stationId: String,

    val slotId: String,

    val stationName: String,

    val location: String,

    val date: String,

    val time: String,

    val energyAmount: String,

    val status: String,

    val updatedAt: String
)