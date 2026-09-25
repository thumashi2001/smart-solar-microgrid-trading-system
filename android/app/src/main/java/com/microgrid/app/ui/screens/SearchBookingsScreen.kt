package com.microgrid.app.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
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

private val SearchGreen = Color(0xFF07553F)
private val SearchAccentGreen = Color(0xFF148A61)
private val SearchBackground = Color(0xFFF8F7F3)
private val SearchSecondaryText = Color(0xFF777777)
private val SearchLightGreen = Color(0xFFE8F4EE)

@Composable
fun SearchBookingsScreen(
    nic: String,
    onBackClick: () -> Unit,
    onBookingClick: (String) -> Unit
) {
    var searchText by remember { mutableStateOf("") }
    var selectedFilter by remember { mutableStateOf("All") }

    var bookings by remember {
        mutableStateOf<List<BookingUiModel>>(emptyList())
    }

    var isLoading by remember {
        mutableStateOf(true)
    }

    var errorMessage by remember {
        mutableStateOf<String?>(null)
    }

    LaunchedEffect(nic) {
        if (nic.isBlank()) {
            errorMessage = "Unable to identify the logged-in prosumer."
            isLoading = false
            return@LaunchedEffect
        }

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

                bookings = reservations.map { reservation ->

                    val slot = slots.find {
                        it.slotId == reservation.slotId
                    }

                    val node = nodes.find {
                        it.nodeId == reservation.stationId
                    }

                    BookingUiModel(
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
                    "Unable to load bookings from the server."
            }

        } catch (e: Exception) {
            errorMessage =
                e.message ?: "Unable to connect to the server."

        } finally {
            isLoading = false
        }
    }

    val filteredBookings = bookings.filter { booking ->

        val query = searchText.trim()

        val matchesSearch =
            query.isBlank() ||
                    booking.id.contains(query, ignoreCase = true) ||
                    booking.stationName.contains(query, ignoreCase = true) ||
                    booking.location.contains(query, ignoreCase = true) ||
                    booking.date.contains(query, ignoreCase = true) ||
                    booking.status.contains(query, ignoreCase = true)

        val matchesFilter =
            selectedFilter == "All" ||
                    booking.status.equals(
                        selectedFilter,
                        ignoreCase = true
                    )

        matchesSearch && matchesFilter
    }

    Scaffold(
        containerColor = SearchBackground
    ) { innerPadding ->

        LazyColumn(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
                .padding(horizontal = 20.dp),
            contentPadding = PaddingValues(
                top = 18.dp,
                bottom = 30.dp
            ),
            verticalArrangement = Arrangement.spacedBy(14.dp)
        ) {

            item {
                Row(
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    IconButton(
                        onClick = onBackClick
                    ) {
                        Icon(
                            imageVector = Icons.Default.ArrowBack,
                            contentDescription = "Back",
                            tint = SearchGreen
                        )
                    }

                    Column {
                        Text(
                            text = "Search Bookings",
                            fontSize = 24.sp,
                            fontWeight = FontWeight.Bold,
                            color = SearchGreen
                        )

                        Text(
                            text = "Find your energy reservations",
                            fontSize = 12.sp,
                            color = SearchSecondaryText
                        )
                    }
                }
            }

            item {
                OutlinedTextField(
                    value = searchText,
                    onValueChange = {
                        searchText = it
                    },
                    modifier = Modifier.fillMaxWidth(),
                    placeholder = {
                        Text(
                            "Booking ID, station, location or date"
                        )
                    },
                    leadingIcon = {
                        Icon(
                            imageVector = Icons.Default.Search,
                            contentDescription = "Search"
                        )
                    },
                    trailingIcon = {
                        if (searchText.isNotEmpty()) {
                            IconButton(
                                onClick = {
                                    searchText = ""
                                }
                            ) {
                                Icon(
                                    imageVector = Icons.Default.Close,
                                    contentDescription = "Clear search"
                                )
                            }
                        }
                    },
                    singleLine = true,
                    shape = RoundedCornerShape(14.dp)
                )
            }

            item {
                Text(
                    text = "Filter by Status",
                    fontSize = 14.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = SearchGreen
                )
            }

            item {
                Column(
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement =
                            Arrangement.spacedBy(8.dp)
                    ) {
                        SearchFilterButton(
                            text = "All",
                            selected = selectedFilter == "All",
                            modifier = Modifier.weight(1f)
                        ) {
                            selectedFilter = "All"
                        }

                        SearchFilterButton(
                            text = "Pending",
                            selected = selectedFilter == "Pending",
                            modifier = Modifier.weight(1f)
                        ) {
                            selectedFilter = "Pending"
                        }

                        SearchFilterButton(
                            text = "Approved",
                            selected = selectedFilter == "Approved",
                            modifier = Modifier.weight(1f)
                        ) {
                            selectedFilter = "Approved"
                        }
                    }

                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement =
                            Arrangement.spacedBy(8.dp)
                    ) {
                        SearchFilterButton(
                            text = "Completed",
                            selected = selectedFilter == "Completed",
                            modifier = Modifier.weight(1f)
                        ) {
                            selectedFilter = "Completed"
                        }

                        SearchFilterButton(
                            text = "Cancelled",
                            selected = selectedFilter == "Cancelled",
                            modifier = Modifier.weight(1f)
                        ) {
                            selectedFilter = "Cancelled"
                        }
                    }
                }
            }

            item {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement =
                        Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = "Search Results",
                        fontSize = 16.sp,
                        fontWeight = FontWeight.Bold,
                        color = SearchGreen
                    )

                    if (!isLoading) {
                        Text(
                            text = "${filteredBookings.size} found",
                            fontSize = 12.sp,
                            color = SearchSecondaryText
                        )
                    }
                }
            }

            when {
                isLoading -> {
                    item {
                        Box(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(top = 60.dp),
                            contentAlignment = Alignment.Center
                        ) {
                            CircularProgressIndicator(
                                color = SearchAccentGreen
                            )
                        }
                    }
                }

                errorMessage != null -> {
                    item {
                        Card(
                            modifier = Modifier.fillMaxWidth(),
                            shape = RoundedCornerShape(14.dp),
                            colors = CardDefaults.cardColors(
                                containerColor = Color(0xFFFFEBEE)
                            )
                        ) {
                            Row(
                                modifier = Modifier.padding(16.dp),
                                verticalAlignment =
                                    Alignment.CenterVertically
                            ) {
                                Icon(
                                    imageVector =
                                        Icons.Default.ErrorOutline,
                                    contentDescription = null,
                                    tint = Color(0xFFB3261E)
                                )

                                Spacer(
                                    modifier = Modifier.width(10.dp)
                                )

                                Text(
                                    text = errorMessage
                                        ?: "Unable to load bookings.",
                                    color = Color(0xFFB3261E),
                                    fontSize = 13.sp
                                )
                            }
                        }
                    }
                }

                filteredBookings.isEmpty() -> {
                    item {
                        SearchEmptyView(
                            hasQuery =
                                searchText.isNotBlank() ||
                                        selectedFilter != "All"
                        )
                    }
                }

                else -> {
                    items(
                        items = filteredBookings,
                        key = { booking ->
                            booking.id
                        }
                    ) { booking ->

                        SearchBookingCard(
                            booking = booking,
                            onClick = {
                                onBookingClick(booking.id)
                            }
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun SearchFilterButton(
    text: String,
    selected: Boolean,
    modifier: Modifier = Modifier,
    onClick: () -> Unit
) {
    if (selected) {
        Button(
            onClick = onClick,
            modifier = modifier,
            shape = RoundedCornerShape(22.dp),
            contentPadding =
                PaddingValues(vertical = 9.dp),
            colors = ButtonDefaults.buttonColors(
                containerColor = SearchGreen
            )
        ) {
            Text(
                text = text,
                fontSize = 11.sp
            )
        }
    } else {
        OutlinedButton(
            onClick = onClick,
            modifier = modifier,
            shape = RoundedCornerShape(22.dp),
            contentPadding =
                PaddingValues(vertical = 9.dp)
        ) {
            Text(
                text = text,
                color = SearchGreen,
                fontSize = 11.sp
            )
        }
    }
}

@Composable
private fun SearchBookingCard(
    booking: BookingUiModel,
    onClick: () -> Unit
) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .clickable {
                onClick()
            },
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(
            containerColor = Color.White
        ),
        elevation = CardDefaults.cardElevation(
            defaultElevation = 2.dp
        )
    ) {
        Column(
            modifier = Modifier.padding(18.dp)
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
                        fontSize = 17.sp,
                        fontWeight = FontWeight.Bold,
                        color = SearchGreen
                    )

                    Spacer(
                        modifier = Modifier.height(4.dp)
                    )

                    Text(
                        text = booking.id,
                        fontSize = 11.sp,
                        color = SearchSecondaryText
                    )
                }

                SearchStatusBadge(
                    status = booking.status
                )
            }

            Spacer(
                modifier = Modifier.height(14.dp)
            )

            SearchInfoRow(
                icon = Icons.Default.LocationOn,
                text = booking.location
            )

            Spacer(
                modifier = Modifier.height(9.dp)
            )

            SearchInfoRow(
                icon = Icons.Default.CalendarMonth,
                text = booking.date
            )

            Spacer(
                modifier = Modifier.height(9.dp)
            )

            SearchInfoRow(
                icon = Icons.Default.Schedule,
                text = booking.time
            )

            Spacer(
                modifier = Modifier.height(9.dp)
            )

            SearchInfoRow(
                icon = Icons.Default.Bolt,
                text = booking.energyAmount
            )

            Spacer(
                modifier = Modifier.height(14.dp)
            )

            HorizontalDivider(
                color = Color(0xFFE6E6E6)
            )

            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(top = 12.dp),
                horizontalArrangement = Arrangement.End,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = "View Details",
                    color = SearchAccentGreen,
                    fontSize = 12.sp,
                    fontWeight = FontWeight.SemiBold
                )

                Spacer(
                    modifier = Modifier.width(4.dp)
                )

                Icon(
                    imageVector = Icons.Default.ChevronRight,
                    contentDescription = null,
                    tint = SearchAccentGreen,
                    modifier = Modifier.size(18.dp)
                )
            }
        }
    }
}

@Composable
private fun SearchInfoRow(
    icon: ImageVector,
    text: String
) {
    Row(
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(
            imageVector = icon,
            contentDescription = null,
            tint = SearchAccentGreen,
            modifier = Modifier.size(18.dp)
        )

        Spacer(
            modifier = Modifier.width(10.dp)
        )

        Text(
            text = text,
            fontSize = 13.sp,
            color = Color(0xFF555555)
        )
    }
}

@Composable
private fun SearchStatusBadge(
    status: String
) {
    val background =
        when (status.lowercase()) {
            "pending" -> Color(0xFFFFF3D6)
            "approved" -> Color(0xFFE4F4EA)
            "completed" -> Color(0xFFE5F3EC)
            "cancelled" -> Color(0xFFFFE5E5)
            else -> Color(0xFFEEEEEE)
        }

    val foreground =
        when (status.lowercase()) {
            "pending" -> Color(0xFF9A6700)
            "approved" -> Color(0xFF19733E)
            "completed" -> SearchAccentGreen
            "cancelled" -> Color(0xFFB3261E)
            else -> Color.DarkGray
        }

    Surface(
        color = background,
        shape = RoundedCornerShape(18.dp)
    ) {
        Text(
            text = status,
            modifier = Modifier.padding(
                horizontal = 12.dp,
                vertical = 7.dp
            ),
            color = foreground,
            fontSize = 11.sp,
            fontWeight = FontWeight.Medium
        )
    }
}

@Composable
private fun SearchEmptyView(
    hasQuery: Boolean
) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(
                top = 70.dp,
                bottom = 70.dp
            ),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Box(
            modifier = Modifier
                .size(72.dp)
                .background(
                    SearchLightGreen,
                    RoundedCornerShape(36.dp)
                ),
            contentAlignment = Alignment.Center
        ) {
            Icon(
                imageVector = Icons.Default.Search,
                contentDescription = null,
                tint = SearchAccentGreen,
                modifier = Modifier.size(34.dp)
            )
        }

        Spacer(
            modifier = Modifier.height(18.dp)
        )

        Text(
            text =
                if (hasQuery)
                    "No matching bookings"
                else
                    "No bookings found",
            fontSize = 18.sp,
            fontWeight = FontWeight.Bold,
            color = SearchGreen
        )

        Spacer(
            modifier = Modifier.height(7.dp)
        )

        Text(
            text =
                if (hasQuery)
                    "Try another search or change the status filter."
                else
                    "Your reservations will appear here.",
            fontSize = 13.sp,
            color = SearchSecondaryText
        )
    }
}