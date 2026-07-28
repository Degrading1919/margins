# Margins Roadmap Amendment 001 — Unity Selection v0.1

## Status and authority

- **Status:** Current amendment to `Margins_Master_Roadmap_v0.1.md`
- **Effective date:** July 27, 2026
- **Authority:** Applies `00_ADMIN/Decisions/Margins_Engine_Selection_Decision_v1.0.md` and `00_ADMIN/Decisions/Margins_Unity_Foundation_Baseline_Decision_v1.0.md` to the proposed roadmap baseline.
- **Scope:** This amendment changes the engine-selection path only. It does not approve a new full-project schedule.

## Reason for amendment

The master roadmap assumed that engine selection would require a formal multi-candidate prototype phase. The project owner has now selected Unity and rejected the proposed 230–390-hour three-engine comparison as disproportionate to the decision.

The roadmap must therefore stop treating engine comparison as the critical path.

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

Exit evidence:

- Unity recorded as the approved engine;
- current project brief, scope boundaries, and technical direction synchronized;
- the engine-evaluation package explicitly superseded as an execution plan;
- unresolved Unity baseline choices clearly listed.

### Amended Stage 2 — Unity technical baseline

**Status:** Complete

**Computer access required:** No

Approved outputs:

- `00_ADMIN/Decisions/Margins_Unity_Foundation_Baseline_Decision_v1.0.md`;
- `01_PRE-PRODUCTION/1.4 Technical Foundation/Margins_Unity_Technical_Baseline_v0.1.md`;
- `01_PRE-PRODUCTION/1.4 Technical Foundation/Margins_Unity_Bootstrap_Standard_v0.1.md`;
- `02_VERTICAL_SLICE/Business Prototype/Margins_Unity_First_Foundation_Spike_v0.1.md`; and
- `02_VERTICAL_SLICE/Business Prototype/Margins_Unity_First_Foundation_Spike_Agent_Prompt.md`.

This stage approves only the decisions required to create and evaluate the first clean Unity project. It does not establish full production architecture.

### Amended Stage 3 — Unity foundation spike

**Status:** Ready after required local installation and licensing confirmation

**Computer access required:** Yes

Create the smallest runnable Unity project proving:

1. first-person movement and look;
2. one data-defined product;
3. product pickup and deterministic shelf snapping;
4. valid and invalid placement feedback;
5. save and restore of product placement;
6. one placeholder navigation agent in a graybox store;
7. a reproducible repository checkout; and
8. a runnable desktop PC build.

Do not add final art, economy, customer simulation, employee behavior, generalized framework code, or the complete convenience-store loop.

### Stage 3 exit gate

Continue with Unity when:

- the project opens and runs reproducibly;
- the build launches;
- generated and human-written changes remain understandable;
- the interaction and navigation proof works without a project-blocking Unity limitation;
- the project owner accepts the editor and iteration workflow.

A failed implementation attempt triggers diagnosis and bounded correction before any engine reconsideration.

## Schedule disposition

The dates in `Margins_Master_Roadmap_v0.1.md` remain proposals and were already identified by the project owner as substantially inaccurate.

Do not issue another full-project forecast until the Unity foundation spike provides measured setup, implementation, debugging, agent-review, and build velocity.

The next useful timing evidence is:

- hours to install and verify the Unity baseline;
- hours to create and commit the project;
- hours to achieve the first snapped product;
- hours to save and restore it;
- hours to produce the first runnable build;
- human review burden for agent-generated code.

Use those measurements to replace speculation with project-specific velocity.

## Immediate next task

Complete the local Unity 6.5 Supported installation, confirm the applicable Unity licensing case, then execute `Margins_Unity_First_Foundation_Spike_Agent_Prompt.md` from current `main` and measure the result.