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
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.microgrid.app.data.BookingUiModel
import com.microgrid.app.data.RetrofitClient
import androidx.compose.ui.platform.LocalContext
import com.microgrid.app.data.toEntity
import com.microgrid.app.data.toUiModel
import com.microgrid.app.local.AppDatabase

private val HistoryGreen = Color(0xFF07553F)
private val HistoryAccentGreen = Color(0xFF148A61)
private val HistoryBackground = Color(0xFFF8F7F3)
private val HistoryLightGreen = Color(0xFFE8F4EE)
private val HistoryBorder = Color(0xFFE2E2DE)
private val HistoryTextSecondary = Color(0xFF777777)
private val HistoryPendingBackground = Color(0xFFFFF1CC)
private val HistoryPendingText = Color(0xFF9A6500)
private val HistoryCancelledBackground = Color(0xFFFFE5E5)
private val HistoryCancelledText = Color(0xFFB3261E)

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

    var bookings by remember {
        mutableStateOf<List<BookingUiModel>>(emptyList())
    }

    var isLoading by remember {
        mutableStateOf(true)
    }

    var errorMessage by remember {
        mutableStateOf<String?>(null)
    }
    val context = LocalContext.current

    val bookingDao = remember {
        AppDatabase.getDatabase(context).bookingDao()
    }

    /*
     * Load the logged-in prosumer's reservation history from the API.
     * Reservation, slot and microgrid node information are combined
     * to create the data required by the booking history UI.
     */
    LaunchedEffect(nic) {

        if (nic.isBlank()) {
            errorMessage = "Unable to identify the logged-in prosumer."
            isLoading = false
            return@LaunchedEffect
        }

        isLoading = true
        errorMessage = null

        /*
         * STEP 1:
         * Load locally cached booking history first.
         *
         * This allows the screen to continue working when
         * the Web API or network connection is unavailable.
         */
        try {

            val cachedBookings =
                bookingDao.getBookingsByProsumer(nic)

            bookings = cachedBookings
                .map { entity ->
                    entity.toUiModel()
                }
                .filter { booking ->

                    booking.status.equals(
                        "Completed",
                        ignoreCase = true
                    ) ||
                            booking.status.equals(
                                "Cancelled",
                                ignoreCase = true
                            )
                }

        } catch (e: Exception) {

            // A local-cache error should not prevent
            // the application from trying the Web API.
        }

        /*
         * STEP 2:
         * Try to refresh the local database using
         * the latest information from the Web API.
         */
        try {

            val reservationResponse =
                RetrofitClient.instance
                    .getReservationHistory(nic)

            val slotResponse =
                RetrofitClient.instance
                    .getSlots()

            val nodeResponse =
                RetrofitClient.instance
                    .getMicrogridNodes()

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
                 * Convert ALL reservations into BookingUiModel.
                 *
                 * We intentionally do not filter to Completed /
                 * Cancelled yet because the Room bookings table
                 * is shared by:
                 *
                 * - My Bookings
                 * - Booking History
                 * - Search
                 * - Booking Details
                 */
                val latestBookings =
                    reservations.map { reservation ->

                        val slot =
                            slots.find { currentSlot ->
                                currentSlot.slotId ==
                                        reservation.slotId
                            }

                        val node =
                            nodes.find { currentNode ->
                                currentNode.nodeId ==
                                        reservation.stationId
                            }

                        BookingUiModel(

                            id =
                                reservation.reservationId,

                            stationName =
                                node?.nodeName
                                    ?: reservation.stationId,

                            location =
                                node?.location
                                    ?: "Unknown location",

                            date =
                                slot?.date
                                    ?.substringBefore("T")
                                    ?: "Date unavailable",

                            time =
                                if (slot != null) {

                                    "${slot.startTime} - ${slot.endTime}"

                                } else {

                                    "Time unavailable"
                                },

                            /*
                             * ReservationResponse currently does not
                             * contain a separate reserved-energy value,
                             * so the existing UI uses slot capacity.
                             */
                            energyAmount =
                                if (slot != null) {

                                    "${slot.capacity} kWh"

                                } else {

                                    "Capacity unavailable"
                                },

                            status =
                                reservation.status
                        )
                    }

                /*
                 * STEP 3:
                 * Convert the API results into Room entities.
                 *
                 * Preserve the real stationId, slotId and
                 * updatedAt values from each reservation.
                 */
                val bookingEntities =
                    reservations.map { reservation ->

                        val slot =
                            slots.find { currentSlot ->
                                currentSlot.slotId ==
                                        reservation.slotId
                            }

                        val node =
                            nodes.find { currentNode ->
                                currentNode.nodeId ==
                                        reservation.stationId
                            }

                        val uiModel =
                            BookingUiModel(

                                id =
                                    reservation.reservationId,

                                stationName =
                                    node?.nodeName
                                        ?: reservation.stationId,

                                location =
                                    node?.location
                                        ?: "Unknown location",

                                date =
                                    slot?.date
                                        ?.substringBefore("T")
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

                                status =
                                    reservation.status
                            )

                        uiModel.toEntity(
                            prosumerNic = nic,
                            stationId =
                                reservation.stationId,
                            slotId =
                                reservation.slotId,
                            updatedAt =
                                reservation.updatedAt
                        )
                    }

                /*
                 * STEP 4:
                 * Save/update API records in SQLite.
                 *
                 * BookingDao uses REPLACE on conflict and
                 * reservationId is the primary key.
                 *
                 * Therefore:
                 *
                 * Pending -> Approved
                 * Pending -> Cancelled
                 * Approved -> Completed
                 *
                 * will update the existing local record.
                 */
                if (bookingEntities.isNotEmpty()) {

                    bookingDao.insertBookings(
                        bookingEntities
                    )
                }

                /*
                 * STEP 5:
                 * History displays only reservations that
                 * are no longer current.
                 */
                bookings =
                    latestBookings.filter { booking ->

                        booking.status.equals(
                            "Completed",
                            ignoreCase = true
                        ) ||
                                booking.status.equals(
                                    "Cancelled",
                                    ignoreCase = true
                                )
                    }

                errorMessage = null

            } else {

                /*
                 * The server responded, but one or more
                 * requests were unsuccessful.
                 *
                 * If cached history exists, keep showing it.
                 */
                if (bookings.isEmpty()) {

                    errorMessage =
                        """
                    Unable to refresh booking history.

                    Reservation API: ${reservationResponse.code()}
                    Slots API: ${slotResponse.code()}
                    Nodes API: ${nodeResponse.code()}
                    """.trimIndent()
                }
            }

        } catch (e: Exception) {

            /*
             * Network/API failure:
             *
             * Try Room once more in case the cache was
             * populated previously.
             */
            try {

                val cachedBookings =
                    bookingDao.getBookingsByProsumer(nic)

                val cachedHistory =
                    cachedBookings
                        .map { entity ->
                            entity.toUiModel()
                        }
                        .filter { booking ->

                            booking.status.equals(
                                "Completed",
                                ignoreCase = true
                            ) ||
                                    booking.status.equals(
                                        "Cancelled",
                                        ignoreCase = true
                                    )
                        }

                if (cachedHistory.isNotEmpty()) {

                    bookings = cachedHistory
                    errorMessage = null

                } else {

                    /*
                     * An empty history can also be legitimate.
                     *
                     * If local bookings exist but none are
                     * Completed/Cancelled, show the normal
                     * empty-history screen instead of treating
                     * that as an error.
                     */
                    if (cachedBookings.isNotEmpty()) {

                        bookings = emptyList()
                        errorMessage = null

                    } else {

                        errorMessage =
                            "Unable to connect to the server and no cached booking history is available."
                    }
                }

            } catch (cacheException: Exception) {

                errorMessage =
                    e.message
                        ?: "Unable to connect to the server."
            }

        } finally {

            isLoading = false
        }
    }

    /*
     * Apply the selected status filter and search query.
     */
    val filteredBookings = bookings.filter { booking ->

        val matchesSearch =
            searchText.isBlank() ||
                    booking.id.contains(
                        searchText,
                        ignoreCase = true
                    ) ||
                    booking.stationName.contains(
                        searchText,
                        ignoreCase = true
                    ) ||
                    booking.location.contains(
                        searchText,
                        ignoreCase = true
                    )

        val matchesStatus =
            selectedFilter == "All" ||
                    booking.status.equals(
                        selectedFilter,
                        ignoreCase = true
                    )

        matchesSearch && matchesStatus
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
                .padding(horizontal = 20.dp),
            contentPadding = PaddingValues(
                top = 18.dp,
                bottom = 24.dp
            ),
            verticalArrangement = Arrangement.spacedBy(14.dp)
        ) {

            /*
             * Page header
             */
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
                            text = "View your previous reservations",
                            fontSize = 12.sp,
                            color = HistoryTextSecondary
                        )
                    }
                }
            }

            /*
             * Search field
             */
            item {

                OutlinedTextField(
                    value = searchText,
                    onValueChange = {
                        searchText = it
                    },
                    modifier = Modifier.fillMaxWidth(),
                    placeholder = {
                        Text(
                            "Search booking, station or location"
                        )
                    },
                    leadingIcon = {

                        Icon(
                            imageVector = Icons.Default.Search,
                            contentDescription = "Search"
                        )
                    },
                    singleLine = true,
                    shape = RoundedCornerShape(14.dp)
                )
            }

            /*
             * Status filter buttons
             */
            item {

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement =
                        Arrangement.spacedBy(8.dp)
                ) {

                    HistoryFilterButton(
                        text = "All",
                        selected =
                            selectedFilter == "All",
                        modifier =
                            Modifier.weight(1f)
                    ) {
                        selectedFilter = "All"
                    }

                    HistoryFilterButton(
                        text = "Completed",
                        selected =
                            selectedFilter == "Completed",
                        modifier =
                            Modifier.weight(1f)
                    ) {
                        selectedFilter = "Completed"
                    }

                    HistoryFilterButton(
                        text = "Cancelled",
                        selected =
                            selectedFilter == "Cancelled",
                        modifier =
                            Modifier.weight(1f)
                    ) {
                        selectedFilter = "Cancelled"
                    }
                }
            }

            /*
             * Section title
             */
            item {

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    verticalAlignment =
                        Alignment.CenterVertically,
                    horizontalArrangement =
                        Arrangement.SpaceBetween
                ) {

                    Text(
                        text = "Past Bookings",
                        fontSize = 16.sp,
                        fontWeight = FontWeight.Bold,
                        color = HistoryGreen
                    )

                    if (!isLoading) {

                        Text(
                            text =
                                "${filteredBookings.size} found",
                            fontSize = 12.sp,
                            color = HistoryTextSecondary
                        )
                    }
                }
            }

            /*
             * Loading / Error / Empty / Data states
             */
            when {

                isLoading -> {

                    item {

                        Box(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(top = 60.dp),
                            contentAlignment =
                                Alignment.Center
                        ) {

                            CircularProgressIndicator(
                                color = HistoryAccentGreen
                            )
                        }
                    }
                }

                errorMessage != null -> {

                    item {

                        HistoryErrorCard(
                            message =
                                errorMessage
                                    ?: "Unable to load history."
                        )
                    }
                }

                filteredBookings.isEmpty() -> {

                    item {

                        EmptyHistoryView(
                            hasSearchOrFilter =
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

                        HistoryBookingCard(
                            booking = booking,
                            onClick = {
                                onBookingClick(
                                    booking.id
                                )
                            }
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun HistoryFilterButton(
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
            colors = ButtonDefaults.buttonColors(
                containerColor = HistoryGreen
            ),
            contentPadding =
                PaddingValues(vertical = 10.dp)
        ) {

            Text(
                text = text,
                fontSize = 12.sp
            )
        }

    } else {

        OutlinedButton(
            onClick = onClick,
            modifier = modifier,
            shape = RoundedCornerShape(22.dp),
            contentPadding =
                PaddingValues(vertical = 10.dp)
        ) {

            Text(
                text = text,
                fontSize = 12.sp,
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
                verticalAlignment =
                    Alignment.Top,
                horizontalArrangement =
                    Arrangement.SpaceBetween
            ) {

                Column(
                    modifier =
                        Modifier.weight(1f)
                ) {

                    Text(
                        text = booking.stationName,
                        fontSize = 17.sp,
                        fontWeight = FontWeight.Bold,
                        color = HistoryGreen
                    )

                    Spacer(
                        modifier =
                            Modifier.height(4.dp)
                    )

                    Text(
                        text = booking.id,
                        fontSize = 11.sp,
                        color = HistoryTextSecondary
                    )
                }

                HistoryStatusBadge(
                    status = booking.status
                )
            }

            Spacer(
                modifier =
                    Modifier.height(14.dp)
            )

            HistoryInformationRow(
                icon = Icons.Default.LocationOn,
                text = booking.location
            )

            Spacer(
                modifier =
                    Modifier.height(10.dp)
            )

            HistoryInformationRow(
                icon = Icons.Default.CalendarMonth,
                text = booking.date
            )

            Spacer(
                modifier =
                    Modifier.height(10.dp)
            )

            HistoryInformationRow(
                icon = Icons.Default.Schedule,
                text = booking.time
            )

            Spacer(
                modifier =
                    Modifier.height(10.dp)
            )

            HistoryInformationRow(
                icon = Icons.Default.Bolt,
                text = booking.energyAmount
            )

            Spacer(
                modifier =
                    Modifier.height(14.dp)
            )

            HorizontalDivider(
                color = HistoryBorder
            )

            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .clickable {
                        onClick()
                    }
                    .padding(top = 13.dp),
                horizontalArrangement =
                    Arrangement.End,
                verticalAlignment =
                    Alignment.CenterVertically
            ) {

                Text(
                    text = "View Details",
                    color = HistoryAccentGreen,
                    fontSize = 12.sp,
                    fontWeight = FontWeight.SemiBold
                )

                Spacer(
                    modifier =
                        Modifier.width(4.dp)
                )

                Icon(
                    imageVector =
                        Icons.Default.ChevronRight,
                    contentDescription =
                        "View booking details",
                    tint = HistoryAccentGreen,
                    modifier =
                        Modifier.size(18.dp)
                )
            }
        }
    }
}

@Composable
private fun HistoryInformationRow(
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    text: String
) {

    Row(
        verticalAlignment =
            Alignment.CenterVertically
    ) {

        Icon(
            imageVector = icon,
            contentDescription = null,
            tint = HistoryAccentGreen,
            modifier =
                Modifier.size(18.dp)
        )

        Spacer(
            modifier =
                Modifier.width(10.dp)
        )

        Text(
            text = text,
            fontSize = 13.sp,
            color = Color(0xFF555555)
        )
    }
}

@Composable
private fun HistoryStatusBadge(
    status: String
) {

    val backgroundColor =
        when {

            status.equals(
                "Completed",
                ignoreCase = true
            ) -> HistoryLightGreen

            status.equals(
                "Cancelled",
                ignoreCase = true
            ) -> HistoryCancelledBackground

            status.equals(
                "Pending",
                ignoreCase = true
            ) -> HistoryPendingBackground

            else -> Color(0xFFEEEEEE)
        }

    val textColor =
        when {

            status.equals(
                "Completed",
                ignoreCase = true
            ) -> HistoryAccentGreen

            status.equals(
                "Cancelled",
                ignoreCase = true
            ) -> HistoryCancelledText

            status.equals(
                "Pending",
                ignoreCase = true
            ) -> HistoryPendingText

            else -> Color.DarkGray
        }

    Surface(
        color = backgroundColor,
        shape = RoundedCornerShape(18.dp)
    ) {

        Text(
            text = status,
            modifier = Modifier.padding(
                horizontal = 12.dp,
                vertical = 7.dp
            ),
            color = textColor,
            fontSize = 11.sp,
            fontWeight = FontWeight.Medium
        )
    }
}

@Composable
private fun EmptyHistoryView(
    hasSearchOrFilter: Boolean
) {

    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(
                top = 70.dp,
                bottom = 70.dp
            ),
        horizontalAlignment =
            Alignment.CenterHorizontally
    ) {

        Box(
            modifier = Modifier
                .size(72.dp)
                .background(
                    color = HistoryLightGreen,
                    shape =
                        RoundedCornerShape(36.dp)
                ),
            contentAlignment =
                Alignment.Center
        ) {

            Icon(
                imageVector = Icons.Default.History,
                contentDescription = null,
                tint = HistoryAccentGreen,
                modifier =
                    Modifier.size(34.dp)
            )
        }

        Spacer(
            modifier =
                Modifier.height(18.dp)
        )

        Text(
            text =
                if (hasSearchOrFilter)
                    "No matching bookings"
                else
                    "No booking history yet",
            fontSize = 18.sp,
            fontWeight = FontWeight.Bold,
            color = HistoryGreen
        )

        Spacer(
            modifier =
                Modifier.height(7.dp)
        )

        Text(
            text =
                if (hasSearchOrFilter)
                    "Try changing your search or filter."
                else
                    "Completed and cancelled bookings will appear here.",
            fontSize = 13.sp,
            color = HistoryTextSecondary
        )
    }
}

@Composable
private fun HistoryErrorCard(
    message: String
) {

    Card(
        modifier =
            Modifier.fillMaxWidth(),
        shape =
            RoundedCornerShape(14.dp),
        colors =
            CardDefaults.cardColors(
                containerColor =
                    Color(0xFFFFEBEE)
            )
    ) {

        Row(
            modifier =
                Modifier.padding(16.dp),
            verticalAlignment =
                Alignment.CenterVertically
        ) {

            Icon(
                imageVector =
                    Icons.Default.ErrorOutline,
                contentDescription = null,
                tint = HistoryCancelledText
            )

            Spacer(
                modifier =
                    Modifier.width(10.dp)
            )

            Text(
                text = message,
                color = HistoryCancelledText,
                fontSize = 13.sp
            )
        }
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
                    imageVector =
                        Icons.Default.Home,
                    contentDescription = "Home"
                )
            },
            label = {
                Text("Home")
            }
        )

        NavigationBarItem(
            selected = true,
            onClick = onBookingsClick,
            icon = {

                Icon(
                    imageVector =
                        Icons.Default.CalendarMonth,
                    contentDescription = "Bookings"
                )
            },
            label = {
                Text("Bookings")
            },
            colors =
                NavigationBarItemDefaults.colors(
                    selectedIconColor =
                        HistoryAccentGreen,
                    selectedTextColor =
                        HistoryAccentGreen
                )
        )

        NavigationBarItem(
            selected = false,
            onClick = onProfileClick,
            icon = {

                Icon(
                    imageVector =
                        Icons.Default.Person,
                    contentDescription = "Profile"
                )
            },
            label = {
                Text("Profile")
            }
        )
    }
}