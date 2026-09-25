package com.microgrid.app.ui.screens
import com.microgrid.app.data.BookingUiModel
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.microgrid.app.data.RetrofitClient
import kotlinx.coroutines.async
import kotlinx.coroutines.coroutineScope

private val HistoryGreen = Color(0xFF0B4F3C)
private val HistoryAccentGreen = Color(0xFF1E8754)
private val HistoryBackground = Color(0xFFF8F6F2)
private val HistoryGray = Color(0xFF7A7A7A)

@Composable
fun BookingHistoryScreen(
    nic: String,
    onBackClick: () -> Unit,
    onBookingClick: (String) -> Unit,
    onHomeClick: () -> Unit,
    onBookingsClick: () -> Unit,
    onProfileClick: () -> Unit
) {
    var searchText by remember { mutableStateOf("") }
    var selectedFilter by remember { mutableStateOf("All") }

    /*
     * Temporary UI data.
     *
     * These bookings are used only while the reservation API is not
     * implemented. Later this list should be replaced by booking history
     * retrieved from the Web API.
     */
    val historyBookings = remember {
        listOf(
            BookingUiModel(
                id = "BK-0901",
                stationName = "Colombo Solar Hub",
                location = "Colombo",
                date = "12 Sep 2026",
                time = "09:30 AM",
                energyAmount = "18 kWh",
                status = "Completed"
            ),
            BookingUiModel(
                id = "BK-0902",
                stationName = "Malabe Solar Hub",
                location = "Malabe",
                date = "08 Sep 2026",
                time = "02:00 PM",
                energyAmount = "25 kWh",
                status = "Completed"
            ),
            BookingUiModel(
                id = "BK-0903",
                stationName = "Kaduwela Energy Station",
                location = "Kaduwela",
                date = "02 Sep 2026",
                time = "11:00 AM",
                energyAmount = "15 kWh",
                status = "Cancelled"
            ),
            BookingUiModel(
                id = "BK-0904",
                stationName = "Negombo Solar Station",
                location = "Negombo",
                date = "28 Aug 2026",
                time = "03:30 PM",
                energyAmount = "20 kWh",
                status = "Completed"
            )
        )
    }

    val filteredBookings = historyBookings.filter { booking ->

        val matchesSearch =
            booking.id.contains(searchText, ignoreCase = true) ||
                    booking.stationName.contains(searchText, ignoreCase = true) ||
                    booking.location.contains(searchText, ignoreCase = true)

        val matchesFilter =
            selectedFilter == "All" ||
                    booking.status.equals(selectedFilter, ignoreCase = true)

        matchesSearch && matchesFilter
    }

    Scaffold(
        containerColor = HistoryBackground,

        bottomBar = {
            HistoryBottomBar(
                onHomeClick = onHomeClick,
                onBookingsClick = onBookingsClick,
                onProfileClick = onProfileClick
            )
        }

    ) { innerPadding ->

        LazyColumn(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
                .background(HistoryBackground),
            contentPadding = PaddingValues(
                start = 20.dp,
                end = 20.dp,
                top = 18.dp,
                bottom = 24.dp
            ),
            verticalArrangement = Arrangement.spacedBy(14.dp)
        ) {

            // Header
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
                            tint = HistoryGreen
                        )
                    }

                    Column {

                        Text(
                            text = "Booking History",
                            fontSize = 24.sp,
                            fontWeight = FontWeight.Bold,
                            color = HistoryGreen
                        )

                        Text(
                            text = "View your previous energy reservations",
                            fontSize = 12.sp,
                            color = HistoryGray
                        )
                    }
                }
            }

            // Search
            item {

                OutlinedTextField(
                    value = searchText,
                    onValueChange = {
                        searchText = it
                    },
                    modifier = Modifier.fillMaxWidth(),
                    placeholder = {
                        Text("Search booking, station or location")
                    },
                    leadingIcon = {
                        Icon(
                            imageVector = Icons.Default.Search,
                            contentDescription = "Search"
                        )
                    },
                    singleLine = true,
                    shape = RoundedCornerShape(12.dp),
                    colors = OutlinedTextFieldDefaults.colors(
                        focusedBorderColor = HistoryAccentGreen,
                        cursorColor = HistoryAccentGreen
                    )
                )
            }

            // Filters
            item {

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {

                    HistoryFilterButton(
                        title = "All",
                        selected = selectedFilter == "All",
                        modifier = Modifier.weight(1f)
                    ) {
                        selectedFilter = "All"
                    }

                    HistoryFilterButton(
                        title = "Completed",
                        selected = selectedFilter == "Completed",
                        modifier = Modifier.weight(1f)
                    ) {
                        selectedFilter = "Completed"
                    }

                    HistoryFilterButton(
                        title = "Cancelled",
                        selected = selectedFilter == "Cancelled",
                        modifier = Modifier.weight(1f)
                    ) {
                        selectedFilter = "Cancelled"
                    }
                }
            }

            // Result heading
            item {

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {

                    Text(
                        text = when (selectedFilter) {
                            "Completed" -> "Completed Bookings"
                            "Cancelled" -> "Cancelled Bookings"
                            else -> "Previous Bookings"
                        },
                        fontSize = 17.sp,
                        fontWeight = FontWeight.Bold,
                        color = HistoryGreen
                    )

                    Text(
                        text = "${filteredBookings.size} found",
                        fontSize = 12.sp,
                        color = HistoryGray
                    )
                }
            }

            if (filteredBookings.isEmpty()) {

                item {
                    EmptyHistoryView()
                }

            } else {

                items(
                    items = filteredBookings,
                    key = { it.id }
                ) { booking ->

                    HistoryBookingCard(
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

@Composable
private fun HistoryFilterButton(
    title: String,
    selected: Boolean,
    modifier: Modifier = Modifier,
    onClick: () -> Unit
) {

    if (selected) {

        Button(
            onClick = onClick,
            modifier = modifier,
            colors = ButtonDefaults.buttonColors(
                containerColor = HistoryGreen
            ),
            shape = RoundedCornerShape(20.dp),
            contentPadding = PaddingValues(
                horizontal = 4.dp,
                vertical = 8.dp
            )
        ) {

            Text(
                text = title,
                fontSize = 11.sp
            )
        }

    } else {

        OutlinedButton(
            onClick = onClick,
            modifier = modifier,
            shape = RoundedCornerShape(20.dp),
            contentPadding = PaddingValues(
                horizontal = 4.dp,
                vertical = 8.dp
            )
        ) {

            Text(
                text = title,
                fontSize = 11.sp,
                color = HistoryGreen
            )
        }
    }
}

@Composable
private fun HistoryBookingCard(
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
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.Top
            ) {

                Column(
                    modifier = Modifier.weight(1f)
                ) {

                    Text(
                        text = booking.stationName,
                        fontSize = 17.sp,
                        fontWeight = FontWeight.Bold,
                        color = HistoryGreen
                    )

                    Spacer(
                        modifier = Modifier.height(4.dp)
                    )

                    Text(
                        text = booking.id,
                        fontSize = 11.sp,
                        color = HistoryGray
                    )
                }

                HistoryStatusBadge(
                    status = booking.status
                )
            }

            Spacer(
                modifier = Modifier.height(16.dp)
            )

            HistoryInfoRow(
                icon = Icons.Default.LocationOn,
                text = booking.location
            )

            Spacer(
                modifier = Modifier.height(9.dp)
            )

            HistoryInfoRow(
                icon = Icons.Default.CalendarMonth,
                text = booking.date
            )

            Spacer(
                modifier = Modifier.height(9.dp)
            )

            HistoryInfoRow(
                icon = Icons.Default.Schedule,
                text = booking.time
            )

            Spacer(
                modifier = Modifier.height(9.dp)
            )

            HistoryInfoRow(
                icon = Icons.Default.Bolt,
                text = booking.energyAmount
            )

            Spacer(
                modifier = Modifier.height(15.dp)
            )

            HorizontalDivider(
                color = Color(0xFFEAEAEA)
            )

            Spacer(
                modifier = Modifier.height(12.dp)
            )

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.End,
                verticalAlignment = Alignment.CenterVertically
            ) {

                Text(
                    text = "View Details",
                    fontSize = 13.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = HistoryAccentGreen
                )

                Spacer(
                    modifier = Modifier.width(4.dp)
                )

                Icon(
                    imageVector = Icons.Default.ChevronRight,
                    contentDescription = "View booking details",
                    tint = HistoryAccentGreen,
                    modifier = Modifier.size(18.dp)
                )
            }
        }
    }
}

@Composable
private fun HistoryInfoRow(
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    text: String
) {

    Row(
        verticalAlignment = Alignment.CenterVertically
    ) {

        Icon(
            imageVector = icon,
            contentDescription = null,
            tint = HistoryAccentGreen,
            modifier = Modifier.size(18.dp)
        )

        Spacer(
            modifier = Modifier.width(10.dp)
        )

        Text(
            text = text,
            fontSize = 13.sp,
            color = Color(0xFF444444)
        )
    }
}

@Composable
private fun HistoryStatusBadge(
    status: String
) {

    val backgroundColor =
        when (status.lowercase()) {

            "completed" ->
                Color(0xFFE4F4EA)

            "cancelled" ->
                Color(0xFFFFE4E1)

            else ->
                Color(0xFFEDEDED)
        }

    val textColor =
        when (status.lowercase()) {

            "completed" ->
                Color(0xFF19733E)

            "cancelled" ->
                Color(0xFFB3261E)

            else ->
                Color.DarkGray
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
                vertical = 6.dp
            )
        )
    }
}

@Composable
private fun EmptyHistoryView() {

    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(top = 60.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {

        Box(
            modifier = Modifier
                .size(64.dp)
                .background(
                    Color(0xFFE5F2EB),
                    CircleShape
                ),
            contentAlignment = Alignment.Center
        ) {

            Icon(
                imageVector = Icons.Default.History,
                contentDescription = null,
                tint = HistoryAccentGreen,
                modifier = Modifier.size(30.dp)
            )
        }

        Spacer(
            modifier = Modifier.height(16.dp)
        )

        Text(
            text = "No booking history found",
            fontSize = 17.sp,
            fontWeight = FontWeight.Bold,
            color = HistoryGreen
        )

        Spacer(
            modifier = Modifier.height(6.dp)
        )

        Text(
            text = "Try changing your search or filter.",
            fontSize = 13.sp,
            color = HistoryGray
        )
    }
}

@Composable
private fun HistoryBottomBar(
    onHomeClick: () -> Unit,
    onBookingsClick: () -> Unit,
    onProfileClick: () -> Unit
) {

    NavigationBar(
        containerColor = Color.White
    ) {

        NavigationBarItem(
            selected = false,
            onClick = onHomeClick,
            icon = {
                Icon(
                    imageVector = Icons.Default.Home,
                    contentDescription = "Home"
                )
            },
            label = {
                Text("Home")
            }
        )

        NavigationBarItem(
            selected = false,
            onClick = onBookingsClick,
            icon = {
                Icon(
                    imageVector = Icons.Default.CalendarMonth,
                    contentDescription = "Bookings"
                )
            },
            label = {
                Text("Bookings")
            }
        )

        NavigationBarItem(
            selected = false,
            onClick = onProfileClick,
            icon = {
                Icon(
                    imageVector = Icons.Default.Person,
                    contentDescription = "Profile"
                )
            },
            label = {
                Text("Profile")
            }
        )
    }
}