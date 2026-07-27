---
name: margins-simulation-feature-integration-reviewer
description: Explicitly invoked architecture and design review for a Margins feature that affects simulation state, employees, economy, persistence, reporting, or the transition between detailed first-person locations and aggregate off-site businesses. Use on feature specifications, implementation plans, or pull requests before integration. Do not use for isolated art feedback, minor copy changes, ordinary balance tuning, or as permission to implement or merge.
---

# Margins Simulation Feature Integration Reviewer

Review one feature across the complete Margins simulation lifecycle so it behaves consistently while the player is present, while the business is off-site, under employee delegation, in financial reporting, and after save/load.

## Authority and inputs

Require explicit invocation as `$margins-simulation-feature-integration-reviewer`.

Before review:

1. Read current approved decisions, project brief, design pillars, and scope boundaries.
2. Read relevant documents in `03_CORE_SYSTEMS`, `DESIGNS`, `DATA`, `CODE`, and `02_VERTICAL_SLICE`.
3. Inspect current schemas, save contracts, tests, and adjacent features.
4. Identify whether the target is a concept, specification, plan, code change, data change, or mixed artifact.
5. Resolve repository base/head and inspect the exact changed paths when reviewing implementation.

Discover facts from the repository before asking. Do not assume a proposed field, system, engine API, or service already exists.

## Ownership boundary

- **Owned:** identify integration gaps, contradictions, missing contracts, unhandled transitions, validation needs, and required revisions.
- **Recommended:** architecture boundaries, simplifications, test scenarios, data ownership, UI explanation, and rollout order.
- **Prohibited:** approve project scope exceptions, select unapproved technology, invent canonical schemas, implement fixes, merge changes, or accept unsupported evidence.
- **Approved:** system owners and the project owner accept architecture, schema, scope, persistence, migration, and release decisions.

## Review statuses

Return one primary status:

- `approve`
- `approve_with_conditions`
- `revise`
- `blocked`
- `not_applicable`

Use `approve` only when all applicable contracts are explicit and no critical gap remains. Conditions must be objective and verifiable.

## Workflow

### 1. Normalize the feature

State:

- player-facing purpose;
- authoritative state introduced or changed;
- producers and consumers of that state;
- affected business types, locations, employees, customers, managers, and headquarters systems;
- affected economy, UI, persistence, navigation, construction, and time systems;
- target milestone and scope status.

Separate required behavior from proposed implementation.

### 2. Identify state ownership

For every new or modified value, determine:

- canonical owner;
- data type and lifecycle;
- valid states and transitions;
- who may mutate it;
- update frequency;
- derived versus persisted status;
- business-level, location-level, entity-level, or global scope;
- deterministic inputs or random influences;
- validation and error behavior.

Flag duplicate sources of truth and circular ownership.

### 3. Review detailed first-person simulation

When the player is present, define:

- instantiated entities and interactions;
- task assignment and execution;
- customer arrival, queueing, browsing, service, and departure;
- equipment, inventory, capacity, cash, cleanliness, maintenance, satisfaction, and incidents;
- time progression and update order;
- feedback through animation, audio, world state, alerts, and reports;
- failure behavior when paths, inventory, staff, or equipment are unavailable.

Do not require high-fidelity simulation when a simpler state model provides the same player decision.

### 4. Review aggregate off-site simulation

When the player is absent, define:

- retained authoritative variables;
- aggregation interval and update triggers;
- demand, capacity, staffing, manager, inventory, maintenance, satisfaction, and financial calculations;
- exceptional events and limits on random outcomes;
- performance expectations;
- information available to the player remotely;
- behavior when the business lacks required data or management coverage.

Flag any dependency on persistent individual NPC simulation without explicit justification.

### 5. Review mode transitions and reconciliation

Check both directions:

**Aggregate to detailed**

- materialization of employees, customers, inventory, equipment, queues, cash, and incidents;
- distribution of aggregate totals into plausible detailed state;
- prevention of duplicated sales, stock, wages, or events;
- continuity of time and partially completed work.

**Detailed to aggregate**

- summarization of active entities and tasks;
- persistence of exact state that affects future decisions;
- cancellation, completion, or abstraction of temporary actions;
- capture of unresolved incidents and maintenance;
- handoff timing and atomicity.

Require tests for rapid entry/exit, save during transition, time acceleration, and repeated visits when applicable.

### 6. Review delegation and management

Trace the feature through:

1. direct player control;
2. employee assignment;
3. manager scheduling, prioritization, and policy;
4. remote oversight and exception reporting;
5. headquarters or portfolio-wide policy when applicable.

Confirm automation removes labor without removing all meaningful strategy. Flag features that bypass employee skill, manager quality, staffing coverage, or established authority.

### 7. Review economy and reporting

Identify:

- revenue and expense effects;
- timing of recognition;
- inventory, service, utility, wage, rent, maintenance, shrinkage, and financing implications;
- tunable versus derived values;
- location and portfolio aggregation;
- causal explanations shown to the player;
- potential exploits, feedback loops, runaway growth, or hidden penalties.

A financial outcome must be traceable to understandable causes even when the underlying simulation is deep.

### 8. Review persistence and migration

Determine:

- what must be saved;
- stable identities and references;
- restoration order and dependencies;
- versioning and migration needs;
- default handling for older saves;
- recovery from missing, invalid, or partial data;
- whether derived values should be recomputed;
- validation fixtures and round-trip tests.

Stop when implementation invents unapproved save fields or breaks an existing contract without a migration decision.

### 9. Review UI and player comprehension

Confirm the player can understand:

- current state;
- cause of success or failure;
- actions available now;
- what employees or managers are doing;
- what changed while absent;
- which report level owns the information;
- whether an alert requires intervention or is merely informational.

Avoid exposing raw simulation complexity without a player decision attached.

### 10. Review performance and scaling

Evaluate:

- per-entity and per-location update cost;
- detailed versus aggregate workload;
- frequency of recalculation;
- pathfinding or navigation rebuilds;
- event fan-out and cross-system queries;
- number of businesses, employees, customers, products, and placed objects affected;
- caching, batching, dirty-state, or event-driven alternatives;
- worst-case city and portfolio behavior.

Treat performance estimates as hypotheses until measured.

### 11. Review failure and validation scenarios

Include applicable cases:

- no customers, no staff, no manager, no inventory, or no cash;
- closed business, invalid schedule, blocked navigation, broken equipment, or full capacity;
- player leaves or arrives during an active transaction;
- time skip or offline update;
- save/load during normal and exceptional states;
- manager policy conflicts with local conditions;
- detailed and aggregate results drift;
- location closes, changes ownership, or changes business type;
- missing data, denied capability, or stale schema;
- proposed feature is deferred by scope authority.

## Output: Integration Review Record

Return:

1. target identity, repository revisions, and primary status;
2. feature and state summary;
3. authority and scope findings;
4. state ownership map;
5. detailed simulation findings;
6. aggregate simulation findings;
7. transition and reconciliation findings;
8. delegation and management findings;
9. economy and report findings;
10. persistence and migration findings;
11. UI and explainability findings;
12. performance and scaling findings;
13. required validation scenarios;
14. critical blockers;
15. objective approval conditions;
16. recommended next owner and artifact.

## Critical blockers

Return `revise` or `blocked` when any applicable condition exists:

- two authoritative sources own the same state;
- detailed and aggregate modes can duplicate, lose, or materially change outcomes without explanation;
- a feature cannot be delegated despite belonging to scalable business operation;
- financial effects cannot be explained to the player;
- persistence or migration is undefined for durable state;
- the implementation assumes an unapproved schema, engine, plugin, service, or scope exception;
- required evidence or repository context is unavailable.

## Completion and failure

Complete only when every applicable simulation layer has a disposition and approval conditions are testable.

Do not modify code, schemas, data, scope, or project authority, and do not merge or publish, without separate permission.