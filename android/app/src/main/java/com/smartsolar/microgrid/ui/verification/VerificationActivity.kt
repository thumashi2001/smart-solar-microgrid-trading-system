package com.smartsolar.microgrid.ui.verification

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.isVisible
import androidx.lifecycle.lifecycleScope
import com.smartsolar.microgrid.R
import com.smartsolar.microgrid.data.api.ApiClient
import com.smartsolar.microgrid.data.api.ApiErrorParser
import com.smartsolar.microgrid.databinding.ActivityVerificationBinding
import com.smartsolar.microgrid.ui.transfer.TransferResultActivity
import com.smartsolar.microgrid.util.toast
import kotlinx.coroutines.launch

class VerificationActivity : AppCompatActivity() {

    private lateinit var binding: ActivityVerificationBinding
    private var reservationId: String = ""

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityVerificationBinding.inflate(layoutInflater)
        setContentView(binding.root)

        binding.toolbar.setNavigationOnClickListener { finish() }

        reservationId = intent.getStringExtra(EXTRA_RESERVATION_ID).orEmpty()
        val eligible = intent.getBooleanExtra(EXTRA_ELIGIBLE, false)
        val reference = intent.getStringExtra(EXTRA_TRANSACTION_REFERENCE).orEmpty()
        val status = intent.getStringExtra(EXTRA_STATUS).orEmpty()
        val stationName = intent.getStringExtra(EXTRA_STATION_NAME).orEmpty()
        val stationLocation = intent.getStringExtra(EXTRA_STATION_LOCATION).orEmpty()
        val slotId = intent.getStringExtra(EXTRA_SLOT_ID).orEmpty()
        val prosumer = intent.getStringExtra(EXTRA_PROSUMER_MASKED).orEmpty()
        val message = intent.getStringExtra(EXTRA_MESSAGE).orEmpty()

        binding.reservationValue.text = getString(R.string.label_reservation_id) + ": " + reservationId
        binding.referenceValue.text = getString(R.string.label_transaction_ref) + ": " + reference
        binding.statusValue.text = getString(R.string.label_status) + ": " + status
        binding.stationValue.text = getString(R.string.label_station) + ": " +
            listOf(stationName, stationLocation).filter { it.isNotBlank() }.joinToString(" — ")
        binding.slotValue.text = getString(R.string.label_slot) + ": " + slotId
        binding.prosumerValue.text = getString(R.string.label_prosumer) + ": " + prosumer
        binding.messageValue.text = message

        binding.confirmButton.isEnabled = eligible
        if (!eligible) {
            toast(getString(R.string.not_eligible, message.ifBlank { status }))
        }

        binding.confirmButton.setOnClickListener { completeTransfer() }
    }

    private fun completeTransfer() {
        if (reservationId.isBlank()) return
        setLoading(true)
        lifecycleScope.launch {
            try {
                val response = ApiClient.api.completeTransfer(reservationId)
                if (!response.isSuccessful || response.body() == null) {
                    toast(ApiErrorParser.messageFrom(response))
                    return@launch
                }
                val body = response.body()!!
                startActivity(
                    Intent(this@VerificationActivity, TransferResultActivity::class.java).apply {
                        putExtra(TransferResultActivity.EXTRA_ALREADY_COMPLETED, body.alreadyCompleted)
                        putExtra(TransferResultActivity.EXTRA_MESSAGE, body.message)
                        putExtra(TransferResultActivity.EXTRA_RESERVATION_ID, body.reservationId)
                        putExtra(TransferResultActivity.EXTRA_STATUS, body.status)
                    },
                )
                finish()
            } catch (_: Exception) {
                toast(getString(R.string.error_network))
            } finally {
                setLoading(false)
            }
        }
    }

    private fun setLoading(loading: Boolean) {
        binding.confirmButton.isEnabled = !loading && intent.getBooleanExtra(EXTRA_ELIGIBLE, false)
        binding.progressBar.isVisible = loading
    }

    companion object {
        const val EXTRA_RESERVATION_ID = "extra_reservation_id"
        const val EXTRA_ELIGIBLE = "extra_eligible"
        const val EXTRA_TRANSACTION_REFERENCE = "extra_transaction_reference"
        const val EXTRA_STATUS = "extra_status"
        const val EXTRA_STATION_NAME = "extra_station_name"
        const val EXTRA_STATION_LOCATION = "extra_station_location"
        const val EXTRA_SLOT_ID = "extra_slot_id"
        const val EXTRA_PROSUMER_MASKED = "extra_prosumer_masked"
        const val EXTRA_MESSAGE = "extra_message"

        fun intentFromVerify(
            context: android.content.Context,
            verify: com.smartsolar.microgrid.data.api.dto.VerifyTransferResponse,
        ): Intent {
            return Intent(context, VerificationActivity::class.java).apply {
                putExtra(EXTRA_RESERVATION_ID, verify.reservationId)
                putExtra(EXTRA_ELIGIBLE, verify.eligible)
                putExtra(EXTRA_TRANSACTION_REFERENCE, verify.transactionReference)
                putExtra(EXTRA_STATUS, verify.status)
                putExtra(EXTRA_STATION_NAME, verify.stationName)
                putExtra(EXTRA_STATION_LOCATION, verify.stationLocation)
                putExtra(EXTRA_SLOT_ID, verify.slotId)
                putExtra(EXTRA_PROSUMER_MASKED, verify.prosumerNicMasked)
                putExtra(EXTRA_MESSAGE, verify.message)
            }
        }
    }
}
