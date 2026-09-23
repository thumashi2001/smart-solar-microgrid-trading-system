# Reservation & Slot API Integration Contract

This document provides the API contract for Component 3 (Dashboards, Booking Views & Microgrid Node Management) to interact with Component 2 (Energy Reservation and Slot Management).

## 1. API Base URL
The API is currently configured for development. The base URLs are:
- **HTTPS Development:** `https://localhost:7193`
- **HTTP Development:** `http://localhost:5148`
- **IIS Express:** `http://localhost:40456`

*(Note: Production API URL is not yet available and must not expose MongoDB credentials).*

## 2. Reservation Data Contract
The authoritative JSON structure for an Energy Reservation object is:

```json
{
  "id": "64c8d5f3a0...",
  "reservationId": "RES-E2A91D34",
  "prosumerNic": "981234567V",
  "stationId": "ST-001",
  "slotId": "SLOT-001",
  "status": "Approved",
  "createdAt": "2026-09-23T11:00:00Z",
  "updatedAt": "2026-09-23T11:00:00Z",
  "transactionReference": ""
}
```

### Reservation Status Values
Only the following exact status values are permitted for reservations:
- `"Pending"`
- `"Approved"`
- `"Cancelled"`
- `"Completed"`

## 3. Slot Data Contract
The authoritative JSON structure for an Energy Booking Slot object is:

```json
{
  "id": "64c8d5f3b1...",
  "slotId": "SLOT-001",
  "stationId": "ST-001",
  "date": "2026-09-25T00:00:00Z",
  "startTime": "09:00",
  "endTime": "10:00",
  "capacity": 5,
  "availability": 4,
  "status": "Available",
  "createdAt": "2026-09-23T08:00:00Z",
  "updatedAt": "2026-09-23T08:00:00Z"
}
```

### Slot Status Values
- `"Available"`
- `"Unavailable"`

## 4. Station Relationship
Do NOT duplicate `SolarStationInfo` or node data. Use the exact relationships defined below:
- `SolarStationInfo.NodeId` matches `EnergyBookingSlot.StationId`
- `EnergyBookingSlot.SlotId` matches `EnergyReservation.SlotId`
- `EnergyReservation.StationId` is denormalized and matches `SolarStationInfo.NodeId` directly.

## 5. Actual Existing API Endpoints

### 5.1 GET /api/reservations
- **Description:** Returns all reservations in the system.
- **Request Parameters:** None
- **Response Structure:** JSON Array of Reservation objects (see section 2).

### 5.2 GET /api/reservations/{id}
- **Description:** Returns a single reservation by its MongoDB `_id`.
- **Request Parameters:** `id` (path parameter, MongoDB ObjectId)
- **Response Structure:** A single Reservation object (see section 2).
- **Errors:** 404 Not Found if missing.

### 5.3 GET /api/reservations/history/{prosumerNic}
- **Description:** Returns reservation history for a specific prosumer.
- **Request Parameters:** `prosumerNic` (path parameter)
- **Response Structure:** JSON Array of Reservation objects for that NIC. This includes `Cancelled` and `Completed` bookings.

### 5.4 POST /api/reservations
- **Description:** Creates a new reservation.
- **Request Body:**
```json
{
  "prosumerNic": "981234567V",
  "stationId": "ST-001",
  "slotId": "SLOT-001"
}
```

### 5.5 PUT /api/reservations/{id}
- **Description:** Modifies a reservation to a new slot.
- **Request Body:**
```json
{
  "stationId": "ST-001",
  "slotId": "SLOT-002"
}
```

### 5.6 DELETE /api/reservations/{id}
- **Description:** Cancels an existing reservation. (Logical cancellation).
- **Request Body:** None.

## 6. Actual Existing Slot API Endpoints

### 6.1 GET /api/slots
- **Description:** Returns slots, with optional filtering.
- **Request Parameters (Query):** 
  - `stationId` (optional string): Filters by Station ID.
  - `date` (optional string): Filters by Date (YYYY-MM-DD).
- **Response Structure:** JSON Array of Slot objects (see section 3).

### 6.2 GET /api/slots/{id}
- **Description:** Returns a single slot by its MongoDB `_id`.
- **Request Parameters:** `id` (path parameter, MongoDB ObjectId)
- **Response Structure:** A single Slot object (see section 3).
- **Errors:** 404 Not Found if missing.

### 6.3 POST /api/slots
- **Description:** Creates a new slot for a station.
- **Request Body:**
```json
{
  "stationId": "ST-001",
  "date": "2026-09-25",
  "startTime": "09:00",
  "endTime": "10:00",
  "capacity": 5
}
```
- **Business Rule:** Capacity must be > 0. The referenced Station must exist and be active. The system automatically initializes `availability` to match `capacity`.

### 6.4 PUT /api/slots/{id}
- **Description:** Updates a slot's status.
- **Request Body:**
```json
{
  "status": "Unavailable"
}
```
- **Business Rule:** To preserve integrity, this endpoint ONLY allows updating the `status` (to `"Available"` or `"Unavailable"`). Modifying `capacity` or `availability` directly is blocked to prevent breaking ongoing reservation logic.

## 7. Business-Rule Notes for UI/Dashboard

Nethasa's UI must respect the API as the absolute authority for these business rules:

- **Creation:** A slot must exist, belong to the station, be `"Available"`, have `Availability > 0`, and start within 7 days. Availability is decreased safely.
- **Modification:** Requires at least 12 hours' notice based on the CURRENT slot's start time. Availability is automatically exchanged.
- **Cancellation:** Requires at least 12 hours' notice. The reservation becomes `"Cancelled"` and is retained for history. The slot's availability is atomically restored where possible.
- **Slot Capacity vs Availability:** Capacity is immutable after slot creation to ensure system integrity. Slot deactivation (changing status to `"Unavailable"`) prevents new bookings but does not forcefully cancel existing active reservations.

## 8. Remaining API Gaps for Dashboards and Search

Currently, the API has the following remaining gaps regarding dashboard requirements:

1. **Missing Reservation Search & Server-Side Filtering:** The API only supports retrieving all reservations globally (`/api/reservations`) or by a specific Prosumer NIC (`/api/reservations/history/{prosumerNic}`). It does not currently support server-side filtering by Station ID, Status, or Date range. Nethasa would currently have to fetch all reservations and filter them client-side.
2. **No Dashboard Aggregation Endpoints:** To calculate total, pending, approved, or cancelled counts per station, the dashboard must fetch the entire list of reservations and run manual counting logic. A dedicated `/api/reservations/stats` endpoint would be highly beneficial to avoid massive payload transfers as the database scales.
