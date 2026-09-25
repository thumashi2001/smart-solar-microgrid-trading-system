package com.microgrid.app.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.microgrid.app.data.MicrogridNode
import com.microgrid.app.data.Reservation
import com.microgrid.app.data.RetrofitClient
import com.microgrid.app.data.Slot
import com.microgrid.app.data.UpdateReservationRequest
import kotlinx.coroutines.launch

/**
 * UpdateReservationFlow — Component 2
 *
 * Implements the "Change Slot" flow:
 *   1. Pick new station  (StationSelectionScreen reused)
 *   2. Pick new date     (DateSelectionScreen reused)
 *   3. Pick new slot     (SlotSelectionScreen reused)
 *   4. Confirm update    (this screen)
 *
 * Calls: PUT /api/reservations/{id}  { stationId, slotId }
 * The 12-hour rule is enforced by the API — do NOT implement it on the client.
 *
 * @param reservation      The reservation being changed.
 * @param onBack           Cancel update, go back to MyBookingsScreen.
 * @param onUpdateComplete Refresh MyBookingsScreen after success.
 */

// Sub-step enum for the update flow
enum class UpdateStep { PICK_STATION, PICK_DATE, PICK_SLOT, CONFIRM }

@Composable
fun UpdateReservationFlow(
    reservation: Reservation,
    onBack: () -> Unit,
    onUpdateComplete: () -> Unit
) {
    var updateStep by remember { mutableStateOf(UpdateStep.PICK_STATION) }
    var newStation by remember { mutableStateOf<MicrogridNode?>(null) }
    var newDate by remember { mutableStateOf("") }
    var newSlot by remember { mutableStateOf<Slot?>(null) }

    when (updateStep) {
        UpdateStep.PICK_STATION -> {
            StationSelectionScreen(
                onBack = onBack,
                onStationSelected = { station ->
                    newStation = station
                    updateStep = UpdateStep.PICK_DATE
                }
            )
        }
        UpdateStep.PICK_DATE -> {
            DateSelectionScreen(
                station = newStation!!,
                onBack = { updateStep = UpdateStep.PICK_STATION },
                onDateSelected = { date ->
                    newDate = date
                    updateStep = UpdateStep.PICK_SLOT
                }
            )
        }
        UpdateStep.PICK_SLOT -> {
            SlotSelectionScreenContent(
                station = newStation!!,
                selectedDate = newDate,
                prosumerNic = reservation.prosumerNic,
                onBack = { updateStep = UpdateStep.PICK_DATE },
                onSlotSelected = { slot ->
                    newSlot = slot
                    updateStep = UpdateStep.CONFIRM
                }
            )
        }
        UpdateStep.CONFIRM -> {
            UpdateConfirmScreen(
                reservation = reservation,
                newStation = newStation!!,
                newSlot = newSlot!!,
                onBack = { updateStep = UpdateStep.PICK_SLOT },
                onUpdateComplete = onUpdateComplete
            )
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun UpdateConfirmScreen(
    reservation: Reservation,
    newStation: MicrogridNode,
    newSlot: Slot,
    onBack: () -> Unit,
    onUpdateComplete: () -> Unit
) {
    val scope = rememberCoroutineScope()
    var isSubmitting by remember { mutableStateOf(false) }
    var error by remember { mutableStateOf("") }
    var success by remember { mutableStateOf(false) }

    if (success) {
        Column(
            modifier = Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(24.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Spacer(Modifier.height(48.dp))
            Icon(Icons.Filled.CheckCircle, null, tint = AccentGreen, modifier = Modifier.size(72.dp))
            Spacer(Modifier.height(16.dp))
            Text("Booking Updated!", fontSize = 22.sp, fontWeight = FontWeight.Bold, color = DarkGreen)
            Spacer(Modifier.height(8.dp))
            Text(
                "Your reservation has been moved to the new slot.\nThe booking remains in its current approval status.",
                fontSize = 14.sp, color = Color.Gray, lineHeight = 20.sp
            )
            Spacer(Modifier.height(28.dp))
            Button(
                onClick = onUpdateComplete,
                modifier = Modifier.fillMaxWidth().height(52.dp),
                shape = RoundedCornerShape(12.dp),
                colors = ButtonDefaults.buttonColors(containerColor = AccentGreen)
            ) { Text("Back to My Bookings", fontWeight = FontWeight.Bold, fontSize = 16.sp) }
        }
        return
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Change Slot — Confirm", fontWeight = FontWeight.Bold) },
                navigationIcon = {
                    IconButton(onClick = onBack) { Icon(Icons.Filled.ArrowBack, "Back") }
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
            // Current reservation
            SectionCard(title = "Current Reservation", items = listOf(
                "Reservation ID" to reservation.reservationId,
                "Station" to reservation.stationId,
                "Slot" to reservation.slotId,
                "Status" to reservation.status
            ))

            Spacer(Modifier.height(14.dp))

            // Arrow
            Box(modifier = Modifier.fillMaxWidth(), contentAlignment = Alignment.Center) {
                Text("↓  Moving to new slot  ↓", fontSize = 13.sp, color = AccentGreen, fontWeight = FontWeight.Bold)
            }

            Spacer(Modifier.height(14.dp))

            // New slot
            SectionCard(title = "New Slot", items = listOf(
                "Station" to (newStation.nodeName.ifBlank { newStation.nodeId }),
                "Station ID" to newStation.nodeId,
                "Slot ID" to newSlot.slotId,
                "Date" to newSlot.date,
                "Time" to "${newSlot.startTime} – ${newSlot.endTime}",
                "Spaces Available" to "${newSlot.availability} of ${newSlot.capacity}"
            ))

            Spacer(Modifier.height(14.dp))

            // Policy reminder
            Card(
                colors = CardDefaults.cardColors(containerColor = Color(0xFFFEF7E0)),
                shape = RoundedCornerShape(10.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Text(
                    "📋 The API will reject this change if the current slot starts within 12 hours.\n" +
                    "The new slot must also start within 7 days from now.",
                    modifier = Modifier.padding(14.dp),
                    fontSize = 12.sp, color = Color(0xFF7A5700), lineHeight = 18.sp
                )
            }

            if (error.isNotBlank()) {
                Spacer(Modifier.height(12.dp))
                Card(
                    colors = CardDefaults.cardColors(containerColor = Color(0xFFFCE8E6)),
                    shape = RoundedCornerShape(8.dp), modifier = Modifier.fillMaxWidth()
                ) {
                    Text("⚠ $error", modifier = Modifier.padding(14.dp), fontSize = 13.sp,
                        color = Color(0xFFC5221F), fontWeight = FontWeight.Medium)
                }
            }

            Spacer(Modifier.height(24.dp))

            Button(
                onClick = {
                    error = ""
                    scope.launch {
                        isSubmitting = true
                        try {
                            val resMongoId = reservation.id
                            val r = RetrofitClient.instance.updateReservation(
                                id = resMongoId,
                                request = UpdateReservationRequest(
                                    stationId = newStation.nodeId,
                                    slotId = newSlot.slotId
                                )
                            )
                            if (r.isSuccessful) {
                                success = true
                            } else {
                                val bodyStr = r.errorBody()?.string() ?: ""
                                error = when (r.code()) {
                                    400 -> extractUpdateMsg(bodyStr, "Cannot update: check 12-hour rule or slot validity.")
                                    404 -> extractUpdateMsg(bodyStr, "Reservation or new slot not found.")
                                    409 -> extractUpdateMsg(bodyStr, "Conflict: reservation was modified concurrently.")
                                    else -> "Update failed (HTTP ${r.code()})"
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
                Text(if (isSubmitting) "Updating…" else "Confirm Change", fontWeight = FontWeight.Bold, fontSize = 16.sp)
            }

            Spacer(Modifier.height(16.dp))
        }
    }
}

@Composable
private fun SectionCard(title: String, items: List<Pair<String, String>>) {
    Card(
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        elevation = CardDefaults.cardElevation(3.dp),
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text(title, fontWeight = FontWeight.Bold, fontSize = 15.sp, color = DarkGreen)
            Spacer(Modifier.height(10.dp))
            items.forEach { (label, value) ->
                Row(modifier = Modifier.fillMaxWidth().padding(vertical = 3.dp), horizontalArrangement = Arrangement.SpaceBetween) {
                    Text(label, fontSize = 13.sp, color = Color.Gray)
                    Text(value, fontSize = 13.sp, fontWeight = FontWeight.SemiBold, color = Color(0xFF1C1F1E))
                }
            }
        }
    }
}

private fun extractUpdateMsg(body: String, fallback: String): String {
    val match = Regex("\"message\"\\s*:\\s*\"([^\"]+)\"").find(body)
    return match?.groupValues?.get(1) ?: fallback
}
