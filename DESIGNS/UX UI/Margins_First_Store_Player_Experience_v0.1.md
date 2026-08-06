# Margins First-Store Player Experience v0.1

## Status and authority

- **Status:** Proposed for project-owner review
- **Scope:** Controls, feedback, onboarding, accessibility, and information flow for the first hands-on store loop
- **Framework status:** No final UI framework is selected or implied
- **Validation status:** Requires Unity implementation and observed task-based playtesting
- **Owner-direction update:** Incorporates the July 29, 2026 owner playtest observations and the July 30, 2026 art-and-UX direction session. Recommendations not stated by the owner remain proposed.

## Experience objective

The player should understand what they are targeting, what action will occur, why an action is blocked, and whether business state actually changed. Physical work should feel direct and assisted rather than fussy, physics-dependent, or broken into unnecessary staged rituals.

The store should remain the primary information surface during hands-on operation. HUD and report layers should explain ambiguity and business consequences without competing with the physical work.

## Owner-stated experience direction

Carry the following direction into the next prototype and validation pass:

- A physical shelf should be a forgiving semantic target. The player should not need to aim at a tiny hidden placement box or a specific snap point.
- Checkout should preserve item-based scanning. The physical product should be the meaningful target or presented object; the register is primarily a feedback surface.
- Opening a delivery reveals contents. It should not look as though the box unloads itself.
- Setup, opening, closing, cleaning, and exterior interactions should exist only when they create meaningful business decisions or believable work.
- Daily operation should feel seamless rather than segmented by arbitrary stages.
- Closing should not force a full-screen end-of-day summary. A report may post in the background and remain available for optional review.
- Financial totals require a transparent validation example before prominent presentation is trusted.

## Control intent

Current foundation behavior is preserved:

- keyboard movement and mouse look;
- `E`-style primary interaction;
- quarter-turn rotation while holding or placing;
- explicit save and load debug bindings during development.

Proposed intent for the first-store proof:

| Intent | Default concept | Remapping requirement | Notes |
|---|---|---|---|
| Move | directional input | fully remappable | Support simultaneous axes |
| Look | pointer delta | sensitivity and invert controls | No forced smoothing |
| Primary interact | one context action | fully remappable | Pick up, open, take, stock, scan, clean, or operate the focused semantic target |
| Cancel/back | one cancel action | fully remappable | Restores prior valid state where possible |
| Rotate placement | quarter turn | fully remappable | Repeated press; no analog precision required |
| Placement mode | explicit enter/confirm flow | fully remappable | Do not overload accidental pickup |
| Store-state control | contextual open or close action | fully remappable | Use confirmation only for meaningful destructive or state-changing choices |
| Pause/menu | explicit | fully remappable | Cursor released and simulation paused where supported |
| Company management | explicit toggle | fully remappable | Opens without ending physical store operation where simulation rules allow |

Final bindings, gamepad coverage, and binding-display implementation remain owner choices. No action may be permanently hard-coded without a remapping path in the production input design.

## Interaction targeting

- Use a centered view ray or equivalent deterministic focus query.
- Only one primary target owns the prompt at a time.
- Resolve overlapping candidates by explicit priority, then distance, then stable identifier.
- Maximum interaction distance is inspector-configurable.
- Target highlight supplements, but never replaces, a text or icon prompt.
- Loss of target immediately removes the prompt and preview.
- A blocked action remains targetable so the player can learn why it is blocked.
- Hidden object discovery is not a UX behavior; scene references and target types must be explicit.
- Target the meaningful physical object rather than requiring the player to discover an arbitrary sub-collider.
- For stocking, the fixture or broad shelf face is the target. Compatible snap points are placement results selected by the system, not precision aim requirements.
- For checkout, the physical product owns scanning interaction when a product is present. The register owns line, subtotal, correction, and completion feedback.
- Small handles or control panels are acceptable only when their physical purpose is visually obvious and their target area is forgiving.

Proposed target priority:

1. held-unit compatible shelf or placement target;
2. active checkout product;
3. checkout correction or completion control;
4. delivery container opening or visible unit removal;
5. loose product pickup;
6. fixture placement handle;
7. cleaning or maintenance task;
8. store-state control.

This priority remains a tuning proposal and requires in-engine usability review.

## Flow sheets

### Fixture placement

1. Target an unplaced fixture kit or obvious placement handle.
2. Enter placement mode; show footprint and store grid locally.
3. Move and rotate the preview.
4. Show valid state with shape, color-independent marking, and concise reason text.
5. Confirm to commit or cancel to restore the prior exact state.
6. On rejection, remain in placement mode and identify the first blocking cell or boundary.

Required feedback:

- valid footprint outline;
- invalid outline plus icon or pattern;
- occupied-cell indication;
- quarter-turn orientation cue;
- confirm, rotate, and cancel actions;
- no state change on invalid confirmation.

### Delivery box

1. Target the sealed physical box: `Open delivery`.
2. Opening changes the lid, seal, or label state and reveals physically represented contents.
3. Target one visible product: `Take [product] — [x] left`.
4. Exactly one physical unit transfers to held state only after domain acceptance.
5. Remaining contents stay visibly and logically in the box.
6. Empty state reads `Delivery empty`.
7. Moving or dismissing the box cannot silently discard or auto-transfer contents.

### Pickup and stocking

1. Target a loose or boxed physical unit.
2. Pick up; prompt changes to held controls.
3. Aim at the compatible physical shelf or broad fixture face.
4. The system selects a deterministic compatible snap destination and previews it.
5. Preview shows destination, remaining capacity, and any blocker.
6. Confirm to transfer and snap.
7. If blocked, the unit remains held and the shelf state remains unchanged.
8. Cancel returns the unit to its prior valid inventory location or a defined loose recovery point.

The player should not need to look directly at a small individual snap point unless a future expert-placement mode is deliberately approved.

### Checkout

1. A staged or customer-presented basket places the next physical product in a readable scanning position.
2. Target or present the physical product to the scanner.
3. Each accepted scan gives distinct world, visual, and audio feedback and updates the register line quantity and subtotal.
4. The register display communicates current line, subtotal, correction state, and completion readiness.
5. Invalid product or insufficient stock shows a reason and changes no line.
6. Correction removes the last or selected line quantity with confirmation only when meaningfully destructive.
7. Complete once; repeated completion shows `Already completed` and never consumes inventory again.

The register should not replace the item-based scanning rhythm. No customer-payment, cash-drawer, or change-making interaction is required for this proof unless separately approved.

### Cleaning or maintenance

1. The task appears in a believable location and state.
2. Target the affected area or required tool.
3. Show task name and remaining work only while relevant.
4. Primary interaction applies bounded progress or performs one clear action.
5. Progress uses more than color: meter, state text, sound, animation, decal change, or world-state change.
6. Completion gives a persistent completed state and cannot award completion twice.

Cleaning is a reusable operating task, not a required ritual in every short session. Its frequency and consequence require later system and economy tuning.

### Opening and closing

Opening:

- The store can open when required business prerequisites are satisfied.
- Missing prerequisites are shown through a compact actionable list only when opening is attempted.
- Confirmation is used only when opening has meaningful consequences.
- The player should not need to perform unrelated exterior or staging interactions merely to change store state.

Closing:

- Closing stops or resolves new customer intake according to the operating rules.
- Active transaction, held unit, or genuine unresolved work may block closing with an actionable reason.
- Successful closing posts the shift result in the background.
- Storefront state, lighting, ambience, and staff behavior communicate closure.
- A concise notification may announce that the report is ready.
- The player may review the report immediately or continue without a mandatory full-screen modal.

## Prompt model

Prompts should use:

`[input] Action — optional short state or blocker`

Examples:

- `[Interact] Open delivery`
- `[Interact] Take Mile 7 Cola — 5 left`
- `[Interact] Stock Mile 7 Cola — shelf full`
- `[Rotate] Turn fixture 90°`
- `[Confirm] Place shelf — blocked by checkout counter`
- `[Interact] Scan Mile 7 Cola — $1.49`
- `[Interact] Clean spill — 2/4`
- `[Interact] Open store — 1 prerequisite missing`

Prompt rules:

- Lead with the action that will occur.
- Show capacity, amount, or blocker only when useful.
- Do not duplicate the same instruction in several persistent HUD panels.
- Do not show raw enum names, stable IDs, stack traces, or financial formulas to the player.
- Do not confirm obvious reversible success with unnecessary text when world feedback is sufficient.

## HUD hierarchy

Default first-person HUD:

1. small reticle;
2. focused interaction prompt;
3. brief success, failure, or business-state feedback;
4. optional compact cash display if playtesting proves it useful.

Contextual only:

- held item identity;
- placement controls;
- targeted shelf capacity;
- active checkout lines and subtotal;
- current cleaning progress;
- opening or closing blockers;
- one current guided objective.

Menu or report only:

- detailed sales, COGS, expenses, contribution, and cash reconciliation;
- completed shift history;
- full checklist history;
- location, employee, policy, and portfolio detail.

A permanent live ledger, large checklist, large objective card, and control legend should not all remain visible during ordinary operation.

## Valid and invalid feedback

Every consequential interaction uses at least two channels:

- shape, icon, text, position, animation, world-state change, or sound-ready event;
- color only as a supporting channel.

Valid:

- visible world-state change;
- concise verb or amount when clarification is useful;
- destination or capacity where relevant;
- one bounded confirmation cue.

Invalid:

- no authoritative mutation;
- clear reason;
- retained target when corrective action is possible;
- error cue rate-limited to avoid spam.

## Error recovery

| Error | Recovery |
|---|---|
| Placement overlaps or leaves bounds | Preserve previous accepted placement; keep preview active |
| Box opened twice | Treat as idempotent; show already open |
| Attempted unit overdraw | Keep all quantities unchanged; show available units |
| Held destination becomes invalid | Keep unit held; allow retarget or cancel |
| Shelf placement succeeds visually but domain transfer fails | Roll back visual placement and report configuration fault |
| Invalid scan | Add no line and consume no stock |
| Duplicate checkout completion | Return existing completion state |
| Close with active work | Remain in the current state and focus the genuine blocker |
| Shift report cannot post | Preserve the completed shift and expose a non-destructive diagnostic |
| Load rejects a record | Preserve other accepted records; show a non-destructive diagnostic in development |
| Player becomes stuck in interaction mode | Cancel/back restores normal movement and cursor state |

## Beginner onboarding

Use a short contextual sequence rather than a separate tutorial level:

1. Look and move inside the mostly empty store.
2. Place the checkout counter.
3. Open the delivery.
4. Take one visible product.
5. Stock it by targeting the compatible shelf generally.
6. Open the store when actual prerequisites are met.
7. Scan one physical product and complete one staged sale.
8. Complete one believable cleaning or maintenance task if present in the scenario.
9. Close the store and receive a non-blocking report-ready notification.
10. Open the company overview or save, reload, and continue.

Rules:

- One current objective at a time.
- Guidance becomes dismissible after opening.
- Completed steps may remain available in an optional shift-notes view, not a permanently large checklist.
- No timed tutorial failure.
- Repeated failures may reveal a more explicit hint, not silently perform the task.
- World signage and object state should carry guidance before a floating objective beacon is used.
- Developer acceptance must include a clean guided run and a run after guidance is dismissed.

## Management-screen UX direction

Use stable top-level navigation that can grow from one store to a portfolio:

- Overview
- Locations
- People
- Inventory
- Finance
- Properties
- Expansion

Each screen should prioritize:

1. current state;
2. items requiring attention;
3. understandable cause;
4. available decision;
5. optional deeper detail.

The first prototype only requires a compact company overview. Default Unity control styling and dense rows are acceptable for temporary validation but are not a production presentation target.

## Accessibility requirements

Proposed minimum requirements for this proof and any surviving production work:

- full remapping plan for gameplay actions;
- separate horizontal and vertical look sensitivity;
- invert-Y option;
- toggle or hold choice where an interaction otherwise requires sustained input;
- no required rapid tapping;
- color-independent valid and invalid indicators;
- scalable prompt, menu, and report text;
- readable contrast and background treatment for text;
- captions or visual equivalents for functional audio cues when audio is added;
- reduced camera motion and optional head-bob removal if camera motion is introduced;
- pause availability during onboarding and optional report review;
- interaction timing not dependent on hearing;
- clear focus state for keyboard and gamepad menu navigation;
- persistent settings across save and reload.

Exact accessibility conformance targets and final platform/input matrix remain unresolved owner decisions.

## Information placement

| Information | World | Minimal HUD | Menu or report |
|---|---:|---:|---:|
| Current target and action | highlight or state | contextual prompt | no |
| Placement footprint and blocked cells | preview and grid | short reason | no |
| Box open or empty state | lid, label, visible contents | remaining quantity while targeted | delivery detail only if later needed |
| Held product identity | visible held object | contextual name | no |
| Shelf capacity | visible occupancy | targeted capacity | store detail later |
| Checkout scan feedback | physical item, scanner, register | active line and subtotal | completed transaction detail |
| Cleaning progress | world-state change | targeted progress | optional result note |
| Opening prerequisites | control state and world cues | blocker count after attempted opening | compact checklist |
| Store state | sign, lighting, ambience | optional compact state | session detail |
| Cash | no constant world requirement | optional compact display | company overview |
| Sales, COGS, expenses, contribution | register or office cues only | no permanent ledger | finance and shift report |
| Shift result | store state change | concise report-ready notification | optional report |
| Stable IDs and diagnostics | no | no | developer-only diagnostics |

## Financial presentation validation

Before financial values receive prominent styling, validate one transparent example containing:

- starting company cash;
- units and selling price;
- gross sales;
- sale-time unit cost;
- COGS;
- operating expenses;
- contribution or operating result;
- purchases or inventory changes;
- ending company cash.

The report should label each concept accurately and make it possible to explain why cash and profit-like measures differ.

## Usability acceptance tasks

- Place, rotate, move, and cancel a fixture without unintended state loss.
- Identify why one placement is invalid without reading documentation.
- Stock a held unit by targeting the physical shelf generally rather than a tiny hidden region.
- Understand which snap destination will receive the held product.
- Open a delivery and take exactly one visible unit without auto-unloading the box.
- Scan the physical checkout item and understand the register feedback.
- Correct a checkout line and complete a sale once.
- Finish a cleaning task and understand that it is complete.
- Find every genuine blocker preventing opening or closing.
- Close the store without a mandatory full-screen result interruption.
- Open the posted report voluntarily and reconcile its numbers against a manual example.
- Resume after reload and identify the current store state.

Passing tests require observed Unity behavior. This sheet alone does not establish usability.
