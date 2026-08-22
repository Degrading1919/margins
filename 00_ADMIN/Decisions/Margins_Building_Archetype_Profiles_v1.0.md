# Margins Building Archetype Profiles v1.0

## Status and authority

- **Status:** Approved
- **Approved by:** Project owner
- **Approval date:** August 22, 2026
- **Scope:** Procedural commercial-building archetypes, ground-floor retail shells, upper-story dimensional profiles, storefront context selection, business-to-building separation, and externally generated upper-floor scope
- **Related decisions:**
  - `00_ADMIN/Decisions/Margins_Dimensional_Authoring_and_Snapping_Convention_v1.0.md`
  - `00_ADMIN/Decisions/Margins_Architectural_Dimensional_Profile_v1.0.md`
  - `00_ADMIN/Decisions/Margins_Architectural_Asset_Interface_Contract_v1.0.md`

This decision defines the first approved building-archetype profiles for procedural city and business-location generation. It does not define the complete procedural-building implementation.

## Core separation: building archetype and business type

**Building archetype and business type are separate selections.**

The building archetype defines the available shell, facade context, story profile, frontage, structural dimensions, roofline, and ground-floor commercial-unit envelope.

The business type defines the operational and interior requirements that must fit inside an available commercial unit.

A business such as a coffee shop may therefore appear in a standalone building, strip center, older Main Street mixed-use building, modern mixed-use building, or tower podium when the selected unit satisfies that business's requirements.

The intended composition is:

`building archetype + available commercial unit + business requirements = generated business location`

Do not bind one business category to one architectural archetype unless a later approved business rule explicitly requires it.

## Archetype 1: standalone small commercial

This profile remains aligned with the approved architectural dimensional profile.

Typical uses include convenience stores, laundromats, small cafes, gas-station shops, and comparable low-rise standalone businesses.

Approved baseline:

- exterior structural wall / roof-deck height: **12 ft**;
- default finished interior ceiling height: **10 ft AFF**;
- default parapet top: **14 ft**;
- default storefront sill / knee wall: **18 in AFF**;
- default storefront top line: **9 ft AFF**;
- normally **1 story**;
- flat roof is the default roof type;
- entrance may be centered or offset;
- exact footprint varies under the approved structural planning grid and later footprint-generation rules.

## Archetype 2: strip-center / inline retail

This profile supports shopping-center and inline-tenant construction where multiple businesses share one continuous building mass.

Approved baseline:

- structural height: **14 ft**;
- default finished interior ceiling: **12 ft AFF**;
- default parapet top: **16 ft**;
- default storefront sill: **18 in AFF**;
- default storefront top line: **10 ft AFF**;
- typical tenant frontage: approximately **20-50 ft**;
- typical tenant depth: approximately **40-80 ft**;
- adjacent tenants may share side walls;
- public storefronts normally face the customer frontage side;
- rear service doors should be supported where the site layout permits;
- multiple tenants may share a continuous roof and parapet while retaining independent facade, signage, material, and business identity.

Tenant dimensions are archetype ranges, not requirements to generate arbitrary fractional dimensions. Weighted standard widths should be preferred under the dimensional-variation rule below.

## Archetype 3: older Main Street mixed-use

This profile represents older American downtown or neighborhood commercial buildings with ground-floor storefronts and offices, apartments, or other uses above.

Approved baseline:

- ground-floor floor-to-floor height: **14 ft**;
- ground-floor storefront sill: **12-18 in AFF**;
- typical ground-floor storefront top: approximately **10-11 ft AFF**;
- upper-floor floor-to-floor height: **10 ft**;
- typical total stories: **2-4**;
- typical lot/frontage width: approximately **18-40 ft**;
- buildings generally meet or sit close to the public sidewalk/frontage;
- masonry-heavy facade treatments are appropriate;
- upper-story windows should usually follow coherent vertical alignment and repeated facade rhythm;
- cornice or parapet roof termination is appropriate;
- rear or service access may be generated where the lot permits.

This archetype is intended to provide older urban fabric and prevent the city from reading as uniformly suburban or recently developed.

## Archetype 4: modern mixed-use / mid-rise

This profile represents newer apartments, offices, hotels, or similar uses above active ground-floor commercial frontage.

Approved baseline:

- ground-floor floor-to-floor height: **16 ft**;
- default storefront curb / sill: **6 in AFF**;
- **12 in** curb variants may be used for less-glassy facade treatments;
- typical glazing top: approximately **13-14 ft AFF**;
- upper-floor floor-to-floor height: **10 ft**;
- typical total stories: **4-8**;
- storefront bays may be broader and more highly glazed than older Main Street frontage;
- repeated upper-floor facade modules are encouraged;
- upper-floor setbacks, balconies, or similar articulation may be used as compatible archetype variants;
- ground-floor commercial units remain independently configurable from the upper-story facade.

## Archetype 5: modern tower / skyscraper podium

This profile represents modern tower and skyscraper ground floors, including retail within podiums.

Approved baseline:

- ground-floor floor-to-floor height: **18 ft**;
- default storefront curb / sill: **6 in AFF**;
- typical ground-floor glazing top: approximately **16 ft AFF**;
- standard commercial door leaves may remain **7 ft** tall while taller transoms, frames, or curtain-wall assemblies occupy the remaining facade height;
- podium floor-to-floor height above the ground floor: **14 ft** where podium floors are used;
- typical tower floor-to-floor height: **12 ft**;
- a variable podium of roughly **1-5 floors** may precede the tower mass;
- tower mass may step inward above the podium;
- high glazing percentages and repeated curtain-wall bays are appropriate;
- retail frontage normally occupies only portions of the ground-floor podium rather than defining the entire tower footprint.

The tower profile must remain modular enough that ground-floor businesses can use the same door, mullion, glazing, sign, and attachment concepts as other archetypes while using taller vertical facade parameters.

## Upper-floor generation and simulation scope

Upper floors should be generated sufficiently to make multistory buildings visually and architecturally coherent, but they do **not** automatically receive fully simulated interiors.

For non-active upper floors, procedural generation may create:

- exterior facade geometry;
- windows and facade repetition;
- floor slabs or equivalent visual separation where required;
- roof and roofline geometry;
- stair/elevator-core implication or externally coherent access treatment where visually necessary;
- other architectural detail required for a believable exterior silhouette.

Detailed interiors, fixtures, navigation, NPC simulation, furnishing, and business-state simulation are generated only when gameplay requires access to or simulation of that floor.

This preserves the ability to make a complete multistory city while avoiding unnecessary detailed simulation and furnishing cost.

A later property-development or ownership feature may expose additional floors for detailed use without requiring every generated building to begin fully interiorized.

## Dimensional variation rule

Building archetypes should use **weighted dimensional presets and bounded ranges**, not unlimited arbitrary random dimensions.

Prefer dimensions that reflect repeated real-world construction conventions and produce compatible modular assembly.

For example, an older Main Street frontage may strongly favor widths such as:

- **20 ft**;
- **24 ft**;
- **30 ft**;
- **36 ft**.

Depth may vary more freely on the approved **1 ft structural planning grid** where the site and business requirements permit.

The purpose is to produce believable repetition and variation without creating difficult one-off geometry or meaningless dimensions such as 27 ft 3 in merely for randomness.

## Procedural-generation implications

The generator must select or resolve a building archetype before facade and commercial-unit generation.

Archetype selection controls at least:

- story profile;
- ground-floor facade context;
- allowable storefront sill and top-line behavior;
- roof/parapet treatment;
- typical frontage and depth ranges;
- upper-floor generation behavior;
- whether commercial units are standalone, inline, or embedded beneath upper stories.

Business placement occurs after a compatible commercial-unit envelope exists or as part of a constrained co-generation step. The business may request required floor area, frontage, access, equipment zones, service access, or other approved needs, but it does not redefine the building archetype arbitrarily.

## Scope boundary

This decision does not yet approve:

- the final footprint-generation algorithm;
- parcel or lot-generation rules;
- the final commercial-unit subdivision algorithm;
- corner-lot behavior;
- irregular or non-rectangular building footprints;
- tower massing algorithms beyond the approved podium/tower profile;
- player structural editing;
- automatic detailed interiors for all upper floors;
- zoning, code, structural-engineering, utility, elevator, or fire-safety simulation;
- a final district-to-archetype weighting model.

Those are separate decisions or implementation steps.

## Reopening rule

Reopen an archetype profile when production evidence shows that its approved dimensions or generation behavior materially harms visual variety, business compatibility, gameplay, navigation, asset reuse, performance, or city composition. Prefer adding or refining archetypes over collapsing distinct urban forms into one universal building profile.
