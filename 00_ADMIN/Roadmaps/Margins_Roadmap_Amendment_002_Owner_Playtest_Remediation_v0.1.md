# Margins Roadmap Amendment 002 — Owner Playtest Remediation v0.1

## Status and authority

- **Status:** Approved current execution amendment.
- **Approved by:** Project owner.
- **Approval date:** August 23, 2026.
- **Scope:** Near-term vertical-slice execution order after the first broad owner playtest following PR #40.
- **Authority:** This amendment changes execution priority and milestone interpretation only. It does not override foundational decisions in `00_ADMIN/Decisions`.
- **Evidence:** `02_VERTICAL_SLICE/Validation & QA/Margins_Owner_Playtest_Findings_2026-08-23_v0.1.md`.

## Why this amendment exists

PRs through #40 established a strong technical foundation across first-store operation, customers, employees, procurement, merchandising, procedural commercial generation, multi-location portfolio state, detailed/aggregate reconciliation, generated-store operation, persistence, reporting, alerts, and the Owner Phone.

The August 23 owner playtest showed that automated correctness has outpaced player-facing coherence. Several systems are technically functional but still present as prototype/admin interactions, and the complete session loop is not yet intuitive enough for vertical-slice acceptance.

The critical path therefore shifts temporarily from adding systems to **making the existing systems play as one understandable game**.

---

## Stage interpretation after the playtest

### Stage 4 — First-store hands-on loop

Remain **implementation-complete but acceptance-open**.

The next acceptance pass must specifically resolve:

- movement/camera comfort;
- interaction targeting and prompt clarity;
- delivery/mop object lifecycle;
- checkout mode and stale checkout state;
- reliable close-store flow;
- Build Mode readability;
- settings/input presentation;
- opening-state requirements and startup consistency.

### Stage 5 — Store simulation, customers, employees, and economy

Backend scope remains substantially implemented. Acceptance remains open because customer checkout presentation, player recovery from blockers, and day-to-day causal readability still need owner validation.

### Stage 6 — Delegation, generated locations, and reporting

The core backend, generated detailed-store operation, Owner Phone, safe location transitions, overnight progression, reporting, and persistence are implemented through merged PR #40.

Stage 6 is **functionally implemented but not player-experience accepted** until the first-day → management → end-day → return/load flows survive owner testing without hidden-state knowledge.

### Stage 7 — Content and presentation integration

Production-asset work may continue in parallel, but it must not displace the remediation wave. Graybox presentation is acceptable where needed to test interaction clarity; broad art replacement is not a substitute for fixing the loop.

---

## Immediate execution wave

### Wave A — Session and first-day continuity

Goal: make one complete play session understandable from launch through reload.

Required outcomes:

1. Title presentation is separate from the active gameplay world.
2. New Business enters a setup flow rather than immediately dropping into the existing scene.
3. Setup supports business name, colors, logo-selection placeholder/data hook, seed field, and difficulty selection without inventing unresolved difficulty modifiers.
4. New Business initializes a clean startup state.
5. Loading restores the saved operating state consistently.
6. Return to Title cleanly exits live gameplay presentation.
7. Save/load blockers correspond to visible, resolvable player state.

### Wave B — Checkout, closure, and next day

Goal: make the core convenience-store service loop tactile and self-explanatory.

Required outcomes:

1. Customer merchandise is visibly presented at checkout.
2. Interacting with the checkout enters a dedicated checkout mode.
3. Normal movement/unrelated world interactions are constrained while serving a customer.
4. Scanning, payment, correction/cancel, and exit are obvious.
5. Checkout completion/cancellation always clears active transient state.
6. Store closing cannot be blocked by an unreachable stale checkout.
7. Closing naturally leads to the existing End Day/report flow.
8. End Day advances exactly once through the existing authoritative progression/reconciliation path.

### Wave C — Interaction and HUD clarity

Goal: remove prototype friction from normal movement and interaction.

Required outcomes:

- tune walking speed and responsiveness;
- remove positional walking/sprinting camera bob;
- replace persistent crosshair/target-box presentation with restrained contextual highlighting;
- shorten normal interaction prompts to key + action;
- make held-object actions available from held state rather than arbitrary look targets;
- remove stock-item rotation guidance;
- make Build Mode unmistakable while active;
- add a restrained persistent HUD showing current cash and a small number of global actions such as Owner Phone access;
- improve settings checkboxes, sensitivity control, scrollbar styling, and the inappropriate `Attack` action label while preserving rebinding/accessibility foundations.

### Wave D — Tactile cleanup

Goal: remove obvious dead-end physical interactions.

Required outcomes:

- empty delivery boxes have an intentional completion/disposal outcome;
- delivery opening is discoverable while holding the box;
- the receiving-station artifact is removed or given a real purpose;
- mop/bucket handling becomes one coherent tool lifecycle;
- customer basket presentation is removed in favor of readable carried/checkout merchandise where practical.

---

## Explicit boundary: procedural city direction

The owner playtest requested seed-driven city generation at New Business. Current FD-005 instead approves a recognizable authored city built from reusable modules and states that runtime procedural generation of the full city is not current direction.

This amendment **does not silently overturn FD-005**.

Codex may implement the New Business setup and persist a seed only where it has a bounded valid purpose. It must not convert the full city to procedural generation or redefine the city model without a separate owner-approved decision that reopens FD-005.

---

## Verification gate

The remediation wave is not complete merely because individual buttons or states pass tests.

Before the next broad content/system expansion, require:

- full EditMode and PlayMode suites green;
- Windows x64 build success;
- focused automated coverage for new session/checkout/close/end-day state transitions;
- no duplicated inventory, transaction, time, location, or persistence authority;
- a fresh owner playtest completing:

**Launch → New Business → setup → first day → checkout → close → End Day → save → title → load → continue.**

The owner playtest is the acceptance evidence for human usability; automated tests remain the acceptance evidence for deterministic state correctness.

## Schedule effect

No new long-range reforecast is approved from this playtest alone. This remediation belongs inside the already forecast late-August/September vertical-slice closeout window. Reforecast again only if remediation reveals a deeper architectural problem or materially changes measured human bottlenecks.