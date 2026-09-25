package com.microgrid.app.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
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
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.microgrid.app.data.CreateReservationRequest
import com.microgrid.app.data.RetrofitClient
import com.microgrid.app.data.Slot
import kotlinx.coroutines.launch

/**
 * ReservationSummaryScreen — Component 2
 *
 * Shows the chosen slot details and lets the prosumer enter energyAmount/notes
 * before confirming the booking. On success it transitions to MyBookingsScreen.
 *
 * @param slot          The slot chosen on SlotSelectionScreen.
 * @param prosumerNic   NIC of the logged-in prosumer.
 * @param onBack        Pop back to SlotSelectionScreen.
 * @param onBooked      Navigate to MyBookingsScreen after a successful booking.
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ReservationSummaryScreen(
    slot: Slot,
    prosumerNic: String,
    onBack: () -> Unit,
    onBooked: () -> Unit
) {
    val scope = rememberCoroutineScope()
    var energyAmount by remember { mutableStateOf("") }
    var notes by remember { mutableStateOf("") }
    var isSubmitting by remember { mutableStateOf(false) }
    var error by remember { mutableStateOf("") }
    var success by remember { mutableStateOf(false) }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Confirm Booking", fontWeight = FontWeight.Bold) },
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
        // ── Success overlay ─────────────────────────────────────────────
        if (success) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding)
                    .background(PageBg),
                contentAlignment = Alignment.Center
            ) {
                Column(
                    horizontalAlignment = Alignment.CenterHorizontally,
                    modifier = Modifier.padding(32.dp)
                ) {
                    Icon(
                        Icons.Filled.CheckCircle,
                        contentDescription = null,
                        tint = AccentGreen,
                        modifier = Modifier.size(72.dp)
                    )
                    Spacer(Modifier.height(16.dp))
                    Text(
                        "Booking Submitted!",
                        fontSize = 22.sp,
                        fontWeight = FontWeight.Bold,
                        color = DarkGreen
                    )
                    Spacer(Modifier.height(8.dp))
                    Text(
                        "Your reservation request is now Pending.\nAn admin will review and approve it shortly.",
                        fontSize = 14.sp,
                        color = Color.Gray,
                        lineHeight = 20.sp
                    )
                    Spacer(Modifier.height(28.dp))
                    Button(
                        onClick = onBooked,
                        modifier = Modifier.fillMaxWidth(),
                        shape = RoundedCornerShape(10.dp),
                        colors = ButtonDefaults.buttonColors(containerColor = AccentGreen)
                    ) {
                        Text("View My Bookings", fontWeight = FontWeight.Bold)
                    }
                }
            }
            return@Scaffold
        }

        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .verticalScroll(rememberScrollState())
                .padding(16.dp)
        ) {
            // ── Slot summary card ────────────────────────────────────────
            Card(
                shape = RoundedCornerShape(14.dp),
                colors = CardDefaults.cardColors(containerColor = Color.White),
                elevation = CardDefaults.cardElevation(defaultElevation = 4.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(modifier = Modifier.padding(18.dp)) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Icon(Icons.Filled.BoltSharp, contentDescription = null, tint = AccentGreen)
                        Spacer(Modifier.width(8.dp))
                        Text("Slot Details", fontWeight = FontWeight.Bold, fontSize = 16.sp, color = DarkGreen)
                    }
                    Spacer(Modifier.height(14.dp))
                    SlotDetailRow(Icons.Filled.LocationOn, "Station", slot.stationId)
                    SlotDetailRow(Icons.Filled.CalendarToday, "Date", slot.date)
                    SlotDetailRow(Icons.Filled.Schedule, "Time", "${slot.startTime} – ${slot.endTime}")
                    Spacer(Modifier.height(8.dp))
                    HorizontalDivider()
                    Spacer(Modifier.height(8.dp))
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            Text("${slot.availability}", fontSize = 20.sp, fontWeight = FontWeight.Bold, color = AccentGreen)
                            Text("Spaces Left", fontSize = 11.sp, color = Color.Gray)
                        }
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            Text("${slot.capacity}", fontSize = 20.sp, fontWeight = FontWeight.Bold, color = DarkGreen)
                            Text("Capacity", fontSize = 11.sp, color = Color.Gray)
                        }
                        Surface(
                            color = Color(0xFFE6F4EA),
                            shape = RoundedCornerShape(10.dp)
                        ) {
                            Text(
                                slot.status,
                                modifier = Modifier.padding(horizontal = 10.dp, vertical = 6.dp),
                                fontSize = 12.sp,
                                fontWeight = FontWeight.Bold,
                                color = AccentGreen
                            )
                        }
                    }
                }
            }

            Spacer(Modifier.height(20.dp))

            // ── Booking form ─────────────────────────────────────────────
            Text("Booking Details", fontWeight = FontWeight.Bold, fontSize = 15.sp, color = DarkGreen)
            Spacer(Modifier.height(10.dp))

            OutlinedTextField(
                value = energyAmount,
                onValueChange = { v -> if (v.all { it.isDigit() || it == '.' }) energyAmount = v },
                label = { Text("Energy Amount (kWh) *") },
                placeholder = { Text("e.g. 5.0") },
                leadingIcon = { Icon(Icons.Filled.BoltSharp, contentDescription = null, tint = AccentGreen) },
                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                singleLine = true,
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(10.dp),
                colors = OutlinedTextFieldDefaults.colors(
                    focusedBorderColor = AccentGreen,
                    focusedLabelColor = AccentGreen
                )
            )

            Spacer(Modifier.height(12.dp))

            OutlinedTextField(
                value = notes,
                onValueChange = { notes = it },
                label = { Text("Notes (optional)") },
                placeholder = { Text("Any special requirements…") },
                maxLines = 4,
                modifier = Modifier
                    .fillMaxWidth()
                    .height(120.dp),
                shape = RoundedCornerShape(10.dp),
                colors = OutlinedTextFieldDefaults.colors(
                    focusedBorderColor = AccentGreen,
                    focusedLabelColor = AccentGreen
                )
            )

            if (error.isNotBlank()) {
                Spacer(Modifier.height(12.dp))
                Card(
                    colors = CardDefaults.cardColors(containerColor = Color(0xFFFCE8E6)),
                    shape = RoundedCornerShape(8.dp),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text(
                        "⚠ $error",
                        modifier = Modifier.padding(12.dp),
                        fontSize = 13.sp,
                        color = Color(0xFFC5221F),
                        fontWeight = FontWeight.Medium
                    )
                }
            }

            Spacer(Modifier.height(24.dp))

            // ── Submit button ────────────────────────────────────────────
            Button(
                onClick = {
                    // Validate
                    val kWh = energyAmount.toDoubleOrNull()
                    if (kWh == null || kWh <= 0) {
                        error = "Please enter a valid energy amount greater than 0 kWh."
                        return@Button
                    }
                    error = ""
                    scope.launch {
                        isSubmitting = true
                        try {
                            val slotId = slot.id.ifBlank { slot.slotId }
                            val response = RetrofitClient.instance.createReservation(
                                CreateReservationRequest(
                                    slotId = slotId,
                                    prosumerNic = prosumerNic,
                                    energyAmount = kWh,
                                    notes = notes.trim()
                                )
                            )
                            if (response.isSuccessful) {
                                success = true
                            } else {
                                // Try to extract server message
                                val body = response.errorBody()?.string() ?: ""
                                error = when (response.code()) {
                                    409 -> "This slot has no availability left. Please choose another slot."
                                    400 -> "Invalid booking details: $body"
                                    else -> "Booking failed (HTTP ${response.code()}). Try again."
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
                modifier = Modifier
                    .fillMaxWidth()
                    .height(52.dp),
                shape = RoundedCornerShape(12.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = DarkGreen,
                    disabledContainerColor = Color(0xFF8CB8A8)
                )
            ) {
                if (isSubmitting) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(22.dp),
                        color = Color.White,
                        strokeWidth = 2.dp
                    )
                    Spacer(Modifier.width(10.dp))
                }
                Text(
                    if (isSubmitting) "Submitting…" else "Confirm Booking",
                    fontWeight = FontWeight.Bold,
                    fontSize = 16.sp
                )
            }

            Spacer(Modifier.height(8.dp))

            // Policy note
            Text(
                "Your reservation will be in Pending status until an administrator approves it. Cancellations must be made at least 12 hours before the slot start time.",
                fontSize = 11.sp,
                color = Color.Gray,
                lineHeight = 16.sp,
                modifier = Modifier.padding(horizontal = 4.dp)
            )

            Spacer(Modifier.height(16.dp))
        }
    }
}

@Composable
private fun SlotDetailRow(icon: ImageVector, label: String, value: String) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 4.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(icon, contentDescription = null, tint = Color.Gray, modifier = Modifier.size(16.dp))
        Spacer(Modifier.width(8.dp))
        Text("$label: ", fontSize = 13.sp, color = Color.Gray, fontWeight = FontWeight.Medium)
        Text(value, fontSize = 13.sp, fontWeight = FontWeight.SemiBold, color = Color(0xFF1C1F1E))
    }
}
