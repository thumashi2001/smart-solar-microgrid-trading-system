package com.smartsolar.microgrid.data.api

import com.smartsolar.microgrid.data.session.SessionManager
import kotlinx.coroutines.runBlocking
import okhttp3.Interceptor
import okhttp3.Response

class AuthInterceptor(
    private val sessionManager: SessionManager,
) : Interceptor {

    override fun intercept(chain: Interceptor.Chain): Response {
        val original = chain.request()
        val token = runBlocking { sessionManager.getToken() }
        if (token.isNullOrBlank()) {
            return chain.proceed(original)
        }
        val authed = original.newBuilder()
            .header("Authorization", "Bearer $token")
            .build()
        return chain.proceed(authed)
    }
}
