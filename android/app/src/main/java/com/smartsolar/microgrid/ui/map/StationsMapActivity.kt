package com.smartsolar.microgrid.ui.map

import android.Manifest
import android.annotation.SuppressLint
import android.content.pm.PackageManager
import android.os.Bundle
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import androidx.core.view.isVisible
import androidx.lifecycle.lifecycleScope
import com.google.android.gms.location.LocationServices
import com.google.android.gms.location.Priority
import com.google.android.gms.maps.CameraUpdateFactory
import com.google.android.gms.maps.GoogleMap
import com.google.android.gms.maps.SupportMapFragment
import com.google.android.gms.maps.model.LatLng
import com.google.android.gms.maps.model.Marker
import com.google.android.gms.maps.model.MarkerOptions
import com.smartsolar.microgrid.R
import com.smartsolar.microgrid.data.api.ApiClient
import com.smartsolar.microgrid.data.api.ApiErrorParser
import com.smartsolar.microgrid.data.db.StationEntity
import com.smartsolar.microgrid.databinding.ActivityStationsMapBinding
import com.smartsolar.microgrid.util.smartSolarApp
import com.smartsolar.microgrid.util.toast
import kotlinx.coroutines.launch
import kotlinx.coroutines.tasks.await

class StationsMapActivity : AppCompatActivity() {

    private lateinit var binding: ActivityStationsMapBinding
    private var googleMap: GoogleMap? = null
    private val stationByMarker = mutableMapOf<String, MapStationModel>()
    private var stationsLoaded = false

    private val locationPermissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestMultiplePermissions(),
    ) { loadStations() }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityStationsMapBinding.inflate(layoutInflater)
        setContentView(binding.root)

        binding.toolbar.setNavigationOnClickListener { finish() }
        binding.stationBottomSheet.isVisible = false

        val mapFragment = supportFragmentManager.findFragmentById(R.id.mapFragment) as SupportMapFragment
        mapFragment.getMapAsync { map ->
            googleMap = map
            map.uiSettings.isZoomControlsEnabled = true
            map.setOnMarkerClickListener { marker ->
                showStationDetails(marker)
                true
            }
            requestLocationAndLoad()
        }
    }

    private fun requestLocationAndLoad() {
        val fine = ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION)
        val coarse = ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_COARSE_LOCATION)
        if (fine != PackageManager.PERMISSION_GRANTED && coarse != PackageManager.PERMISSION_GRANTED) {
            locationPermissionLauncher.launch(
                arrayOf(
                    Manifest.permission.ACCESS_FINE_LOCATION,
                    Manifest.permission.ACCESS_COARSE_LOCATION,
                ),
            )
        } else {
            loadStations()
        }
    }

    private fun loadStations() {
        if (stationsLoaded) return
        binding.mapProgress.isVisible = true
        lifecycleScope.launch {
            try {
                val locationGranted = hasLocationPermission()
                val stations = if (locationGranted) {
                    val loc = fetchLastLocation()
                    if (loc != null) {
                        val response = ApiClient.api.getNearbyStations(loc.latitude, loc.longitude)
                        if (response.isSuccessful && response.body() != null) {
                            response.body()!!.map {
                                MapStationModel(
                                    nodeId = it.nodeId,
                                    nodeName = it.nodeName,
                                    location = it.location,
                                    latitude = it.latitude,
                                    longitude = it.longitude,
                                    capacityKWh = it.capacityKWh,
                                    batterySlots = it.batterySlots,
                                    status = it.status,
                                    distanceKm = it.distanceKm,
                                )
                            }
                        } else {
                            toast(getString(R.string.map_error, ApiErrorParser.messageFrom(response)))
                            loadAllStations()
                        }
                    } else {
                        toast(getString(R.string.location_permission_optional))
                        loadAllStations()
                    }
                } else {
                    toast(getString(R.string.location_permission_optional))
                    loadAllStations()
                }

                val toShow = if (stations.isEmpty() && locationGranted) {
                    loadAllStations()
                } else {
                    stations
                }

                if (toShow.isEmpty()) {
                    loadFromCacheOrEmpty()
                } else {
                    cacheStations(toShow)
                    renderStations(toShow, focusLatLng = null)
                }
            } catch (_: Exception) {
                toast(getString(R.string.error_network))
                loadFromCacheOrEmpty()
            } finally {
                binding.mapProgress.isVisible = false
                stationsLoaded = true
            }
        }
    }

    private suspend fun loadAllStations(): List<MapStationModel> {
        val response = ApiClient.api.getMicrogridNodes()
        if (!response.isSuccessful || response.body() == null) {
            return emptyList()
        }
        return response.body()!!.map {
            MapStationModel(
                nodeId = it.nodeId,
                nodeName = it.nodeName,
                location = it.location,
                latitude = it.latitude,
                longitude = it.longitude,
                capacityKWh = it.capacityKWh,
                batterySlots = it.batterySlots,
                status = it.status,
                distanceKm = null,
            )
        }
    }

    private suspend fun loadFromCacheOrEmpty() {
        val cached = smartSolarApp.database.stationCacheDao().getAll()
        if (cached.isEmpty()) {
            toast(getString(R.string.map_error, getString(R.string.error_generic)))
            return
        }
        toast(getString(R.string.cached_stations_notice))
        val models = cached.map { it.toModel() }
        renderStations(models, focusLatLng = null)
    }

    private suspend fun cacheStations(stations: List<MapStationModel>) {
        val now = System.currentTimeMillis()
        smartSolarApp.database.stationCacheDao().upsertAll(
            stations.map {
                StationEntity(
                    nodeId = it.nodeId,
                    nodeName = it.nodeName,
                    location = it.location,
                    latitude = it.latitude,
                    longitude = it.longitude,
                    capacityKWh = it.capacityKWh,
                    batterySlots = it.batterySlots,
                    status = it.status,
                    cachedAt = now,
                )
            },
        )
    }

    private fun renderStations(stations: List<MapStationModel>, focusLatLng: LatLng?) {
        val map = googleMap ?: return
        map.clear()
        stationByMarker.clear()

        val valid = stations.filter { it.latitude in -90.0..90.0 && it.longitude in -180.0..180.0 }
        if (valid.isEmpty()) return

        valid.forEach { station ->
            val marker = map.addMarker(
                MarkerOptions()
                    .position(LatLng(station.latitude, station.longitude))
                    .title(station.nodeName),
            )
            if (marker != null) {
                stationByMarker[marker.id] = station
            }
        }

        val first = focusLatLng ?: LatLng(valid.first().latitude, valid.first().longitude)
        map.moveCamera(CameraUpdateFactory.newLatLngZoom(first, 11f))
    }

    private fun showStationDetails(marker: Marker) {
        val station = stationByMarker[marker.id] ?: return
        binding.stationName.text = station.nodeName
        binding.stationLocation.text = station.location
        binding.stationCapacity.text = getString(R.string.capacity_kwh, station.capacityKWh)
        binding.stationSlots.text = getString(R.string.battery_slots_total, station.batterySlots)
        binding.stationStatus.text = getString(R.string.label_status) + ": " + station.status
        if (station.distanceKm != null) {
            binding.stationDistance.isVisible = true
            binding.stationDistance.text = getString(R.string.distance_km, station.distanceKm)
        } else {
            binding.stationDistance.isVisible = false
        }
        binding.stationBottomSheet.isVisible = true
    }

    private fun hasLocationPermission(): Boolean {
        val fine = ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION)
        val coarse = ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_COARSE_LOCATION)
        return fine == PackageManager.PERMISSION_GRANTED || coarse == PackageManager.PERMISSION_GRANTED
    }

    @SuppressLint("MissingPermission")
    private suspend fun fetchLastLocation(): android.location.Location? {
        if (!hasLocationPermission()) return null
        val client = LocationServices.getFusedLocationProviderClient(this)
        return try {
            client.getCurrentLocation(Priority.PRIORITY_BALANCED_POWER_ACCURACY, null).await()
                ?: client.lastLocation.await()
        } catch (_: Exception) {
            null
        }
    }

    private fun StationEntity.toModel() = MapStationModel(
        nodeId = nodeId,
        nodeName = nodeName,
        location = location,
        latitude = latitude,
        longitude = longitude,
        capacityKWh = capacityKWh,
        batterySlots = batterySlots,
        status = status,
        distanceKm = null,
    )
}
