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

- Project owner approves Unity 6.3 LTS, URP, package baseline, project location, and Windows x64 development build target.
- Unity Hub and Unity 6.3 LTS editor are available locally.
- Unity Personal eligibility is confirmed by the project owner.
- Git LFS is installed before adding binary production assets; the spike may use primitive placeholders without binary source assets.

## Required Behaviors

- Player can move and look around the graybox room with keyboard and mouse.
- Player can pick up one product, hold it visibly, and release it.
- Shelf exposes explicit snap points with stable identifiers.
- Product snaps only to compatible, unoccupied snap points.
- Invalid placement gives visible feedback and does not occupy a slot.
- Occupied slots reject additional placement.
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
- snap compatibility tag;
- scan-demo value.

### Shelf and Snap Definition

Required fields only:

- stable fixture identifier;
- stable snap-point identifiers;
- snap-point local position and orientation;
- accepted compatibility tags;
- occupied state;
- placement validation result.

### Placed Product State

Required fields only:

- product definition identifier;
- fixture identifier;
- snap-point identifier;
- quarter-turn orientation integer `0-3`.

Quantity is excluded unless the coding agent proves it is required for the single-product save/reload proof.

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
- duplicate stable identifiers in authored data: fail validation before play/build.

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
- a Windows desktop development build is produced and launches locally;
- required tests or deterministic checks pass;
- no authored code, asset, data, scene behavior, or direct package dependency implements an excluded system; documented exclusions and unavoidable core/transitive Unity dependencies do not violate this criterion;
- the PR contains a final diff review and does not merge itself.

## Owner Continuation Gate

After technical acceptance, the coding agent must report measured setup, implementation, debugging, test, build, and review evidence and ask the project owner for a `continue`, `adjust`, or `stop` decision. The coding agent must not claim the Unity workflow is owner-accepted.

## Manual Validation Steps

1. Open `CODE/Unity/Margins` in Unity 6.3 LTS.
2. Open the first foundation spike scene.
3. Enter Play Mode.
4. Move and look around the graybox room.
5. Pick up the product and attempt an invalid placement.
6. Confirm invalid feedback and no slot occupation.
7. Snap the product to a valid shelf slot.
8. Attempt to place another instance or duplicate into the occupied slot and confirm rejection.
9. Save, reload, and confirm placement equality.
10. Confirm the navigation placeholder moves between its two points.
11. Build and launch a Windows desktop development build.

## Automated Checks

Required checks:

- duplicate product identifiers fail validation;
- invalid snap-point references fail validation or load safely;
- occupied slots reject placement deterministically;
- save/reload placement equality compares product id, fixture id, snap id, and orientation.

## Completion Evidence

The coding-agent PR must include:

- Unity editor version and packages actually used;
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

- Unity 6.3 LTS cannot be installed or licensed;
- URP project creation fails repeatedly;
- selected packages cannot be installed in a clean project;
- first-person movement, snapping, save/reload, or build cannot be proven without adding a broad framework;
- the implementation requires paid assets, cloud services, or packages outside the approved baseline;
- generated code becomes difficult to inspect or debug.
