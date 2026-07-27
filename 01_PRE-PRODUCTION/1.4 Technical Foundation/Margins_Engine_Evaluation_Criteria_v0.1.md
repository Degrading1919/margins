# Margins Engine Evaluation Criteria v0.1

**Status:** Proposed evaluation framework; engine-neutral; not an engine decision<br>
**Authority:** Derived from approved foundational decisions and current pre-production direction. This document is subordinate to the repository authority hierarchy and requires project-owner approval before it governs engine selection.<br>
**Decision owner:** Project owner<br>
**Last evidence review:** 2026-07-27

## Purpose

Define how Margins will compare game engines without turning documented features, familiarity, or preliminary scoring into a selection. The criteria are specific to the approved PC-only, solo-developed convenience-store vertical slice and its detailed/aggregate simulation boundary.

This file does **not** select or recommend an engine.

## Governing constraints

The evaluation must preserve:

- PC-only development; less than $1,000 total pre-revenue spending unless the owner approves a change; approximately 20–30 direct human development hours per week; extensive agentic assistance; solo-production practicality.
- A standalone convenience-store slice with tactile first-person receiving, box/product handling, shelf snapping, scanning/checkout, cleaning, maintenance, and fixture placement.
- Furnished-store navigation for instantiated nearby customers and persistent employees, including at least two worker roles and one manager role.
- Detailed simulation at the player's location, aggregate simulation off-site, reliable transitions, two locations with different market conditions, and actionable portfolio reporting.
- Structured and validated data, reproducible tests, save/restore and migration control, modular Stylized Contemporary Americana content, and long-term extension without implementing deferred systems early.
- No current requirement for multiplayer, drivable vehicles, public modding, public markets, or deep autonomous rival-company AI.

## Hard disqualifiers

A disqualifier overrides every weighted score. It may be applied only from reproducible evidence, current binding terms, or a failed comparative prototype—not from reputation or feature-list absence.

| ID | Disqualifier | Required evidence |
|---|---|---|
| D1 | A legal, distributable build cannot be produced for the project-owner-designated desktop PC operating system on the evaluated supported release. | Current platform/export terms plus a failed clean-environment build/export check. The target PC operating system is not yet approved. |
| D2 | Mandatory engine/tool spending before revenue makes the approved sub-$1,000 total budget infeasible. | Current binding terms and an itemized minimum-cost calculation; optional assets and hypothetical future revenue do not count as mandatory pre-revenue cost. |
| D3 | Required project state cannot be loaded, validated, saved, restored, versioned, and migrated without custom-engine work incompatible with solo production. | Prototype failure after one bounded remediation attempt, with time and custom work recorded. |
| D4 | A required tactile, furnished-navigation, or detailed/aggregate transition test cannot meet its pass conditions on representative content. | Repeatable failure on the supported release after one bounded remediation attempt. |
| D5 | A clean checkout cannot be reproduced, reviewed, merged, reverted, built, and diagnosed using a documented source-control workflow. | Repository exercise including representative scene/content conflicts and a clean-environment build. Binary assets alone are not a disqualifier. |
| D6 | The engine cannot expose sufficient logs, automated checks, and CPU/GPU/memory evidence to diagnose the required prototypes. | Failed workflow package with captured missing evidence and attempted supported tooling. |
| D7 | The evaluated release has no credible maintenance path for the planned PC development period, or a required dependency creates an equivalent abandonment risk. | Official support/release evidence and a dependency inventory. |
| D8 | Comparable evidence cannot be reached within the owner-approved per-candidate human-effort ceiling. | Time log against the same prototype specification. The plan estimate is not this ceiling; the owner must set one separately before D8 can be applied. |
| D9 | Binding license or service terms make the required agent-assisted workflow unavailable under owner-approved data-use controls, with no compliant alternative inside the budget and effort constraints. | Current binding engine and agent-provider terms, the exact data/code exposure path, and owner or qualified legal review when material. |

## Weighted criteria

| Criterion | Weight | Why it carries this weight for Margins |
|---|---:|---|
| C1. Tactile first-person and placement workflow | 18% | Stocking, box handling, snapping, scanning, cleaning, maintenance, and fixture placement are the slice's immediate player-facing proof. |
| C2. Furnished navigation and simulation-boundary fit | 16% | Customers and workers must navigate changing interiors while location state moves reliably between detailed and aggregate execution. |
| C3. Data, validation, persistence, and migration control | 16% | Products, fixtures, staff, locations, layouts, portfolio state, and tuning must remain structured, valid, saveable, and evolvable. |
| C4. Solo iteration, testing, diagnostics, and PC delivery | 15% | A 20–30-hour human week requires short feedback loops, reproducible automation, useful profiling, and dependable builds. This excludes source-control/agent review evidence scored in C7. |
| C5. Management UI and reporting workflow | 10% | Management decisions require dense but actionable interfaces and two-location reports alongside first-person play. |
| C6. Modular stylized art and asset pipeline | 9% | The slice needs reusable environments, products, fixtures, UI, and modest-complexity characters consistent with the approved references. |
| C7. Agent-assisted and source-control workflow | 8% | Extensive AI assistance is useful only when changes are bounded, inspectable, mergeable, recoverable, and accepted through human review. Test/build quality remains C4 evidence. |
| C8. Licensing, mandatory cost, and support/dependency durability | 8% | The engine must fit the pre-revenue cap and retain a support and dependency path that a solo project can afford. Project-change recovery is scored in C7, not here. |
| **Total** | **100%** | |

## Scoring and confidence

Use a 0–4 scale per criterion:

| Score | Meaning |
|---:|---|
| 0 | Demonstrated failure or infeasibility; check for a hard disqualifier. |
| 1 | Demonstrated material gap, high custom burden, or serious workflow risk. |
| 2 | Plausible/documented capability, but material Margins-specific evidence is incomplete. |
| 3 | Comparative prototype passes with manageable burden and no material unresolved defect. |
| 4 | Comparative prototype passes strongly, repeatably, and with lower measured burden than the alternatives. |
| U | Unscored: evidence is missing or conflicting. U is not zero, neutral, or positive. |

- All composite criteria, including C8, remain U or no higher than 2 before their complete evidence bundle is available. A verified license term may support C8, but cannot represent its untested maintenance and dependency portions.
- A documented feature earns, at most, evidence that a test is plausible; it does not prove Margins suitability.
- Calculate a weighted total only after every non-disqualified candidate has comparable evidence for all eight criteria: `sum(weight × score ÷ 4)`.
- Record confidence separately: **Low** (documentation or inference only), **Medium** (one representative execution), or **High** (repeatable result plus clean checkout/build). Never increase a score merely because confidence is low.
- A difference smaller than the evidence uncertainty is a tie, not a ranking. Prototype results must be able to reverse any preliminary order.
- Assign each observation to one primary criterion before scoring. It may support another criterion's narrative but contributes numerically only once: iteration/test/build evidence to C4; agent review/merge/recovery to C7; license/cost/support/dependency evidence to C8.

## Evidence rules

| Evidence state | Treatment |
|---|---|
| Current official documentation or binding terms | Label **verified fact**, cite near the claim, and date unstable facts. |
| Margins-specific conclusion derived from verified facts | Label **inference** and state the dependency. |
| Expected behavior not yet executed | Label **hypothesis** or **prototype question**; score no higher than 2. |
| Missing evidence | Mark U, reduce confidence, and assign an owner or prototype step. Do not substitute a midpoint. |
| Conflicting official evidence | Record both sources and applicable versions/terms; leave unresolved until the orchestrator rechecks or the owner accepts the risk. |
| Community report | Use only when primary evidence is unavailable; label anecdotal and never use it alone for elimination. |

Evidence must be version-specific where behavior can change. Effort, learning burden, asset fit, merge behavior, and generated-code quality require direct measurement. A candidate may not hide a fatal weakness behind its aggregate score.

## Requirement traceability

| Criteria | Repository authority |
|---|---|
| C1 | `Margins_Foundational_Decisions_v1.0.md` FD-004, FD-016; `Margins_Project_Brief_v0.1.md`; `Margins_Design_Pillars_v0.1.md`; `Margins_Technical_Foundation_Direction_v0.1.md` |
| C2 | Foundational Decisions FD-004, FD-015; `Margins_Initial_Scope_Boundaries_v0.1.md`; Technical Foundation Direction |
| C3 | Foundational Decisions FD-004, FD-028; Technical Foundation Direction; `Margins_Economy_and_Progression_Direction_v0.1.md` |
| C4 | Foundational Decisions FD-001, FD-002, FD-003, FD-009, FD-010; Technical Foundation Direction |
| C5 | Foundational Decision FD-004; Project Brief; Design Pillars; Technical Foundation Direction |
| C6 | Foundational Decisions FD-004, FD-011; `Margins_Content_and_Commercial_Strategy_v0.1.md`; `Margins_Art_Audio_and_Presentation_Direction_v0.1.md` |
| C7 | Foundational Decisions FD-003, FD-028; Technical Foundation Direction; `Margins_Assistant_Roles.md` |
| C8 | Foundational Decisions FD-001, FD-002, FD-010; Technical Foundation Direction; `Margins_Repository_Structure.md` |

The approved decisions remain controlling. The merged `Margins_Master_Roadmap_v0.1.md` is a proposed planning baseline only and is not a source of new requirements.

## Unresolved project-owner choices

Before final scoring, the owner must resolve or bound:

- target development and minimum player hardware, the desktop PC operating system(s), performance budgets, and acceptable build/package size;
- maximum direct human effort per candidate and acceptable editor/build iteration time;
- candidate release lane at prototype start (supported stable/LTS versus current update);
- prototype implementation lane per candidate (including Unreal Blueprint/C++ and Godot GDScript/C# boundaries) and how prior experience will be normalized;
- how much of the sub-$1,000 cap may fund engine seats, plugins, assets, or specialist tooling;
- the developing individual/entity and trailing-12-month **Total Finances** relevant to Unity tier eligibility;
- whether engine source access is required or only an optional recovery path;
- acceptable binary-asset and locking workflow versus a stronger preference for text-reviewable changes;
- acceptable reliance on third-party testing, validation, save, navigation, or content-pipeline dependencies;
- approved agent providers, retention/training settings, and what licensed engine code/content may be exposed to them;
- minimum support horizon and migration tolerance.

These are decision inputs, not delegated approvals. The Technical Architect may recommend; only the project owner selects the engine or changes the constraints.
