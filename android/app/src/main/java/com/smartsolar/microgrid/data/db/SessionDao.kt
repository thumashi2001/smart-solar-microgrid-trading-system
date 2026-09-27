package com.smartsolar.microgrid.data.db

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query

@Dao
interface SessionDao {

    @Query("SELECT * FROM user_session WHERE id = :id LIMIT 1")
    suspend fun getSession(id: Int = SessionEntity.SINGLETON_ID): SessionEntity?

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsert(session: SessionEntity)

    @Query("DELETE FROM user_session")
    suspend fun clear()
}
