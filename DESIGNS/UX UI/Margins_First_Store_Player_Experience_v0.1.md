# Margins First-Store Player Experience v0.1

## Status and authority

- **Status:** Proposed for project-owner review
- **Scope:** Controls, feedback, onboarding, accessibility, and information flow for the first hands-on store loop
- **Framework status:** No final UI framework is selected or implied
- **Validation status:** Requires Unity implementation and observed task-based playtesting

## Experience objective

The player should understand what they are targeting, what action will occur, why an action is blocked, and whether business state actually changed. Physical work should feel direct and assisted rather than fussy or physics-dependent.

## Control intent

Current foundation behavior is preserved:

- keyboard movement and mouse look;
- `E`-style primary interaction;
- `R`-style quarter-turn rotation while holding;
- explicit save and load debug bindings during development.

Proposed intent for the first-store proof:

| Intent | Default concept | Remapping requirement | Notes |
|---|---|---|---|
| Move | directional input | fully remappable | Support simultaneous axes |
| Look | pointer delta | sensitivity and invert controls | No forced smoothing |
| Primary interact | one context action | fully remappable | Pick up, open, remove, scan, apply cleaning progress |
| Cancel/back | one cancel action | fully remappable | Restores prior valid state where possible |
| Rotate placement | quarter turn | fully remappable | Repeated press; no analog precision required |
| Placement mode | explicit enter/confirm flow | fully remappable | Do not overload accidental pickup |
| Open/close control | world control plus confirmation | fully remappable | Prevent accidental day-state changes |
| Pause/menu | explicit | fully remappable | Cursor released and simulation paused where supported |

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

Proposed target priority:

1. held-unit placement target;
2. active checkout item or correction;
3. delivery container opening or unit removal;
4. loose product pickup;
5. fixture placement handle;
6. cleaning or maintenance task;
7. store open/close control.

This priority is a tuning proposal and requires in-engine usability review.

## Flow sheets

### Fixture placement

1. Target an unplaced fixture or placement handle.
2. Enter placement mode; show footprint and store grid locally.
3. Move and rotate the preview.
4. Show valid state with shape, color, and concise reason text.
5. Confirm to commit or cancel to restore the prior exact state.
6. On rejection, remain in placement mode and identify the first blocking cell or boundary.

Required feedback:

- valid footprint outline;
- invalid outline plus icon/pattern;
- occupied-cell indication;
- quarter-turn orientation cue;
- “confirm,” “rotate,” and “cancel” actions;
- no state change on invalid confirmation.

### Delivery box

1. Target sealed box: `Open delivery`.
2. Open once; lid or label state changes when art exists.
3. Target contents: `Take [product] (x remaining)`.
4. Unit transfers to held state only after domain acceptance.
5. Empty state reads `Delivery empty`; dismissing it cannot discard units.

### Pickup and stocking

1. Target a loose or boxed unit.
2. Pick up; prompt changes to held controls.
3. Aim at a compatible shelf point.
4. Preview shows destination and remaining capacity.
5. Confirm to transfer and snap.
6. If blocked, unit remains held and the shelf state remains unchanged.
7. Cancel returns the unit to its prior valid inventory location or a defined loose recovery point.

### Checkout

1. Activate the checkout station with a staged basket.
2. Target or present each test product to the scanner.
3. Each accepted scan gives distinct visual and audio-ready feedback and updates line quantity and subtotal.
4. Invalid product or insufficient stock shows a reason and changes no line.
5. Correction removes the last or selected line quantity with confirmation when destructive.
6. Complete once; repeated completion shows `Already completed` and never consumes inventory again.

No customer, payment, cash-drawer, or change-making interaction is part of this proof.

### Cleaning or maintenance

1. Target the task area.
2. Show task name and remaining work.
3. Primary interaction applies bounded progress.
4. Progress uses more than color: meter, state text, or world-state change.
5. Completion gives a persistent completed state and cannot award completion twice.

### Opening and closing

Opening control:

- available while `preparing`;
- shows a checklist of unmet prerequisites;
- confirmation is required only when all prerequisites pass;
- rejection links each blocker to an actionable location or object when practical.

Closing control:

- transitions to `closing` and stops new baskets;
- shows unresolved transaction, held unit, or required task;
- final close presents the result before returning to `closed`.

## Prompt model

Prompts should use:

`[input] Action — optional short state or blocker`

Examples:

- `[Interact] Open delivery`
- `[Interact] Take Cola Can — 5 left`
- `[Interact] Stock Cola Can — shelf full`
- `[Rotate] Turn fixture 90°`
- `[Confirm] Place shelf — blocked by checkout counter`
- `[Interact] Scan Cola Can — 150¢`
- `[Interact] Clean spill — 2/4`
- `[Interact] Open store — 1 prerequisite missing`

Do not show raw enum names, stable IDs, stack traces, or financial formulas to the player.

## Valid and invalid feedback

Every consequential interaction uses at least two channels:

- shape, icon, text, position, animation, or sound-ready event;
- color only as a supporting channel.

Valid:

- positive outline or material;
- concise verb;
- destination or amount;
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
| Duplicate checkout completion | Return existing summary |
| Close with active work | Remain closing and focus the blocker |
| Load rejects a record | Preserve other accepted records; show a non-destructive diagnostic in development |
| Player becomes stuck in interaction mode | Cancel/back restores normal movement and cursor state |

## Beginner onboarding

Use a short, contextual sequence rather than a separate tutorial level:

1. Look and move inside the empty store.
2. Place the checkout counter.
3. Place one shelf.
4. Open the delivery.
5. Take and stock one unit.
6. Open the store.
7. Scan and complete one staged basket.
8. Complete the cleaning task.
9. Close and read the result.
10. Save, reload, and confirm continuation.

Rules:

- One current objective at a time.
- Guidance becomes dismissible after opening.
- Completed steps remain viewable in a compact checklist.
- No timed tutorial failure.
- Repeated failures may reveal a more explicit hint, not silently perform the task.
- Developer acceptance must include a clean run with guidance and a run after guidance is dismissed.

## Accessibility requirements

Proposed minimum requirements for this proof and any surviving production work:

- full remapping plan for gameplay actions;
- separate horizontal and vertical look sensitivity;
- invert-Y option;
- toggle or hold choice where an interaction otherwise requires sustained input;
- no required rapid tapping;
- color-independent valid and invalid indicators;
- scalable prompt and result text;
- readable contrast and background treatment for text;
- captions or visual equivalents for functional audio cues when audio is added;
- reduced camera motion and optional head-bob removal if camera motion is introduced;
- pause availability during onboarding and result review;
- interaction timing not dependent on hearing;
- clear focus state for keyboard and gamepad navigation when menus are later implemented;
- persistent settings across save and reload.

Exact accessibility conformance targets and final platform/input matrix remain unresolved owner decisions.

## Information placement

| Information | World | Minimal HUD | Menu/result |
|---|---:|---:|---:|
| Current target and action | highlight/state | prompt | no |
| Placement footprint and blocked cells | preview/grid | short reason | no |
| Box open/empty state | lid/label/content | remaining quantity while targeted | delivery detail only if later needed |
| Held product identity | visible held object | name and quantity | no |
| Shelf capacity | visible occupancy | targeted capacity | store detail later |
| Checkout scan feedback | scanner/item | active lines and subtotal | completed summary |
| Cleaning progress | world-state change | targeted progress | result note |
| Opening prerequisites | open/close control state | blocker count | checklist |
| Store state | sign/lighting-ready cue | compact state | session detail |
| Cash and contribution | no constant world requirement | optional compact cash | close result |
| Stable IDs and validation diagnostics | no | no | developer-only diagnostics |

## Usability acceptance tasks

- Place, rotate, move, and cancel a fixture without unintended state loss.
- Identify why one placement is invalid without reading documentation.
- Open a delivery and stock one unit without duplicating it.
- Correct a checkout line and complete a sale once.
- Finish the cleaning task and understand that it is complete.
- Find every blocker preventing opening or closing.
- Resume after reload and identify the current store state.

Passing tests require observed Unity behavior. This sheet alone does not establish usability.
