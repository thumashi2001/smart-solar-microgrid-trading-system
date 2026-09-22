package com.smartsolar.microgrid.util

import android.content.Context
import android.widget.Toast
import com.smartsolar.microgrid.SmartSolarApp

val Context.smartSolarApp: SmartSolarApp
    get() = applicationContext as SmartSolarApp

fun Context.toast(message: String) {
    Toast.makeText(this, message, Toast.LENGTH_LONG).show()
}
