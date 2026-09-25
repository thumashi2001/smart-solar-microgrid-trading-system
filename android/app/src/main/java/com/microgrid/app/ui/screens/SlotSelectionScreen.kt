package com.microgrid.app.ui.screens

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
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
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.microgrid.app.data.MicrogridNode
import com.microgrid.app.data.RetrofitClient
import com.microgrid.app.data.Slot
import kotlinx.coroutines.launch

/**
 * SlotSelectionScreen — Component 2, Step 3 of 4
 *
 * Loads available slots for a specific station + date via GET /api/slots.
 * Only Available slots with availability > 0 are shown.
 *
 * @param station       Chosen station (Step 1).
 * @param selectedDate  Chosen date in yyyy-MM-dd format (Step 2).
 * @param onBack        Navigate back to DateSelectionScreen.
 * @param onSlotSelected Navigate to Step 4 (ReservationSummaryScreen) with the chosen slot.
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SlotSelectionScreen(
    station: MicrogridNode,
    selectedDate: String,
    onBack: () -> Unit,
    onSlotSelected: (Slot) -> Unit
) {
    // Legacy overload: when called without station/date context (from old flow), load all
    // This overload is kept for any remaining legacy call sites.
    SlotSelectionScreenContent(
        station = station,
        selectedDate = selectedDate,
        prosumerNic = "",
        onBack = onBack,
        onSlotSelected = onSlotSelected
    )
}

// Separate content function — also re-usable from the update flow
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SlotSelectionScreenContent(
    station: MicrogridNode,
    selectedDate: String,
    prosumerNic: String,
    onBack: () -> Unit,
    onSlotSelected: (Slot) -> Unit
) {
    val scope = rememberCoroutineScope()
    var slots by remember { mutableStateOf<List<Slot>>(emptyList()) }
    var isLoading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf("") }

    LaunchedEffect(station.nodeId, selectedDate) {
        scope.launch {
            isLoading = true
            error = ""
            try {
                val response = RetrofitClient.instance.getSlots(
                    stationId = station.nodeId,
                    date = selectedDate
                )
                if (response.isSuccessful) {
                    slots = (response.body() ?: emptyList())
                        .filter { it.status == "Available" && it.availability > 0 }
                } else {
                    error = "Failed to load slots (HTTP ${response.code()})"
                }
            } catch (ex: Exception) {
                error = "Network error: ${ex.message}"
            } finally {
                isLoading = false
            }
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text("Book Energy", fontWeight = FontWeight.Bold, fontSize = 16.sp)
                        Text("Step 3 of 4 — Select Slot", fontSize = 11.sp, color = Color.White.copy(alpha = 0.7f))
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
                .padding(horizontal = 16.dp)
        ) {
            BookingProgressBar(currentStep = 3)

            Spacer(Modifier.height(12.dp))

            // Context chips
            Card(
                colors = CardDefaults.cardColors(containerColor = Color(0xFFE6F4EA)),
                shape = RoundedCornerShape(10.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Row(
                    modifier = Modifier.padding(12.dp),
                    horizontalArrangement = Arrangement.spacedBy(16.dp)
                ) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Icon(Icons.Filled.LocationOn, null, tint = AccentGreen, modifier = Modifier.size(16.dp))
                        Spacer(Modifier.width(4.dp))
                        Text(station.nodeName.ifBlank { station.nodeId }, fontSize = 13.sp, color = DarkGreen, fontWeight = FontWeight.SemiBold)
                    }
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Icon(Icons.Filled.CalendarToday, null, tint = AccentGreen, modifier = Modifier.size(16.dp))
                        Spacer(Modifier.width(4.dp))
                        Text(selectedDate, fontSize = 13.sp, color = DarkGreen, fontWeight = FontWeight.SemiBold)
                    }
                }
            }

            Spacer(Modifier.height(12.dp))

            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Text("Available Time Slots", fontSize = 16.sp, fontWeight = FontWeight.Bold, color = DarkGreen)
                Text("${slots.size} found", fontSize = 12.sp, color = Color.Gray)
            }

            Spacer(Modifier.height(8.dp))

            when {
                isLoading -> {
                    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            CircularProgressIndicator(color = AccentGreen)
                            Spacer(Modifier.height(12.dp))
                            Text("Loading slots…", color = Color.Gray, fontSize = 13.sp)
                        }
                    }
                }
                error.isNotBlank() -> {
                    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            Text("⚠", fontSize = 32.sp)
                            Spacer(Modifier.height(8.dp))
                            Text(error, color = Color(0xFFC5221F), fontSize = 13.sp)
                            Spacer(Modifier.height(16.dp))
                            Button(
                                onClick = {
                                    scope.launch {
                                        isLoading = true; error = ""
                                        try {
                                            val r = RetrofitClient.instance.getSlots(stationId = station.nodeId, date = selectedDate)
                                            slots = r.body()?.filter { it.status == "Available" && it.availability > 0 } ?: emptyList()
                                            if (!r.isSuccessful) error = "HTTP ${r.code()}"
                                        } catch (ex: Exception) { error = ex.message ?: "Error" }
                                        finally { isLoading = false }
                                    }
                                },
                                colors = ButtonDefaults.buttonColors(containerColor = AccentGreen)
                            ) { Text("Retry") }
                        }
                    }
                }
                slots.isEmpty() -> {
                    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            Text("⚡", fontSize = 40.sp)
                            Spacer(Modifier.height(12.dp))
                            Text("No slots available", fontWeight = FontWeight.SemiBold, color = DarkGreen)
                            Text("No available slots for this station and date.", fontSize = 13.sp, color = Color.Gray)
                            Spacer(Modifier.height(8.dp))
                            OutlinedButton(onClick = onBack) { Text("Choose Different Date") }
                        }
                    }
                }
                else -> {
                    LazyColumn(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                        items(slots) { slot ->
                            SlotCard(slot = slot, onClick = { onSlotSelected(slot) })
                        }
                        item { Spacer(Modifier.height(16.dp)) }
                    }
                }
            }
        }
    }
}

@Composable
private fun SlotCard(slot: Slot, onClick: () -> Unit) {
    val fillPct = if (slot.capacity > 0) slot.availability.toFloat() / slot.capacity else 0f
    val barColor = when {
        fillPct <= 0f -> Color(0xFFC5221F)
        fillPct <= 0.4f -> Color(0xFFF9AB00)
        else -> AccentGreen
    }

    Card(
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        elevation = CardDefaults.cardElevation(defaultElevation = 3.dp),
        modifier = Modifier.fillMaxWidth().clickable(onClick = onClick)
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Surface(color = Color(0xFFEBE8E1), shape = RoundedCornerShape(6.dp)) {
                    Text(
                        slot.slotId,
                        modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp),
                        fontSize = 11.sp, fontWeight = FontWeight.Bold, color = DarkGreen
                    )
                }
                Surface(color = Color(0xFFE6F4EA), shape = RoundedCornerShape(12.dp)) {
                    Row(modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp), verticalAlignment = Alignment.CenterVertically) {
                        Icon(Icons.Filled.CheckCircle, null, tint = AccentGreen, modifier = Modifier.size(13.dp))
                        Spacer(Modifier.width(4.dp))
                        Text("Available", fontSize = 11.sp, fontWeight = FontWeight.Bold, color = AccentGreen)
                    }
                }
            }

            Spacer(Modifier.height(10.dp))

            Row(horizontalArrangement = Arrangement.spacedBy(16.dp)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Icon(Icons.Filled.CalendarToday, null, tint = Color.Gray, modifier = Modifier.size(14.dp))
                    Spacer(Modifier.width(4.dp))
                    Text(slot.date, fontSize = 13.sp, color = Color(0xFF444444))
                }
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Icon(Icons.Filled.Schedule, null, tint = Color.Gray, modifier = Modifier.size(14.dp))
                    Spacer(Modifier.width(4.dp))
                    Text("${slot.startTime} – ${slot.endTime}", fontSize = 13.sp, fontWeight = FontWeight.SemiBold, color = Color(0xFF444444))
                }
            }

            Spacer(Modifier.height(10.dp))

            Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                Text("${slot.availability} spaces left", fontSize = 12.sp, fontWeight = FontWeight.Bold, color = barColor)
                Text("of ${slot.capacity}", fontSize = 12.sp, color = Color.Gray)
            }
            Spacer(Modifier.height(4.dp))
            LinearProgressIndicator(
                progress = { fillPct },
                modifier = Modifier.fillMaxWidth().height(6.dp),
                color = barColor,
                trackColor = Color(0xFFEBE8E1)
            )

            Spacer(Modifier.height(10.dp))

            Button(
                onClick = onClick,
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(8.dp),
                colors = ButtonDefaults.buttonColors(containerColor = AccentGreen)
            ) {
                Icon(Icons.Filled.BoltSharp, null, modifier = Modifier.size(16.dp))
                Spacer(Modifier.width(6.dp))
                Text("Book This Slot", fontWeight = FontWeight.Bold)
            }
        }
    }
}
