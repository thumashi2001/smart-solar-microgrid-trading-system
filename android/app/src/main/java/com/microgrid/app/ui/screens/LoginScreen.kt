package com.microgrid.app.ui.screens
import androidx.compose.foundation.clickable

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.WbSunny
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.microgrid.app.data.LoginRequest
import com.microgrid.app.data.RetrofitClient
import kotlinx.coroutines.launch

val AccentGreen = Color(0xFF1E7A4D)
val DarkGreen = Color(0xFF0B3B2E)
val PageBg = Color(0xFFF7F5F1)

@Composable
fun LoginScreen(
    onLoginSuccess: (role: String, fullName: String, token: String, nic: String) -> Unit,
    onNavigateToRegister: () -> Unit
) {
    var identifier by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    var isLoading by remember { mutableStateOf(false) }
    val scope = rememberCoroutineScope()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(PageBg)
            .padding(24.dp),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Box(
            modifier = Modifier
                .size(72.dp)
                .background(AccentGreen, CircleShape),
            contentAlignment = Alignment.Center
        ) {
            Icon(Icons.Filled.WbSunny, contentDescription = null, tint = Color.White)
        }
        Spacer(modifier = Modifier.height(12.dp))
        Text("Smart Solar", fontSize = 24.sp, fontWeight = androidx.compose.ui.text.font.FontWeight.Bold, color = DarkGreen)
        Text("Clean Energy Brighter Tomorrow", fontSize = 12.sp, color = Color.Gray)

        Spacer(modifier = Modifier.height(32.dp))

        Card(
            shape = RoundedCornerShape(16.dp),
            colors = CardDefaults.cardColors(containerColor = Color.White),
            elevation = CardDefaults.cardElevation(defaultElevation = 4.dp),
            modifier = Modifier.fillMaxWidth()
        ) {
            Column(modifier = Modifier.padding(24.dp)) {
                OutlinedTextField(
                    value = identifier,
                    onValueChange = { identifier = it },
                    label = { Text("Email or NIC") },
                    leadingIcon = { Icon(Icons.Filled.Person, contentDescription = null) },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true
                )
                Spacer(modifier = Modifier.height(14.dp))

                OutlinedTextField(
                    value = password,
                    onValueChange = { password = it },
                    label = { Text("Password") },
                    leadingIcon = { Icon(Icons.Filled.Lock, contentDescription = null) },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true,
                    visualTransformation = PasswordVisualTransformation(),
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password)
                )

                errorMessage?.let {
                    Spacer(modifier = Modifier.height(10.dp))
                    Text(it, color = MaterialTheme.colorScheme.error, fontSize = 13.sp)
                }

                Spacer(modifier = Modifier.height(20.dp))

                Button(
                    onClick = {
                        errorMessage = null
                        isLoading = true
                        scope.launch {
                            try {
                                val response = RetrofitClient.instance.login(LoginRequest(identifier, password))
                                isLoading = false
                                if (response.isSuccessful && response.body() != null) {
                                    val body = response.body()!!
                                    if (body.role == "Prosumer") {
                                        onLoginSuccess(body.role, body.fullName, body.token, body.nic)
                                    } else {
                                        errorMessage = "This app is for Prosumers only."
                                    }
                                } else {
                                    val apiError = response.errorBody()?.string()
                                    errorMessage = apiError ?: "Invalid credentials."
                                }
                            } catch (e: Exception) {
                                isLoading = false
                                errorMessage = "Could not connect to server."
                            }
                        }
                    },
                    modifier = Modifier.fillMaxWidth().height(48.dp),
                    colors = ButtonDefaults.buttonColors(containerColor = AccentGreen),
                    shape = RoundedCornerShape(10.dp),
                    enabled = !isLoading
                ) {
                    Text(if (isLoading) "Signing in..." else "Login")
                }

                Spacer(modifier = Modifier.height(16.dp))

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.Center
                ) {
                    Text("Don't have an account? ", fontSize = 13.sp, color = Color.Gray)
                    Text(
                        "Register",
                        fontSize = 13.sp,
                        color = AccentGreen,
                        fontWeight = androidx.compose.ui.text.font.FontWeight.Bold,
                        modifier = Modifier.clickable(onClick = onNavigateToRegister)
                    )
                }
            }
        }
    }
}