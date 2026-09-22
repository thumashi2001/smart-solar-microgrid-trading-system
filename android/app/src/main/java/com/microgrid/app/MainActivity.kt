package com.microgrid.app

import android.content.Intent
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Surface
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.lifecycle.lifecycleScope
import com.microgrid.app.ui.screens.*
import com.microgrid.app.ui.theme.MicrogridAppTheme
import com.smartsolar.microgrid.SmartSolarApp
import com.smartsolar.microgrid.ui.map.StationsMapActivity
import com.smartsolar.microgrid.ui.operator.OperatorHomeActivity
import com.smartsolar.microgrid.ui.prosumer.ProsumerQrEntryActivity
import kotlinx.coroutines.launch

sealed class Screen {
    object Login : Screen()
    object Register : Screen()
    object Profile : Screen()
    object MyProfile : Screen()
    object ChangePassword : Screen()
    object Notifications : Screen()
    object HelpSupport : Screen()
    object About : Screen()
}

/**
 * Thumashi auth/profile Compose host.
 * Also exposes Suwani QR entry and Maps from the profile menu so one app navigation covers both.
 */
class MainActivity : ComponentActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()

        val startRegister = intent.getBooleanExtra(EXTRA_START_REGISTER, false)
        val openProfile = intent.getBooleanExtra(EXTRA_OPEN_PROFILE, false)
        val initialFullName = intent.getStringExtra(EXTRA_FULL_NAME).orEmpty()
        val initialNic = intent.getStringExtra(EXTRA_NIC).orEmpty()

        setContent {
            MicrogridAppTheme {
                Surface(modifier = Modifier.fillMaxSize()) {
                    var currentScreen by remember {
                        mutableStateOf(
                            when {
                                startRegister -> Screen.Register
                                openProfile && initialFullName.isNotBlank() -> Screen.Profile
                                else -> Screen.Login
                            },
                        )
                    }

                    var loggedInFullName by remember { mutableStateOf(initialFullName) }
                    var loggedInNic by remember { mutableStateOf(initialNic) }

                    when (currentScreen) {
                        is Screen.Login -> {
                            LoginScreen(
                                onLoginSuccess = { role, fullName, token, nic ->
                                    loggedInFullName = fullName
                                    loggedInNic = nic.ifBlank { "" }
                                    lifecycleScope.launch {
                                        (application as SmartSolarApp).sessionManager.saveSession(
                                            token = token,
                                            role = role,
                                            fullName = fullName,
                                            identifier = nic.ifBlank { fullName },
                                        )
                                    }
                                    when (role) {
                                        "GridOperator" -> {
                                            startActivity(Intent(this@MainActivity, OperatorHomeActivity::class.java))
                                            finish()
                                        }
                                        else -> currentScreen = Screen.Profile
                                    }
                                },
                                onNavigateToRegister = { currentScreen = Screen.Register },
                            )
                        }
                        is Screen.Register -> {
                            RegisterScreen(
                                onRegisterSuccess = { currentScreen = Screen.Login },
                                onNavigateToLogin = { currentScreen = Screen.Login },
                            )
                        }
                        is Screen.Profile -> {
                            ProfileScreen(
                                fullName = loggedInFullName,
                                nic = loggedInNic,
                                onMyProfile = { currentScreen = Screen.MyProfile },
                                onChangePassword = { currentScreen = Screen.ChangePassword },
                                onNotifications = { currentScreen = Screen.Notifications },
                                onHelpSupport = { currentScreen = Screen.HelpSupport },
                                onAbout = { currentScreen = Screen.About },
                                onReservationQr = {
                                    startActivity(Intent(this@MainActivity, ProsumerQrEntryActivity::class.java))
                                },
                                onStationsMap = {
                                    startActivity(Intent(this@MainActivity, StationsMapActivity::class.java))
                                },
                                onLogout = {
                                    loggedInFullName = ""
                                    loggedInNic = ""
                                    lifecycleScope.launch {
                                        (application as SmartSolarApp).sessionManager.clearSession()
                                        startActivity(
                                            Intent(
                                                this@MainActivity,
                                                com.smartsolar.microgrid.ui.login.LoginActivity::class.java,
                                            ).apply {
                                                flags = Intent.FLAG_ACTIVITY_NEW_TASK or
                                                    Intent.FLAG_ACTIVITY_CLEAR_TASK
                                            },
                                        )
                                        finish()
                                    }
                                },
                            )
                        }
                        is Screen.MyProfile -> {
                            MyProfileScreen(
                                fullName = loggedInFullName,
                                nic = loggedInNic,
                                onBack = { currentScreen = Screen.Profile },
                            )
                        }
                        is Screen.ChangePassword -> {
                            ChangePasswordScreen(
                                nic = loggedInNic,
                                fullName = loggedInFullName,
                                email = "",
                                phone = "",
                                onBack = { currentScreen = Screen.Profile },
                            )
                        }
                        is Screen.Notifications -> {
                            NotificationsScreen(onBack = { currentScreen = Screen.Profile })
                        }
                        is Screen.HelpSupport -> {
                            HelpSupportScreen(onBack = { currentScreen = Screen.Profile })
                        }
                        is Screen.About -> {
                            AboutScreen(onBack = { currentScreen = Screen.Profile })
                        }
                    }
                }
            }
        }
    }

    companion object {
        const val EXTRA_OPEN_PROFILE = "extra_open_profile"
        const val EXTRA_START_REGISTER = "extra_start_register"
        const val EXTRA_FULL_NAME = "extra_full_name"
        const val EXTRA_NIC = "extra_nic"
    }
}
