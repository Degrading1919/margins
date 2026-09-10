# Margins Production 3D Asset Intake Workflow v1.0

## Status and purpose

- **Status:** Production workflow implementing existing approved asset-interface, dimensional, budget, and provenance requirements.
- **Purpose:** Move a Blender-normalized Tripo, generated, sourced, or authored 3D asset into a Unity production prefab and identify automatic failures before owner review.
- **Authority:** This workflow applies existing repository decisions. It does not approve an asset, license, art direction, or runtime system.

The production budget catalog at `01_PRE-PRODUCTION/1.7 Art Audio & Presentation/Margins_3D_Asset_Budget_Catalog_v0.1.csv` is the working machine-readable authority for asset identity, ceilings, collider policy, material/texture limits, and catalog constraints. The provenance ledger records source evidence, Unity measurements, review state, and disposition. It does not independently maintain budget ceilings.

## Intake sequence

1. Retain the source files and source/license/AI evidence required by the provenance ledger.
2. Normalize dimensions, pivot, transforms, separation, material regions, and Unity-facing `+Y` up / `+Z` forward orientation in Blender.
3. Select the approved production `asset_id` from the budget catalog and create a matching provenance row in `Margins_Asset_Provenance_Ledger.csv` using the schema in `DESIGNS/Templates & Schemas/Margins_Asset_Provenance_Ledger_Template_v0.1.csv`.
4. Import the normalized model and textures into the appropriate Unity production-content folder. Do not place production assets in the procedural-placeholder registry.
5. Author a prefab with `ProductionAssetMetadata` on the root and a direct, identity-transform `Visual` child. Set the production catalog `asset_id`, intended meter dimensions, dimension tolerance, Unity materials, LODs, and Unity-owned colliders. If the prefab also participates in procedural generation, configure `ProceduralAssetComponent` under its separate runtime identity contract; its stable ID does not need to equal the production catalog ID.
6. Select the prefab and run **Margins > Production Assets > Measure Selected Prefab For Intake**. This reports Unity measurements and checks available catalog ceilings without requiring those measurements to be pre-recorded.
7. Correct technical errors and record the reported measured values in the provenance ledger.
8. Run **Margins > Production Assets > Validate Selected Prefab** for the strict gate. Strict validation compares Unity measurements with the ledger and requires the final technical/review state.
9. The owner separately reviews visual consistency, interaction readability, licensing risk, and final acceptance before setting production disposition.

## Operational ledger values

The validator uses these exact values so intake state is unambiguous:

- `normalization_status`: `complete` after the Blender-normalized export has been checked;
- `unity_import_status`: `complete` after the Unity prefab/import setup has been checked;
- `performance_review_status`: `passed` after the recorded automatic budgets pass;
- visual, interaction, and owner review statuses: `pending`, `accepted`, or `rejected`;
- `disposition`: `intake`, `quarantined`, `production`, or `rejected`.

Use `n/a` rather than leaving required provenance fields blank. Optional measured fields may remain blank until Unity reports them or when the catalog classification makes them inapplicable. Catalog fields that happen to describe source, measured, or planning status are not used as the provenance/review record by this workflow.

## Automated boundary

The Unity validator resolves ceilings and constraints from the production budget catalog. It checks the provenance-ledger link and required records, root/visual normalization, expected meter dimensions, LOD triangles, materials/textures, collider policy and collision triangles, technical statuses, manual-gate state, and production disposition. Intake mode exposes Unity measurements first; strict mode additionally requires matching recorded measurements and completed gates.

Automation cannot infer whether the modeled front actually faces `+Z`, judge visual consistency or interaction readability, accept licensing risk, or grant final production acceptance. Root and `Visual` transform checks detect axis-conversion rotations, but semantic facing still requires owner review.
