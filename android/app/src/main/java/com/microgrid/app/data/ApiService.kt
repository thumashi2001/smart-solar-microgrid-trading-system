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

    // ── Microgrid Nodes / Stations (Component 2 station selection) ─────
    /** Returns all microgrid nodes. Used for station selection in booking flow. */
    @GET("api/microgridnodes")
    suspend fun getMicrogridNodes(): Response<List<MicrogridNode>>

    // ── Slots (Component 2) ───────────────────────────────────────────
    /**
     * GET api/slots
     * Returns all slots. Optional query parameters (stationId, date) supported by the backend.
     */
    @GET("api/slots")
    suspend fun getSlots(
        @Query("stationId") stationId: String? = null,
        @Query("date") date: String? = null
    ): Response<List<Slot>>

    /** GET api/slots/{id} — single slot by MongoDB _id */
    @GET("api/slots/{id}")
    suspend fun getSlot(@Path("id") id: String): Response<Slot>

    // ── Reservations (Component 2) ─────────────────────────────────────

    /**
     * GET api/reservations
     * Returns ALL reservations (no filter query params — backend does not support them).
     * For backoffice monitoring only. Do NOT use this to get a prosumer's own reservations.
     */
    @GET("api/reservations")
    suspend fun getAllReservations(): Response<List<Reservation>>

    /** GET api/reservations/{id} — single reservation by MongoDB _id */
    @GET("api/reservations/{id}")
    suspend fun getReservation(@Path("id") id: String): Response<Reservation>

    /**
     * GET api/reservations/history/{prosumerNic}
     * Returns ALL reservations for a specific prosumer (all statuses).
     * Use this for My Bookings / history.
     */
    @GET("api/reservations/history/{prosumerNic}")
    suspend fun getReservationHistory(@Path("prosumerNic") prosumerNic: String): Response<List<Reservation>>

    /**
     * POST api/reservations
     * Creates a new reservation.
     * Backend DTO: { prosumerNic, stationId, slotId }
     * No energyAmount, no notes — those fields are NOT in the backend DTO.
     */
    @POST("api/reservations")
    suspend fun createReservation(
        @Body request: CreateReservationRequest
    ): Response<Reservation>

    /**
     * PUT api/reservations/{id}
     * Moves reservation to a DIFFERENT slot.
     * Backend DTO: { stationId, slotId }
     * NOT a status-change endpoint.
     */
    @PUT("api/reservations/{id}")
    suspend fun updateReservation(
        @Path("id") id: String,
        @Body request: UpdateReservationRequest
    ): Response<Reservation>

    /**
     * DELETE api/reservations/{id}
     * Cancels a reservation.
     * Backend enforces 12-hour notice. Returns 400 if < 12 hours to slot start.
     * Returns 400 if already Cancelled or Completed.
     */
    @DELETE("api/reservations/{id}")
    suspend fun cancelReservation(@Path("id") id: String): Response<Any>
}