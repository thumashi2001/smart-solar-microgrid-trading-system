package com.microgrid.app.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.BoltSharp
import androidx.compose.material.icons.filled.CalendarToday
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.LocationOn
import androidx.compose.material.icons.filled.Schedule
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.microgrid.app.data.CreateReservationRequest
import com.microgrid.app.data.MicrogridNode
import com.microgrid.app.data.Reservation
import com.microgrid.app.data.RetrofitClient
import com.microgrid.app.data.Slot
import kotlinx.coroutines.launch

/**
 * ReservationSummaryScreen — Component 2, Step 4 of 4
 *
 * Displays station + slot details and asks for final confirmation before
 * calling POST /api/reservations.
 *
 * Backend DTO contract (CreateReservationRequest.cs):
 *   - prosumerNic   ← required
 *   - stationId     ← required
 *   - slotId        ← required (backend uses SlotId field, not MongoDB _id)
 *
 * Note: energyAmount and notes are NOT part of the backend contract.
 *       Do not send them in the request body.
 *
 * @param station       The chosen station (Step 1).
 * @param slot          The chosen slot (Step 3).
 * @param prosumerNic   NIC of the logged-in prosumer.
 * @param onBack        Navigate back to SlotSelectionScreen.
 * @param onBooked      Navigate to MyBookingsScreen after successful booking.
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ReservationSummaryScreen(
    station: MicrogridNode,
    slot: Slot,
    prosumerNic: String,
    onBack: () -> Unit,
    onBooked: (Reservation) -> Unit
) {
    val scope = rememberCoroutineScope()
    var isSubmitting by remember { mutableStateOf(false) }
    var error by remember { mutableStateOf("") }
    var createdReservation by remember { mutableStateOf<Reservation?>(null) }

    // ── Success state ────────────────────────────────────────────────
    if (createdReservation != null) {
        val res = createdReservation!!
        Column(
            modifier = Modifier
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(24.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Spacer(Modifier.height(32.dp))
            Icon(Icons.Filled.CheckCircle, null, tint = AccentGreen, modifier = Modifier.size(80.dp))
            Spacer(Modifier.height(16.dp))
            Text("Booking Confirmed!", fontSize = 24.sp, fontWeight = FontWeight.Bold, color = DarkGreen)
            Spacer(Modifier.height(8.dp))
            Text(
                "Your reservation has been submitted and is now Pending admin approval.",
                fontSize = 14.sp, color = Color.Gray,
                lineHeight = 20.sp
            )
            Spacer(Modifier.height(24.dp))

            // Summary card
            Card(
                colors = CardDefaults.cardColors(containerColor = Color.White),
                shape = RoundedCornerShape(14.dp),
                elevation = CardDefaults.cardElevation(4.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(modifier = Modifier.padding(18.dp)) {
                    Text("Booking Summary", fontWeight = FontWeight.Bold, fontSize = 15.sp, color = DarkGreen)
                    Spacer(Modifier.height(12.dp))
                    SummaryRow("Reservation ID", res.reservationId)
                    SummaryRow("Station", station.nodeName.ifBlank { station.nodeId })
                    SummaryRow("Slot", slot.slotId)
                    SummaryRow("Date", slot.date)
                    SummaryRow("Time", "${slot.startTime} – ${slot.endTime}")
                    SummaryRow("Status", res.status)
                }
            }

            Spacer(Modifier.height(28.dp))

            Button(
                onClick = { onBooked(res) },
                modifier = Modifier.fillMaxWidth().height(52.dp),
                shape = RoundedCornerShape(12.dp),
                colors = ButtonDefaults.buttonColors(containerColor = AccentGreen)
            ) {
                Text("View My Bookings", fontWeight = FontWeight.Bold, fontSize = 16.sp)
            }
        }
        return
    }

    // ── Main content ─────────────────────────────────────────────────
    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text("Book Energy", fontWeight = FontWeight.Bold, fontSize = 16.sp)
                        Text("Step 4 of 4 — Confirm", fontSize = 11.sp, color = Color.White.copy(alpha = 0.7f))
                    }
                },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.Filled.ArrowBack, contentDescription = "Back")
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
                .verticalScroll(rememberScrollState())
                .padding(16.dp)
        ) {
            BookingProgressBar(currentStep = 4)

            Spacer(Modifier.height(16.dp))

            // ── Station card ─────────────────────────────────────────
            Card(
                shape = RoundedCornerShape(14.dp),
                colors = CardDefaults.cardColors(containerColor = Color.White),
                elevation = CardDefaults.cardElevation(4.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(modifier = Modifier.padding(18.dp)) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Icon(Icons.Filled.LocationOn, null, tint = AccentGreen)
                        Spacer(Modifier.width(8.dp))
                        Text("Station", fontWeight = FontWeight.Bold, fontSize = 16.sp, color = DarkGreen)
                    }
                    Spacer(Modifier.height(10.dp))
                    DetailRow(Icons.Filled.LocationOn, "Name", station.nodeName.ifBlank { station.nodeId })
                    DetailRow(Icons.Filled.LocationOn, "Station ID", station.nodeId)
                    if (station.location.isNotBlank()) {
                        DetailRow(Icons.Filled.LocationOn, "Location", station.location)
                    }
                }
            }

            Spacer(Modifier.height(14.dp))

            // ── Slot card ────────────────────────────────────────────
            Card(
                shape = RoundedCornerShape(14.dp),
                colors = CardDefaults.cardColors(containerColor = Color.White),
                elevation = CardDefaults.cardElevation(4.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(modifier = Modifier.padding(18.dp)) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Icon(Icons.Filled.BoltSharp, null, tint = AccentGreen)
                        Spacer(Modifier.width(8.dp))
                        Text("Slot Details", fontWeight = FontWeight.Bold, fontSize = 16.sp, color = DarkGreen)
                    }
                    Spacer(Modifier.height(10.dp))
                    DetailRow(Icons.Filled.BoltSharp, "Slot ID", slot.slotId)
                    DetailRow(Icons.Filled.CalendarToday, "Date", slot.date)
                    DetailRow(Icons.Filled.Schedule, "Time", "${slot.startTime} – ${slot.endTime}")
                    Spacer(Modifier.height(8.dp))
                    HorizontalDivider()
                    Spacer(Modifier.height(8.dp))
                    Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceEvenly) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            Text("${slot.availability}", fontSize = 20.sp, fontWeight = FontWeight.Bold, color = AccentGreen)
                            Text("Spaces Left", fontSize = 11.sp, color = Color.Gray)
                        }
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            Text("${slot.capacity}", fontSize = 20.sp, fontWeight = FontWeight.Bold, color = DarkGreen)
                            Text("Capacity", fontSize = 11.sp, color = Color.Gray)
                        }
                    }
                }
            }

            Spacer(Modifier.height(14.dp))

            // ── Policy notice ────────────────────────────────────────
            Card(
                colors = CardDefaults.cardColors(containerColor = Color(0xFFFEF7E0)),
                shape = RoundedCornerShape(10.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Text(
                    "📋 Booking Policy\n• Status begins as Pending until admin approves.\n• Modifications require ≥12 hours notice before slot start.\n• Cancellations require ≥12 hours notice before slot start.\n• Slots can only be booked within 7 days from today.",
                    modifier = Modifier.padding(14.dp),
                    fontSize = 12.sp,
                    color = Color(0xFF7A5700),
                    lineHeight = 18.sp
                )
            }

            // ── Error ────────────────────────────────────────────────
            if (error.isNotBlank()) {
                Spacer(Modifier.height(12.dp))
                Card(
                    colors = CardDefaults.cardColors(containerColor = Color(0xFFFCE8E6)),
                    shape = RoundedCornerShape(8.dp),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text(
                        "⚠ $error",
                        modifier = Modifier.padding(14.dp),
                        fontSize = 13.sp,
                        color = Color(0xFFC5221F),
                        fontWeight = FontWeight.Medium
                    )
                }
            }

            Spacer(Modifier.height(24.dp))

            // ── Confirm button ───────────────────────────────────────
            Button(
                onClick = {
                    error = ""
                    scope.launch {
                        isSubmitting = true
                        try {
                            // Use SlotId (the business key), not the MongoDB _id
                            val slotIdToSend = slot.slotId.ifBlank { slot.id }
                            val response = RetrofitClient.instance.createReservation(
                                CreateReservationRequest(
                                    prosumerNic = prosumerNic,
                                    stationId = station.nodeId,
                                    slotId = slotIdToSend
                                )
                            )
                            if (response.isSuccessful && response.body() != null) {
                                createdReservation = response.body()
                            } else {
                                val bodyStr = response.errorBody()?.string() ?: ""
                                error = when (response.code()) {
                                    400 -> extractMessage(bodyStr, "Invalid booking request. Check station/slot validity.")
                                    404 -> extractMessage(bodyStr, "Station, slot, or prosumer not found.")
                                    409 -> "Slot is fully booked. Please choose another slot."
                                    503 -> "Service temporarily unavailable. Please try again shortly."
                                    else -> "Booking failed (HTTP ${response.code()}). Please try again."
                                }
                            }
                        } catch (ex: Exception) {
                            error = "Network error: ${ex.message}"
                        } finally {
                            isSubmitting = false
                        }
                    }
                },
                enabled = !isSubmitting,
                modifier = Modifier.fillMaxWidth().height(52.dp),
                shape = RoundedCornerShape(12.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = DarkGreen,
                    disabledContainerColor = Color(0xFF8CB8A8)
                )
            ) {
                if (isSubmitting) {
                    CircularProgressIndicator(modifier = Modifier.size(20.dp), color = Color.White, strokeWidth = 2.dp)
                    Spacer(Modifier.width(10.dp))
                }
                Text(
                    if (isSubmitting) "Submitting…" else "Confirm Booking",
                    fontWeight = FontWeight.Bold, fontSize = 16.sp
                )
            }

            Spacer(Modifier.height(16.dp))
        }
    }
}

private fun extractMessage(body: String, fallback: String): String {
    // Try to parse {"message":"..."} from error body
    val match = Regex("\"message\"\\s*:\\s*\"([^\"]+)\"").find(body)
    return match?.groupValues?.get(1) ?: fallback
}

@Composable
private fun DetailRow(icon: ImageVector, label: String, value: String) {
    Row(
        modifier = Modifier.fillMaxWidth().padding(vertical = 4.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(icon, null, tint = Color.Gray, modifier = Modifier.size(15.dp))
        Spacer(Modifier.width(8.dp))
        Text("$label: ", fontSize = 13.sp, color = Color.Gray, fontWeight = FontWeight.Medium)
        Text(value, fontSize = 13.sp, fontWeight = FontWeight.SemiBold, color = Color(0xFF1C1F1E))
    }
}

@Composable
private fun SummaryRow(label: String, value: String) {
    Row(
        modifier = Modifier.fillMaxWidth().padding(vertical = 3.dp),
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Text(label, fontSize = 13.sp, color = Color.Gray)
        Text(value, fontSize = 13.sp, fontWeight = FontWeight.SemiBold, color = DarkGreen)
    }
}
