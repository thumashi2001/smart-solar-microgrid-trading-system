package com.microgrid.app.data

import com.smartsolar.microgrid.data.api.ApiClient
import com.smartsolar.microgrid.data.api.ApiUrlConfig
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

object RetrofitClient {

    // Primary from BuildConfig/local.properties; HostFallbackInterceptor tries teammate then emulator.
    val instance: ApiService by lazy {
        Retrofit.Builder()
            .baseUrl(ApiUrlConfig.primary())
            .client(ApiClient.plainOkHttpClient)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
            .create(ApiService::class.java)
    }
}
