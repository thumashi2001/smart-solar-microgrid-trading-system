package com.microgrid.app.data

import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.PATCH
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path

interface ApiService {

    @POST("api/auth/login")
    suspend fun login(@Body request: LoginRequest): Response<LoginResponse>

    @POST("api/prosumers/register")
    suspend fun registerProsumer(@Body request: ProsumerRegisterRequest): Response<Any>

    @PUT("api/prosumers/{nic}")
    suspend fun updateProsumer(@Path("nic") nic: String, @Body request: ProsumerRegisterRequest): Response<Any>

    @PATCH("api/prosumers/{nic}/change-password")
    suspend fun changePassword(@Path("nic") nic: String, @Body request: ChangePasswordRequest): Response<Any>
}