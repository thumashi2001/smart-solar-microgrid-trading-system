package com.microgrid.app.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
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

private val DashboardGreen = Color(0xFF1E7A4D)
private val DashboardDarkGreen = Color(0xFF0B3B2E)
private val DashboardBackground = Color(0xFFF7F5F1)

@Composable
fun ProsumerDashboardScreen(
    fullName: String,
    nic: String,
    onProfileClick: () -> Unit,
    onBookingsClick: () -> Unit,
    onHistoryClick: () -> Unit,
    onSearchClick: () -> Unit
) {

    Scaffold(
        containerColor = DashboardBackground,
        bottomBar = {
            DashboardBottomBar(
                onProfileClick = onProfileClick,
                onBookingsClick = onBookingsClick
            )
        }
    ) { innerPadding ->

        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
                .padding(horizontal = 20.dp)
        ) {

            Spacer(modifier = Modifier.height(20.dp))

            // Header
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {

                Column {
                    Text(
                        text = "Smart Solar",
                        fontSize = 22.sp,
                        fontWeight = FontWeight.Bold,
                        color = DashboardDarkGreen
                    )

                    Text(
                        text = "Prosumer Dashboard",
                        fontSize = 13.sp,
                        color = Color.Gray
                    )
                }

                Box(
                    modifier = Modifier
                        .size(46.dp)
                        .background(DashboardGreen, CircleShape)
                        .clickable {
                            onProfileClick()
                        },
                    contentAlignment = Alignment.Center
                ) {
                    Icon(
                        imageVector = Icons.Default.Person,
                        contentDescription = "Profile",
                        tint = Color.White
                    )
                }
            }

            Spacer(modifier = Modifier.height(28.dp))

            // Welcome section
            Text(
                text = "Welcome back,",
                fontSize = 15.sp,
                color = Color.Gray
            )

            Text(
                text = fullName,
                fontSize = 27.sp,
                fontWeight = FontWeight.Bold,
                color = DashboardDarkGreen
            )

            Text(
                text = "NIC: $nic",
                fontSize = 13.sp,
                color = Color.Gray
            )

            Spacer(modifier = Modifier.height(24.dp))

            // Reservation summary
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(12.dp)
            ) {

                ReservationSummaryCard(
                    modifier = Modifier.weight(1f),
                    title = "Pending",
                    count = "0",
                    icon = Icons.Default.Schedule
                )

                ReservationSummaryCard(
                    modifier = Modifier.weight(1f),
                    title = "Approved",
                    count = "0",
                    icon = Icons.Default.CheckCircle
                )
            }

            Spacer(modifier = Modifier.height(24.dp))

            // Reserve Energy card
            Card(
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(18.dp),
                colors = CardDefaults.cardColors(
                    containerColor = DashboardDarkGreen
                )
            ) {

                Column(
                    modifier = Modifier.padding(20.dp)
                ) {

                    Row(
                        verticalAlignment = Alignment.CenterVertically
                    ) {

                        Box(
                            modifier = Modifier
                                .size(44.dp)
                                .background(
                                    Color.White.copy(alpha = 0.15f),
                                    CircleShape
                                ),
                            contentAlignment = Alignment.Center
                        ) {
                            Icon(
                                imageVector = Icons.Default.Bolt,
                                contentDescription = null,
                                tint = Color.White
                            )
                        }

                        Spacer(modifier = Modifier.width(14.dp))

                        Column {

                            Text(
                                text = "Reserve Energy",
                                color = Color.White,
                                fontWeight = FontWeight.Bold,
                                fontSize = 18.sp
                            )

                            Text(
                                text = "Find an available energy slot",
                                color = Color.White.copy(alpha = 0.75f),
                                fontSize = 13.sp
                            )
                        }
                    }

                    Spacer(modifier = Modifier.height(18.dp))

                    Button(
                        onClick = {
                            // Reservation screen will be connected later
                        },
                        modifier = Modifier.fillMaxWidth(),
                        colors = ButtonDefaults.buttonColors(
                            containerColor = Color.White,
                            contentColor = DashboardDarkGreen
                        ),
                        shape = RoundedCornerShape(10.dp)
                    ) {

                        Icon(
                            imageVector = Icons.Default.Search,
                            contentDescription = null
                        )

                        Spacer(modifier = Modifier.width(8.dp))

                        Text(
                            text = "Find Energy",
                            fontWeight = FontWeight.Bold
                        )
                    }
                }
            }

            Spacer(modifier = Modifier.height(26.dp))

            Text(
                text = "Quick Actions",
                fontSize = 18.sp,
                fontWeight = FontWeight.Bold,
                color = DashboardDarkGreen
            )

            Spacer(modifier = Modifier.height(14.dp))

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(12.dp)
            ) {

                QuickActionCard(
                    modifier = Modifier.weight(1f),
                    title = "My Bookings",
                    icon = Icons.Default.CalendarMonth,
                    onClick = onBookingsClick
                )

                QuickActionCard(
                    modifier = Modifier.weight(1f),
                    title = "History",
                    icon = Icons.Default.History,
                    onClick = onHistoryClick
                )
            }

            Spacer(modifier = Modifier.height(12.dp))

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(12.dp)
            ) {

                QuickActionCard(
                    modifier = Modifier.weight(1f),
                    title = "Search",
                    icon = Icons.Default.Search,
                    onClick = onSearchClick
                )

                QuickActionCard(
                    modifier = Modifier.weight(1f),
                    title = "Profile",
                    icon = Icons.Default.Person,
                    onClick = onProfileClick
                )
            }

            Spacer(modifier = Modifier.height(26.dp))

            Text(
                text = "Upcoming Reservation",
                fontSize = 18.sp,
                fontWeight = FontWeight.Bold,
                color = DashboardDarkGreen
            )

            Spacer(modifier = Modifier.height(12.dp))

            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(
                    containerColor = Color.White
                ),
                shape = RoundedCornerShape(16.dp),
                elevation = CardDefaults.cardElevation(
                    defaultElevation = 2.dp
                )
            ) {

                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(22.dp),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {

                    Icon(
                        imageVector = Icons.Default.EventAvailable,
                        contentDescription = null,
                        tint = DashboardGreen,
                        modifier = Modifier.size(36.dp)
                    )

                    Spacer(modifier = Modifier.height(10.dp))

                    Text(
                        text = "No upcoming reservation",
                        fontWeight = FontWeight.SemiBold,
                        color = DashboardDarkGreen
                    )

                    Spacer(modifier = Modifier.height(4.dp))

                    Text(
                        text = "Your approved reservation will appear here.",
                        fontSize = 12.sp,
                        color = Color.Gray
                    )
                }
            }
        }
    }
}

@Composable
private fun ReservationSummaryCard(
    modifier: Modifier,
    title: String,
    count: String,
    icon: ImageVector
) {

    Card(
        modifier = modifier,
        colors = CardDefaults.cardColors(
            containerColor = Color.White
        ),
        shape = RoundedCornerShape(16.dp),
        elevation = CardDefaults.cardElevation(
            defaultElevation = 2.dp
        )
    ) {

        Column(
            modifier = Modifier.padding(18.dp)
        ) {

            Icon(
                imageVector = icon,
                contentDescription = null,
                tint = DashboardGreen
            )

            Spacer(modifier = Modifier.height(12.dp))

            Text(
                text = count,
                fontSize = 26.sp,
                fontWeight = FontWeight.Bold,
                color = DashboardDarkGreen
            )

            Text(
                text = title,
                fontSize = 13.sp,
                color = Color.Gray
            )
        }
    }
}

@Composable
private fun QuickActionCard(
    modifier: Modifier,
    title: String,
    icon: ImageVector,
    onClick: () -> Unit
) {

    Card(
        modifier = modifier
            .height(100.dp)
            .clickable {
                onClick()
            },
        colors = CardDefaults.cardColors(
            containerColor = Color.White
        ),
        shape = RoundedCornerShape(16.dp),
        elevation = CardDefaults.cardElevation(
            defaultElevation = 2.dp
        )
    ) {

        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(16.dp),
            verticalArrangement = Arrangement.Center
        ) {

            Icon(
                imageVector = icon,
                contentDescription = null,
                tint = DashboardGreen
            )

            Spacer(modifier = Modifier.height(8.dp))

            Text(
                text = title,
                fontWeight = FontWeight.SemiBold,
                color = DashboardDarkGreen
            )
        }
    }
}

@Composable
private fun DashboardBottomBar(
    onProfileClick: () -> Unit,
    onBookingsClick: () -> Unit
) {

    NavigationBar(
        containerColor = Color.White
    ) {

        NavigationBarItem(
            selected = true,
            onClick = { },
            icon = {
                Icon(
                    Icons.Default.Home,
                    contentDescription = "Home"
                )
            },
            label = {
                Text("Home")
            },
            colors = NavigationBarItemDefaults.colors(
                selectedIconColor = DashboardGreen,
                selectedTextColor = DashboardGreen
            )
        )

        NavigationBarItem(
            selected = false,
            onClick = onBookingsClick,
            icon = {
                Icon(
                    Icons.Default.CalendarMonth,
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