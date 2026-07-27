# Margins Technical Foundation Direction

## Status and authority

- **Status:** Current pre-production technical direction synchronized to the approved foundational decisions
- **Authority:** This document defines requirements and decision boundaries only. It does not select an engine, language, dependency, rendering stack, or architecture baseline.

## Production context

The technical foundation must support a solo-developed PC game with:

- less than **$1,000** in pre-revenue development spending;
- approximately **20–30 direct human development hours per week**;
- extensive agentic AI assistance;
- a durable vertical slice that can become the commercial-game foundation; and
- later expansion only when evidence, reuse, and production capacity justify it.

## Engine-selection status

Margins remains engine-neutral.

Engine and language selection must be performed through a deliberate evaluation that considers:

- the approved first-person and management gameplay requirements;
- detailed simulation while the player is present;
- aggregate off-site simulation while absent;
- persistence, save migration, and restore safety;
- structured data, schemas, validation, and internal content tools;
- modular environment, character, product, and business content;
- UI and reporting needs across store, location, company, property, and portfolio scales;
- PC performance and debugging quality;
- asset availability and compatibility with Stylized Contemporary Americana;
- licensing, dependency, and maintenance costs;
- AI-agent compatibility, testability, and automation;
- solo-development learning and iteration cost; and
- reversibility or migration risk.

No technology should be selected because it is fashionable, familiar to an assistant, or convenient for one prototype while failing the long-term project requirements.

## Required technical capabilities

The eventual foundation must be able to support:

- first-person interaction and reliable tactile-but-assisted object handling;
- grid-based interior placement and saved layouts;
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
- later property, finance, competition, and additional-business expansion without assuming those systems belong in the vertical slice.

## Risk-prototype requirement

Before an engine or architecture baseline is approved, the technical evaluation should identify and prototype the assumptions most likely to invalidate a candidate. At minimum, evaluation must account for:

- tactile stocking, snapping, and checkout interaction;
- customer and employee navigation in a furnished store;
- transition between detailed and aggregate business simulation;
- save and restore of layouts plus durable business state;
- data-driven content loading and validation;
- portfolio reporting across at least two locations; and
- performance and debugging visibility suitable for solo development.

This requirement does not prescribe the number, order, or implementation of prototypes; those belong to the Technical Architect and Producer workflows and require project-owner approval.

## Data and modding boundary

Margins is data-driven from the beginning but not mod-platform-first.

- Structured validated data should support safe human and agent-assisted content production.
- Runtime logic and permanent invariants remain enforced in code.
- Internal authoring and validation tools come before public mod tools.
- Public formats, editors, compatibility guarantees, and Workshop integration are not committed.

## Traversal boundary

The vertical slice is primarily on foot within one compact block. Early separated-district travel may be abstracted or transitional. Drivable vehicles require later technical and scope prototypes and are not an unconditional architecture requirement.

## Explicitly unresolved

- engine and language;
- rendering and physics approach;
- scene, entity, or data architecture;
- detailed aggregate-simulation formulas and update cadence;
- save format and migration strategy;
- networking or multiplayer architecture, because multiplayer is outside current scope;
- performance budgets and target hardware;
- build, deployment, telemetry, and crash-reporting tools;
- public mod architecture;
- vehicle implementation;
- platform services and storefront integrations;
- exact internal editor and validation tooling.

All recommendations must remain proposals until approved and recorded through the repository authority hierarchy.