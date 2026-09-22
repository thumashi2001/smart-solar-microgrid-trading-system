# Suwani test checklist

## Automated (ran in this workspace)

| Command | Result |
|---------|--------|
| `dotnet build api/MicrogridApi` | Passed (0 errors) |
| `dotnet test api/MicrogridApi.Tests` | **Passed: 16**, Failed: 0 |

Covered by unit tests:

- Malformed / empty QR reference
- Missing reservation
- Pending / cancelled / completed rejection on verify
- Successful verify for approved + active station
- Verify does not call complete
- Idempotent complete when already completed
- Complete persists server operator identity
- Concurrent complete race → alreadyCompleted
- Cancelled between verify and complete
- QR reuse + non-owner prosumer rejection
- JWT secret requirement + role/sub claims

## Android build

| Command | Result |
|---------|--------|
| `cd android; .\gradlew.bat assembleDebug` (JDK 17) | **BUILD SUCCESSFUL** |

## Manual device checklist (you run)

- [ ] Create GridOperator user via existing users API / Backoffice
- [ ] API running; Android `API_BASE_URL` correct (emulator `10.0.2.2:5148`)
- [ ] `POST /api/dev/suwani-fixtures/approved-reservation` (Development) → note `reservationId` + `transactionReference`
- [ ] Prosumer (or operator) opens QR display for `reservationId` — QR encodes opaque reference only
- [ ] Operator scans QR with camera — verification details match API
- [ ] Confirm complete once — status `completed`
- [ ] Scan/complete again — `alreadyCompleted`, no duplicate side effects
- [ ] Cancelled fixture reservation rejected on verify/complete
- [ ] Deny camera — recovery UI works
- [ ] Deny location — map still shows stations
- [ ] Maps markers match API lat/lng (requires Maps API key)
- [ ] Logout clears session; expired JWT prompts re-login

## Screenshot list (assignment)

1. Login  
2. Operator home  
3. QR display  
4. Camera scanner  
5. Verification details  
6. Completion success  
7. Stations map + marker detail  

Do not invent screenshots — capture after real runs.

## End-to-end flow

Approved reservation → QR retrieve/display → operator scan → server verify → confirm → atomic complete → reservation status `completed` (history/dashboard refresh depends on Nethasa/Viman readers).
