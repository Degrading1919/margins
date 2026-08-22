# Margins Building Footprint and Commercial Unit Subdivision v1.0

## Status and authority

- **Status:** Approved
- **Approved by:** Project owner
- **Approval date:** August 22, 2026
- **Scope:** Procedural building footprints, facade/frontage classification, commercial-unit subdivision, ground-floor retail allocation, business-to-unit fit, corner units, mixed-use/tower core reservations, generation order, and deterministic generation
- **Related decisions:**
  - `00_ADMIN/Decisions/Margins_Dimensional_Authoring_and_Snapping_Convention_v1.0.md`
  - `00_ADMIN/Decisions/Margins_Architectural_Dimensional_Profile_v1.0.md`
  - `00_ADMIN/Decisions/Margins_Architectural_Asset_Interface_Contract_v1.0.md`
  - `00_ADMIN/Decisions/Margins_Building_Archetype_Profiles_v1.0.md`

This decision defines how procedural buildings establish footprints and divide ground-floor space into usable commercial units. It separates parcel/city concerns from building generation and separates building geometry from the business content that occupies it.

## 1. Parcel and building-generator boundary

The building generator receives a **buildable parcel or envelope** from a higher-level city/parcel system.

The building generator does not independently invent the entire city parcel system.

This keeps:

- city/parcel generation;
- building generation;
- commercial-unit subdivision; and
- business assignment

as separable responsibilities.

## 2. Initial footprint language

Initial procedural building footprints are **orthogonal only** and resolve to the approved **1 ft structural planning grid**.

Supported initial footprint families:

- rectangle;
- L-shape;
- stepped rectangle / stepped orthogonal footprint.

Arbitrary-angle walls, curved footprints, and freeform polygonal buildings are not required for the initial system.

### Dominant default

The **rectangle** is the dominant default footprint.

L-shaped and stepped footprints provide controlled visual and spatial variety without turning the generator into a general-purpose architectural solver.

## 3. Explicit frontage classification

Each exterior or shared building face relevant to commercial generation must be classified explicitly rather than inferred from appearance alone.

Initial frontage roles:

- **Primary public frontage** — preferred storefront, public entrance, glazing, signage, and customer-facing facade;
- **Secondary public frontage** — optional additional glazing, signage, or corner exposure;
- **Rear/service frontage** — deliveries, service doors, utilities, waste, employee/service access where applicable;
- **Shared/internal face** — party wall, shared building boundary, core boundary, or otherwise non-public frontage.

The building generator must not guess which wall should become the storefront solely from mesh orientation or longest-wall heuristics when frontage metadata exists.

## 4. Standard commercial bays and merging

Ground-floor retail should be subdivided into standardized frontage bays, then adjacent compatible bays may be merged to accommodate larger businesses.

Approved preferred frontage widths:

- **20 ft**;
- **24 ft**;
- **30 ft**;
- **36 ft**;
- **40 ft**.

These are preferred dimensional presets, not a rule that every building face must divide perfectly into only these values. Residual/filler conditions may be handled by archetype-specific logic when necessary.

Larger businesses should normally occupy **merged adjacent bays** rather than forcing arbitrary one-off unit dimensions.

Example:

- one 24 ft bay can support a small cafe or narrow retailer;
- two compatible 24 ft bays can combine into a 48 ft unit for a larger laundromat or comparable business.

## 5. Business requirements select or merge units

Building archetype and business type remain independent.

A business definition may declare spatial requirements such as:

- minimum frontage;
- minimum usable floor area;
- acceptable depth range;
- required public entrance access;
- service access requirements;
- required equipment/fixture clearance;
- loading or delivery needs;
- other bounded business-specific spatial constraints.

The generator selects an existing compatible unit or merges adjacent bays until those requirements are satisfied.

Do not distort the building or silently remove business requirements merely to force a business into an unsuitable unit.

## 6. Typical frontage-to-rear units

For strip-center and older Main Street-style buildings, commercial units should normally extend from public frontage toward the rear of the available ground-floor depth.

Exceptions are allowed when the building archetype reserves:

- rear service corridors;
- shared utility/service zones;
- vertical circulation;
- lobby/core space;
- other explicitly defined shared building areas.

## 7. Mixed-use and tower ground-floor reservations

Mixed-use and tower buildings reserve required non-retail building areas **before** commercial-unit subdivision.

Potential reserved ground-floor areas include:

- building lobby;
- stair/elevator-core implication;
- mechanical/service zones;
- common access corridors;
- other building-wide functions required by the archetype.

Retail/commercial units occupy the remaining suitable perimeter frontage.

A ground-floor business does not implicitly own or occupy the entire ground floor of a multistory building or skyscraper.

Detailed upper-floor interiors and complete circulation-core simulation remain outside this decision unless gameplay later requires them.

## 8. Corner-unit behavior

Corner buildings may expose both a primary and secondary public frontage.

A corner commercial unit may therefore support:

- storefront glazing on two exterior faces;
- signage on both public faces;
- facade treatment on both public faces;
- one designated primary public entrance, with additional entrances optional rather than required.

Corner exposure is a property/building-layout condition, not a separate business category.

## 9. Commercial-unit validity requirements

Every generated commercial unit must satisfy baseline validity checks before business assignment.

Required checks:

- one contiguous usable floor area;
- at least one valid public-facing entrance location;
- reachable interior space from the public entrance;
- required business footprint and clearances can fit when a business is assigned;
- no overlapping or contradictory wall/opening geometry;
- no inaccessible leftover floor islands treated as usable business area;
- service access is present only when required and available;
- reserved building-core/common space remains distinct from tenant space.

These are game-generation validity constraints, not claims of real-world building-code compliance.

## 10. Generation order

Commercial subdivision occurs before detailed interior layout and fixture dressing.

Approved high-level order:

1. parcel/buildable envelope input;
2. building archetype selection;
3. building footprint generation;
4. frontage classification;
5. shared/core/service-space reservation where required;
6. commercial-unit subdivision;
7. business assignment and bay merging as required;
8. storefront/opening generation;
9. interior partition/layout generation;
10. business fixtures/equipment placement;
11. decorative/environmental dressing;
12. validation and repair/rejection if generation rules fail.

This order prevents detailed interior content from becoming authoritative before the building and tenant envelope are valid.

## 11. Deterministic generation

Procedural building footprint and unit subdivision must be deterministic from explicit generation inputs and a seed.

For the same:

- generator version;
- building archetype;
- parcel/envelope;
- business requirements;
- configuration data; and
- seed,

the system should produce the same footprint and commercial-unit layout.

This requirement exists to support:

- persistence and save reconstruction;
- debugging;
- regression testing;
- reproducible competitor locations;
- content validation;
- safe agent-assisted development.

If generation algorithms or authoritative configuration change in a way that would alter existing generated properties, persistence/versioning behavior must be handled deliberately rather than silently regenerating occupied locations into a different layout.

## Design principle

The generator should create **believable reusable commercial space first**, then fit business content into that space.

It should not require a unique building model for each business type, and it should not treat every commercial unit as an arbitrary one-off dimension.

Standardized bays, mergeable units, explicit frontage roles, and archetype-specific shared-space reservations are the approved foundation for controlled variety.

## Scope boundary

This decision does not approve:

- a complete city parcel-generation algorithm;
- curved or arbitrary-angle procedural footprints;
- unrestricted player structural editing;
- detailed building-code, egress, structural-engineering, plumbing, electrical, or zoning simulation;
- fully simulated upper-floor interiors by default;
- a final business spatial-requirements schema;
- a final procedural interior-layout algorithm;
- automatic regeneration of persisted properties when generator rules change.

Those remain separate design or technical decisions when required.

## Reopening rule

Reopen this decision when implementation or playtest evidence shows that orthogonal footprint families, standard commercial bays, frontage classification, core/service reservations, deterministic generation, or the approved generation order materially constrain business variety, city believability, navigation, persistence, performance, or content-production efficiency.