package com.smartsolar.microgrid.data.db

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query

@Dao
interface StationCacheDao {

    @Query("SELECT * FROM cached_stations")
    suspend fun getAll(): List<StationEntity>

    @Query("SELECT MAX(cachedAt) FROM cached_stations")
    suspend fun latestCachedAt(): Long?

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsertAll(stations: List<StationEntity>)

    @Query("DELETE FROM cached_stations")
    suspend fun clear()
}
