# Margins First-Store Snapshot Contract v0.1

## Status

- **Status:** Proposed for project-owner review
- **Implementation marker:** Draft implementation — local compilation, EditMode,
  and PlayMode verification complete; manual build validation pending
- **Contract version:** `1` inside the distinct first-store snapshot envelope
- **Disposition:** Temporary vertical-slice snapshot and mapper target awaiting project-owner approval

## Relationship to the foundation save

This contract is additive. It does not replace, rename, migrate, or silently reinterpret:

- `FoundationSaveData`;
- `PlacementSaveController.CurrentSaveVersion == 1`; or
- `foundation-spike-save.json`.

The existing foundation placement-save contract remains valid for its approved spike. A future owner-approved persistence decision may map foundation placements into the first-store envelope, keep both during development, or define a migration. This draft does not choose among those production options.

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
- one bounded completed-transaction ledger with deterministic transaction-ID order;
- store operating state, session identity, and end totals including gross sales,
  cost of goods sold, included operating expenses, contribution after COGS, units,
  and transaction count;
- one bounded cleaning-task snapshot.

## Invariants

- Stable identifiers use lowercase ASCII letters, digits, hyphens, underscores, or dots, are 1–64 characters, and begin and end with a letter or digit.
- Fixture placements restore in stable-identifier order and rebuild occupancy.
- Duplicate fixture, product, location, container, transaction, checkout-line,
  physical-unit, or physical shelf-placement identifiers reject the snapshot.
- Inventory quantities are positive integers in records and cannot exceed location capacity.
- Inventory restoration registers products and locations before seeding quantities.
- Ledger restoration validates every transaction subtotal and unit total but never
  replays stock consumption.
- Ledger totals are derived from its completed transactions.
- Every domain unit in a loose, held, or shelf location has exactly one visible
  physical-unit record; delivery-container units remain represented by the box.
- Physical-unit product/location counts must exactly match accepted inventory.
- Held units restore to the explicit hold point. Shelved units restore only to
  configured product-specific shelf locations and snap points.
- `closed_with_result_pending` requires totals that reconcile to the ledger and
  configured product unit-cost inputs.
- Unsupported versions reject the snapshot without partial mutation.
- Presentation state, prompts, target selection, materials, animations, and cached occupancy are excluded.

## Restoration order

1. Validate envelope and version.
2. Restore fixture layout and derived occupancy.
3. Restore product registry, inventory locations, and quantities.
4. Restore delivery containers against delivery-type inventory locations.
5. Restore the bounded transaction ledger against known products without
   consuming inventory.
6. Restore store operating state and validate totals against the ledger and
   configured product unit costs.
7. Validate the optional cleaning task.
8. Validate physical-unit counts and shelf placements against the accepted
   inventory without mutating the scene.
9. Reconcile distinct Unity physical-unit objects only after the complete domain
   restore succeeds.

## Deferred decisions

- JSON codec and file path for the first-store envelope
- Production save-slot behavior
- Migration from the foundation save
- Atomic disk-write and backup strategy
- Corruption recovery and player-facing messaging
- Multi-location envelope and aggregate-simulation state
- Compatibility guarantees

No draft field becomes a production compatibility promise until the project owner approves the consuming schema and local Unity validation succeeds.
