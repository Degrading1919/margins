# Margins First-Store Snapshot Contract v0.1

## Status

- **Status:** Owner-authorized temporary vertical-slice implementation
- **Implementation marker:** Disk, portfolio/property, delegation, detailed-to-
  aggregate reconciliation, and procurement implementation with focused
  EditMode/PlayMode verification; integrated suites and build are recorded with
  the implementing pull request
- **File-envelope version:** `4`
- **First-store snapshot version:** `4`
- **Portfolio snapshot version:** `4`
- **Procurement snapshot version:** `1`
- **Disposition:** Reversible first-store validation implementation only; it is not
  approval of the eventual production save architecture, migration policy, or slots

## Relationship to the foundation save

This contract is additive. It does not replace, rename, migrate, or silently reinterpret:

- `FoundationSaveData`;
- `PlacementSaveController.CurrentSaveVersion == 1`; or
- `foundation-spike-save.json`.

The existing foundation placement-save contract remains valid for its approved
spike. The first-store validation scene writes only the distinct file below and
does not write the FoundationSpike sidecar for the same state. A future
owner-approved production persistence decision may replace this temporary proof;
this contract does not choose its migration or compatibility policy.

## Temporary disk disposition

- File: `Margins/first-store-vertical-slice.json` beneath
  `Application.persistentDataPath`.
- `F5` writes and `F9` loads in the first-store validation scene.
- A same-directory temporary file is fully written and flushed before replacing
  the accepted file. The prior accepted file remains available until replacement
  succeeds.
- Unsupported versions, malformed JSON, invalid identifiers, contradictory
  totals, and invalid physical reconciliation reject before live-state mutation.
- Saving is rejected while a product is held or a checkout is incomplete.
- This proof has one file, no slots, migration tooling, cloud storage,
  encryption, compression, or save-menu framework.

## Envelope

The file envelope contains the first-store snapshot, player transform, portfolio
snapshot, and an optional active generated-location snapshot. The generated
snapshot identifies the persistent business location, carries that location's
detailed store state, records the parked first-store return transform, and
preserves whether shared customer and employee work adapters should resume. The
envelope carries an explicit generated-location presence bit because Unity's
JSON serializer can otherwise materialize a null nested object as an empty
instance. The presence bit is authoritative and is false for legacy envelopes.
Version `4` also records one explicit source state: detailed first store,
management/aggregate, or detailed generated location. It preserves the intended
first-store customer-flow and employee-work enablement separately from the
management state's deliberately quiescent loaded rig. File-envelope versions
`1` through `3` normalize to the historical detailed-first-store source.
The portfolio snapshot remains the authority for company cash, company/brand/
property/unit/location identity, employees, schedules, delegation policies,
product-level aggregate inventory, procurement, operating reports, alerts, and
per-location detailed/aggregate reconciliation.

The first-store snapshot contains:

- integer version;
- fixture grid width and depth;
- stable fixture placement records;
- registered product identifiers;
- inventory location definitions and integer quantities;
- delivery-container identifiers, inventory-location references, and open state;
- visible physical-unit identifiers, product/location references, and shelf
  fixture/snap placement when the unit is shelved;
- the next deterministic physical-unit ordinal;
- one bounded completed-transaction ledger with deterministic transaction-ID
  order and sale-time unit price and unit cost on every completed line;
- store operating state, session identity, and end totals including gross sales,
  cost of goods sold, included operating expenses, contribution after COGS, units,
  and transaction count;
- one bounded cleaning-task snapshot;
- active customer flow, deterministic arrival state, and cumulative customer
  visits, service, abandonment, requested units, and unavailable units used by
  detailed-operation reporting;
- player world position, body yaw, and camera pitch.

The portfolio snapshot contains:

- one player company with independent brand, commercial-property,
  commercial-unit, and business-location collections linked by stable IDs;
- multiple brands, multiple units per property, vacant units, and properties
  without business locations are valid even though current authored content
  still instantiates only the convenience brand and occupied locations;
- leased or owned property tenure, acquisition history, unit occupancy, and
  persistent unit improvements;
- generator version, seed, archetype, authored footprint dimensions, selected
  commercial unit, canonical layout signature, revision, and stable-ID player
  modifications for each generated unit;
- employees with stable location assignments, roles, performance, task focus,
  wages, and same-day weekly schedules;
- per-location pricing, reorder, manager-authority, spending-limit,
  maintenance, and operating-standard policies;
- product-level quantities and captured unit costs that reconcile to each
  location's aggregate inventory total;
- customer satisfaction, service quality, availability, product mix,
  maintenance condition, failure pressure, and recoverable operating alerts;
- per-location cumulative money, inventory, staffing, operation, and report
  totals; and
- a per-location detailed-session baseline and cumulative reconciliation record
  so repeated synchronization and later detailed sessions cannot repost prior
  money, inventory, customers, or progress, including a persisted loaded-scene
  inventory baseline when returning after aggregate operation.

The nested procurement snapshot contains:

- one monotonic procurement clock and next purchase-order ordinal;
- purchase orders sorted by stable order ID;
- location, supplier, and container-definition IDs;
- captured placement and fulfillment ticks;
- `pending`, `fulfilled`, `delivered`, `partially_received`, `canceled`, or
  `completed` status;
- configured resource lines with ordered and received integer quantities,
  captured unit cost, and checked line cost;
- subtotal, delivery fee, total payment, and cancellation refund in integer
  cents; and
- deterministic payment, fulfillment, delivery, inventory-receipt,
  completion-receipt, and cancellation event IDs as applicable to status.

## Invariants

- Stable identifiers use lowercase ASCII letters, digits, hyphens, underscores, or dots, are 1–64 characters, and begin and end with a letter or digit.
- Fixture placements restore in stable-identifier order and rebuild occupancy.
- Duplicate fixture, product, location, container, transaction, checkout-line,
  physical-unit, or physical shelf-placement identifiers reject the snapshot.
- Inventory quantities are positive integers in records and cannot exceed location capacity.
- Inventory restoration registers products and locations before seeding quantities.
- Ledger restoration validates every transaction subtotal and unit total but never
  replays stock consumption.
- Ledger totals are derived from its completed transactions. Historical COGS is
  derived from each line's captured sale-time unit cost, never from later product
  configuration.
- Every domain unit in a loose, held, or shelf location has exactly one visible
  physical-unit record; delivery-container units remain represented by the box.
- Physical-unit product/location counts must exactly match accepted inventory.
- Held units restore to the explicit hold point. Shelved units restore only to
  configured product-specific shelf locations and snap points.
- `closed_with_result_pending` requires totals that reconcile to the ledger and
  its captured sale-time unit costs.
- Unsupported versions reject the snapshot without partial mutation.
- Legacy first-store versions `2` and `3`, file-envelope versions `1` through `3`,
  and portfolio versions `1` through `3` normalize deterministically to the
  current temporary contract before validation and live-state mutation.
- Every business location resolves by ID to one brand, property, and occupied
  commercial unit. Brands and properties may exist without locations, and
  commercial units may be vacant. Property, unit, and business-location
  identifiers remain distinct and unique.
- At most one business location is physically detailed at a time. Its generated
  layout signature must match the deterministic result of its persisted
  generator inputs before player modifications are replayed.
- A management source has no active detailed portfolio location and keeps the
  loaded first-store customer, employee, and detailed-procurement adapters
  quiescent. Delegated day advancement rejects while either the first store or a
  generated location can still mutate detailed state.
- Leaving the first store reconciles its final detailed delta, rejects unsafe
  customer, employee, carried-tool, or in-progress delivery state, clears the
  active detailed location, and only then enables aggregate operation. Returning
  establishes a fresh per-location reconciliation baseline before re-enabling
  the saved detailed adapters.
- An active generated-location envelope must identify the same detailed location
  as the portfolio, retain a valid parked first-store snapshot, and cannot park
  active customers in that inactive first-store scene.
- Restoring a generated location rematerializes its persisted generator inputs,
  rebinds shared detailed authorities, restores authoritative inventory,
  employees, customer observations and safe active-customer state, then creates
  a fresh reconciliation baseline before physical operation resumes. A restored
  customer position may be projected to the rebuilt NavMesh without changing its
  stable identity, requested products, reservations, or progress state.
- Product-level inventory exactly reconciles to the location total; detailed
  sessions post only changes since their captured start baselines.
- Employee schedules, location assignments, manager authority, spending limits,
  and operating standards must validate before a delegated day advances.
- Owned properties accrue no new rent after acquisition; historical rent remains
  part of immutable prior reports and lifetime totals.
- A location has at most one nonterminal purchase order.
- Order and event IDs are unique, deterministic, and never reused. Status must
  agree with every present or absent lifecycle event.
- Payment is recorded when the order is placed. Only a pending order may cancel,
  and its full payment is refunded once.
- Fulfillment cannot occur before the captured due tick. Delivery creation and
  absolute receipt updates are idempotent.
- Received quantity cannot move backward or exceed ordered quantity. Completion
  requires every line to be fully received.
- Net purchase-order delivery fees reconcile exactly to each location's lifetime
  delivery-fee total; procurement purchases cannot exceed its recorded lifetime
  inventory purchases.
- For an active detailed `delivered` or `partially_received` order, the physical
  delivery-location quantities exactly equal ordered quantity minus received
  quantity for every line. Off-site inventory never receives those units again.
- Player targeting, prompts, previews, development-HUD state, derived objective
  text, presentation materials, animations, and cached occupancy are excluded.

## Restoration order

1. Validate the file envelope and nested snapshot versions.
2. Normalize supported legacy snapshots, then validate the company/brand/
   property/unit/location graph, generated-layout identity, improvements,
   employees, schedules, delegation policies, product inventory, reconciliation
   baselines, reports, alerts, procurement clock, purchase orders, lifecycle
   events, and company cash without mutation.
3. Validate detailed purchase-order and merchandising quantities against each
   persisted detailed location before accepting any snapshot.
4. If a generated location is active, leave its temporary scene instance and
   restore the parked first-store adapters before applying accepted state.
5. Restore fixture layout and derived occupancy.
6. Restore product registry, inventory locations, and quantities.
7. Restore delivery containers against delivery-type inventory locations.
8. Restore the bounded transaction ledger against known products without
   consuming inventory.
9. Restore store operating state and validate totals against the ledger's
   captured sale-time product unit costs.
10. Validate the optional cleaning task.
11. Validate physical-unit counts and shelf placements against the accepted
    inventory without mutating the scene.
12. Validate player position, body yaw, and camera pitch.
13. Apply the accepted portfolio; restore the recorded detailed-first-store,
    management, or generated source state; rematerialize an active generated
    layout when present; rebind and restore shared detailed adapters; reconcile
    distinct Unity physical-unit objects; and establish a current per-location
    reconciliation baseline before detailed operation resumes. A management
    restore leaves those adapters disabled.
14. Restore cumulative customer-flow observations and apply the validated player
    transform only after every validation succeeds. If application fails, restore
    the captured pre-load store, portfolio, location, and player state.

## Deferred decisions

- Production save-slot behavior
- Migration from the foundation save
- Production atomic-write, backup-retention, corruption-recovery, and
  player-facing messaging policies
- Compatibility guarantees
- Complex supplier behavior, routing, substitutions, damage, invoices, and
  speculative logistics

No temporary field becomes a production compatibility promise. The eventual
production envelope, migration policy, and compatibility guarantees remain
separate owner decisions.
