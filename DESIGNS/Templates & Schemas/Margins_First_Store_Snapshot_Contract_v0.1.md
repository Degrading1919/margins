# Margins First-Store Snapshot Contract v0.1

## Status

- **Status:** Owner-authorized temporary vertical-slice implementation
- **Implementation marker:** Disk implementation and focused EditMode/PlayMode
  verification complete; final integrated suites, build, and owner testing pending
- **File-envelope version:** `1`
- **First-store snapshot version:** `2`
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
- player world position, body yaw, and camera pitch.

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
- Player targeting, prompts, previews, development-HUD state, derived objective
  text, presentation materials, animations, and cached occupancy are excluded.

## Restoration order

1. Validate envelope and version.
2. Restore fixture layout and derived occupancy.
3. Restore product registry, inventory locations, and quantities.
4. Restore delivery containers against delivery-type inventory locations.
5. Restore the bounded transaction ledger against known products without
   consuming inventory.
6. Restore store operating state and validate totals against the ledger's
   captured sale-time product unit costs.
7. Validate the optional cleaning task.
8. Validate physical-unit counts and shelf placements against the accepted
   inventory without mutating the scene.
9. Validate player position, body yaw, and camera pitch.
10. Reconcile distinct Unity physical-unit objects only after the complete domain
    restore succeeds, then apply the validated player transform.

## Deferred decisions

- Production save-slot behavior
- Migration from the foundation save
- Production atomic-write, backup-retention, corruption-recovery, and
  player-facing messaging policies
- Multi-location envelope and aggregate-simulation state
- Compatibility guarantees

No temporary field becomes a production compatibility promise. The eventual
production envelope, migration policy, and compatibility guarantees remain
separate owner decisions.
