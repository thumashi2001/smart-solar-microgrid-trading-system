package com.microgrid.app.ui.screens

import androidx.compose.foundation.background
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
import androidx.compose.material.icons.filled.Schedule
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.microgrid.app.data.RetrofitClient
import com.microgrid.app.data.Slot
import kotlinx.coroutines.launch

/**
 * SlotSelectionScreen — Component 2
 *
 * Shows all Available energy booking slots. The prosumer taps a slot to
 * proceed to the booking confirmation flow.
 *
 * Navigation inputs:
 *  @param prosumerNic  NIC of the logged-in prosumer (from LoginScreen)
 *  @param onBack       Pop back to ProfileScreen
 *  @param onSlotSelected  Navigate to ReservationSummaryScreen with the chosen slot
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SlotSelectionScreen(
    prosumerNic: String,
    onBack: () -> Unit,
    onSlotSelected: (Slot) -> Unit
) {
    val scope = rememberCoroutineScope()
    var slots by remember { mutableStateOf<List<Slot>>(emptyList()) }
    var isLoading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf("") }
    var selectedDate by remember { mutableStateOf("") }

    // Load available slots on first compose (and when date filter changes)
    LaunchedEffect(selectedDate) {
        scope.launch {
            isLoading = true
            error = ""
            try {
                val response = RetrofitClient.instance.getSlots(
                    date = selectedDate.ifBlank { null }
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
                title = { Text("Book Energy Slot", fontWeight = FontWeight.Bold) },
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
            Spacer(Modifier.height(12.dp))

            // ── Info banner ──────────────────────────────────────────────
            Card(
                colors = CardDefaults.cardColors(containerColor = Color(0xFFE6F4EA)),
                shape = RoundedCornerShape(10.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Row(
                    modifier = Modifier.padding(12.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Icon(Icons.Filled.BoltSharp, contentDescription = null, tint = AccentGreen)
                    Spacer(Modifier.width(10.dp))
                    Text(
                        "Select an available slot to book your energy reservation at a microgrid station.",
                        fontSize = 13.sp,
                        color = Color(0xFF0B3B2E)
                    )
                }
            }

            Spacer(Modifier.height(12.dp))

            // ── Section label ────────────────────────────────────────────
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Text(
                    "Available Slots",
                    fontSize = 16.sp,
                    fontWeight = FontWeight.Bold,
                    color = DarkGreen
                )
                Text(
                    "${slots.size} found",
                    fontSize = 12.sp,
                    color = Color.Gray
                )
            }

            Spacer(Modifier.height(8.dp))

            // ── Content ──────────────────────────────────────────────────
            when {
                isLoading -> {
                    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            CircularProgressIndicator(color = AccentGreen)
                            Spacer(Modifier.height(12.dp))
                            Text("Loading available slots…", color = Color.Gray, fontSize = 13.sp)
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
                                        isLoading = true
                                        error = ""
                                        try {
                                            val r = RetrofitClient.instance.getSlots()
                                            slots = r.body()
                                                ?.filter { it.status == "Available" && it.availability > 0 }
                                                ?: emptyList()
                                        } catch (ex: Exception) {
                                            error = ex.message ?: "Unknown error"
                                        } finally { isLoading = false }
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
                            Text("No available slots right now", fontWeight = FontWeight.SemiBold, color = DarkGreen)
                            Text("Check back later for new openings.", fontSize = 13.sp, color = Color.Gray)
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
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onClick)
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            // Header row
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Surface(
                    color = Color(0xFFEBE8E1),
                    shape = RoundedCornerShape(6.dp)
                ) {
                    Text(
                        slot.slotId,
                        modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp),
                        fontSize = 11.sp,
                        fontWeight = FontWeight.Bold,
                        color = DarkGreen
                    )
                }
                Surface(
                    color = Color(0xFFE6F4EA),
                    shape = RoundedCornerShape(12.dp)
                ) {
                    Row(
                        modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Icon(
                            Icons.Filled.CheckCircle,
                            contentDescription = null,
                            tint = AccentGreen,
                            modifier = Modifier.size(13.dp)
                        )
                        Spacer(Modifier.width(4.dp))
                        Text("Available", fontSize = 11.sp, fontWeight = FontWeight.Bold, color = AccentGreen)
                    }
                }
            }

            Spacer(Modifier.height(10.dp))

            // Station
            Text(
                "Station: ${slot.stationId}",
                fontSize = 14.sp,
                fontWeight = FontWeight.SemiBold,
                color = DarkGreen
            )

            Spacer(Modifier.height(6.dp))

            // Date + time
            Row(horizontalArrangement = Arrangement.spacedBy(16.dp)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Icon(Icons.Filled.CalendarToday, contentDescription = null, tint = Color.Gray, modifier = Modifier.size(14.dp))
                    Spacer(Modifier.width(4.dp))
                    Text(slot.date, fontSize = 13.sp, color = Color(0xFF444444))
                }
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Icon(Icons.Filled.Schedule, contentDescription = null, tint = Color.Gray, modifier = Modifier.size(14.dp))
                    Spacer(Modifier.width(4.dp))
                    Text("${slot.startTime} – ${slot.endTime}", fontSize = 13.sp, color = Color(0xFF444444))
                }
            }

            Spacer(Modifier.height(10.dp))

            // Availability bar
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    "${slot.availability} spaces left",
                    fontSize = 12.sp,
                    fontWeight = FontWeight.Bold,
                    color = barColor
                )
                Text(
                    "of ${slot.capacity}",
                    fontSize = 12.sp,
                    color = Color.Gray
                )
            }
            Spacer(Modifier.height(4.dp))
            LinearProgressIndicator(
                progress = { fillPct },
                modifier = Modifier
                    .fillMaxWidth()
                    .height(6.dp),
                color = barColor,
                trackColor = Color(0xFFEBE8E1)
            )

            Spacer(Modifier.height(10.dp))

            // CTA
            Button(
                onClick = onClick,
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(8.dp),
                colors = ButtonDefaults.buttonColors(containerColor = AccentGreen)
            ) {
                Text("Book This Slot", fontWeight = FontWeight.Bold)
            }
        }
    }
}
