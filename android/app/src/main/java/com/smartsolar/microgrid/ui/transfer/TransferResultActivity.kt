package com.smartsolar.microgrid.ui.transfer

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import com.smartsolar.microgrid.R
import com.smartsolar.microgrid.databinding.ActivityTransferResultBinding
import com.smartsolar.microgrid.ui.operator.OperatorHomeActivity

class TransferResultActivity : AppCompatActivity() {

    private lateinit var binding: ActivityTransferResultBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityTransferResultBinding.inflate(layoutInflater)
        setContentView(binding.root)

        val alreadyCompleted = intent.getBooleanExtra(EXTRA_ALREADY_COMPLETED, false)
        val message = intent.getStringExtra(EXTRA_MESSAGE).orEmpty()
        val reservationId = intent.getStringExtra(EXTRA_RESERVATION_ID).orEmpty()
        val status = intent.getStringExtra(EXTRA_STATUS).orEmpty()

        binding.resultTitle.text = if (alreadyCompleted) {
            getString(R.string.transfer_already_title)
        } else {
            getString(R.string.transfer_success_title)
        }

        val details = buildString {
            if (reservationId.isNotBlank()) append(getString(R.string.label_reservation_id)).append(": ").append(reservationId).append('\n')
            if (status.isNotBlank()) append(getString(R.string.label_status)).append(": ").append(status).append('\n')
            if (message.isNotBlank()) append(message)
        }
        binding.resultMessage.text = details.trim()

        binding.doneButton.setOnClickListener {
            startActivity(
                Intent(this, OperatorHomeActivity::class.java).apply {
                    flags = Intent.FLAG_ACTIVITY_CLEAR_TOP
                },
            )
            finish()
        }
    }

    companion object {
        const val EXTRA_ALREADY_COMPLETED = "extra_already_completed"
        const val EXTRA_MESSAGE = "extra_message"
        const val EXTRA_RESERVATION_ID = "extra_reservation_id"
        const val EXTRA_STATUS = "extra_status"
    }
}
