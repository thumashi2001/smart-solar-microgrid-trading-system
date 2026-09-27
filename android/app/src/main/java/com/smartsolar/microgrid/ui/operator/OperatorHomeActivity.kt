package com.smartsolar.microgrid.ui.operator

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.smartsolar.microgrid.R
import com.smartsolar.microgrid.databinding.ActivityOperatorHomeBinding
import com.smartsolar.microgrid.ui.login.LoginActivity
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
                smartSolarApp.sessionManager.clearSession()
                smartSolarApp.database.stationCacheDao().clear()
                redirectToLogin()
            }
        }
    }

    private fun redirectToLogin() {
        startActivity(
            Intent(this, LoginActivity::class.java).apply {
                flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
            },
        )
        finish()
    }
}
