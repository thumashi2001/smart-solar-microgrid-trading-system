package com.smartsolar.microgrid.data.session

import com.smartsolar.microgrid.data.db.SessionDao
import com.smartsolar.microgrid.data.db.SessionEntity

class SessionManager(
    private val sessionDao: SessionDao,
) {

    suspend fun getToken(): String? = sessionDao.getSession()?.token

    suspend fun getSession(): SessionEntity? = sessionDao.getSession()

    suspend fun isLoggedIn(): Boolean = getToken()?.isNotBlank() == true

    suspend fun saveSession(
        token: String,
        role: String,
        fullName: String,
        identifier: String,
    ) {
        sessionDao.upsert(
            SessionEntity(
                token = token,
                role = role,
                fullName = fullName,
                identifier = identifier,
            ),
        )
    }

    suspend fun clearSession() {
        sessionDao.clear()
    }
}
