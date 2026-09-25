package com.microgrid.app.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.BoltSharp
import androidx.compose.material.icons.filled.CalendarToday
import androidx.compose.material.icons.filled.Schedule
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.microgrid.app.data.Reservation
import com.microgrid.app.data.RetrofitClient
import com.microgrid.app.data.UpdateReservationRequest
import kotlinx.coroutines.launch

/**
 * MyBookingsScreen — Component 2
 *
 * Lists all of the logged-in prosumer's reservations, grouped by status tab
 * (All / Pending / Approved / Cancelled). Supports cancel action for Pending
 * reservations. Navigates back to ProfileScreen.
 *
 * @param prosumerNic  NIC of the logged-in prosumer.
 * @param onBack       Navigate back (to ProfileScreen or wherever is appropriate).
 * @param onBookNew    Navigate to SlotSelectionScreen to make a new booking.
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MyBookingsScreen(
    prosumerNic: String,
    onBack: () -> Unit,
    onBookNew: () -> Unit
) {
    val scope = rememberCoroutineScope()
    var reservations by remember { mutableStateOf<List<Reservation>>(emptyList()) }
    var isLoading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf("") }
    var activeTab by remember { mutableStateOf("All") }
    var cancellingId by remember { mutableStateOf<String?>(null) }
    var confirmCancelTarget by remember { mutableStateOf<Reservation?>(null) }

    val tabs = listOf("All", "Pending", "Approved", "Cancelled")

    // Load this prosumer's reservations
    fun loadReservations() {
        scope.launch {
            isLoading = true
            error = ""
            try {
                val response = RetrofitClient.instance.getReservations(prosumerNic = prosumerNic)
                reservations = if (response.isSuccessful) response.body() ?: emptyList() else emptyList()
                if (!response.isSuccessful) error = "Failed to load bookings (HTTP ${response.code()})"
            } catch (ex: Exception) {
                error = "Network error: ${ex.message}"
            } finally {
                isLoading = false
            }
        }
    }

    LaunchedEffect(prosumerNic) { loadReservations() }

    val displayed = if (activeTab == "All") reservations
    else reservations.filter { it.status.equals(activeTab, ignoreCase = true) }

    // ── Cancel confirmation dialog ────────────────────────────────────────
    if (confirmCancelTarget != null) {
        val target = confirmCancelTarget!!
        AlertDialog(
            onDismissRequest = { confirmCancelTarget = null },
            title = { Text("Cancel Reservation?", fontWeight = FontWeight.Bold) },
            text = { Text("This will cancel reservation ${target.reservationId}. This cannot be undone.") },
            confirmButton = {
                TextButton(
                    onClick = {
                        confirmCancelTarget = null
                        val resMongoId = target.id.ifBlank { target.reservationId }
                        scope.launch {
                            cancellingId = resMongoId
                            try {
                                val r = RetrofitClient.instance.updateReservation(
                                    id = resMongoId,
                                    request = UpdateReservationRequest(status = "Cancelled")
                                )
                                if (r.isSuccessful) {
                                    loadReservations()
                                } else {
                                    error = when (r.code()) {
                                        409 -> "Cannot cancel: reservation is already in a terminal state."
                                        400 -> "Cancellation failed: less than 12 hours before the slot."
                                        else -> "Cancel failed (HTTP ${r.code()})"
                                    }
                                }
                            } catch (ex: Exception) {
                                error = "Network error: ${ex.message}"
                            } finally {
                                cancellingId = null
                            }
                        }
                    }
                ) { Text("Yes, Cancel", color = Color(0xFFC5221F), fontWeight = FontWeight.Bold) }
            },
            dismissButton = {
                TextButton(onClick = { confirmCancelTarget = null }) { Text("Keep Booking") }
            }
        )
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("My Bookings", fontWeight = FontWeight.Bold) },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.Filled.ArrowBack, contentDescription = "Back")
                    }
                },
                actions = {
                    TextButton(onClick = onBookNew) {
                        Text("+ Book New", color = Color.White, fontWeight = FontWeight.Bold)
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = DarkGreen,
                    titleContentColor = Color.White,
                    navigationIconContentColor = Color.White
                )
            )
        },
        containerColor = PageBg
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            // ── Status tabs ──────────────────────────────────────────────
            LazyRow(
                modifier = Modifier
                    .fillMaxWidth()
                    .background(Color.White)
                    .padding(horizontal = 12.dp, vertical = 8.dp),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                items(tabs) { tab ->
                    val count = if (tab == "All") reservations.size
                    else reservations.count { it.status.equals(tab, ignoreCase = true) }
                    val selected = tab == activeTab
                    FilterChip(
                        selected = selected,
                        onClick = { activeTab = tab },
                        label = {
                            Text(
                                "$tab ($count)",
                                fontSize = 13.sp,
                                fontWeight = if (selected) FontWeight.Bold else FontWeight.Normal
                            )
                        },
                        colors = FilterChipDefaults.filterChipColors(
                            selectedContainerColor = DarkGreen,
                            selectedLabelColor = Color.White,
                            containerColor = Color(0xFFF0ECE1)
                        )
                    )
                }
            }

            // ── Error banner ─────────────────────────────────────────────
            if (error.isNotBlank()) {
                Card(
                    colors = CardDefaults.cardColors(containerColor = Color(0xFFFCE8E6)),
                    shape = RoundedCornerShape(0.dp),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Row(
                        modifier = Modifier.padding(12.dp),
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Text("⚠ $error", fontSize = 13.sp, color = Color(0xFFC5221F), modifier = Modifier.weight(1f))
                        TextButton(onClick = { error = "" }) { Text("✕", color = Color(0xFFC5221F)) }
                    }
                }
            }

            // ── Content ──────────────────────────────────────────────────
            when {
                isLoading -> {
                    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            CircularProgressIndicator(color = AccentGreen)
                            Spacer(Modifier.height(12.dp))
                            Text("Loading your bookings…", color = Color.Gray, fontSize = 13.sp)
                        }
                    }
                }
                displayed.isEmpty() -> {
                    Box(
                        modifier = Modifier
                            .fillMaxSize()
                            .padding(32.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            Text("📋", fontSize = 40.sp)
                            Spacer(Modifier.height(12.dp))
                            Text(
                                if (activeTab == "All") "No bookings yet" else "No $activeTab bookings",
                                fontWeight = FontWeight.SemiBold,
                                color = DarkGreen,
                                fontSize = 16.sp
                            )
                            Text("Your energy reservations will appear here.", fontSize = 13.sp, color = Color.Gray)
                            Spacer(Modifier.height(20.dp))
                            Button(
                                onClick = onBookNew,
                                colors = ButtonDefaults.buttonColors(containerColor = AccentGreen),
                                shape = RoundedCornerShape(10.dp)
                            ) { Text("Book a Slot Now") }
                        }
                    }
                }
                else -> {
                    LazyColumn(
                        contentPadding = PaddingValues(16.dp),
                        verticalArrangement = Arrangement.spacedBy(12.dp)
                    ) {
                        items(displayed, key = { it.id.ifBlank { it.reservationId } }) { res ->
                            BookingCard(
                                reservation = res,
                                isCancelling = cancellingId == res.id || cancellingId == res.reservationId,
                                onCancel = { confirmCancelTarget = res }
                            )
                        }
                        item { Spacer(Modifier.height(8.dp)) }
                    }
                }
            }
        }
    }
}

@Composable
private fun BookingCard(
    reservation: Reservation,
    isCancelling: Boolean,
    onCancel: () -> Unit
) {
    val statusColor = when (reservation.status) {
        "Approved" -> Color(0xFF137333)
        "Cancelled" -> Color(0xFFC5221F)
        else -> Color(0xFFB06000)   // Pending
    }
    val statusBg = when (reservation.status) {
        "Approved" -> Color(0xFFE6F4EA)
        "Cancelled" -> Color(0xFFFCE8E6)
        else -> Color(0xFFFEF7E0)
    }
    val isPending = reservation.status == "Pending"

    Card(
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        elevation = CardDefaults.cardElevation(defaultElevation = 3.dp),
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            // Header
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Surface(color = Color(0xFFEBE8E1), shape = RoundedCornerShape(6.dp)) {
                    Text(
                        reservation.reservationId,
                        modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp),
                        fontSize = 11.sp,
                        fontWeight = FontWeight.Bold,
                        color = DarkGreen
                    )
                }
                Surface(color = statusBg, shape = RoundedCornerShape(12.dp)) {
                    Text(
                        reservation.status,
                        modifier = Modifier.padding(horizontal = 10.dp, vertical = 4.dp),
                        fontSize = 11.sp,
                        fontWeight = FontWeight.Bold,
                        color = statusColor
                    )
                }
            }

            Spacer(Modifier.height(10.dp))

            // Station / Slot
            Text(
                "Station: ${reservation.stationId}",
                fontSize = 14.sp,
                fontWeight = FontWeight.SemiBold,
                color = DarkGreen
            )
            Text(
                "Slot: ${reservation.slotId}",
                fontSize = 12.sp,
                color = Color.Gray
            )

            Spacer(Modifier.height(8.dp))

            // Date / Time
            Row(horizontalArrangement = Arrangement.spacedBy(16.dp)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Icon(Icons.Filled.CalendarToday, null, tint = Color.Gray, modifier = Modifier.size(14.dp))
                    Spacer(Modifier.width(4.dp))
                    Text(reservation.date, fontSize = 13.sp, color = Color(0xFF444444))
                }
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Icon(Icons.Filled.Schedule, null, tint = Color.Gray, modifier = Modifier.size(14.dp))
                    Spacer(Modifier.width(4.dp))
                    Text("${reservation.startTime} – ${reservation.endTime}", fontSize = 13.sp, color = Color(0xFF444444))
                }
            }

            Spacer(Modifier.height(8.dp))

            // Energy
            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(Icons.Filled.BoltSharp, null, tint = AccentGreen, modifier = Modifier.size(14.dp))
                Spacer(Modifier.width(4.dp))
                Text(
                    "${reservation.energyAmount} kWh",
                    fontSize = 13.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = DarkGreen
                )
            }

            // Notes
            if (reservation.notes.isNotBlank()) {
                Spacer(Modifier.height(6.dp))
                Text(
                    "📝 ${reservation.notes}",
                    fontSize = 12.sp,
                    color = Color.Gray,
                    lineHeight = 16.sp
                )
            }

            // Cancel button (Pending only)
            if (isPending) {
                Spacer(Modifier.height(12.dp))
                OutlinedButton(
                    onClick = onCancel,
                    enabled = !isCancelling,
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(8.dp),
                    colors = ButtonDefaults.outlinedButtonColors(contentColor = Color(0xFFC5221F))
                ) {
                    if (isCancelling) {
                        CircularProgressIndicator(modifier = Modifier.size(16.dp), color = Color(0xFFC5221F), strokeWidth = 2.dp)
                        Spacer(Modifier.width(8.dp))
                    }
                    Text(if (isCancelling) "Cancelling…" else "Cancel Booking", fontWeight = FontWeight.Bold)
                }
            }
        }
    }
}
