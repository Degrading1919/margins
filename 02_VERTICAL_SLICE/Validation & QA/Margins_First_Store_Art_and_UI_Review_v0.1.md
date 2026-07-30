# Margins First-Store Art and UI Review v0.1

## Record status

- **Status:** Directional presentation review complete; no production-art or licensing approval granted
- **Reviewed repository state:** Draft stacked branch `agent/validate-first-store-playable-loop`
- **Reviewed head:** `dc2395c1220a272aa283120a7adc7f4610f6343f`
- **Related implementation evidence:** `02_VERTICAL_SLICE/Validation & QA/Margins_First_Store_Local_Unity_Evidence_Record_v0.1.md`
- **Primary lens:** Art and Presentation Director Assistant
- **Secondary lens:** UX and Player-Experience Designer Assistant
- **Manual-validation boundary:** The reviewed current-build head still requires the owner playable-loop pass. Visual and usability findings derived from source, serialized scene construction, and prior owner observations are not a substitute for observed current-build playtesting.

## Review purpose

Evaluate the current playable scene, HUD, management screens, interaction presentation, and implemented 3D assets against the approved Stylized Contemporary Americana direction and the solo-production constraints.

This document records findings and proposed direction. It does not silently convert recommendations into approved project canon.

## Executive finding

The current branch is a successful **directional prototype** and an unsuitable **production-art baseline**.

It now demonstrates a recognizable temporary store identity, authored palette, functional zoning, state-reactive world presentation, simple animation, audio feedback, and a complete first-store-to-management visual bridge. However, most visible assets remain Unity primitives, most interface surfaces remain legacy `OnGUI`, and character representation remains validation-grade.

The correct next step is not broad polish. It is one bounded identity slice that replaces the smallest set of world and interface elements needed to establish a repeatable production standard.

## Approved direction used for review

- Stylized Contemporary Americana
- Road 96 reference responsibility for roadside and contemporary-Americana character
- Schedule I reference responsibility for practical model and animation complexity
- Firewatch reference responsibility for lighting, atmosphere, color, silhouette, and composition
- simplified readable 3D forms;
- believable functional proportions;
- authored signage and district identity;
- grounded tone with light humor;
- interaction readability;
- consistency over raw fidelity;
- controlled hybrid asset pipeline with provenance and human acceptance;
- rejection of photorealism and generic asset-pack low-poly presentation.

## Current implementation strengths

### Temporary identity

`Mile 7 Market` is a useful prototype brand. It is regionally suggestive without relying on a real chain and supports the approved roadside-commercial direction.

### Palette and lighting

The implemented cream, charcoal, teal, orange, warm laminate, cool dusk exterior, and warm interior practicals form a coherent initial palette. The warm/cool contrast provides stronger identity than the earlier validation materials.

### Functional zoning

The scene visually separates:

- storefront threshold;
- checkout;
- primary shelf area;
- receiving and backroom;
- cleaning task;
- parking and exterior context.

The layout supports the intended first-person loop and communicates store function before final art is present.

### World-state presentation

The branch contains useful presentation patterns that should survive:

- delivery lid movement;
- visible delivery contents;
- checkout display changes;
- checkout product props;
- spill reduction;
- storefront state changes;
- practical-light changes;
- product and placement feedback;
- functional audio cues;
- objective-linked world targets.

These are better foundations than adding more persistent HUD panels.

### Interaction-target correction

The current source configures a collider across each physical shelf fixture and routes stocking through the shelf as the semantic target. This is aligned with the owner's objection to tiny shelf placement targets.

Checkout product props also receive explicit interaction targets, which moves the implementation toward item-based scanning rather than requiring the player to stare only at the register.

These corrections still require owner validation in the current build.

## Temporary scaffolding that should not become production art

### Primitive-built environment and props

The current storefront, shelves, checkout, products, cars, distant buildings, mop equipment, and signage are substantially assembled from Unity primitive shapes. They prove scale, palette, and hierarchy but do not provide:

- coherent edge treatment;
- construction detail;
- authored UVs;
- material breakup;
- wear states;
- believable joints and supports;
- consistent silhouette refinement;
- efficient final mesh organization.

They should be treated as replaceable presentation scaffolding around reusable gameplay systems.

### Capsule employees

Employees remain capsule forms with floating `TextMesh` labels. This is sufficient for simulation validation and not a candidate character style.

### Floating world text

Debug-like world text and floating role labels undermine environmental credibility. Functional signage should migrate toward a coherent decal, texture, mesh-sign, or runtime sign system.

### Legacy interface technology

The HUD, pause menu, title menu, settings, and company desk use legacy `OnGUI`. This is acceptable for proving information and interaction but should not define the production UI architecture or final visual standard.

### Procedural prototype audio

Generated clips are useful for proving event timing and multimodal feedback. They are not an approved final sound identity.

## UX findings

### First-person visual clutter

The current presentation can display several of the following simultaneously:

- store badge;
- live financial ledger;
- shift checklist;
- objective panel;
- crosshair;
- held-item panel;
- feedback panel;
- introductory controls;
- full-screen result panel.

This quantity competes with the physical work and makes the game feel dashboard-led rather than world-led.

**Recommendation:** Retain only a small reticle, contextual action, brief feedback, and optional compact cash during ordinary first-person operation.

### Redundant guidance

The same objective may be communicated through a checklist, large bottom objective card, crosshair state, floating objective beacon, world text, and interaction prompt.

**Recommendation:** One contextual prompt should own immediate action. One optional current objective should own guided progression. Environmental affordances should carry the remaining guidance.

### Stocking target

Prior owner playtesting found that shelf placement required aiming at an overly specific safety area.

The current branch's whole-fixture collider is the correct direction. The product should be stocked by targeting the physical shelf body, with the system selecting and previewing a compatible snap destination. Exact snap points should be results of assisted placement, not required aim targets.

### Checkout target

Prior owner playtesting found that looking at the register rather than the physical item weakened the scanning interaction.

The register should communicate subtotal, line state, and completion. The physical product should remain the meaningful scan target or presented object. The current explicit checkout-product targets should be validated against that intent.

### Delivery behavior

A delivery box should not appear to unload itself. Opening a box should reveal contents. Taking one product should transfer one visible physical unit into the player's held state. Other contents should remain visibly and logically in the box.

### Artificial staging

Setup, clock-in, open, closing, result acknowledgment, and exterior controls currently risk feeling like a sequence of validation gates rather than natural business operation.

Opening and closing should exist when they express a meaningful business-state decision. They should not require arbitrary travel, repeated confirmation, or detached exterior chores merely to complete a scripted shift.

Cleaning should remain a credible store task, but it should arise in a believable location and not function as an obligatory ritual in every short proof.

### End-of-shift interruption

The current first-shift result uses a full-screen darkened modal. The owner has stated that a mandatory end-of-day cash-flow pop-up is not wanted.

**Recommendation:** Post the report in the background, show one concise notification, and leave review optional through the company desk or a report surface.

### Financial trust

The owner has questioned whether the displayed end-of-day total was mathematically correct.

The UI should not add more financial prominence until sales, COGS, operating expenses, contribution, and cash reconciliation are validated against a manually understandable example. Financial labels should distinguish revenue, cost, contribution, operating profit, and cash rather than treating them as interchangeable.

## Management-screen findings

The current company desk proves the required categories and actions, including:

- overview;
- people;
- locations;
- reports;
- hiring;
- training;
- promotion;
- task focus;
- reassignment;
- location comparison;
- pricing and reorder policy;
- delegated-day advancement.

The information architecture is directionally useful, but presentation remains dense and default-looking. Many rows mix identity, statistics, explanation, and several actions without a strong hierarchy.

The production direction should:

1. preserve stable navigation across company growth;
2. lead with current state and actionable alerts;
3. explain causes before exposing deep data;
4. separate summary cards, policy controls, employee records, and detailed reports;
5. share typography, color, spacing, focus states, and button treatment with the HUD and pause menu.

## Visual-consistency findings

### Positive

- Store branding, product colors, fixture accents, and HUD accents use related colors.
- The physical scene now has more authored identity than a generic graybox.
- Functional silhouettes are readable.
- The store visibly changes from setup to operation.

### Inconsistent

- The authored dark teal and orange HUD does not fully carry into the default-looking pause and management interfaces.
- Primitive world assets, floating text, and polished-looking HUD panels sit at different fidelity levels.
- Capsule characters are substantially below the environment's intended identity.
- Large emissive objective markers and debug-like signage compete with grounded commercial presentation.

## Production risks

| Risk | Impact | Direction |
|---|---|---|
| Broad asset-pack adoption | Generic presentation and inconsistent provenance | Purchase only to remove specific systemic bottlenecks |
| One-off product creation | High labor and weak reuse | Build product-family meshes, labels, and atlases |
| Full custom character pipeline | Excessive solo-production burden | License a sound shared rig or base, then customize |
| Uncontrolled AI generation | Legal uncertainty and style drift | Use quarantine, provenance ledger, and human acceptance |
| Legacy `OnGUI` expansion | Rework and inaccessible production UI | Stop broad UI feature expansion before a framework prototype |
| Dynamic-light proliferation | Performance and maintenance burden | Use restrained mixed or baked lighting with selective dynamic cues |
| Permanent tutorial overlays | Clutter and reduced immersion | Progressive disclosure and optional guidance |
| Finalizing the current graybox | Polished prototype geometry that still reads generic | Replace only identity-critical modules through the identity slice |

## Recommended smallest prototype

Create the `Margins Mile 7 Identity Slice` described in:

`02_VERTICAL_SLICE/Design Docs/Margins_Mile_7_Identity_Slice_Brief_v0.1.md`

It should improve only:

- facade and sign;
- one shelf family;
- checkout;
- delivery box;
- two products;
- cleaning prop;
- one modular employee;
- lighting;
- one contextual prompt;
- one compact company overview.

## Manual review tasks for the current build

Before implementation conclusions are locked, the owner should verify:

1. A held product can be stocked by targeting the physical shelf generally rather than a tiny hidden region.
2. The chosen snap destination is readable before confirmation.
3. Delivery opening reveals contents without automatically transferring units.
4. Taking one product moves exactly one visible unit.
5. Checkout requires meaningful interaction with the physical item.
6. Register feedback is visible without becoming the sole interaction target.
7. HUD elements can be understood and then mentally ignored during ordinary work.
8. Opening, closing, and cleaning feel like business actions rather than arbitrary gates.
9. Closing does not need to interrupt movement with a full-screen report in the eventual direction.
10. A manually calculated transaction matches displayed sales, COGS, expenses, contribution, and company cash.

## Disposition

- **Keep:** gameplay systems, functional scale, Mile 7 as a prototype identity, initial palette, warm/cool lighting idea, state-reactive world feedback, semantic whole-shelf targeting direction, item-target checkout direction.
- **Replace or prototype before production:** primitive hero assets, capsule employees, floating labels, legacy UI presentation, objective beacon, full-screen shift result, procedural final-audio assumption.
- **Do not expand yet:** broad asset library, full customer crowd, second-location art, character creator, driving art, district kit, dozens of products, marketing polish.

## Unresolved owner decisions

- Permanent status of Mile 7 Market.
- Character proportion and facial target.
- Accepted HUD density and optional cash display.
- Final handling of shift reports.
- UI framework and typography.
- Paid character or animation foundation.
- Dusk as a showcase condition versus general identity.
- Asset budget allocation and licensing risk acceptance.
