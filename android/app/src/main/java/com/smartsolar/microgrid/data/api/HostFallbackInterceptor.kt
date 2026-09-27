package com.smartsolar.microgrid.data.api

import android.util.Log
import com.smartsolar.microgrid.BuildConfig
import okhttp3.HttpUrl.Companion.toHttpUrl
import okhttp3.Interceptor
import okhttp3.Response

/**
 * Pins all requests to the single selected API_BASE_URL for this app session.
 * Does not silently retry against teammate/fallback hosts (avoids replaying auth/writes).
 * Adds ngrok browser-warning skip when the selected host is ngrok.
 */
class SelectedHostInterceptor : Interceptor {

    override fun intercept(chain: Interceptor.Chain): Response {
        val original = chain.request()
        val primary = ApiUrlConfig.primary().toHttpUrl()
        val pinnedUrl = original.url.newBuilder()
            .scheme(primary.scheme)
            .host(primary.host)
            .port(primary.port)
            .build()

        val builder = original.newBuilder().url(pinnedUrl)
        if (primary.host.contains("ngrok", ignoreCase = true)) {
            builder.header("ngrok-skip-browser-warning", "true")
        }

        if (BuildConfig.DEBUG) {
            Log.d(TAG, "API host: ${pinnedUrl.host}:${pinnedUrl.port}")
        }

        return chain.proceed(builder.build())
    }

    companion object {
        private const val TAG = "ApiSelectedHost"
    }
}

/** @deprecated Name retained for call-site clarity; behavior is single-host only. */
typealias HostFallbackInterceptor = SelectedHostInterceptor

object ApiUrlConfig {
    fun primary(): String = normalize(BuildConfig.API_BASE_URL)

    fun fallback(): String = normalize(BuildConfig.API_FALLBACK_URL)

    fun emulator(): String = normalize(BuildConfig.API_EMULATOR_URL)

    fun normalize(url: String): String {
        val trimmed = url.trim()
        if (trimmed.isEmpty()) return ""
        return if (trimmed.endsWith("/")) trimmed else "$trimmed/"
    }
}
