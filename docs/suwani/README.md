# Suwani — Component 4 (Operator / Maps / QR / Transfers)

Owner: **Suwani**  
Branch: `feature/suwani-operator-maps-qr`  
Repository: https://github.com/thumashi2001/smart-solar-microgrid-trading-system

## Implemented scope

| Area | Status |
|------|--------|
| JWT-protected Grid Operator transfer API | Implemented |
| QR issuance for approved reservations | Implemented |
| QR scan → verify → confirm → complete (Android) | Implemented |
| Nearby/all stations Google Maps | Implemented (needs Maps API key) |
| SQLite/Room session + station cache | Implemented |
| Provisional `energyReservation` Mongo adapter | Implemented (Viman booking CRUD not owned) |
| Dev-only fixture seeding | Implemented (`ASPNETCORE_ENVIRONMENT=Development`) |
| Idempotent Suwani seed (users/stations/slot/reservation) | Implemented — see [SEED.md](./SEED.md) |

## Paths

- API: `api/MicrogridApi/`
- Tests: `api/MicrogridApi.Tests/`
- Android: `android/` (package `com.smartsolar.microgrid`)
- Docs: `docs/suwani/`

## Prerequisites

- .NET 8 SDK
- MongoDB (local or Atlas) — configure via gitignored `appsettings*.json`
- JDK 17 + Android SDK 34
- Google Maps SDK API key (Android)

## Quick start (API)

```powershell
cd api/MicrogridApi
copy appsettings.json.example appsettings.json
copy appsettings.Development.json.example appsettings.Development.json
# Edit ConnectionString, DatabaseName, JwtSettings:SecretKey (min 32 chars)
dotnet restore
dotnet run --launch-profile http
# Swagger: http://localhost:5148/swagger
# Seed (Development only): POST /api/dev/suwani-fixtures/seed
```

See [SEED.md](./SEED.md) for test logins and cleanup keys.

## Quick start (Android)

```powershell
cd android
copy local.properties.example local.properties
# Set sdk.dir and MAPS_API_KEY
.\gradlew.bat assembleDebug
```

- Emulator API base URL is `http://10.0.2.2:5148/` (maps to host loopback).
- Physical device: change `API_BASE_URL` in `app/build.gradle.kts` to your LAN IP / HTTPS host.

## Related docs

- [API_CONTRACT.md](./API_CONTRACT.md)
- [SEED.md](./SEED.md)
- [TEST_CHECKLIST.md](./TEST_CHECKLIST.md)
- [HANDOFF.md](./HANDOFF.md)
- [android/README.md](../../android/README.md)
