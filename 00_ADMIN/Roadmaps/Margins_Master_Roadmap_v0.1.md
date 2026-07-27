# Margins Master Roadmap v0.1

## Status and authority

- **Status:** Proposed first roadmap baseline
- **Prepared:** July 27, 2026
- **Authority:** This roadmap sequences approved direction but does not outrank `00_ADMIN/Decisions/Margins_Foundational_Decisions_v1.0.md` or approve unresolved features, technology, spending, dates, or scope changes.
- **Approval:** The project owner must approve this baseline and every material rebaseline.
- **Schedule meaning:** All dates are planning ranges, not release promises.

## Roadmap purpose

This roadmap provides a dependency-aware path from the synchronized pre-production foundation to:

1. an engine and technical foundation selected through evidence;
2. a durable internal vertical slice proving the convenience-store owner-operator-to-portfolio loop;
3. a public demo or controlled playtest;
4. an evidence-based decision on paid Early Access or continued private production; and
5. a complete premium 1.0 release meeting the approved minimum scope.

The roadmap uses rolling-wave planning:

- the next 90 days are described in the most detail;
- work through the internal vertical slice is milestone-level but actionable;
- work beyond public validation remains directional until earlier evidence exists.

## Planning assumptions

### Production capacity

The planning baseline assumes:

- PC-only development;
- less than **$1,000** in total pre-revenue development spending unless the project owner approves a change;
- approximately **20–30 direct human development hours per week**;
- a planning midpoint of roughly **25 direct human hours per week**;
- extensive agentic AI work for research, documentation, decomposition, code assistance, data authoring, test generation, validation, and repetitive content tasks;
- the project owner remains the decision-maker, integrator, playtester, and final acceptor;
- no committed outside team, contractor capacity, publisher support, or external funding.

### Interpreting the current three-hour pace

The project owner reports that the foundational decisions, role architecture, quality audits, and pre-production synchronization completed during roughly three hours of focused collaboration.

That demonstrates unusually high velocity for:

- decision framing;
- repository research;
- document authoring;
- cross-document consistency review;
- agent-assisted governance work.

It should move document-heavy stages toward the aggressive end of their ranges. It should **not** be extrapolated directly to implementation, integration, debugging, asset production, balancing, or playtesting. Those areas contain feedback loops that cannot be compressed in the same proportion merely by generating more output.

### Estimation model

The stage estimates total roughly **2,000–3,400 direct human hours** from roadmap approval to an internally accepted vertical slice. At 20–30 hours per week, that creates a broad raw range of approximately 16–39 months. Agentic acceleration, overlapping workstreams, strong scope control, and reuse support a practical planning target near the middle rather than the extremes.

The baseline target is therefore:

- **Internal vertical slice:** approximately Q2–Q3 2028
- **Public demo or controlled playtest:** approximately Q4 2028–Q1 2029
- **Possible paid Early Access:** approximately H2 2029, only if the quality gate passes
- **Premium 1.0 release:** approximately 2031 in the planning case

These targets must be reforecast after engine selection, risk prototypes, first-store playability, and vertical-slice acceptance.

---

## Timeline scenarios

| Outcome | Aggressive case | Planning case | Conservative case |
|---|---|---|---|
| Engine and technical baseline | October 2026 | November–December 2026 | Q1 2027 |
| First complete hands-on store loop | Q2 2027 | Q3 2027 | Q1 2028 |
| Internal vertical slice | Q4 2027–Q1 2028 | Q2–Q3 2028 | H1 2029 |
| Public demo or controlled playtest | Q2 2028 | Q4 2028–Q1 2029 | H2 2029 |
| Possible paid Early Access | Q1 2029 | H2 2029 | 2030 or later |
| Premium 1.0 | H2 2030 | 2031 | 2032–2034 |

### Scenario interpretation

- **Aggressive:** major technical assumptions prove correct quickly; a well-fitting engine and asset pipeline are found; the vertical-slice scope remains disciplined; agent workflows are reliable; little foundational rework occurs.
- **Planning:** normal prototype failures, integration work, tooling development, content iteration, and playtest revision occur without a major reset.
- **Conservative:** engine or architecture changes, weak asset compatibility, navigation or simulation problems, save-system rework, prolonged balancing, life interruptions, or scope drift materially delay progress.

The planning case is the recommended baseline. The aggressive case is a target of opportunity, not a commitment.

---

# Stage roadmap

## Stage 0 — Foundation and governance baseline

**Calendar:** Completed July 2026  
**Status:** Complete, subject to normal maintenance

### Outcome

The project has an approved foundational decision record, synchronized pre-production direction, nine canonical assistant roles, activation prompts, four repository-local workflow skills, and a documented source-of-truth hierarchy.

### Completed evidence

- approved foundational decisions;
- three-gate decisions and roles audit;
- synchronized project brief, pillars, scope, technical direction, content/commercial strategy, economy/progression direction, and art/audio/presentation direction;
- canonical roles and activation prompts;
- repository structure and role-versus-skill model.

### Exit gate

**Passed:** repository foundation is sufficiently aligned to begin technical selection and roadmap execution.

---

## Stage 1 — Technical requirements and candidate shortlist

**Proposed duration:** 2–4 weeks  
**Planning calendar:** August 2026  
**Direct human effort:** approximately 50–100 hours

### Objective

Convert the approved game direction into a measurable engine and technical-evaluation specification without selecting technology prematurely.

### Required work

- define engine-evaluation criteria and weighting;
- identify a bounded shortlist of viable engines or frameworks;
- define prototype acceptance tests before building them;
- define target development-machine and provisional player-hardware assumptions;
- identify licensing, deployment, asset-store, source-control, debugging, and build constraints;
- evaluate AI-agent workflow compatibility;
- define the minimum evidence required to select an engine;
- create a technical risk register.

### Human work

- approve evaluation criteria and weights;
- determine acceptable learning burden and workflow preferences;
- inspect candidate-editor usability;
- approve prototype scope and spending;
- reject candidates that conflict with the intended development experience.

### Agent-delegable work

- candidate research and comparison tables;
- license and pricing summaries with source citations;
- technical-requirement traceability;
- prototype-plan drafts;
- risk-register construction;
- documentation and issue decomposition.

### Exit evidence

- approved engine-evaluation matrix;
- candidate shortlist;
- approved risk-prototype plan;
- provisional hardware and performance assumptions;
- no engine selected yet.

### Gate 1

**Technical Evaluation Ready:** the project owner approves the criteria, shortlist, and prototype plan.

---

## Stage 2 — Risk prototypes and engine selection

**Proposed duration:** 6–12 weeks  
**Planning calendar:** September–November 2026  
**Direct human effort:** approximately 180–320 hours

### Objective

Use executable evidence to select the engine and retire the assumptions most likely to invalidate the project.

### Prototype set

The exact number and implementation may change, but the evaluation must cover:

1. first-person product pickup, box handling, shelf snapping, and item scanning;
2. furnished-store customer and employee navigation;
3. data-driven product loading and validation;
4. save and restore of a modified store layout plus business state;
5. transition between detailed present simulation and aggregate absent simulation;
6. two-location state and portfolio reporting;
7. import and presentation of representative stylized environment, product, and character assets;
8. debugging, profiling, automated testing, and build/export workflow.

### Human work

- implement or directly supervise the decisive prototype interactions;
- evaluate editor usability and iteration friction;
- playtest tactile feel;
- assess debugging clarity;
- approve the engine and major dependency baseline.

### Agent-delegable work

- scaffold prototypes;
- generate test fixtures and structured sample data;
- implement bounded comparison tasks;
- document failures and performance observations;
- maintain the requirements-to-evidence matrix;
- draft the final technology decision record.

### Exit evidence

- comparable results from the viable candidates;
- known limitations and mitigation paths;
- selected engine and language recorded in an approved decision;
- rejected candidates and reasons documented;
- approved initial technical baseline.

### Gate 2

**Engine Adoption:** no production build begins until the project owner approves the technical decision record.

---

## Stage 3 — Production skeleton, data contracts, and pipelines

**Proposed duration:** 6–10 weeks  
**Planning calendar:** November 2026–January 2027  
**Direct human effort:** approximately 150–260 hours

### Objective

Turn the chosen engine into a durable production foundation rather than immediately building unstructured features.

### Required work

- establish engine-specific repository structure;
- define module and state-ownership boundaries;
- establish coding, data, test, and naming conventions;
- define initial product, fixture, employee, customer-context, location, and business-state contracts;
- create schema validation and error reporting;
- establish save-versioning and migration conventions;
- establish automated test and build workflows appropriate to the project;
- establish the controlled asset ledger and import pipeline;
- create a graybox test store and test block;
- create milestone evidence and defect-recording conventions.

### Human work

- approve architecture and data-contract boundaries;
- validate that tooling remains understandable and maintainable;
- approve asset-pipeline and source-control practices;
- review agent-produced code before adoption.

### Agent-delegable work

- code and project scaffolding;
- schemas, validators, fixtures, and tests;
- documentation generation;
- asset-ledger setup;
- repetitive import or conversion scripts;
- CI or local validation automation where practical.

### Exit evidence

- clean project build;
- representative validated data loads correctly;
- graybox store saves and reloads;
- automated checks run reproducibly;
- one representative asset completes the provenance-to-runtime pipeline;
- architecture decision records exist for irreversible choices.

### Gate 3

**Production Foundation Accepted:** the project owner and Data/Validation role accept the foundation as adequate for vertical-slice implementation.

---

## Stage 4 — First-store hands-on operating loop

**Proposed duration:** 14–22 weeks  
**Planning calendar:** January–June 2027  
**Direct human effort:** approximately 350–550 hours

### Objective

Create a playable graybox convenience store in which the fundamental physical work is responsive and enjoyable before management complexity is layered on top.

### Required work

- player movement and interaction;
- guided leasing and initial store preparation in simplified form;
- receiving deliveries and handling boxes;
- stocking snapped products onto valid fixtures;
- product and inventory state;
- checkout and item scanning;
- cleaning and basic maintenance;
- grid-based fixtures and equipment placement;
- opening and closing the store;
- accelerated operational time and overnight skip in minimum viable form;
- save and reload of player, layout, inventory, and store state;
- basic feedback, prompts, and error recovery.

### Scope rule

Use placeholder or low-cost assets where presentation does not affect the mechanic being tested. Do not build final city content, deep economy, advanced management, fuel systems, driving, or a second business here.

### Exit evidence

A new player can:

1. enter a mostly empty leased store;
2. place essential fixtures;
3. receive products;
4. stock shelves;
5. open the store;
6. scan and sell products;
7. clean or maintain essential equipment;
8. close the day;
9. save, exit, reload, and continue.

### Gate 4

**Hands-on Loop Accepted:** repeated internal play confirms that stocking, scanning, layout, and daily operation are understandable and sufficiently satisfying to justify building the deeper simulation around them.

---

## Stage 5 — Store simulation, customers, employees, and economy

**Proposed duration:** 14–24 weeks  
**Planning calendar:** April–October 2027, overlapping the later part of Stage 4  
**Direct human effort:** approximately 350–600 hours

### Objective

Make the store succeed or fail for understandable reasons and introduce the people systems required for delegation.

### Required work

- aggregate local demand model;
- instantiated nearby customers representing demand;
- basic satisfaction, reputation, pricing, and product-mix effects;
- understandable local competition effects;
- revenue, inventory cost, rent, payroll, debt, and simplified operating expenses;
- visible failure pressure and initial recovery actions;
- persistent employee records;
- hiring, scheduling, task assignment, reliability, skill, and satisfaction;
- at least two worker roles;
- employee execution of tasks the player learned physically;
- basic manager role and bounded decision behavior;
- actionable store-level reporting;
- scenario fixtures for economy and people validation.

### Exit evidence

- business outcomes can be traced to pricing, stock availability, customer demand, staffing, cleanliness, capacity, or management quality;
- two worker roles can perform meaningful store tasks;
- employee state persists correctly;
- the player can recover from at least several designed operating failures;
- reports explain causes rather than only displaying totals.

### Gate 5

**Store Simulation Accepted:** the first location functions as a coherent business rather than a collection of disconnected minigames.

---

## Stage 6 — Delegation, off-site simulation, second location, and portfolio reporting

**Proposed duration:** 16–26 weeks  
**Planning calendar:** August 2027–February 2028  
**Direct human effort:** approximately 400–650 hours

### Objective

Prove the defining Margins transition from hands-on owner-operator to multi-location manager.

### Required work

- manager appointment and authority;
- remote prices, schedules, budgets, policies, and purchasing rules in bounded form;
- manager communication, alerts, and exceptions;
- aggregate off-site business simulation;
- transition between detailed and aggregate state without duplication, loss, or exploitable discontinuity;
- second convenience-store location with meaningfully different market conditions;
- local market differences and understandable competitor effects;
- combined location and portfolio reporting;
- travel between locations through the approved vertical-slice traversal boundary;
- physical intervention where remote control is intentionally insufficient;
- persistence across both locations and transitions.

### Exit evidence

The player can:

1. operate the first store personally;
2. hire and develop employees;
3. appoint a manager;
4. establish remote policies;
5. open a second location;
6. leave the first location running in aggregate mode;
7. observe understandable consequences from manager quality and policy choices;
8. compare both locations through actionable reports;
9. return physically and find a coherent detailed state;
10. save and restore the entire two-location portfolio.

### Gate 6

**Owner-Operator-to-Portfolio Loop Proven:** the project has demonstrated its unique core progression in functional form.

---

## Stage 7 — Vertical-slice content and presentation integration

**Proposed duration:** 12–20 weeks  
**Planning calendar:** October 2027–March 2028, overlapping Stages 5 and 6  
**Direct human effort:** approximately 300–500 hours

### Objective

Replace enough prototype presentation with coherent Stylized Contemporary Americana content to test the intended player experience rather than a purely graybox simulation.

### Required work

- one compact authored commercial block;
- two visually and economically distinct convenience-store locations;
- approved visual-reference implementation through original work;
- modular store, fixture, product, prop, employee, customer, signage, and environment assets;
- fictional brands and packaging sufficient for the slice;
- lighting, color, atmosphere, silhouettes, and readability;
- minimum viable animation and interaction feedback;
- initial ambience and functional audio feedback without pretending the complete audio direction is solved;
- onboarding and guided-startup presentation;
- UI presentation across store operation, management, alerts, and reports;
- asset ledger, provenance, license, attribution, and AI-involvement records.

### Exit evidence

- the slice communicates the approved tone and visual identity;
- all integrated assets have traceable provenance;
- the environment supports gameplay and navigation;
- presentation makes financial and physical progression legible;
- the content set remains within the approved vertical-slice boundary.

### Gate 7

**Presentation Coherence Accepted:** the vertical slice is recognizable as Margins rather than only a technical prototype.

---

## Stage 8 — Internal vertical-slice hardening and acceptance

**Proposed duration:** 8–16 weeks  
**Planning calendar:** March–July 2028  
**Direct human effort:** approximately 220–400 hours

### Objective

Convert the integrated build into a stable internal vertical slice and determine whether the project’s core premise is genuinely worth continuing.

### Required work

- full-loop internal playthroughs;
- defect triage and regression coverage;
- save corruption, restore, transition, and migration testing;
- detailed-versus-aggregate parity testing;
- economy and progression tuning sufficient for the slice;
- onboarding, control, feedback, and report usability testing;
- navigation, performance, memory, and load-time profiling;
- accessibility-risk review and minimum requirements proposal;
- scope audit against the approved commitments;
- asset-provenance and licensing audit;
- documentation of known limitations and deferred work.

### Proposed acceptance evidence

- at least three complete internal playthroughs from startup through stable two-location delegation;
- no unresolved blocker involving save integrity, portfolio-state correctness, or core progression;
- major failures have reproducible cases and dispositions;
- a new tester can understand the operating loop and major causes of success or failure;
- the owner confirms the hands-on, delegation, and portfolio layers are each enjoyable enough to continue;
- every approved vertical-slice commitment is either demonstrated or explicitly returned for owner disposition.

### Gate 8

**Internal Vertical Slice Accepted:** the project owner formally accepts, revises, or rejects the vertical slice based on evidence. Failure at this gate may trigger redesign, scope reduction, technology revision, or cancellation rather than automatic continuation.

---

## Stage 9 — Public demo or controlled playtest

**Proposed duration:** 10–18 weeks after internal acceptance  
**Planning calendar:** Q3 2028–Q1 2029

### Objective

Test the vertical slice with external players and determine whether the game communicates its value outside the development context.

### Required work

- choose controlled playtest versus public demo;
- define target player profiles and questions;
- harden build distribution, crash reporting, feedback capture, and privacy practices;
- improve onboarding and accessibility based on observed failures;
- prepare only the storefront and marketing material needed for the selected test path;
- collect behavioral, qualitative, defect, and retention evidence;
- distinguish polish complaints from foundational design problems;
- update risks, forecasts, and commercial assumptions.

### Exit evidence

- external players can complete and understand the core loop;
- feedback indicates whether hands-on work, delegation, and portfolio progression form a compelling whole;
- technical stability and support burden are measured;
- the project has evidence for the next commercial decision.

### Gate 9

**Public Validation:** decide among continued private development, another test cycle, quality-gated paid Early Access, major redesign, or project stop.

---

## Stage 10 — Commercial production gate

**Proposed duration:** 4–8 weeks  
**Planning calendar:** following public validation

### Objective

Choose the commercial path based on evidence rather than treating Early Access as inevitable.

### Decision options

1. enter paid Early Access;
2. remain private and continue toward a larger release build;
3. conduct another public validation cycle;
4. reduce or restructure 1.0 scope while preserving approved minimums only through a new owner decision;
5. pause or stop development.

### Required evidence

- product appeal and differentiation;
- technical stability;
- content-production throughput;
- support burden;
- budget and runway;
- forecast for the second business and property systems;
- pricing, storefront, legal, disclosure, marketing, and community requirements;
- revised schedule scenarios.

### Gate 10

**Commercial Baseline Approved:** no paid product or public release commitment occurs without an approved decision record.

---

## Stage 11 — 1.0 production

**Proposed duration after public validation:** approximately 24–42 months  
**Planning calendar:** approximately 2029–2031 in the planning case

### Required approved minimum

The 1.0 target must contain:

- at least two complete business categories;
- property ownership and development;
- core holding-company progression;
- a premium single-player release of coherent quality.

### Directional workstreams

- select the second business only after vertical-slice evidence;
- design and implement the selected business through the shared foundation;
- deepen convenience-retail progression where evidence supports it;
- implement property purchase, renovation, subdivision, and an approved development depth;
- implement company, brand, headquarters, and portfolio progression to the accepted 1.0 boundary;
- expand the city through handcrafted modular districts only as required by the approved content plan;
- deepen economy, financing, administration, recovery, competitors, and endgame where milestone decisions approve them;
- complete UX, accessibility, art, audio, performance, onboarding, localization, and release-quality work;
- continuously validate detailed and aggregate simulation parity, save migration, and content contracts.

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

**1.0 Scope and Content Lock:** approve the final business, property, holding-company, city, content, accessibility, presentation, and launch requirements before release-candidate work.

---

## Stage 12 — Release candidate, launch, and stabilization

**Proposed duration:** 3–6 months after 1.0 feature and content lock

### Objective

Deliver a stable premium PC release without allowing late optional features to displace quality work.

### Required work

- feature and content freeze;
- save migration and backward-compatibility validation appropriate to prior public builds;
- regression, performance, hardware, accessibility, onboarding, and balance testing;
- licensing, provenance, attribution, AI disclosure, and storefront compliance review;
- pricing, marketing, support, patch, backup, and release-process preparation;
- release-candidate signoff;
- launch monitoring and bounded stabilization patches.

### Gate 12

**Release Acceptance:** only the project owner approves the final build, price, storefront, date, and publication.

---

# First 90-day execution plan

The first 90 days after roadmap approval should be treated as the highest-confidence portion of this roadmap.

## Weeks 1–2

- activate the Technical Architect and Producer/Roadmap roles;
- write the engine-evaluation specification;
- define weighted criteria and non-negotiable requirements;
- establish the technical risk register;
- define prototype acceptance tests;
- shortlist viable candidates;
- identify any required no-cost or low-cost representative assets.

## Weeks 3–6

- establish minimal candidate projects;
- implement the tactile stocking, snapping, and scanning spike;
- test representative asset import and presentation;
- test navigation inside a furnished store;
- record editor, debugging, build, and agent-workflow friction;
- begin data-loading and validation spike.

## Weeks 7–10

- implement save/restore spike;
- implement detailed-to-aggregate transition spike;
- implement two-location reporting spike;
- compare performance, maintainability, tooling, licensing, and learning burden;
- eliminate candidates that fail non-negotiable requirements.

## Weeks 11–13

- complete the engine decision record;
- select the technical baseline;
- create the initial engine-specific production structure;
- reforecast Stages 3–8 using actual prototype velocity;
- approve the next 90-day plan.

---

# Work allocation model

## Project owner responsibilities

The project owner should retain direct control of:

- approval and rejection of decisions;
- tactile-feel judgment;
- playtesting and usability observation;
- engine and architecture adoption;
- scope exceptions;
- spending and licensing risk;
- final asset acceptance;
- milestone and release acceptance.

## Agent responsibilities

Agents should be used aggressively for:

- repository research and traceability;
- comparison matrices;
- implementation planning and decomposition;
- bounded code scaffolding and refactoring;
- schemas, validators, fixtures, and tests;
- documentation and evidence records;
- data authoring and consistency checks;
- asset inventories and provenance records;
- defect reproduction and regression generation;
- repetitive content preparation under approved constraints.

## Human-in-the-loop work

The following should generally use agent production followed by direct human inspection and testing:

- gameplay implementation;
- simulation formulas;
- save and migration code;
- economy tuning;
- navigation and AI behavior;
- UI flows;
- shaders, lighting, and assets;
- generated content;
- public-facing text or media.

No quantity of generated output substitutes for integration evidence.

---

# Critical path

The critical path to the vertical slice is:

**Requirements → risk prototypes → engine decision → production skeleton → hands-on store loop → store simulation and people → delegation and aggregate simulation → second location and reporting → integration → internal acceptance**

The following may run in parallel only when they do not destabilize that path:

- visual reference studies and modular asset planning;
- fictional-brand exploration;
- schema and validation design;
- accessibility research;
- content inventories;
- sound-reference exploration;
- business and market research;
- tool automation.

Final content production should not outpace stable system and data contracts.

---

# Reforecast and change-control rules

Reforecast the roadmap at minimum after:

1. engine selection;
2. production-foundation acceptance;
3. first complete hands-on loop;
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

# Current recommendation

Adopt the **planning case** as the working baseline:

- engine and technical baseline by late 2026;
- internally accepted vertical slice around Q2–Q3 2028;
- public validation around Q4 2028–Q1 2029;
- possible Early Access during H2 2029 only if justified;
- premium 1.0 during 2031, with a credible range from H2 2030 to 2034 depending on evidence and scope.

The first major opportunity to move this schedule substantially earlier or later is the engine-and-risk-prototype stage. No later date should be treated as reliable until that evidence exists.
