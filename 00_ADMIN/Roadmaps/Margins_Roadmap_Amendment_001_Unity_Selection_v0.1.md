# Margins Roadmap Amendment 001 — Unity Selection v0.1

## Status and authority

- **Status:** Current engine-selection amendment to `Margins_Master_Roadmap_v0.1.md`
- **Effective date:** July 27, 2026
- **Progress synchronized:** August 6, 2026
- **Authority:** Applies `00_ADMIN/Decisions/Margins_Engine_Selection_Decision_v1.0.md` and `00_ADMIN/Decisions/Margins_Unity_Foundation_Baseline_Decision_v1.0.md` to the roadmap baseline.
- **Scope:** This amendment changes the engine-selection path only. It does not approve a new full-project schedule.

## Reason for amendment

The master roadmap originally assumed that engine selection would require a formal multi-candidate prototype phase. The project owner selected Unity and rejected the proposed 230–390-hour three-engine comparison as disproportionate to the decision.

The roadmap therefore stopped treating engine comparison as the critical path.

## Superseded roadmap direction

The following roadmap direction is superseded:

- continued engine-neutral execution;
- an Unreal Engine, Unity, and Godot comparative implementation program;
- selection only after equivalent deep prototypes in all surviving candidates;
- the previous Stage 1 and Stage 2 duration assumptions where they depend on multi-engine comparison;
- any schedule forecast that assumes several months must be spent before Unity production work begins.

The research package remains historical evidence, but its comparison plan is not authorized work.

## Current technical sequence

### Amended Stage 1 — Unity decision and repository synchronization

**Status:** Complete

Completed evidence:

- [x] Unity recorded as the approved engine
- [x] Project brief, scope boundaries, and technical direction synchronized
- [x] Engine-evaluation package explicitly superseded as an execution plan
- [x] Unresolved Unity baseline choices clearly listed before selection

### Amended Stage 2 — Unity technical baseline

**Status:** Complete

Approved outputs:

- [x] `00_ADMIN/Decisions/Margins_Unity_Foundation_Baseline_Decision_v1.0.md`
- [x] `01_PRE-PRODUCTION/1.4 Technical Foundation/Margins_Unity_Technical_Baseline_v0.1.md`
- [x] `01_PRE-PRODUCTION/1.4 Technical Foundation/Margins_Unity_Bootstrap_Standard_v0.1.md`
- [x] `02_VERTICAL_SLICE/Business Prototype/Margins_Unity_First_Foundation_Spike_v0.1.md`
- [x] `02_VERTICAL_SLICE/Business Prototype/Margins_Unity_First_Foundation_Spike_Agent_Prompt.md`

This stage approved only the decisions required to create and evaluate the first clean Unity project. It did not establish full production architecture by itself.

### Amended Stage 3 — Unity foundation spike

**Status:** Complete

Completed proof:

- [x] First-person movement and mouse look
- [x] One data-defined product
- [x] Product pickup, rotation, and deterministic shelf snapping
- [x] Valid and invalid placement feedback
- [x] Save and restore of product placement
- [x] One placeholder navigation agent in a graybox store
- [x] Reproducible repository project structure
- [x] Focused EditMode and PlayMode tests
- [x] Runnable Windows x64 build
- [x] No project-blocking Unity limitation identified

PR #12 merged this foundation spike. Later merged work expanded it into the first-store interaction, customer, employee, persistence, delegation, reporting, and reusable business-operation systems now reflected in the master roadmap progress checklist.

### Stage 3 exit gate

**Passed for continued Unity development:**

- the project opens, tests, and builds reproducibly;
- generated and human-reviewed changes remain understandable;
- the interaction and navigation foundation works without a project-blocking Unity limitation;
- the project owner continued development and merged the foundation into `main`.

Continued playtesting may reveal defects or feel problems, but those are handled through subsequent fixes rather than by reopening engine selection without a concrete Unity blocker.

## Schedule disposition

The dates in the original `Margins_Master_Roadmap_v0.1.md` remain historical proposals and were already identified as substantially inaccurate. The August 6 progress synchronization records completed work but does not establish a new full-project forecast.

Do not issue a new release schedule without recording:

- actual implementation and review hours where available;
- local test and build velocity;
- owner playtest and bug-fix burden;
- asset-production and integration throughput;
- remaining Stage 5–8 dependencies;
- revised aggressive, planning, and conservative scenarios;
- project-owner approval.

## Current disposition

The Unity selection and foundation-spike sequence is complete. Current execution should continue from the merged first-store and portfolio foundation, close remaining gameplay gaps, integrate production presentation, and perform bounded playtest-driven fixes. Engine reconsideration requires a concrete project-blocking Unity limitation or a new project-owner decision.
