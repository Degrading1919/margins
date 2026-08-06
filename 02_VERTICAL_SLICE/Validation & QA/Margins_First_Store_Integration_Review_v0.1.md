# First-Store Package Integration Review v0.1

## 1. Target and status

- **Workflow:** `$margins-simulation-feature-integration-reviewer`
- **Target:** First-store design, UX, economy, style, content, tuning, and acceptance package
- **Base revision:** `main` at `4e26b668110f9a3ee3a3343c2a0a5533345b8dcd`
- **Head revision:** Task 1 branch revision to be recorded by the draft pull request
- **Artifact type:** Mixed design and proposed structured data
- **Primary status:** `approve_with_conditions`

## 2. Feature and state summary

The package defines one detailed, player-present convenience-store session from preparation through save/reload. Durable state is limited to fixture placement, inventory locations and quantities, one delivery container, checkout completion, one task, store operating state, and minimal session totals.

Customers, employees, delegation, aggregate off-site simulation, time skipping, the second location, and portfolio reporting are explicitly deferred.

## 3. Authority and scope findings

- Aligns with approved guided startup and tactile interaction direction.
- Advances only the hands-on subset of the approved convenience-store vertical slice.
- Does not reopen engine, Unity version, packages, render pipeline, project path, or foundation-spike decisions.
- Does not add customers, employees, delegation, aggregate simulation, or a second location.
- Structured content is marked proposed and remains outside Unity runtime assets.
- No scope blocker remains.

## 4. State ownership map

| State | Owner | Derived consumers | Finding |
|---|---|---|---|
| Fixture placement | fixture-placement domain | Unity transforms, preview, future layout report | Single owner |
| Grid occupancy | derived from placements | placement validation | Correctly not persisted as a second authority |
| Product quantity by location | inventory domain | boxes, held item, shelf visuals, checkout | Single owner |
| Delivery open state | receiving domain | box presentation and prompt | Single owner |
| Checkout completion | checkout domain | result and future reporting | Idempotency required |
| Cleaning task state | bounded task component/domain state | close prerequisite and presentation | Single owner for proof |
| Store operating state | operating domain | open/close presentation and allowed actions | Single owner |
| Session result | operating domain from completed transactions and included expenses | close result | Inputs remain traceable |

The existing foundation shelf occupancy remains physical snap state. It must not become a quantity ledger.

## 5. Detailed simulation findings

- Player actions and valid transitions are explicit.
- Inventory transfers are atomic and conserve units.
- A scripted basket exercises checkout without inventing customer behavior.
- Opening and closing prerequisites prevent ambiguous half-states.
- Failure and recovery preserve prior accepted state.
- Cleaning has a bounded deterministic task contract.

No detailed-simulation blocker remains.

## 6. Aggregate simulation findings

Aggregate off-site simulation is out of scope. The package preserves future compatibility by using location-level stable snapshots and completed transaction summaries rather than relying on live scene objects.

Condition: a later aggregate-simulation specification must define update cadence, demand, staffing, manager authority, inventory consumption, maintenance, financial recognition, and reconciliation. This package cannot be treated as that specification.

## 7. Transition and reconciliation findings

Presence/absence transitions are not implemented. Save/reload reconciliation is defined:

- clear derived occupancy;
- validate stable identities;
- restore fixture and inventory state;
- restore completed checkout without replay;
- restore store state;
- reconcile presentation last.

Required later tests include repeated restore, rejected partial records, and no unit or revenue duplication.

## 8. Delegation and management findings

Delegation is not implemented. Fixture, stocking, checkout, cleaning, and open/close actions have bounded task/state contracts that may later be assigned to workers or managers.

Condition: employee or manager execution must use the same authoritative mutations and pass a new integration review; it may not directly manipulate presentation state.

## 9. Economy and report findings

- All first-proof money uses integer cents.
- Revenue, cost of goods sold, and included operating expenses are separately explainable.
- Proposed ranges are hypotheses and are not represented as balanced.
- Weak-opening and overbuy scenarios demonstrate recoverable pressure.
- The result is explicitly a session contribution, not audited profit.

No economy blocker remains. Customer demand and final balance evidence remain unavailable by design.

## 10. Persistence and migration findings

- Durable state and restore order are explicit.
- Transient preview and targeting state are excluded.
- The proposed first-store snapshot is additive.
- The existing `FoundationSaveData` version 1 contract must remain unchanged.
- Duplicate stable IDs and unsupported versions are blocking validation errors.

Condition: the Task 2 snapshot must be a distinct temporary vertical-slice contract or mapper target awaiting owner approval, with round-trip and no-duplication tests.

## 11. UI and explainability findings

- Prompts identify action and blocker.
- Invalid feedback does not rely on color alone.
- Opening and closing use actionable prerequisite lists.
- Result information connects outcomes to causes.
- Developer diagnostics are separated from player-facing text.
- Remapping and accessibility requirements are captured without selecting a UI framework.

Usability remains Unity-unverified and requires observed task completion.

## 12. Performance and scaling findings

The proof is bounded to:

- one location;
- one rectangular placement grid;
- a small fixture set;
- six proposed products;
- one delivery;
- a small number of scripted transactions;
- one task.

Deterministic dictionary/list operations are adequate at this scale. Future multi-location aggregation, customer pathfinding, and employee task scheduling are not performance claims of this package.

## 13. Required validation scenarios

The proposed acceptance CSV covers:

- placement bounds and overlap;
- receiving and inventory conservation;
- box-to-loose-to-held-to-shelf transfer;
- shelf rejection;
- checkout totals and idempotency;
- cleaning completion;
- operating transitions;
- snapshot round trip;
- economy result;
- weak-session recovery;
- malformed and duplicate identifiers.

Task 2 and Task 3 must author focused tests. Task 4 must require local Unity and manual evidence.

## 14. Critical blockers

None after correction.

Corrections incorporated during review:

- removed any dependency on customer actors by defining staged transaction baskets;
- separated physical shelf occupancy from inventory quantity authority;
- made completed checkout restoration non-replaying and idempotent;
- stated that the first-store snapshot is additive and cannot replace the foundation save;
- converted final-looking economy values into ranges and deterministic scenarios;
- made future delegation and aggregate simulation explicit later review gates.

## 15. Objective approval conditions

1. Project owner reviews and approves or revises the proposed design and structured data before any values become runtime assets.
2. Task 2 enforces placement determinism, inventory conservation, checkout idempotency, state transitions, stable-ID validation, and additive snapshot behavior in source and authored tests.
3. Task 3 uses explicit inspector references and rolls back cross-domain adapter failures without duplication or silent loss.
4. Task 4 records Unity compilation, tests, scene setup, interactions, persistence, build, launch, logs, and defects before any implementation is accepted.
5. Any customer, employee, delegation, aggregate, or second-location work receives a separate scoped specification and integration review.

## 16. Next owner and artifact

- **Next owner:** Technical Architect Assistant with Systems and Simulation Designer Assistant
- **Next artifact:** Pure C# first-store domain foundation and focused authored EditMode tests
- **Owner gate:** Project owner retains approval of all proposed design, tuning, content, and runtime adoption
