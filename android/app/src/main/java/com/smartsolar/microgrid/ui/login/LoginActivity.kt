package com.smartsolar.microgrid.ui.login

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.isVisible
import androidx.lifecycle.lifecycleScope
import com.smartsolar.microgrid.R
import com.smartsolar.microgrid.data.api.ApiClient
import com.smartsolar.microgrid.data.api.ApiErrorParser
import com.smartsolar.microgrid.data.api.dto.LoginRequest
import com.smartsolar.microgrid.databinding.ActivityLoginBinding
import com.smartsolar.microgrid.ui.operator.OperatorHomeActivity
import com.smartsolar.microgrid.ui.prosumer.ProsumerQrEntryActivity
import com.smartsolar.microgrid.util.smartSolarApp
import com.smartsolar.microgrid.util.toast
import kotlinx.coroutines.launch

class LoginActivity : AppCompatActivity() {

    private lateinit var binding: ActivityLoginBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityLoginBinding.inflate(layoutInflater)
        setContentView(binding.root)

        binding.signInButton.setOnClickListener { attemptLogin() }

        lifecycleScope.launch {
            val session = smartSolarApp.sessionManager.getSession()
            when (session?.role) {
                GRID_OPERATOR_ROLE -> openOperatorHome()
                PROSUMER_ROLE -> openProsumerQrEntry()
            }
        }
    }

    private fun attemptLogin() {
        val identifier = binding.identifierInput.text?.toString()?.trim().orEmpty()
        val password = binding.passwordInput.text?.toString().orEmpty()

        binding.identifierLayout.error = if (identifier.isEmpty()) getString(R.string.hint_identifier) else null
        binding.passwordLayout.error = if (password.isEmpty()) getString(R.string.hint_password) else null
        if (identifier.isEmpty() || password.isEmpty()) return

        setLoading(true)
        lifecycleScope.launch {
            try {
                val response = ApiClient.api.login(LoginRequest(identifier, password))
                if (!response.isSuccessful || response.body() == null) {
                    toast(ApiErrorParser.messageFrom(response))
                    return@launch
                }
                val body = response.body()!!
                if (body.role != GRID_OPERATOR_ROLE && body.role != PROSUMER_ROLE) {
                    toast(getString(R.string.error_operator_only))
                    return@launch
                }
                smartSolarApp.sessionManager.saveSession(
                    token = body.token,
                    role = body.role,
                    fullName = body.fullName,
                    identifier = identifier,
                )
                if (body.role == GRID_OPERATOR_ROLE) {
                    openOperatorHome()
                } else {
                    openProsumerQrEntry()
                }
            } catch (_: Exception) {
                toast(getString(R.string.error_network))
            } finally {
                setLoading(false)
            }
        }
    }

    private fun setLoading(loading: Boolean) {
        binding.signInButton.isEnabled = !loading
        binding.progressBar.isVisible = loading
    }

    private fun openOperatorHome() {
        startActivity(Intent(this, OperatorHomeActivity::class.java))
        finish()
    }

    private fun openProsumerQrEntry() {
        startActivity(Intent(this, ProsumerQrEntryActivity::class.java))
        finish()
    }

    companion object {
        const val GRID_OPERATOR_ROLE = "GridOperator"
        const val PROSUMER_ROLE = "Prosumer"
    }
}
