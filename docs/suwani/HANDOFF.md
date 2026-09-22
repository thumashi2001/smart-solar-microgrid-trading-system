# Suwani handoff notes

## For Thumashi (auth)

- Login still uses `Identifier` / `Password` and returns `Token`, `Role`, `FullName`.
- `Token` is now a JWT. Web clients that only stored the token string keep working for login UX, but **must** send `Authorization: Bearer` for any newly protected routes.
- Existing `/api/users`, `/api/prosumers`, `/api/microgridnodes` CRUD remain **unauthenticated** as before (not rewritten). Please harden those under your ownership when ready.
- Shared files touched: `AuthController.cs`, `Program.cs`, `Auth/*`, `appsettings*.example`.

## For Viman (reservations / slots)

- Suwani reads/updates collection `energyReservation` via `IReservationAccess`.
- Model fields: `ReservationId`, `ProsumerNic`, `StationId` (= `NodeId`), `SlotId`, `Status`, `TransactionReference`, completion metadata, UTC timestamps.
- Suwani does **not** implement booking create/update/cancel or 7-day / 12-hour rules.
- When your module lands, keep status values lowercase (`approved`, etc.) or coordinate a migration.
- Completion does **not** decrement slots — coordinate capacity side effects in your domain if required.
- Android: launch QR from booking detail:

```kotlin
startActivity(Intent(context, QrDisplayActivity::class.java).apply {
    putExtra(QrDisplayActivity.EXTRA_RESERVATION_ID, reservationId)
})
```

## For Nethasa (stations / dashboards)

- Station list path remains `/api/microgridnodes` (not `/api/stations`).
- Added backward-compatible `GET /api/microgridnodes/nearby`.
- Operator pending/completed-today counts were **not** invented; wire your dashboard read APIs when available.
- `BatterySlots` is labeled as total slots on Android map details.

## Provisional assumptions

1. Reservation status lowercase.
2. `StationId` on reservations = `NodeId`.
3. Transfer `{id}` = `ReservationId`.
4. QR payload = opaque `TransactionReference` only; reuse on re-fetch.
5. Fixture controller is Development-only and is **not** integrated booking.

## IIS notes

- Publish: `dotnet publish api/MicrogridApi -c Release`
- Set env vars / `appsettings` for Mongo + JWT on the server (never commit secrets).
- Ensure HTTPS binding; Android release should use HTTPS base URL (cleartext is debug-only).
