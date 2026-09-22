# Suwani API contract

## Authentication (shared change — Thumashi review)

`POST /api/auth/login` request/response shapes are **unchanged**:

```json
{ "identifier": "email-or-nic", "password": "..." }
→ { "token": "<JWT>", "role": "GridOperator|Backoffice|Prosumer", "fullName": "..." }
```

- Token is now a **signed JWT** (not a random GUID).
- Clients must send `Authorization: Bearer <token>` for protected endpoints.
- Roles/identity are taken from the token principal only (never from request body).
- Secret: `JwtSettings:SecretKey` (local config / env), minimum 32 characters.

## Identifiers

| Field | Meaning |
|-------|---------|
| `MicrogridNode.Id` | Mongo ObjectId |
| `MicrogridNode.NodeId` | Application station id (`NODE-XXXXXXXX`) |
| `EnergyReservation.ReservationId` | Business reservation id; used as `{id}` in complete |
| `EnergyReservation.StationId` | Equals `NodeId` (not Mongo Id) |
| `TransactionReference` | Opaque QR payload (`TRX-` + hex). Sole QR content |

## Status casing (reservations)

Lowercase, consistent with existing user/node statuses:

`pending` | `approved` | `cancelled` | `completed`

## BatterySlots

Total configured slots on the node. **Not** live availability. Live availability belongs to Viman’s slot domain and is not fabricated.

## QR behaviour

- `GET /api/reservations/{reservationId}/qr` (Authorize: Prosumer owner, Backoffice, or GridOperator)
- Only `approved` reservations receive QR data.
- Repeated retrieval **reuses** the same `TransactionReference` (avoids invalidating displayed codes).
- Cancellation/status change makes verify/complete reject the reference.
- Payload contains **no** NIC, password, login token, or DB credentials.

## Transfer endpoints (Suwani)

### `POST /api/transfers/verify` — Role: `GridOperator`

Request:

```json
{ "transactionReference": "TRX-..." }
```

Success `200`:

```json
{
  "eligible": true,
  "reservationId": "...",
  "transactionReference": "TRX-...",
  "status": "approved",
  "stationId": "NODE-...",
  "stationName": "...",
  "stationLocation": "...",
  "slotId": "...",
  "prosumerNicMasked": "*********678",
  "message": "..."
}
```

Errors: `400` malformed, `401/403` auth, `404` missing, `409` ineligible status/station.

**Does not** complete the transfer.

### `PATCH /api/transfers/{id}/complete` — Role: `GridOperator`

- `{id}` = `ReservationId`
- Atomic `approved` → `completed` via `FindOneAndUpdate`
- Operator id/name/timestamp from JWT + server UTC
- Already completed → `200` with `alreadyCompleted: true` (safe retry)
- Concurrent second winner → same idempotent response
- Cancelled/pending after verify → `409`

## Maps / stations

Existing (Nethasa): `GET /api/microgridnodes`, `GET /api/microgridnodes/{id}`

Suwani addition: `GET /api/microgridnodes/nearby?lat=&lng=&radiusKm=`

- Default radius: **10 km** (max 100)
- Active stations only
- Zero coordinates allowed if in valid WGS84 range
- Response includes `distanceKm` and labels `batterySlots` as total capacity

## Collection

Uses proposed name `energyReservation` (no rename of `SolarStationInfo` / `users` / `prosumers`).

## Dev fixture (Development only)

`POST /api/dev/suwani-fixtures/seed`  
Alias: `POST /api/dev/suwani-fixtures/approved-reservation`

Idempotent Suwani seed (users, three stations, Colombo slot, approved reservation + TRX). Returns `404` outside Development. See [SEED.md](./SEED.md).

## UTC

All timestamps stored/returned as UTC. Android should display in device local timezone when formatting.
