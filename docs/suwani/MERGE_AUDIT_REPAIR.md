# Merge audit repair notes (uncommitted)

## Merge identity
- Merge commit: `84a221e`
- First parent (Suwani): `8964e7e`
- Second parent (origin/dev): `3c520bb`
- Local `dev` tip may still be older (`9a4c2f9`); remote `origin/dev` is `3c520bb`.

## Already preserved from origin/dev (verified in tree)
- `8019fc5` email/NIC prosumer login in `AuthController`
- `LoginResponse.Nic`
- `PATCH /api/prosumers/{nic}/change-password`
- Compose screens: Login/Register/Profile/MyProfile/ChangePassword/Notifications/Help/About
- Profile photo picker (`GetContent` + Coil) in `ProfileScreen`

## Gaps found and repaired (working tree, not committed)
1. Dual launchers hid integration — replaced with **single** `LoginActivity` launcher.
2. Prosumer path now opens `MainActivity` profile hub (photo/change-password/menu).
3. Profile menu adds **Reservation QR** + **Stations map** → Suwani activities.
4. Compose login now saves JWT into Suwani `SessionManager` so QR APIs keep working.
5. Compose `RetrofitClient` uses `BuildConfig.API_BASE_URL` (was a hard-coded LAN IP).
6. Register reachable from Suwani login via **Register prosumer**.

## applicationId (Maps)
- release: `com.smartsolar.microgrid`
- debug: `com.smartsolar.microgrid.debug`
