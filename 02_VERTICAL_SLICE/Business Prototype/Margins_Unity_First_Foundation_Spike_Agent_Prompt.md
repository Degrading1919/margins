# Margins Unity First Foundation Spike Agent Prompt

Assume the **Margins Technical Architect Assistant** role, with the **Producer and Roadmap Assistant** as a secondary production lens.

Repository:

`https://github.com/Degrading1919/margins`

## Authority Files to Inspect First

Read the current versions of:

- `00_ADMIN/Reference/Margins_Assistant_Roles.md`
- `00_ADMIN/Reference/Margins_Repository_Structure.md`
- `00_ADMIN/Decisions/Margins_Engine_Selection_Decision_v1.0.md`
- `00_ADMIN/Roadmaps/Margins_Roadmap_Amendment_001_Unity_Selection_v0.1.md`
- `01_PRE-PRODUCTION/1.1 Core Vision/Margins_Project_Brief_v0.1.md`
- `01_PRE-PRODUCTION/1.3 Feature Set & Scope/Margins_Initial_Scope_Boundaries_v0.1.md`
- `01_PRE-PRODUCTION/1.4 Technical Foundation/Margins_Technical_Foundation_Direction_v0.1.md`
- `01_PRE-PRODUCTION/1.4 Technical Foundation/Margins_Unity_Technical_Baseline_v0.1.md`
- `01_PRE-PRODUCTION/1.4 Technical Foundation/Margins_Unity_Bootstrap_Standard_v0.1.md`
- `02_VERTICAL_SLICE/Business Prototype/Margins_Unity_First_Foundation_Spike_v0.1.md`
- `.gitignore`
- `.gitattributes`

Treat Unity as the approved engine. Do not reopen engine selection and do not compare engines.

## Execution Baseline

Use:

- the latest project-owner-approved Unity 6.5 Supported patch (`6000.5.x`) available at execution time;
- Universal Render Pipeline;
- C# as the default implementation language;
- editor-authored scenes, prefabs, components, and ScriptableObjects where appropriate;
- no Visual Scripting for spike behavior;
- Input System `1.20.0`;
- AI Navigation `2.0.14`;
- Universal Render Pipeline at the editor-matched Unity 6.5 package lane;
- Unity Test Framework at the editor-matched Unity 6.5 package lane;
- Windows desktop x64 development build target;
- Unity project path `CODE/Unity/Margins`;
- root namespace `Margins`;
- human-readable local JSON save proof.

Record the exact Unity editor patch in `CODE/Unity/Margins/ProjectSettings/ProjectVersion.txt` and in the PR body. Record resolved package versions from `CODE/Unity/Margins/Packages/manifest.json` and `CODE/Unity/Margins/Packages/packages-lock.json`.

If any baseline item or named package version cannot be applied locally, stop, explain the blocker, and do not replace it with an unapproved package, package version, or architecture. Do not silently change package versions after project creation.

## Implementation Scope

Create only:

1. a new Unity project at `CODE/Unity/Margins`;
2. one graybox convenience-store room;
3. first-person movement and mouse look;
4. one data-defined product;
5. one product pickup and release interaction;
6. one shelf with deterministic snap points;
7. valid and invalid placement feedback;
8. save and reload of the snapped product placement;
9. one placeholder navigation agent moving between two points;
10. one runnable local Windows desktop development build;
11. focused tests or deterministic checks for duplicate product identifiers, invalid snap-point references, occupied-slot rejection, and save/reload placement equality.

## Data and Save Boundary

Implement only the minimum fields from `Margins_Unity_First_Foundation_Spike_v0.1.md`.

Do not implement full inventory, suppliers, pricing, customers, employees, economy, properties, multi-location state, production UI, or production save migration.

Product and placement limits:

- use exactly one product definition;
- the product definition may contain only identity, display, visual representation, shelf footprint or size, and snap compatibility;
- do not add scanning or checkout data;
- authored shelf/snap definitions may contain only stable fixture and snap-point identifiers, local position/orientation, and accepted compatibility tags;
- do not store runtime occupancy in shared authored definitions or ScriptableObjects;
- runtime occupancy must exist on shelf instances during play or be derived from current placed-product records.

Occupied-slot validation:

- use a second temporary scene instance of the same product definition only as a validation fixture;
- this does not create inventory, quantity, product stacks, or multiple product types;
- attempting to place the second instance in an occupied slot must fail without losing, duplicating, or replacing the existing product.

Save behavior must include:

- version field;
- stable identifiers;
- safe failure behavior for malformed save, unsupported version, missing product, missing snap point, and duplicate authored identifiers;
- human-readable JSON;
- no cloud save and no database.

Load behavior must begin with all runtime snap points unoccupied, validate saved placement records, place valid products, and rebuild occupancy only from accepted placements.

## Code Standard

Write minimal readable C#.

Required:

- fewest scripts that cleanly express the behavior;
- one responsibility per script where it improves debugging;
- explicit inspector fields;
- deterministic snap-point selection;
- explicit validation errors;
- bounded debug logging;
- focused EditMode or PlayMode tests when they replace repeated manual checking.

Prohibited:

- empty abstraction layers;
- speculative interfaces;
- global service locator;
- dependency-injection framework;
- generalized event bus;
- singleton unless proven necessary in the PR;
- reflection-heavy framework;
- third-party gameplay framework;
- comments that restate obvious code;
- performance optimization without measured evidence.

## Explicit Exclusions

Do not add:

- boxes;
- checkout payment;
- pricing;
- economy;
- customers beyond one navigation placeholder;
- employees;
- multiple locations;
- detailed or aggregate simulation;
- production UI;
- final art;
- final animation;
- sound;
- day/night;
- construction systems;
- procedural generation;
- multiplayer;
- driving;
- production save migration;
- generalized frameworks;
- CI/CD.

## Required Workflow

0. Verify that the project owner approved the Unity baseline package and that all baseline documents listed above exist on current `main`; otherwise stop.
1. Verify repository state and create a new branch from latest `main`.
2. Create the Unity project in the approved path.
3. Configure only the approved packages and settings required for the spike.
4. Implement the spike.
5. Run focused tests or deterministic checks.
6. Produce a local Windows desktop development build.
7. Review the final diff and confirm no excluded systems were added.
8. Commit the intended files only.
9. Open a **draft pull request** targeting `main`.
10. Do **not** merge.

## Final Response Requirements

Report only:

- branch name;
- draft PR number and title;
- Unity editor version;
- render pipeline;
- selected packages;
- exact editor patch recorded in `ProjectVersion.txt`;
- resolved package versions from `manifest.json` and `packages-lock.json`;
- project path;
- scene path;
- tests or checks run;
- build output path or exact build limitation;
- manual validation result;
- measured workflow evidence for owner `continue`, `adjust`, or `stop` review;
- files changed summary;
- confirmation that no excluded systems were added;
- blockers or owner decisions needed.

Avoid generic explanation and long narrative output.
