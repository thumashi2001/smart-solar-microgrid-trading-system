package com.smartsolar.microgrid.ui.operator

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.microgrid.app.MainActivity
import com.microgrid.app.local.AppDatabase
import com.smartsolar.microgrid.R
import com.smartsolar.microgrid.databinding.ActivityOperatorHomeBinding
import com.smartsolar.microgrid.ui.map.StationsMapActivity
import com.smartsolar.microgrid.ui.scanner.QrScannerActivity
import com.smartsolar.microgrid.util.smartSolarApp
import kotlinx.coroutines.launch

class OperatorHomeActivity : AppCompatActivity() {

    private lateinit var binding: ActivityOperatorHomeBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityOperatorHomeBinding.inflate(layoutInflater)
        setContentView(binding.root)

        lifecycleScope.launch {
            val session = smartSolarApp.sessionManager.getSession()
            if (session == null) {
                redirectToLogin()
                return@launch
            }
            binding.welcomeText.text = getString(R.string.welcome_operator, session.fullName)
        }

        binding.scanQrButton.setOnClickListener {
            startActivity(Intent(this, QrScannerActivity::class.java))
        }
        binding.stationsMapButton.setOnClickListener {
            startActivity(Intent(this, StationsMapActivity::class.java))
        }
        binding.logoutButton.setOnClickListener {
            lifecycleScope.launch {
                // Clear both session stores so the Compose login does not auto-login again
                smartSolarApp.sessionManager.clearSession()
                smartSolarApp.database.stationCacheDao().clear()
                AppDatabase.getDatabase(applicationContext).sessionDao().clearSession()
                redirectToLogin()
            }
        }
    }

    // Returns to the single Compose login screen (MainActivity).
    private fun redirectToLogin() {
        startActivity(
            Intent(this, MainActivity::class.java).apply {
                flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
            },
        )
        finish()
    }
}