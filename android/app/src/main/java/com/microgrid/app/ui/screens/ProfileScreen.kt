package com.microgrid.app.ui.screens

import android.net.Uri
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import coil.compose.rememberAsyncImagePainter

@Composable
fun ProfileScreen(
    fullName: String,
    nic: String,
    onMyProfile: () -> Unit,
    onChangePassword: () -> Unit,
    onNotifications: () -> Unit,
    onHelpSupport: () -> Unit,
    onAbout: () -> Unit,
    // ── Component 2 entry points ────────────────────────────────────────
    onMyBookings: () -> Unit,
    onBookSlot: () -> Unit,
    // ── Auth ─────────────────────────────────────────────────────────────
    onLogout: () -> Unit
) {
    var photoUri by remember { mutableStateOf<Uri?>(null) }
    val launcher = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.GetContent()
    ) { uri: Uri? -> photoUri = uri }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(PageBg)
            .verticalScroll(rememberScrollState())
            .padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Spacer(modifier = Modifier.height(24.dp))

        // ── Avatar ─────────────────────────────────────────────────────
        Box(
            modifier = Modifier
                .size(80.dp)
                .clip(CircleShape)
                .background(Color(0xFFD8D4CB))
                .clickable { launcher.launch("image/*") },
            contentAlignment = Alignment.Center
        ) {
            if (photoUri != null) {
                Image(
                    painter = rememberAsyncImagePainter(photoUri),
                    contentDescription = "Profile photo",
                    modifier = Modifier.fillMaxSize(),
                    contentScale = ContentScale.Crop
                )
            } else {
                Icon(Icons.Filled.Person, contentDescription = null, tint = Color.White, modifier = Modifier.size(40.dp))
            }
        }
        Spacer(modifier = Modifier.height(6.dp))
        Text("Tap to change photo", fontSize = 11.sp, color = Color.Gray)
        Spacer(modifier = Modifier.height(8.dp))
        Text(fullName, fontSize = 20.sp, fontWeight = FontWeight.Bold, color = DarkGreen)
        Text(nic, fontSize = 13.sp, color = Color.Gray)

        Spacer(modifier = Modifier.height(24.dp))

        // ── Energy Booking section ─────────────────────────────────────
        Text(
            "⚡ Energy Booking",
            fontSize = 12.sp,
            fontWeight = FontWeight.Bold,
            color = AccentGreen,
            modifier = Modifier
                .align(Alignment.Start)
                .padding(bottom = 6.dp)
        )
        Card(
            shape = RoundedCornerShape(16.dp),
            colors = CardDefaults.cardColors(containerColor = Color.White),
            elevation = CardDefaults.cardElevation(defaultElevation = 4.dp),
            modifier = Modifier.fillMaxWidth()
        ) {
            Column {
                ProfileMenuItem(Icons.Filled.BoltSharp, "Book Energy Slot", onBookSlot)
                HorizontalDivider(modifier = Modifier.padding(horizontal = 20.dp))
                ProfileMenuItem(Icons.Filled.List, "My Bookings", onMyBookings)
            }
        }

        Spacer(modifier = Modifier.height(16.dp))

        // ── Account section ────────────────────────────────────────────
        Text(
            "👤 Account",
            fontSize = 12.sp,
            fontWeight = FontWeight.Bold,
            color = Color.Gray,
            modifier = Modifier
                .align(Alignment.Start)
                .padding(bottom = 6.dp)
        )
        Card(
            shape = RoundedCornerShape(16.dp),
            colors = CardDefaults.cardColors(containerColor = Color.White),
            elevation = CardDefaults.cardElevation(defaultElevation = 4.dp),
            modifier = Modifier.fillMaxWidth()
        ) {
            Column {
                ProfileMenuItem(Icons.Filled.Person, "My Profile", onMyProfile)
                HorizontalDivider(modifier = Modifier.padding(horizontal = 20.dp))
                ProfileMenuItem(Icons.Filled.Lock, "Change Password", onChangePassword)
                HorizontalDivider(modifier = Modifier.padding(horizontal = 20.dp))
                ProfileMenuItem(Icons.Filled.Notifications, "Notifications", onNotifications)
                HorizontalDivider(modifier = Modifier.padding(horizontal = 20.dp))
                ProfileMenuItem(Icons.Filled.Info, "Help & Support", onHelpSupport)
                HorizontalDivider(modifier = Modifier.padding(horizontal = 20.dp))
                ProfileMenuItem(Icons.Filled.Info, "About", onAbout)
            }
        }

        Spacer(modifier = Modifier.height(20.dp))

        OutlinedButton(
            onClick = onLogout,
            modifier = Modifier
                .fillMaxWidth()
                .height(48.dp),
            shape = RoundedCornerShape(10.dp),
            colors = ButtonDefaults.outlinedButtonColors(contentColor = Color(0xFFB14A3C))
        ) {
            Icon(Icons.Filled.Logout, contentDescription = null)
            Spacer(modifier = Modifier.width(8.dp))
            Text("Logout")
        }

        Spacer(modifier = Modifier.height(16.dp))
    }
}

@Composable
private fun ProfileMenuItem(icon: ImageVector, label: String, onClick: () -> Unit) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onClick)
            .padding(horizontal = 20.dp, vertical = 14.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(icon, contentDescription = null, tint = AccentGreen)
        Spacer(modifier = Modifier.width(16.dp))
        Text(label, fontSize = 14.sp, color = Color(0xFF1C1F1E))
        Spacer(Modifier.weight(1f))
        Icon(Icons.Filled.ChevronRight, contentDescription = null, tint = Color(0xFFCCCCCC), modifier = Modifier.size(18.dp))
    }
}