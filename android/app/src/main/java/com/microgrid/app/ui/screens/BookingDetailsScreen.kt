package com.microgrid.app.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

private val DetailsGreen = Color(0xFF0B4F3C)
private val DetailsAccentGreen = Color(0xFF1E8754)
private val DetailsBackground = Color(0xFFF8F6F2)
private val DetailsGray = Color(0xFF777777)

@Composable
fun BookingDetailsScreen(
    bookingId: String,
    onBackClick: () -> Unit
) {

    // Temporary sample data until Viman's reservation API is available.
    val booking = when (bookingId) {

        "BK-1002" -> BookingUiModel(
            id = "BK-1002",
            stationName = "Malabe Solar Hub",
            location = "Malabe",
            date = "29 Sep 2026",
            time = "02:30 PM",
            energyAmount = "15 kWh",
            status = "Approved"
        )

        "BK-1003" -> BookingUiModel(
            id = "BK-1003",
            stationName = "Kaduwela Energy Station",
            location = "Kaduwela",
            date = "30 Sep 2026",
            time = "09:00 AM",
            energyAmount = "25 kWh",
            status = "Pending"
        )

        else -> BookingUiModel(
            id = "BK-1001",
            stationName = "Colombo Solar Hub",
            location = "Colombo",
            date = "28 Sep 2026",
            time = "10:00 AM",
            energyAmount = "20 kWh",
            status = "Pending"
        )
    }

    Scaffold(
        containerColor = DetailsBackground
    ) { innerPadding ->

        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
                .background(DetailsBackground)
                .padding(20.dp)
        ) {

            Row(
                verticalAlignment = Alignment.CenterVertically
            ) {

                IconButton(
                    onClick = onBackClick
                ) {
                    Icon(
                        imageVector = Icons.Default.ArrowBack,
                        contentDescription = "Back",
                        tint = DetailsGreen
                    )
                }

                Column {

                    Text(
                        text = "Booking Details",
                        fontSize = 24.sp,
                        fontWeight = FontWeight.Bold,
                        color = DetailsGreen
                    )

                    Text(
                        text = "Reservation information",
                        fontSize = 12.sp,
                        color = DetailsGray
                    )
                }
            }

            Spacer(modifier = Modifier.height(24.dp))

            Card(
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(18.dp),
                colors = CardDefaults.cardColors(
                    containerColor = Color.White
                ),
                elevation = CardDefaults.cardElevation(
                    defaultElevation = 2.dp
                )
            ) {

                Column(
                    modifier = Modifier.padding(20.dp)
                ) {

                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.Top
                    ) {

                        Column(
                            modifier = Modifier.weight(1f)
                        ) {

                            Text(
                                text = booking.stationName,
                                fontSize = 20.sp,
                                fontWeight = FontWeight.Bold,
                                color = DetailsGreen
                            )

                            Spacer(modifier = Modifier.height(5.dp))

                            Row(
                                verticalAlignment = Alignment.CenterVertically
                            ) {

                                Icon(
                                    imageVector = Icons.Default.LocationOn,
                                    contentDescription = null,
                                    tint = DetailsAccentGreen,
                                    modifier = Modifier.size(17.dp)
                                )

                                Spacer(modifier = Modifier.width(5.dp))

                                Text(
                                    text = booking.location,
                                    fontSize = 13.sp,
                                    color = DetailsGray
                                )
                            }
                        }

                        DetailsStatusBadge(
                            status = booking.status
                        )
                    }

                    Spacer(modifier = Modifier.height(22.dp))

                    HorizontalDivider(
                        color = Color(0xFFE8E8E8)
                    )

                    Spacer(modifier = Modifier.height(18.dp))

                    Text(
                        text = "Booking ID",
                        fontSize = 12.sp,
                        color = DetailsGray
                    )

                    Spacer(modifier = Modifier.height(3.dp))

                    Text(
                        text = booking.id,
                        fontSize = 16.sp,
                        fontWeight = FontWeight.SemiBold,
                        color = DetailsGreen
                    )
                }
            }

            Spacer(modifier = Modifier.height(18.dp))

            Text(
                text = "Reservation Details",
                fontSize = 17.sp,
                fontWeight = FontWeight.Bold,
                color = DetailsGreen
            )

            Spacer(modifier = Modifier.height(10.dp))

            Card(
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(18.dp),
                colors = CardDefaults.cardColors(
                    containerColor = Color.White
                ),
                elevation = CardDefaults.cardElevation(
                    defaultElevation = 2.dp
                )
            ) {

                Column(
                    modifier = Modifier.padding(20.dp)
                ) {

                    DetailsRow(
                        icon = Icons.Default.CalendarMonth,
                        title = "Reservation Date",
                        value = booking.date
                    )

                    HorizontalDivider(
                        modifier = Modifier.padding(vertical = 14.dp),
                        color = Color(0xFFECECEC)
                    )

                    DetailsRow(
                        icon = Icons.Default.Schedule,
                        title = "Reservation Time",
                        value = booking.time
                    )

                    HorizontalDivider(
                        modifier = Modifier.padding(vertical = 14.dp),
                        color = Color(0xFFECECEC)
                    )

                    DetailsRow(
                        icon = Icons.Default.Bolt,
                        title = "Energy Amount",
                        value = booking.energyAmount
                    )

                    HorizontalDivider(
                        modifier = Modifier.padding(vertical = 14.dp),
                        color = Color(0xFFECECEC)
                    )

                    DetailsRow(
                        icon = Icons.Default.CheckCircle,
                        title = "Status",
                        value = booking.status
                    )
                }
            }

            Spacer(modifier = Modifier.weight(1f))

            OutlinedButton(
                onClick = onBackClick,
                modifier = Modifier
                    .fillMaxWidth()
                    .height(50.dp),
                shape = RoundedCornerShape(12.dp)
            ) {

                Icon(
                    imageVector = Icons.Default.ArrowBack,
                    contentDescription = null
                )

                Spacer(modifier = Modifier.width(8.dp))

                Text(
                    text = "Back to My Bookings"
                )
            }

            Spacer(modifier = Modifier.height(10.dp))
        }
    }
}

@Composable
private fun DetailsRow(
    icon: ImageVector,
    title: String,
    value: String
) {

    Row(
        modifier = Modifier.fillMaxWidth(),
        verticalAlignment = Alignment.CenterVertically
    ) {

        Surface(
            modifier = Modifier.size(42.dp),
            shape = RoundedCornerShape(10.dp),
            color = Color(0xFFE6F3EC)
        ) {

            Box(
                contentAlignment = Alignment.Center
            ) {

                Icon(
                    imageVector = icon,
                    contentDescription = null,
                    tint = DetailsAccentGreen,
                    modifier = Modifier.size(21.dp)
                )
            }
        }

        Spacer(modifier = Modifier.width(14.dp))

        Column {

            Text(
                text = title,
                fontSize = 12.sp,
                color = DetailsGray
            )

            Spacer(modifier = Modifier.height(3.dp))

            Text(
                text = value,
                fontSize = 15.sp,
                fontWeight = FontWeight.SemiBold,
                color = DetailsGreen
            )
        }
    }
}

@Composable
private fun DetailsStatusBadge(
    status: String
) {

    val backgroundColor =
        when (status.lowercase()) {

            "approved" -> Color(0xFFE4F4EA)

            "pending" -> Color(0xFFFFF3D6)

            "cancelled" -> Color(0xFFFFE4E1)

            else -> Color(0xFFEDEDED)
        }

    val textColor =
        when (status.lowercase()) {

            "approved" -> Color(0xFF19733E)

            "pending" -> Color(0xFF9A6700)

            "cancelled" -> Color(0xFFB3261E)

            else -> Color.DarkGray
        }

    Surface(
        shape = RoundedCornerShape(20.dp),
        color = backgroundColor
    ) {

        Text(
            text = status,
            color = textColor,
            fontSize = 11.sp,
            fontWeight = FontWeight.SemiBold,
            modifier = Modifier.padding(
                horizontal = 12.dp,
                vertical = 7.dp
            )
        )
    }
}