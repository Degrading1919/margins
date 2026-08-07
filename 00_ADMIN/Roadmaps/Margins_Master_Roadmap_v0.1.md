# Margins Master Roadmap v0.1

## Status and authority

- **Status:** Active planning baseline; progress synchronized August 6, 2026
- **Originally prepared:** July 27, 2026
- **Last progress sync:** August 6, 2026
- **Authority:** This roadmap sequences approved direction but does not outrank `00_ADMIN/Decisions/Margins_Foundational_Decisions_v1.0.md` or approve unresolved features, technology, spending, dates, or scope changes.
- **Approval:** The project owner must approve every material rebaseline.
- **Schedule meaning:** All dates remain planning ranges, not release promises.
- **Progress meaning:** A checked item is implemented or documented on merged `main`. An unchecked item remains incomplete or only partially implemented. A completed implementation item does not automatically pass its player-experience or milestone acceptance gate.

## Progress snapshot

| Stage | Current status | Evidence summary |
|---|---|---|
| 0 — Foundation and governance | **Complete** | Foundational decisions, synchronized pre-production direction, roles, skills, and source-of-truth hierarchy are merged. |
| 1 — Technical requirements and shortlist | **Complete as amended** | Unity was selected by owner decision; the multi-engine execution plan was superseded by Amendment 001. |
| 2 — Risk prototypes and engine selection | **Complete as amended** | Unity technical baseline and bootstrap standard are approved; the original multi-engine comparison is no longer authorized work. |
| 3 — Production skeleton and pipelines | **Functionally complete** | Unity project, state boundaries, validation, tests, builds, persistence, graybox store, and evidence conventions are merged. Asset intake remains iterative. |
| 4 — First-store hands-on loop | **Core implementation complete; acceptance gate pending continued playtesting** | Movement, receiving, stocking, checkout, cleaning, fixtures, store state, save/load, prompts, and error handling are merged. |
| 5 — Store simulation, customers, employees, and economy | **In progress; major backend systems merged** | Autonomous customers, employees, manager work, demand, pricing, competition, payroll, reports, and persistence exist. Several depth and recovery items remain. |
| 6 — Delegation, off-site simulation, second location, and reporting | **In progress; major backend systems merged** | Delegated simulation, policies, two locations, portfolio reports, and persistence exist. Travel, communication, schedules, and complete detailed-return flow remain. |
| 7 — Presentation integration | **Preparation in progress** | Art direction, identity brief, asset budgets, provenance template, and Tripo prompting skill are merged; production content integration remains. |
| 8–12 | **Not started as milestone phases** | Later hardening, external validation, commercial production, 1.0, and release work remain future stages. |

## Merged progress evidence used for this sync

- PR #11 — approved Unity foundation baseline and completed amended Stages 1 and 2;
- PR #12 — implemented the Unity foundation spike with tests and a Windows build;
- PR #20 — integrated the first-store interaction loop, fixture placement, persistence, and validation evidence;
- PR #21 — documented the Mile 7 art, UI, and player-experience direction;
- PR #22 — locked approved 3D asset budgets and provenance requirements;
- PR #23 — added autonomous first-store customers using physical stock and authoritative checkout;
- PR #24 — added the repository-local Tripo 3D prompting skill;
- PR #25 — connected employees to the live store and extracted shared business-operation boundaries.

This synchronization records progress only. It does not approve a second business, final art, a new release date, or a revised full-project hour forecast.

---

## Roadmap purpose

This roadmap provides a dependency-aware path from the synchronized pre-production foundation to:

1. an engine and technical foundation selected through evidence;
2. a durable internal vertical slice proving the convenience-store owner-operator-to-portfolio loop;
3. a public demo or controlled playtest;
4. an evidence-based decision on paid Early Access or continued private production; and
5. a complete premium 1.0 release meeting the approved minimum scope.

The roadmap uses rolling-wave planning:

- near-term work is described in the most detail;
- work through the internal vertical slice is milestone-level but actionable;
- work beyond public validation remains directional until earlier evidence exists.

## Planning assumptions

The planning baseline assumes:

- PC-only development;
- less than **$1,000** in total pre-revenue development spending unless the project owner approves a change;
- approximately **20–30 direct human development hours per week**;
- extensive agentic AI assistance for research, documentation, decomposition, code, data, testing, validation, and repetitive content work;
- the project owner remains the decision-maker, integrator, playtester, and final acceptor;
- no committed outside team, contractor capacity, publisher support, or external funding.

The original 2,000–3,400-hour vertical-slice estimate and 2028 planning dates were produced before measured Unity and agent-assisted implementation velocity existed. They remain historical planning ranges until a separate owner-approved reforecast is completed.

---

## Historical timeline scenarios — reforecast required

| Outcome | Aggressive case | Planning case | Conservative case |
|---|---|---|---|
| Engine and technical baseline | October 2026 | November–December 2026 | Q1 2027 |
| First complete hands-on store loop | Q2 2027 | Q3 2027 | Q1 2028 |
| Internal vertical slice | Q4 2027–Q1 2028 | Q2–Q3 2028 | H1 2029 |
| Public demo or controlled playtest | Q2 2028 | Q4 2028–Q1 2029 | H2 2029 |
| Possible paid Early Access | Q1 2029 | H2 2029 | 2030 or later |
| Premium 1.0 | H2 2030 | 2031 | 2032–2034 |

The engine baseline and much of the first-store implementation were completed substantially earlier than these scenarios predicted. Do not silently extrapolate that acceleration to final art, balancing, debugging, content production, external testing, or release work.

---

# Stage roadmap

## Stage 0 — Foundation and governance baseline

**Status:** Complete, subject to normal maintenance

### Completed work

- [x] Approved foundational decisions
- [x] Three-gate decisions and roles audit
- [x] Synchronized project brief, pillars, scope, technical direction, content/commercial strategy, economy/progression direction, and art/audio/presentation direction
- [x] Canonical roles and activation prompts
- [x] Repository structure and role-versus-skill model
- [x] Repository-local workflow skills and skill catalog

### Gate 0

**Passed:** repository foundation is sufficiently aligned for technical and vertical-slice execution.

---

## Stage 1 — Technical requirements and candidate shortlist

**Status:** Complete as amended by `Margins_Roadmap_Amendment_001_Unity_Selection_v0.1.md`

The original multi-engine shortlist and comparison program was superseded when the project owner selected Unity.

### Required work

- [x] Define engine-evaluation criteria and weighting
- [x] Identify a bounded candidate shortlist
- [x] Define prototype acceptance tests
- [x] Define target development-machine and provisional player-hardware assumptions
- [x] Identify licensing, deployment, asset, source-control, debugging, and build constraints
- [x] Evaluate AI-agent workflow compatibility
- [x] Define minimum engine-selection evidence
- [x] Create the technical risk package
- [x] Record the owner’s Unity decision and disposition of rejected execution paths

### Gate 1

**Passed through owner decision and Amendment 001.**

---

## Stage 2 — Risk prototypes and engine selection

**Status:** Complete as amended

The original requirement for equivalent Unreal Engine, Unity, and Godot implementation prototypes is not authorized. The approved replacement was a bounded Unity baseline followed by a Unity foundation spike.

### Amended required work

- [x] Record Unity as the approved engine
- [x] Approve Unity 6000.5.5f1 and the initial package baseline
- [x] Define repository, serialization, input, navigation, testing, render-pipeline, and Windows-build conventions
- [x] Implement first-person movement and look
- [x] Implement one data-defined product
- [x] Implement pickup, rotation, shelf snapping, and placement feedback
- [x] Implement versioned placement save/load validation
- [x] Implement one placeholder navigation agent
- [x] Run focused EditMode and PlayMode tests
- [x] Produce and launch a Windows x64 build
- [x] Confirm no project-blocking Unity limitation

### Gate 2

**Passed:** Unity is the production engine and the foundation spike was merged.

---

## Stage 3 — Production skeleton, data contracts, and pipelines

**Status:** Functionally complete; asset-pipeline refinement continues

### Required work

- [x] Establish engine-specific repository structure
- [x] Define module and state-ownership boundaries
- [x] Establish coding, data, test, and naming conventions
- [x] Define product, fixture, employee, customer, location, operation, and business-state contracts
- [x] Create schema validation and error reporting
- [x] Establish save-versioning and compatibility conventions
- [x] Establish reproducible automated tests and Windows build workflow
- [ ] Complete the production asset intake, normalization, provenance, and Unity import pipeline for representative final assets
- [x] Create a graybox test store and test block
- [x] Create milestone evidence and defect-recording conventions
- [x] Extract reusable business-operation recipes, station capacity, task progress, employee performance, and aggregate simulation profiles

### Exit evidence

- [x] Clean Unity project build
- [x] Representative validated data loads correctly
- [x] Graybox store saves and reloads
- [x] Automated checks run reproducibly
- [ ] One representative production-quality asset completes the full provenance-to-runtime pipeline and owner acceptance
- [x] Architecture and decision records exist for major adopted choices

### Gate 3

**Functionally passed for vertical-slice implementation.** Final-asset pipeline acceptance remains a Stage 7 dependency rather than a blocker to continued systems work.

---

## Stage 4 — First-store hands-on operating loop

**Status:** Core implementation complete; continued playtesting and bug fixing remain

### Required work

- [x] Player movement and interaction
- [ ] Guided leasing and initial store preparation in simplified form
- [x] Receiving deliveries and handling boxes
- [x] Stocking snapped products onto valid fixtures
- [x] Product and physical inventory state
- [x] Checkout and exact-item scanning
- [x] Cleaning and basic maintenance
- [x] Grid-based fixture and equipment placement
- [x] Opening and closing the store
- [ ] Accelerated operational time and seamless overnight progression in minimum viable form
- [x] Save and reload of player, layout, inventory, customers, employees, and store state
- [x] Basic feedback, prompts, validation, and error recovery
- [x] Autonomous customers that take real shelf units, queue, pay, abandon, and leave

### Exit evidence

A player can currently:

- [x] Enter the graybox first store
- [x] Place and move essential fixtures
- [x] Receive products
- [x] Stock shelves
- [x] Open the store
- [x] Serve autonomous customers by scanning their exact physical items
- [x] Clean the store
- [x] Close the store after active customers resolve
- [x] Save, exit, reload, and continue without replaying revenue

### Gate 4

**Implementation evidence exists.** The subjective hands-on acceptance gate remains open for continued owner playtesting, interaction refinement, and bug fixing; it does not require holding completed implementation PRs open.

---

## Stage 5 — Store simulation, customers, employees, and economy

**Status:** In progress; major backend systems are merged

### Required work

- [x] Aggregate local demand model
- [x] Instantiated nearby customers representing live demand
- [ ] Complete satisfaction, reputation, pricing, product-mix, and service-quality effects across detailed and aggregate play
- [x] Understandable local competition input in aggregate simulation
- [ ] Complete revenue, inventory cost, rent, payroll, debt, and operating-expense depth
- [ ] Expand visible failure pressure and designed recovery actions
- [x] Persistent employee records
- [ ] Complete hiring, scheduling, task assignment, reliability, skill, and satisfaction depth
- [x] At least two worker roles
- [x] Employee execution of tasks the player learned physically
- [x] Basic manager role and bounded influence on detailed and aggregate work
- [x] Actionable store and portfolio reporting with identified primary causes
- [x] Scenario tests for economy, employees, inventory, checkout, abandonment, reporting, and persistence
- [x] Shared employee-performance rules used by both detailed and aggregate simulation

### Exit evidence

- [x] Outcomes can be traced to price, demand, stock, staffing, capacity, manager quality, or competition in the current aggregate model
- [x] Cashier and stock-clerk roles perform meaningful live-store tasks
- [x] Manager work affects standards and employee performance
- [x] Employee state persists correctly
- [ ] The player can recover from a defined set of operating failures through clear in-world or management actions
- [x] Reports explain a primary cause rather than only totals
- [ ] Detailed customer satisfaction and product-mix consequences are fully integrated and validated

### Gate 5

**Not yet passed.** The first location has a coherent operating backbone, but failure recovery, detailed satisfaction, product mix, scheduling, and economy depth remain incomplete.

---

## Stage 6 — Delegation, off-site simulation, second location, and portfolio reporting

**Status:** In progress; major backend systems are merged

### Required work

- [x] Manager appointment and basic authority
- [ ] Complete remote prices, schedules, budgets, policies, and purchasing controls
- [ ] Manager communication, alerts, and exceptions
- [x] Aggregate off-site business simulation
- [x] Detailed and aggregate financial reconciliation without duplicate sales or inventory
- [x] Second convenience-store location options with different market conditions
- [x] Local market differences and competition inputs
- [x] Combined location and portfolio reporting
- [ ] Travel between locations through the approved traversal boundary
- [ ] Physical intervention at either location where remote control is insufficient
- [x] Persistence across company, employees, policies, reports, and two-location state
- [x] Reusable simulation profiles that avoid convenience-store literals in shared aggregate rules

### Exit evidence

The current backend supports:

- [x] Operating the first store personally
- [x] Hiring, training, promoting, focusing, and assigning employees
- [x] Appointing a manager
- [x] Setting pricing and reorder policies
- [x] Opening a second location
- [x] Advancing delegated operating days
- [x] Applying manager quality and employee focus to aggregate results
- [x] Comparing locations through reports
- [ ] Traveling to and physically operating both locations through a coherent detailed-state transition
- [x] Saving and restoring the two-location portfolio state

### Gate 6

**Not yet passed.** The portfolio backend is substantially proven, but physical travel, detailed return at both locations, management communications, schedules, budgets, and complete intervention flow remain.

---

## Stage 7 — Vertical-slice content and presentation integration

**Status:** Preparation in progress; production integration remains

### Completed preparation

- [x] Approved Stylized Contemporary Americana direction
- [x] Mile 7 identity-slice and art/UI review documents
- [x] Approved 3D asset technical ceilings and collider rules
- [x] Asset provenance ledger template
- [x] Tripo 3D prompting skill with fact-verification requirements
- [x] Player-experience direction for targeting, physical deliveries, item scanning, seamless operation, and non-blocking reports

### Required integration work

- [ ] One compact authored commercial block
- [ ] Two visually and economically distinct convenience-store locations
- [ ] Original implementation of approved visual-reference responsibilities
- [ ] Modular store, fixture, product, prop, employee, customer, signage, and environment assets
- [ ] Fictional brands and packaging sufficient for the slice
- [ ] Lighting, color, atmosphere, silhouettes, and readability
- [ ] Minimum viable character animation and interaction feedback
- [ ] Initial ambience and functional audio integrated with production presentation
- [ ] Onboarding and guided-startup presentation
- [ ] Production UI across store operation, management, alerts, and reports
- [ ] Completed asset provenance, licensing, attribution, and AI-involvement records for integrated assets

### Gate 7

**Not yet passed:** the current build remains a systems-heavy prototype and has not reached presentation coherence.

---

## Stage 8 — Internal vertical-slice hardening and acceptance

**Status:** Not started as a milestone phase

### Required work

- [ ] Full-loop internal playthroughs
- [ ] Defect triage and regression coverage
- [ ] Save corruption, restore, transition, and migration testing at vertical-slice scale
- [ ] Detailed-versus-aggregate parity testing
- [ ] Economy and progression tuning
- [ ] Onboarding, controls, feedback, and report usability testing
- [ ] Navigation, performance, memory, and load-time profiling
- [ ] Accessibility-risk review and minimum requirements
- [ ] Scope audit against approved commitments
- [ ] Asset-provenance and licensing audit
- [ ] Known-limitations and deferred-work record

### Proposed acceptance evidence

- [ ] At least three complete internal playthroughs through stable two-location delegation
- [ ] No unresolved blocker involving save integrity, portfolio correctness, or core progression
- [ ] Major failures have reproducible cases and dispositions
- [ ] A new tester can understand the operating loop and major causes of success or failure
- [ ] The owner confirms the hands-on, delegation, and portfolio layers are enjoyable enough to continue
- [ ] Every approved vertical-slice commitment is demonstrated or returned for owner disposition

### Gate 8

**Internal Vertical Slice Accepted:** the project owner formally accepts, revises, or rejects the vertical slice based on evidence.

---

## Stage 9 — Public demo or controlled playtest

**Status:** Not started

### Required work

- [ ] Choose controlled playtest versus public demo
- [ ] Define target player profiles and questions
- [ ] Harden distribution, crash reporting, feedback capture, and privacy practices
- [ ] Improve onboarding and accessibility from observed failures
- [ ] Prepare only required storefront and marketing material
- [ ] Collect behavioral, qualitative, defect, and retention evidence
- [ ] Distinguish polish complaints from foundational design problems
- [ ] Update risks, forecasts, and commercial assumptions

### Gate 9

**Public Validation:** decide among continued private development, another test cycle, paid Early Access, redesign, or project stop.

---

## Stage 10 — Commercial production gate

**Status:** Not started

### Decision options

1. enter paid Early Access;
2. remain private and continue toward a larger release build;
3. conduct another public validation cycle;
4. reduce or restructure 1.0 scope through a new owner decision;
5. pause or stop development.

### Required evidence

- [ ] Product appeal and differentiation
- [ ] Technical stability
- [ ] Content-production throughput
- [ ] Support burden
- [ ] Budget and runway
- [ ] Forecast for the second business and property systems
- [ ] Pricing, storefront, legal, disclosure, marketing, and community requirements
- [ ] Revised schedule scenarios

### Gate 10

**Commercial Baseline Approved:** no paid product or public release commitment occurs without an approved decision record.

---

## Stage 11 — 1.0 production

**Status:** Not started

### Required approved minimum

- [ ] At least two complete business categories
- [ ] Property ownership and development
- [ ] Core holding-company progression
- [ ] Premium single-player release of coherent quality

### Directional workstreams

- [ ] Select the second business only after vertical-slice evidence
- [ ] Implement the selected business through the shared operation foundation
- [ ] Deepen convenience-retail progression where evidence supports it
- [ ] Implement property purchase, renovation, subdivision, and approved development depth
- [ ] Implement company, brand, headquarters, and portfolio progression
- [ ] Expand the city through handcrafted modular districts as required
- [ ] Deepen economy, financing, administration, recovery, competitors, and endgame only when approved
- [ ] Complete UX, accessibility, art, audio, performance, onboarding, localization, and release-quality work
- [ ] Continuously validate detailed/aggregate parity, save migration, and content contracts

### Explicitly unassigned

The following remain outside the committed roadmap until separately approved:

- drivable vehicles;
- detailed mergers and acquisitions milestone;
- public markets or IPOs;
- public mod support or Workshop integration;
- a third business category;
- multiplayer;
- full persistent city residents;
- mixed-use or unrestricted multi-story construction;
- post-1.0 expansion or sequel scope.

### Gate 11

**1.0 Scope and Content Lock:** approve final business, property, holding-company, city, content, accessibility, presentation, and launch requirements before release-candidate work.

---

## Stage 12 — Release candidate, launch, and stabilization

**Status:** Not started

### Required work

- [ ] Feature and content freeze
- [ ] Save migration and backward-compatibility validation
- [ ] Regression, performance, hardware, accessibility, onboarding, and balance testing
- [ ] Licensing, provenance, attribution, AI disclosure, and storefront compliance review
- [ ] Pricing, marketing, support, patch, backup, and release-process preparation
- [ ] Release-candidate signoff
- [ ] Launch monitoring and bounded stabilization patches

### Gate 12

**Release Acceptance:** only the project owner approves the final build, price, storefront, date, and publication.

---

# First execution-wave progress

The original first 90-day plan was completed or overtaken much faster than forecast. The checklist below records its current disposition without creating a new schedule.

## Original Weeks 1–2

- [x] Activate Technical Architect and Producer/Roadmap responsibilities
- [x] Write the engine-evaluation specification
- [x] Define weighted criteria and non-negotiable requirements
- [x] Establish the technical risk package
- [x] Define prototype acceptance tests
- [x] Shortlist viable candidates
- [x] Decide that the multi-engine implementation comparison was disproportionate

## Original Weeks 3–6

- [x] Establish the Unity project and approved package baseline
- [x] Implement the tactile stocking, snapping, and scanning foundation
- [ ] Complete representative production-asset import and presentation validation
- [x] Test navigation inside a graybox/furnished store path
- [x] Record editor, test, build, and agent-workflow evidence
- [x] Implement data loading and validation foundations

## Original Weeks 7–10

- [x] Implement save/restore
- [x] Implement detailed-to-aggregate reconciliation
- [x] Implement two-location reporting and delegated simulation
- [x] Resolve engine selection through owner decision rather than unnecessary comparison work

## Original Weeks 11–13

- [x] Complete the engine decision record
- [x] Select and implement the technical baseline
- [x] Create the engine-specific production structure
- [ ] Complete a measured reforecast of Stages 3–8
- [ ] Approve the next bounded execution wave after current gameplay and presentation review

---

# Work allocation model

## Project owner responsibilities

The project owner retains direct control of:

- approval and rejection of decisions;
- tactile-feel judgment and playtesting;
- engine and architecture adoption;
- scope exceptions;
- spending and licensing risk;
- final asset acceptance;
- milestone and release acceptance.

## Agent responsibilities

Agents should be used aggressively for:

- repository research and traceability;
- implementation planning and decomposition;
- bounded code scaffolding and refactoring;
- schemas, validators, fixtures, and tests;
- documentation and evidence records;
- data authoring and consistency checks;
- asset inventories and provenance records;
- defect reproduction and regression generation;
- repetitive content preparation under approved constraints.

## Human-in-the-loop work

Agent production should be followed by direct human inspection and testing for:

- gameplay implementation;
- simulation formulas;
- save and migration code;
- economy tuning;
- navigation and AI behavior;
- UI flows;
- shaders, lighting, and assets;
- generated content;
- public-facing text or media.

No quantity of generated output substitutes for integration evidence or owner judgment.

---

# Current critical path

The completed portion of the original critical path is:

**Requirements → Unity decision → technical baseline → production skeleton → core hands-on store loop → customers and live employees → delegated aggregate simulation → two-location portfolio backend**

The current critical path is:

**Playtest and fix core interactions → complete remaining Stage 5/6 gameplay gaps → integrate production presentation → harden the full vertical slice → internal acceptance**

The following may run in parallel when they do not destabilize that path:

- modular asset production and intake validation;
- character and animation pipeline work;
- fictional-brand exploration;
- accessibility research;
- content inventories;
- sound-reference and audio implementation;
- business and market research;
- tool automation.

Final content production should not outpace stable system and data contracts.

---

# Reforecast and change-control rules

Reforecast the roadmap at minimum after:

1. engine selection — **completed; reforecast not yet recorded**;
2. production-foundation acceptance — **functionally completed; reforecast not yet recorded**;
3. first complete hands-on loop — **implementation completed; acceptance and reforecast pending**;
4. store-simulation acceptance;
5. owner-operator-to-portfolio proof;
6. internal vertical-slice acceptance;
7. external validation;
8. second-business selection;
9. 1.0 scope lock.

A reforecast must record:

- actual direct human hours where available;
- agent contribution and review burden;
- completed evidence;
- unresolved defects and technical debt;
- new dependencies;
- scope added, removed, or deferred;
- budget spent and remaining;
- revised aggressive, planning, and conservative dates;
- owner approval.

Schedule pressure alone does not permit silent removal of approved requirements or silent addition of unapproved systems.
