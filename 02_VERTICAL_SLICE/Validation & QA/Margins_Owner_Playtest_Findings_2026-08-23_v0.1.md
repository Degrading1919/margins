# Margins Owner Playtest Findings — 2026-08-23 v0.1

## Status and authority

- **Status:** Owner playtest evidence plus approved near-term remediation direction.
- **Evidence date:** August 23, 2026.
- **Build context:** Post-PR #40 Stage 5/6 continuity, management, generated-location, and persistence implementation.
- **Authority:** This file records observed player-experience evidence and the project owner's remediation priorities. It does not override approved foundational decisions in `00_ADMIN/Decisions`.

## Overall result

The backend and automated regression foundation are substantially ahead of the current player-facing experience. The tested build is not yet acceptable as a coherent vertical-slice gameplay loop even though many underlying systems are functional and heavily tested.

The immediate priority is therefore **playtest remediation**, not another backend expansion wave.

The next owner-facing proof must make this sequence understandable and continuous without developer knowledge:

**Launch → New Business → startup/setup → operate first day → checkout → close store → end day/review → save → title → load → continue.**

Broader production-content conversion can continue in parallel where it does not distract from this loop, but the next major implementation gate is player-facing coherence.

---

## High-severity findings

### Session, title, and New Business lifecycle

Observed:

- the title screen visibly overlays the live game world;
- choosing New Business drops directly into the existing scene;
- returning to title behaves like pausing the current session rather than leaving gameplay;
- New Business restores the same authored startup scene rather than presenting a proper setup/loading flow.

Approved remediation direction:

- title/front-end presentation must be visually and behaviorally separate from the live session;
- New Business must enter a setup flow before gameplay;
- setup must support business name, business colors, a later-expandable logo selection field, seed entry, and difficulty selection;
- difficulty **labels and gameplay modifiers remain unresolved** and must not be invented by implementation;
- returning to title must leave the active gameplay presentation cleanly rather than exposing the frozen world behind the menu.

### Checkout is not yet a coherent human interaction

Observed:

- customers do not naturally place merchandise on the checkout counter;
- the player must decipher multiple overlapping prompts/targets to scan items;
- the current checkout interaction can leave an incomplete session after the customer is gone;
- that stale session can block both closing and saving;
- the current checkout presentation still exposes prototype/staged-checkout behavior rather than a finished player flow.

Approved remediation direction:

- customers present their actual selected items on the checkout counter;
- interacting with checkout enters a clear dedicated checkout mode;
- during checkout, normal movement and unrelated world interactions are unavailable while scanning/payment/exit remain clear;
- completing or cancelling checkout must reliably clear the active checkout state;
- no invisible or unreachable checkout state may continue blocking store closure or persistence;
- item-based scanning remains required under FD-016.

### Store close and day transition are disconnected

Observed:

- closing can become blocked by stale checkout state;
- once the store is successfully closed, the next required action is not obvious;
- the player is not naturally led from store close into day completion/reporting/next day.

Approved remediation direction:

- close-store blockers must correspond to visible, resolvable player state;
- closing must reliably drain/resolve customers and checkout state;
- after closure, the game must clearly surface the day-completion action and resulting summary;
- End Day must continue to use the existing authoritative overnight/delegated-day transition rather than introducing a second time authority.

### New-business world variation is currently unresolved against approved city direction

Owner playtest expectation included entering a seed and generating the city when starting a new game.

This conflicts with current FD-005, which approves one fictional recognizable city built as expanding handcrafted districts from reusable modules and explicitly says runtime procedural generation of the full city is not current direction.

Therefore:

- **do not implement a fully procedurally generated city under this playtest-remediation wave;**
- the New Business setup may expose/store a seed only if it has a bounded valid use;
- seed-driven layout/content variation, authored-district variation, or reopening FD-005 requires a separate owner decision before implementation changes the city model.

---

## Interaction and usability remediation

The following are approved near-term player-experience changes from the playtest:

- increase/tune walking speed; current movement feels too slow and clunky;
- remove positional walking/sprinting camera wobble rather than relying on the player to disable it;
- remove the persistent center crosshair and target-box treatment; use a restrained interactable highlight/outline where needed;
- simplify contextual prompts to the key plus the action, with short blocker text only when necessary;
- held-object actions must remain available while held and must not require looking at an arbitrary object unless the target itself matters;
- remove merchandise rotation guidance while carrying normal stock;
- make Build Mode unmistakable with a strong temporary presentation treatment such as construction-tape edge framing or equivalent;
- add a small persistent gameplay HUD for essential always-needed information, especially current cash and a small number of global actions such as the Owner Phone; avoid turning it into a permanent tutorial wall;
- a minimap is a strong current-working-direction candidate, but its exact scope/presentation is not locked by this file.

---

## Tactile object-flow findings

### Delivery boxes

Observed:

- pickup/open/remove-item handling improved;
- opening needs an obvious prompt while carrying the sealed box;
- empty boxes have no clear end state;
- the old receiving station is redundant now that deliveries arrive outside.

Current working direction:

- keep delivery boxes physically handled;
- make Open available while holding a sealed box;
- add a simple intentional empty-container outcome such as discard/recycle/flatten, using the smallest fitting implementation;
- remove or repurpose the receiving station instead of preserving it only because older implementation used it.

### Mop and bucket

Observed:

- the mop can be removed but then placed arbitrarily;
- the bucket and mop do not behave as one understandable tool kit.

Current working direction:

- carry the mop bucket as the primary object;
- set the bucket down to take out/use the mop;
- return the mop to that bucket when done;
- prevent arbitrary world placement of the mop when the expected completion action is returning it to the bucket.

### Customers and carried goods

Current working direction:

- remove the generic basket presentation from convenience-store customers;
- show selected merchandise in customer hands and/or on the checkout counter during service;
- preserve the underlying exact physical-unit/inventory authority rather than creating presentation-only sale state.

---

## Settings and menu findings

Approved remediation items:

- checkbox controls should not have full-row background blocks extending across the settings panel;
- Look Sensitivity should use a discrete numeric stepper with left/right controls rather than the current slider;
- the controls-list scrollbar must match the menu visual language;
- rename/remove the player-facing `Attack` binding; it is not an appropriate Margins action name;
- preserve input rebinding and UI scaling while improving presentation;
- the current title composition is improved over earlier builds but still requires another visual pass after lifecycle behavior is corrected.

---

## Persistence and consistency findings

Observed:

- save is blocked by a stale checkout state even when no customer is meaningfully serviceable;
- load can restore the store as open while New Business begins closed;
- checkout fixture placement persists on load, which is expected for a save, but the visible operating state/startup expectations are currently confusing.

Required outcome:

- save blockers must be real, visible, and recoverable;
- loading must restore the exact saved operating state consistently;
- New Business must initialize from its defined startup state, not accidentally inherit live/session state;
- opening a new business should not require stock already on a shelf; lack of sellable stock may affect customers/revenue, but it is no longer a hard opening prerequisite unless later explicitly reapproved.

---

## Deferred or unresolved decisions

Do not let implementation silently decide these:

- exact difficulty names/modifiers;
- exact logo catalog or final logo asset pipeline;
- full procedural-city generation versus authored-city variation under FD-005;
- final minimap presentation and world-map relationship;
- final title/menu art treatment;
- exact late-game time/calendar presentation;
- final customer animation/hand-carry presentation beyond the functional requirement that checkout merchandise be readable.

---

## Next implementation gate

The next implementation wave should be accepted only when automated tests and a new owner playtest demonstrate that a normal player can complete the full first-day/session loop without knowing backend terminology or hidden state.

No second business type or new later-game system should displace this remediation gate.