package com.microgrid.app.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.ArrowForward
import androidx.compose.material.icons.filled.LocationOn
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
import kotlinx.coroutines.launch

/**
 * StationSelectionScreen — Component 2, Step 1 of 4
 *
 * Displays active microgrid stations loaded from GET /api/microgridnodes.
 * Only active stations are selectable.
 *
 * @param onBack            Navigate back to ProfileScreen.
 * @param onStationSelected Navigate to Step 2 (DateSelectionScreen) with chosen station.
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun StationSelectionScreen(
    onBack: () -> Unit,
    onStationSelected: (MicrogridNode) -> Unit
) {
    val scope = rememberCoroutineScope()
    var stations by remember { mutableStateOf<List<MicrogridNode>>(emptyList()) }
    var isLoading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf("") }

    LaunchedEffect(Unit) {
        scope.launch {
            try {
                val response = RetrofitClient.instance.getMicrogridNodes()
                if (response.isSuccessful) {
                    stations = response.body() ?: emptyList()
                } else {
                    error = "Failed to load stations (HTTP ${response.code()})"
                }
            } catch (ex: Exception) {
                error = "Network error: ${ex.message}"
            } finally {
                isLoading = false
            }
        }
    }

    val activeStations = stations.filter { it.status.equals("active", ignoreCase = true) }
    val inactiveStations = stations.filter { !it.status.equals("active", ignoreCase = true) }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text("Book Energy", fontWeight = FontWeight.Bold, fontSize = 16.sp)
                        Text("Step 1 of 4 — Select Station", fontSize = 11.sp, color = Color.White.copy(alpha = 0.7f))
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
            // Progress indicator
            BookingProgressBar(currentStep = 1)

            Spacer(Modifier.height(16.dp))

            when {
                isLoading -> {
                    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            CircularProgressIndicator(color = AccentGreen)
                            Spacer(Modifier.height(12.dp))
                            Text("Loading stations…", color = Color.Gray, fontSize = 13.sp)
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
                                            val r = RetrofitClient.instance.getMicrogridNodes()
                                            stations = r.body() ?: emptyList()
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
                stations.isEmpty() -> {
                    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            Text("📡", fontSize = 40.sp)
                            Spacer(Modifier.height(12.dp))
                            Text("No stations found", fontWeight = FontWeight.SemiBold, color = DarkGreen)
                            Text("No microgrid stations are registered.", fontSize = 13.sp, color = Color.Gray)
                        }
                    }
                }
                else -> {
                    LazyColumn(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                        if (activeStations.isNotEmpty()) {
                            item {
                                Text("Active Stations", fontSize = 13.sp, fontWeight = FontWeight.Bold,
                                    color = AccentGreen, modifier = Modifier.padding(vertical = 4.dp))
                            }
                            items(activeStations) { station ->
                                StationCard(station = station, enabled = true) { onStationSelected(station) }
                            }
                        }
                        if (inactiveStations.isNotEmpty()) {
                            item {
                                Spacer(Modifier.height(8.dp))
                                Text("Inactive Stations", fontSize = 13.sp, fontWeight = FontWeight.Bold,
                                    color = Color.Gray, modifier = Modifier.padding(vertical = 4.dp))
                            }
                            items(inactiveStations) { station ->
                                StationCard(station = station, enabled = false) {}
                            }
                        }
                        item { Spacer(Modifier.height(16.dp)) }
                    }
                }
            }
        }
    }
}

@Composable
private fun StationCard(station: MicrogridNode, enabled: Boolean, onClick: () -> Unit) {
    val isActive = station.status.equals("active", ignoreCase = true)
    Card(
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(containerColor = if (enabled) Color.White else Color(0xFFF5F5F5)),
        elevation = CardDefaults.cardElevation(defaultElevation = if (enabled) 3.dp else 1.dp),
        modifier = Modifier
            .fillMaxWidth()
            .then(if (enabled) Modifier.clickable(onClick = onClick) else Modifier)
    ) {
        Row(
            modifier = Modifier.padding(16.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Surface(
                color = if (isActive) Color(0xFFE6F4EA) else Color(0xFFF0F0F0),
                shape = RoundedCornerShape(10.dp),
                modifier = Modifier.size(44.dp)
            ) {
                Box(contentAlignment = Alignment.Center) {
                    Icon(
                        Icons.Filled.LocationOn,
                        contentDescription = null,
                        tint = if (isActive) AccentGreen else Color.Gray,
                        modifier = Modifier.size(24.dp)
                    )
                }
            }
            Spacer(Modifier.width(14.dp))
            Column(Modifier.weight(1f)) {
                Text(
                    station.nodeName.ifBlank { station.nodeId },
                    fontWeight = FontWeight.Bold,
                    fontSize = 15.sp,
                    color = if (enabled) DarkGreen else Color.Gray
                )
                Text(
                    "ID: ${station.nodeId}",
                    fontSize = 12.sp,
                    color = Color.Gray
                )
                if (station.location.isNotBlank()) {
                    Text(
                        "📍 ${station.location}",
                        fontSize = 12.sp,
                        color = Color.Gray
                    )
                }
            }
            Column(horizontalAlignment = Alignment.End) {
                Surface(
                    color = if (isActive) Color(0xFFE6F4EA) else Color(0xFFFCE8E6),
                    shape = RoundedCornerShape(8.dp)
                ) {
                    Text(
                        station.status.replaceFirstChar { it.uppercase() },
                        modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp),
                        fontSize = 11.sp,
                        fontWeight = FontWeight.Bold,
                        color = if (isActive) AccentGreen else Color(0xFFC5221F)
                    )
                }
                if (enabled) {
                    Spacer(Modifier.height(4.dp))
                    Icon(
                        Icons.Filled.ArrowForward,
                        contentDescription = null,
                        tint = AccentGreen,
                        modifier = Modifier.size(16.dp)
                    )
                }
            }
        }
    }
}

/** Shared step-progress bar for the 4-step booking flow */
@Composable
fun BookingProgressBar(currentStep: Int) {
    val steps = listOf("Station", "Date", "Slot", "Confirm")
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 8.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        steps.forEachIndexed { index, label ->
            val step = index + 1
            val isDone = step < currentStep
            val isCurrent = step == currentStep
            Column(
                horizontalAlignment = Alignment.CenterHorizontally,
                modifier = Modifier.weight(1f)
            ) {
                Box(
                    modifier = Modifier
                        .size(28.dp)
                        .background(
                            color = when {
                                isDone -> AccentGreen
                                isCurrent -> DarkGreen
                                else -> Color(0xFFE0E0E0)
                            },
                            shape = androidx.compose.foundation.shape.CircleShape
                        ),
                    contentAlignment = Alignment.Center
                ) {
                    Text(
                        if (isDone) "✓" else "$step",
                        color = if (isDone || isCurrent) Color.White else Color.Gray,
                        fontSize = 12.sp,
                        fontWeight = FontWeight.Bold
                    )
                }
                Spacer(Modifier.height(4.dp))
                Text(
                    label,
                    fontSize = 10.sp,
                    color = if (isCurrent) DarkGreen else Color.Gray,
                    fontWeight = if (isCurrent) FontWeight.Bold else FontWeight.Normal
                )
            }
            if (index < steps.size - 1) {
                Box(
                    modifier = Modifier
                        .weight(0.5f)
                        .height(2.dp)
                        .background(
                            if (step < currentStep) AccentGreen else Color(0xFFE0E0E0)
                        )
                )
            }
        }
    }
}
