package com.microgrid.app.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.CalendarToday
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.microgrid.app.data.MicrogridNode
import java.time.LocalDate
import java.time.format.DateTimeFormatter
import java.time.format.TextStyle
import java.util.Locale

/**
 * DateSelectionScreen — Component 2, Step 2 of 4
 *
 * Shows the next 7 days (7-day booking window enforced by backend).
 * Prosumer selects a date, then proceeds to slot selection.
 *
 * @param station      Station chosen in Step 1.
 * @param onBack       Navigate back to StationSelectionScreen.
 * @param onDateSelected Navigate to Step 3 (SlotSelectionScreen).
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DateSelectionScreen(
    station: MicrogridNode,
    onBack: () -> Unit,
    onDateSelected: (String) -> Unit        // yyyy-MM-dd
) {
    val today = LocalDate.now()
    val displayDates = (0 until 7).map { today.plusDays(it.toLong()) }
    val isoFormatter = DateTimeFormatter.ofPattern("yyyy-MM-dd")
    var selectedDate by remember { mutableStateOf<LocalDate?>(null) }
    val listState = rememberLazyListState()

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text("Book Energy", fontWeight = FontWeight.Bold, fontSize = 16.sp)
                        Text("Step 2 of 4 — Select Date", fontSize = 11.sp, color = Color.White.copy(alpha = 0.7f))
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
            BookingProgressBar(currentStep = 2)

            Spacer(Modifier.height(16.dp))

            // Station reminder chip
            Card(
                colors = CardDefaults.cardColors(containerColor = Color(0xFFE6F4EA)),
                shape = RoundedCornerShape(10.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Row(modifier = Modifier.padding(12.dp), verticalAlignment = Alignment.CenterVertically) {
                    Icon(Icons.Filled.CalendarToday, contentDescription = null, tint = AccentGreen, modifier = Modifier.size(18.dp))
                    Spacer(Modifier.width(8.dp))
                    Column {
                        Text("Station", fontSize = 11.sp, color = Color.Gray)
                        Text(
                            station.nodeName.ifBlank { station.nodeId },
                            fontWeight = FontWeight.Bold, fontSize = 14.sp, color = DarkGreen
                        )
                    }
                }
            }

            Spacer(Modifier.height(20.dp))

            Text(
                "Select a Date",
                fontSize = 18.sp,
                fontWeight = FontWeight.Bold,
                color = DarkGreen
            )
            Text(
                "Reservations can only be made within 7 days from today.",
                fontSize = 12.sp,
                color = Color.Gray,
                modifier = Modifier.padding(top = 2.dp, bottom = 16.dp)
            )

            // Calendar strip
            LazyRow(
                state = listState,
                horizontalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                itemsIndexed(displayDates) { _, date ->
                    val isSelected = date == selectedDate
                    val isToday = date == today
                    val dayName = date.dayOfWeek.getDisplayName(TextStyle.SHORT, Locale.getDefault())
                    val dayNum = date.dayOfMonth
                    val monthName = date.month.getDisplayName(TextStyle.SHORT, Locale.getDefault())

                    Column(
                        horizontalAlignment = Alignment.CenterHorizontally,
                        modifier = Modifier
                            .width(64.dp)
                            .background(
                                color = when {
                                    isSelected -> DarkGreen
                                    isToday -> Color(0xFFE6F4EA)
                                    else -> Color.White
                                },
                                shape = RoundedCornerShape(12.dp)
                            )
                            .clickable { selectedDate = date }
                            .padding(vertical = 12.dp, horizontal = 8.dp)
                    ) {
                        Text(
                            dayName.uppercase(),
                            fontSize = 10.sp,
                            color = if (isSelected) Color.White.copy(alpha = 0.7f) else Color.Gray,
                            fontWeight = FontWeight.Bold
                        )
                        Spacer(Modifier.height(6.dp))
                        Text(
                            "$dayNum",
                            fontSize = 22.sp,
                            fontWeight = FontWeight.Bold,
                            color = when {
                                isSelected -> Color.White
                                isToday -> AccentGreen
                                else -> DarkGreen
                            }
                        )
                        Spacer(Modifier.height(2.dp))
                        Text(
                            monthName,
                            fontSize = 10.sp,
                            color = if (isSelected) Color.White.copy(alpha = 0.7f) else Color.Gray
                        )
                        if (isToday) {
                            Spacer(Modifier.height(4.dp))
                            Box(
                                modifier = Modifier
                                    .size(6.dp)
                                    .background(if (isSelected) Color.White else AccentGreen, shape = androidx.compose.foundation.shape.CircleShape)
                            )
                        }
                    }
                }
            }

            Spacer(Modifier.height(24.dp))

            // Selected date display
            if (selectedDate != null) {
                Card(
                    colors = CardDefaults.cardColors(containerColor = Color.White),
                    shape = RoundedCornerShape(12.dp),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Row(
                        modifier = Modifier.padding(16.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Icon(Icons.Filled.CalendarToday, contentDescription = null, tint = AccentGreen)
                        Spacer(Modifier.width(12.dp))
                        Column {
                            Text("Selected Date", fontSize = 11.sp, color = Color.Gray)
                            Text(
                                selectedDate!!.format(DateTimeFormatter.ofPattern("EEEE, d MMMM yyyy", Locale.getDefault())),
                                fontWeight = FontWeight.Bold,
                                fontSize = 14.sp,
                                color = DarkGreen
                            )
                        }
                    }
                }
                Spacer(Modifier.height(24.dp))
            }

            Spacer(Modifier.weight(1f))

            // Continue button
            Button(
                onClick = {
                    selectedDate?.let { onDateSelected(it.format(isoFormatter)) }
                },
                enabled = selectedDate != null,
                modifier = Modifier
                    .fillMaxWidth()
                    .height(52.dp),
                shape = RoundedCornerShape(12.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = DarkGreen,
                    disabledContainerColor = Color(0xFF8CB8A8)
                )
            ) {
                Text(
                    "View Available Slots →",
                    fontWeight = FontWeight.Bold,
                    fontSize = 16.sp
                )
            }

            Spacer(Modifier.height(16.dp))
        }
    }
}
