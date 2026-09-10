# Margins Production 3D Asset Intake Workflow v1.0

## Status and purpose

- **Status:** Production workflow implementing existing approved asset-interface, dimensional, budget, and provenance requirements.
- **Purpose:** Move a Blender-normalized Tripo, generated, sourced, or authored 3D asset into a Unity production prefab and identify automatic failures before owner review.
- **Authority:** This workflow applies existing repository decisions. It does not approve an asset, license, art direction, or runtime system.

## Intake sequence

1. Retain the source files and source/license/AI evidence required by the provenance ledger.
2. Normalize dimensions, pivot, transforms, separation, material regions, and Unity-facing `+Y` up / `+Z` forward orientation in Blender.
3. Create one row in `Margins_Asset_Provenance_Ledger.csv` using the existing schema in `DESIGNS/Templates & Schemas/Margins_Asset_Provenance_Ledger_Template_v0.1.csv`.
4. Import the normalized model and textures into the appropriate Unity production-content folder. Do not place production assets in the procedural-placeholder registry.
5. Author a prefab with `ProductionAssetMetadata` on the root and a direct, identity-transform `Visual` child. Set the ledger `asset_id`, intended meter dimensions, dimension tolerance, Unity materials, LODs, and Unity-owned colliders.
6. Select the prefab and run **Margins > Production Assets > Validate Selected Prefab**.
7. Correct technical errors and update the ledger's measured values. Warnings identify review or disposition gates that automation cannot close.
8. The owner separately reviews visual consistency, interaction readability, licensing risk, and final acceptance before setting production disposition.

## Operational ledger values

The validator uses these exact values so intake state is unambiguous:

- `normalization_status`: `complete` after the Blender-normalized export has been checked;
- `unity_import_status`: `complete` after the Unity prefab/import setup has been checked;
- `performance_review_status`: `passed` after the recorded automatic budgets pass;
- visual, interaction, and owner review statuses: `pending`, `accepted`, or `rejected`;
- `disposition`: `intake`, `quarantined`, `production`, or `rejected`.

Use `n/a` rather than leaving required provenance or constraint fields blank. Optional category fields such as character or vegetation limits may remain blank when they do not apply.

Use `character` in `asset_class` when the complete-character bone, influence, renderer, and material ceilings apply. Use `vegetation` when alpha-tested material count and final-distance representation are required. Skinned non-character assets retain their normal class and are not evaluated against character-only ceilings.

## Automated boundary

The Unity validator checks the ledger link and required records, root/visual normalization, expected meter dimensions, LOD triangle ceilings and measurements, material-slot ceilings and measurements, collider policy and collision triangles, technical statuses, manual-gate state, and production disposition.

Automation cannot infer whether the modeled front actually faces `+Z`, judge visual consistency or interaction readability, accept licensing risk, or grant final production acceptance. Root and `Visual` transform checks detect axis-conversion rotations, but semantic facing still requires owner review.
