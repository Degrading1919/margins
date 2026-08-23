# Margins Master Roadmap v0.1

## Status and authority

- **Status:** Active planning baseline; evidence-based reforecast proposed August 23, 2026
- **Originally prepared:** July 27, 2026
- **Last progress sync:** August 23, 2026
- **Last schedule reforecast:** August 23, 2026
- **Authority:** This roadmap sequences approved direction but does not outrank `00_ADMIN/Decisions/Margins_Foundational_Decisions_v1.0.md` or later approved decisions.
- **Approval:** The project owner approves material roadmap baselines, milestone acceptance, scope changes, spending, and release timing.
- **Schedule meaning:** Dates are planning ranges based on observed throughput. They are not release promises.
- **Progress meaning:** A checked item is implemented or documented on merged `main`. Open or unmerged PR work is identified separately and is not counted as completed.

## Current progress snapshot

| Stage | Current status | Evidence summary |
|---|---|---|
| 0 — Foundation and governance | **Complete** | Foundational decisions, synchronized pre-production direction, roles, skills, and source-of-truth hierarchy are merged. |
| 1 — Technical requirements and shortlist | **Complete as amended** | Unity was selected by owner decision; the multi-engine execution plan was superseded. |
| 2 — Risk prototypes and engine selection | **Complete as amended** | Unity 6.5 baseline, packages, tests, persistence proof, and Windows build path are established. |
| 3 — Production skeleton and pipelines | **Functionally complete; production-content pipeline still maturing** | Runtime/data/test/save/build foundations, reusable business-operation boundaries, procedural commercial contracts, and procedural graybox generation are merged. |
| 4 — First-store hands-on loop | **Core implementation complete; playtest refinement remains** | Receiving, stocking, exact-item checkout, cleaning, Build Mode, store operation, physical movement/navigation, persistence, procurement, merchandising, and durable settings/UI foundations are merged. |
| 5 — Store simulation, customers, employees, and economy | **Substantially implemented; closeout and tuning remain** | Customers, employees, managers, demand, pricing, product mix, procurement, rent/payroll/operating costs, satisfaction/reputation inputs, reports, alerts, schedules, and persistence are implemented. |
| 6 — Delegation, off-site simulation, location travel, and reporting | **Core backend and location handoff implemented; detailed generated-store expansion in review** | Delegated simulation, policies, schedules, portfolio/property state, detailed/aggregate reconciliation, generated persistent locations, and player Visit/Return flow are merged. PR #38 is in review for full detailed generated convenience-store simulation. |
| 7 — Vertical-slice content and presentation integration | **Active production integration** | Art direction, asset ceilings, provenance structure, UI Toolkit foundation, Tripo workflow, source character work, a textured production prop, and procedural placeholder/content infrastructure exist; broad replacement of graybox content remains. |
| 8 — Internal vertical-slice hardening and acceptance | **Formal milestone not started; continuous regression work already active** | Every major implementation increment is already carrying EditMode/PlayMode regression suites and Windows builds, but full-loop tuning, profiling, accessibility, provenance audit, and formal owner acceptance remain. |
| 9–12 | **Not started as milestone phases** | External validation, commercial gate, broader 1.0 production, release-candidate work, and launch remain future stages. |

## Evidence used for the August 23 reforecast

### Repository throughput

The repository was created on **July 27, 2026 at 00:04 UTC**. By the August 23 reforecast point:

- `main` is **211 commits** ahead of the initial repository commit;
- **29 pull requests have been merged**;
- the project has moved from repository bootstrap to a tested Unity game foundation, physical first-store loop, autonomous customers, employees, procurement, merchandising, procedural commercial generation, persistent multi-location portfolio state, and generated-location travel;
- PR #38 is active but unmerged and is therefore treated as in-flight evidence rather than completed scope.

Commit and PR counts are throughput indicators, not effort estimates. Documentation commits, implementation commits, fixes, and merge commits do not carry equal production value.

### Calendar milestone calibration

Measured from repository creation:

| Milestone | Observed completion |
|---|---|
| Unity selected | **12 hours 27 minutes** after repository creation |
| Unity foundation baseline approved | **1 day 3 hours 43 minutes** |
| Runnable Unity foundation spike merged | **1 day 12 hours 13 minutes** |
| Integrated first-store playable loop merged | **9 days 23 hours 57 minutes** |
| Autonomous customers, live employees, and shared business-operation foundation merged | **10 days 23 hours 23 minutes** |
| Persistent procurement merged | **11 days 14 hours 26 minutes** |
| Physical player/NPC navigation merged | **11 days 23 hours 22 minutes** |
| Durable UI Toolkit/settings foundation merged | **12 days 15 hours 4 minutes** |
| Player-controlled merchandising/pricing merged | **18 days 19 hours 29 minutes** |
| Procedural commercial graybox generation merged | **27 days 2 hours 51 minutes** |
| Persistent delegated portfolio/property backend merged | **27 days 12 hours 45 minutes** |
| Persistent generated-location Visit/Return flow merged | **27 days 14 hours 18 minutes** |

PR #12 also recorded a particularly useful implementation measurement: approximately **38 minutes from implementation start to a runnable Windows build** for the bounded Unity foundation spike. That is evidence of exceptional agent-assisted implementation velocity for tightly scoped engineering work, not a general estimate for art, tuning, or release production.

### Reforecast interpretation

The original roadmap assumed approximately **2,000–3,400 direct human hours** to an internally accepted vertical slice and placed that milestone in 2028. Current evidence disproves that estimate for Margins' engineering and documentation workflow.

The reforecast therefore uses two different velocity classes:

1. **Agent-heavy engineering, data, tests, and documentation:** measured in hours to days for many major bounded increments.
2. **Human-bottleneck work:** art direction and final asset acceptance, 3D cleanup/integration, tactile playtesting, UX judgment, balancing, onboarding, audio, accessibility, external testing, and commercial presentation. These remain the dominant schedule risk.

The schedule must not apply engineering velocity directly to final art, polish, balancing, or market validation.

---

## August 23, 2026 evidence-based timeline reforecast

| Outcome | Aggressive case | Planning case | Conservative case |
|---|---|---|---|
| Close remaining Stage 5/6 functional gaps and validate generated-location detailed play | **late Aug–early Sep 2026** | **Sep 2026** | **Oct 2026** |
| Presentation-integrated vertical-slice candidate | **late Sep–Oct 2026** | **Oct–Nov 2026** | **Jan 2027** |
| Internally accepted vertical slice | **Oct 2026** | **Nov–Dec 2026** | **Feb–Mar 2027** |
| Public demo or controlled playtest | **Nov–Dec 2026** | **Jan–Mar 2027** | **Q2 2027** |
| Commercial production / Early Access decision gate | **Q1 2027** | **Q2 2027** | **H2 2027** |
| Possible paid Early Access, only if approved | **Q1–Q2 2027** | **Q3–Q4 2027** | **2028 or no Early Access** |
| Premium 1.0 | **Q4 2027** | **Q2–Q3 2028** | **H1–H2 2029** |

### Confidence by horizon

- **Stage 5/6 closeout:** high confidence because most required architecture and runtime systems already exist and are under active automated regression coverage.
- **Internal vertical slice:** medium confidence. Presentation throughput, owner playtesting, onboarding, tuning, and final-asset integration now dominate the critical path.
- **External validation:** medium-low confidence because test format, tester availability, feedback severity, and required iteration are not yet measured.
- **1.0:** low confidence. The second business is intentionally unselected until vertical-slice validation, and production-scale city, property-development presentation, art/content volume, localization, accessibility, launch, and market feedback remain incompletely measured.

### Remaining direct-human planning range to internal vertical-slice acceptance

Actual direct human hours have not been consistently logged across the repository, so calendar throughput is better measured than labor throughput. For planning only, the remaining Stage 5–8 work is provisionally bounded at approximately **120–300 direct human hours**, concentrated in:

- owner playtesting and bug disposition;
- production asset creation, cleanup, integration, and acceptance;
- presentation/UI/audio/onboarding work;
- tuning, profiling, accessibility, and final vertical-slice QA.

This range must be replaced with measured production-art and playtest throughput after Stage 7 has produced a coherent identity slice.

### Superseded historical schedule

The original roadmap forecast late-2026 engine selection, a Q3-2027 hands-on store loop, a Q2–Q3-2028 internal vertical slice, and approximately 2031 for 1.0. Those dates are retained only as historical estimation evidence and no longer govern planning.

---

# Stage roadmap

## Stage 0 — Foundation and governance baseline

**Status:** Complete, subject to normal maintenance

- [x] Approved foundational decisions
- [x] Synchronized project brief, pillars, scope, technical, economy, content, and art direction
- [x] Canonical assistant roles and operating standard
- [x] Repository structure and source-of-truth hierarchy
- [x] Repository-local workflow skills and skill catalog

**Gate 0:** Passed.

---

## Stage 1 — Technical requirements and candidate shortlist

**Status:** Complete as amended by `Margins_Roadmap_Amendment_001_Unity_Selection_v0.1.md`

- [x] Define engine-evaluation criteria and constraints
- [x] Bound candidate research
- [x] Reject disproportionate multi-engine execution work
- [x] Record the project owner's Unity selection

**Gate 1:** Passed through owner decision and Amendment 001.

---

## Stage 2 — Unity foundation and executable proof

**Status:** Complete

- [x] Approve Unity 6000.5.5f1 and initial package baseline
- [x] Establish URP, Input System, AI Navigation, Unity Test Framework, source-control, and Windows-build conventions
- [x] Implement first-person movement/look
- [x] Implement data-defined product pickup, rotation, shelf snapping, and feedback
- [x] Implement versioned placement persistence proof
- [x] Implement navigation proof
- [x] Run EditMode/PlayMode tests and a Windows build
- [x] Confirm no project-blocking Unity limitation

**Gate 2:** Passed.

---

## Stage 3 — Production skeleton, contracts, and reusable foundations

**Status:** Functionally complete; production asset pipeline refinement continues

### Completed

- [x] Engine-specific repository structure and assemblies
- [x] State-ownership and reconciliation boundaries
- [x] Product, fixture, checkout, inventory, employee, customer, location, operation, property, and portfolio contracts
- [x] Validation, save-versioning, migration, test, and build conventions
- [x] Reusable business-operation recipes, station capacity, task progress, employee-performance, and aggregate simulation profiles
- [x] Approved six-inch fixture grid and structural dimensional conventions
- [x] Approved architectural asset interface, building archetype, footprint/unit, and procedural interior rules
- [x] Procedural asset category/business recipe contract
- [x] Deterministic procedural commercial graybox generator for approved initial archetype implementations
- [x] Persistent generator identity/seed/version/signature integrated into portfolio state
- [x] Graybox test store and generated-location test coverage

### Remaining pipeline evidence

- [ ] Complete a repeatable production-asset intake path with provenance, source asset, normalized export, Unity import, collider/material review, and owner acceptance
- [ ] Demonstrate that path across enough asset classes to remove graybox replacement as a schedule unknown

Current repository evidence includes source character work in `04_CONTENT_PRODUCTION` and a textured safety-bollard asset integrated into Unity, so production asset work has begun; this does not by itself close the complete asset-pipeline gate.

**Gate 3:** Functionally passed for continued implementation. Production-content throughput remains a Stage 7 schedule dependency.

---

## Stage 4 — First-store hands-on operating loop

**Status:** Core implementation complete; owner-experience refinement remains

### Completed implementation

- [x] Player movement, sprint, jump, look, targeting, and interaction
- [x] Receiving deliveries and handling/opening boxes
- [x] Physical product inventory and stocking
- [x] Exact-item checkout and completed-transaction ledger
- [x] Cleaning and basic maintenance interaction
- [x] Property-wide Build Mode with six-inch fixture grid, movement, rotation, collision, and save migration
- [x] Opening/closing behavior and customer drain before final close
- [x] Persistent procurement and recurring deliveries
- [x] Player-controlled shelf merchandising and retail pricing
- [x] Physical customer/employee navigation and fixture repathing
- [x] Durable UI Toolkit menu/settings/rebinding foundation
- [x] Save/load across first-store gameplay state without revenue replay

### Remaining Stage 4 work

- [ ] Guided leasing and startup preparation in the minimum vertical-slice form
- [ ] Minimum accelerated operational time / overnight progression where needed by the approved pacing model
- [ ] Owner playtest refinement of movement, Build Mode, targeting, queueing, stocking, checkout, closing, and settings feel
- [ ] Resolve regressions discovered by those playtests

**Gate 4:** Implementation proof exists. Final subjective acceptance remains open until the owner accepts the hands-on loop after continued playtesting.

---

## Stage 5 — Store simulation, customers, employees, and economy

**Status:** Substantially implemented; closeout, recovery design, and tuning remain

### Completed or materially proven

- [x] Aggregate demand and instantiated local customers
- [x] Physical shelf-unit reservations, queueing, abandonment, exact-item sales, and no-stock handling
- [x] Player-controlled prices and price-willingness effects
- [x] Product-mix, availability, service-capacity, standards, satisfaction, and reputation inputs in delegated simulation
- [x] Detailed/aggregate product-mix and satisfaction parity tests
- [x] Local competition input
- [x] Persistent employees with cashier, stock-clerk, and manager work
- [x] Shared skill, reliability, focus, and manager-competence performance model
- [x] Employee schedules and location assignments in portfolio state
- [x] Rent, payroll, procurement, merchandise cost, base operating cost, maintenance pressure, sales, COGS, and profit reporting
- [x] Deterministic operating alerts and bounded maintenance recovery actions
- [x] Actionable location/portfolio reporting with causal inputs
- [x] Extensive automated regression coverage for customers, employees, inventory, economy, persistence, and reports

### Remaining

- [ ] Final vertical-slice tuning of demand, pricing, product mix, service quality, satisfaction, reputation, wages, costs, and failure thresholds
- [ ] Complete the player-facing recovery loop for the failure cases chosen for the vertical slice
- [ ] Complete any employee satisfaction/development behavior required by the accepted vertical-slice experience
- [ ] Add startup debt or other financing only to the depth required by approved vertical-slice scope; do not expand into the later corporate-finance system
- [ ] Owner playtest the causes and recoveries rather than relying only on deterministic tests

**Gate 5:** Expected to close during the September 2026 planning window if playtesting does not reveal a foundational economy or employee-design problem.

---

## Stage 6 — Delegation, off-site simulation, generated locations, and portfolio reporting

**Status:** Core backend and physical handoff implemented; player-facing management depth and full generated-store detail are the remaining near-term work

### Completed on merged `main`

- [x] Manager appointment and bounded authority
- [x] Delegated aggregate simulation
- [x] Location policies for purchasing, pricing/reorder behavior, operating standards, maintenance authority, and spending limits
- [x] Employee schedules and assignments
- [x] Deterministic alerts/exceptions and acknowledgement state
- [x] Detailed/aggregate financial and inventory reconciliation without replay
- [x] Persistent company, brand, business-location, property, and commercial-unit identity graph
- [x] Leased/owned tenure, property acquisition, unit improvements, vacant units, and multiple-brand/multi-property structural support
- [x] Removal of the hard two-location portfolio count cap
- [x] Deterministic persistent procedural-location materialization
- [x] Player-facing management Visit/Return flow into generated locations
- [x] Physical checkout in a generated location using existing authoritative first-store systems
- [x] Leave → aggregate operation → deterministic return → additional physical sale without duplicate cash or inventory
- [x] Consolidated location/portfolio reports and persistence

### In review, not yet counted as complete

PR #38 proposes full detailed simulation in generated convenience stores, including generated-location customers, employee work, physical deliveries/stocking, cleaning, runtime NavMesh, operating controls, and isolation across multiple generated stores. Until merged, these remain in-flight evidence.

### Remaining after the current generated-store work

- [ ] Complete player-facing management UI for schedules, delegation standards, alerts/exceptions, and remaining policy/budget controls
- [ ] Define and implement the approved minimum travel presentation beyond the current immediate management transition only if needed for vertical-slice acceptance
- [ ] Resolve safe save/load behavior while physically visiting a generated location or explicitly keep the current leave-before-save restriction for the slice
- [ ] Owner-playtest physical intervention, location switching, reporting, and detailed/aggregate continuity

**Gate 6:** Planning target is September 2026. The backend is no longer the principal vertical-slice schedule risk.

---

## Stage 7 — Vertical-slice content and presentation integration

**Status:** Active production integration; now the principal critical-path risk

### Foundation already complete

- [x] Stylized Contemporary Americana direction
- [x] Mile 7 identity-slice and art/UI review
- [x] Approved 3D asset ceilings and collider rules
- [x] Asset provenance ledger structure
- [x] Tripo 3D prompting workflow
- [x] Durable UI Toolkit runtime foundation
- [x] Approved procedural architectural dimensions, interfaces, archetypes, and business asset categories
- [x] Initial production source character work in repository
- [x] Initial textured environmental prop integrated into Unity
- [x] Procedural placeholder asset library and generated commercial-layout infrastructure

### Required vertical-slice integration

- [ ] Replace identity-critical graybox architecture and fixtures with approved production assets
- [ ] Complete two visually and economically distinct convenience-store locations using the same business foundation
- [ ] Integrate representative customer and employee character assets, animation, and readable interaction feedback
- [ ] Build fictional convenience-store brands, products, packaging, signs, and merchandising sufficient for the slice
- [ ] Establish lighting, color, atmosphere, silhouettes, and day/time presentation appropriate to the approved visual direction
- [ ] Integrate initial ambience, functional audio, and feedback audio
- [ ] Complete guided startup/onboarding presentation
- [ ] Complete production UI for store operation, management, schedules/policies/alerts, and reports
- [ ] Complete provenance, licensing, attribution, and AI-involvement records for shipped vertical-slice assets
- [ ] Measure production-asset throughput and use it to replace the provisional Stage 7 estimate

**Gate 7:** A coherent identity slice must make Margins look and feel like a game rather than a systems-heavy graybox while preserving the proven runtime foundation.

**Planning window:** October–November 2026; this is the largest source of variance in the internal vertical-slice forecast.

---

## Stage 8 — Internal vertical-slice hardening and acceptance

**Status:** Formal milestone not started; continuous regression discipline is already active

### Required work

- [ ] At least three complete internal playthroughs through stable multi-location delegation
- [ ] Defect triage and regression coverage for discovered failures
- [ ] Save corruption, restore, transition, and migration testing at vertical-slice scale
- [ ] Detailed/aggregate parity testing at accepted tuning values
- [ ] Economy and progression tuning
- [ ] Onboarding, controls, feedback, management UI, and report usability testing
- [ ] Navigation, performance, memory, and load-time profiling
- [ ] Accessibility-risk review and minimum requirements
- [ ] Scope audit against approved vertical-slice commitments
- [ ] Asset provenance/licensing audit
- [ ] Known-limitations and deferred-work record

### Acceptance evidence

- [ ] No unresolved blocker involving save integrity, portfolio correctness, or core progression
- [ ] Major failures have reproducible cases and dispositions
- [ ] A new tester can understand the operating loop and major causes of success/failure
- [ ] Detailed and aggregate operation reconcile across the accepted location flow
- [ ] The owner confirms hands-on operation, delegation, expansion, and reporting are enjoyable enough to proceed
- [ ] Every approved vertical-slice commitment is demonstrated or explicitly returned for owner disposition

**Gate 8:** Internal Vertical Slice Accepted.

**Planning window:** November–December 2026, with February–March 2027 retained as the conservative case if presentation or full-loop playtesting exposes major rework.

---

## Stage 9 — Public demo or controlled playtest

**Status:** Not started

- [ ] Choose controlled playtest versus public demo
- [ ] Define target player profiles and questions
- [ ] Harden distribution, crash reporting, feedback capture, and privacy practices
- [ ] Improve onboarding/accessibility from observed failures
- [ ] Prepare only required storefront/marketing material
- [ ] Collect behavioral, qualitative, defect, and retention evidence
- [ ] Separate polish complaints from foundational design problems
- [ ] Reforecast commercial scope and schedule from external evidence

**Gate 9 — Public Validation:** decide among continued private development, another test cycle, paid Early Access, redesign, or stop.

**Planning window:** January–March 2027.

---

## Stage 10 — Commercial production gate

**Status:** Not started

Decision options remain:

1. enter paid Early Access;
2. remain private and continue toward a larger release build;
3. conduct another validation cycle;
4. reduce or restructure 1.0 scope through owner decision;
5. pause or stop development.

Required evidence includes product appeal, stability, content-production throughput, support burden, budget/runway, second-business and property forecast, pricing/storefront/legal/disclosure needs, and a revised schedule.

**Gate 10 — Commercial Baseline Approved.**

**Planning window:** Q2 2027.

---

## Stage 11 — 1.0 production

**Status:** Not started as a milestone phase; several reusable foundations already exist

### Approved minimum

- [ ] At least two complete business categories
- [ ] Property ownership and development
- [ ] Core holding-company progression
- [ ] Premium single-player release of coherent quality

### Foundation already available before Stage 11 begins

The current repository has pulled forward several later foundations that the original roadmap assumed would be built much later:

- reusable business-operation recipes and simulation profiles;
- category/capability-based procedural business asset placement;
- procedural commercial building/unit generation foundations;
- persistent company/brand/location/property/unit identity graph;
- property acquisition and unit-improvement state;
- multi-location detailed/aggregate reconciliation;
- portfolio reporting and generated-location materialization.

These foundations reduce expected engineering cost but do not count as a finished second business, final property-development gameplay, final city, or release content.

### Remaining directional work

- [ ] Select the second business only after vertical-slice evidence
- [ ] Implement the selected business through the shared operation and procedural-content foundations
- [ ] Deepen convenience-retail progression where evidence supports it
- [ ] Implement player-facing property purchase, renovation, subdivision, and approved development depth
- [ ] Complete holding-company, brand, headquarters, and portfolio progression
- [ ] Expand the city and commercial-property presentation to release scope
- [ ] Deepen financing, competitors, recovery, administration, and endgame only where approved
- [ ] Complete UX, accessibility, art, audio, performance, onboarding, localization, and release-quality work
- [ ] Continuously validate save migration and detailed/aggregate parity

Explicitly unassigned until separately approved: drivable vehicles, detailed M&A milestone, public markets/IPO, public mod support, a third business category, multiplayer, full persistent city residents, unrestricted mixed-use/multi-story construction, and post-1.0 promises.

**Gate 11 — 1.0 Scope and Content Lock.**

**Planning release window:** Q2–Q3 2028; aggressive Q4 2027; conservative H1–H2 2029.

---

## Stage 12 — Release candidate, launch, and stabilization

**Status:** Not started

- [ ] Feature/content freeze
- [ ] Save migration/backward-compatibility validation
- [ ] Regression, performance, hardware, accessibility, onboarding, and balance testing
- [ ] Licensing/provenance/attribution/AI-disclosure/storefront compliance review
- [ ] Pricing, marketing, support, patch, backup, and release-process preparation
- [ ] Release-candidate signoff
- [ ] Launch monitoring and bounded stabilization patches

**Gate 12 — Release Acceptance:** only the project owner approves the final build, price, storefront, date, and publication.

---

# Current execution wave — August through November 2026

The original 90-day plan has been overtaken. The next execution wave is now organized around the actual remaining critical path.

## August 23–September 13 — close the functional vertical slice

Priority order:

1. review/merge or revise PR #38 without treating unmerged work as complete;
2. close remaining Stage 5/6 management, recovery, save/travel, and pacing gaps;
3. perform owner playtests across first store, delegated operation, generated-location visit/return, and multi-location reconciliation;
4. fix high-value interaction and state-continuity defects immediately;
5. keep full EditMode/PlayMode suites and Windows builds green.

Exit target: the owner-operator-to-portfolio loop is functionally complete in graybox form, with no major system still awaiting invention.

## September 14–October 31 — presentation and content conversion

Priority order:

1. prove the full production-asset intake/provenance path;
2. replace identity-critical store/environment/fixture/product assets;
3. integrate representative production characters and animation;
4. complete store-management/report UI needed by the slice;
5. establish lighting, audio, fictional branding, packaging, and onboarding;
6. measure actual asset throughput and reforecast Stage 7 when enough samples exist.

Exit target: a presentation-integrated vertical-slice candidate suitable for full-loop internal testing.

## November–December — hardening and internal acceptance

Priority order:

1. complete repeated full-loop playthroughs;
2. fix blockers and high-severity regressions;
3. tune economy, pacing, staffing, demand, and failure recovery;
4. profile representative hardware and heavy scenes;
5. complete accessibility and provenance checks;
6. decide whether the build is ready for controlled/public validation.

Exit target: Gate 8 owner acceptance or an evidence-backed list of changes required before acceptance.

---

# Work allocation model

## Project owner responsibilities

The project owner retains direct control of:

- approval/rejection of decisions;
- tactile-feel judgment and playtesting;
- scope exceptions and spending;
- final asset acceptance;
- milestone and release acceptance.

## Agent-heavy work

Agents should continue to carry most:

- repository research and traceability;
- implementation and focused refactoring;
- schemas, validators, fixtures, tests, and migrations;
- documentation and evidence records;
- data authoring and consistency checks;
- defect reproduction/regression generation;
- repetitive content preparation under approved constraints.

## Human-bottleneck work

Schedule conservatively around:

- playtesting and subjective feel;
- final visual judgment;
- 3D cleanup and asset acceptance;
- economy/pacing tuning;
- UX/onboarding decisions;
- external tester coordination and interpretation;
- commercial/release decisions.

These bottlenecks, rather than raw coding throughput, now govern the critical-path forecast.

---

# Current critical path

Completed critical-path work:

**Repository foundation → Unity decision → executable Unity foundation → hands-on store loop → customers/employees → procurement/merchandising → physical navigation → reusable operations → procedural commercial generation → persistent portfolio/property backend → generated-location Visit/Return**

Current critical path:

**Finish full generated-location detailed operation → close Stage 5/6 playtest gaps → production asset/content integration → onboarding/UI/audio/presentation → full-loop hardening/tuning → internal acceptance → external validation**

Parallel work that may continue without destabilizing that path:

- modular asset generation/cleanup and provenance preparation;
- character/animation production;
- fictional-brand/product-content production;
- procedural architecture and asset-library expansion under approved contracts;
- accessibility research;
- sound-reference and audio preparation;
- tool automation and validation.

Do not let speculative later-business, driving, M&A, or broad city systems displace vertical-slice presentation and acceptance work.

---

# Reforecast and change-control rules

The August 23 reforecast closes the previously overdue reforecast points for:

1. engine selection;
2. production-foundation acceptance;
3. first complete hands-on loop;
4. early store/delegation/portfolio implementation velocity.

Reforecast again after:

1. PR #38 / full generated-location detailed-operation disposition;
2. Stage 7 has enough production assets to measure sustained asset throughput;
3. internal vertical-slice acceptance;
4. first external validation cycle;
5. second-business selection;
6. 1.0 scope lock.

Each future reforecast should record:

- calendar duration and actual direct human hours where available;
- agent contribution and human review burden;
- completed evidence and unresolved defects;
- content/asset throughput;
- dependencies added or retired;
- budget spent and remaining;
- revised aggressive, planning, and conservative dates;
- project-owner approval.

Schedule pressure never permits silent removal of approved requirements or silent addition of unapproved scope.
