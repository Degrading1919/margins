# Margins Architectural Dimensional Profile v1.0

## Status and authority

- **Status:** Approved
- **Approved by:** Project owner
- **Approval date:** August 22, 2026
- **Scope:** Commercial building shells, storefront systems, ground-floor retail frontage, doors, windows, trim, awnings, signs, suspended ceilings, procedural-building inputs, and architectural asset authoring
- **Related decision:** `00_ADMIN/Decisions/Margins_Dimensional_Authoring_and_Snapping_Convention_v1.0.md`

This profile defines the approved dimensional baseline for reusable commercial architecture in Margins. It is a project content and generation standard, not a claim of real-world building-code compliance.

## Core dimensional baseline

Margins uses normal U.S. commercial dimensions for human-facing authoring while Unity remains meter-native under the approved dimensional convention.

### Standalone small-commercial shell

- exterior structural wall / roof-deck height: **12 ft**;
- default finished interior ceiling height: **10 ft AFF**;
- default parapet top: **14 ft**;
- exterior wall thickness: **8 in**;
- interior partition thickness: **4.5 in**;
- floor-slab visual thickness: **6 in**;
- structural planning increment: **1 ft**;
- normal opening-placement increment along walls: **6 in**.

The 12 ft / 10 ft / 14 ft relationship is the default standalone small-commercial vertical profile. Other building archetypes, especially multistory and tower ground floors, are allowed to use different floor-to-floor and facade heights.

## Storefront context profiles

The procedural-building and asset systems must support more than one storefront context. Do not force all businesses to use the standalone-store proportions.

### Standalone commercial storefront

Default for convenience stores, laundromats, small cafés, gas-station shops, strip-center units, and comparable low-rise businesses.

- default storefront sill / knee wall: **18 in AFF**;
- default storefront glazing / transom top line: **9 ft AFF**;
- default entrance/transom top line: **9 ft AFF**;
- sign/fascia zone: generally **9–12 ft AFF**;
- awning mounting zone: generally **9–10 ft AFF**;
- roof deck: **12 ft**;
- parapet top: **14 ft**.

### Multistory and mixed-use ground-floor storefront

Ground-floor commercial units in multistory buildings must use the same reusable opening, door, glazing, material, and attachment concepts without assuming the standalone 12 ft shell or 9 ft storefront top line.

Approved requirements:

- the storefront system must support ground-floor retail embedded under upper stories;
- ground-floor floor-to-floor height and storefront top line are building-archetype parameters rather than one universal constant;
- storefront modules must be able to extend vertically or combine with transoms without requiring a unique wall mesh for every building;
- doors and glazing may align to a shared storefront datum chosen by that building archetype;
- generated upper-story structure must not force the ground-floor retail facade to reuse standalone parapet logic.

Exact multistory floor-to-floor presets remain a later building-archetype decision unless separately approved.

### Modern tower / skyscraper ground-floor storefront

Modern skyscraper and tower-podium commercial frontage should read as substantially more open and contemporary than the standalone-store profile.

- default modern tower storefront curb / sill: **6 in AFF**;
- glazing should be **near floor-to-ceiling** relative to the ground-floor facade zone;
- the storefront top line is controlled by the tower or podium ground-floor story height rather than the standalone 9 ft datum;
- doors may retain standard commercial leaf dimensions while using taller surrounding frames, transoms, or curtain-wall assemblies;
- curtain-wall/storefront framing should remain modular so the same basic glazing assets can be reused at different vertical spans where practical.

The 6 in curb is the approved default for modern tower storefronts. It does not replace the 18 in standalone-store sill.

## Doors

Approved default door dimensions:

- standard commercial entrance leaf: **3 ft W x 7 ft H**;
- glass storefront entrance leaf: **3 ft W x 7 ft H**;
- rear/service door: **3 ft W x 7 ft H**;
- interior commercial door: **3 ft W x 7 ft H**;
- double entrance: **6 ft total W x 7 ft H**;
- nominal door-leaf thickness: **1.75 in**;
- standalone storefront entrance transom: **2 ft H**, producing the default **9 ft** standalone assembly top line.

Taller multistory or tower facade systems may surround these leaves with taller frames/transoms without changing the basic leaf size.

## Storefront windows and glazing

### Standalone storefront family

Approved initial storefront widths:

- narrow: **4 ft**;
- standard: **6 ft**;
- wide: **8 ft**.

Standalone default:

- sill: **18 in AFF**;
- top: **9 ft AFF**;
- resulting default glass-opening height: **7 ft 6 in**.

### Modern tower storefront family

- default curb/sill: **6 in AFF**;
- glazing height is parameterized by the tower/podium ground-floor facade rather than fixed to 9 ft;
- width families may reuse the 4 ft / 6 ft / 8 ft logic where appropriate, but curtain-wall spans may also be assembled from repeated narrower bays and shared mullions.

### General non-storefront commercial window

A useful general-purpose working family may use a **3 ft sill** and **8 ft top** where that suits the building archetype. This is supporting guidance rather than a requirement that all conventional windows use that exact size.

## Storefront framing

Approved starting dimensions:

- storefront frame face width: **2–2.5 in**;
- mullion face width: **2–2.5 in**;
- storefront / door-frame depth: **4.5 in**.

Glass thickness may be visually simplified. Do not add unnecessary geometric detail merely to simulate real glazing construction.

## Facade accessories

Approved starting standards:

- standard awning projection: **3 ft**;
- large awning variant: **4 ft** where justified;
- awning vertical depth: **12–18 in**;
- box-sign depth: **6–10 in**;
- typical storefront sign height: **2–3 ft**;
- blade-sign projection: **2–3 ft**.

These are reusable authored variants, not mandatory dimensions for every building.

## Interior trim and wall dressing

Approved baseline:

- baseboard height: **4 in**;
- baseboard projection: **0.5 in**;
- typical door casing width: **2.5–3 in**;
- optional chair-rail datum where used: **36 in AFF**;
- typical wall-outlet center: **18 in AFF**;
- typical switch center: **48 in AFF**;
- typical thermostat center: **54 in AFF**.

Electrical/device placement is environmental dressing guidance, not approval of an electrical simulation system.

## Suspended ceiling system

The initial shared suspended-ceiling family should use a **2 ft x 4 ft acoustic ceiling grid** at a default **10 ft AFF** in the standalone small-commercial profile.

The family should support:

- 2 ft x 4 ft ceiling tiles;
- 2 ft x 4 ft fluorescent/troffer fixtures;
- 2 ft x 2 ft half modules where useful;
- independently socketed recessed or specialty lights where required.

Other ceiling heights and exposed-ceiling archetypes remain valid for businesses and building types that require them.

## Opening and generation rules

Approved initial generation rules:

- normal opening edges use **6 in increments** along the wall;
- normal door/window openings should remain at least **2 ft from an exterior corner** unless a specific storefront/corner system allows otherwise;
- unrelated normal openings should preserve at least **1 ft of solid wall** between them;
- adjacent storefront panes may connect through shared mullions and are exempt from the normal 1 ft separation rule;
- storefront doors may participate in continuous glazing assemblies;
- standalone storefront doors and nearby glazing should normally share the **9 ft** top datum;
- primary entrances should preserve roughly **4 ft x 4 ft** of usable clear approach space before movable fixtures are allowed to occupy it.

These are Margins generation constraints for reliable gameplay and visual composition, not real-world code claims.

## Procedural-building implications

The generator should treat architectural dimensions as data rather than baking every combination into unique meshes.

- walls are generated around declared openings rather than boolean-cut at runtime or authored in every possible window/door combination;
- storefront context is selected from the building archetype before facade assembly;
- standalone, mixed-use, and modern-tower storefronts may share doors, frames, mullions, signs, materials, and attachment concepts while using different vertical parameters;
- reusable materials provide finish variation without duplicating structural geometry;
- authored assets should expose predictable pivots and attachment transforms suitable for procedural placement;
- socket-based attached components remain independent from the 1 ft structural and 6 in fixture-placement grids;
- player Build Mode does not gain unrestricted structural wall/window editing through this decision.

## Asset-authoring implications

Before generating or modeling detailed architectural assets, use this profile to keep modules compatible.

In particular:

- do not generate a window already fused into a large wall when the wall generator should own surrounding geometry;
- do not generate standalone-store glazing in a way that prevents reuse beneath multistory/tower facades;
- prefer frame/mullion/transom systems that can be extended or repeated;
- keep material regions separable where color or finish variation is expected;
- normalize Tripo or sourced geometry in Blender to these approved dimensions before production acceptance.

## Scope boundary

This decision does not approve:

- a complete procedural-building architecture;
- arbitrary player structural editing;
- runtime boolean construction tools;
- structural-engineering simulation;
- zoning or building-code simulation;
- exact multistory or skyscraper floor-to-floor presets beyond the approved storefront-context requirements;
- a universal socket schema.

Those require separate design or technical decisions when needed.

## Reopening rule

Reopen this profile when production evidence shows that an approved dimension materially harms gameplay, procedural variety, asset reuse, navigation, visual quality, or efficient authoring. Add new building-archetype profiles rather than forcing all future architecture back into the standalone-store dimensions.