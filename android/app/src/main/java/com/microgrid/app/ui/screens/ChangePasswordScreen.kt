package com.microgrid.app.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.microgrid.app.data.ChangePasswordRequest
import com.microgrid.app.data.RetrofitClient
import kotlinx.coroutines.launch

@Composable
fun ChangePasswordScreen(nic: String, fullName: String, email: String, phone: String, onBack: () -> Unit) {
    var currentPassword by remember { mutableStateOf("") }
    var newPassword by remember { mutableStateOf("") }
    var confirmPassword by remember { mutableStateOf("") }
    var message by remember { mutableStateOf<String?>(null) }
    var isError by remember { mutableStateOf(false) }
    var isLoading by remember { mutableStateOf(false) }
    val scope = rememberCoroutineScope()

    Column(modifier = Modifier.fillMaxSize().background(PageBg)) {
        TopBar(title = "Change Password", onBack = onBack)

        Column(modifier = Modifier.padding(24.dp)) {
            Card(
                shape = RoundedCornerShape(16.dp),
                colors = CardDefaults.cardColors(containerColor = Color.White),
                elevation = CardDefaults.cardElevation(defaultElevation = 4.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(modifier = Modifier.padding(20.dp)) {
                    OutlinedTextField(
                        value = currentPassword, onValueChange = { currentPassword = it },
                        label = { Text("Current Password") }, modifier = Modifier.fillMaxWidth(), singleLine = true,
                        visualTransformation = PasswordVisualTransformation()
                    )
                    Spacer(modifier = Modifier.height(12.dp))
                    OutlinedTextField(
                        value = newPassword, onValueChange = { newPassword = it },
                        label = { Text("New Password") }, modifier = Modifier.fillMaxWidth(), singleLine = true,
                        visualTransformation = PasswordVisualTransformation()
                    )
                    Spacer(modifier = Modifier.height(12.dp))
                    OutlinedTextField(
                        value = confirmPassword, onValueChange = { confirmPassword = it },
                        label = { Text("Confirm New Password") }, modifier = Modifier.fillMaxWidth(), singleLine = true,
                        visualTransformation = PasswordVisualTransformation()
                    )

                    message?.let {
                        Spacer(modifier = Modifier.height(10.dp))
                        Text(it, color = if (isError) MaterialTheme.colorScheme.error else Color(0xFF2E7D4F), fontSize = 13.sp)
                    }

                    Spacer(modifier = Modifier.height(20.dp))

                    Button(
                        onClick = {
                            message = null
                            if (newPassword != confirmPassword) {
                                isError = true
                                message = "New passwords do not match."
                                return@Button
                            }
                            if (newPassword.isBlank()) {
                                isError = true
                                message = "New password cannot be empty."
                                return@Button
                            }
                            isLoading = true
                            scope.launch {
                                try {
                                    val response = RetrofitClient.instance.changePassword(
                                        nic,
                                        ChangePasswordRequest(newPassword)
                                    )
                                    isLoading = false
                                    if (response.isSuccessful) {
                                        isError = false
                                        message = "Password updated successfully."
                                    } else {
                                        isError = true
                                        message = "Failed to update password."
                                    }
                                } catch (e: Exception) {
                                    isLoading = false
                                    isError = true
                                    message = "Could not connect to server."
                                }
                            }
                        },
                        modifier = Modifier.fillMaxWidth().height(48.dp),
                        colors = ButtonDefaults.buttonColors(containerColor = AccentGreen),
                        shape = RoundedCornerShape(10.dp),
                        enabled = !isLoading
                    ) {
                        Text(if (isLoading) "Updating..." else "Update Password")
                    }
                }
            }
        }
    }
}