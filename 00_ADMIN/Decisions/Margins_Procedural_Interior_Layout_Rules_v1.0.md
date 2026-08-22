# Margins Procedural Interior Layout Rules v1.0

## Status and authority

- **Status:** Approved
- **Approved by:** Project owner
- **Approval date:** August 22, 2026
- **Scope:** Procedural commercial interior generation, functional zoning, interior partitions, circulation, fixture placement order, fixture metadata, layout validation, and deterministic layout selection
- **Related decisions:**
  - `00_ADMIN/Decisions/Margins_Dimensional_Authoring_and_Snapping_Convention_v1.0.md`
  - `00_ADMIN/Decisions/Margins_Architectural_Dimensional_Profile_v1.0.md`
  - `00_ADMIN/Decisions/Margins_Architectural_Asset_Interface_Contract_v1.0.md`
  - `00_ADMIN/Decisions/Margins_Building_Archetype_Profiles_v1.0.md`
  - `00_ADMIN/Decisions/Margins_Building_Footprint_and_Commercial_Unit_Subdivision_v1.0.md`

This decision defines the approved generation order and constraints for procedural business interiors. It is intentionally a functional layout contract rather than a universal interior-design AI system.

## Core generation principle

Procedural interiors are generated from functional requirements first. The approved order is:

`commercial unit -> business requirements -> required/optional zones -> zone adjacency -> interior partitions/openings -> required circulation -> large functional equipment -> counters/shelving/fixtures -> small props/decor -> validation`

Do not begin with random fixture placement and attempt to infer a coherent store afterward.

## Interior generator inputs

The interior generator receives an already-defined commercial unit with, where applicable:

- unit footprint;
- public frontage or frontages;
- designated primary entrance;
- optional service entrance;
- reserved building/core areas;
- business type or business requirement profile;
- deterministic generation seed.

The interior generator does not alter the exterior building footprint merely to force a business to fit.

## Business requirements describe needs, not one fixed floor plan

A business definition supplies functional and spatial requirements rather than a single hard-coded layout.

Requirements may include:

- required and optional functional zones;
- minimum usable floor area;
- minimum frontage;
- required fixture or equipment categories;
- adjacency preferences;
- service-access requirements;
- wall-placement preferences;
- customer, staff, storage, or work-space needs.

The same business type should be capable of producing multiple valid interior layouts across compatible commercial units.

## Shared functional zone vocabulary

Initial shared zone concepts are:

- **Public** - customer-accessible sales, seating, service, or browsing areas;
- **Transaction** - checkout, payment, reception, or equivalent transaction area;
- **Work** - business-operating equipment or staff production activity;
- **Storage** - inventory, supplies, back stock, or comparable stored resources;
- **Service** - utility, receiving, maintenance, waste, or support functions;
- **Staff** - employee-only support areas where required;
- **Circulation** - required routes and transition space.

These are reusable functional concepts rather than business-specific room names. Business presentation and equipment determine how a zone manifests.

## Open versus enclosed zones

A functional zone does not automatically require an enclosed room.

Examples:

- public sales floor may remain open;
- checkout or transaction space may remain open;
- back stock may be enclosed;
- employee office may be enclosed;
- coffee-prep work space may remain open behind a counter;
- restrooms, where present, are enclosed.

Interior partitions are generated only where functional requirements justify separation.

## Circulation-first rule

Required circulation is established before normal fixture placement.

Approved gameplay-generation clearances are:

- primary customer route: **4 ft preferred clear width**;
- normal customer aisle: **3 ft minimum clear width**;
- staff/service route: **3 ft minimum clear width**;
- primary entrance approach/landing: retain the previously approved roughly **4 ft x 4 ft** usable clear area;
- checkout queue lane: roughly **3 ft clear width** where a queue is required.

These are Margins gameplay-generation standards, not claims of real-world code compliance.

A fixture placement is invalid when it breaks required circulation or blocks required interaction access.

## Entrance arrival zone

The primary entrance establishes a protected arrival/readability area inside the business.

- preserve roughly **6 ft of depth** from the primary entrance before aggressively placing major fixtures;
- major equipment and large fixtures should not immediately obstruct the entry path or sightline;
- small dressing elements such as baskets, compact displays, signs, or similar props may use portions of this area when validation confirms circulation remains clear.

## Transaction-zone placement preference

For businesses with a checkout, reception, or transaction counter:

- prefer the front portion of the unit;
- prefer reasonable visibility from the entrance and primary customer circulation;
- prefer proximity to customer exit flow;
- do not obstruct the entrance or required arrival zone;
- place queue space into usable interior area rather than through the doorway.

These are weighted preferences rather than a fixed rule that every checkout must occupy the same corner or orientation.

## Storage and service placement preference

Where applicable:

- stockrooms and service areas normally prefer rear or non-public frontage;
- adjacency to a valid service entrance receives high preference when one exists;
- back-of-house size is determined by business requirements rather than one universal floor-area percentage;
- alternate locations remain valid when the unit footprint prevents a rear arrangement.

Do not enforce a universal rule such as "backroom equals 20 percent of floor area."

## Interior partition rules

Generated interior partitions follow the approved architectural dimensional system:

- nominal interior partition thickness: **4.5 in**;
- coarse partition endpoints use the approved **1 ft structural planning increment** where applicable;
- doors and normal openings locate along partitions using the approved **6 in opening-placement increment**;
- exact wall faces and perpendicular mounting offsets remain derived from actual wall geometry rather than quantized to those grids.

## Fixture-placement order

Normal fixture and prop placement occurs after zone topology, partitions, openings, and required circulation are established.

Preferred order:

1. large functional equipment;
2. counters, shelving, major fixtures, and business-critical stations;
3. secondary fixtures and supporting equipment;
4. small props, wall dressing, signage, and decorative variation;
5. final validation.

## Procedural fixture metadata

A procedural fixture should be able to declare, where relevant:

- physical footprint;
- authored facing direction;
- interaction side or sides;
- required interaction clearance;
- preferred host zone;
- allowed mounting mode such as floor, wall, ceiling, or socket;
- required or preferred adjacency;
- placement or compatibility classification;
- any no-block or access volume required for gameplay.

The generator should not treat a fixture as only an undifferentiated bounding box when gameplay depends on which side is usable.

## Hard constraints and soft scoring

Layout generation separates hard validity constraints from soft quality preferences.

### Hard constraints

A candidate layout must satisfy, where applicable:

- all required zones exist;
- required fixtures or equipment fit;
- required circulation remains traversable;
- the primary entrance remains valid and reachable;
- no illegal fixture or architectural overlap occurs;
- required interaction positions remain reachable;
- required service or staff routes remain usable;
- no inaccessible leftover floor islands are created.

### Soft scoring

Among valid candidates, score or prefer layouts based on criteria such as:

- preferred zone adjacency;
- useful public frontage;
- storage near service access;
- checkout visibility;
- efficient customer/staff circulation;
- sensible wall use;
- visual variety;
- business-specific presentation preferences.

The deterministic seed may select among sufficiently strong valid candidates so procedural variety does not require accepting invalid layouts.

## Explicit failure behavior

When a commercial unit cannot satisfy a business requirement profile, generation must reject that business/unit pairing rather than silently degrading required functionality.

Do not force a fit by:

- shrinking required aisles below the approved minimum;
- deleting required zones;
- deleting required business-critical equipment;
- distorting fixed assets;
- blocking entrance or interaction clearances;
- generating inaccessible rooms or floor islands.

City/business assignment should choose a different compatible unit or configuration instead.

## Generated layout as initial state

A procedurally generated layout is an initial business state, not a permanently immutable special class of store.

- generated competitor locations may retain their generated layouts during normal simulation;
- when design scope allows a property to become player-editable, the generated layout may become the starting state for the same approved Build Mode and property-development systems;
- this decision does not by itself approve unrestricted structural editing.

## Determinism and validation

Interior generation should be deterministic from the same authoritative inputs and generation seed.

The generator must retain enough structured data to support:

- save/load stability;
- reproducible bugs;
- automated layout validation;
- regression testing;
- later regeneration or migration only when explicitly controlled.

## Scope boundary

This decision does not approve:

- one universal business-specific floor-plan solver;
- arbitrary player structural editing;
- real-world building-code simulation;
- utility-planning simulation;
- random fixture placement without functional zoning;
- deletion or compression of hard requirements merely to make a business fit;
- complete business requirement profiles for every Margins business.

Those profiles and implementation details are separate follow-on decisions.

## Reopening rule

Reopen these rules when production evidence shows that the approved zone vocabulary, circulation widths, placement order, fixture metadata, validation behavior, or hard/soft constraint separation materially harms gameplay, procedural variety, performance, authoring efficiency, or business-specific authenticity.
