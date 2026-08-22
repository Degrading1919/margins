# Margins Dimensional Authoring and Snapping Convention v1.0

## Status and authority

- **Status:** Approved
- **Approved by:** Project owner
- **Approval date:** August 22, 2026
- **Scope:** 3D asset authoring, Unity world scale, modular-building generation, fixture placement, and attachment sockets
- **Supersedes:** The use of the current 0.5 m first-store fixture grid as a project-wide dimensional convention. The existing runtime implementation remains valid only until it is deliberately migrated.

## Decision

Margins uses an **imperial-friendly authoring convention with meter-native Unity runtime scale**.

### Human-facing asset authoring

- Design commercial architecture, fixtures, furniture, equipment, and related props using normal U.S. feet-and-inches dimensions.
- Blender may use Imperial unit display so artists can enter and inspect dimensions directly in feet and inches.
- Assets must remain physically scaled to their intended real-world size. Imperial display is an authoring convenience, not a different runtime scale.
- Do not distort otherwise-standard American commercial dimensions merely to fit a metric grid.

### Unity runtime scale

- Unity remains meter-native: **1 Unity unit = 1 meter**.
- Exact conversion values are authoritative when conversion is required:
  - **1 ft = 0.3048 m**
  - **6 in = 0.1524 m**
  - **1 in = 0.0254 m**
- Imported meshes, generated geometry, physics, navigation, colliders, and transforms must resolve to correct real-world meter scale in Unity.

### Structural planning and procedural generation

- Use a **1 ft structural planning increment** for building footprints, primary wall endpoints, and comparable coarse architectural layout decisions.
- This is a planning/snap increment, not a requirement that every architectural dimension be an integer number of feet.
- Doors, windows, trim, wall thicknesses, counters, equipment, and other components may use normal real-world inch dimensions.
- Procedurally generated walls should be built around declared openings rather than requiring a separate authored wall mesh for every door/window combination.

### Player fixture placement

- Use a **6 in placement increment** for normal movable fixtures and furniture in player Build Mode.
- This replaces 0.5 m as the intended production placement increment.
- The finer grid is intended to support counters, shelving, coolers, appliances, café furniture, and other commercial fixtures without forcing awkward metric-derived dimensions.
- A future free-placement mode is not approved by this decision.

### Attachment sockets

- Components that attach to another object should use explicit local attachment sockets rather than the world placement grid.
- Socket-to-socket placement is not constrained to 1 ft or 6 in increments.
- Examples include registers on counters, card terminals, scanners, signage on walls, awnings, shelf attachments, and other parent-relative modules.
- Socket transforms must preserve authored local position, orientation, and scale through export/import.
- A universal socket schema is not approved by this decision; define only the metadata needed by each production system until reuse proves a broader contract is necessary.

### Rotation

- **15 degree increments** are the default working rotation step for movable assets when finer rotation is useful.
- **90 degree rotation** remains the expected common case for shelving, counters, appliances, and other orthogonal commercial fixtures.
- Asset-specific or socket-driven orientation may override the general rotation step when required.

## Modular-building implication

Plain structural geometry does not need to exist as a unique Blender or Tripo mesh when Unity can generate it more cleanly from dimensions and material assignments.

The preferred working direction is hybrid:

- generate or construct simple walls, floors, ceilings, roof slabs, and opening infill procedurally where practical;
- author visually meaningful components such as doors, storefront frames, window systems, trim, awnings, signs, columns, and decorative facade pieces as reusable assets;
- apply reusable materials and color variants in Unity rather than creating duplicate geometry for every finish;
- keep procedural assembly and player-placeable assets compatible through consistent dimensions, pivots, footprints, and sockets where those concepts overlap.

This is an approved dimensional convention, not approval of a complete procedural-building architecture or unrestricted structural editing in player Build Mode.

## Blender and generated-asset intake

For Blender, Tripo, sourced meshes, and other external assets:

1. author or normalize to believable real-world dimensions;
2. use feet/inches for human-facing dimension decisions when that is the natural commercial reference;
3. preserve clean pivots and any required attachment transforms;
4. apply/normalize transforms as required by the production pipeline;
5. verify the imported Unity size against known dimensions rather than trusting exporter defaults; and
6. reject scale fixes that merely compensate for a broken import/export setup.

Tripo prompts may state important dimensions in feet/inches. Generated dimensions remain approximate until measured and normalized in Blender/Unity.

## Current implementation and migration boundary

The existing first-store fixture-placement implementation currently contains a **0.5 m** configurable cell-size default. That is current implementation evidence, not the production dimensional standard after this decision.

Do **not** silently reinterpret existing persisted grid coordinates under the new 6 in convention.

The runtime migration must be handled as a deliberate implementation change that:

- preserves existing fixture world positions or migrates them deterministically;
- updates placement/grid conversion logic through one authoritative dimensional contract rather than scattered literals;
- preserves stable fixture identities and save/load behavior;
- updates placement validation and tests;
- verifies navigation/obstacle behavior after fixture movement; and
- records any save-schema or legacy-layout migration required.

Until that implementation change lands, documentation and new asset authoring should follow this approved convention while the current playable build may still exhibit the legacy 0.5 m placement behavior.

## Scope boundary

This decision does **not** by itself approve:

- player relocation of exterior structural walls;
- arbitrary runtime boolean wall cutting;
- unrestricted door/window placement by the player;
- a full Sims-style construction system;
- a universal procedural-building framework;
- utility, structural-engineering, zoning, or code-compliance simulation; or
- any specific socket data schema beyond the need for explicit parent-relative attachment points.

Those remain separate design and implementation decisions.

## Reopening rule

Reopen this convention only when production evidence shows that the 1 ft structural increment, 6 in fixture increment, meter-native runtime scale, or socket-based attachment model creates a concrete usability, content-authoring, persistence, navigation, or performance problem. Preference for round metric values alone is not sufficient.