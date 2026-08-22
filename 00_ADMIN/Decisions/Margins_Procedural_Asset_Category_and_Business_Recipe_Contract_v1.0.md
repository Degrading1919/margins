# Margins Procedural Asset Category and Business Recipe Contract v1.0

## Status and authority

- **Status:** Approved
- **Approved by:** Project owner
- **Approval date:** August 22, 2026
- **Scope:** Procedural business-asset categorization, capability tags, placement traits, generic category behavior, business recipes, quantity scaling, asset selection, and procedural-placement responsibility boundaries
- **Related decisions:**
  - `00_ADMIN/Decisions/Margins_Dimensional_Authoring_and_Snapping_Convention_v1.0.md`
  - `00_ADMIN/Decisions/Margins_Architectural_Dimensional_Profile_v1.0.md`
  - `00_ADMIN/Decisions/Margins_Architectural_Asset_Interface_Contract_v1.0.md`
  - `00_ADMIN/Decisions/Margins_Building_Archetype_Profiles_v1.0.md`
  - `00_ADMIN/Decisions/Margins_Building_Footprint_and_Commercial_Unit_Subdivision_v1.0.md`
  - `00_ADMIN/Decisions/Margins_Procedural_Interior_Layout_Rules_v1.0.md`

This contract defines the approved abstraction for procedurally placing business assets across Margins. It exists to prevent business-specific placement code from multiplying as the business roster grows.

## Core abstraction

Every procedural business asset has:

1. **one primary procedural category**, which supplies default placement behavior;
2. **zero or more capability tags**, which describe what business role the asset can satisfy;
3. **placement traits**, which describe the asset's spatial, interaction, mounting, and assembly behavior.

Business recipes request **category + capability requirements** rather than exact prefab names or exact coordinates.

The procedural engine applies behavior in this order:

`business recipe -> category defaults -> asset-specific traits -> hard-constraint placement -> soft scoring -> validation`

## One primary category per asset

Each procedural business asset has exactly one primary category.

Do not model broad behavioral traits as additional categories merely because an asset is wall-mounted, exterior, customer-operated, repeatable, or socket-mounted. Those are represented through traits.

Examples:

- a refrigerated merchandise case may be `Display` with capability `ColdMerchandise`, a strong wall preference, and front customer access;
- a fuel pump may be `ProcessStation` with capability `FuelDispense`, exterior-pad mounting, and customer operation;
- a washer may be `ProcessStation` with capability `LaundryWash`, front interaction clearance, wall preference, and repeatable-bank behavior.

## Initial primary categories

The approved initial category vocabulary is:

### Display

Purpose: presents products, goods, or resources for customer selection.

Typical examples:

- gondola shelving;
- wall shelving;
- refrigerated merchandise cases;
- freestanding product displays.

Default behavior:

- prefer `Public` zones;
- preserve customer-accessible interaction faces;
- preserve required aisle circulation;
- may form rows, banks, or repeated runs when the asset supports that behavior.

### Transaction

Purpose: supports checkout, payment, reception, order intake, or transaction handoff.

Typical examples:

- checkout counters;
- reception desks;
- service registers.

Default behavior:

- prefer the `Transaction` zone or the boundary between `Transaction` and `Public`;
- preserve customer access;
- preserve staff access when the asset requires a staff side;
- preserve queue clearance when the business or asset requires a queue.

### WorkSurface

Purpose: provides a staff work surface or hosts socket-mounted equipment.

Typical examples:

- preparation counters;
- service counters;
- workbenches.

Default behavior:

- prefer `Work` or `Transaction` zones;
- preserve staff working clearance;
- expose compatible sockets/anchors when authored;
- may support attached process or transaction equipment without becoming that equipment's primary category.

### ProcessStation

Purpose: performs, transforms, dispenses, or processes something as part of the business operation.

Typical examples:

- washers;
- dryers;
- espresso machines;
- fuel pumps;
- comparable operating equipment.

Default behavior:

- prefer `Work` or `Public` according to access traits and business needs;
- preserve required operator or customer interaction clearance;
- may repeat or form banks when the asset supports that behavior.

### UseStation

Purpose: is occupied or directly used by a customer or employee for a period of time.

Typical examples:

- computer stations;
- arcade cabinets;
- copiers or comparable customer-use equipment.

Default behavior:

- usually prefer `Public` unless restricted by traits;
- preserve occupancy and interaction clearance;
- preserve a valid route into and out of the station;
- may repeat in rows or banks when supported.

### Storage

Purpose: holds inventory, supplies, back stock, or other business resources awaiting use.

Typical examples:

- stock racks;
- storage cabinets;
- backroom shelving.

Default behavior:

- prefer `Storage` or `Staff` zones;
- require staff accessibility unless explicitly authored otherwise;
- may form dense repeated runs when circulation and access remain valid.

### Service

Purpose: supports receiving, maintenance, waste handling, operational support, or comparable non-customer functions.

Typical examples:

- receiving staging areas;
- waste stations;
- maintenance/support fixtures.

Default behavior:

- prefer `Service` or `Staff` zones;
- remain outside primary customer circulation unless the asset or business requires public access;
- preserve maintenance, receiving, or staff access clearances declared by the asset.

### Amenity

Purpose: supports waiting, comfort, or convenience without being a core operating station.

Typical examples:

- chairs;
- waiting benches;
- customer tables;
- comparable non-core seating/furnishing.

Default behavior:

- normally prefer `Public` or `Staff` according to access traits;
- place only after core operational assets and required circulation are valid;
- preserve occupancy and circulation clearance.

### Decor

Purpose: provides visual dressing without a required operational function.

Typical examples:

- plants;
- posters;
- bulletin boards;
- decorative signs;
- comparable dressing assets.

Default behavior:

- lowest procedural-placement priority;
- may use available wall, floor, ceiling, or socket positions according to traits;
- must never invalidate required circulation, interaction, or operational clearances.

## Capability tags

Capability tags describe **what business role an asset can satisfy**. They do not contain procedural placement logic.

Examples include, but are not limited to:

- `DryMerchandise`;
- `ColdMerchandise`;
- `Payment`;
- `Receiving`;
- `LaundryWash`;
- `LaundryDry`;
- `BeveragePrep`;
- `CoffeePrep`;
- `ComputerUse`;
- `FuelDispense`;
- `CustomerSeating`;
- `InventoryStorage`;
- `WasteHandling`.

Capabilities should be data-defined identifiers rather than requiring a growing universal hard-coded enum whenever a new business introduces a new business role.

Adding a new capability must not require changing the generic placement engine unless that capability reveals a genuinely new spatial behavior that cannot be represented by the existing category and trait system.

## Placement traits

Asset-specific traits modify category defaults and describe actual spatial behavior.

Only traits relevant to a given asset need to be present.

### Mounting traits

Supported concepts may include:

- `Floor`;
- `Wall`;
- `Ceiling`;
- `Socket`;
- `ExteriorPad`.

Mounting traits use the approved pivot, host-surface, socket, anchor, and exact-offset conventions from the architectural asset-interface contract.

### Access traits

Supported concepts:

- customer access;
- staff access;
- both customer and staff access.

### Interaction traits

Assets may declare:

- interaction side or sides;
- interaction clearance;
- no-block volumes;
- occupancy clearance;
- operator side;
- customer side;
- other required approach volumes.

### Surface relationship traits

Supported concepts:

- wall required;
- wall strongly preferred;
- wall preferred;
- neutral;
- wall avoided.

These are placement modifiers rather than separate primary categories.

### Environment traits

Supported concepts:

- indoor only;
- outdoor only;
- indoor or outdoor.

### Assembly traits

Supported concepts may include:

- independent;
- linear run;
- bank/repeated row;
- socket child.

The existing `Fixed`, `Repeatable`, and `Stretch-safe` classifications and approved extension-anchor conventions remain authoritative and are not duplicated by this contract.

## Business recipes

A business recipe is a lightweight composition definition rather than a procedural floor-plan script.

A recipe may declare:

### Required and optional functional zones

Using the approved shared vocabulary:

- `Public`;
- `Transaction`;
- `Work`;
- `Storage`;
- `Service`;
- `Staff`;
- `Circulation`.

### Asset requests

An asset request should be able to express, where relevant:

- primary category;
- required capability tag or tags;
- minimum quantity;
- preferred quantity;
- maximum quantity;
- required or optional status;
- preferred host zone;
- request priority;
- other bounded composition preferences that do not encode exact coordinates or bespoke placement algorithms.

Recipes do not request exact prefab names and do not prescribe exact world coordinates.

## Quantity scaling

The approved quantity model is:

- **Minimum** — below this quantity the layout is invalid when the request is required;
- **Preferred** — the generator should try to reach this quantity when space and validity allow;
- **Maximum** — the generator must stop adding instances for this request after this quantity.

Available valid space determines how far generation progresses from Minimum toward Preferred and Maximum.

This supports differently sized businesses without requiring separate hard-coded floor plans for small, medium, and large locations.

## Asset-selection rule

Business recipes must select assets indirectly through category and capability requirements.

For example, a recipe requests:

`ProcessStation + LaundryWash`

rather than:

`Washer_Model_03`.

Candidate assets may then be filtered or scored using data such as:

- required capability;
- primary category;
- physical fit;
- indoor/outdoor compatibility;
- available mounting condition;
- business/content availability;
- style/variation pool;
- asset-specific clearances and interaction requirements;
- deterministic seed.

Adding a new compatible asset should not require editing every business recipe that can use it.

## Example recipe composition

A laundromat recipe may request conceptually:

- `ProcessStation + LaundryWash`, minimum 4, preferred 8, maximum 20;
- `ProcessStation + LaundryDry`, minimum 4, preferred 8, maximum 20;
- `Transaction + Payment`, minimum 1;
- `Amenity + CustomerSeating`, minimum 1, preferred 3;
- `Storage + InventoryStorage`, minimum 1.

The laundromat recipe does not need to encode whether a particular washer prefers a wall, how much front clearance it needs, or whether it can form a bank. That information belongs to the washer asset's procedural metadata.

This example demonstrates the contract and is not itself approval of the final laundromat recipe.

## Placement responsibility boundary

### Category rules own generic behavior

Examples:

- Displays preserve customer access and aisle circulation;
- Transactions preserve transaction/customer/staff access;
- ProcessStations preserve operational interaction clearances;
- Storage remains staff-accessible;
- Decor yields to operational requirements.

### Asset metadata owns asset-specific behavior

Examples:

- exact footprint;
- exact interaction side;
- wall preference;
- mounting mode;
- sockets and anchors;
- required clearances;
- repeatability or bank behavior.

### Business recipes own composition

Examples:

- which categories/capabilities are required;
- quantity ranges;
- which zones the business needs;
- business-level weighting and composition preferences.

Business recipes should not absorb generic asset-placement behavior, and assets should not contain business-specific floor-plan instructions.

## Operational placement priority

Core operational assets are placed and validated before optional amenities and decorative dressing.

Preferred high-level order:

1. required operational requests;
2. required circulation and interaction validation;
3. optional operational requests toward Preferred quantities;
4. amenities;
5. decor/dressing;
6. final validation.

Amenities and decor may not displace required operational content or invalidate required circulation.

## Exception rule

Do not add business-specific placement code by default.

A bespoke business-specific placement rule may be introduced only when implementation or playtest evidence demonstrates a genuinely irreducible case that cannot be represented cleanly through:

- existing category behavior;
- asset-specific traits;
- capability tags;
- business recipe composition;
- generic hard/soft layout constraints.

When such an exception is required, keep it narrow and document why the shared abstraction was insufficient.

## Generator implications

The intended data flow is:

`business recipe -> required zones -> asset requests -> compatible asset candidates -> category defaults -> asset-specific traits -> hard-constraint placement -> soft scoring -> deterministic selection -> validation`

This contract extends rather than replaces the approved procedural interior-layout rules.

## Scope boundary

This decision does not approve:

- the final capability-tag catalog for every Margins business;
- final business recipes for the complete business roster;
- exact implementation classes, ScriptableObject schemas, enums, serialization formats, or editor tooling;
- one universal hard-coded capability enum;
- business-specific floor-plan scripts by default;
- exact prefab selection in business recipes;
- duplication of existing dimensional, socket, anchor, circulation, or architectural contracts;
- a general-purpose procedural interior-design system beyond Margins' business-generation needs.

Those items should be added only where needed to implement and validate the approved abstraction.

## Reopening rule

Reopen this contract when implementation or playtest evidence shows that the one-primary-category model, initial nine-category vocabulary, capability/trait separation, quantity scaling, or business-recipe abstraction materially harms business variety, asset reuse, generation validity, authoring efficiency, performance, or explainability.