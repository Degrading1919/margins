# Margins First-Store Local Unity Evidence Record v0.1

## Record status

- **Status:** Automated implementation evidence complete; owner playthrough remains required.
- **Exact tested implementation commit:** `96d7b6c0039f55cd3934a6ecf84322966f3550dd`
- **Test window:** 2026-07-29 14:22-14:26 EDT (`UTC-04:00`)
- **Primary role:** Technical Architect; Producer sequencing and Data/Validation/QA evidence lenses.
- **Merge disposition:** All implementation PRs remain draft and unmerged.
- **Manual disposition:** `FSV-003` remains open until the owner completes the current-build checklist.

The owner-authorized JSON file is a temporary, isolated first-store vertical-slice
implementation. This record does not approve the production save architecture,
migration policy, save-slot UI, cloud storage, or long-term envelope.

## Draft PR stack

| PR | Branch | Target | Tested implementation commit | Status |
|---|---|---|---|---|
| #17 | `agent/implement-first-store-world-interactions` | `agent/prepare-first-store-local-validation` | `db7c3bed7eed62b7541acbe28fc34191be9c386d` | Draft, unmerged |
| #18 | `agent/implement-first-store-fixture-placement-mode` | `agent/implement-first-store-world-interactions` | `a0c2246aa8265c5c2a9da2192b049a81f11114c5` | Draft, unmerged |
| #19 | `agent/implement-first-store-disk-persistence` | `agent/implement-first-store-fixture-placement-mode` | `1273dc800a4d11fc88b37c1a81a0559ebc5584dc` | Draft, unmerged |
| #20 | `agent/validate-first-store-playable-loop` | `agent/implement-first-store-disk-persistence` | `96d7b6c0039f55cd3934a6ecf84322966f3550dd` | Draft, unmerged |

## Environment and compilation

| Field | Result |
|---|---|
| OS | Windows 11 `10.0.26200`, x64 |
| Unity | `6000.5.5f1` (`d16e074b49fd`) |
| Final C# compilation | Pass; no compiler error in the focused correction, complete-suite, or successful-build logs |
| Scene load and script wiring | Pass; `FirstStoreValidation` loaded throughout PlayMode validation with explicit persistence/transient-reset references |
| Foundation sidecar | Unchanged and isolated; Foundation suites remained green |

## Focused implementation evidence

Focused counts overlap and are not cumulative unique-test counts.

| Stage | EditMode | PlayMode | Additional evidence |
|---|---:|---:|---|
| Task 1 - world interactions | 28/28 | 14/14 | Scene cleanup smoke 1/1 |
| Task 2 - fixture placement mode | 52/52 | 17/17 | Prompt/compile smoke 1/1 |
| Task 3 - disk persistence | 56/56 | 5/5 | 25 scene script GUIDs, 0 missing |
| Task 4 - review corrections | 34/34 | 13/13 | Checkout/fixture load recovery and sealed-content prompt regressions included |

Task 3 focused coverage includes full file and player-transform round trips,
historical sale-time unit cost, later configured-cost changes, malformed and
unsupported files, interrupted-write preservation, repeated load without replay,
invalid physical reconciliation without partial mutation, and save rejection while
holding or during an incomplete checkout.

## Final complete Unity suites

| Suite | Result | Passed | Failed | Skipped | Duration | Local result artifact |
|---|---|---:|---:|---:|---:|---|
| All EditMode | Pass | 80 | 0 | 0 | 0.256168 s | `CODE/Unity/Margins/Logs/task4-final-edit.xml` |
| All PlayMode | Pass | 30 | 0 | 0 | 6.2320187 s | `CODE/Unity/Margins/Logs/task4-final-play.xml` |

EditMode fixtures: first-person snapshot 3, first-store adapter 22, first-store
domain 31, fixture-placement mode 7, world-target selection 5, staged checkout 5,
and Foundation 7.

PlayMode fixtures: first-store adapter 2, disk persistence 7, fixture placement 5,
input 6, world interaction 6, Foundation input 2, and Foundation scene 2.

## Windows x64 development build

| Field | Result |
|---|---|
| Target | Windows x64, `BuildOptions.Development`, first-store validation scene only |
| Output | `CODE/Unity/Margins/Builds/FirstStoreValidation/MarginsFirstStoreValidation.exe` |
| Successful build window | 2026-07-29 14:25:06-14:25:28 EDT |
| Unity build result | Pass - `Build Finished, Result: Success.` |
| Build report | 167,718,098 bytes in 13.6700081 s |
| Output inspection | 285 files, 167,926,640 bytes on disk; Burst debug-information folder present |
| Executable | 667,648 bytes |
| Executable SHA-256 | `6456584D36A1ED8F5A4662A524F11BF1DA4C580870AA6257E4CF086300F4F615` |
| Development marker | `player-connection-mode=Listen` in generated `boot.config` |
| Committed generated output | None; `Builds/` and local `Logs/` remain ignored |

The first build attempt reached player post-processing but could not copy
`UnityCrashHandler64.exe` and `UnityPlayer.dll` because a prior first-store player
and crash handler still held the old output. Only those two project processes were
terminated. No Windows Security dialog interaction occurred. Source remained
unchanged, and the required development build then succeeded.

## Startup-log inspection

- The current development player was **not launched**.
- Existing `Player.log` and `Player-prev.log` were from 09:16 and 09:10 EDT,
  before the current build.
- Both prior logs reached Unity `6000.5.5f1`, Direct3D 12, Input System
  initialization, and `Initialized with explicit first-store references.`
- No exception or crash entry was present in the inspected prior startup logs.
- Those prior logs are not evidence of current-build manual input or save/relaunch
  success.

## Defects found and corrected in this pass

| ID | Severity | Concrete defect | Correction and evidence | Status |
|---|---|---|---|---|
| `FSV-004` | major | Loading an accepted save cleared checkout authority but retained staged basket/session transients, leaving later `E` scans pointed at a removed active session. | Successful load now clears scan/session state and derives the first incomplete basket from the restored completed ledger; focused EditMode and PlayMode recovery tests pass. | Resolved at `96d7b6c` |
| `FSV-005` | major | A fixture placement preview survived load and could confirm or cancel stale pre-load presentation against restored state. | Successful load reapplies the restored authoritative placement, cancels the preview session, and clears focus; focused EditMode and PlayMode recovery tests pass. | Resolved at `96d7b6c` |
| `FSV-006` | minor | Explicit delivery-content targets were excluded while sealed, hiding the product/remaining-count sealed blocker prompt. | Configured content targets remain targetable while sealed and reject without mutation; focused PlayMode prompt/conservation test passes. | Resolved at `96d7b6c` |

Earlier implementation corrections retained in the tested stack include Unity
component serialization/scene wiring, stale fixture-preview confirmation,
operating-state fixture restrictions, removal presentation, and JsonUtility's
all-zero placeholder for an absent result total. All have regression coverage in
the complete suites.

## Blocked or manually unverified

| Item | Status |
|---|---|
| `FSV-003` WASD, mouse look, exact-target `E`, and HUD switching in the current player | Owner playthrough required; automated coverage passes |
| Complete world loop in the current player | Owner playthrough required |
| `F5` save, process exit, relaunch, `F9` load, and visual/state comparison | Owner playthrough required |
| Windows Security firewall dialog | Not dismissed, bypassed, or otherwise operated by Codex |

Use `Margins_First_Store_Owner_Playable_Loop_Checklist_v0.1.md` for the one
remaining owner pass. Production persistence architecture and operational
time/overnight reset remain separate future decisions.
