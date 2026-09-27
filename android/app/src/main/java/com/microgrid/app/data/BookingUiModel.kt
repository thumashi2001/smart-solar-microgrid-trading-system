package com.microgrid.app.data

data class BookingUiModel(
    val id: String,
    val stationName: String,
    val location: String,
    val date: String,
    val time: String,
    val energyAmount: String,
    val status: String
)