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
import androidx.compose.material.icons.filled.Edit
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
import kotlinx.coroutines.launch

/**
 * MyBookingsScreen — Component 2
 *
 * Loads the logged-in prosumer's full reservation history from:
 *   GET /api/reservations/history/{prosumerNic}
 *
 * Tabs:
 *   Upcoming  = Pending + Approved
 *   Pending   = Pending only
 *   Approved  = Approved only
 *   History   = Cancelled + Completed
 *
 * Actions:
 *   Cancel    → DELETE /api/reservations/{id}   (Pending and Approved only)
 *   Update    → navigates to SlotSelectionScreen to pick a new slot,
 *               then PUT /api/reservations/{id}
 *
 * The 12-hour rule is enforced by the API — the client does NOT implement it.
 *
 * @param prosumerNic  NIC of the logged-in prosumer.
 * @param onBack       Navigate back to ProfileScreen.
 * @param onBookNew    Navigate to StationSelectionScreen for a new booking.
 * @param onUpdateReservation Navigate to update flow (passes reservation to update).
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MyBookingsScreen(
    prosumerNic: String,
    onBack: () -> Unit,
    onBookNew: () -> Unit,
    onUpdateReservation: (Reservation) -> Unit
) {
    val scope = rememberCoroutineScope()
    var reservations by remember { mutableStateOf<List<Reservation>>(emptyList()) }
    var isLoading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf("") }
    var activeTab by remember { mutableStateOf("Upcoming") }
    var cancellingId by remember { mutableStateOf<String?>(null) }
    var confirmCancelTarget by remember { mutableStateOf<Reservation?>(null) }
    var operationResult by remember { mutableStateOf("") }

    val tabs = listOf("Upcoming", "Pending", "Approved", "History")

    fun loadReservations() {
        scope.launch {
            isLoading = true
            error = ""
            try {
                // Use the dedicated history endpoint — GET /api/reservations has no NIC filter
                val response = RetrofitClient.instance.getReservationHistory(prosumerNic)
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

    val displayed = when (activeTab) {
        "Upcoming" -> reservations.filter { it.status == "Pending" || it.status == "Approved" }
        "Pending" -> reservations.filter { it.status == "Pending" }
        "Approved" -> reservations.filter { it.status == "Approved" }
        "History" -> reservations.filter { it.status == "Cancelled" || it.status == "Completed" }
        else -> reservations
    }

    // Tab counts
    val tabCounts = mapOf(
        "Upcoming" to reservations.count { it.status == "Pending" || it.status == "Approved" },
        "Pending" to reservations.count { it.status == "Pending" },
        "Approved" to reservations.count { it.status == "Approved" },
        "History" to reservations.count { it.status == "Cancelled" || it.status == "Completed" }
    )

    // ── Cancel confirmation dialog ─────────────────────────────────────
    if (confirmCancelTarget != null) {
        val target = confirmCancelTarget!!
        AlertDialog(
            onDismissRequest = { confirmCancelTarget = null },
            title = { Text("Cancel Reservation?", fontWeight = FontWeight.Bold) },
            text = {
                Text(
                    "Cancel reservation ${target.reservationId}?\n\n" +
                    "This cannot be undone. The API will reject cancellations within 12 hours of the slot start time."
                )
            },
            confirmButton = {
                TextButton(
                    onClick = {
                        val resId = target.id
                        confirmCancelTarget = null
                        scope.launch {
                            cancellingId = resId
                            error = ""
                            try {
                                // Correct: use DELETE /api/reservations/{id}
                                val r = RetrofitClient.instance.cancelReservation(resId)
                                if (r.isSuccessful) {
                                    operationResult = "Reservation ${target.reservationId} cancelled."
                                    loadReservations()
                                } else {
                                    val bodyStr = r.errorBody()?.string() ?: ""
                                    error = when (r.code()) {
                                        400 -> extractMsg(bodyStr, "Cannot cancel: check 12-hour notice rule or reservation status.")
                                        404 -> "Reservation not found."
                                        409 -> extractMsg(bodyStr, "Reservation was modified concurrently. Please refresh.")
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
                    IconButton(onClick = onBack) { Icon(Icons.Filled.ArrowBack, contentDescription = "Back") }
                },
                actions = {
                    TextButton(onClick = onBookNew) {
                        Text("+ New", color = Color.White, fontWeight = FontWeight.Bold)
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
        Column(modifier = Modifier.fillMaxSize().padding(padding)) {

            // ── Status tabs ──────────────────────────────────────────
            LazyRow(
                modifier = Modifier.fillMaxWidth().background(Color.White).padding(horizontal = 12.dp, vertical = 8.dp),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                items(tabs) { tab ->
                    FilterChip(
                        selected = tab == activeTab,
                        onClick = { activeTab = tab },
                        label = {
                            Text(
                                "$tab (${tabCounts[tab] ?: 0})",
                                fontSize = 13.sp,
                                fontWeight = if (tab == activeTab) FontWeight.Bold else FontWeight.Normal
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

            // ── Success banner ────────────────────────────────────────
            if (operationResult.isNotBlank()) {
                Card(
                    colors = CardDefaults.cardColors(containerColor = Color(0xFFE6F4EA)),
                    shape = RoundedCornerShape(0.dp), modifier = Modifier.fillMaxWidth()
                ) {
                    Row(modifier = Modifier.padding(12.dp), horizontalArrangement = Arrangement.SpaceBetween) {
                        Text("✓ $operationResult", fontSize = 13.sp, color = Color(0xFF137333), modifier = Modifier.weight(1f))
                        TextButton(onClick = { operationResult = "" }) { Text("✕", color = Color(0xFF137333)) }
                    }
                }
            }

            // ── Error banner ──────────────────────────────────────────
            if (error.isNotBlank()) {
                Card(
                    colors = CardDefaults.cardColors(containerColor = Color(0xFFFCE8E6)),
                    shape = RoundedCornerShape(0.dp), modifier = Modifier.fillMaxWidth()
                ) {
                    Row(modifier = Modifier.padding(12.dp), horizontalArrangement = Arrangement.SpaceBetween) {
                        Text("⚠ $error", fontSize = 13.sp, color = Color(0xFFC5221F), modifier = Modifier.weight(1f))
                        TextButton(onClick = { error = "" }) { Text("✕", color = Color(0xFFC5221F)) }
                    }
                }
            }

            // ── Content ───────────────────────────────────────────────
            when {
                isLoading -> {
                    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            CircularProgressIndicator(color = AccentGreen)
                            Spacer(Modifier.height(12.dp))
                            Text("Loading bookings…", color = Color.Gray, fontSize = 13.sp)
                        }
                    }
                }
                displayed.isEmpty() -> {
                    Box(Modifier.fillMaxSize().padding(32.dp), contentAlignment = Alignment.Center) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            Text(if (activeTab == "History") "📜" else "📋", fontSize = 40.sp)
                            Spacer(Modifier.height(12.dp))
                            Text(
                                when (activeTab) {
                                    "History" -> "No booking history"
                                    else -> "No $activeTab bookings"
                                },
                                fontWeight = FontWeight.SemiBold, color = DarkGreen, fontSize = 16.sp
                            )
                            Text(
                                when (activeTab) {
                                    "History" -> "Completed and cancelled bookings will appear here."
                                    "Upcoming" -> "No active upcoming bookings. Book a new slot!"
                                    else -> "No reservations in this status."
                                },
                                fontSize = 13.sp, color = Color.Gray
                            )
                            if (activeTab != "History") {
                                Spacer(Modifier.height(20.dp))
                                Button(
                                    onClick = onBookNew,
                                    colors = ButtonDefaults.buttonColors(containerColor = AccentGreen),
                                    shape = RoundedCornerShape(10.dp)
                                ) { Text("Book a Slot") }
                            }
                        }
                    }
                }
                else -> {
                    LazyColumn(
                        contentPadding = PaddingValues(16.dp),
                        verticalArrangement = Arrangement.spacedBy(12.dp)
                    ) {
                        items(displayed, key = { it.id }) { res ->
                            BookingCard(
                                reservation = res,
                                isCancelling = cancellingId == res.id,
                                onCancel = { confirmCancelTarget = res },
                                onUpdate = { onUpdateReservation(res) }
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
    onCancel: () -> Unit,
    onUpdate: () -> Unit
) {
    val statusColor = when (reservation.status) {
        "Approved" -> Color(0xFF137333)
        "Cancelled" -> Color(0xFFC5221F)
        "Completed" -> Color(0xFF1A73E8)
        else -> Color(0xFFB06000)   // Pending
    }
    val statusBg = when (reservation.status) {
        "Approved" -> Color(0xFFE6F4EA)
        "Cancelled" -> Color(0xFFFCE8E6)
        "Completed" -> Color(0xFFE8F0FE)
        else -> Color(0xFFFEF7E0)
    }
    val isModifiable = reservation.status == "Pending" || reservation.status == "Approved"

    Card(
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        elevation = CardDefaults.cardElevation(defaultElevation = 3.dp),
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            // Header
            Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.CenterVertically) {
                Surface(color = Color(0xFFEBE8E1), shape = RoundedCornerShape(6.dp)) {
                    Text(reservation.reservationId, modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp), fontSize = 11.sp, fontWeight = FontWeight.Bold, color = DarkGreen)
                }
                Surface(color = statusBg, shape = RoundedCornerShape(12.dp)) {
                    Text(reservation.status, modifier = Modifier.padding(horizontal = 10.dp, vertical = 4.dp), fontSize = 11.sp, fontWeight = FontWeight.Bold, color = statusColor)
                }
            }

            Spacer(Modifier.height(10.dp))

            // Station / Slot
            Text("Station: ${reservation.stationId}", fontSize = 14.sp, fontWeight = FontWeight.SemiBold, color = DarkGreen)
            Text("Slot: ${reservation.slotId}", fontSize = 12.sp, color = Color.Gray)

            Spacer(Modifier.height(8.dp))

            // Date / Time (from reservation fields if available, else empty)
            Row(horizontalArrangement = Arrangement.spacedBy(16.dp)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Icon(Icons.Filled.BoltSharp, null, tint = AccentGreen, modifier = Modifier.size(14.dp))
                    Spacer(Modifier.width(4.dp))
                    Text("NIC: ${reservation.prosumerNic}", fontSize = 12.sp, color = Color.Gray)
                }
            }

            if (reservation.transactionReference.isNotBlank()) {
                Spacer(Modifier.height(4.dp))
                Text("Ref: ${reservation.transactionReference}", fontSize = 11.sp, color = Color.Gray)
            }

            Spacer(Modifier.height(4.dp))
            Text("Created: ${reservation.createdAt.take(10)}", fontSize = 11.sp, color = Color.Gray)
            if (reservation.updatedAt != reservation.createdAt) {
                Text("Updated: ${reservation.updatedAt.take(10)}", fontSize = 11.sp, color = Color.Gray)
            }

            // Action buttons — only for modifiable reservations
            if (isModifiable) {
                Spacer(Modifier.height(12.dp))
                Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    // Update: change slot
                    OutlinedButton(
                        onClick = onUpdate,
                        modifier = Modifier.weight(1f),
                        shape = RoundedCornerShape(8.dp),
                        colors = ButtonDefaults.outlinedButtonColors(contentColor = AccentGreen)
                    ) {
                        Icon(Icons.Filled.Edit, null, modifier = Modifier.size(14.dp))
                        Spacer(Modifier.width(4.dp))
                        Text("Change Slot", fontSize = 12.sp, fontWeight = FontWeight.Bold)
                    }
                    // Cancel
                    OutlinedButton(
                        onClick = onCancel,
                        enabled = !isCancelling,
                        modifier = Modifier.weight(1f),
                        shape = RoundedCornerShape(8.dp),
                        colors = ButtonDefaults.outlinedButtonColors(contentColor = Color(0xFFC5221F))
                    ) {
                        if (isCancelling) {
                            CircularProgressIndicator(modifier = Modifier.size(14.dp), color = Color(0xFFC5221F), strokeWidth = 2.dp)
                            Spacer(Modifier.width(4.dp))
                        }
                        Text(if (isCancelling) "Cancelling…" else "Cancel", fontSize = 12.sp, fontWeight = FontWeight.Bold)
                    }
                }
            }
        }
    }
}

private fun extractMsg(body: String, fallback: String): String {
    val match = Regex("\"message\"\\s*:\\s*\"([^\"]+)\"").find(body)
    return match?.groupValues?.get(1) ?: fallback
}
