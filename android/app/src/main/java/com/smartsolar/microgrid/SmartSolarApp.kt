package com.smartsolar.microgrid

import android.app.Application
import com.smartsolar.microgrid.data.api.ApiClient
import com.smartsolar.microgrid.data.db.AppDatabase
import com.smartsolar.microgrid.data.session.SessionManager

class SmartSolarApp : Application() {

    lateinit var sessionManager: SessionManager
        private set

    lateinit var database: AppDatabase
        private set

    override fun onCreate() {
        super.onCreate()
        database = AppDatabase.getInstance(this)
        sessionManager = SessionManager(database.sessionDao())
        ApiClient.init(sessionManager)
    }
}
