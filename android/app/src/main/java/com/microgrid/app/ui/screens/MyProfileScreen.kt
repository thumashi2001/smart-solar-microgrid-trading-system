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

import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.ui.text.input.KeyboardType
import com.microgrid.app.data.ProsumerRegisterRequest

@Composable
fun MyProfileScreen(
    fullName: String,
    nic: String,
    onBack: () -> Unit,
    onAccountDeactivated: () -> Unit
) {
    var isEditing by remember { mutableStateOf(false) }
    var editFullName by remember { mutableStateOf(fullName) }
    var editEmail by remember { mutableStateOf("") }
    var editPhone by remember { mutableStateOf("") }
    
    var displayFullName by remember { mutableStateOf(fullName) }
    var displayEmail by remember { mutableStateOf("") }
    var displayPhone by remember { mutableStateOf("") }

    var showConfirmDialog by remember { mutableStateOf(false) }
    var showSuccessDialog by remember { mutableStateOf(false) }
    var isLoading by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    var successMessage by remember { mutableStateOf<String?>(null) }
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
                    if (isEditing) {
                        OutlinedTextField(
                            value = editFullName, onValueChange = { editFullName = it },
                            label = { Text("Full Name") }, modifier = Modifier.fillMaxWidth(), singleLine = true
                        )
                        Spacer(modifier = Modifier.height(12.dp))
                        OutlinedTextField(
                            value = nic, onValueChange = { },
                            label = { Text("NIC") }, modifier = Modifier.fillMaxWidth(), enabled = false, singleLine = true
                        )
                        Spacer(modifier = Modifier.height(12.dp))
                        OutlinedTextField(
                            value = editEmail, onValueChange = { editEmail = it },
                            label = { Text("Email") }, modifier = Modifier.fillMaxWidth(), singleLine = true,
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Email)
                        )
                        Spacer(modifier = Modifier.height(12.dp))
                        OutlinedTextField(
                            value = editPhone, onValueChange = { editPhone = it },
                            label = { Text("Phone") }, modifier = Modifier.fillMaxWidth(), singleLine = true,
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Phone)
                        )
                    } else {
                        ProfileField("Full Name", displayFullName)
                        ProfileField("NIC", nic)
                        if (displayEmail.isNotBlank()) ProfileField("Email", displayEmail)
                        if (displayPhone.isNotBlank()) ProfileField("Phone", displayPhone)
                        ProfileField("Role", "Prosumer")
                    }
                }
            }

            errorMessage?.let {
                Spacer(modifier = Modifier.height(12.dp))
                Text(it, color = MaterialTheme.colorScheme.error, fontSize = 13.sp)
            }
            successMessage?.let {
                Spacer(modifier = Modifier.height(12.dp))
                Text(it, color = Color(0xFF2E7D4F), fontSize = 13.sp)
            }

            Spacer(modifier = Modifier.height(24.dp))

            if (isEditing) {
                Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    OutlinedButton(
                        onClick = { 
                            isEditing = false 
                            errorMessage = null
                        },
                        modifier = Modifier.weight(1f).height(48.dp),
                        shape = RoundedCornerShape(10.dp)
                    ) {
                        Text("Cancel", color = DarkGreen)
                    }
                    Button(
                        onClick = {
                            errorMessage = null
                            successMessage = null
                            isLoading = true
                            scope.launch {
                                try {
                                    val request = ProsumerRegisterRequest(
                                        nic = nic,
                                        fullName = editFullName,
                                        email = editEmail,
                                        phone = editPhone,
                                        passwordHash = "" // Password update handled in Change Password
                                    )
                                    val response = RetrofitClient.instance.updateProsumer(nic, request)
                                    isLoading = false
                                    if (response.isSuccessful) {
                                        displayFullName = editFullName
                                        displayEmail = editEmail
                                        displayPhone = editPhone
                                        isEditing = false
                                        successMessage = "Profile updated successfully."
                                    } else {
                                        errorMessage = "Failed to update profile."
                                    }
                                } catch (e: Exception) {
                                    isLoading = false
                                    errorMessage = "Could not connect to server."
                                }
                            }
                        },
                        modifier = Modifier.weight(1f).height(48.dp),
                        colors = ButtonDefaults.buttonColors(containerColor = AccentGreen),
                        shape = RoundedCornerShape(10.dp),
                        enabled = !isLoading
                    ) {
                        Text(if (isLoading) "Saving..." else "Save")
                    }
                }
            } else {
                Button(
                    onClick = { 
                        isEditing = true
                        successMessage = null
                        errorMessage = null 
                    },
                    modifier = Modifier.fillMaxWidth().height(48.dp),
                    colors = ButtonDefaults.buttonColors(containerColor = AccentGreen),
                    shape = RoundedCornerShape(10.dp)
                ) {
                    Text("Edit Profile")
                }
                Spacer(modifier = Modifier.height(12.dp))
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