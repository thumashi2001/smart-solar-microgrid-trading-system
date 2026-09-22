package com.smartsolar.microgrid.ui.qr

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.isVisible
import androidx.lifecycle.lifecycleScope
import com.smartsolar.microgrid.R
import com.smartsolar.microgrid.data.api.ApiClient
import com.smartsolar.microgrid.data.api.ApiErrorParser
import com.smartsolar.microgrid.databinding.ActivityQrDisplayBinding
import com.smartsolar.microgrid.ui.login.LoginActivity
import com.smartsolar.microgrid.util.QrEncoder
import com.smartsolar.microgrid.util.smartSolarApp
import com.smartsolar.microgrid.util.toast
import kotlinx.coroutines.launch

/**
 * Shows a reservation QR for authenticated users (typically Prosumers).
 *
 * Teammate entry point:
 * ```
 * startActivity(Intent(context, QrDisplayActivity::class.java).apply {
 *     putExtra(QrDisplayActivity.EXTRA_RESERVATION_ID, reservationId)
 * })
 * ```
 */
class QrDisplayActivity : AppCompatActivity() {

    private lateinit var binding: ActivityQrDisplayBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityQrDisplayBinding.inflate(layoutInflater)
        setContentView(binding.root)

        binding.toolbar.setNavigationOnClickListener { onBackPressedDispatcher.onBackPressed() }

        val reservationId = intent.getStringExtra(EXTRA_RESERVATION_ID)?.trim().orEmpty()
        if (reservationId.isEmpty()) {
            showError(getString(R.string.error_generic))
            return
        }

        lifecycleScope.launch {
            if (!smartSolarApp.sessionManager.isLoggedIn()) {
                toast(getString(R.string.error_generic))
                startActivity(android.content.Intent(this@QrDisplayActivity, LoginActivity::class.java))
                finish()
                return@launch
            }
            loadQr(reservationId)
        }
    }

    private suspend fun loadQr(reservationId: String) {
        showLoading(true)
        try {
            val response = ApiClient.api.getReservationQr(reservationId)
            if (!response.isSuccessful || response.body() == null) {
                showError(getString(R.string.qr_error, ApiErrorParser.messageFrom(response)))
                return
            }
            val qr = response.body()!!
            if (qr.payload.isBlank()) {
                showError(getString(R.string.qr_error, "Empty payload"))
                return
            }
            binding.qrImage.setImageBitmap(QrEncoder.encodeToBitmap(qr.payload))
            binding.payloadText.text = getString(R.string.qr_payload_label) + ": " + qr.payload
            binding.qrImage.isVisible = true
            binding.payloadText.isVisible = true
            binding.errorText.isVisible = false
        } catch (_: Exception) {
            showError(getString(R.string.qr_error, getString(R.string.error_network)))
        } finally {
            showLoading(false)
        }
    }

    private fun showLoading(loading: Boolean) {
        binding.loadingIndicator.isVisible = loading
    }

    private fun showError(message: String) {
        binding.errorText.text = message
        binding.errorText.isVisible = true
        binding.qrImage.isVisible = false
        binding.payloadText.isVisible = false
        binding.loadingIndicator.isVisible = false
    }

    companion object {
        const val EXTRA_RESERVATION_ID = "com.smartsolar.microgrid.extra.RESERVATION_ID"
    }
}
