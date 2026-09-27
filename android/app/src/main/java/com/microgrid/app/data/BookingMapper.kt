package com.microgrid.app.data

import com.microgrid.app.local.BookingEntity

fun BookingUiModel.toEntity(
    prosumerNic: String,
    stationId: String = "",
    slotId: String = "",
    updatedAt: String = ""
): BookingEntity {

    return BookingEntity(
        reservationId = id,
        prosumerNic = prosumerNic,
        stationId = stationId,
        slotId = slotId,
        stationName = stationName,
        location = location,
        date = date,
        time = time,
        energyAmount = energyAmount,
        status = status,
        updatedAt = updatedAt
    )
}

fun BookingEntity.toUiModel(): BookingUiModel {

    return BookingUiModel(
        id = reservationId,
        stationName = stationName,
        location = location,
        date = date,
        time = time,
        energyAmount = energyAmount,
        status = status
    )
}