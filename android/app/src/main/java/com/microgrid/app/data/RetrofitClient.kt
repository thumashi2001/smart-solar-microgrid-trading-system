package com.microgrid.app.data

import okhttp3.Interceptor
import okhttp3.OkHttpClient
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

object RetrofitClient {

    private const val BASE_URL = "https://query-flanked-unfailing.ngrok-free.dev/"

    private val skipNgrokWarningInterceptor = Interceptor { chain ->
        val request = chain.request().newBuilder()
            .addHeader("ngrok-skip-browser-warning", "true")
            .build()
        chain.proceed(request)
    }

    private val client = OkHttpClient.Builder()
        .addInterceptor(skipNgrokWarningInterceptor)
        .build()

    val instance: ApiService by lazy {
        Retrofit.Builder()
            .baseUrl(BASE_URL)
            .client(client)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
            .create(ApiService::class.java)
    }
}