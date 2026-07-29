# Margins First-Store Local Unity Evidence Record v0.1

## Record status

- **Status:** Executed; adjust recommended before merge
- **Execution status:** Local Unity correction and verification completed on 2026-07-29 with the limitations and persistence block recorded below
- **Implementation marker:** Draft implementation — Unity-verified at source commit `72e3463a40bce75c99ab42f83a5bbd92e79fe47e`; documentation-only descendant rerun required before handoff
- **Packet:** `Margins_First_Store_Local_Unity_Verification_Packet_v0.1.md`

Leave a field as `not run`, `blocked`, or `not applicable` rather than inferring a
pass. Link or attach durable evidence for every pass or failure.

## Exact source and environment

| Field | Required record |
|---|---|
| Exact tested commit (`git rev-parse HEAD`) | `72e3463a40bce75c99ab42f83a5bbd92e79fe47e` |
| Test date/time and timezone | 2026-07-29 01:37–01:52 EDT (`UTC-04:00`) |
| Tester | Codex, Technical Architect primary; Producer/Roadmap sequencing; Data/Validation/QA evidence |
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
| New or modified serialized files | Pass | Unity-created validation scene, two ProductDefinitions, two prefabs, and validation materials; no serialized YAML hand-authored |
| Import warnings | Pass | Editor Console 0 warnings |
| Import errors | Pass | Editor Console 0 errors |
| C# compile result | Pass | `CODE/Unity/Margins/Logs/pr16-post-fix-compile.log` |
| Final Console error count | Pass | 0 |
| Final Console warning count | Pass | 0 |

## Automated test evidence

| Suite | Exact filter | Passed | Failed | Skipped/Inconclusive | Duration | Result artifact / screenshot |
|---|---|---:|---:|---:|---:|---|
| Complete EditMode | all EditMode tests | 38 | 0 | 0 | 0.2276881 s | `CODE/Unity/Margins/Logs/pr16-post-fix-editmode-results.xml` |
| First-store domain EditMode | `Margins.Tests.FirstStoreDomainEditModeTests` | 22 | 0 | 0 | 0.024612 s | same XML |
| First-store adapter EditMode | `Margins.Tests.FirstStoreAdapterEditModeTests` | 9 | 0 | 0 | 0.149647 s | same XML |
| Complete PlayMode | all PlayMode tests | 6 | 0 | 0 | 1.6256942 s | `CODE/Unity/Margins/Logs/pr16-post-fix-playmode-results.xml` |

Focused evidence must identify the exact tests or manual steps proving checkout
line mutation resistance, two completed transactions, duplicate transaction-ID
rejection, deterministic ledger totals, COGS, contribution, repeated restore
without revenue/stock replay, mixed-product delivery, product-specific shelf
consumption, repeated distinct physical units, failed-placement rollback, and
required-fixture restrictions.

For every failure, record the full test name, stack trace, reproduction frequency,
owning branch, defect ID, and rerun result. The source-authored tests remain
Unity-unverified until this table contains actual local results.

## Scene and inspector evidence

| Configuration | Stable IDs / object names | Result | Evidence |
|---|---|---|---|
| Inventory authority and locations | `loc-delivery`, `loc-loose`, `loc-held`, `loc-shelf-cola`, `loc-shelf-chips` | Pass | One explicitly referenced inventory authority; capacities 16/16/1/4/4 |
| Product definitions | `prod-cola-can-355ml`, `prod-potato-chips-small` | Pass | Unity-created validation ProductDefinition assets |
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
| First-person move and look | Existing foundation controls remain usable | Partial | Mouse look changed the executable view. UI automation could not sustain held-key translation; the dedicated PlayMode movement test passed. |
| Foundation pickup, rotate, release | Existing product behavior remains compatible | Automated pass; direct manual not completed | Foundation runtime-loop PlayMode test passed; legacy pickup/rotate/release was not directly exercised. |
| `fs-accept-014` invalid/duplicate IDs | Blocked with bounded diagnostic and no partial state | Pass | Duplicate checkout begin rejected with no active session or stock mutation; configuration tests passed. |
| `fs-accept-001` fixture placement | In-bounds, deterministic footprint/orientation | Pass | Required checkout fixture placed; deterministic placement tests passed. |
| `fs-accept-002` overlap/move rejection | Prior accepted placement preserved | Pass | Open/Closing move rejected manually; overlap/prior-state tests passed. |
| Fixture remove and recovery | Domain and visible state agree; no orphan occupancy | Automated pass; direct recovery not completed | Open/Closing removal rejected manually; domain removal/recovery covered in EditMode. |
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
| In-memory physical/domain restore | Physical counts/placements equal inventory after repeated restore; no replay | Pass | A third post-snapshot sale mutated state; two restores returned ledger, inventory, and visible shelf state to the same two-sale snapshot. |
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

If only the in-memory mapper round trip is available, record that result under
automated tests and leave the full restart result blocked.

## Windows x64 development-build evidence

| Field | Required record |
|---|---|
| Build target and architecture | Windows x64 |
| Development build settings | Development build enabled by the existing validation build method |
| Exact output path | `C:/Users/CK/Documents/margins/CODE/Unity/Margins/Builds/FirstStoreValidation/MarginsFirstStoreValidation.exe` |
| Build start/end time | 2026-07-29 01:39:45–01:40:06 EDT |
| Build result | Pass — `Build Finished, Result: Success.` |
| Build warnings/errors | No build failure; final editor Console 0 warnings / 0 errors |
| Executable and data-folder sizes | EXE 667,648 bytes; complete build 178 files / 102,924,832 bytes |
| Executable hash | SHA-256 `C7E7C0FB799BC979D2A086D8836D9570443F925C753442C2979FAC9E38D69C13` |
| Launch result | Pass — exact Unity `6000.5.5f1` player launched |
| Manual controls result | Feature-validation mouse controls and mouse look passed; held-key translation and direct legacy pickup remained unverified |
| Clean exit result | Pass — normal Input System and physics shutdown logged |
| `Player.log` path | `C:/Users/CK/AppData/LocalLow/DefaultCompany/Margins/Player.log` |
| `Player.log` findings | 0 warning/error/exception entries. Non-fatal D3D12 info-queue diagnostic only. |

## Defect log

| Defect ID | Severity | Area / scenario | Reproduction | Expected | Actual | Owning branch | Correction commit | Rerun evidence | Status |
|---|---|---|---|---|---|---|---|---|---|
| `FSV-001` | major | Duplicate checkout transaction | Complete two sales, begin the second ID again, then try a unique sale | Replay rejected without opening a session; unique sale remains possible | Replay rejected only at completion and left an active session, wedging later checkout | PR #15 | `9f84800` | EditMode 38/38, PlayMode 6/6; manual replay rejected at begin and transaction 003 then completed | Resolved |
| `FSV-OBS-001` | observation | Direct executable input evidence | UI automation emits taps, not sustained key state | Directly observe translation and legacy pickup/rotate/release | Mouse look observed; translation and legacy pickup not directly completed | PR #16 evidence | none | Foundation input/interaction PlayMode tests 4/4 | Open evidence gap |
| `FSV-OBS-002` | observation | Full disk restart | Request first-store disk persistence without an approved contract | Approved save boundary exists first | No approved first-store disk contract | Owner decision | none | In-memory restore passed | Blocked by decision |

Severity must be one of `blocker`, `major`, `minor`, or `observation`, using the
definitions in the verification packet.

## Corrections made during local verification

| Order | Defect ID | Files/assets changed through approved workflow | Reason | Owning PR updated | Descendants refreshed | Verification restarted at step |
|---:|---|---|---|---|---|---:|
| 1 | `FSV-001` | `CheckoutStationComponent.cs` and focused adapter test | Reject completed transaction IDs before creating an active session | #15 at `9f84800` | #16 rebased/force-with-lease updated | 5 |
| 2 | `FSV-OBS-001` support | Development-only validation HUD mouse controls | Directly exercise scoped domain interactions under available UI automation | #16 at `72e3463` | Not applicable | 5 |

## Final recommendation

Select exactly one after all required evidence and reruns are complete.

- [ ] **Continue** — no unresolved blocker or major defect; proposed owner choices
  needed by this proof are approved; required evidence is complete.
- [x] **Adjust** — the direction remains viable, but named corrections or owner
  decisions are required before merge.
- [ ] **Stop** — a blocker, invalid assumption, or unacceptable scope/risk requires
  the stack to pause.

**Recommendation rationale:** The corrected implementation compiles, all 44 Unity
tests pass, the Windows build runs, scoped interactions and in-memory restore are
coherent, and the discovered major defect is resolved. Merge should wait for
project-owner disposition of proposed tuning/content, an explicit persistence
decision if disk restart is required, and direct human confirmation of held-key
movement plus the legacy pickup/rotate/release loop.

**Unresolved owner decisions:** Proposed tuning/content approval; first-store disk
persistence boundary.

**Unresolved blocker/major defects:** None.

**Project-owner reviewer and date:**
