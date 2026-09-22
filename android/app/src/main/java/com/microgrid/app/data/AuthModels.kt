package com.microgrid.app.data

data class LoginRequest(
    val identifier: String,
    val password: String
)

data class LoginResponse(
    val token: String,
    val role: String,
    val fullName: String,
    val nic: String
)

data class ProsumerRegisterRequest(
    val nic: String,
    val fullName: String,
    val email: String,
    val phone: String,
    val passwordHash: String
)
data class ChangePasswordRequest(
    val newPassword: String
)