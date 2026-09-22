# Suwani development seed

**Development only.** Never available when `ASPNETCORE_ENVIRONMENT=Production` (endpoint returns `404`).

## Enable Development

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project api/MicrogridApi --launch-profile http
```

The `http` launch profile already sets `ASPNETCORE_ENVIRONMENT=Development`.

## Database used

Whatever is configured in your **local** (gitignored) files:

- `api/MicrogridApi/appsettings.json`
- `api/MicrogridApi/appsettings.Development.json`

Team default database name: `microgrid_db`  
Collection names used by the seeder (existing / planned — no renames):

| Collection | Purpose |
|------------|---------|
| `users` | Backoffice + GridOperator |
| `prosumers` | Test prosumer |
| `SolarStationInfo` | Three stations (existing name) |
| `energyBookingSlots` | One Colombo slot |
| `energyReservation` | One approved reservation + TRX |

## Run the seed

With the API running in Development:

```http
POST http://localhost:5148/api/dev/suwani-fixtures/seed
```

Compatibility alias (same idempotent behaviour):

```http
POST http://localhost:5148/api/dev/suwani-fixtures/approved-reservation
```

Safe response fields only: emails/userIds, NIC, station NodeIds, slotId, reservationId, transactionReference, created/updated keys. **No** password hashes or connection strings.

Running seed multiple times upserts the same stable keys and does **not** create duplicates or delete other records.

## Development login credentials (dev-only)

| Role | Identifier | Password |
|------|------------|----------|
| GridOperator | `operator@test.com` | `Operator@12345` |
| Prosumer | `199912345678` | `Prosumer@12345` |
| Backoffice | `backoffice@test.com` | `Backoffice@12345` |

**Warning:** These passwords are for local/development demos only. Do not reuse them in production. Rotate Atlas credentials if they were ever shared in chat.

## Stable seed identifiers (for manual cleanup)

If you need to remove **only** Suwani seed rows (do not wipe teammate data):

| Key | Value |
|-----|-------|
| Operator email | `operator@test.com` |
| Backoffice email | `backoffice@test.com` |
| Prosumer NIC | `199912345678` |
| Station NodeIds | `NODE-SUWANI-COLOMBO`, `NODE-SUWANI-KANDY`, `NODE-SUWANI-GALLE` |
| SlotId | `SLOT-SUWANI-COLOMBO-001` |
| ReservationId | `RES-SUWANI-APPROVED-001` |
| TransactionReference | `TRX-SUWANISEED0001APPROVEDRESERVATION01` |

Example MongoDB shell deletes (manual, targeted):

```javascript
db.users.deleteMany({ email: { $in: ["operator@test.com", "backoffice@test.com"] } })
db.prosumers.deleteMany({ nic: "199912345678" })
db.SolarStationInfo.deleteMany({ nodeId: { $in: ["NODE-SUWANI-COLOMBO", "NODE-SUWANI-KANDY", "NODE-SUWANI-GALLE"] } })
db.energyBookingSlots.deleteMany({ slotId: "SLOT-SUWANI-COLOMBO-001" })
db.energyReservation.deleteMany({ reservationId: "RES-SUWANI-APPROVED-001" })
```

## Assumptions

1. Reservation status casing is lowercase (`approved`), matching existing user/node status style.
2. `StationId` / slot `StationId` store `MicrogridNode.NodeId`, not Mongo `_id`.
3. Slot documents live in `energyBookingSlots` (team plan name); Suwani does not implement slot CRUD APIs.
4. Seed re-asserts the approved reservation + stable TRX so QR demos remain repeatable after a completed transfer test.
