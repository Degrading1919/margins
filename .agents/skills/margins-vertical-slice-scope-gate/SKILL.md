---
name: margins-vertical-slice-scope-gate
description: Explicitly invoked review workflow for classifying proposed Margins features, documents, prototypes, or implementation work against the approved vertical-slice commitments and deferred list. Use before adding work to the first playable milestone or when scope status is disputed. Do not use to rewrite the project vision, approve exceptions, estimate schedules as facts, or evaluate post-slice priorities unrelated to the vertical slice.
---

# Margins Vertical Slice Scope Gate

Determine whether proposed work belongs in the current vertical slice and identify the smallest acceptable proof without silently expanding the milestone.

## Authority and inputs

Require explicit invocation as `$margins-vertical-slice-scope-gate`.

Read, in order:

1. current approved decisions in `00_ADMIN/Decisions`;
2. `01_PRE-PRODUCTION/1.3 Feature Set & Scope/Margins_Initial_Scope_Boundaries_v0.1.md`;
3. the current project brief and design pillars;
4. relevant vertical-slice designs, dependencies, and implementation plans;
5. the exact proposal, changed paths, or requested outcome under review.

Record the repository head and revisions of the authority files used. Discover proposal details from the repository before asking.

## Ownership boundary

- **Owned:** classify the proposal, identify scope multipliers, state the smallest in-scope version, and expose owner decisions.
- **Recommended:** sequencing, reduction, substitution, prototype boundaries, and deferral rationale.
- **Prohibited:** alter the scope document, approve an exception, commit resources, choose an engine or architecture, or reject long-term ideas solely because they are deferred.
- **Approved:** the project owner accepts exceptions, milestone expansion, substitutions that change the proof objective, and permanent removal from the long-term vision.

## Status vocabulary

Return exactly one primary disposition:

- `in_scope`
- `supporting_prerequisite`
- `prototype_only`
- `defer`
- `reject`
- `owner_decision_required`
- `blocked`

Definitions:

- **in_scope:** directly satisfies a current vertical-slice commitment at appropriate fidelity.
- **supporting_prerequisite:** necessary for an in-scope commitment but not itself a player-facing proof objective.
- **prototype_only:** a bounded experiment may reduce risk, but production integration is not currently committed.
- **defer:** valuable to the long-term game but unnecessary for the current proof.
- **reject:** conflicts with the project direction, duplicates another mechanism without benefit, or cannot justify its cost even outside the slice.
- **owner_decision_required:** changes scope, substitutes a commitment, or depends on a creative/technical decision reserved to the owner.
- **blocked:** evidence or authority required for classification is unavailable or contradictory.

## Workflow

### 1. Normalize the proposal

State:

- requested outcome;
- player-visible behavior;
- systems, data, content, tools, and assets affected;
- intended milestone;
- claimed necessity;
- explicit and hidden dependencies;
- requested writes or implementation effects.

Separate the enduring feature idea from the amount of work proposed now.

### 2. Map to vertical-slice commitments

For every current commitment, mark the proposal as:

- directly proves;
- enables;
- unrelated;
- weakens;
- replaces.

A proposal is not in scope merely because it may appear in the final game.

### 3. Check the deferred list

Identify any deferred feature directly or indirectly introduced by the proposal, including:

- a second complete business category;
- multi-story player construction;
- runtime procedural generation of the full city;
- mixed-use development;
- detailed headquarters departments;
- advanced franchising;
- multiplayer;
- broad economic cycles or stock markets;
- deep competitor AI;
- universal building interiors;
- fully persistent crowds.

A small risk-reduction experiment may be `prototype_only`; it does not convert the deferred feature into a commitment.

### 4. Detect scope multipliers

Check whether the proposal creates:

- a new core system instead of extending a shared one;
- business-specific architecture that should be data-driven;
- unique art, animation, UI, audio, save, or AI pipelines;
- detailed simulation requirements for off-screen locations;
- new persistence or migration obligations;
- unapproved engine, plugin, service, networking, or platform dependencies;
- broad tools or editor work whose only consumer is deferred;
- combinatorial testing across businesses, districts, floors, or management layers.

Distinguish immediate implementation cost from permanent maintenance cost.

### 5. Test necessity

Ask:

- Does the vertical slice fail to prove its core progression without this?
- Can an existing shared system provide the proof?
- Can a fixed, authored, simplified, or data-only substitute answer the risk?
- Is the proposal solving a current problem or preparing for hypothetical future scale?
- Can the uncertainty be resolved with a disposable prototype or design record first?

Reject “we will need it eventually” as sufficient justification for current commitment.

### 6. Define the smallest acceptable slice

When the idea has value, provide the minimum version that answers the current question. Specify:

- included behavior;
- excluded behavior;
- temporary assumptions;
- success evidence;
- throwaway versus production intent;
- invalidation condition;
- follow-on gate before expansion.

Do not disguise a production foundation as a prototype when it creates permanent dependencies.

### 7. Classify and route

Return the primary disposition and any secondary flags:

- `scope_multiplier`
- `technical_authority_missing`
- `creative_owner_decision`
- `schema_required`
- `performance_risk`
- `persistence_risk`
- `content_pipeline_risk`
- `rights_or_license_risk`

Name the exact owner and unblock action for every required decision.

## Output: Scope Gate Record

Return:

1. proposal identity and repository revision;
2. normalized proposal;
3. primary disposition;
4. commitments proved or enabled;
5. deferred features touched;
6. direct and hidden dependencies;
7. scope multipliers and long-term maintenance burden;
8. smallest acceptable version;
9. excluded work;
10. evidence required for completion;
11. owner decisions and unblock actions;
12. recommended destination, milestone, and next artifact.

## Completion and failure

Complete only when the disposition is traceable to current authority and the record distinguishes the long-term value of an idea from its present milestone status.

Stop as `blocked` when scope authority conflicts, the proposal cannot be identified, or a missing owner decision prevents truthful classification.

Do not edit scope authority, implement the proposal, or publish the decision without separate permission.