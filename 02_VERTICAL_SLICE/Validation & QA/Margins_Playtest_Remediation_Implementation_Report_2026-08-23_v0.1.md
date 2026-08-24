# Margins Playtest Remediation Implementation Report — 2026-08-23 v0.1

## Status and authority

- **Status:** Implemented and automated-verified; owner playtest acceptance remains open.
- **Implementation base:** Repository `main` at `606723e`.
- **Scope authority:** `Margins_Roadmap_Amendment_002_Owner_Playtest_Remediation_v0.1.md` and `Margins_Owner_Playtest_Findings_2026-08-23_v0.1.md`.
- **Decision boundary:** This implementation does not amend foundational decisions. In particular, it does not add full procedural-city generation or difficulty modifiers.

## Implemented outcome

### Session and first-day continuity

- The title and New Business setup are explicit application states with an opaque front end, disabled gameplay input, and no exposed frozen-world presentation.
- Return to Title ends the active-session guard. New Business and Load therefore act directly from the title instead of treating the hidden world as a resumable paused session.
- New Business now opens setup for business name, two brand colors, a placeholder logo-selection data hook, startup seed, and an FD-006 economic-purpose selection.
- Difficulty purpose is stored only as metadata. No difficulty name or gameplay modifier was invented.
- The seed is persisted and used only for bounded deterministic customer product-request ordering. It does not generate or vary the city.
- Starting applies setup to the existing clean initialization snapshot, including the company, convenience brand, first location, and first-store sign presentation. It does not retain live session state.
- The portfolio snapshot contract is version `5`. Version `4` saves migrate with the default startup profile; existing versions `1` through `3` retain their established deterministic migration path.
- Save/load preserves the startup profile and exact saved operating state. Returning to title and loading the saved business restores that state through the existing atomic persistence path.
- Opening no longer requires merchandise already shelved; missing sellable stock remains an economic/customer outcome instead of an opening blocker.

### Checkout, closure, and day transition

- Customers use their reserved physical product units in hand and visibly move those exact units to checkout item points; the generic basket presentation is removed.
- Interacting with checkout enters a dedicated checkout mode. Movement, look, jump, Owner Phone switching, Build Mode, and unrelated world targeting are constrained until checkout exits.
- The checkout panel shows item progress, subtotal, the next exact product action, payment, undo, and cancel/exit controls.
- Assisted scanning advances one exact presented physical unit at a time through the existing checkout station. Payment still commits through the existing transaction, inventory, and physical-unit authorities.
- Undo removes the most recent scan. Cancelling at zero scans abandons the checkout, returns reserved units through their existing physical locations, clears the station session, and exits the mode.
- Complete, cancel, timeout, restore, and authority-mismatch paths clear transient dedicated-mode state.
- An incomplete station session with no owning customer is repaired before save or close blocker evaluation. A customer/session mismatch is resolved through the existing customer abandonment and inventory-return path.
- Beginning closure stops service and sends all remaining customers visibly toward the exit while immediately returning their reserved units and clearing checkout/queue state. Final close waits only for those visible customers to leave.
- Closing creates no phantom sale and consumes no abandoned inventory.
- After the first closed shift, the HUD directs the player to the Owner Phone for shift review and End Day. End Day remains on the existing overnight, reconciliation, reporting, portfolio, and time authority and advances exactly once.

### Interaction, comfort, HUD, and settings

- First-person movement is tuned to a `3.2` walk speed, `5.4` sprint speed, `24` acceleration, and `30` deceleration.
- Positional camera bob and roll are removed. The existing camera-motion preference now controls only the subtle sprint field-of-view change and is labeled accordingly.
- The persistent center crosshair is removed. Focus feedback is reduced from the large corner box to a small contextual marker.
- The first-store HUD persistently exposes cash, day, and Owner Phone access while keeping objective and interaction guidance contextual.
- Stock carry guidance no longer advertises rotation. Fixture rotation remains confined to Build Mode.
- Build Mode receives an amber edge/tape treatment and explicit mode controls.
- Look Sensitivity is a discrete numeric stepper, toggle backgrounds are compact, and the controls scrollbar uses the menu visual language.
- Unused template actions including player-facing `Attack` are removed from the presented/rebindable controls list without changing the input-action authority.

## Authority reuse

No parallel inventory, transaction, customer, operating-state, portfolio, overnight, generated-location, time, or persistence state was introduced. New setup data is stored on the existing portfolio snapshot; checkout mode is transient interaction state over the existing customer, physical-unit, and checkout station authorities.

## Verification evidence

- Unity EditMode: **170 passed, 0 failed, 0 skipped**.
- Unity PlayMode: **82 passed, 0 failed, 0 skipped**.
- Workflow regressions cover title/setup state, clean customized New Business initialization, startup-profile save/load, opening without shelved stock, visible exact-item checkout, interaction lock, scan/payment/undo/cancel cleanup, stale-checkout repair, visible close drain without phantom transactions, existing End Day exact-once progression, and disk round-trip continuity.
- Windows player: Unity `StandaloneWindows64` build succeeded using the enabled project scenes. Build report size was `105,220,544` bytes; the generated executable PE machine is `0x8664` (AMD64).
- `git diff --check`: clean.

## Remaining owner-playtest items

The following findings are not silently closed by this implementation:

- A fresh owner playtest must still complete **Launch → New Business → setup → first day → checkout → close store → End Day/review → save → title → load → continue**. Automated state correctness is verified; human usability acceptance is not.
- Wave D delivery cleanup remains: discoverable sealed-box opening while carried, an intentional empty-box discard/recycle/flatten outcome, and removal or meaningful repurposing of the receiving-station artifact.
- Wave D mop/bucket cleanup remains: bucket-first carry, set-down/take-mop/use/return lifecycle, and removal of arbitrary mop placement as the completion path.
- Exact difficulty names and gameplay modifiers remain unresolved. This implementation stores only the four approved FD-006 purpose hooks.
- The exact logo catalog/final logo asset pipeline and the next title/menu art pass remain unresolved.
- Full procedural-city generation remains outside current direction under FD-005. The saved seed has no city-generation effect.
- Final minimap/world-map scope and presentation remain unresolved.
- Final customer animation and hand-carry presentation beyond readable exact merchandise remains production polish.
- Exact late-game time/calendar presentation remains unresolved.

## Next acceptance action

Run the fresh owner playtest on the Windows x64 build and record any remaining blockers against the sequence above. If the complete loop passes, schedule the bounded Wave D object-lifecycle cleanup without reopening the deferred city, difficulty, map, or final-art decisions by implication.
