package com.microgrid.app.ui.screens

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
import com.microgrid.app.data.BookingUiModel
import com.microgrid.app.data.RetrofitClient
import androidx.compose.ui.platform.LocalContext
import com.microgrid.app.data.toEntity
import com.microgrid.app.data.toUiModel
import com.microgrid.app.local.AppDatabase

private val BookingGreen = Color(0xFF0B4F3C)
private val BookingAccentGreen = Color(0xFF1E8754)
private val BookingBackground = Color(0xFFF8F6F2)
private val BookingGray = Color(0xFF7A7A7A)



@Composable
fun BookingsScreen(
    nic: String,
    onBackClick: () -> Unit,
    onBookingClick: (String) -> Unit,
    onHomeClick: () -> Unit,
    onProfileClick: () -> Unit
) {
    var searchText by remember { mutableStateOf("") }
    var selectedFilter by remember { mutableStateOf("All") }

    var bookings by remember { mutableStateOf<List<BookingUiModel>>(emptyList()) }
    var isLoading by remember { mutableStateOf(true) }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    val context = LocalContext.current
    val database = remember {
        AppDatabase.getDatabase(context)
    }
    val bookingDao = remember {
        database.bookingDao()
    }

    LaunchedEffect(nic) {

        if (nic.isBlank()) {
            errorMessage = "Unable to identify the logged-in prosumer."
            isLoading = false
            return@LaunchedEffect
        }

        isLoading = true
        errorMessage = null

        // First load locally cached bookings.
        // This means previously synchronized data can be displayed
        // even if the API is temporarily unavailable.
        try {
            val cachedBookings =
                bookingDao.getBookingsByProsumer(nic)

            if (cachedBookings.isNotEmpty()) {
                bookings = cachedBookings
                    .map { it.toUiModel() }
                    .filter {
                        it.status.equals("Pending", ignoreCase = true) ||
                                it.status.equals("Approved", ignoreCase = true)
                    }
            }
        } catch (e: Exception) {
            // Cache failure should not stop the API request.
        }

        try {

            // Request the latest reservation information
            // from the central Web API.
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

                /*
                 * Combine reservation, slot and microgrid-node
                 * information into the model used by the UI.
                 */
                val latestBookings =
                    reservations.map { reservation ->

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

                /*
                 * Replace this prosumer's cached records with the
                 * latest data returned by the server.
                 */
                bookingDao.deleteBookingsByProsumer(nic)

                val entities =
                    reservations.map { reservation ->

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
                        ).toEntity(
                            prosumerNic = nic,
                            stationId = reservation.stationId,
                            slotId = reservation.slotId,
                            updatedAt = reservation.updatedAt
                        )
                    }

                if (entities.isNotEmpty()) {
                    bookingDao.insertBookings(entities)
                }

                /*
                 * My Bookings only displays current Pending
                 * and Approved reservations.
                 */
                bookings =
                    latestBookings.filter {
                        it.status.equals(
                            "Pending",
                            ignoreCase = true
                        ) ||
                                it.status.equals(
                                    "Approved",
                                    ignoreCase = true
                                )
                    }

                errorMessage = null

            } else {

                /*
                 * If the API fails but cached records exist,
                 * continue displaying the local data.
                 */
                if (bookings.isEmpty()) {

                    errorMessage =
                        """
                    Unable to load bookings.

                    Reservation API: ${reservationResponse.code()}
                    Slots API: ${slotResponse.code()}
                    Nodes API: ${nodeResponse.code()}
                    """.trimIndent()
                }
            }

        } catch (e: Exception) {

            /*
             * A network failure should not remove data that was
             * already loaded from SQLite.
             */
            if (bookings.isEmpty()) {

                errorMessage =
                    "Unable to connect to the server and no cached bookings are available."
            }

        } finally {

            isLoading = false
        }
    }

    val filteredBookings = bookings.filter { booking ->

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
        containerColor = BookingBackground,

        bottomBar = {
            BookingsBottomBar(
                onHomeClick = onHomeClick,
                onProfileClick = onProfileClick
            )
        }

    ) { innerPadding ->

        LazyColumn(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
                .background(BookingBackground),
            contentPadding = PaddingValues(
                start = 20.dp,
                end = 20.dp,
                top = 18.dp,
                bottom = 24.dp
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
                            tint = BookingGreen
                        )
                    }

                    Column {

                        Text(
                            text = "My Bookings",
                            fontSize = 24.sp,
                            fontWeight = FontWeight.Bold,
                            color = BookingGreen
                        )

                        Text(
                            text = "View and manage your energy reservations",
                            fontSize = 12.sp,
                            color = BookingGray
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
                        focusedBorderColor = BookingAccentGreen,
                        cursorColor = BookingAccentGreen
                    )
                )
            }

            item {

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {

                    BookingFilterButton(
                        title = "All",
                        selected = selectedFilter == "All",
                        modifier = Modifier.weight(1f)
                    ) {
                        selectedFilter = "All"
                    }

                    BookingFilterButton(
                        title = "Pending",
                        selected = selectedFilter == "Pending",
                        modifier = Modifier.weight(1f)
                    ) {
                        selectedFilter = "Pending"
                    }

                    BookingFilterButton(
                        title = "Approved",
                        selected = selectedFilter == "Approved",
                        modifier = Modifier.weight(1f)
                    ) {
                        selectedFilter = "Approved"
                    }
                }
            }

            item {

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {

                    Text(
                        text = when (selectedFilter) {
                            "Pending" -> "Pending Bookings"
                            "Approved" -> "Approved Bookings"
                            else -> "Current Bookings"
                        },
                        fontSize = 17.sp,
                        fontWeight = FontWeight.Bold,
                        color = BookingGreen
                    )

                    Text(
                        text = "${filteredBookings.size} found",
                        fontSize = 12.sp,
                        color = BookingGray
                    )
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
                                color = BookingAccentGreen
                            )
                        }
                    }
                }

                errorMessage != null -> {

                    item {

                        Card(
                            modifier = Modifier.fillMaxWidth(),
                            colors = CardDefaults.cardColors(
                                containerColor = Color(0xFFFFE9E7)
                            ),
                            shape = RoundedCornerShape(12.dp)
                        ) {

                            Column(
                                modifier = Modifier.padding(16.dp)
                            ) {

                                Text(
                                    text = "Unable to load bookings",
                                    fontWeight = FontWeight.Bold,
                                    color = Color(0xFFB3261E)
                                )

                                Spacer(
                                    modifier = Modifier.height(6.dp)
                                )

                                Text(
                                    text = errorMessage ?: "Unknown error",
                                    fontSize = 13.sp,
                                    color = Color(0xFFB3261E)
                                )
                            }
                        }
                    }
                }

                filteredBookings.isEmpty() -> {

                    item {
                        EmptyBookingsView()
                    }
                }

                else -> {

                    items(
                        items = filteredBookings,
                        key = { it.id }
                    ) { booking ->

                        BookingCard(
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
private fun BookingFilterButton(
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
                containerColor = BookingGreen
            ),
            shape = RoundedCornerShape(20.dp),
            contentPadding = PaddingValues(
                horizontal = 6.dp,
                vertical = 8.dp
            )
        ) {

            Text(
                text = title,
                fontSize = 12.sp
            )
        }

    } else {

        OutlinedButton(
            onClick = onClick,
            modifier = modifier,
            shape = RoundedCornerShape(20.dp),
            contentPadding = PaddingValues(
                horizontal = 6.dp,
                vertical = 8.dp
            )
        ) {

            Text(
                text = title,
                fontSize = 12.sp,
                color = BookingGreen
            )
        }
    }
}

@Composable
private fun BookingCard(
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
                        color = BookingGreen
                    )

                    Spacer(
                        modifier = Modifier.height(4.dp)
                    )

                    Text(
                        text = booking.id,
                        fontSize = 11.sp,
                        color = BookingGray
                    )
                }

                BookingStatusBadge(
                    status = booking.status
                )
            }

            Spacer(
                modifier = Modifier.height(16.dp)
            )

            BookingInfoRow(
                icon = Icons.Default.LocationOn,
                text = booking.location
            )

            Spacer(
                modifier = Modifier.height(9.dp)
            )

            BookingInfoRow(
                icon = Icons.Default.CalendarMonth,
                text = booking.date
            )

            Spacer(
                modifier = Modifier.height(9.dp)
            )

            BookingInfoRow(
                icon = Icons.Default.Schedule,
                text = booking.time
            )

            Spacer(
                modifier = Modifier.height(9.dp)
            )

            BookingInfoRow(
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
                    color = BookingAccentGreen
                )

                Spacer(
                    modifier = Modifier.width(4.dp)
                )

                Icon(
                    imageVector = Icons.Default.ChevronRight,
                    contentDescription = "View booking",
                    tint = BookingAccentGreen,
                    modifier = Modifier.size(18.dp)
                )
            }
        }
    }
}

@Composable
private fun BookingInfoRow(
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    text: String
) {

    Row(
        verticalAlignment = Alignment.CenterVertically
    ) {

        Icon(
            imageVector = icon,
            contentDescription = null,
            tint = BookingAccentGreen,
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
private fun BookingStatusBadge(
    status: String
) {

    val backgroundColor =
        when (status.lowercase()) {

            "approved" ->
                Color(0xFFE4F4EA)

            "pending" ->
                Color(0xFFFFF3D6)

            "cancelled" ->
                Color(0xFFFFE4E1)

            else ->
                Color(0xFFEDEDED)
        }

    val textColor =
        when (status.lowercase()) {

            "approved" ->
                Color(0xFF19733E)

            "pending" ->
                Color(0xFF9A6700)

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
private fun EmptyBookingsView() {

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
                imageVector = Icons.Default.CalendarMonth,
                contentDescription = null,
                tint = BookingAccentGreen,
                modifier = Modifier.size(30.dp)
            )
        }

        Spacer(
            modifier = Modifier.height(16.dp)
        )

        Text(
            text = "No bookings found",
            fontSize = 17.sp,
            fontWeight = FontWeight.Bold,
            color = BookingGreen
        )

        Spacer(
            modifier = Modifier.height(6.dp)
        )

        Text(
            text = "Try changing your search or filter.",
            fontSize = 13.sp,
            color = BookingGray
        )
    }
}

@Composable
private fun BookingsBottomBar(
    onHomeClick: () -> Unit,
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
            selected = true,
            onClick = {},
            icon = {

                Icon(
                    imageVector = Icons.Default.CalendarMonth,
                    contentDescription = "Bookings"
                )
            },
            label = {
                Text("Bookings")
            },
            colors = NavigationBarItemDefaults.colors(
                selectedIconColor = BookingAccentGreen,
                selectedTextColor = BookingAccentGreen
            )
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