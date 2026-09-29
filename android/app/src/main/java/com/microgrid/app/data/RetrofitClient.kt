package com.microgrid.app.data

import com.smartsolar.microgrid.data.api.ApiClient
import com.smartsolar.microgrid.data.api.ApiUrlConfig
import okhttp3.Interceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

/**
 * Compose booking/auth client.
 * Uses the single selected API_BASE_URL for the session (no silent host failover).
 * Adds ngrok browser-warning skip only when the configured base URL is an ngrok host.
 */
object RetrofitClient {

    private val ngrokSkipInterceptor = Interceptor { chain ->
        val request = chain.request()
        val host = request.url.host
        if (host.contains("ngrok", ignoreCase = true)) {
            chain.proceed(
                request.newBuilder()
                    .header("ngrok-skip-browser-warning", "true")
                    .build(),
            )
        } else {
            chain.proceed(request)
        }
    }

    val instance: ApiService by lazy {
        val client = ApiClient.sharedOkHttpClient(sessionManager = ApiClient.sessionManager)
            .newBuilder()
            .addInterceptor(ngrokSkipInterceptor)
            .build()

        Retrofit.Builder()
            .baseUrl(ApiUrlConfig.primary())
            .client(client)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
            .create(ApiService::class.java)
    }
}
