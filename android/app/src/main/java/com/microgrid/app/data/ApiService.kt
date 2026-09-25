package com.microgrid.app.data

import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.PATCH
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path

interface ApiService {

    // Authentication
    @POST("api/auth/login")
    suspend fun login(
        @Body request: LoginRequest
    ): Response<LoginResponse>


    // Prosumer registration
    @POST("api/prosumers/register")
    suspend fun registerProsumer(
        @Body request: ProsumerRegisterRequest
    ): Response<Any>


    // Update prosumer profile
    @PUT("api/prosumers/{nic}")
    suspend fun updateProsumer(
        @Path("nic") nic: String,
        @Body request: ProsumerRegisterRequest
    ): Response<Any>


    // Change prosumer password
    @PATCH("api/prosumers/{nic}/change-password")
    suspend fun changePassword(
        @Path("nic") nic: String,
        @Body request: ChangePasswordRequest
    ): Response<Any>


    // Get reservation history for a specific prosumer
    @GET("api/reservations/history/{prosumerNic}")
    suspend fun getReservationHistory(
        @Path("prosumerNic") prosumerNic: String
    ): Response<List<ReservationResponse>>


    // Get all available energy slots
    @GET("api/slots")
    suspend fun getSlots(): Response<List<SlotResponse>>


    // Get all microgrid nodes
    @GET("api/microgridnodes")
    suspend fun getMicrogridNodes(): Response<List<MicrogridNodeResponse>>

}