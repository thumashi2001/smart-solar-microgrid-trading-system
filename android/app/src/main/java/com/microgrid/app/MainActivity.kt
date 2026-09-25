package com.microgrid.app

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
import com.microgrid.app.ui.screens.*
import com.microgrid.app.ui.theme.MicrogridAppTheme

// ──────────────────────────────────────────────────────────────────────
// Navigation graph
// ──────────────────────────────────────────────────────────────────────
sealed class Screen {
    // Auth screens (Component 1)
    object Login : Screen()
    object Register : Screen()

    // Profile hub
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

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            MicrogridAppTheme {
                Surface(modifier = Modifier.fillMaxSize()) {
                    var currentScreen by remember { mutableStateOf<Screen>(Screen.Login) }

                    // Logged-in user state — kept alive across screens
                    var loggedInFullName by remember { mutableStateOf("") }
                    var loggedInNic by remember { mutableStateOf("") }

                    when (val screen = currentScreen) {

                        // ── Auth ─────────────────────────────────────────────────────
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

                        // ── Profile hub ──────────────────────────────────────────────
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
}