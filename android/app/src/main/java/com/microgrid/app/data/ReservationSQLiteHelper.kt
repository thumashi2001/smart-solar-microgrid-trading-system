package com.microgrid.app.data

import android.content.ContentValues
import android.content.Context
import android.database.Cursor
import android.database.sqlite.SQLiteDatabase
import android.database.sqlite.SQLiteOpenHelper
import android.util.Log

class ReservationSQLiteHelper(context: Context) : SQLiteOpenHelper(context, DATABASE_NAME, null, DATABASE_VERSION) {

    companion object {
        private const val DATABASE_VERSION = 1
        private const val DATABASE_NAME = "ReservationCache.db"

        // Table Name
        private const val TABLE_RESERVATIONS = "reservation_local"

        // Column Names
        private const val COLUMN_ID = "id"
        private const val COLUMN_RESERVATION_ID = "reservationId"
        private const val COLUMN_PROSUMER_NIC = "prosumerNic"
        private const val COLUMN_STATION_ID = "stationId"
        private const val COLUMN_SLOT_ID = "slotId"
        private const val COLUMN_STATUS = "status"
        private const val COLUMN_CREATED_AT = "createdAt"
        private const val COLUMN_UPDATED_AT = "updatedAt"
        private const val COLUMN_TRANSACTION_REFERENCE = "transactionReference"
    }

    override fun onCreate(db: SQLiteDatabase) {
        val createTable = ("CREATE TABLE " + TABLE_RESERVATIONS + "("
                + COLUMN_ID + " TEXT PRIMARY KEY,"
                + COLUMN_RESERVATION_ID + " TEXT,"
                + COLUMN_PROSUMER_NIC + " TEXT,"
                + COLUMN_STATION_ID + " TEXT,"
                + COLUMN_SLOT_ID + " TEXT,"
                + COLUMN_STATUS + " TEXT,"
                + COLUMN_CREATED_AT + " TEXT,"
                + COLUMN_UPDATED_AT + " TEXT,"
                + COLUMN_TRANSACTION_REFERENCE + " TEXT" + ")")
        db.execSQL(createTable)
    }

    override fun onUpgrade(db: SQLiteDatabase, oldVersion: Int, newVersion: Int) {
        db.execSQL("DROP TABLE IF EXISTS $TABLE_RESERVATIONS")
        onCreate(db)
    }

    fun insertOrUpdateReservation(reservation: Reservation) {
        val db = this.writableDatabase
        val values = ContentValues().apply {
            put(COLUMN_ID, reservation.id)
            put(COLUMN_RESERVATION_ID, reservation.reservationId)
            put(COLUMN_PROSUMER_NIC, reservation.prosumerNic)
            put(COLUMN_STATION_ID, reservation.stationId)
            put(COLUMN_SLOT_ID, reservation.slotId)
            put(COLUMN_STATUS, reservation.status)
            put(COLUMN_CREATED_AT, reservation.createdAt)
            put(COLUMN_UPDATED_AT, reservation.updatedAt)
            put(COLUMN_TRANSACTION_REFERENCE, reservation.transactionReference)
        }
        
        // Use insertWithOnConflict to update if exists
        db.insertWithOnConflict(
            TABLE_RESERVATIONS,
            null,
            values,
            SQLiteDatabase.CONFLICT_REPLACE
        )
        db.close()
    }

    fun updateReservationStatus(id: String, newStatus: String) {
        val db = this.writableDatabase
        val values = ContentValues().apply {
            put(COLUMN_STATUS, newStatus)
        }
        db.update(
            TABLE_RESERVATIONS,
            values,
            "$COLUMN_ID = ?",
            arrayOf(id)
        )
        db.close()
    }
    
    fun getReservationById(id: String): Reservation? {
        val db = this.readableDatabase
        var cursor: Cursor? = null
        var reservation: Reservation? = null

        try {
            cursor = db.query(
                TABLE_RESERVATIONS,
                null,
                "$COLUMN_ID = ?",
                arrayOf(id),
                null, null, null
            )

            if (cursor != null && cursor.moveToFirst()) {
                reservation = mapCursorToReservation(cursor)
            }
        } catch (e: Exception) {
            Log.e("SQLite", "Error fetching reservation: ${e.message}")
        } finally {
            cursor?.close()
            db.close()
        }
        
        return reservation
    }

    fun getReservationsByProsumer(nic: String): List<Reservation> {
        val reservationList = mutableListOf<Reservation>()
        val db = this.readableDatabase
        var cursor: Cursor? = null

        try {
            cursor = db.query(
                TABLE_RESERVATIONS,
                null,
                "$COLUMN_PROSUMER_NIC = ?",
                arrayOf(nic),
                null, null, "$COLUMN_CREATED_AT DESC"
            )

            if (cursor != null && cursor.moveToFirst()) {
                do {
                    reservationList.add(mapCursorToReservation(cursor))
                } while (cursor.moveToNext())
            }
        } catch (e: Exception) {
            Log.e("SQLite", "Error fetching reservations: ${e.message}")
        } finally {
            cursor?.close()
            db.close()
        }

        return reservationList
    }
    
    // Deletes entirely - used if we don't want logical cancellation
    fun deleteReservation(id: String) {
        val db = this.writableDatabase
        db.delete(
            TABLE_RESERVATIONS,
            "$COLUMN_ID = ?",
            arrayOf(id)
        )
        db.close()
    }

    private fun mapCursorToReservation(cursor: Cursor): Reservation {
        return Reservation(
            id = cursor.getString(cursor.getColumnIndexOrThrow(COLUMN_ID)),
            reservationId = cursor.getString(cursor.getColumnIndexOrThrow(COLUMN_RESERVATION_ID)),
            prosumerNic = cursor.getString(cursor.getColumnIndexOrThrow(COLUMN_PROSUMER_NIC)),
            stationId = cursor.getString(cursor.getColumnIndexOrThrow(COLUMN_STATION_ID)),
            slotId = cursor.getString(cursor.getColumnIndexOrThrow(COLUMN_SLOT_ID)),
            status = cursor.getString(cursor.getColumnIndexOrThrow(COLUMN_STATUS)),
            createdAt = cursor.getString(cursor.getColumnIndexOrThrow(COLUMN_CREATED_AT)),
            updatedAt = cursor.getString(cursor.getColumnIndexOrThrow(COLUMN_UPDATED_AT)),
            transactionReference = cursor.getString(cursor.getColumnIndexOrThrow(COLUMN_TRANSACTION_REFERENCE))
        )
    }
}
