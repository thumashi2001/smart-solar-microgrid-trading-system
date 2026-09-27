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
import com.microgrid.app.local.AppDatabase
import com.microgrid.app.local.SessionEntity
import com.microgrid.app.ui.screens.*
import com.microgrid.app.ui.theme.MicrogridAppTheme
import com.smartsolar.microgrid.SmartSolarApp
import com.smartsolar.microgrid.ui.login.LoginActivity
import com.smartsolar.microgrid.ui.map.StationsMapActivity
import com.smartsolar.microgrid.ui.operator.OperatorHomeActivity
import com.smartsolar.microgrid.ui.prosumer.ProsumerQrEntryActivity
import kotlinx.coroutines.launch

sealed class Screen {
    object Login : Screen()
    object Register : Screen()

    object Dashboard : Screen()
    object Bookings : Screen()
    object BookingHistory : Screen()
    object SearchBookings : Screen()

    data class BookingDetails(
        val bookingId: String
    ) : Screen()

    object Profile : Screen()
    object MyProfile : Screen()
    object ChangePassword : Screen()
    object Notifications : Screen()
    object HelpSupport : Screen()
    object About : Screen()
}

/**
 * Thumashi auth/profile/booking Compose host.
 * Routes GridOperator to Suwani operator home; Prosumer keeps dashboard + QR/Maps entry points.
 */
class MainActivity : ComponentActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()

        val db = AppDatabase.getDatabase(applicationContext)
        val startRegister = intent.getBooleanExtra(EXTRA_START_REGISTER, false)
        val openProfile = intent.getBooleanExtra(EXTRA_OPEN_PROFILE, false)
        val initialFullName = intent.getStringExtra(EXTRA_FULL_NAME).orEmpty()
        val initialNic = intent.getStringExtra(EXTRA_NIC).orEmpty()

        setContent {
            MicrogridAppTheme {
                Surface(modifier = Modifier.fillMaxSize()) {
                    var currentScreen by remember {
                        mutableStateOf<Screen>(
                            when {
                                startRegister -> Screen.Register
                                openProfile && initialFullName.isNotBlank() -> Screen.Profile
                                else -> Screen.Login
                            },
                        )
                    }
                    var loggedInFullName by remember { mutableStateOf(initialFullName) }
                    var loggedInNic by remember { mutableStateOf(initialNic) }
                    var isCheckingSession by remember { mutableStateOf(!openProfile && !startRegister) }
                    val scope = rememberCoroutineScope()
                    val smartSolarApp = application as SmartSolarApp

                    LaunchedEffect(Unit) {
                        if (!isCheckingSession) return@LaunchedEffect
                        val session = db.sessionDao().getSession()
                        if (session != null) {
                            loggedInFullName = session.fullName
                            loggedInNic = session.nic
                            smartSolarApp.sessionManager.saveSession(
                                token = session.token,
                                role = session.role,
                                fullName = session.fullName,
                                identifier = session.nic.ifBlank { session.fullName },
                            )
                            when (session.role) {
                                "GridOperator" -> {
                                    startActivity(Intent(this@MainActivity, OperatorHomeActivity::class.java))
                                    finish()
                                    return@LaunchedEffect
                                }
                                else -> currentScreen = Screen.Dashboard
                            }
                        }
                        isCheckingSession = false
                    }

                    if (isCheckingSession) {
                        return@Surface
                    }

                    when (currentScreen) {
                        is Screen.Login -> {
                            LoginScreen(
                                onLoginSuccess = { role, fullName, token, nic ->
                                    loggedInFullName = fullName
                                    loggedInNic = nic
                                    scope.launch {
                                        db.sessionDao().saveSession(
                                            SessionEntity(
                                                nic = nic,
                                                fullName = fullName,
                                                token = token,
                                                role = role,
                                                photoUri = null,
                                            ),
                                        )
                                        smartSolarApp.sessionManager.saveSession(
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
                                        "Prosumer" -> currentScreen = Screen.Dashboard
                                        else -> {
                                            // Do not elevate Backoffice/unknown roles into operator or prosumer flows.
                                            scope.launch {
                                                db.sessionDao().clearSession()
                                                smartSolarApp.sessionManager.clearSession()
                                            }
                                            loggedInFullName = ""
                                            loggedInNic = ""
                                            currentScreen = Screen.Login
                                        }
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
                        is Screen.Dashboard -> {
                            ProsumerDashboardScreen(
                                fullName = loggedInFullName,
                                nic = loggedInNic,
                                onProfileClick = { currentScreen = Screen.Profile },
                                onBookingsClick = { currentScreen = Screen.Bookings },
                                onHistoryClick = { currentScreen = Screen.BookingHistory },
                                onSearchClick = { currentScreen = Screen.SearchBookings },
                            )
                        }
                        is Screen.Bookings -> {
                            BookingsScreen(
                                nic = loggedInNic,
                                onBackClick = { currentScreen = Screen.Dashboard },
                                onBookingClick = { bookingId -> currentScreen = Screen.BookingDetails(bookingId) },
                                onHomeClick = { currentScreen = Screen.Dashboard },
                                onProfileClick = { currentScreen = Screen.Profile },
                            )
                        }
                        is Screen.BookingHistory -> {
                            BookingHistoryScreen(
                                nic = loggedInNic,
                                onBackClick = { currentScreen = Screen.Dashboard },
                                onBookingClick = { bookingId -> currentScreen = Screen.BookingDetails(bookingId) },
                                onHomeClick = { currentScreen = Screen.Dashboard },
                                onBookingsClick = { currentScreen = Screen.Bookings },
                                onProfileClick = { currentScreen = Screen.Profile },
                            )
                        }
                        is Screen.SearchBookings -> {
                            SearchBookingsScreen(
                                nic = loggedInNic,
                                onBackClick = { currentScreen = Screen.Dashboard },
                                onBookingClick = { bookingId -> currentScreen = Screen.BookingDetails(bookingId) },
                            )
                        }
                        is Screen.BookingDetails -> {
                            val screen = currentScreen as Screen.BookingDetails
                            BookingDetailsScreen(
                                bookingId = screen.bookingId,
                                nic = loggedInNic,
                                onBackClick = { currentScreen = Screen.Bookings },
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
                                    scope.launch {
                                        db.sessionDao().clearSession()
                                        smartSolarApp.sessionManager.clearSession()
                                    }
                                    loggedInFullName = ""
                                    loggedInNic = ""
                                    startActivity(
                                        Intent(this@MainActivity, LoginActivity::class.java).apply {
                                            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
                                        },
                                    )
                                    finish()
                                },
                                onBackToHome = { currentScreen = Screen.Dashboard },
                            )
                        }
                        is Screen.MyProfile -> {
                            MyProfileScreen(
                                fullName = loggedInFullName,
                                nic = loggedInNic,
                                onBack = { currentScreen = Screen.Profile },
                                onAccountDeactivated = {
                                    scope.launch {
                                        db.sessionDao().clearSession()
                                        smartSolarApp.sessionManager.clearSession()
                                    }
                                    loggedInFullName = ""
                                    loggedInNic = ""
                                    currentScreen = Screen.Login
                                },
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
