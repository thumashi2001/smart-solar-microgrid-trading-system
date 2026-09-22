package com.smartsolar.microgrid.data.api

import android.util.Log
import com.smartsolar.microgrid.BuildConfig
import okhttp3.HttpUrl.Companion.toHttpUrl
import okhttp3.Interceptor
import okhttp3.Response
import java.io.IOException
import java.net.ConnectException
import java.net.SocketTimeoutException
import java.net.UnknownHostException

/**
 * Tries the request against the configured primary host first.
 * On connectivity failures only, retries against fallback host(s).
 * Does not retry on normal HTTP status codes (4xx/5xx).
 */
class HostFallbackInterceptor : Interceptor {

    override fun intercept(chain: Interceptor.Chain): Response {
        val original = chain.request()
        val hosts = ApiUrlConfig.hostChain()
        var lastError: IOException? = null

        hosts.forEachIndexed { index, hostUrl ->
            val nextRequest = if (index == 0 && original.url.host == hostUrl.host && original.url.port == hostUrl.port) {
                original
            } else {
                val rewritten = original.url.newBuilder()
                    .scheme(hostUrl.scheme)
                    .host(hostUrl.host)
                    .port(hostUrl.port)
                    .build()
                original.newBuilder().url(rewritten).build()
            }

            if (BuildConfig.DEBUG) {
                Log.d(TAG, "Trying API host: ${nextRequest.url.host}:${nextRequest.url.port}")
            }

            try {
                return chain.proceed(nextRequest)
            } catch (ex: IOException) {
                if (!isConnectivityFailure(ex) || index == hosts.lastIndex) {
                    throw ex
                }
                lastError = ex
                if (BuildConfig.DEBUG) {
                    Log.d(TAG, "API host unreachable (${ex.javaClass.simpleName}); trying fallback")
                }
            }
        }

        throw lastError ?: IOException("No API hosts available")
    }

    private fun isConnectivityFailure(ex: IOException): Boolean {
        return ex is ConnectException ||
            ex is SocketTimeoutException ||
            ex is UnknownHostException ||
            ex.message?.contains("Failed to connect", ignoreCase = true) == true ||
            ex.message?.contains("Unable to resolve host", ignoreCase = true) == true ||
            ex.message?.contains("timeout", ignoreCase = true) == true ||
            ex.message?.contains("ECONNREFUSED", ignoreCase = true) == true ||
            ex.message?.contains("ENETUNREACH", ignoreCase = true) == true
    }

    companion object {
        private const val TAG = "ApiHostFallback"
    }
}

object ApiUrlConfig {
    fun primary(): String = normalize(BuildConfig.API_BASE_URL)

    fun fallback(): String = normalize(BuildConfig.API_FALLBACK_URL)

    fun emulator(): String = normalize(BuildConfig.API_EMULATOR_URL)

    fun hostChain(): List<okhttp3.HttpUrl> {
        val urls = linkedSetOf<String>()
        urls.add(primary())
        // Prefer emulator loopback before teammate LAN IP when debugging (faster on AVD).
        if (BuildConfig.DEBUG && emulator().isNotBlank() && emulator() != primary()) {
            urls.add(emulator())
        }
        if (fallback().isNotBlank() && fallback() != primary() && fallback() != emulator()) {
            urls.add(fallback())
        }
        return urls.map { it.toHttpUrl() }
    }

    fun normalize(url: String): String {
        val trimmed = url.trim()
        if (trimmed.isEmpty()) return ""
        return if (trimmed.endsWith("/")) trimmed else "$trimmed/"
    }
}
