# Margins First-Store Hands-On Operating Loop v0.1

## Status and authority

- **Status:** Proposed for project-owner review
- **Scope:** The first hands-on convenience-store loop only
- **Authority:** Applies approved FD-004, FD-007, FD-016, FD-018, and the current vertical-slice scope without replacing them
- **Implementation status:** No implementation or Unity verification is claimed by this specification

## Purpose

Prove one understandable, persistent operating session in which the player prepares a mostly empty leased store, handles a delivery, stocks products, opens, completes a small set of sales, performs one cleaning or maintenance task, closes, reviews the result, and resumes the same state after reload.

This package deliberately stops before customer simulation, employees, delegation, off-site simulation, or portfolio reporting.

## Player sequence

1. Enter a small, mostly empty leased store while it is `closed`.
2. Enter `preparing` and place the essential fixtures.
3. Receive one preconfigured delivery container in the delivery zone.
4. Open the container and remove product units.
5. move units through loose, held, and shelved inventory locations.
6. Verify opening prerequisites and open the store.
7. Scan and complete scripted or manually staged transaction baskets.
8. Complete one cleaning or basic-maintenance task.
9. Stop new transactions and close the store.
10. Review a simple session result with causes.
11. Save, exit, reload, and continue from the accepted state.

The transaction basket is a validation fixture. This specification does not define customer arrival, browsing, queues, demand, satisfaction, payment, or change-making.

## Authoritative state

| State | Scope | Canonical owner | Persisted | Notes |
|---|---|---|---|---|
| Store operating state | location | store operating domain | yes | `closed`, `preparing`, `open`, `closing`, `closed_with_result_pending` |
| Operating-session identity | location/session | store operating domain | yes | Stable identifier; no full calendar required |
| Fixture placements | location | fixture-placement domain | yes | Stable instance ID, grid position, footprint, quarter-turn orientation |
| Grid occupancy | location | fixture-placement domain | derived | Rebuilt from accepted placement snapshots |
| Product quantities | product/location | inventory domain | yes | Integer units only |
| Delivery-container state | container | receiving domain | yes | Stable container ID and sealed/open state |
| Checkout lines and completion | transaction | checkout domain | yes for completed summary | Active targeting and presentation are transient |
| Session totals | location/session | operating domain | yes | Gross sales, units, transaction count, included operating expenses |
| Cleaning or maintenance task | task/location | task component | yes if incomplete or outcome-relevant | One bounded task in the proof |
| Interaction target and prompt | player | interaction adapter | no | Recomputed from the current view |
| Placement preview | player | placement adapter | no | Never owns placement or inventory |

No scene object, prompt, animation, material, or HUD element is an authoritative source of business state.

## Store operating-state transitions

| Current | Action | Next | Required conditions | Rejection and recovery |
|---|---|---|---|---|
| `closed` | begin preparation | `preparing` | Valid session identity | Remain closed and explain invalid identity |
| `preparing` | open store | `open` | Essential fixtures placed; checkout configured; at least one sellable unit shelved; no held unit; no blocking validation error | Remain preparing; list unmet prerequisites |
| `preparing` | abandon preparation | `closed` | No active transaction | Preserve accepted layout and inventory |
| `open` | begin closing | `closing` | No new transaction begins after transition | Finish or cancel the active validation basket |
| `closing` | finalize result | `closed_with_result_pending` | No active transaction or held unit; totals supplied | Remain closing and identify unresolved state |
| `closed_with_result_pending` | acknowledge result | `closed` | Result has been presented or explicitly acknowledged | Preserve result until acknowledgment |

Any other transition is invalid and must leave the current state unchanged.

## Fixture placement

### Player actions

- Target an unplaced or placed fixture.
- Enter placement preview.
- Move the preview by grid cells.
- Rotate in quarter turns.
- Confirm, move, cancel, or remove.

### Rules

- The first proof uses one bounded rectangular placement grid.
- Each fixture has a stable instance identifier and positive rectangular footprint.
- Quarter turns are normalized to `0`, `1`, `2`, or `3`.
- Odd quarter turns swap footprint width and depth.
- Every occupied cell must be inside store bounds and unoccupied by another fixture.
- Validation order is deterministic: identifier, bounds, then occupied cells in row-major order.
- A rejected place or move does not change the prior accepted placement.
- Removing a fixture frees its cells only after the removal is accepted.
- Essential fixtures may not be removed while the store is open.
- Layout state is authoritative; preview transforms are presentation only.

### Failure and recovery

- Out of bounds: keep the last valid state and show the blocked edge.
- Occupied cell: identify the conflicting fixture and cells.
- Duplicate or malformed stable ID: block acceptance and record a validation error.
- Cancel move: restore the exact prior placement.
- Reload: clear runtime occupancy and rebuild it from accepted placement records.

## Receiving and delivery containers

### First-proof contract

- One preconfigured supplier delivery creates one container at a designated delivery location.
- The container has a stable instance ID, product quantities, and a sealed/open state.
- `sealed` may transition to `open`; reopening is idempotent.
- Product removal is blocked while sealed.
- Opening does not create, destroy, or relocate units.
- Removing units transfers them atomically from `delivery_container` to `loose_backroom` or `held`.
- An empty open container may remain as empty evidence or be dismissed; dismissal is presentation state and cannot discard units.

### Explicitly not included

- Ordering UI
- Delivery scheduling or routing
- Supplier simulation
- Partial shipment, substitution, damage, spoilage, or invoice disputes
- Pallets, forklifts, loading docks, or player driving

## Inventory and transfers

### Inventory locations used by this proof

| Location kind | Purpose | Capacity behavior |
|---|---|---|
| `delivery_container` | Boxed units received from the supplier | Fixed by the accepted delivery contents |
| `loose_backroom` | Units removed from a box but not held or shelved | Bounded or unlimited by location definition |
| `held` | Unit currently represented in the player’s hands | Capacity one; one product kind |
| `shelf` | Sellable units assigned to a shelf location | Bounded; one product kind per location for this proof |

### Transfer rules

- Product identifiers are stable nonblank strings.
- Quantities are nonnegative integers; transfer requests must be positive integers.
- Source and destination locations must exist and differ.
- The source must contain the requested quantity.
- The destination must have capacity and accept the product without violating its one-product rule.
- Validation completes before mutation.
- A successful transfer subtracts and adds the same quantity in one operation.
- A rejected transfer changes nothing.
- A sale is an explicit checkout consumption, not an ordinary transfer and not silent loss.
- Totals before and after a transfer must be equal for every product.

### Physical representation boundary

One visible unit may represent one inventory unit during this proof. Runtime adapters must not instantiate a visible product unless the domain transfer is accepted, and must roll back domain state if physical placement fails.

## Stocking

1. Open the delivery container.
2. Transfer one unit to `held`.
3. Target a compatible unoccupied shelf point.
4. Show valid or invalid preview without mutating inventory.
5. On confirmation, attempt the domain transfer from `held` to the shelf location.
6. Apply the existing deterministic shelf snap only when both domain and physical placement can complete.
7. On failure, preserve the unit in `held` and preserve prior shelf occupancy.

The existing foundation shelf snap remains the physical-placement authority for its authored snap points. The new inventory domain owns quantities. Neither may silently infer or overwrite the other.

## Checkout

### Minimal checkout contract

- A checkout session has a stable transaction identity.
- A scripted or manually staged basket requests scans of valid product IDs.
- Each line has integer quantity and integer-cent unit price.
- The session checks sellable shelf stock, including quantities already scanned in that session.
- Repeated scans at the same price aggregate deterministically.
- A correction removes a requested quantity without going below zero.
- Subtotal is the sum of `unit_price_cents × quantity`.
- Completion atomically consumes the sold units and produces one immutable transaction summary.
- Repeating completion returns the existing summary and never consumes stock twice.

### Explicitly not included

- Customer actors, arrival, browsing, queues, patience, or satisfaction
- Tax
- Discounts, coupons, loyalty, returns, or refunds
- Cash, card, payment authorization, change-making, tips, or debt
- Theft, shrinkage, spoilage, age verification, or restricted products

## Cleaning or basic maintenance

The proof contains one stable task, recommended as a small floor spill because it is visible, bounded, and understandable.

States:

`needs_service → in_progress → complete`

Rules:

- Progress is deterministic integer work units.
- Progress cannot decrease or exceed the required total.
- Completion is idempotent.
- The task must be complete before final closing in the acceptance scenario.
- Cleaning changes only task state in this proof. It does not yet affect customer satisfaction, equipment health, or demand.

The same contract may later support an employee task, but no employee execution is implemented here.

## Opening, closing, and result

### Opening prerequisites

- Store state is `preparing`.
- Essential fixtures are placed and inside bounds.
- Checkout station configuration is valid.
- At least one proposed starter product has a positive shelf quantity.
- No unit is in `held`.
- No duplicate stable identifiers or inventory validation errors remain.

### Closing prerequisites

- Store state is `closing`.
- No transaction is active.
- No unit is in `held`.
- The required task is complete.

### Simple result

Present:

- gross sales;
- units sold;
- completed transactions;
- cost of sold inventory if supplied by the tuning fixture;
- included delivery, rent allocation, cleaning/maintenance, and utility-proxy expenses;
- resulting contribution;
- ending cash and reserve comparison;
- remaining shelf, loose, and boxed units;
- one or two causal notes, such as “sales were limited by shelf stock.”

This is a first-store result, not portfolio reporting or detailed accounting.

## Persistence and restoration

Persist:

- snapshot version;
- fixture instance IDs, grid cells, footprints, and quarter turns;
- inventory location definitions and quantities;
- delivery-container IDs and open state;
- latest completed transaction summary required for idempotency;
- store state, session identity, and end totals;
- the required task state if incomplete or result-relevant.

Do not persist:

- current raycast target;
- prompts;
- placement preview;
- material or animation state;
- cached grid occupancy;
- derived subtotals that can be checked from transaction lines;
- transient logging.

Restore order:

1. Parse and validate snapshot version.
2. Reject malformed or duplicate stable identifiers.
3. Restore fixture placements and rebuild grid occupancy.
4. Restore inventory locations and quantities.
5. Restore delivery-container state against its inventory location.
6. Restore completed checkout summary without replaying consumption.
7. Restore operating state and totals.
8. Reconcile Unity-facing objects from accepted domain state.
9. Report rejected records without inventing replacement state.

The new first-store snapshot is additive and must not replace or silently modify the existing `FoundationSaveData` placement-save contract.

## Failure-pressure boundary

The first proof may end with a negative contribution, but it must not create bankruptcy, debt, eviction, or irreversible save loss. The result explains the pressure and allows replay or continuation with remaining cash and inventory.

## Minimum future contracts

Later systems may consume, but may not yet mutate through this package:

- task definitions that can later be assigned to workers;
- completed transaction summaries that can later feed demand and reports;
- location-level inventory and operating snapshots that can later be aggregated;
- opening prerequisites that a manager may later satisfy;
- stable fixture and product identifiers reusable across locations.

Future delegation and aggregate simulation require a new integration review. They must not infer completed work or sales from presentation objects.

## Explicitly deferred

- Customer simulation, demand, satisfaction, queues, and recurring customers
- Employees, schedules, roles, skills, wages, managers, and delegation
- Off-site or time-skipped simulation
- Second location, travel, local markets, competition, and portfolio reports
- Dynamic pricing, purchasing, reorder policies, and supplier depth
- Taxes, loans, investors, acquisitions, and property development
- Equipment wear beyond one bounded task
- Theft, shrinkage, spoilage, returns, refunds, and restricted products
- Production UI framework, final art, animation, audio, and onboarding scripting
- Full calendar, weather, day/night, and construction

## Acceptance summary

The package is ready for owner review when every state mutation has one owner, all rejected actions are atomic, inventory is conserved except for explicit completed sales, the operating sequence can be restored without replaying transactions, and every deferred system is visibly outside the proof.
