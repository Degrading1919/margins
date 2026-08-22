# Margins Architectural Asset Interface Contract v1.0

## Status and authority

- **Status:** Approved
- **Approved by:** Project owner
- **Approval date:** August 22, 2026
- **Scope:** Reusable architectural assets, procedural-building assembly, Blender normalization, Unity prefab authoring, opening interfaces, attachment sockets, extension anchors, material regions, pivots, and naming
- **Related decisions:**
  - `00_ADMIN/Decisions/Margins_Dimensional_Authoring_and_Snapping_Convention_v1.0.md`
  - `00_ADMIN/Decisions/Margins_Architectural_Dimensional_Profile_v1.0.md`

This contract defines how reusable architectural assets interface with Unity and procedural building assembly. It is intentionally narrower than a complete procedural-building architecture and does not approve unrestricted player structural editing.

## Coordinate and scale convention

All reusable architectural assets use the following Unity-facing orientation:

- **+Y = up**;
- **+Z = forward / outward-facing side**;
- **+X = right**;
- Unity remains meter-native at **1 Unity unit = 1 meter**.

For exterior facade components, +Z points away from the building toward the exterior/street side. For interior wall-mounted components, +Z points away from the host wall into the room.

Blender may use Imperial display for authoring, but exports must preserve correct real-world scale and orientation after normalization.

## Pivot conventions

### Floor-standing fixtures

Examples: counters, coolers, shelving, ATMs, cabinets, freestanding displays.

- pivot at the **bottom-center of the intended placement footprint**;
- pivot lies on finished-floor level;
- forward orientation follows the +Z convention.

### Wall-opening assets

Examples: doors, storefront frames, window assemblies.

- pivot at the **bottom-center of the required wall opening**;
- pivot lies on the wall reference plane;
- opening dimensions are supplied explicitly through prefab metadata rather than inferred from mesh bounds.

### Wall-mounted accessories

Examples: signs, cameras, electrical boxes, awnings, wall lights.

- pivot at the **center of the mounting plane** where the asset contacts the host surface;
- the asset's local +Z points away from the host wall.

### Ceiling-mounted assets

Examples: lights, fans, hanging signs.

- pivot at the **center of the attachment point** on the ceiling plane.

### Corner pieces

Examples: exterior corner trim, corner pilasters, architectural corner caps.

- pivot at the bottom of the intended architectural corner intersection.

## Wall reference geometry

Procedural walls are defined from a **wall centerline plus wall thickness and orientation**.

The generator derives:

- wall centerline;
- exact wall thickness;
- exterior face;
- interior face;
- outward normal;
- inward normal.

Reusable wall-mounted or opening assets must not assume that every host wall has one fixed thickness unless the asset explicitly requires a bounded jamb or frame depth.

### Grid and surface-offset rule

The structural and placement grids control positions **along surfaces and in plan**, but they do **not** quantize the perpendicular mounting offset.

- structural wall endpoints use the approved **1 ft** planning increment where applicable;
- normal opening positions along a wall use the approved **6 in** increment;
- player floor-fixture placement uses the approved **6 in** Build Mode increment;
- wall and ceiling attachments resolve their perpendicular position from the **exact host-surface geometry**;
- socket transforms use their exact local transform and are not forced to the 1 ft or 6 in grids.

Examples:

- an **8 in** exterior wall has faces exactly **4 in** from its centerline;
- a **4.5 in** interior partition has faces exactly **2.25 in** from its centerline;
- a wall-mounted bulletin board may move along the wall in 6 in steps while remaining exactly flush to the host wall face;
- a register may snap to a countertop socket at any exact local offset regardless of the counter's world-grid placement.

This separation is authoritative and prevents wall thickness, sockets, or attached props from being distorted to match the floor-placement grid.

## Opening metadata

Door and window assets must declare the opening they require instead of forcing the generator to infer dimensions from render geometry.

Minimum opening metadata should support, where relevant:

- opening width;
- opening height;
- sill height;
- required or supported host-wall depth range;
- opening category or compatibility classification when needed.

The procedural wall generator creates wall geometry around the declared opening and then places the corresponding architectural asset into that opening.

Do not require a separate authored wall mesh for every door/window combination.

## Fixed, repeatable, and stretch-safe classification

Architectural and modular assets must be classified by how they may be resized or assembled.

### Fixed

Do not non-uniformly stretch during normal procedural assembly.

Typical examples:

- doors;
- windows;
- equipment;
- columns;
- detailed decorative modules.

### Repeatable

Extend through repetition or anchor-to-anchor assembly rather than arbitrary stretching.

Typical examples:

- storefront mullions and glazing bays;
- baseboards;
- trim;
- parapet/coping sections;
- ceiling-grid pieces;
- railings;
- shelving and counter modules.

### Stretch-safe

May be resized along explicitly approved axes when the geometry and material treatment remain visually correct.

Typical examples:

- simple flat fascia fillers;
- basic sign panels;
- intentionally authored filler trim.

Detailed assets are fixed by default unless explicitly authored and tested as repeatable or stretch-safe.

## Socket scope

Sockets are explicit local transforms used for parent-relative attachment. They are not world-grid points and are not quantized to the structural or fixture placement grid.

Start with a small semantic set rather than a universal socket framework.

Initial general-purpose socket concepts may include:

- `WallMount`;
- `CeilingMount`;
- `CountertopMount`;
- `SignMount`;
- `AccessoryMount`;
- `LeftExtension`;
- `RightExtension`;
- `TopExtension`.

Asset-specific sockets may be added when justified, such as:

- `RegisterMount`;
- `ScannerMount`;
- `CardTerminalMount`.

Socket names may evolve as implementation proves better terminology. This decision approves the semantic approach, not a frozen universal schema.

## Modular extension anchors

Assets intended to splice into longer assemblies should expose explicit extension anchors rather than relying on repeated bounding-box calculations.

Preferred extension anchors:

- `LeftAnchor`;
- `RightAnchor`;
- `TopAnchor` or `BottomAnchor` only where vertically modular assembly requires them.

The next module aligns its compatible anchor to the previous module's anchor while preserving the authored local orientation and scale.

This approach is especially appropriate for:

- storefront glazing systems;
- gondola shelving;
- counters;
- cabinets;
- trim;
- ceiling grids;
- fencing and railings.

## Material and customization regions

Reusable assets should be authored around logical customization regions rather than duplicate meshes for each color or finish.

Examples:

### Storefront frame

- frame;
- glass;
- hardware;
- optional accent.

### Commercial door

- door surface;
- frame;
- glass;
- hardware.

### Awning

- body/fabric;
- frame;
- trim/accent.

### Counter

- body;
- countertop;
- trim;
- hardware.

Logical customization regions do not require one Unity material slot each. Prefer efficient masks, shared materials, shader properties, atlases, or other reviewed approaches where appropriate.

## Mesh-separation rule

Separate geometry when a part needs one or more of the following:

- independent movement or animation;
- independent swapping or procedural inclusion/exclusion;
- materially different customization treatment that cannot be handled cleanly in one mesh/material setup;
- independent reuse in another asset family;
- separate interaction or functional behavior.

Keep minor nonfunctional details integrated when separation provides no practical benefit.

Do not create excessive GameObject or mesh fragmentation for screws, fixed hinges, decorative seams, or comparable micro-components.

## Naming convention

Use predictable, descriptive asset names.

Preferred pattern:

`ARCH_<Category>_<Descriptor>_<MeaningfulDimensionOrVariant>`

Examples:

- `ARCH_Door_CommercialGlass_3x7`;
- `ARCH_Door_Service_3x7`;
- `ARCH_Storefront_4ft`;
- `ARCH_Storefront_6ft`;
- `ARCH_Storefront_8ft`;
- `ARCH_Awning_3ft`;
- `ARCH_Trim_ExteriorCorner`.

Use human-readable imperial dimensions in names when those dimensions materially distinguish modules. Store exact runtime dimensions in authoritative Unity metadata using meter-native values.

## Tripo -> Blender -> Unity responsibility split

### Tripo

Tripo is responsible for producing useful initial visual geometry and candidate shape language.

Tripo output is not expected to be dimensionally exact or production-ready.

### Blender

Blender is responsible for normalization work such as:

- correcting exact dimensions;
- establishing the approved pivot and orientation;
- separating only functionally required pieces;
- topology cleanup where needed;
- establishing logical material regions;
- establishing or verifying authored attachment locations where practical;
- applying/normalizing transforms;
- exporting clean production intake at correct real-world scale.

### Unity

Unity owns production prefab behavior and runtime-facing metadata, including:

- opening metadata;
- final sockets/anchors where appropriate;
- colliders;
- material assignments and runtime customization;
- procedural-building classification;
- Build Mode behavior;
- gameplay interaction metadata;
- validation of imported scale and orientation.

Do not spend disproportionate effort trying to force Tripo to produce exact architectural dimensions when Blender normalization is the controlled production step.

## Procedural-building implications

The generator should assemble approved components from explicit dimensions, host surfaces, openings, anchors, and semantic compatibility rather than relying on mesh-bound guessing.

The generator may:

- place fixed assets into declared openings;
- repeat compatible modules through extension anchors;
- resize only explicitly stretch-safe assets;
- derive exact wall-face offsets from wall centerline and thickness;
- place attached components through exact local sockets;
- apply approved material/customization variants without duplicating geometry.

The generator must not:

- arbitrarily non-uniformly scale fixed detailed assets;
- force sockets to the 6 in fixture grid;
- reinterpret wall thickness to satisfy a placement grid;
- infer critical opening dimensions solely from rendered mesh bounds when authoritative metadata exists;
- treat this contract as approval for a universal runtime construction system.

## Scope boundary

This decision does not approve:

- the final procedural-building architecture;
- unrestricted player wall, window, or door editing;
- runtime boolean building tools;
- one universal socket schema for all Margins content;
- a specific shader or material-property implementation;
- an automatic Tripo-to-production pipeline without Blender/Unity validation;
- arbitrary scaling of detailed assets.

## Reopening rule

Reopen this contract when production evidence shows that the approved axis, pivot, wall-reference, opening, anchor, socket, material-region, or normalization conventions create concrete asset-authoring, procedural-generation, Build Mode, persistence, navigation, or visual-quality problems.