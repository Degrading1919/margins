# Margins First-Store Local Unity Evidence Record v0.1

## Record status

- **Status:** Executed; adjust recommended before merge
- **Execution status:** Corrected first-store built-player input source, automated suites, and Windows build rerun on 2026-07-29; executable interaction rerun awaits dismissal of the Windows Security firewall prompt recorded below
- **Implementation marker:** Draft implementation — corrected PR #15 commit `3c99ad62307f9f9821a7be28a08ff7020240fcfb`; rebased PR #16 source head `0e4be4773f6a3d1e1d216f2d8b6e89474808c8c9`
- **Packet:** `Margins_First_Store_Local_Unity_Verification_Packet_v0.1.md`

Leave a field as `not run`, `blocked`, or `not applicable` rather than inferring a
pass. Link or attach durable evidence for every pass or failure.

## Exact source and environment

| Field | Required record |
|---|---|
| Exact tested commit (`git rev-parse HEAD`) | `0e4be4773f6a3d1e1d216f2d8b6e89474808c8c9` |
| Test date/time and timezone | 2026-07-29 08:56–09:00 EDT (`UTC-04:00`) |
| Tester | Codex, Technical Architect primary; Data/Validation/QA secondary evidence lens |
| OS and version | Microsoft Windows 11 Home `10.0.26200`, build `26200` |
| CPU / RAM / GPU | AMD Ryzen 9 270 with Radeon 780M; 31.3 GiB RAM; Radeon 780M and NVIDIA RTX 5070 Laptop GPU |
| Unity editor version | `6000.5.5f1` (`d16e074b49fd`) |
| Render pipeline package | `com.unity.render-pipelines.universal` `17.5.0` |
| Input System package | `com.unity.inputsystem` `1.20.0` |
| AI Navigation package | `com.unity.ai.navigation` `2.0.14` |
| Test Framework package | `com.unity.test-framework` `1.7.0` |
| Full package manifest/lock evidence | `CODE/Unity/Margins/Packages/manifest.json` and `packages-lock.json`; unchanged baseline |
| Draft PR base/head comparisons | Every branch confirmed zero commits behind its intended base after refresh; final hashes/counts recorded in PR descriptions |

## Import and compilation

| Check | Result | Evidence / exact message |
|---|---|---|
| Initial import completed | Pass | Editor opened the project and became idle in exact Unity `6000.5.5f1` |
| `.meta` reconciliation | Pass | Unity-created scene/content `.meta` files imported; no unexpected reconciliation remained |
| New or modified serialized files | Pass | Unity serialized the explicit `FirstStoreInteractionController`, camera, stocking, and first-person references in `FirstStoreValidation.unity`; no serialized YAML hand-authored |
| Import warnings | Pass | Editor Console 0 warnings |
| Import errors | Pass | Editor Console 0 errors |
| C# compile result | Pass | Unity `6000.5.5f1`; 0 compiler errors and 0 compiler warnings in both complete-suite logs |
| Final Console error count | Pass | 0 |
| Final Console warning count | Pass | 0 |

## Automated test evidence

| Suite | Exact filter | Passed | Failed | Skipped/Inconclusive | Duration | Result artifact / screenshot |
|---|---|---:|---:|---:|---:|---|
| Complete EditMode | all EditMode tests | 42 | 0 | 0 | 0.1708064 s | `CODE/Unity/Margins/Logs/pr16-pre-editmode.xml` |
| First-store domain EditMode | `Margins.Tests.FirstStoreDomainEditModeTests` | 22 | 0 | 0 | 0.015804 s | same XML |
| First-store adapter EditMode | `Margins.Tests.FirstStoreAdapterEditModeTests` | 13 | 0 | 0 | 0.116383 s | same XML |
| Complete PlayMode | all PlayMode tests | 12 | 0 | 0 | 3.0179807 s | `CODE/Unity/Margins/Logs/pr16-pre-playmode.xml` |
| First-store input PlayMode | `Margins.Tests.FirstStoreInputPlayModeTests` | 6 | 0 | 0 | 1.305969 s | same XML |

Focused evidence must identify the exact tests or manual steps proving checkout
line mutation resistance, two completed transactions, duplicate transaction-ID
rejection, deterministic ledger totals, COGS, contribution, repeated restore
without revenue/stock replay, mixed-product delivery, product-specific shelf
consumption, repeated distinct physical units, failed-placement rollback, and
required-fixture restrictions.

For every failure, record the full test name, stack trace, reproduction frequency,
owning branch, defect ID, and rerun result. This table contains the actual local
results for the source-authored fixture-presentation correction tests.

## Scene and inspector evidence

| Configuration | Stable IDs / object names | Result | Evidence |
|---|---|---|---|
| Inventory authority and locations | `loc-delivery`, `loc-loose`, `loc-held`, `loc-shelf-cola`, `loc-shelf-chips` | Pass | One explicitly referenced inventory authority; capacities 16/16/1/4/4 |
| Product definitions | `prod-cola-can-355ml`, `prod-potato-chips-small` | Pass | Unity-created validation ProductDefinition assets |
| Player mode and first-store interaction | `Validation Player`, `Validation Camera`, `FirstPersonController`, `FirstStoreInteractionController` | Pass | Explicit references; no validation-scene `ProductInteraction`; focused cursor, look, target, scroll, HUD suppression, and stocking tests passed |
| Fixture grid and required fixtures | `fixture-checkout-essential-01`; two shelf fixtures | Pass | Explicit scene references; placement and restriction checks passed |
| Mixed-product delivery container | `container-mixed-starter-01`; both products; 4 units each | Pass | Explicit container/inventory/loose/physical references |
| Physical-unit prefabs, spawns, and reconciliation | Cola/chips validation prefabs | Pass | Distinct runtime IDs `physical-unit-000001` through `000006`; repeated restore reconciled |
| Product-specific shelves, hold point, and snap points | Product-specific shelf locations; four snap IDs each | Pass | Cola and chips stocked and consumed from their mapped shelves |
| Checkout shelf/price/unit-cost mappings and ledger capacity | Cola 149/70 cents; chips 199/80 cents; bounded ledger | Pass | Valid mapping initialized; duplicate/non-shelf mappings rejected in tests |
| Cleaning task | `task-floor-spill-01`, progress 4 | Pass | Manual 1/4 through 4/4, then idempotent fifth action |
| Store session/controller | `session-first-store-validation-001`; 90-cent expense | Pass | Preparing/Open/Closing/ClosedWithResultPending executed |
| First-store mapper | Same explicit domain/adapter instances | Pass | Two manual restores plus automated no-replay reconciliation |
| Existing foundation placement save | Unchanged foundation contract | Pass | Foundation suites passed; no replacement envelope added |

Record every Unity-created or modified scene, prefab, ScriptableObject asset, and
`.meta` file. Do not mark this section complete from source inspection alone.

## Manual interaction results

| Interaction / scenario | Expected result | Result | Evidence / defect |
|---|---|---|---|
| Owner built-player baseline before correction | WASD, mouse look, and `E` pickup work | Failed | Owner reported WASD **pass**, mouse look **failed**, and `E` pickup **failed**. |
| Gameplay cursor, mouse look, and `Tab` HUD switching | Gameplay locks cursor and looks; `Tab` unlocks for HUD and relocks on return | Automated pass; executable rerun pending | `GameplayStartsLockedAndTabTogglesHudMode` and `LockedCursorMouseInputRotatesPlayerAndCamera` passed; final player is behind a Windows Security firewall prompt. |
| Targeted `E` pickup | Exact raycast target moves loose→held in both physical and domain state | Automated pass; executable rerun pending | Exact-target and rejection-conservation EditMode/PlayMode tests passed. |
| Mouse-wheel held rotation | Up/down change opposite single quarter turns, wrap 0–3, and update the held visual | Automated pass; executable rerun pending | `ScrollDirectionsAreOppositeAndQuarterTurnsWrap` passed. |
| HUD input suppression | HUD mode does not trigger world pickup, stocking, or product rotation | Automated pass; executable rerun pending | `HudModeSuppressesPickupStockingAndRotation` passed. |
| `E` stocking | Accepted held orientation reaches the existing domain-safe stocking path without unit loss | Automated pass; executable rerun pending | `EStocksAcceptedRotationAndPreservesConservation` and failed-stocking conservation checks passed. |
| `fs-accept-014` invalid/duplicate IDs | Blocked with bounded diagnostic and no partial state | Pass | Duplicate checkout begin rejected with no active session or stock mutation; configuration tests passed. |
| `fs-accept-001` fixture placement | In-bounds, deterministic footprint/orientation | Pass | Required checkout fixture placed; deterministic placement tests passed. |
| `fs-accept-002` overlap/move rejection | Prior accepted placement preserved | Pass | Open/Closing move rejected manually; overlap/prior-state tests passed. |
| Fixture remove and recovery | Domain and visible state agree; no orphan occupancy | Pass | In the Windows player, successful removal returned `None` and hid the fixture; immediate re-placement returned `None` and restored it. `FixtureRemovalClearsDomainAndPresentationAndAllowsReplacement` passed. |
| Fixture snapshot omission/inclusion | Restore resets all configured fixtures; omitted stays unplaced; included restores normally | Pass | Empty snapshot → place → restore hid the fixture; placed snapshot → remove → restore reinstated it. Both focused restore tests passed. |
| `fs-accept-003` receiving | Sealed reject; open then conserved box-to-loose transfer | Partial manual / automated pass | Mixed box opened manually; sealed rejection and conservation covered in automated tests. |
| Mixed-product removal | Requested cola/chips unit removed; invalid/exhausted request changes nothing | Pass | Four cola and two chips removed; fifth cola request rejected `InsufficientQuantity` without a new unit. |
| `fs-accept-004` valid stocking | Loose-to-held-to-shelf moves exactly one unit and one visible item | Pass | Two cola and two chips stocked through held state to product-specific shelves. |
| Repeated physical units | Repeated removals create distinct visible units and stock distinct snap points | Pass | Six distinct IDs materialized; four distinct units stocked at separate snap points. |
| `fs-accept-005` invalid stocking | Quantity, held state, and occupancy remain unchanged | Automated pass | Adapter EditMode covers invalid placement atomically. |
| Physical placement rollback | Failed placement retains exactly one held domain/visible unit | Automated pass | `FailedPhysicalPlacementKeepsOneHeldUnitWithoutDuplication` passed. |
| Product-specific shelf consumption | Each checkout product consumes only its mapped shelf location | Pass | Cola sale removed cola shelf unit; chips sale removed chips shelf unit. |
| Checkout-line mutation resistance | External mutation cannot alter active product, price, or quantity | Automated pass | `CheckoutLineExposureCannotMutateActiveState` passed. |
| `fs-accept-006` checkout | At least two transactions complete with exact integer-cent subtotals | Pass | `manual-transaction-001` $1.49 and `manual-transaction-002` $1.99 completed. |
| Duplicate transaction ID | Rejected before ledger, stock, or physical-unit mutation | Pass | Replay rejected before session start; a later unique sale completed, proving recovery. |
| Ledger/result reconciliation | Gross, COGS, expenses, contribution, units, and transaction count reconcile | Pass | Restored two-entry ledger derived gross $3.48, COGS $1.50, expense $0.90, contribution $1.08, 2 units, 2 transactions; exact automated assertion passed. |
| `fs-accept-007` repeat completion | Same summary; no second revenue or stock consumption | Pass | Replay changed neither ledger nor inventory and did not wedge checkout. |
| `fs-accept-008` cleaning | Progress clamps and completion is idempotent | Pass | `Progressed` 1/4–3/4, `Completed` 4/4, then `AlreadyComplete` 4/4. |
| `fs-accept-009` opening prerequisites | Invalid open rejected with actionable explanation | Pass | Open rejected until the required fixture and shelf stock existed. |
| `fs-accept-010` close/result | Valid sequence and totals retained until acknowledgement | Pass | Open → Closing → ClosedWithResultPending completed after cleaning. |
| Required fixture move/remove while open | Both operations rejected; accepted placement preserved | Pass | Both returned `OperatingStateRestricted`. |
| Required fixture move/remove while closing | Both operations rejected; accepted placement preserved | Pass | Both returned `OperatingStateRestricted`. |
| In-memory physical/domain restore | Physical counts/placements equal inventory after repeated restore; no replay | Pass | Fixture omission/inclusion was exercised directly; prior repeated restore evidence returned ledger, inventory, and visible shelf state to the same two-sale snapshot. |
| `fs-accept-011` full disk persistence | Save/exit/reload equality | Blocked pending persistence decision | No approved first-store disk persistence contract exists. |
| `fs-accept-012` E1 arithmetic | `$10,000-$3,000-$2,250-$1,100-$205+$500=$3,945`; contribution `$10` | Pass | Independently recomputed; proposed tuning remains unapproved. |
| `fs-accept-012` E2 arithmetic | `$10,000-$3,000-$2,250-$1,100-$205+$300=$3,745`; contribution `-$85` | Pass | Independently recomputed; proposed tuning remains unapproved. |
| `fs-accept-012` E3 arithmetic | `$8,000-$3,000-$2,250-$1,800-$205+$500=$1,245`; contribution `$10` | Pass | Independently recomputed; proposed tuning remains unapproved. |
| `fs-accept-013` recovery | Weak result explained; continuation remains possible | Not run | Proposed design scenario was not separately exercised in the executable. |

## Save-file and restoration evidence

| Field | Required record |
|---|---|
| Owner-approved first-store save disposition | Blocked — no decision exists |
| First-store save path | Not applicable; deliberately not implemented |
| Foundation placement sidecar path | Existing foundation behavior unchanged; not used as a first-store substitute |
| Save action and timestamp | Blocked |
| File existence, size, and hash before exit | Blocked |
| Sanitized save excerpt or attached file | Blocked |
| Full process/editor exit evidence | Player exited cleanly; no first-store disk save was claimed |
| Reload action and timestamp | Blocked |
| File size and hash after reload | Blocked |
| Fixture-placement equality | Pass at the approved in-memory boundary |
| Inventory per-location equality | Pass at the approved in-memory boundary |
| Delivery-container state equality | Pass at the approved in-memory boundary |
| Completed-transaction ledger equality / no replay | Pass after two repeated manual restores and automated reconciliation |
| Cleaning-state equality | Pass at the approved in-memory boundary |
| Operating-state and totals equality | Pass at the approved in-memory boundary |
| Distinct physical-unit / shelf-placement reconciliation | Pass at the approved in-memory boundary |
| Rejected or repaired records | Contradictory totals rejected by focused domain test |
| Future historical COGS requirement | Completed transaction lines must preserve unit cost at sale so later cost changes cannot alter historical COGS. |

If only the in-memory mapper round trip is available, record that result under
automated tests and leave the full restart result blocked.

## Windows x64 development-build evidence

| Field | Required record |
|---|---|
| Build target and architecture | Windows x64 |
| Build configuration | Standard Windows x64 player (`BuildOptions.None`) |
| Exact output path | `C:/Users/CK/Documents/margins/CODE/Unity/Margins/Builds/FirstStoreValidation/MarginsFirstStoreValidation.exe` |
| Build start/end time | 2026-07-29 09:08–09:09 EDT |
| Build result | Pass — `Build Finished, Result: Success.` |
| Build warnings/errors | No build failure; 0 C# compiler warnings / 0 C# compiler errors |
| Executable and data-folder sizes | EXE 667,648 bytes; complete build 176 files / 102,901,168 bytes |
| Executable hash | SHA-256 `C7E7C0FB799BC979D2A086D8836D9570443F925C753442C2979FAC9E38D69C13` |
| Launch result | Blocked after process launch — the Windows Security firewall prompt created by the earlier development-player launch remains over the standard player; automation is not permitted to act on Windows Security |
| Manual controls result | Not run on this build pending manual dismissal of the Windows Security prompt |
| Clean exit result | Pending executable interaction rerun |
| `Player.log` path | `C:/Users/CK/AppData/LocalLow/DefaultCompany/Margins/Player.log` |
| `Player.log` findings | Pending executable interaction rerun and clean exit |

## Defect log

| Defect ID | Severity | Area / scenario | Reproduction | Expected | Actual | Owning branch | Correction commit | Rerun evidence | Status |
|---|---|---|---|---|---|---|---|---|---|
| `FSV-001` | major | Duplicate checkout transaction | Complete two sales, begin the second ID again, then try a unique sale | Replay rejected without opening a session; unique sale remains possible | Replay rejected only at completion and left an active session, wedging later checkout | PR #15 | `9f84800` | EditMode 38/38, PlayMode 6/6; manual replay rejected at begin and transaction 003 then completed | Resolved |
| `FSV-002` | major | Fixture removal and snapshot presentation | Remove a placed fixture; separately restore a snapshot that omits a currently placed fixture | Occupancy and presentation both clear; omitted fixture remains unplaced; fixture can be re-placed | Domain occupancy cleared, but the visible fixture remained; omitted restore left stale visible placement | PR #15 | `a49bc2a` | EditMode 41/41, PlayMode 6/6; Windows player remove/re-place and omitted/included restore passed | Resolved |
| `FSV-003` | major | Built-player cursor and first-store interaction | At prior PR #16 head `77b3212`, launch the player, move with WASD, move the mouse, and press `E` while targeting a loose product | WASD moves; mouse controls look; `E` transfers the exact target through authoritative inventory | Owner result: WASD passed; mouse look failed because the validation controller permanently unlocked the cursor; `E` pickup failed because no domain-safe world interaction path existed | PR #15 | `3c99ad6` | EditMode 42/42 and PlayMode 12/12, including six focused first-store input tests; Windows build passed | Correction verified automatically; executable rerun pending |
| `FSV-OBS-002` | observation | Full disk restart | Request first-store disk persistence without an approved contract | Approved save boundary exists first | No approved first-store disk contract | Owner decision | none | In-memory restore passed | Blocked by decision |

Severity must be one of `blocker`, `major`, `minor`, or `observation`, using the
definitions in the verification packet.

## Corrections made during local verification

| Order | Defect ID | Files/assets changed through approved workflow | Reason | Owning PR updated | Descendants refreshed | Verification restarted at step |
|---:|---|---|---|---|---|---:|
| 1 | `FSV-001` | `CheckoutStationComponent.cs` and focused adapter test | Reject completed transaction IDs before creating an active session | #15 at `9f84800` | #16 rebased/force-with-lease updated | 5 |
| 2 | `FSV-002` | `FixturePlacementController.cs`, `PlaceableFixtureComponent.cs`, and focused adapter tests | Clear/re-activate fixture presentation and reset all configured fixtures before applying restore | #15 at `a49bc2a` | #16 rebased onto the correction | 5 |
| 3 | `FSV-003` | `FirstPersonController.cs`, narrow `FirstStoreInteractionController.cs`, targeted `StockingController` overload, `ProductItem.cs`, Unity-serialized validation scene, and focused tests | Restore locked-cursor look and route exact-target pickup/held rotation/stocking through first-store authority while suppressing HUD leakage | #15 at `3c99ad6` | #16 rebased; obsolete #16 cursor-unlock commit dropped | 5 |

## Final recommendation

Select exactly one after all required evidence and reruns are complete.

- [ ] **Continue** — no unresolved blocker or major defect; proposed owner choices
  needed by this proof are approved; required evidence is complete.
- [x] **Adjust** — the direction remains viable, but named corrections or owner
  decisions are required before merge.
- [ ] **Stop** — a blocker, invalid assumption, or unacceptable scope/risk requires
  the stack to pause.

**Recommendation rationale:** The corrected implementation compiles, all 54 Unity
tests pass, and the standard Windows x64 player build succeeds. The owner-reported
cursor and `E` failures are covered by focused automated correction evidence, but
the final executable interaction rerun remains pending behind the Windows Security
prompt. Merge should also wait for project-owner disposition of proposed
tuning/content and an explicit persistence decision if disk restart is required.

**Unresolved owner decisions:** Proposed tuning/content approval; first-store disk
persistence boundary.

**Unresolved blocker/major defects:** `FSV-003` remains open until the corrected
Windows player is manually exercised after the Windows Security prompt is
dismissed.

**Project-owner reviewer and date:**
