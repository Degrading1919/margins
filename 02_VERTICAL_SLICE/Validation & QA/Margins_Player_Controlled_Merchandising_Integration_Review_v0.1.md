# Player-Controlled Merchandising Integration Review v0.1

## 1. Target, revisions, and status

- Target: mixed Unity implementation and persistence-schema change for first-store shelf assignment and player-controlled sale prices.
- Repository: `Degrading1919/margins`.
- Base: `origin/main` at `ecec3498c97538e7a093317175cf1e93d7303d61`.
- Reviewed head: uncommitted working tree on `agent/player-controlled-merchandising`, based directly on the same revision.
- Review date: 2026-08-08.
- Pass 1 status: `revise`.
- Pass 2 status: `approve_with_conditions`.
- Final primary status: `approve_with_conditions`; no critical integration blocker remains.
- Reviewer workflow: `$margins-simulation-feature-integration-reviewer`.

## 2. Feature and state summary

The feature lets the player assign one catalog product to each sellable shelf, set a location-owned sale price, and optionally set short shelf-label text. A derived merchandise offer joins product, stable shelf fixture, inventory location, reference price, current sale price, and display label. Detailed customers, stocking, checkout, employee stockers, aggregate simulation, reports, and save/load consume that offer.

Required behavior remains bounded to the current convenience-store path. Product identity, procurement cost, physical inventory, transaction authority, employee recipes, and portfolio accounting remain with their existing owners.

## 3. Authority and scope findings

- Approved repository product, inventory, checkout, procurement, employee, portfolio, and persistence authorities are reused.
- No new engine, package, service, business type, promotion system, tax system, or market simulator is introduced.
- `ProductDefinition` remains catalog identity and physical-compatibility data; it does not own mutable sale price.
- The portfolio location snapshot is the sole persistent owner of mutable merchandising state. Scene components are adapters or projections.
- The old pricing-policy value is retained only as a bulk preset/migration input; aggregate revenue reads merchandise prices, not the policy enum.

## 4. State ownership map

| State | Canonical owner and scope | Lifecycle / mutation | Persistence | Consumers |
|---|---|---|---|---|
| Shelf assignment | `PortfolioLocationSnapshot.shelfMerchandiseAssignments`, per location and stable fixture | Defaults/migration, then atomic player reassignment | Yes, snapshot v3 | stocking, employee destination, checkout mapping, customers, aggregate offers, label |
| Sale price | `PortfolioLocationSnapshot.merchandisePrices`, per location and product | Defaults/migration, shelf editor, or explicit bulk preset | Yes, snapshot v3 | customer response, checkout, aggregate sales, label, reports |
| Reference price | Same merchandise price record, validated against authored checkout catalog for the detailed first store | Configured default/reference; not a mutable economy result | Yes | price-response model and UI explanation |
| Unit/procurement cost | Existing checkout configuration, procurement catalog, and aggregate unit-economy authorities | Existing procurement/economy workflows only | Existing contracts | checkout COGS, purchasing, aggregate COGS |
| Merchandise offer | Derived `MerchandiseOffer` | Resolved on demand from location state | No | detailed and aggregate adapters |
| Exact completed sale price | Checkout transaction line or aggregate merchandise sale line | Captured at completion/allocation; immutable historical fact | Yes | revenue, detailed reconciliation, reports |
| Shelf-label text | Shelf assignment record, per fixture | Optional player edit | Yes | world label only |

No duplicate mutable price authority or circular ownership was found.

## 5. Detailed simulation findings

- Physical units and inventory transfers remain authoritative; reassignment does not convert them to shelf counts.
- Stocking resolves the current offer and then the current shelf transform/snap set. Moving a shelf preserves its stable identity and assignment.
- Employee stockers reuse the same dynamic stocking resolution and current work-point transform.
- Customers evaluate current price deterministically before reserving a physical unit.
- Checkout scans the current authoritative price and stores that exact price and existing unit cost on the transaction line. Completed transactions are idempotent and are not repriced.
- Reassignment blocks physical shelf stock, occupied snaps, held units, customer reservations, and an incomplete checkout.
- Pass-1 reservation gap resolved: price-only edits now reject while a customer holds a reservation from the affected shelf.
- Pass-1 response gap resolved: low, reference, and excessive prices now produce distinct deterministic acceptance in both detailed and aggregate calculations. Reference price retains 95 percent willingness, low price reaches 100 percent, and excessive price falls sharply to zero at the upper ratio bound.

## 6. Aggregate simulation findings

- Aggregate sales resolve the same persisted per-location sale prices and assigned products.
- The price-response curve is deterministic and bounded; extreme prices reduce demand to zero and low prices provide a modest demand lift.
- Revenue is the sum of exact per-product/price sale lines. Historical report lines do not read current prices retroactively.
- Existing aggregate inventory and COGS remain pooled convenience-store abstractions. This is not a new merchandising authority, but it means product-specific stock mix and product-specific off-site COGS are not simulated.
- Performance is constant for the current two-product/two-shelf business and linear in offers for later catalog expansion.

## 7. Transition and reconciliation findings

- Detailed-to-portfolio reconciliation aggregates immutable checkout lines by product and exact unit price, then reconciles them to checkout totals.
- Portfolio merchandising already exists while the detailed scene is loaded, so delegation does not copy or fork prices.
- Disk restore preflights the candidate portfolio against saved physical shelf fixture, inventory-location, and product identities before applying either side.
- Restoration uses saved stable fixture/location/snap identities rather than the pre-load live assignment, avoiding circular restore order.
- Repeated detailed reconciliation and repeated load remain idempotent under the existing transaction/session authorities.
- Returning from aggregate operation to a newly materialized detailed location remains limited by the pre-existing one-way vertical-slice transition; this change neither expands nor worsens that boundary.

## 8. Delegation and management findings

- Direct player stocking and delegated stocker work share destination resolution.
- Off-site managers consume current merchandise prices through aggregate simulation and preserve employee skill/focus, service, stocking, payroll, and reorder effects.
- Pass-1 preset gap resolved: both management presentations route loaded-first-store presets through `FirstStoreMerchandisingComponent`; off-site locations continue to mutate the same portfolio price state directly because they have no instantiated reservations or checkout.
- No automatic manager repricing or portfolio-wide price authority was added.

## 9. Economy and report findings

- Retail price changes affect revenue only; procurement/unit cost does not change.
- Detailed revenue is recognized once by the completed transaction ledger and then reconciled by delta.
- Aggregate revenue is recognized once per simulated day from exact merchandise sale lines.
- New reports validate exact line quantities and gross sales against their report totals. Mixed-price reports no longer pretend to have one unit price.
- Player-facing causes distinguish price rejection, low-price demand lift, stock limits, and checkout capacity.
- Excessive-price exploitation is bounded by zero willingness above the configured ratio ceiling.

## 10. Persistence and migration findings

- Portfolio snapshot version advances from 2 to 3.
- Version 1 and 2 snapshots migrate existing policy/configuration relationships into stable default product, shelf, inventory-location, reference-price, and sale-price records.
- Stable first-store fixture, inventory-location, product, session, physical-unit, and transaction IDs are preserved.
- Current snapshots reject missing, duplicate, incompatible, partial, or non-reconciling merchandising and report state.
- Save/load coverage includes assignment, sale price, custom label, physical reconciliation, report history, and version-2 migration.

## 11. UI and explainability findings

- Two world-space shelf tags show product and current price and move with their shelf fixtures.
- Tag interaction opens a compact UI Toolkit editor using the existing panel settings and visual language.
- Product, price, reference price, unchanged procurement-cost explanation, optional label, validation error, Apply, Cancel, and Escape behavior are present.
- Blockers are human-facing and leave the draft open for correction.
- Owner playtesting is still required for tag legibility, collider targeting, keyboard/controller focus, dropdown readability, and overlap with stocking/build interactions.

## 12. Performance and scaling findings

- Detailed offer resolution performs small linear catalog/assignment scans at interaction, customer shopping, stocking work, and a 0.25-second label poll.
- Aggregate work is linear in active offers per location and does not retain individual off-site customers.
- No pathfinding rebuild occurs on price/assignment changes. Shelf movement continues to use the existing transform/navigation behavior.
- No measured city-scale result is claimed; current two-location/two-product cost is negligible by inspection.

## 13. Required validation scenarios

- Assignment, unassignment, duplicate-product rejection, incompatible-product rejection, physical-stock rejection, held-unit rejection, reservation rejection, and incomplete-checkout rejection.
- Shelf movement retaining assignment, price, label, and employee destination.
- Reference, low, excessive, and deterministic price response in both detailed and aggregate paths.
- Price accepted by a customer remaining consistent through reservation and checkout.
- Direct shelf edit and management preset safety during queued reservations and active checkout.
- Checkout exact-once revenue, unit-cost independence, historical price immutability, and mixed-price reporting.
- Current save round trip, version-1 disk migration, version-2 portfolio migration, invalid partial-state rejection, and repeated load.
- Full EditMode, full PlayMode, and Windows x64 player build after blocker fixes.

Post-fix evidence:

- EditMode: 142 total, 142 passed, 0 failed.
- PlayMode: 62 total, 62 passed, 0 failed.
- Focused live employee/customer scenario after price-response tuning: 1 total, 1 passed.
- Windows x64 build: `Build Finished, Result: Success`; Unity report 104,823,420 bytes.
- Build output: 178 files, 105,032,150 bytes on disk.
- Executable: 667,648 bytes; SHA-256 `C7E7C0FB799BC979D2A086D8836D9570443F925C753442C2979FAC9E38D69C13`.

## 14. Critical blockers

None remain after pass 2.

Resolved in the post-review patch:

1. Detailed and aggregate price response now share low/reference/excessive acceptance points.
2. A queued physical reservation blocks an affected direct price edit without mutating the offer.
3. Loaded-first-store management presets use the same reservation and active-checkout boundary, with focused PlayMode coverage for both blockers.

## 15. Objective approval conditions

Pass-1 conditions 1-5 are satisfied by the post-review patch and evidence above.

Conditions before merge:

1. The project owner playtests the generated Windows build and confirms both shelf tags are readable and targetable at normal first-person distance, including after moving each shelf in Build Mode.
2. The project owner exercises product reassignment, individual price entry, optional label text, queued-customer blocking, and management presets with the intended keyboard/controller flow and confirms the blocker copy is understandable.
3. Any pull-request checks must remain green for the published implementation commit. These conditions do not authorize merge.

## 16. Recommended next owner and artifact

The next owner is the project owner for Windows-build playtesting, followed by the normal pull-request reviewer. The implementation may be published as a clean draft PR with this review record and verification evidence; it must not be merged under this review.
