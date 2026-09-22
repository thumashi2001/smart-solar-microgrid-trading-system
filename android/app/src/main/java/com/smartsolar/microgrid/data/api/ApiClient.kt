package com.smartsolar.microgrid.data.api

import com.smartsolar.microgrid.BuildConfig
import com.smartsolar.microgrid.data.session.SessionManager
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

object ApiClient {

    private lateinit var sessionManager: SessionManager

    fun init(sessionManager: SessionManager) {
        this.sessionManager = sessionManager
    }

    val api: ApiService by lazy { retrofit.create(ApiService::class.java) }

    val plainOkHttpClient: OkHttpClient by lazy { sharedOkHttpClient(sessionManager = null) }

    private val retrofit: Retrofit by lazy {
        Retrofit.Builder()
            .baseUrl(ApiUrlConfig.primary())
            .client(sharedOkHttpClient(sessionManager))
            .addConverterFactory(GsonConverterFactory.create())
            .build()
    }

    fun sharedOkHttpClient(sessionManager: SessionManager? = null): OkHttpClient {
        // BASIC avoids dumping Authorization headers / JWT / passwords in logcat.
        val logging = HttpLoggingInterceptor().apply {
            level = if (BuildConfig.DEBUG) {
                HttpLoggingInterceptor.Level.BASIC
            } else {
                HttpLoggingInterceptor.Level.NONE
            }
        }

        val builder = OkHttpClient.Builder()
            .connectTimeout(10, TimeUnit.SECONDS)
            .readTimeout(20, TimeUnit.SECONDS)
            .writeTimeout(20, TimeUnit.SECONDS)
            .addInterceptor(HostFallbackInterceptor())

        if (sessionManager != null) {
            builder.addInterceptor(AuthInterceptor(sessionManager))
        }

        builder.addInterceptor(logging)
        return builder.build()
    }
}
