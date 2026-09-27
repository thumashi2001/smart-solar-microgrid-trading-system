package com.microgrid.app.local

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query

@Dao
interface BookingDao {

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertBookings(bookings: List<BookingEntity>)

    @Query(
        """
        SELECT * FROM bookings
        WHERE prosumerNic = :nic
        ORDER BY date DESC
        """
    )
    suspend fun getBookingsByProsumer(
        nic: String
    ): List<BookingEntity>

    @Query(
        """
        SELECT * FROM bookings
        WHERE reservationId = :reservationId
        LIMIT 1
        """
    )
    suspend fun getBookingById(
        reservationId: String
    ): BookingEntity?

    @Query(
        """
        SELECT * FROM bookings
        WHERE prosumerNic = :nic
        AND status = :status
        ORDER BY date DESC
        """
    )
    suspend fun getBookingsByStatus(
        nic: String,
        status: String
    ): List<BookingEntity>

    @Query(
        """
        DELETE FROM bookings
        WHERE prosumerNic = :nic
        """
    )
    suspend fun deleteBookingsByProsumer(
        nic: String
    )

    @Query("DELETE FROM bookings")
    suspend fun clearBookings()
}