# Smart Solar Microgrid — Android (Grid Operator)

Native Kotlin app for **Grid Operator** workflows: sign in, scan transfer QR codes, verify/complete transfers, and browse microgrid stations on a map.

Package: `com.smartsolar.microgrid`  
Min SDK **26**, compile/target SDK **34**.

## Prerequisites

- Android Studio Hedgehog (2023.1.1) or newer
- Android SDK 34
- JDK 17
- Running [Microgrid API](../api/MicrogridApi) (default dev URL: `http://10.0.2.2:5148/` from the Android emulator)
- Google Maps API key (Maps SDK for Android enabled)

## Setup

1. Copy `local.properties.example` to `local.properties` (gitignored):

   ```properties
   sdk.dir=C\:\\Users\\YourName\\AppData\\Local\\Android\\Sdk
   MAPS_API_KEY=your_maps_key
   ```

2. **Gradle wrapper** — `gradle/wrapper/gradle-wrapper.properties` is included. If `gradle/wrapper/gradle-wrapper.jar` is missing, generate the wrapper from the `android/` directory:

   ```bash
   gradle wrapper --gradle-version 8.2
   ```

   Or in Android Studio: **File → New → Import Project** and let Studio download the wrapper.

3. Open the `android/` folder in Android Studio and sync Gradle.

4. Start the API locally (port **5148** matches `BuildConfig.API_BASE_URL` in debug).

5. Run on an emulator or device. Use a **GridOperator** account from your MongoDB seed/backoffice users.

## API base URL

Debug builds use:

`BuildConfig.API_BASE_URL` → `http://10.0.2.2:5148/`

To point at another host, change `buildConfigField` in `app/build.gradle.kts` or add product flavors.

## Cleartext HTTP (debug only)

Debug builds merge `src/debug/res/xml/network_security_config.xml`, which allows cleartext to `10.0.2.2` / localhost. Release uses HTTPS-only defaults in `src/main/res/xml/network_security_config.xml`.

## Screens

| Activity | Purpose |
|----------|---------|
| `LoginActivity` | Identifier + password → `POST /api/auth/login` |
| `OperatorHomeActivity` | Scan QR, stations map, logout |
| `QrScannerActivity` | CameraX + ML Kit → `POST /api/transfers/verify` |
| `VerificationActivity` | Review verify payload → `PATCH /api/transfers/{id}/complete` |
| `TransferResultActivity` | Success / already completed |
| `StationsMapActivity` | Google Maps + `GET /api/microgridnodes` / nearby |
| `QrDisplayActivity` | `GET /api/reservations/{id}/qr` + ZXing encode (Prosumer/deep link) |

### Navigation entry (QR display)

From another module or future booking flow:

```kotlin
startActivity(Intent(context, QrDisplayActivity::class.java).apply {
    putExtra(QrDisplayActivity.EXTRA_RESERVATION_ID, reservationId)
})
```

## Session storage

Room table `user_session` stores **token**, **role**, **fullName**, and **identifier** only (never the password). Cleared on logout.

Optional station cache: `cached_stations` with `cachedAt` for offline map fallback.

## Permissions

- `INTERNET` — API calls
- `CAMERA` — QR scanner
- `ACCESS_FINE_LOCATION` / `ACCESS_COARSE_LOCATION` — optional; nearby stations when granted

## Tech stack

- Kotlin, View Binding, XML layouts
- Retrofit + OkHttp + Gson
- Room
- CameraX, ML Kit barcode scanning, ZXing core (QR generation)
- Google Maps SDK, Play Services Location

## Build from CLI

```bash
cd android
./gradlew assembleDebug
```

On Windows:

```bat
gradlew.bat assembleDebug
```
