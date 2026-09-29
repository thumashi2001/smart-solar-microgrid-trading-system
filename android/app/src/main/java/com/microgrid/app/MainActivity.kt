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
import com.microgrid.app.data.MicrogridNode
import com.microgrid.app.data.Reservation
import com.microgrid.app.data.Slot
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

// ──────────────────────────────────────────────────────────────────────
// Navigation graph
// ──────────────────────────────────────────────────────────────────────
sealed class Screen {
    // Auth screens (Component 1)
    object Login : Screen()
    object Register : Screen()

    // Profile hub
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

    // Component 2 — booking flow (4 steps)
    object StationSelection : Screen()
    data class DateSelection(val station: MicrogridNode) : Screen()
    data class SlotSelection(val station: MicrogridNode, val date: String) : Screen()
    data class ReservationSummary(val station: MicrogridNode, val slot: Slot) : Screen()

    // Component 2 — My Bookings + actions
    object MyBookings : Screen()
    data class UpdateReservation(val reservation: Reservation) : Screen()
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
                    // Suwani: honor LoginActivity handoff extras; otherwise restore session.
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

                    when (val screen = currentScreen) {

                        // ── Auth ─────────────────────────────────────────────────────
                        is Screen.Login -> {
                            LoginScreen(
                                onLoginSuccess = { role, fullName, token, nic ->
                                    loggedInFullName = fullName
                                    loggedInNic = nic
                                    scope.launch {
                                        if (role == "Prosumer" || role == "GridOperator") {
                                            // Save the session first, then navigate
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
                                        } else {
                                            db.sessionDao().clearSession()
                                            smartSolarApp.sessionManager.clearSession()
                                        }

                                        when (role) {
                                            "GridOperator" -> {
                                                startActivity(Intent(this@MainActivity, OperatorHomeActivity::class.java))
                                                finish()
                                            }
                                            "Prosumer" -> currentScreen = Screen.Dashboard
                                            else -> {
                                                loggedInFullName = ""
                                                loggedInNic = ""
                                                currentScreen = Screen.Login
                                            }
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
                        // ── Profile hub ──────────────────────────────────────────────
                        is Screen.Dashboard -> {
                            ProsumerDashboardScreen(
                                fullName = loggedInFullName,
                                nic = loggedInNic,
                                onProfileClick = { currentScreen = Screen.Profile },
                                onBookingsClick = { currentScreen = Screen.Bookings },
                                onHistoryClick = { currentScreen = Screen.BookingHistory },
                                onSearchClick = { currentScreen = Screen.SearchBookings },
                                onFindEnergyClick = { currentScreen = Screen.StationSelection },
                                onNearbyStationsClick = { startActivity(Intent(this@MainActivity, StationsMapActivity::class.java)) }
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
                                onMyBookings = { currentScreen = Screen.MyBookings },
                                onBookSlot = { currentScreen = Screen.StationSelection },
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
                                    currentScreen = Screen.Login
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

                        // ── Component 2: Booking flow — 4 steps ─────────────────────
                        is Screen.StationSelection -> {
                            StationSelectionScreen(
                                onBack = { currentScreen = Screen.Profile },
                                onStationSelected = { station ->
                                    currentScreen = Screen.DateSelection(station)
                                }
                            )
                        }
                        is Screen.DateSelection -> {
                            DateSelectionScreen(
                                station = screen.station,
                                onBack = { currentScreen = Screen.StationSelection },
                                onDateSelected = { date ->
                                    currentScreen = Screen.SlotSelection(screen.station, date)
                                }
                            )
                        }
                        is Screen.SlotSelection -> {
                            SlotSelectionScreenContent(
                                station = screen.station,
                                selectedDate = screen.date,
                                prosumerNic = loggedInNic,
                                onBack = { currentScreen = Screen.DateSelection(screen.station) },
                                onSlotSelected = { slot ->
                                    currentScreen = Screen.ReservationSummary(screen.station, slot)
                                }
                            )
                        }
                        is Screen.ReservationSummary -> {
                            ReservationSummaryScreen(
                                station = screen.station,
                                slot = screen.slot,
                                prosumerNic = loggedInNic,
                                onBack = {
                                    currentScreen = Screen.SlotSelection(screen.station, screen.slot.date)
                                },
                                onBooked = { _ ->
                                    currentScreen = Screen.MyBookings
                                }
                            )
                        }

                        // ── Component 2: My Bookings ─────────────────────────────────
                        is Screen.MyBookings -> {
                            MyBookingsScreen(
                                prosumerNic = loggedInNic,
                                onBack = { currentScreen = Screen.Profile },
                                onBookNew = { currentScreen = Screen.StationSelection },
                                onUpdateReservation = { reservation ->
                                    currentScreen = Screen.UpdateReservation(reservation)
                                }
                            )
                        }

                        // ── Component 2: Update reservation (change slot) ─────────────
                        is Screen.UpdateReservation -> {
                            UpdateReservationFlow(
                                reservation = screen.reservation,
                                onBack = { currentScreen = Screen.MyBookings },
                                onUpdateComplete = { currentScreen = Screen.MyBookings }
                            )
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
