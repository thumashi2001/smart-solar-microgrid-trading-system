package com.microgrid.app.local

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "session")
data class SessionEntity(
    @PrimaryKey val id: Int = 1,
    val nic: String,
    val fullName: String,
    val token: String,
    val photoUri: String?
)