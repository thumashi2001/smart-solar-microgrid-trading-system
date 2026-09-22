package com.smartsolar.microgrid.data.api

import com.google.gson.Gson
import com.smartsolar.microgrid.data.api.dto.ApiMessageResponse
import retrofit2.Response

object ApiErrorParser {

    private val gson = Gson()

    fun messageFrom(response: Response<*>): String {
        val errorBody = response.errorBody()?.string()
        if (!errorBody.isNullOrBlank()) {
            runCatching {
                gson.fromJson(errorBody, ApiMessageResponse::class.java)?.message
            }.getOrNull()?.takeIf { it.isNotBlank() }?.let { return it }
            if (errorBody.length <= 200) return errorBody
        }
        return when (response.code()) {
            401 -> "Unauthorized."
            403 -> "Forbidden."
            404 -> "Not found."
            else -> "Request failed (${response.code()})."
        }
    }
}
