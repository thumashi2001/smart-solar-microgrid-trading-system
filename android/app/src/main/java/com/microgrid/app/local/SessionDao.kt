package com.microgrid.app.local

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query

@Dao
interface SessionDao {

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun saveSession(session: SessionEntity)

    @Query("SELECT * FROM session WHERE id = 1 LIMIT 1")
    suspend fun getSession(): SessionEntity?

    @Query("DELETE FROM session")
    suspend fun clearSession()
}