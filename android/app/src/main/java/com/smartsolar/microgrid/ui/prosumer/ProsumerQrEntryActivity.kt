package com.smartsolar.microgrid.ui.prosumer

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import com.smartsolar.microgrid.databinding.ActivityProsumerQrEntryBinding
import com.smartsolar.microgrid.ui.login.LoginActivity
import com.smartsolar.microgrid.ui.qr.QrDisplayActivity
import com.smartsolar.microgrid.util.smartSolarApp
import kotlinx.coroutines.launch
import androidx.lifecycle.lifecycleScope
import com.smartsolar.microgrid.R

/**
 * Minimal prosumer entry to exercise QR display until Viman's booking detail screen
 * launches [QrDisplayActivity] directly with [QrDisplayActivity.EXTRA_RESERVATION_ID].
 */
class ProsumerQrEntryActivity : AppCompatActivity() {

    private lateinit var binding: ActivityProsumerQrEntryBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityProsumerQrEntryBinding.inflate(layoutInflater)
        setContentView(binding.root)

        binding.toolbar.setNavigationOnClickListener { finish() }
        binding.showQrButton.setOnClickListener {
            val reservationId = binding.reservationIdInput.text?.toString()?.trim().orEmpty()
            if (reservationId.isEmpty()) {
                binding.reservationIdLayout.error = getString(R.string.hint_reservation_id)
                return@setOnClickListener
            }
            startActivity(
                Intent(this, QrDisplayActivity::class.java).apply {
                    putExtra(QrDisplayActivity.EXTRA_RESERVATION_ID, reservationId)
                },
            )
        }
        binding.logoutButton.setOnClickListener {
            lifecycleScope.launch {
                smartSolarApp.sessionManager.clearSession()
                startActivity(
                    Intent(this@ProsumerQrEntryActivity, LoginActivity::class.java).apply {
                        flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
                    },
                )
            }
        }
    }
}
