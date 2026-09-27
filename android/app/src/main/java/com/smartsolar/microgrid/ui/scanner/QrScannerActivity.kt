package com.smartsolar.microgrid.ui.scanner

import android.Manifest
import android.content.pm.PackageManager
import android.os.Bundle
import android.os.SystemClock
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import androidx.camera.core.CameraSelector
import androidx.camera.core.ImageAnalysis
import androidx.camera.core.Preview
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.core.content.ContextCompat
import androidx.core.view.isVisible
import androidx.lifecycle.lifecycleScope
import com.google.mlkit.vision.barcode.BarcodeScannerOptions
import com.google.mlkit.vision.barcode.BarcodeScanning
import com.google.mlkit.vision.barcode.common.Barcode
import com.google.mlkit.vision.common.InputImage
import com.smartsolar.microgrid.R
import com.smartsolar.microgrid.data.api.ApiClient
import com.smartsolar.microgrid.data.api.ApiErrorParser
import com.smartsolar.microgrid.data.api.dto.VerifyTransferRequest
import com.smartsolar.microgrid.databinding.ActivityQrScannerBinding
import com.smartsolar.microgrid.ui.verification.VerificationActivity
import com.smartsolar.microgrid.util.toast
import kotlinx.coroutines.launch
import java.util.concurrent.ExecutorService
import java.util.concurrent.Executors

class QrScannerActivity : AppCompatActivity() {

    private lateinit var binding: ActivityQrScannerBinding
    private var cameraExecutor: ExecutorService? = null
    private var processingScan = false
    private var lastPayload: String? = null
    private var lastScanAtMs: Long = 0L

    private val permissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestPermission(),
    ) { granted ->
        if (granted) {
            binding.permissionPanel.isVisible = false
            startCamera()
        } else {
            binding.permissionPanel.isVisible = true
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityQrScannerBinding.inflate(layoutInflater)
        setContentView(binding.root)

        binding.toolbar.setNavigationOnClickListener { finish() }
        binding.grantPermissionButton.setOnClickListener {
            permissionLauncher.launch(Manifest.permission.CAMERA)
        }

        cameraExecutor = Executors.newSingleThreadExecutor()
        ensureCameraPermission()
    }

    private fun ensureCameraPermission() {
        when {
            ContextCompat.checkSelfPermission(this, Manifest.permission.CAMERA) ==
                PackageManager.PERMISSION_GRANTED -> {
                binding.permissionPanel.isVisible = false
                startCamera()
            }
            else -> binding.permissionPanel.isVisible = true
        }
    }

    private fun startCamera() {
        val cameraProviderFuture = ProcessCameraProvider.getInstance(this)
        cameraProviderFuture.addListener({
            val cameraProvider = cameraProviderFuture.get()
            val preview = Preview.Builder().build().also {
                it.setSurfaceProvider(binding.previewView.surfaceProvider)
            }

            val options = BarcodeScannerOptions.Builder()
                .setBarcodeFormats(Barcode.FORMAT_QR_CODE)
                .build()
            val scanner = BarcodeScanning.getClient(options)

            val analysis = ImageAnalysis.Builder()
                .setBackpressureStrategy(ImageAnalysis.STRATEGY_KEEP_ONLY_LATEST)
                .build()

            analysis.setAnalyzer(cameraExecutor!!) { imageProxy ->
                val mediaImage = imageProxy.image
                if (mediaImage == null) {
                    imageProxy.close()
                    return@setAnalyzer
                }
                val image = InputImage.fromMediaImage(mediaImage, imageProxy.imageInfo.rotationDegrees)
                scanner.process(image)
                    .addOnSuccessListener { barcodes ->
                        val raw = barcodes.firstOrNull()?.rawValue?.trim().orEmpty()
                        if (raw.isNotEmpty()) {
                            onBarcodeDetected(raw)
                        }
                    }
                    .addOnCompleteListener { imageProxy.close() }
            }

            cameraProvider.unbindAll()
            cameraProvider.bindToLifecycle(
                this,
                CameraSelector.DEFAULT_BACK_CAMERA,
                preview,
                analysis,
            )
        }, ContextCompat.getMainExecutor(this))
    }

    private fun onBarcodeDetected(payload: String) {
        if (processingScan) return
        val now = SystemClock.elapsedRealtime()
        if (payload == lastPayload && now - lastScanAtMs < DEBOUNCE_MS) return
        lastPayload = payload
        lastScanAtMs = now
        processingScan = true

        runOnUiThread { binding.scanProgress.isVisible = true }
        lifecycleScope.launch {
            try {
                val response = ApiClient.api.verifyTransfer(VerifyTransferRequest(payload))
                if (!response.isSuccessful || response.body() == null) {
                    toast(ApiErrorParser.messageFrom(response))
                    processingScan = false
                    runOnUiThread { binding.scanProgress.isVisible = false }
                    return@launch
                }
                val verify = response.body()!!
                startActivity(VerificationActivity.intentFromVerify(this@QrScannerActivity, verify))
                finish()
            } catch (_: Exception) {
                toast(getString(R.string.error_network))
                processingScan = false
                runOnUiThread { binding.scanProgress.isVisible = false }
            }
        }
    }

    override fun onDestroy() {
        cameraExecutor?.shutdown()
        cameraExecutor = null
        super.onDestroy()
    }

    companion object {
        private const val DEBOUNCE_MS = 2500L
    }
}
