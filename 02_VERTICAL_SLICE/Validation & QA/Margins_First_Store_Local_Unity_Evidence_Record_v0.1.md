# Margins First-Store Local Unity Evidence Record v0.1

## Record status

- **Status:** Proposed for project-owner review
- **Execution status:** Not run
- **Implementation marker:** Draft implementation — Unity verification pending
- **Packet:** `Margins_First_Store_Local_Unity_Verification_Packet_v0.1.md`

Leave a field as `not run`, `blocked`, or `not applicable` rather than inferring a
pass. Link or attach durable evidence for every pass or failure.

## Exact source and environment

| Field | Required record |
|---|---|
| Exact tested commit (`git rev-parse HEAD`) | |
| Test date/time and timezone | |
| Tester | |
| OS and version | |
| CPU / RAM / GPU | |
| Unity editor version | Expected `6000.5.5f1`; record actual |
| Render pipeline package | Baseline `com.unity.render-pipelines.universal` `17.5.0`; record resolved |
| Input System package | Baseline `com.unity.inputsystem` `1.20.0`; record resolved |
| AI Navigation package | Baseline `com.unity.ai.navigation` `2.0.14`; record resolved |
| Test Framework package | Baseline `com.unity.test-framework` `1.7.0`; record resolved |
| Full package manifest/lock evidence | |
| Draft PR base/head comparisons | |

## Import and compilation

| Check | Result | Evidence / exact message |
|---|---|---|
| Initial import completed | Not run | |
| `.meta` reconciliation | Not run | |
| New or modified serialized files | Not run | |
| Import warnings | Not run | |
| Import errors | Not run | |
| C# compile result | Not run | |
| Final Console error count | Not run | |
| Final Console warning count | Not run | |

## Automated test evidence

| Suite | Exact filter | Passed | Failed | Skipped/Inconclusive | Duration | Result artifact / screenshot |
|---|---|---:|---:|---:|---:|---|
| Complete EditMode | | | | | | |
| First-store domain EditMode | `Margins.Tests.FirstStoreDomainEditModeTests` | | | | | |
| First-store adapter EditMode | `Margins.Tests.FirstStoreAdapterEditModeTests` | | | | | |
| Complete PlayMode | | | | | | |

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
| Inventory authority and locations | | Not run | |
| Product definitions | | Not run | |
| Fixture grid and required fixtures | | Not run | |
| Mixed-product delivery container | | Not run | |
| Physical-unit prefabs, spawns, and reconciliation | | Not run | |
| Product-specific shelves, hold point, and snap points | | Not run | |
| Checkout shelf/price/unit-cost mappings and ledger capacity | | Not run | |
| Cleaning task | | Not run | |
| Store session/controller | | Not run | |
| First-store mapper | | Not run | |
| Existing foundation placement save | | Not run | |

Record every Unity-created or modified scene, prefab, ScriptableObject asset, and
`.meta` file. Do not mark this section complete from source inspection alone.

## Manual interaction results

| Interaction / scenario | Expected result | Result | Evidence / defect |
|---|---|---|---|
| First-person move and look | Existing foundation controls remain usable | Not run | |
| Foundation pickup, rotate, release | Existing product behavior remains compatible | Not run | |
| `fs-accept-014` invalid/duplicate IDs | Blocked with bounded diagnostic and no partial state | Not run | |
| `fs-accept-001` fixture placement | In-bounds, deterministic footprint/orientation | Not run | |
| `fs-accept-002` overlap/move rejection | Prior accepted placement preserved | Not run | |
| Fixture remove and recovery | Domain and visible state agree; no orphan occupancy | Not run | |
| `fs-accept-003` receiving | Sealed reject; open then conserved box-to-loose transfer | Not run | |
| Mixed-product removal | Requested cola/chips unit removed; invalid/exhausted request changes nothing | Not run | |
| `fs-accept-004` valid stocking | Loose-to-held-to-shelf moves exactly one unit and one visible item | Not run | |
| Repeated physical units | Repeated removals create distinct visible units and stock distinct snap points | Not run | |
| `fs-accept-005` invalid stocking | Quantity, held state, and occupancy remain unchanged | Not run | |
| Physical placement rollback | Failed placement retains exactly one held domain/visible unit | Not run | |
| Product-specific shelf consumption | Each checkout product consumes only its mapped shelf location | Not run | |
| Checkout-line mutation resistance | External mutation cannot alter active product, price, or quantity | Not run | |
| `fs-accept-006` checkout | At least two transactions complete with exact integer-cent subtotals | Not run | |
| Duplicate transaction ID | Rejected before ledger, stock, or physical-unit mutation | Not run | |
| Ledger/result reconciliation | Gross, COGS, expenses, contribution, units, and transaction count reconcile | Not run | |
| `fs-accept-007` repeat completion | Same summary; no second revenue or stock consumption | Not run | |
| `fs-accept-008` cleaning | Progress clamps and completion is idempotent | Not run | |
| `fs-accept-009` opening prerequisites | Invalid open rejected with actionable explanation | Not run | |
| `fs-accept-010` close/result | Valid sequence and totals retained until acknowledgement | Not run | |
| Required fixture move/remove while open | Both operations rejected; accepted placement preserved | Not run | |
| Required fixture move/remove while closing | Both operations rejected; accepted placement preserved | Not run | |
| In-memory physical/domain restore | Physical counts/placements equal inventory after repeated restore; no replay | Not run | |
| `fs-accept-011` full disk persistence | Save/exit/reload equality | Blocked pending persistence decision | |
| `fs-accept-012` E1 arithmetic | `$10,000-$3,000-$2,250-$1,100-$205+$500=$3,945`; contribution `$10` | Not run | |
| `fs-accept-012` E2 arithmetic | `$10,000-$3,000-$2,250-$1,100-$205+$300=$3,745`; contribution `-$85` | Not run | |
| `fs-accept-012` E3 arithmetic | `$8,000-$3,000-$2,250-$1,800-$205+$500=$1,245`; contribution `$10` | Not run | |
| `fs-accept-013` recovery | Weak result explained; continuation remains possible | Not run | |

## Save-file and restoration evidence

| Field | Required record |
|---|---|
| Owner-approved first-store save disposition | |
| First-store save path | |
| Foundation placement sidecar path | |
| Save action and timestamp | |
| File existence, size, and hash before exit | |
| Sanitized save excerpt or attached file | |
| Full process/editor exit evidence | |
| Reload action and timestamp | |
| File size and hash after reload | |
| Fixture-placement equality | |
| Inventory per-location equality | |
| Delivery-container state equality | |
| Completed-transaction ledger equality / no replay | |
| Cleaning-state equality | |
| Operating-state and totals equality | |
| Distinct physical-unit / shelf-placement reconciliation | |
| Rejected or repaired records | |

If only the in-memory mapper round trip is available, record that result under
automated tests and leave the full restart result blocked.

## Windows x64 development-build evidence

| Field | Required record |
|---|---|
| Build target and architecture | Windows x64 |
| Development build settings | |
| Exact output path | |
| Build start/end time | |
| Build result | Not run |
| Build warnings/errors | |
| Executable and data-folder sizes | |
| Executable hash | |
| Launch result | Not run |
| Manual controls result | Not run |
| Clean exit result | Not run |
| `Player.log` path | |
| `Player.log` findings | |

## Defect log

| Defect ID | Severity | Area / scenario | Reproduction | Expected | Actual | Owning branch | Correction commit | Rerun evidence | Status |
|---|---|---|---|---|---|---|---|---|---|
| | | | | | | | | | |

Severity must be one of `blocker`, `major`, `minor`, or `observation`, using the
definitions in the verification packet.

## Corrections made during local verification

| Order | Defect ID | Files/assets changed through approved workflow | Reason | Owning PR updated | Descendants refreshed | Verification restarted at step |
|---:|---|---|---|---|---|---:|
| | | | | | | |

## Final recommendation

Select exactly one after all required evidence and reruns are complete.

- [ ] **Continue** — no unresolved blocker or major defect; proposed owner choices
  needed by this proof are approved; required evidence is complete.
- [ ] **Adjust** — the direction remains viable, but named corrections or owner
  decisions are required before merge.
- [ ] **Stop** — a blocker, invalid assumption, or unacceptable scope/risk requires
  the stack to pause.

**Recommendation rationale:**

**Unresolved owner decisions:**

**Unresolved blocker/major defects:**

**Project-owner reviewer and date:**
