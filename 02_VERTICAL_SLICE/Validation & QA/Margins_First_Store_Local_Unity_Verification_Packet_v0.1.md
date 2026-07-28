# Margins First-Store Local Unity Verification Packet v0.1

## Status and authority

- **Status:** Proposed for project-owner review
- **Execution status:** Not run
- **Implementation marker:** Draft implementation — Unity verification pending
- **Required editor:** Unity `6000.5.5f1`
- **Remediation owner:** Technical Architect Assistant
- **Sequence owner:** Margins Producer and Roadmap Assistant
- **Evidence owner:** Data, Validation, and QA Engineer Assistant

This packet is the desktop handoff for the complete first-store stack. It does not
assert compilation, test, scene, NavMesh, build, executable, or playability results.
Record every result in
`Margins_First_Store_Local_Unity_Evidence_Record_v0.1.md`.

## Stack under review

| Order | Draft PR | Branch | Target | Review purpose |
|---|---:|---|---|---|
| 1 | #13 | `agent/define-first-store-vertical-slice-package` | `main` | Proposed design, tuning, content, acceptance scenarios, integration review |
| 2 | #14 | `agent/implement-first-store-domain-foundation` | `agent/define-first-store-vertical-slice-package` | Engine-light domain rules, snapshot contract, authored domain tests |
| 3 | #15 | `agent/implement-first-store-unity-adapters` | `agent/implement-first-store-domain-foundation` | Explicit Unity-facing adapters and authored adapter tests |
| 4 | #16 | `agent/prepare-first-store-local-validation` | `agent/implement-first-store-unity-adapters` | This local execution and evidence packet |

Fetch all four branch refs, check out
`agent/prepare-first-store-local-validation`, pull with fast-forward only, and record
`git rev-parse HEAD`. The final branch contains the complete ancestor chain. Before
opening Unity, compare the four PR base/head pairs and confirm that each branch is
ahead of, and not behind, its stated target.

## Required local sequence

Do not skip a failed step. Record the defect, route the correction to its owning
branch, refresh descendants, and restart at the earliest affected step.

1. [ ] **Pull the complete stacked branch chain.** Fetch `main` and all four named
   branches, check out `agent/prepare-first-store-local-validation`, pull
   fast-forward only, record the exact commit, and inspect
   `main..HEAD` before opening Unity.
2. [ ] **Open Unity `6000.5.5f1`.** Open the project at `CODE/Unity/Margins`.
   Do not accept an editor-version upgrade.
3. [ ] **Allow import and `.meta` reconciliation.** Wait for import to become idle.
   Record newly generated or changed `.meta` files. Do not hand-edit Unity
   serialized files to resolve an import issue.
4. [ ] **Inspect Console.** Capture all import warnings and errors before clearing
   or filtering anything. Separate pre-existing messages from stack-introduced
   messages with evidence.
5. [ ] **Repair compilation errors.** Route pure-domain defects to the Task 2
   branch and adapter defects to the Task 3 branch. Update descendant branches,
   reopen the final head, and repeat import and Console inspection. Record every
   correction; do not reinterpret a compile failure as a test result.
6. [ ] **Run EditMode tests.** Run the complete EditMode suite, then the focused
   first-store domain and adapter fixtures. Record counts, duration, failures,
   stack traces, and result-file or screenshot locations.
7. [ ] **Run PlayMode tests.** Run the complete PlayMode suite. Record counts,
   duration, failures, stack traces, and result-file or screenshot locations.
8. [ ] **Create or update required scene objects through Unity.** Use the editor
   for all scene and prefab work. Add only the minimum objects required by the
   approved first-store proof; do not hand-author serialized YAML.
9. [ ] **Connect inspector references.** Wire the grid origin, explicit fixture
   instances, inventory component, delivery box, physical product, shelf, hold
   point, checkout, cleaning task, operating controller, and persistence mapper.
   Validate that every adapter references the same intended inventory authority.
10. [ ] **Create approved runtime assets through Unity.** Only after owner approval
    of the Task 1 proposed data, create the minimum ProductDefinition and other
    required runtime assets in Unity. Record stable IDs and source-row mappings.
11. [ ] **Verify fixture placement.** Exercise place, rotate, reject overlap,
    reject out-of-bounds, move, remove, cancel/recovery, and required-fixture
    opening checks. Confirm accepted state and visible state agree.
12. [ ] **Verify receiving and stocking.** Exercise sealed-box rejection, open,
    box-to-loose removal, loose-to-held pickup, valid stock, occupied or
    incompatible rejection, and conservation after each action.
13. [ ] **Verify checkout.** Begin one scripted or manually staged transaction,
    scan, correct, complete, and request completion again. Confirm integer-cent
    totals and one-time stock consumption.
14. [ ] **Verify cleaning.** Apply progress, invalid input where practical,
    completion clamping, and repeat completion. Confirm clear world and prompt
    feedback.
15. [ ] **Verify opening and closing.** Exercise invalid direct open, preparation,
    missing-prerequisite rejection, open, closing, blocked finalization, completed
    finalization, result acknowledgement, and retained totals.
16. [ ] **Verify save, exit, reload, and restoration.** Save accepted first-store
    state, fully exit the executable or editor play session as defined by the
    approved save path, reload, and compare fixture, inventory, delivery,
    checkout, cleaning, operating, and physical product-placement state. Confirm
    that completed checkout consumption is not replayed.
17. [ ] **Produce a Windows x64 development build.** Record the exact output path,
    build report, warnings, size, and whether symbols and development diagnostics
    were included.
18. [ ] **Launch and manually exercise controls.** Launch the produced executable,
    verify movement/look and every first-store interaction, exit cleanly, and
    inspect `Player.log`.

## Inspector and runtime wiring checklist

All references are explicit. A missing reference is a blocked configuration, not
permission to add runtime object discovery.

- [ ] `FirstStoreInventoryComponent`: approved ProductDefinition references,
  delivery-container, loose, held, and shelf locations, capacities, and starting
  quantities.
- [ ] `FixturePlacementController`: grid origin, cell size, dimensions, and every
  placeable fixture reference.
- [ ] `PlaceableFixtureComponent`: unique stable instance ID, positive footprint,
  and optional complete preview-material set.
- [ ] `DeliveryBoxComponent`: unique container ID, delivery location, loose
  destination, product definition, and the shared inventory component.
- [ ] `StockingController`: shared inventory, physical ProductItem, ShelfFixture,
  hold point, loose/held/shelf locations, and stable snap-point ID.
- [ ] `CheckoutStationComponent`: shared inventory, shelf location, product
  definitions, and integer-cent prices.
- [ ] `CleaningTaskComponent`: stable task ID and positive required progress.
- [ ] `StoreOperatingController`: session ID, fixture/stocking/checkout/cleaning
  references, and the complete required-fixture ID list.
- [ ] `FirstStorePersistenceMapperComponent`: the same fixture, inventory,
  delivery, checkout, cleaning, and operating instances used by the scene.
- [ ] Existing `PlacementSaveController`: unchanged foundation product, fixture,
  and scene-product references remain valid.

## Acceptance evidence order

Use
`Margins_First_Store_Acceptance_Scenarios_Proposed_v0.1.csv` as the scenario
authority after the project owner approves or revises it.

| Run order | Scenario IDs | Evidence focus |
|---:|---|---|
| 1 | `fs-accept-014` | Invalid and duplicate configuration is blocked before mutation |
| 2 | `fs-accept-001`, `fs-accept-002` | Fixture determinism, bounds, overlap, prior-state preservation |
| 3 | `fs-accept-003` | Sealed/open delivery behavior and conserved removal |
| 4 | `fs-accept-004`, `fs-accept-005` | Physical/domain stocking agreement and rejected-action atomicity |
| 5 | `fs-accept-006`, `fs-accept-007` | Exact checkout totals and idempotent completion |
| 6 | `fs-accept-008` | Bounded, idempotent cleaning completion |
| 7 | `fs-accept-009`, `fs-accept-010` | Valid operating sequence, prerequisite explanations, retained totals |
| 8 | `fs-accept-011` | Save/restart equality and no replayed sale |
| 9 | `fs-accept-012`, `fs-accept-013` | Owner-approved tuning fixture and recoverable result explanation |

## Persistence gate

The Task 3 `FirstStorePersistenceMapperComponent` captures and restores the
first-store domain state in memory. It deliberately does not replace or silently
modify `FoundationSaveData` or `PlacementSaveController.CurrentSaveVersion == 1`.
The source stack does not yet choose a first-store JSON file path, save-slot
policy, atomic write strategy, or foundation-sidecar coordination.

Step 16 cannot receive a passing end-to-end result until the project owner chooses
one of these paths:

1. approve a narrowly scoped first-store file controller and explicit coordination
   with the unchanged foundation placement sidecar; or
2. approve a revised additive envelope or migration contract and request a
   separate implementation review.

Until then, record the in-memory mapper round trip separately and mark
save/exit/reload **blocked**, not passed.

## Defect severity and remediation

| Severity | Meaning | Required action |
|---|---|---|
| Blocker | Data loss/duplication, unsupported editor state, stack cannot compile, or required proof cannot run | Stop; fix owning branch; refresh descendants; restart affected sequence |
| Major | Required interaction, test, restore, build, or feedback contract is wrong or unavailable | Do not merge owning PR; fix and rerun affected and downstream checks |
| Minor | Non-blocking clarity, presentation, or maintainability defect inside approved scope | Record owner and correction timing; rerun targeted check if corrected |
| Observation | Evidence or follow-up that does not violate the current proof | Record without silently expanding scope |

The Technical Architect Assistant owns C# remediation. Data/QA records reproduction
and evidence. The Producer decides the earliest safe restart point and prevents
downstream review from outrunning an unresolved blocker or major defect.

## Exact review and merge order

No PR should be merged until the owner has reviewed the complete stack and the
required local evidence has a final recommendation.

### Review before merge

1. Review PR #13 first. Approve or revise every item marked
   `Proposed for project-owner review`, including data rows and unresolved choices.
2. Review PR #14 against the approved Task 1 contract. Confirm domain invariants,
   additive snapshot disposition, and authored-test coverage.
3. Review PR #15 against Tasks 1 and 2. Confirm explicit references, physical/domain
   boundaries, and all Unity-unverified concerns.
4. Review PR #16 and this packet.
5. Execute the complete 18-step sequence at the PR #16 head.
6. Resolve blocker and major defects in their owning branches, refresh descendant
   branches, and repeat affected review and verification.
7. Record exactly one final recommendation: **continue**, **adjust**, or **stop**.

### Merge only after an accepted `continue` recommendation

1. Merge PR #13 into `main`.
2. Confirm PR #14 now targets or is retargeted to `main`; compare its new base/head,
   then merge PR #14.
3. Confirm PR #15 now targets or is retargeted to `main`; compare its new base/head,
   then merge PR #15.
4. Confirm PR #16 now targets or is retargeted to `main`; compare its new
   base/head, then merge it last.
5. After every merge, confirm `main` contains the intended commit and that the next
   PR contains only its own layer. Stop on an unexpected diff or conflict.

This packet does not authorize automated merge or auto-merge.

## Stop conditions

Stop and record evidence if any of the following occurs:

- Unity reports an editor-version mismatch or requests an unapproved upgrade.
- Import changes Packages, ProjectSettings, URP settings, or unrelated serialized
  content.
- Compilation fails after the responsible source correction is identified.
- Any transfer duplicates or loses units.
- A completed checkout consumes stock more than once.
- Restore partially mutates accepted state after rejecting a snapshot.
- Physical shelf occupancy disagrees with authoritative inventory after restore.
- The Windows build fails to launch or `Player.log` contains an unresolved blocker.
