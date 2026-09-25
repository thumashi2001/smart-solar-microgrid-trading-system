package com.microgrid.app.data

import retrofit2.Response
import retrofit2.http.*

interface ApiService {

    // ── Auth ──────────────────────────────────────────────────────────
    @POST("api/auth/login")
    suspend fun login(@Body request: LoginRequest): Response<LoginResponse>

    // ── Prosumer profile ──────────────────────────────────────────────
    @POST("api/prosumers/register")
    suspend fun registerProsumer(@Body request: ProsumerRegisterRequest): Response<Any>

    @PUT("api/prosumers/{nic}")
    suspend fun updateProsumer(
        @Path("nic") nic: String,
        @Body request: ProsumerRegisterRequest
    ): Response<Any>

    @PATCH("api/prosumers/{nic}/change-password")
    suspend fun changePassword(
        @Path("nic") nic: String,
        @Body request: ChangePasswordRequest
    ): Response<Any>

    // ── Slots (Component 2) ───────────────────────────────────────────
    /** List all available slots; optionally filter by stationId and/or date (yyyy-MM-dd). */
    @GET("api/slots")
    suspend fun getSlots(
        @Query("stationId") stationId: String? = null,
        @Query("date") date: String? = null
    ): Response<List<Slot>>

    /** Get a single slot by its MongoDB _id. */
    @GET("api/slots/{id}")
    suspend fun getSlot(@Path("id") id: String): Response<Slot>

    // ── Reservations (Component 2) ─────────────────────────────────────
    /** List reservations; filter by prosumerNic and/or status. */
    @GET("api/reservations")
    suspend fun getReservations(
        @Query("prosumerNic") prosumerNic: String? = null,
        @Query("status") status: String? = null
    ): Response<List<Reservation>>

    /** Get a single reservation by its MongoDB _id. */
    @GET("api/reservations/{id}")
    suspend fun getReservation(@Path("id") id: String): Response<Reservation>

    /** Create a new reservation on a given slot. */
    @POST("api/reservations")
    suspend fun createReservation(
        @Body request: CreateReservationRequest
    ): Response<Reservation>

    /** Update (cancel/amend) an existing reservation. */
    @PUT("api/reservations/{id}")
    suspend fun updateReservation(
        @Path("id") id: String,
        @Body request: UpdateReservationRequest
    ): Response<Reservation>
}