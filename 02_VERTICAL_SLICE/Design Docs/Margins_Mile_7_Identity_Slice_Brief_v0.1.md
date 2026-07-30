# Margins Mile 7 Identity Slice Brief v0.1

## Status and authority

- **Status:** Proposed visual and UI prototype direction for project-owner review
- **Scope:** The smallest presentation pass intended to establish a recognizable Margins identity without broad production-art expansion
- **Implementation basis reviewed:** Draft stacked branch `agent/validate-first-store-playable-loop`, head `dc2395c1220a272aa283120a7adc7f4610f6343f`
- **Authority boundary:** This brief applies the approved Stylized Contemporary Americana foundation but does not approve Mile 7 as permanent canon, individual assets, licenses, paid purchases, final UI technology, or production art budgets.

## Purpose

The current first-store scene proves gameplay and presentation direction, but much of its visible construction remains prototype scaffolding. The identity slice should replace only the smallest set of assets and interface elements needed to answer four questions:

1. Can one screenshot be recognized as Margins rather than a generic store simulator?
2. Can the physical store and company-management interface look like parts of the same game?
3. Can tactile interaction remain understandable with substantially less HUD occupation?
4. Can the chosen asset approach be repeated by a solo developer using extensive AI assistance?

This is not a general polish pass. It is a bounded identity and production-pipeline experiment.

## Direction by status

### Approved foundation applied

- Stylized Contemporary Americana
- Road 96 as the primary roadside and contemporary-Americana reference responsibility
- Schedule I as the practical model and animation-complexity reference responsibility
- Firewatch as the lighting, atmosphere, silhouette, color, and composition reference responsibility
- Simplified readable forms with believable functional proportions
- Grounded tone with light humor
- Tactile but assisted stocking and scanning
- Consistency over raw fidelity
- Controlled hybrid asset sourcing with provenance and human acceptance

### Owner-stated interaction and presentation direction

- Whole physical fixtures should be easy to target; the player should not need to aim at a tiny hidden shelf zone.
- Checkout should retain an item-based scanning rhythm; the physical item, not an arbitrary register surface, should be the meaningful target.
- Delivery boxes should not appear to unload themselves.
- Setup, opening, closing, cleaning, and exterior interactions should not become artificial stages merely to lengthen a shift.
- Daily operation should feel seamless.
- A mandatory full-screen end-of-day financial summary is not wanted.
- Financial calculations and displayed totals require validation before being treated as trusted feedback.

### Current working implementation

- The first store uses the temporary identity `Mile 7 Market`.
- A cream, charcoal, teal, orange, and warm-laminate palette is paired with cool dusk exterior light and warm interior practicals.
- The scene includes a storefront, shelves, checkout, receiving area, product silhouettes, cleaning station, simple parked cars, objective guidance, functional audio, and state-reactive presentation.
- Most geometry is assembled from Unity primitives.
- Gameplay HUD, pause UI, and management UI are implemented with legacy `OnGUI`.
- Employees remain capsule placeholders with floating labels.

### Recommendations in this brief

The recommendations below remain proposed until separately accepted by the project owner.

## Visual objective

The identity slice should communicate:

> A grounded, authored American commercial world seen through the progression of a hands-on owner becoming a portfolio operator.

Two presentation layers should share one family:

1. **Physical business layer:** storefront paint, laminate, metal fixtures, product packaging, window vinyl, delivery labels, uniforms, invoices, price strips, construction marks, vehicles, and district architecture.
2. **Ownership layer:** clean digital controls for employees, locations, cash flow, policies, reports, properties, and expansion.

The management layer should feel like a credible small-business operating platform growing into a holding-company system. It should not look like a sci-fi dashboard, a generic mobile analytics template, or a decorative spreadsheet.

## Identity-slice scope

Only the following ten areas receive a deliberate identity pass:

1. Storefront facade, entry, windows, and primary sign.
2. One modular shelf family.
3. Checkout counter, scanner, and register display.
4. Delivery box and receiving presentation.
5. Cola and chips product packaging as the first product-family proof.
6. Mop bucket, spill, and cleaning-state presentation.
7. One modular employee character capable of representing cashier, stock clerk, and manager variants.
8. Interior and exterior lighting calibration.
9. One production-quality contextual interaction prompt.
10. One compact company-overview screen.

Existing domain logic, interaction rules, persistence, fixture placement, inventory, checkout state, employee simulation, and portfolio simulation should be reused rather than rebuilt for this experiment.

## UI and HUD model

### First-person HUD

The default always-visible layer should be limited to:

- a small center reticle;
- the current context action while a valid target is focused;
- brief state-change or error feedback;
- an optional compact cash display only when it proves useful in playtesting.

Do not retain a permanent combination of store badge, live ledger, shift checklist, objective card, held-item card, feedback card, and control legend.

### Interaction prompt

Use a compact two-level structure near the center of attention:

`E  Stock Mile 7 Cola`

`Shelf full · 4/4`

The first line states the action. The second states capacity, consequence, or blocker only when needed.

The world should carry as much feedback as practical through motion, lighting, object state, sound, and placement. Text should explain ambiguity rather than repeat obvious success.

### Guidance

- Use one current objective at a time during guided startup.
- Guidance becomes optional after the player understands the first operating loop.
- Do not make a permanent checklist the primary way to understand the store.
- Replace the floating `NEXT` beacon with environmental guidance and optional escalating help.
- Stronger world emphasis may appear after repeated failures or a direct help request.

### Shift reporting

Closing should post a report without blocking movement:

- storefront state changes;
- store lighting settles;
- a concise notification confirms the shift posted;
- the company desk or report area receives an unread indicator;
- the player may inspect the result immediately or continue.

Reserve blocking milestone presentations for genuinely major progression events.

## Management-screen model

Use one durable navigation structure that can grow with the game:

- Overview
- Locations
- People
- Inventory
- Finance
- Properties
- Expansion

Early progression may hide or clearly lock unavailable sections.

Each page should answer:

1. What is the current state?
2. What needs attention?
3. Why did it happen?
4. What can the player change?
5. Where is deeper detail available?

The identity slice only needs a compact Overview page showing:

- company and first-location identity;
- cash and current day;
- one location summary;
- staffing status;
- one actionable alert;
- recent sales, contribution, and stock availability;
- a clear route to deeper screens that may remain unavailable in this prototype.

## Proposed visual vocabulary

- warm cream instead of pure white;
- charcoal instead of absolute black;
- faded commercial colors for world surfaces;
- one controlled safety-orange action accent;
- restrained petrol green or turquoise for accepted states;
- tabular or monospaced numerals for financial values;
- condensed commercial-signage typography for major headings;
- readable humanist sans-serif typography for instructions and management text;
- restrained receipt lines, price strips, window vinyl, route diagrams, and ledger dividers as supporting motifs.

These motifs should add identity without turning the interface into literal paper forms or decorative cash-register screens.

## 3D complexity and modularity targets

### Characters

Prototype one shared humanoid rig with:

- grounded, near-human proportions;
- simplified facial planes, hands, hair, clothing folds, and materials;
- two heads;
- two hairstyles;
- shirt, pants, shoes, apron or vest, and name badge;
- walk, idle, box carry, item carry, shelf stocking, register idle, and cleaning animations.

The prototype succeeds when three persistent employees can look distinct through modular combinations without requiring three separately authored character pipelines.

### Environments and fixtures

- Build the storefront shell and fixture family as custom modular assets.
- Use believable construction logic and functional scale.
- Reuse trim, materials, sign sockets, shelf spacing, feet, handles, and edge treatment.
- Support future wear, renovation, and prestige states without unique replacement meshes for every condition.

### Products

Create a shared product-family system rather than isolated hero packages:

- beverage can or bottle;
- snack bag;
- candy bar;
- small carton or box;
- grocery container;
- household bottle or pack.

The identity slice implements only cola and chips, but their mesh, label, and material organization should prove the reusable system.

### Vehicles and secondary props

Vehicles, vegetation, street furniture, and low-priority incidental props should be sourced or purchased and then normalized. They should not define the identity slice or trigger a driving-system commitment.

## Asset-source strategy

| Asset class | Recommended source strategy |
|---|---|
| Storefront, fixture family, checkout, delivery box | Custom-made because they define mechanics, proportions, and identity |
| Product families and fictional brands | Custom-made with AI-assisted concepts, variations, and production tooling |
| UI components, typography system, icons | Custom component family; licensed typefaces only after review |
| Humanoid base and general animation | Properly licensed foundation, then substantially modified and normalized |
| Employee clothing, badges, and role variants | Custom modular additions on the shared rig |
| Vehicles, vegetation, street furniture | Properly licensed or open-source foundations, then restyled |
| General locomotion and non-critical animation | Licensed or open animation sources, retargeted and cleaned |
| Critical stocking, scanning, carrying, and cleaning motion | Custom-authored or heavily edited where generic motion fails readability |

Do not purchase a large retail or low-poly environment megapack merely to fill the scene. A purchase should remove a systemic production bottleneck, not define the game's visual identity by accident.

## Solo-developer pipeline experiment

1. Define the target with a compact style sheet before sourcing.
2. Place every external or generated asset in an intake quarantine.
3. Record provenance, license, attribution, AI involvement, and restrictions.
4. Normalize scale, pivots, naming, UVs, material slots, sockets, colliders, and LODs outside Unity where practical.
5. Import through class-specific presets.
6. Review every candidate under actual store lighting and interaction distance.
7. Accept, revise, or quarantine after visual, legal, technical, and gameplay review.
8. Preserve source files and modification notes for accepted assets.

## Prototype play sequence

The identity slice should support a five-minute demonstration:

1. Enter the mostly empty Mile 7 store.
2. Open a delivery box and physically take one visible product.
3. Target the whole compatible shelf and stock the product through assisted snapping.
4. Present and scan one physical checkout item.
5. See one employee working in the store.
6. Observe the store in an operating state.
7. Open the compact company overview without ending the physical session.
8. Close or leave the shift without a mandatory full-screen results modal.

## Acceptance criteria

The identity slice passes when:

- one ordinary gameplay screenshot is recognizably Margins rather than generic low-poly retail;
- the store and company overview share typography, spacing, palette, and interaction language;
- the player understands target, action, success, and blocker without excessive text;
- the empty-to-operating transformation is immediately visible;
- character, shelf, checkout, products, facade, and interface share one intentional complexity level;
- the whole shelf is a practical stocking target;
- checkout reads as item-based scanning;
- delivery contents remain physically represented;
- the shift report is available without interrupting movement;
- asset provenance is complete for every non-original or AI-assisted accepted asset;
- the work is reusable production foundation rather than a marketing-only diorama.

## Explicit exclusions

- second store art pass;
- full customer crowd;
- full character creator;
- dozens of product packages;
- drivable vehicles;
- final district kit;
- complete property-development visuals;
- full audio identity;
- final title screen or marketing art;
- large permanent first-person dashboard;
- mandatory daily results modal;
- broad replacement of working gameplay systems;
- production approval of any asset or license solely through this brief.

## Unresolved owner decisions

- Whether `Mile 7 Market` becomes permanent first-store identity or remains a prototype label.
- Whether near-human simplified proportions are the accepted character target.
- Whether dusk is only the showcase condition or a broader identity anchor.
- Whether compact cash remains visible during operation.
- Final typography, iconography, and input-prompt presentation.
- Which character and animation foundation may be licensed.
- Initial paid-asset budget allocation.
- Final technical budgets and UI implementation framework.

## Required follow-up

After owner review, approved choices should be synchronized into the canonical art and presentation direction. The implementation prototype should then be planned as a bounded visual, UI, and asset-pipeline task rather than an unrestricted polish pass.
