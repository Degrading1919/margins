# Margins Unity First Foundation Spike v0.1

## Purpose

Create the smallest Unity proof that Margins can support tactile first-person product handling, deterministic shelf placement, placement persistence, and one basic navigation placeholder inside a convenience-store graybox.

This is an implementation specification, not code. It depends on approval of `Margins_Unity_Technical_Baseline_v0.1.md` and `Margins_Unity_Bootstrap_Standard_v0.1.md`.

## Exact Scope

Implement only:

1. a new Unity project at `CODE/Unity/Margins`;
2. one graybox convenience-store room;
3. first-person movement and mouse look;
4. one data-defined product;
5. product pickup and release interaction;
6. one shelf with deterministic snap points;
7. valid and invalid placement feedback;
8. save and reload of snapped product placement;
9. one placeholder navigation agent moving between two points;
10. one runnable local Windows desktop development build;
11. focused tests or deterministic checks for the required validation cases.

## Prerequisites

- Project owner approves Unity 6.5 Supported (`6000.5.x`), URP, package baseline, project location, and Windows x64 development build target.
- Unity Hub and the latest stable project-owner-approved `6000.5.x` patch available at execution time are available locally.
- The exact editor patch must be recorded in `CODE/Unity/Margins/ProjectSettings/ProjectVersion.txt` and in the implementation PR body.
- Project owner confirms the applicable Unity Personal case and confirms the relevant Unity-defined finances remain below the Unity Personal threshold before relying on Unity Personal.
- Git LFS is installed before adding binary production assets; the spike may use primitive placeholders without binary source assets.

## Required Behaviors

- Player can move and look around the graybox room with keyboard and mouse.
- Player can pick up one product, hold it visibly, and release it.
- Shelf exposes explicit snap points with stable identifiers.
- Product snaps only to compatible, unoccupied snap points.
- Invalid placement gives visible feedback and does not occupy a slot.
- Occupied slots reject additional placement.
- The occupied-slot check may use two temporary scene instances of the same product definition solely as a validation fixture.
- Save writes the snapped placement.
- Reload restores the product to the same fixture, snap point, and orientation.
- One placeholder navigation agent travels between two fixed points without crossing solid fixtures.

## Deterministic Interaction Details

- Movement: `WASD` for horizontal movement.
- Look: mouse look.
- Pickup/release: `E` picks up the targeted product when empty-handed and releases the held product when holding one.
- Rotate held product: `R` advances orientation by one quarter turn.
- Save: `F5`.
- Load: `F9`.
- Snap search radius: inspector-configurable, default `0.75` meters.
- Snap selection: nearest compatible unoccupied snap point inside the radius; distance ties resolve by ascending ordinal snap-point identifier, such as `slot-01` before `slot-02`.
- Valid feedback: distinct visible green or equivalent positive material/state on the candidate snap point or product.
- Invalid feedback: distinct visible red or equivalent negative material/state and no slot occupation.
- Saved orientation: quarter-turn integer `0-3` so equality can be exact.

## Minimal Data Boundary

### Product Definition

Required fields only:

- stable product identifier;
- display name;
- referenced visual prefab or placeholder visual;
- shelf footprint or size category;
- snap compatibility tag.

### Shelf and Snap Definition

Authored required fields only:

- stable fixture identifier;
- stable snap-point identifiers;
- snap-point local position and orientation;
- accepted compatibility tags.

Do not store slot use in authored shelf or snap definitions.

Runtime occupancy must exist on the shelf instance during play or be derived from current placed-product records.

### Placed Product State

Required fields only:

- product definition identifier;
- fixture identifier;
- snap-point identifier;
- quarter-turn orientation integer `0-3`.

Quantity is excluded from the foundation spike.

The spike uses exactly one product definition. It may place two temporary scene instances of that same definition only to test occupied-slot rejection. This does not create inventory, quantity, product stacks, or multiple product types.

## Minimal Save Boundary

Use one human-readable JSON save file with:

- integer version field;
- placed-product records;
- stable product, fixture, and snap-point identifiers;
- safe load failure behavior.

Safe failure behavior:

- malformed save: report error, do not overwrite the file, start with empty placement state;
- unsupported version: report error and refuse load;
- missing product or snap target: report error, create no product for that record, and leave no slot occupied;
- occupied or duplicate accepted placement target during load: accept the first valid placement deterministically, reject later conflicting records, and report the conflict;
- duplicate stable identifiers in authored data: fail validation before play/build.

Load order:

1. Begin with all runtime snap points unoccupied.
2. Validate saved placement records against authored product, fixture, and snap identifiers.
3. Place valid products.
4. Rebuild occupancy only from accepted placements.

No cloud saves, database, encryption, compression, user profiles, complete migration framework, or production save architecture.

## Exclusions

Do not implement:

- full inventory;
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
- generalized frameworks.

## Acceptance Criteria

The spike is complete only when:

- the Unity project opens from a fresh checkout without unresolved compile errors;
- the spike scene runs in Play Mode;
- movement, look, pickup, release, valid snap, invalid feedback, occupied-slot rejection, save, reload, and placeholder navigation all work;
- save/reload restores the same product definition, fixture, snap point, and orientation;
- attempting to place the second temporary scene instance into an occupied slot fails without losing, duplicating, or replacing the existing product;
- a Windows desktop development build is produced and launches locally;
- required tests or deterministic checks pass;
- no authored code, asset, data, scene behavior, or direct package dependency implements an excluded system; documented exclusions and unavoidable core/transitive Unity dependencies do not violate this criterion;
- the PR contains a final diff review and does not merge itself.

## Owner Continuation Gate

After technical acceptance, the coding agent must report measured setup, implementation, debugging, test, build, and review evidence and ask the project owner for a `continue`, `adjust`, or `stop` decision. The coding agent must not claim the Unity workflow is owner-accepted.

## Manual Validation Steps

1. Open `CODE/Unity/Margins` in Unity 6.5 Supported.
2. Open the first foundation spike scene.
3. Enter Play Mode.
4. Move and look around the graybox room.
5. Pick up the product and attempt an invalid placement.
6. Confirm invalid feedback and no slot occupation.
7. Snap the product to a valid shelf slot.
8. Use a second temporary scene instance of the same product definition to attempt placement into the occupied slot.
9. Confirm the occupied-slot attempt fails without losing, duplicating, or replacing the existing product.
10. Save, reload, and confirm placement equality.
11. Confirm the navigation placeholder moves between its two points.
12. Build and launch a Windows desktop development build.

## Automated Checks

Required checks:

- duplicate product identifiers fail validation;
- invalid snap-point references fail validation or load safely;
- occupied-slot rejection fails cleanly when a second temporary scene instance of the same product definition targets an occupied slot;
- save/reload placement equality compares product id, fixture id, snap id, and orientation.

## Completion Evidence

The coding-agent PR must include:

- exact Unity editor patch matching `ProjectVersion.txt` and packages actually resolved from `manifest.json` and `packages-lock.json`;
- project path;
- scene path;
- test results or unrun-test explanation;
- build output path or unbuilt explanation;
- manual validation result;
- measured workflow evidence for owner continuation review;
- list of files changed;
- explicit confirmation that excluded systems were not added.

## Rollback or Failure Conditions

Stop and report instead of expanding scope when:

- Unity 6.5 Supported cannot be installed or licensed;
- URP project creation fails repeatedly;
- selected packages cannot be installed in a clean project;
- first-person movement, snapping, save/reload, or build cannot be proven without adding a broad framework;
- the implementation requires paid assets, cloud services, or packages outside the approved baseline;
- generated code becomes difficult to inspect or debug.
