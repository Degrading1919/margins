# Margins Initial Scope Boundaries

## Status and authority

- **Status:** Current vertical-slice scope synchronized to `00_ADMIN/Decisions/Margins_Foundational_Decisions_v1.0.md`
- **Authority:** This document applies the approved foundational decisions to the first playable milestone. It cannot approve exceptions or expand the milestone on its own.

## Purpose

Protect the first playable milestone from expanding into the complete long-term vision before the core owner-operator-to-portfolio loop is validated.

## Vertical-slice objective

Prove that a standalone convenience store can support a satisfying progression from guided startup and hands-on work through staffing, delegation, a second location, off-site simulation, and portfolio reporting.

The vertical slice should become durable commercial-game foundation rather than disposable prototype code or content.

## Vertical-slice commitments

- One compact, primarily walkable commercial city block
- One fully operational **standalone convenience-store** business category
- A guided startup in a small, mostly empty leased storefront with limited startup capital
- At least two commercial locations with meaningfully different market conditions
- First-person receiving, stocking, checkout or service, cleaning, and basic maintenance
- Tactile-but-assisted product handling, shelf snapping, and item-based scanning
- Data-driven products and inventory
- Basic pricing, customer demand, and satisfaction
- Employee hiring, scheduling, and task assignment
- At least two worker roles and one manager role
- Delegated remote management with meaningful physical intervention
- Detailed simulation while the player is present
- Aggregate off-site business and customer simulation while absent
- Location-level and portfolio-level financial reporting
- Interior furniture and equipment placement on a grid
- Saving and loading layouts and business state
- Understandable local competition represented primarily through aggregate market effects

## Vertical-slice constraints

- PC only
- Less than **$1,000** in total pre-revenue development spending across the project unless the project owner approves a change
- Approximately **20–30 direct human development hours per week**, supplemented by agentic AI workflows
- One fictional contemporary American city block; no full-city procedural generation
- Primarily on-foot traversal; supplier deliveries or simplified logistics
- No engine-specific implementation assumptions until the engine-selection process is approved
- Off-screen businesses use aggregate simulation; detailed NPCs and interactions are instantiated only where the player is present or a specific event requires them
- Structured, validated data should support scalable content; permanent runtime invariants remain code-enforced

## Explicitly outside the vertical slice

The following are not part of the first playable commitment:

- Fuel pumps or a combined gas-station convenience store
- Multiple complete business categories
- Selection or implementation of the second business category
- Ground-up commercial building construction
- Player-built multi-story structures
- Mixed-use development
- Detailed corporate departments
- Advanced franchising agreements
- Drivable vehicles or full traffic simulation
- Deep autonomous competitor-company AI
- Mergers and acquisitions
- Private-equity or strategic-investor systems
- Public markets, IPOs, or stock trading
- Full economic cycles
- Public mod tools or Workshop integration
- Multiplayer
- Every city building being enterable
- Fully persistent large crowds or a full persistent-resident simulation
- Mandatory hunger, thirst, hygiene, bathroom, dating, family, or full social-life systems
- Detailed tax filing, bookkeeping, or legal paperwork
- Full property-development endgame
- Complete 1.0 content scope

## Deferred long-term direction

The following are approved directions or possibilities but require later milestones, prototypes, or scope gates:

- Property purchase, renovation, subdivision, vacant-land acquisition, and modular commercial construction
- At least two complete business categories for 1.0
- Holding-company and headquarters progression
- Layered competitor expansion and long-term Coffee Inc–style mergers and acquisitions
- Commercial lending, mortgages, private investors, and acquisition financing
- Map-based or transitional district travel, with driving adopted only if later prototypes justify it
- Public mod support only after formats, persistence, and content tools stabilize
- A public demo or controlled playtest followed by possible quality-gated Early Access

Deferral does not assign a roadmap stage or guarantee implementation beyond explicit 1.0 commitments.

## Scope-change rule

Any proposal that adds a new dependency, simulation domain, content class, technical requirement, or recurring production burden to the vertical slice must be reviewed against:

1. the approved foundational decision record;
2. the smallest proof needed for the owner-operator-to-portfolio loop;
3. shared-system reuse;
4. the budget and solo-development constraints;
5. detailed-versus-aggregate simulation implications;
6. persistence, reporting, and validation requirements; and
7. what existing work the proposal would displace.

Use `$margins-vertical-slice-scope-gate` for formal disposition when the classification is disputed.

## Unresolved vertical-slice details

The following remain to be designed or validated:

- exact store size and block layout;
- exact product catalog and equipment set;
- worker-role definitions and manager capabilities;
- customer archetypes and recurring-customer count;
- prices, wages, demand formulas, difficulty values, and failure thresholds;
- exact remote-management controls and reports;
- local competitor count and behavior;
- save architecture and technical performance budgets;
- onboarding sequence and acceptance criteria;
- art-production budgets and asset list;
- accessibility scope; and
- engine and implementation architecture.