# Margins Technical Foundation Direction

## Status and authority

- **Status:** Current pre-production technical direction synchronized to the approved foundational and Unity engine-selection decisions
- **Authority:** Unity is approved as the production engine. This document defines current technical requirements and decision boundaries but does not approve the Unity version, render pipeline, packages, dependencies, project structure, or architecture baseline.

## Production context

The technical foundation must support a solo-developed PC game with:

- less than **$1,000** in pre-revenue development spending;
- approximately **20–30 direct human development hours per week**;
- extensive agentic AI assistance;
- a durable vertical slice that can become the commercial-game foundation; and
- later expansion only when evidence, reuse, and production capacity justify it.

## Engine-selection status

**Unity is the approved production engine for Margins.**

The engine-evaluation package added by PR #7 is retained as historical research. Its unresolved shortlist and proposed three-engine comparison are superseded by `00_ADMIN/Decisions/Margins_Engine_Selection_Decision_v1.0.md`.

- Unreal Engine and Godot are not active implementation targets.
- No parallel multi-engine prototype program is authorized.
- Engine selection may be reopened only by project-owner decision after a concrete Unity blocker, licensing incompatibility, or unacceptable production burden is demonstrated.

## Required Unity baseline decision

Before creating the durable Unity project, the project must approve a small implementation baseline covering:

- Unity editor version and support lane;
- rendering pipeline;
- scripting and visual-scripting boundaries;
- input package;
- navigation package;
- UI framework;
- test framework and execution method;
- data and validation approach;
- save and serialization approach;
- source-control and large-file rules;
- project folders, assemblies, namespaces, and dependency policy;
- target desktop operating system and provisional hardware assumptions;
- agent-assisted code-generation and review boundaries.

This should be one compact decision package, not another broad engine comparison.

## Required technical capabilities

The Unity foundation must be able to support:

- first-person interaction and reliable tactile-but-assisted object handling;
- deterministic shelf snapping and grid-based interior placement;
- persistent employees and managers;
- nearby instantiated customers driven by aggregate market demand;
- transitions between detailed local simulation and aggregate off-site simulation;
- multi-location business state and portfolio reporting;
- data-driven products, business definitions, properties, customer contexts, events, and tuning;
- code-enforced permanent invariants;
- deterministic or explainable state transitions where practical;
- reproducible validation scenarios and diagnostics;
- versioned save formats and migration planning;
- modular handcrafted city content;
- later property, finance, competition, and additional-business expansion without implementing those systems in the vertical slice.

## Immediate Unity foundation spike

After the Unity baseline is approved and the project owner has computer access, create the smallest executable proof that Unity can support the initial workflow.

The spike should contain only:

1. a clean Unity project committed through the approved repository workflow;
2. a graybox store room;
3. first-person movement and look;
4. one data-defined product;
5. product pickup, release, and deterministic shelf snapping;
6. valid and invalid placement feedback;
7. save and reload of the product's snapped state;
8. one placeholder navigation agent moving between fixed points; and
9. a runnable desktop PC build.

### Spike code standard

- Write only the code required to prove the listed behavior.
- Prefer small, named components over generic manager layers.
- Add only logging and assertions needed for simple diagnosis.
- Do not introduce frameworks, service locators, dependency-injection systems, generalized event buses, databases, production economy logic, or speculative abstractions.
- Keep generated changes reviewable and testable.
- Do not count lines of code or generated volume as progress.

### Spike exit evidence

- a fresh repository checkout opens with documented prerequisites;
- the project runs without unresolved errors;
- one product can be picked up and snapped only to valid shelf targets;
- product identity and placement state survive save and reload;
- the placeholder agent reaches its fixed destinations without crossing solid fixtures;
- a desktop build launches successfully;
- the project owner finds the editor and iteration workflow acceptable enough to continue.

Failure of the spike does not automatically reopen engine selection. First identify whether the problem is a Unity limitation, implementation defect, package choice, or unresolved baseline decision.

## Data and modding boundary

Margins is data-driven from the beginning but not mod-platform-first.

- Structured validated data should support safe human and agent-assisted content production.
- Runtime logic and permanent invariants remain enforced in code.
- Internal authoring and validation tools come before public mod tools.
- Public formats, editors, compatibility guarantees, and Workshop integration are not committed.

## Traversal boundary

The vertical slice is primarily on foot within one compact block. Early separated-district travel may be abstracted or transitional. Drivable vehicles require later technical and scope prototypes and are not an unconditional architecture requirement.

## Explicitly unresolved

- Unity editor version and support lane;
- rendering pipeline and physics configuration;
- scripting and visual-scripting boundaries;
- package and dependency baseline;
- scene, entity, data, and assembly architecture;
- detailed aggregate-simulation formulas and update cadence;
- save format and migration strategy;
- target desktop operating system, performance budgets, and target hardware;
- build, deployment, telemetry, and crash-reporting tools;
- public mod architecture;
- vehicle implementation;
- platform services and storefront integrations;
- exact internal editor and validation tooling.

All recommendations remain proposals until approved and recorded through the repository authority hierarchy.
