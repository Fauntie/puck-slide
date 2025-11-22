# Steam release verification checklist

## Achievements and overlay
- [x] Wire lobby host/join/ready flows into Steam achievements and stats through `SteamPlatformService` so first-time actions are tracked.
- [x] Trigger Steam overlay invite dialogs from the lobby invite button when a Steam lobby is available; fall back to the friends overlay when no lobby exists.
- [ ] Review in-game achievement IDs/strings in Steamworks to ensure they match `ACH_SESSION_HOST`, `ACH_SESSION_JOIN`, and `ACH_READY_UP`.
- [ ] Validate overlay surfaces (friends, invites, achievements) in a Steam-enabled build.

## Steam Cloud and platform builds
- [ ] Confirm save data paths under `Application.persistentDataPath` are registered in Steam Cloud and available for Windows/macOS/Linux depots.
- [ ] Validate depot manifests and platform icons/descriptions are present for every target build.
- [ ] Exercise Steam Cloud conflict resolution once per platform to confirm uploads/downloads succeed.

## Metadata and compliance
- [ ] Re-run compliance checklist: ratings links, EULA, privacy policy, and support URLs accessible from the main menu and store page.
- [ ] Verify store metadata (capsule images, screenshots, tags, and description) matches the current build number shown in the lobby UI.
- [ ] Ensure GDPR/CCPA toggles (telemetry/crash reports) remain opt-in and reflected in crash/metrics services.

## Load and network soak tests
- [ ] Run automated lobby churn test for 60+ minutes using quickplay to monitor relay stability and packet loss.
- [ ] Execute match-state soak test with repeated ready/start/reset cycles to validate resilience during reconnections.
- [ ] Capture network diagnostics logs from `NetworkStatusHUD` and `ResilientSessionManager` for postmortem review.
