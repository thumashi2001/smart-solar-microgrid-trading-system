package com.smartsolar.microgrid.data.api

import com.smartsolar.microgrid.data.api.dto.CompleteTransferResponse
import com.smartsolar.microgrid.data.api.dto.LoginRequest
import com.smartsolar.microgrid.data.api.dto.LoginResponse
import com.smartsolar.microgrid.data.api.dto.MicrogridNodeDto
import com.smartsolar.microgrid.data.api.dto.NearbyStationDto
import com.smartsolar.microgrid.data.api.dto.QrCodeResponse
import com.smartsolar.microgrid.data.api.dto.VerifyTransferRequest
import com.smartsolar.microgrid.data.api.dto.VerifyTransferResponse
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.PATCH
import retrofit2.http.POST
import retrofit2.http.Path
import retrofit2.http.Query

interface ApiService {

    @POST("api/auth/login")
    suspend fun login(@Body request: LoginRequest): Response<LoginResponse>

    @GET("api/reservations/{reservationId}/qr")
    suspend fun getReservationQr(
        @Path("reservationId") reservationId: String,
    ): Response<QrCodeResponse>

    @POST("api/transfers/verify")
    suspend fun verifyTransfer(
        @Body request: VerifyTransferRequest,
    ): Response<VerifyTransferResponse>

    @PATCH("api/transfers/{reservationId}/complete")
    suspend fun completeTransfer(
        @Path("reservationId") reservationId: String,
    ): Response<CompleteTransferResponse>

    @GET("api/microgridnodes")
    suspend fun getMicrogridNodes(): Response<List<MicrogridNodeDto>>

    @GET("api/microgridnodes/nearby")
    suspend fun getNearbyStations(
        @Query("lat") lat: Double,
        @Query("lng") lng: Double,
        @Query("radiusKm") radiusKm: Double = 10.0,
    ): Response<List<NearbyStationDto>>
}
