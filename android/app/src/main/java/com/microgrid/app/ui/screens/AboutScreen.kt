package com.microgrid.app.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

@Composable
fun AboutScreen(onBack: () -> Unit) {
    Column(modifier = Modifier.fillMaxSize().background(PageBg)) {
        TopBar(title = "About", onBack = onBack)

        Column(
            modifier = Modifier.fillMaxWidth().padding(24.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Card(
                shape = RoundedCornerShape(16.dp),
                colors = CardDefaults.cardColors(containerColor = Color.White),
                elevation = CardDefaults.cardElevation(defaultElevation = 4.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(
                    modifier = Modifier.padding(20.dp),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    Text("Smart Solar", fontSize = 18.sp, fontWeight = FontWeight.Bold, color = DarkGreen)
                    Text("Clean Energy Brighter Tomorrow", fontSize = 12.sp, color = Color.Gray)
                    Spacer(modifier = Modifier.height(16.dp))
                    Text(
                        "A Smart Solar Microgrid Trading System connecting solar prosumers with grid operators for efficient, sustainable energy exchange.",
                        fontSize = 13.sp, color = Color(0xFF6B6862)
                    )
                    Spacer(modifier = Modifier.height(16.dp))
                    Text("Version 1.0.0", fontSize = 12.sp, color = Color.Gray)
                }
            }
        }
    }
}