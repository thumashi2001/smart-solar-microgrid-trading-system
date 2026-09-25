package com.microgrid.app.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Warning
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.microgrid.app.data.RetrofitClient
import kotlinx.coroutines.launch

@Composable
fun MyProfileScreen(
    fullName: String,
    nic: String,
    onBack: () -> Unit,
    onAccountDeactivated: () -> Unit
) {
    var showConfirmDialog by remember { mutableStateOf(false) }
    var showSuccessDialog by remember { mutableStateOf(false) }
    var isLoading by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()

    Column(modifier = Modifier.fillMaxSize().background(PageBg)) {
        TopBar(title = "My Profile", onBack = onBack)

        Column(modifier = Modifier.padding(24.dp)) {
            Card(
                shape = RoundedCornerShape(16.dp),
                colors = CardDefaults.cardColors(containerColor = Color.White),
                elevation = CardDefaults.cardElevation(defaultElevation = 4.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(modifier = Modifier.padding(20.dp)) {
                    ProfileField("Full Name", fullName)
                    ProfileField("NIC", nic)
                    ProfileField("Role", "Prosumer")
                }
            }

            errorMessage?.let {
                Spacer(modifier = Modifier.height(12.dp))
                Text(it, color = MaterialTheme.colorScheme.error, fontSize = 13.sp)
            }

            Spacer(modifier = Modifier.height(24.dp))

            OutlinedButton(
                onClick = { showConfirmDialog = true },
                modifier = Modifier.fillMaxWidth().height(48.dp),
                shape = RoundedCornerShape(10.dp),
                colors = ButtonDefaults.outlinedButtonColors(contentColor = Color(0xFFB14A3C)),
                enabled = !isLoading
            ) {
                Text(if (isLoading) "Processing..." else "Deactivate My Account")
            }
        }
    }

    if (showConfirmDialog) {
        AlertDialog(
            onDismissRequest = { showConfirmDialog = false },
            icon = { Icon(Icons.Filled.Warning, contentDescription = null, tint = Color(0xFFB14A3C)) },
            title = { Text("Deactivate account?") },
            text = { Text("This will deactivate your account. You won't be able to log in until a Backoffice officer reactivates it. Are you sure?") },
            confirmButton = {
                TextButton(onClick = {
                    showConfirmDialog = false
                    isLoading = true
                    errorMessage = null
                    scope.launch {
                        try {
                            val response = RetrofitClient.instance.deactivateProsumer(nic)
                            isLoading = false
                            if (response.isSuccessful) {
                                showSuccessDialog = true
                            } else {
                                errorMessage = "Failed to deactivate account."
                            }
                        } catch (e: Exception) {
                            isLoading = false
                            errorMessage = "Could not connect to server."
                        }
                    }
                }) {
                    Text("Deactivate", color = Color(0xFFB14A3C))
                }
            },
            dismissButton = {
                TextButton(onClick = { showConfirmDialog = false }) {
                    Text("Cancel")
                }
            }
        )
    }

    if (showSuccessDialog) {
        AlertDialog(
            onDismissRequest = { },
            title = { Text("Account deactivated") },
            text = { Text("Your account has been deactivated successfully.") },
            confirmButton = {
                TextButton(onClick = {
                    showSuccessDialog = false
                    onAccountDeactivated()
                }) {
                    Text("OK")
                }
            }
        )
    }
}

@Composable
private fun ProfileField(label: String, value: String) {
    Column(modifier = Modifier.padding(vertical = 8.dp)) {
        Text(label, fontSize = 12.sp, color = Color.Gray)
        Text(value, fontSize = 15.sp, fontWeight = FontWeight.Medium, color = Color(0xFF1C1F1E))
    }
}

@Composable
fun TopBar(title: String, onBack: () -> Unit) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .background(Color.White)
            .padding(16.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        IconButton(onClick = onBack) {
            Icon(Icons.Filled.ArrowBack, contentDescription = "Back")
        }
        Spacer(modifier = Modifier.width(8.dp))
        Text(title, fontSize = 18.sp, fontWeight = FontWeight.Bold, color = DarkGreen)
    }
}