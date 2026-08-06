# Margins 3D Asset Technical Budgets

## Status and authority

- **Status:** Approved production ceilings for the current Stylized Contemporary Americana asset pipeline
- **Authority:** Project-owner-approved 3D model, LOD, collision, character, vehicle, product, and vegetation constraints
- **Scope:** Shared commercial and city assets plus the coffee shop, convenience store, gas station, and laundromat model catalogs
- **Interpretation:** Every triangle figure is a maximum LOD0 ceiling, not a target an artist or generation tool should fill
- **Pipeline boundary:** Raw Tripo, MPFB2, Blender, purchased, open-source, or other generated outputs are intake material. They are not accepted production assets until measured, normalized, reviewed, and brought within the applicable ceilings.

The building-shell figure means one assembled business shell. It does not mean the sum of every mesh in the complete modular library.

## General production rules

### Required asset record

Before an asset is accepted for production use, its record must specify:

- LOD0 triangle ceiling;
- LOD1 triangle ceiling;
- LOD2 triangle ceiling;
- collider type: primitive, compound primitive, convex mesh, or static mesh;
- collision triangle ceiling when a mesh collider is justified;
- maximum material slots;
- maximum texture resolution;
- expected maximum visible instances;
- expected closest viewing distance;
- animation or interaction requirements;
- source and licensing status;
- AI involvement;
- normalization and technical-review status;
- for characters: maximum bones, maximum influences per vertex, maximum skinned mesh renderers, and maximum material slots for the complete assembled character; and
- for vegetation: alpha-tested material count and billboard, impostor, or other final-distance representation.

### Collider policy

Default to primitive or compound-primitive colliders.

- Use primitive colliders for simple props.
- Use compound primitives for shelves, counters, vehicles, machines, and interactive fixtures.
- Use convex mesh colliders only when compound primitives cannot represent the required interaction.
- Use static mesh colliders only for nonmoving environment geometry where simpler collision is inadequate.
- Detailed mesh colliders are exceptions, especially for shelves, counters, buildings, and machines.

### Starting LOD policy

- Unique hero machines: LOD1 at 50–60% of LOD0; LOD2 at 20–30%.
- Repeated fixtures: LOD1 at 40–50% of LOD0; LOD2 at 15–20%.
- Close-view vehicles: 20,000–30,000 LOD0; 8,000–12,000 LOD1; 2,000–4,000 LOD2.
- Complete visible characters: 15,000–22,000 LOD0; 7,000–10,000 LOD1; 2,500–4,000 LOD2.
- Repeated products should normally use one low-cost mesh with texture, dimension, and material variants. Separate LODs are not required unless profiling demonstrates a benefit.

These ratios are starting defaults. Profiling and screen-space evaluation may justify stricter asset-specific limits.

## Character assembly ceiling

The current approved starting ceiling is **18,000 LOD0 triangles for one complete visible character**. This total includes:

- body;
- currently equipped clothing;
- hair;
- shoes; and
- visible accessories.

Clothing, hairstyle, hat, shoe, and accessory module ceilings are contained within the 18,000-triangle assembled-character ceiling. They are not additive allowances. No assembled character variant may exceed 18,000 triangles merely because its individual modules remain below their own ceilings.

Current starting character constraints:

- complete visible LOD0 character: 18,000 triangles maximum;
- maximum bones: 80;
- maximum influences per vertex: 4;
- maximum skinned mesh renderers: 4; and
- maximum material slots for the complete character: 4.

The 18,000-triangle ceiling remains provisional pending measurement of the helper-stripped MPFB2 export. Any revision requires project-owner approval after Opus or another reviewed pipeline step records the actual body, clothing, hair, shoe, accessory, bone, renderer, and material costs.

## Vehicle reuse rule

A 12,000-triangle vehicle should normally be either:

- the LOD1 of a close-view vehicle; or
- a genuinely different low-cost silhouette used for background traffic.

Do not build both a 24,000-triangle close-view vehicle and a nearly identical standalone 12,000-triangle vehicle unless the additional visual variation justifies maintaining both assets.

## Vegetation rule

Vegetation performance must be reviewed for alpha overdraw as well as triangle count.

- Default to one alpha-tested foliage material per tree or vegetation family where practical.
- Share foliage materials and atlases across related assets.
- Trees require a billboard or impostor final-distance representation.
- Distant shrubs require a billboard, impostor, or very-low-cost final representation.
- Exceptions to the alpha-tested material default require technical and visual review.

## Business name: Shared commercial and city kit

Model 1 - Modular single-story commercial building shell, assembled - 18,000 tris  
Model 2 - Storefront window bay module - 1,200 tris  
Model 3 - Commercial glass entrance door - 1,500 tris  
Model 4 - Interior wall, baseboard, trim and doorway kit - 4,000 tris  
Model 5 - Suspended ceiling and ceiling-tile kit - 2,500 tris  
Model 6 - Rectangular fluorescent ceiling light - 450 tris  
Model 7 - Recessed ceiling light - 200 tris  
Model 8 - Electrical outlet, switch and thermostat kit - 600 tris total  
Model 9 - Wall clock - 350 tris  
Model 10 - Fire extinguisher and wall bracket - 800 tris  
Model 11 - Bulletin board and notice frame - 900 tris  
Model 12 - Indoor trash can - 650 tris  
Model 13 - Outdoor commercial trash and recycling bin kit - 1,800 tris  
Model 14 - Commercial planter and plant kit - 1,500 tris  
Model 15 - Parking curb and wheel-stop kit - 650 tris  
Model 16 - Safety bollard - 250 tris  
Model 17 - Sidewalk bench - 1,100 tris  
Model 18 - Exterior streetlamp - 1,100 tris  
Model 19 - Utility pole with transformer - 2,500 tris  
Model 20 - Mailbox or newspaper box - 900 tris  
Model 21 - A-frame sidewalk sign - 700 tris  
Model 22 - Close-view generic sedan - 24,000 tris  
Model 23 - Close-view generic station wagon - 24,000 tris  
Model 24 - Close-view generic pickup truck - 28,000 tris  
Model 25 - Close-view generic SUV or van - 28,000 tris  
Model 26 - Background vehicle variant - 12,000 tris  
Model 27 - Deciduous tree - 3,000 tris  
Model 28 - Conifer tree - 2,500 tris  
Model 29 - Shrub and ornamental-grass cluster - 900 tris  
Model 30 - Complete modular employee and customer character - 18,000 tris  
Model 31 - Additional civilian clothing set - 2,500 tris  
Model 32 - Employee polo, apron and name-badge set - 2,000 tris  
Model 33 - Modular hairstyle - 1,200 tris each  
Model 34 - Modular hat or cap - 450 tris each

Model 31 through Model 34 are module ceilings within the complete 18,000-triangle visible-character ceiling.

## Business name: Coffee shop

Model 1 - Coffee-shop facade sign and awning kit - 3,500 tris  
Model 2 - Outdoor café table, chairs and umbrella set - 4,500 tris  
Model 3 - Wooden service-counter module - 4,000 tris  
Model 4 - Glass pastry-display case - 8,000 tris  
Model 5 - Commercial espresso machine - 10,000 tris  
Model 6 - Coffee grinder with bean hopper - 4,500 tris  
Model 7 - Point-of-sale touchscreen - 2,000 tris  
Model 8 - Card-payment terminal - 1,000 tris  
Model 9 - Countertop cup and lid dispenser - 700 tris  
Model 10 - Commercial coffee brewer - 3,500 tris  
Model 11 - Milk pitcher - 300 tris  
Model 12 - Ceramic café mug - 220 tris  
Model 13 - Branded takeaway cup with lid - 150 tris  
Model 14 - Cup sleeve - 60 tris  
Model 15 - Wall-mounted menu-board frame - 700 tris  
Model 16 - Floating wooden shelf module - 800 tris  
Model 17 - Coffee-bag packaging family - 160 tris per bag  
Model 18 - Syrup bottle family - 220 tris each  
Model 19 - Condiment container family - 150 tris each  
Model 20 - Pastry tray and serving tongs - 600 tris  
Model 21 - Muffin - 180 tris  
Model 22 - Croissant - 250 tris  
Model 23 - Cookie - 100 tris  
Model 24 - Pastry slice - 180 tris  
Model 25 - Café dining table - 900 tris  
Model 26 - Wooden café chair - 1,200 tris  
Model 27 - Counter stool - 650 tris  
Model 28 - Hanging pendant light - 700 tris  
Model 29 - Napkin, straw and stirrer organizer - 400 tris  
Model 30 - Small refrigerated countertop display - 3,000 tris  
Model 31 - Under-counter refrigerator or dishwasher - 3,000 tris  
Model 32 - Coffee-shop wall-art frame set - 450 tris

## Business name: Convenience store

Model 1 - Convenience-store facade sign and awning kit - 4,000 tris  
Model 2 - Roadside convenience-store pylon sign - 4,000 tris  
Model 3 - Exterior ice freezer - 3,500 tris  
Model 4 - Exterior newspaper or promotional rack - 900 tris  
Model 5 - Gondola shelf module - 3,500 tris  
Model 6 - Gondola end-cap module - 2,000 tris  
Model 7 - Wall-mounted merchandise shelf - 2,500 tris  
Model 8 - Glass-door beverage-refrigerator module - 8,000 tris  
Model 9 - Chest freezer - 4,500 tris  
Model 10 - Checkout counter - 5,500 tris  
Model 11 - Register touchscreen and customer display - 2,500 tris  
Model 12 - Barcode scanner - 800 tris  
Model 13 - Card-payment terminal - 1,000 tris  
Model 14 - Impulse candy-display fixture - 1,500 tris  
Model 15 - Countertop coffee station - 4,000 tris  
Model 16 - Convenience-store microwave - 1,400 tris  
Model 17 - Back-counter cabinet and worktop kit - 3,500 tris  
Model 18 - Countertop sink - 1,200 tris  
Model 19 - ATM - 4,000 tris  
Model 20 - Wet-floor warning sign - 450 tris  
Model 21 - Employee-room door and sign - 1,000 tris  
Model 22 - Fountain-drink cup - 150 tris  
Model 23 - Countertop candy and gum tray - 750 tris  
Model 24 - Chip-bag shared mesh - 180 tris  
Model 25 - Candy-bar shared mesh - 60 tris  
Model 26 - Boxed-food shared mesh - 120 tris  
Model 27 - Plastic beverage-bottle shared mesh - 220 tris  
Model 28 - Aluminum-can shared mesh - 140 tris  
Model 29 - Milk or juice carton shared mesh - 160 tris  
Model 30 - Cleaning-liquid jug shared mesh - 240 tris  
Model 31 - Condiment or automotive-fluid bottle shared mesh - 200 tris  
Model 32 - Cardboard floor-display unit - 1,000 tris  
Model 33 - Price-label rail - 350 tris per shelf module  
Model 34 - Hanging promotional-sign frame - 250 tris  
Model 35 - Refrigerated-product shelf insert - 800 tris  
Model 36 - Small receipt printer - 450 tris

Product diversity should come primarily from label textures, dimensions, and material variants rather than unique meshes. Individual price labels should use textures or decals; the geometry budget applies to the rail.

## Business name: Gas station

The gas-station store reuses the convenience-store interior kit.

Model 1 - Modular fuel-station canopy, assembled - 18,000 tris  
Model 2 - Canopy branding panel - 1,400 tris  
Model 3 - Fuel-pump island base - 1,200 tris  
Model 4 - Fuel dispenser - 7,000 tris  
Model 5 - Fuel hose and nozzle assembly - 1,400 tris  
Model 6 - Pump display and keypad module - 900 tris  
Model 7 - Pump-side safety bollard set - 600 tris  
Model 8 - Pump trash can and windshield-squeegee station - 1,400 tris  
Model 9 - Fuel-price pylon sign - 4,500 tris  
Model 10 - Forecourt light pole - 1,200 tris  
Model 11 - Underground-tank fill-cap cluster - 500 tris  
Model 12 - Tank vent-pipe assembly - 600 tris  
Model 13 - Air and water service machine - 3,000 tris  
Model 14 - Propane-cage unit - 3,500 tris  
Model 15 - Exterior windshield-fluid display - 1,200 tris  
Model 16 - Fuel-delivery access panel - 400 tris  
Model 17 - Pump-number sign - 180 tris  
Model 18 - Canopy support-column module - 800 tris  
Model 19 - Forecourt curb and protective-island kit - 1,800 tris  
Model 20 - Fuel-grade button and nozzle-color variant kit - 300 tris

## Business name: Laundromat

Model 1 - Laundromat facade and primary-sign kit - 4,000 tris  
Model 2 - Laundromat roadside marquee sign - 4,500 tris  
Model 3 - Standard front-loading washer - 5,000 tris  
Model 4 - Large-capacity front-loading washer - 6,000 tris  
Model 5 - Single commercial dryer - 5,000 tris  
Model 6 - Stacked commercial dryer - 8,000 tris  
Model 7 - Washer and dryer control-panel variant - 800 tris  
Model 8 - Change machine - 3,500 tris  
Model 9 - Detergent vending machine - 3,500 tris  
Model 10 - Folding table - 1,500 tris  
Model 11 - Rolling laundry cart - 2,500 tris  
Model 12 - Plastic laundry basket - 1,200 tris  
Model 13 - Tall rolling laundry hamper - 1,400 tris  
Model 14 - Bench seating - 1,100 tris  
Model 15 - Detergent-bottle shared mesh - 220 tris  
Model 16 - Fabric-softener-bottle shared mesh - 200 tris  
Model 17 - Folded-clothing pile - 450 tris per variation  
Model 18 - Loose laundry load - 600 tris per variation  
Model 19 - Washer-drum clothing insert - 500 tris  
Model 20 - Dryer-drum clothing insert - 500 tris  
Model 21 - Machine-number plaque - 80 tris each  
Model 22 - Wall-mounted rules and warning-sign frame set - 500 tris  
Model 23 - Utility sink and cabinet - 3,000 tris  
Model 24 - Mop, bucket and cleaning station - 2,000 tris  
Model 25 - Ceiling fan - 1,000 tris  
Model 26 - Laundry-supply shelf - 1,500 tris  
Model 27 - Coin box or card-reader module - 600 tris  
Model 28 - Lost-and-found bin - 600 tris  
Model 29 - Folding-service storage rack - 2,000 tris  
Model 30 - Wall-mounted television - 1,000 tris

## Review and revision rules

- These ceilings protect performance and production consistency; they do not require artists or generation tools to use all available triangles.
- Repetition frequency and expected screen importance should guide how far below a ceiling an asset is optimized.
- A ceiling increase requires measured evidence that silhouette, deformation, interaction readability, or close-view quality cannot be preserved within the current limit.
- A ceiling decrease may be imposed after profiling, draw-call review, alpha-overdraw review, memory review, or visible-instance testing.
- Project-owner approval is required to revise the complete-character ceiling, close-view vehicle range, assembled-building shell ceiling, or any category-wide rule.
