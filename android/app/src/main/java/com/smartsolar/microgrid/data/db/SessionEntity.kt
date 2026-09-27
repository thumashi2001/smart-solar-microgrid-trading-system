package com.smartsolar.microgrid.data.db

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "user_session")
data class SessionEntity(
    @PrimaryKey val id: Int = SINGLETON_ID,
    val token: String,
    val role: String,
    val fullName: String,
    val identifier: String,
) {
    companion object {
        const val SINGLETON_ID = 1
    }
}
