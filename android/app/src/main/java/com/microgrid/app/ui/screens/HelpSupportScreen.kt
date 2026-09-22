package com.microgrid.app.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

@Composable
fun HelpSupportScreen(onBack: () -> Unit) {
    Column(modifier = Modifier.fillMaxSize().background(PageBg)) {
        TopBar(title = "Help & Support", onBack = onBack)

        Column(modifier = Modifier.padding(24.dp)) {
            Card(
                shape = RoundedCornerShape(16.dp),
                colors = CardDefaults.cardColors(containerColor = Color.White),
                elevation = CardDefaults.cardElevation(defaultElevation = 4.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(modifier = Modifier.padding(20.dp)) {
                    Text("Need help?", fontSize = 16.sp, fontWeight = FontWeight.Bold, color = DarkGreen)
                    Spacer(modifier = Modifier.height(8.dp))
                    Text(
                        "Contact our support team for any issues with your account, bookings, or the Smart Solar Microgrid platform.",
                        fontSize = 14.sp, color = Color(0xFF6B6862)
                    )
                    Spacer(modifier = Modifier.height(16.dp))
                    Text("Email", fontSize = 12.sp, color = Color.Gray)
                    Text("support@smartsolar.lk", fontSize = 14.sp, color = Color(0xFF1C1F1E))
                    Spacer(modifier = Modifier.height(12.dp))
                    Text("Phone", fontSize = 12.sp, color = Color.Gray)
                    Text("+94 11 234 5678", fontSize = 14.sp, color = Color(0xFF1C1F1E))
                }
            }
        }
    }
}