---
name: margins-business-type-designer
description: Explicitly invoked workflow for designing or substantially revising a Margins business type while maximizing shared-system reuse, defining detailed and off-site simulation, and preserving the owner-to-portfolio progression. Use for business-category proposals such as convenience stores, gas stations, coffee shops, laundromats, arcades, or hobby shops. Do not use for minor product lists, isolated balance changes, implementation code, or approving project scope.
---

# Margins Business Type Designer

Produce one bounded business-type design record that fits the shared Margins simulation framework instead of becoming an unrelated mini-game.

## Authority and inputs

Require explicit invocation as `$margins-business-type-designer`.

Before designing:

1. Read `01_PRE-PRODUCTION/1.1 Core Vision/Margins_Project_Brief_v0.1.md`.
2. Read `01_PRE-PRODUCTION/1.2 Design Pillars/Margins_Design_Pillars_v0.1.md`.
3. Read `01_PRE-PRODUCTION/1.3 Feature Set & Scope/Margins_Initial_Scope_Boundaries_v0.1.md`.
4. Read current approved decisions, relevant core-system documents, schemas, and existing business definitions.
5. Identify the target milestone: exploration, vertical slice, post-slice production, or later expansion.
6. Discover existing reusable systems before proposing new ones.

Ask only for creative decisions that cannot be derived from current authority and materially change the design.

## Ownership boundary

- **Owned:** structure the candidate business, map reusable systems, identify unique mechanics and risks, expose scope and engineering implications, and produce a recommendation.
- **Recommended:** business selection, feature tradeoffs, content priorities, and prototype order.
- **Prohibited:** lock creative direction, approve scope exceptions, select an engine or service, invent unapproved schema fields, commit implementation, or treat estimates as measured facts.
- **Approved:** the project owner accepts the business type, unique engineering, milestone placement, and any scope exception.

## Workflow

### 1. Define the business fantasy

State in one paragraph:

- what the player owns;
- what customers come to obtain;
- what the player physically does early;
- what employees and managers eventually control;
- what strategic decisions remain meaningful after automation.

Reject concepts whose core activity cannot support both first-person operation and later delegation without becoming a different game.

### 2. Define the operating loop

Describe one normal business cycle:

1. demand appears;
2. products, services, capacity, or equipment are prepared;
3. customers enter or request service;
4. employees or the player fulfill demand;
5. revenue and costs are recorded;
6. cleanliness, maintenance, inventory, satisfaction, and reputation change;
7. the player responds through labor, staffing, pricing, ordering, layout, marketing, or management.

Separate essential mechanics from decorative flavor.

### 3. Map the shared foundation

For each domain, identify the existing shared system, business-specific data, and any proposed extension:

- property and location;
- traffic, demographics, demand, and competition;
- staffing, scheduling, training, and management;
- pricing, products, inventory, service capacity, and promotion;
- cleanliness, maintenance, theft, safety, reputation, and satisfaction;
- revenue, cost of goods or service, wages, rent, utilities, debt, and cash flow;
- delegation and manager policy;
- construction, furnishing, equipment, and reusable layouts;
- persistence, reporting, and validation.

Do not label a new system as shared merely to avoid acknowledging unique engineering.

### 4. Bound uniqueness

Prefer no more than:

- one signature operational mechanic;
- one signature financial or operational risk;
- one distinctive demand or customer-flow pattern;
- a small set of unique interactions and animations.

Estimate the design against the current guideline:

- approximately 70% shared systems;
- approximately 20% business-specific data, content, and rules;
- no more than approximately 10% unique engineering without explicit justification.

Return `reuse-risk` when the proposal duplicates existing systems under new names. Return `scope-risk` when unique engineering would materially exceed the guideline.

### 5. Define detailed simulation

Specify what happens while the player is present:

- customer arrival and path;
- queue or browsing behavior;
- player and employee tasks;
- interactive equipment and usable sides;
- inventory or service-state changes;
- failures, complaints, theft, messes, or maintenance;
- feedback visible in the world and interface.

Do not require every simulated variable to receive a unique animation.

### 6. Define aggregate off-site simulation

Specify the minimum variables needed when the player is absent:

- local demand and traffic;
- opening hours and capacity;
- staffing coverage and productivity;
- manager policy and performance;
- inventory, ingredients, consumables, or machine availability;
- pricing and promotion;
- service quality, satisfaction, reputation, maintenance, and risk;
- revenue, expenses, loss, and exceptional events.

Explain how these variables approximate the detailed store without simulating individual NPCs.

### 7. Define mode transitions

State:

- what authoritative state persists across both modes;
- how an off-site location is materialized when visited;
- how detailed activity is summarized when the player leaves;
- how queues, partially completed services, stock, cash, equipment, employees, and incidents reconcile;
- which discontinuities or exploits require tests.

### 8. Trace the delegation ladder

For each repetitive activity, map:

1. player performs it;
2. employee performs it under assignment;
3. manager schedules and supervises it;
4. policy or headquarters support controls it across locations;
5. player retains a meaningful strategic choice.

Flag tasks that remain mandatory manual chores after the player has reached portfolio ownership.

### 9. Define economics and reporting

Identify:

- demand drivers;
- revenue units and average transaction or service value;
- variable costs;
- fixed costs;
- capacity bottlenecks;
- spoilage, shrinkage, downtime, or utilization losses;
- manager-controlled levers;
- player-visible explanations for profit or loss.

Use formulas only as provisional design models unless approved code or tuning tools already define them.

### 10. Identify content and production load

List:

- modular building and furnishing needs;
- products, ingredients, machines, displays, signage, and effects;
- shared and unique animations;
- employee specialist roles;
- customer archetypes and events;
- audio and interface needs;
- licensing or provenance risks.

Separate minimum viable content from expansion content.

### 11. Apply the scope gate

Classify the candidate as:

- `vertical-slice-candidate`
- `post-slice-core`
- `later-expansion`
- `prototype-only`
- `owner-decision-required`
- `reject`

Do not override the current scope document. Route exceptions to `$margins-vertical-slice-scope-gate` and the project owner.

## Output: Business Type Design Record

Return these sections:

1. identity and status;
2. business fantasy;
3. operating loop;
4. shared-system mapping;
5. unique mechanics and justification;
6. detailed simulation;
7. aggregate simulation;
8. mode-transition contract;
9. delegation ladder;
10. economy and P/L explainability;
11. location and customer profile;
12. asset, animation, UI, and audio load;
13. persistence and validation needs;
14. milestone disposition;
15. unresolved owner decisions;
16. risks, smallest prototype, and next artifact.

## Completion and failure

Complete only when the record distinguishes shared reuse, business-specific data, and genuinely unique engineering; defines both simulation modes; traces delegation; explains financial outcomes; and states its scope disposition.

Stop as `blocked` when required authority is missing, current documents conflict, the target milestone is unknown, or a proposed business depends on an unapproved core architecture.

Do not write implementation files, modify scope, or publish the design without separate permission.