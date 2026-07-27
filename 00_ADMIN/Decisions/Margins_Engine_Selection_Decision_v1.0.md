# Margins Engine Selection Decision v1.0

## Status and authority

- **Status:** Approved
- **Approved by:** Project owner
- **Approval date:** July 27, 2026
- **Decision:** Unity is the production engine for Margins.
- **Authority:** This decision supersedes FD-010's engine-neutral holding state and any lower-authority document that still describes the engine as unselected.

## Approved direction

Margins will proceed in **Unity**.

Unity is now the assumed engine for:

- technical planning;
- repository and project bootstrap design;
- implementation prompts and coding workflows;
- asset-pipeline planning;
- first-person interaction prototypes;
- navigation and employee/customer implementation;
- UI and reporting implementation;
- data, validation, persistence, and testing plans;
- performance and build planning.

The project will not spend additional time implementing equivalent Unreal Engine and Godot prototypes merely to validate the engine choice.

## Decision basis

The owner selected Unity after reviewing the engine-evaluation research and determining that a prolonged three-engine comparison would delay production without proportional decision value.

The working rationale is:

- Unity provides a practical 3D foundation for the required first-person and management combination;
- C#-oriented workflows are well suited to bounded AI-assisted implementation and human review;
- Unity has mature navigation, UI, profiling, testing, and asset workflows relevant to Margins;
- its ecosystem is likely to reduce solo-production friction for stylized environments, products, fixtures, characters, and commercial interiors;
- Unreal Engine's additional project weight is not currently justified for this game's target presentation and production constraints;
- Godot's licensing advantages do not currently outweigh Unity's broader 3D production ecosystem for this project.

These are decision reasons, not claims that Unity is universally superior or that all project-specific technical risks are already proven.

## Disposition of the engine-evaluation package

The four files added by PR #7 remain useful **historical research and risk reference**:

- `Margins_Engine_Evaluation_Criteria_v0.1.md`
- `Margins_Engine_Candidate_Shortlist_v0.1.md`
- `Margins_Engine_Risk_Prototype_Plan_v0.1.md`
- `Margins_Engine_Evaluation_Quality_Audit_v0.1.md`

Their unresolved selection state and proposed three-engine comparison no longer govern execution.

- Unity is selected.
- Unreal Engine and Godot are not selected for the current production baseline.
- The 230–390-hour comparative prototype program is not authorized.
- Relevant risk questions may be reused as Unity-specific acceptance checks where they materially protect the project.
- Engine selection may be reopened only by a later project-owner decision after a concrete Unity blocker, licensing incompatibility, or unacceptable production burden is demonstrated.

## Immediate implementation direction

The next executable technical milestone is a **small Unity foundation spike**, not further multi-engine research.

The spike should prove, with minimal code and representative content:

1. a clean Unity project and reproducible repository workflow;
2. first-person movement and interaction;
3. one data-defined product;
4. product pickup and deterministic shelf snapping;
5. valid and invalid placement feedback;
6. save and restore of the snapped product state;
7. one placeholder navigation agent moving inside a graybox store; and
8. a runnable desktop PC build.

The spike is not yet authorized for implementation until its exact Unity baseline and task specification are approved.

## Still unresolved

This decision does **not** approve:

- Unity editor version or support lane;
- rendering pipeline;
- scripting and visual-scripting boundaries;
- input package;
- navigation package and version;
- UI framework;
- save format or serialization library;
- dependency policy;
- source-control and large-file rules;
- folder and assembly structure;
- coding conventions;
- target desktop operating system, minimum hardware, or performance budgets;
- specific assets, plugins, paid tools, or Unity service subscriptions;
- Unity account/tier eligibility or future licensing changes;
- production architecture beyond Unity as the engine.

Those items require focused recommendations and project-owner approval before they become canonical.

## Alternatives and reopening rule

Unreal Engine and Godot remain documented alternatives, not active parallel workstreams.

Reopen engine selection only when at least one of the following is demonstrated:

- Unity cannot legally or affordably support the approved production model;
- the Unity foundation spike exposes a project-blocking limitation that cannot be resolved within a bounded effort;
- Unity's workflow creates an unacceptable solo-development or agent-review burden;
- a later binding requirement invalidates Unity's suitability; or
- the project owner explicitly reopens the decision.

Preference, novelty, or hypothetical superiority alone is insufficient to restart a multi-engine comparison.
