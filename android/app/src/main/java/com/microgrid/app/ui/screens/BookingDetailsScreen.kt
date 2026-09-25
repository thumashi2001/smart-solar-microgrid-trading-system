package com.microgrid.app.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.microgrid.app.data.BookingUiModel
import com.microgrid.app.data.RetrofitClient

private val DetailsGreen = Color(0xFF0B4F3C)
private val DetailsAccentGreen = Color(0xFF1E8754)
private val DetailsBackground = Color(0xFFF8F6F2)
private val DetailsGray = Color(0xFF777777)

@Composable
fun BookingDetailsScreen(
    bookingId: String,
    nic: String,
    onBackClick: () -> Unit
) {
    var booking by remember {
        mutableStateOf<BookingUiModel?>(null)
    }

    var isLoading by remember {
        mutableStateOf(true)
    }

    var errorMessage by remember {
        mutableStateOf<String?>(null)
    }

    /*
     * Load the selected reservation using the logged-in prosumer's
     * reservation history.
     *
     * The reservation is then joined with:
     *  - EnergyBookingSlot using slotId
     *  - MicrogridNode using stationId
     */
    LaunchedEffect(bookingId, nic) {
        isLoading = true
        errorMessage = null

        try {
            val reservationResponse =
                RetrofitClient.instance.getReservationHistory(nic)

            val slotResponse =
                RetrofitClient.instance.getSlots()

            val nodeResponse =
                RetrofitClient.instance.getMicrogridNodes()

            if (
                reservationResponse.isSuccessful &&
                slotResponse.isSuccessful &&
                nodeResponse.isSuccessful
            ) {
                val reservations =
                    reservationResponse.body().orEmpty()

                val slots =
                    slotResponse.body().orEmpty()

                val nodes =
                    nodeResponse.body().orEmpty()

                // Find the reservation selected from My Bookings
                val reservation = reservations.find {
                    it.reservationId == bookingId
                }

                if (reservation == null) {
                    errorMessage = "Booking not found."
                } else {
                    // Find the slot belonging to this reservation
                    val slot = slots.find {
                        it.slotId == reservation.slotId
                    }

                    // Find the microgrid station belonging to this reservation
                    val node = nodes.find {
                        it.nodeId == reservation.stationId
                    }

                    booking = BookingUiModel(
                        id = reservation.reservationId,

                        stationName =
                            node?.nodeName
                                ?: reservation.stationId,

                        location =
                            node?.location
                                ?: "Unknown location",

                        date =
                            slot?.date?.substringBefore("T")
                                ?: "Date unavailable",

                        time =
                            if (slot != null) {
                                "${slot.startTime} - ${slot.endTime}"
                            } else {
                                "Time unavailable"
                            },

                        /*
                         * Current backend reservation response does not
                         * contain a separately reserved energy amount.
                         *
                         * Therefore the slot capacity is displayed here
                         * until the backend provides a reservation amount.
                         */
                        energyAmount =
                            if (slot != null) {
                                "${slot.capacity} kWh"
                            } else {
                                "Capacity unavailable"
                            },

                        status = reservation.status
                    )
                }
            } else {
                errorMessage =
                    "Unable to load booking details."
            }
        } catch (e: Exception) {
            errorMessage =
                e.message ?: "Unable to connect to the server."
        } finally {
            isLoading = false
        }
    }

    Scaffold(
        containerColor = DetailsBackground
    ) { innerPadding ->

        when {
            isLoading -> {
                Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(innerPadding),
                    contentAlignment = Alignment.Center
                ) {
                    Column(
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        CircularProgressIndicator(
                            color = DetailsAccentGreen
                        )

                        Spacer(
                            modifier = Modifier.height(12.dp)
                        )

                        Text(
                            text = "Loading booking details...",
                            color = DetailsGray,
                            fontSize = 13.sp
                        )
                    }
                }
            }

            errorMessage != null -> {
                BookingDetailsErrorView(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(innerPadding),
                    message = errorMessage!!,
                    onBackClick = onBackClick
                )
            }

            booking != null -> {
                BookingDetailsContent(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(innerPadding),
                    booking = booking!!,
                    onBackClick = onBackClick
                )
            }
        }
    }
}

@Composable
private fun BookingDetailsContent(
    modifier: Modifier = Modifier,
    booking: BookingUiModel,
    onBackClick: () -> Unit
) {
    Column(
        modifier = modifier
            .background(DetailsBackground)
            .verticalScroll(rememberScrollState())
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

        Spacer(
            modifier = Modifier.height(24.dp)
        )

        /*
         * Main reservation card
         */
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
                    horizontalArrangement =
                        Arrangement.SpaceBetween,
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

                        Spacer(
                            modifier = Modifier.height(5.dp)
                        )

                        Row(
                            verticalAlignment =
                                Alignment.CenterVertically
                        ) {
                            Icon(
                                imageVector =
                                    Icons.Default.LocationOn,
                                contentDescription = null,
                                tint = DetailsAccentGreen,
                                modifier = Modifier.size(17.dp)
                            )

                            Spacer(
                                modifier = Modifier.width(5.dp)
                            )

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

                Spacer(
                    modifier = Modifier.height(22.dp)
                )

                HorizontalDivider(
                    color = Color(0xFFE8E8E8)
                )

                Spacer(
                    modifier = Modifier.height(18.dp)
                )

                Text(
                    text = "Booking ID",
                    fontSize = 12.sp,
                    color = DetailsGray
                )

                Spacer(
                    modifier = Modifier.height(3.dp)
                )

                Text(
                    text = booking.id,
                    fontSize = 16.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = DetailsGreen
                )
            }
        }

        Spacer(
            modifier = Modifier.height(18.dp)
        )

        Text(
            text = "Reservation Details",
            fontSize = 17.sp,
            fontWeight = FontWeight.Bold,
            color = DetailsGreen
        )

        Spacer(
            modifier = Modifier.height(10.dp)
        )

        /*
         * Reservation information
         */
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
                    modifier =
                        Modifier.padding(vertical = 14.dp),
                    color = Color(0xFFECECEC)
                )

                DetailsRow(
                    icon = Icons.Default.Schedule,
                    title = "Reservation Time",
                    value = booking.time
                )

                HorizontalDivider(
                    modifier =
                        Modifier.padding(vertical = 14.dp),
                    color = Color(0xFFECECEC)
                )

                DetailsRow(
                    icon = Icons.Default.Bolt,
                    title = "Energy Capacity",
                    value = booking.energyAmount
                )

                HorizontalDivider(
                    modifier =
                        Modifier.padding(vertical = 14.dp),
                    color = Color(0xFFECECEC)
                )

                DetailsRow(
                    icon = Icons.Default.CheckCircle,
                    title = "Status",
                    value = booking.status
                )
            }
        }

        Spacer(
            modifier = Modifier.height(24.dp)
        )

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

            Spacer(
                modifier = Modifier.width(8.dp)
            )

            Text(
                text = "Back to My Bookings"
            )
        }

        Spacer(
            modifier = Modifier.height(10.dp)
        )
    }
}

@Composable
private fun BookingDetailsErrorView(
    modifier: Modifier = Modifier,
    message: String,
    onBackClick: () -> Unit
) {
    Column(
        modifier = modifier
            .background(DetailsBackground)
            .padding(20.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Icon(
            imageVector = Icons.Default.ErrorOutline,
            contentDescription = null,
            tint = MaterialTheme.colorScheme.error,
            modifier = Modifier.size(46.dp)
        )

        Spacer(
            modifier = Modifier.height(12.dp)
        )

        Text(
            text = "Unable to load booking",
            fontSize = 18.sp,
            fontWeight = FontWeight.Bold,
            color = DetailsGreen
        )

        Spacer(
            modifier = Modifier.height(6.dp)
        )

        Text(
            text = message,
            fontSize = 13.sp,
            color = DetailsGray
        )

        Spacer(
            modifier = Modifier.height(20.dp)
        )

        OutlinedButton(
            onClick = onBackClick
        ) {
            Icon(
                imageVector = Icons.Default.ArrowBack,
                contentDescription = null
            )

            Spacer(
                modifier = Modifier.width(8.dp)
            )

            Text("Back to My Bookings")
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

        Spacer(
            modifier = Modifier.width(14.dp)
        )

        Column {
            Text(
                text = title,
                fontSize = 12.sp,
                color = DetailsGray
            )

            Spacer(
                modifier = Modifier.height(3.dp)
            )

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