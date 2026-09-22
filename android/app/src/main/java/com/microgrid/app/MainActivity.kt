package com.microgrid.app

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Surface
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import com.microgrid.app.ui.screens.*
import com.microgrid.app.ui.theme.MicrogridAppTheme

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

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            MicrogridAppTheme {
                Surface(modifier = Modifier.fillMaxSize()) {
                    var currentScreen by remember { mutableStateOf<Screen>(Screen.Login) }

                    // Logged-in user's info, kept alive across all screens
                    var loggedInFullName by remember { mutableStateOf("") }
                    var loggedInNic by remember { mutableStateOf("") }

                    when (currentScreen) {
                        is Screen.Login -> {
                            LoginScreen(
                                onLoginSuccess = { _, fullName, _, nic ->
                                    loggedInFullName = fullName
                                    loggedInNic = nic
                                    currentScreen = Screen.Profile
                                },
                                onNavigateToRegister = { currentScreen = Screen.Register }
                            )
                        }
                        is Screen.Register -> {
                            RegisterScreen(
                                onRegisterSuccess = { currentScreen = Screen.Login },
                                onNavigateToLogin = { currentScreen = Screen.Login }
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
                                onLogout = {
                                    loggedInFullName = ""
                                    loggedInNic = ""
                                    currentScreen = Screen.Login
                                }
                            )
                        }
                        is Screen.MyProfile -> {
                            MyProfileScreen(
                                fullName = loggedInFullName,
                                nic = loggedInNic,
                                onBack = { currentScreen = Screen.Profile }
                            )
                        }
                        is Screen.ChangePassword -> {
                            ChangePasswordScreen(
                                nic = loggedInNic,
                                fullName = loggedInFullName,
                                email = "",
                                phone = "",
                                onBack = { currentScreen = Screen.Profile }
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
}