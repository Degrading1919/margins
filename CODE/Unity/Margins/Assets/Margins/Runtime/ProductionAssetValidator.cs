#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Margins
{
    public enum ProductionAssetValidationMode
    {
        IntakeMeasurement,
        ProductionReadiness
    }

    public enum ProductionAssetValidationSeverity
    {
        Warning,
        Error
    }

    public readonly struct ProductionAssetValidationIssue
    {
        public ProductionAssetValidationIssue(
            ProductionAssetValidationSeverity severity,
            string message)
        {
            Severity = severity;
            Message = message;
        }

        public ProductionAssetValidationSeverity Severity { get; }
        public string Message { get; }
    }

    public sealed class ProductionAssetMeasurements
    {
        internal readonly HashSet<Transform> Lod0Bones = new();
        internal readonly HashSet<Texture> Lod0Textures = new();
        internal readonly HashSet<Material> Lod0Materials = new();

        public int Lod0Triangles { get; internal set; }
        public int Lod1Triangles { get; internal set; }
        public int Lod2Triangles { get; internal set; }
        public int CollisionTriangles { get; internal set; }
        public int Lod0MaterialSlots { get; internal set; }
        public int MaximumTextureDimension { get; internal set; }
        public int Lod0SkinnedRendererCount { get; internal set; }
        public int Lod0BoneCount => Lod0Bones.Count;
        public int MaximumInfluencesPerVertex { get; internal set; }
        public int AlphaTestedMaterialCount { get; internal set; }
        public int HighestLodLevel { get; internal set; }

        public int TrianglesFor(int level)
        {
            return level switch
            {
                0 => Lod0Triangles,
                1 => Lod1Triangles,
                2 => Lod2Triangles,
                _ => 0
            };
        }

        internal void AddTriangles(int level, int count)
        {
            switch (level)
            {
                case 0:
                    Lod0Triangles += count;
                    break;
                case 1:
                    Lod1Triangles += count;
                    break;
                case 2:
                    Lod2Triangles += count;
                    break;
            }

            HighestLodLevel = Mathf.Max(HighestLodLevel, level);
        }
    }

    public sealed class ProductionAssetValidationReport
    {
        private readonly List<ProductionAssetValidationIssue> issues = new();

        public IReadOnlyList<ProductionAssetValidationIssue> Issues => issues;
        public ProductionAssetMeasurements Measurements { get; } = new();
        public bool TechnicalRequirementsPassed { get; internal set; }
        public bool ProductionReady { get; internal set; }

        internal void AddError(string message)
        {
            issues.Add(new ProductionAssetValidationIssue(
                ProductionAssetValidationSeverity.Error,
                message));
        }

        internal void AddWarning(string message)
        {
            issues.Add(new ProductionAssetValidationIssue(
                ProductionAssetValidationSeverity.Warning,
                message));
        }

        internal bool HasErrors()
        {
            foreach (ProductionAssetValidationIssue issue in issues)
            {
                if (issue.Severity == ProductionAssetValidationSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static class ProductionAssetValidator
    {
        private static readonly string[] RequiredProvenanceFields =
        {
            "source_type",
            "source_name",
            "creator_or_vendor",
            "source_url",
            "acquisition_date",
            "original_version",
            "license_name",
            "license_url_or_archive_path",
            "commercial_use_status",
            "modification_permission",
            "redistribution_restrictions",
            "attribution_requirement",
            "attribution_text",
            "seat_or_contractor_restrictions",
            "ai_involvement",
            "ai_tool_or_service",
            "ai_terms_archive",
            "source_files_retained",
            "modifications_summary"
        };

        private static readonly string[] RequiredFinalFields =
        {
            "normalization_status",
            "unity_import_status",
            "performance_review_status",
            "visual_consistency_review_status",
            "interaction_readability_review_status",
            "owner_acceptance_status",
            "disposition",
            "reviewer",
            "review_date"
        };

        public static ProductionAssetValidationReport Validate(
            ProductionAssetMetadata metadata,
            ProductionAssetLedger ledger,
            ProductionAssetBudgetCatalog catalog,
            ProductionAssetValidationMode mode)
        {
            ProductionAssetValidationReport report = new();
            if (metadata == null)
            {
                report.AddError("The selected prefab requires ProductionAssetMetadata on its root.");
                return report;
            }

            ValidateProceduralContract(metadata, report);
            ValidateTransformsAndDimensions(metadata, report);
            MeasureMeshes(metadata, report);
            Collider[] colliders = MeasureColliders(metadata, report.Measurements);

            string assetId = metadata.AssetId?.Trim();
            if (string.IsNullOrEmpty(assetId))
            {
                report.AddError("ProductionAssetMetadata requires a production catalog asset_id.");
            }

            ProductionAssetLedgerRecord catalogRecord = null;
            if (catalog == null)
            {
                report.AddError("The production asset budget catalog could not be loaded.");
            }
            else if (!catalog.TryGetRecord(assetId, out catalogRecord))
            {
                report.AddError($"No production budget catalog row matches asset_id '{assetId}'.");
            }
            else
            {
                ValidateCatalogBudgets(
                    metadata,
                    catalogRecord,
                    colliders,
                    mode,
                    report);
            }

            ProductionAssetLedgerRecord ledgerRecord = null;
            if (ledger == null)
            {
                report.AddError("The production asset provenance ledger could not be loaded.");
            }
            else if (!ledger.TryGetRecord(assetId, out ledgerRecord))
            {
                report.AddError($"No provenance ledger row matches asset_id '{assetId}'. Measurements are still available for first-time intake.");
            }
            else
            {
                ValidateProvenance(ledgerRecord, report);
                if (mode == ProductionAssetValidationMode.ProductionReadiness)
                {
                    ValidateRecordedMeasurements(
                        ledgerRecord,
                        catalogRecord,
                        report);
                    ValidateReviewState(ledgerRecord, report);
                }
            }

            report.TechnicalRequirementsPassed = !report.HasErrors();
            report.ProductionReady =
                mode == ProductionAssetValidationMode.ProductionReadiness &&
                report.TechnicalRequirementsPassed &&
                ledgerRecord != null &&
                Is(ledgerRecord.Get("visual_consistency_review_status"), "accepted") &&
                Is(ledgerRecord.Get("interaction_readability_review_status"), "accepted") &&
                Is(ledgerRecord.Get("owner_acceptance_status"), "accepted") &&
                Is(ledgerRecord.Get("disposition"), "production");
            return report;
        }

        private static void ValidateProceduralContract(
            ProductionAssetMetadata metadata,
            ProductionAssetValidationReport report)
        {
            ProceduralAssetComponent procedural =
                metadata.GetComponent<ProceduralAssetComponent>();
            if (procedural != null &&
                !procedural.TryValidateConfiguration(out string error))
            {
                report.AddError($"Procedural asset configuration is invalid: {error}");
            }
        }

        private static void ValidateProvenance(
            ProductionAssetLedgerRecord record,
            ProductionAssetValidationReport report)
        {
            foreach (string field in RequiredProvenanceFields)
            {
                RequireText(record, field, report);
            }
        }

        private static void ValidateCatalogBudgets(
            ProductionAssetMetadata metadata,
            ProductionAssetLedgerRecord record,
            IReadOnlyList<Collider> colliders,
            ProductionAssetValidationMode mode,
            ProductionAssetValidationReport report)
        {
            ProductionAssetMeasurements measured = report.Measurements;
            ValidateLodBudget(record, measured, 0, true, mode, report);
            ValidateLodBudget(record, measured, 1, false, mode, report);
            ValidateLodBudget(record, measured, 2, false, mode, report);

            string materialMaximumText = record.Get("material_slots_max");
            if (!string.IsNullOrWhiteSpace(materialMaximumText) ||
                mode == ProductionAssetValidationMode.ProductionReadiness)
            {
                if (!TryGetPositiveInt(record, "material_slots_max", out int materialMaximum))
                {
                    report.AddError("Budget catalog field 'material_slots_max' requires a positive integer.");
                }
                else if (measured.Lod0MaterialSlots > materialMaximum)
                {
                    report.AddError($"LOD0 uses {measured.Lod0MaterialSlots} material slots and exceeds the catalog ceiling of {materialMaximum}.");
                }
            }

            string textureMaximumText = record.Get("texture_max_px");
            if (!string.IsNullOrWhiteSpace(textureMaximumText) ||
                mode == ProductionAssetValidationMode.ProductionReadiness)
            {
                if (!TryGetPositiveInt(record, "texture_max_px", out int textureMaximum))
                {
                    report.AddError("Budget catalog field 'texture_max_px' requires a positive integer.");
                }
                else if (measured.MaximumTextureDimension > textureMaximum)
                {
                    report.AddError($"A material uses a {measured.MaximumTextureDimension}px texture and exceeds the catalog ceiling of {textureMaximum}px.");
                }
            }

            ValidateColliderBudget(metadata, record, colliders, mode, report);
            ValidateCatalogContext(record, mode, report);
            ValidateCatalogClassConstraints(record, mode, report);
        }

        private static void ValidateCatalogContext(
            ProductionAssetLedgerRecord record,
            ProductionAssetValidationMode mode,
            ProductionAssetValidationReport report)
        {
            if (mode != ProductionAssetValidationMode.ProductionReadiness)
            {
                return;
            }

            if (!TryGetPositiveInt(record, "expected_visible_instances", out _))
            {
                report.AddError("Strict production validation requires a positive catalog expected_visible_instances value.");
            }

            if (!TryGetPositiveFloat(record, "closest_view_m", out _))
            {
                report.AddError("Strict production validation requires a positive catalog closest_view_m value.");
            }

            if (string.IsNullOrWhiteSpace(record.Get("interaction_level")))
            {
                report.AddError("Strict production validation requires catalog interaction_level context.");
            }

            if (string.IsNullOrWhiteSpace(record.Get("animation_requirement")))
            {
                report.AddError("Strict production validation requires catalog animation_requirement context.");
            }
        }

        private static void ValidateLodBudget(
            ProductionAssetLedgerRecord record,
            ProductionAssetMeasurements measured,
            int level,
            bool required,
            ProductionAssetValidationMode mode,
            ProductionAssetValidationReport report)
        {
            string field = $"lod{level}_ceiling_tris";
            string text = record.Get(field);
            int actual = measured.TrianglesFor(level);
            if (!required && string.IsNullOrWhiteSpace(text) &&
                (actual == 0 || mode == ProductionAssetValidationMode.IntakeMeasurement))
            {
                return;
            }

            if (!TryGetPositiveInt(record, field, out int ceiling))
            {
                report.AddError($"Budget catalog field '{field}' requires a positive integer when LOD{level} is present.");
                return;
            }

            if (level > 0 && actual == 0)
            {
                report.AddError($"The catalog declares LOD{level}, but the prefab has no LOD{level} render mesh.");
            }
            else if (actual > ceiling)
            {
                report.AddError($"LOD{level} uses {actual} triangles and exceeds its catalog ceiling of {ceiling}.");
            }
        }

        private static void ValidateColliderBudget(
            ProductionAssetMetadata metadata,
            ProductionAssetLedgerRecord record,
            IReadOnlyList<Collider> colliders,
            ProductionAssetValidationMode mode,
            ProductionAssetValidationReport report)
        {
            if (colliders.Count == 0)
            {
                report.AddError("The production prefab requires at least one Unity collider.");
                return;
            }

            string policy = Normalize(record.Get("collider_type"));
            if (string.IsNullOrEmpty(policy))
            {
                if (mode == ProductionAssetValidationMode.ProductionReadiness)
                {
                    report.AddError("Budget catalog field 'collider_type' is required for production readiness.");
                }

                ValidateCollisionCeiling(record, false, mode, report);
                return;
            }

            foreach (Collider collider in colliders)
            {
                bool allowed = policy switch
                {
                    "primitive" => IsPrimitive(collider),
                    "compoundprimitive" => IsPrimitive(collider),
                    "convexmesh" => collider is MeshCollider convex && convex.convex,
                    "staticmesh" => collider is MeshCollider nonConvex && !nonConvex.convex,
                    _ => false
                };
                if (!allowed)
                {
                    report.AddError($"Collider '{collider.name}' does not match catalog collider_type '{record.Get("collider_type")}'.");
                }
            }

            if (policy == "primitive" && colliders.Count != 1)
            {
                report.AddError("A primitive catalog policy requires exactly one primitive collider.");
            }
            else if (policy == "compoundprimitive" && colliders.Count < 2)
            {
                report.AddError("A compound primitive catalog policy requires at least two primitive colliders.");
            }
            else if (policy == "staticmesh" && !metadata.gameObject.isStatic)
            {
                report.AddError("A static mesh collider asset must be marked Static in Unity.");
            }
            else if (policy != "primitive" && policy != "compoundprimitive" &&
                     policy != "convexmesh" && policy != "staticmesh")
            {
                report.AddError("Budget catalog collider_type must be primitive, compound primitive, convex mesh, or static mesh.");
            }

            ValidateCollisionCeiling(
                record,
                policy == "convexmesh" || policy == "staticmesh" ||
                report.Measurements.CollisionTriangles > 0,
                mode,
                report);
        }

        private static void ValidateCollisionCeiling(
            ProductionAssetLedgerRecord record,
            bool required,
            ProductionAssetValidationMode mode,
            ProductionAssetValidationReport report)
        {
            string ceilingText = record.Get("collision_ceiling_tris");
            if (!required && string.IsNullOrWhiteSpace(ceilingText))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(ceilingText) &&
                mode == ProductionAssetValidationMode.IntakeMeasurement)
            {
                return;
            }

            if (!TryGetPositiveInt(record, "collision_ceiling_tris", out int ceiling))
            {
                report.AddError("Mesh collider catalog records require a positive collision_ceiling_tris value.");
            }
            else if (report.Measurements.CollisionTriangles > ceiling)
            {
                report.AddError($"Collision meshes use {report.Measurements.CollisionTriangles} triangles and exceed the catalog ceiling of {ceiling}.");
            }
        }

        private static void ValidateCatalogClassConstraints(
            ProductionAssetLedgerRecord record,
            ProductionAssetValidationMode mode,
            ProductionAssetValidationReport report)
        {
            if (IsCharacter(record))
            {
                ValidateCharacterConstraints(record, mode, report);
            }

            if (IsVegetation(record))
            {
                ProductionAssetMeasurements measured = report.Measurements;
                if (measured.HighestLodLevel == 0)
                {
                    report.AddError("Vegetation assets require an LODGroup with a final-distance renderer.");
                }

                if (measured.AlphaTestedMaterialCount > 1)
                {
                    report.AddWarning("Vegetation uses more than one alpha-tested material; the approved default requires technical and visual exception review.");
                }
            }
        }

        private static void ValidateCharacterConstraints(
            ProductionAssetLedgerRecord record,
            ProductionAssetValidationMode mode,
            ProductionAssetValidationReport report)
        {
            ProductionAssetMeasurements measured = report.Measurements;
            if (measured.Lod0SkinnedRendererCount == 0)
            {
                report.AddError("A catalog-classified character requires at least one LOD0 SkinnedMeshRenderer.");
            }

            string[] constraintFields =
            {
                "max_bones",
                "max_influences_per_vertex",
                "max_skinned_mesh_renderers",
                "complete_character_max_material_slots"
            };
            bool constraintsAvailable = true;
            foreach (string field in constraintFields)
            {
                if (!TryGetPositiveInt(record, field, out _))
                {
                    constraintsAvailable = false;
                }
            }

            if (!constraintsAvailable)
            {
                if (mode == ProductionAssetValidationMode.ProductionReadiness)
                {
                    report.AddError("The production budget catalog does not yet expose authoritative character bone, influence, renderer, and complete-character material limits; strict readiness cannot be established.");
                }

                return;
            }

            ValidateMaximum(
                record,
                "max_bones",
                measured.Lod0BoneCount,
                "bones",
                report);
            ValidateMaximum(
                record,
                "max_influences_per_vertex",
                measured.MaximumInfluencesPerVertex,
                "influences per vertex",
                report);
            ValidateMaximum(
                record,
                "max_skinned_mesh_renderers",
                measured.Lod0SkinnedRendererCount,
                "skinned mesh renderers",
                report);
            ValidateMaximum(
                record,
                "complete_character_max_material_slots",
                measured.Lod0MaterialSlots,
                "complete-character material slots",
                report);
        }

        private static void ValidateMaximum(
            ProductionAssetLedgerRecord record,
            string field,
            int actual,
            string label,
            ProductionAssetValidationReport report)
        {
            if (TryGetPositiveInt(record, field, out int maximum) && actual > maximum)
            {
                report.AddError($"The character uses {actual} {label} and exceeds the catalog limit of {maximum}.");
            }
        }

        private static void ValidateRecordedMeasurements(
            ProductionAssetLedgerRecord ledger,
            ProductionAssetLedgerRecord catalog,
            ProductionAssetValidationReport report)
        {
            ProductionAssetMeasurements measured = report.Measurements;
            RequireMeasurement(
                ledger,
                "lod0_measured_triangles",
                measured.Lod0Triangles,
                report);

            bool catalogHasLod1 = catalog != null &&
                                  !string.IsNullOrWhiteSpace(catalog.Get("lod1_ceiling_tris"));
            bool catalogHasLod2 = catalog != null &&
                                  !string.IsNullOrWhiteSpace(catalog.Get("lod2_ceiling_tris"));
            if (catalogHasLod1 || measured.Lod1Triangles > 0)
            {
                RequireMeasurement(
                    ledger,
                    "lod1_measured_triangles",
                    measured.Lod1Triangles,
                    report);
            }

            if (catalogHasLod2 || measured.Lod2Triangles > 0)
            {
                RequireMeasurement(
                    ledger,
                    "lod2_measured_triangles",
                    measured.Lod2Triangles,
                    report);
            }

            RequireMeasurement(
                ledger,
                "collision_measured_triangles",
                measured.CollisionTriangles,
                report);
            RequireMeasurement(
                ledger,
                "measured_material_slots",
                measured.Lod0MaterialSlots,
                report);
            RequireMeasurement(
                ledger,
                "measured_max_texture_px",
                measured.MaximumTextureDimension,
                report);

            if (measured.Lod0SkinnedRendererCount > 0)
            {
                RequireMeasurement(
                    ledger,
                    "measured_bones",
                    measured.Lod0BoneCount,
                    report);
                RequireMeasurement(
                    ledger,
                    "measured_max_influences_per_vertex",
                    measured.MaximumInfluencesPerVertex,
                    report);
                RequireMeasurement(
                    ledger,
                    "measured_skinned_mesh_renderers",
                    measured.Lod0SkinnedRendererCount,
                    report);
            }

            if (catalog != null && IsVegetation(catalog))
            {
                RequireMeasurement(
                    ledger,
                    "alpha_tested_material_count",
                    measured.AlphaTestedMaterialCount,
                    report);
                string finalRepresentation = Normalize(
                    ledger.Get("final_distance_representation"));
                if (string.IsNullOrEmpty(finalRepresentation) ||
                    finalRepresentation == "na" || finalRepresentation == "none")
                {
                    report.AddError("Vegetation provenance records require the reviewed final-distance representation.");
                }
            }
        }

        private static void ValidateReviewState(
            ProductionAssetLedgerRecord record,
            ProductionAssetValidationReport report)
        {
            foreach (string field in RequiredFinalFields)
            {
                RequireText(record, field, report);
            }

            RequireStatus(record, "normalization_status", "complete", report);
            RequireStatus(record, "unity_import_status", "complete", report);
            RequireStatus(record, "performance_review_status", "passed", report);

            string[] manualFields =
            {
                "visual_consistency_review_status",
                "interaction_readability_review_status",
                "owner_acceptance_status"
            };
            foreach (string field in manualFields)
            {
                string value = Normalize(record.Get(field));
                if (value != "pending" && value != "accepted" && value != "rejected")
                {
                    report.AddError($"Ledger field '{field}' must be pending, accepted, or rejected.");
                }
                else if (value != "accepted")
                {
                    report.AddWarning($"Manual gate '{field}' is {record.Get(field)}; only the owner can accept it.");
                }
            }

            string disposition = Normalize(record.Get("disposition"));
            if (disposition != "intake" && disposition != "quarantined" &&
                disposition != "production" && disposition != "rejected")
            {
                report.AddError("Ledger disposition must be intake, quarantined, production, or rejected.");
            }
            else if (disposition != "production")
            {
                report.AddWarning($"Asset disposition is '{record.Get("disposition")}', so it is not production-ready.");
            }

            if (disposition == "production" &&
                (!Is(record.Get("visual_consistency_review_status"), "accepted") ||
                 !Is(record.Get("interaction_readability_review_status"), "accepted") ||
                 !Is(record.Get("owner_acceptance_status"), "accepted")))
            {
                report.AddError("Production disposition requires every manual review gate to be accepted.");
            }
        }

        private static void RequireMeasurement(
            ProductionAssetLedgerRecord record,
            string field,
            int actual,
            ProductionAssetValidationReport report)
        {
            if (!record.TryGetNonNegativeInt(field, out int recorded))
            {
                report.AddError($"Strict production validation requires a non-negative ledger value for '{field}'.");
            }
            else if (recorded != actual)
            {
                report.AddError($"Ledger measurement '{field}' is {recorded}, but Unity measured {actual}.");
            }
        }

        private static void RequireText(
            ProductionAssetLedgerRecord record,
            string field,
            ProductionAssetValidationReport report)
        {
            if (!record.HasColumn(field))
            {
                report.AddError($"The provenance ledger schema is missing required field '{field}'.");
            }
            else if (string.IsNullOrWhiteSpace(record.Get(field)))
            {
                report.AddError($"Provenance ledger row {record.RowNumber} requires '{field}' (use 'n/a' when applicable).");
            }
        }

        private static void RequireStatus(
            ProductionAssetLedgerRecord record,
            string field,
            string required,
            ProductionAssetValidationReport report)
        {
            if (!Is(record.Get(field), required))
            {
                report.AddError($"Ledger field '{field}' must be '{required}' for production readiness.");
            }
        }

        private static void ValidateTransformsAndDimensions(
            ProductionAssetMetadata metadata,
            ProductionAssetValidationReport report)
        {
            Transform root = metadata.transform;
            if (!Approximately(root.localScale, Vector3.one))
            {
                report.AddError("The production prefab root must use unit local scale; exporter-compensation scale is not accepted.");
            }

            if (!Approximately(root.localRotation, Quaternion.identity))
            {
                report.AddError("The production prefab root must use identity local rotation (+Y up, +Z forward).");
            }

            Transform visual = metadata.VisualRoot;
            if (visual == null || visual.parent != root)
            {
                report.AddError("ProductionAssetMetadata requires a direct Visual child.");
                return;
            }

            if (!string.Equals(visual.name, "Visual", StringComparison.Ordinal) ||
                !Approximately(visual.localPosition, Vector3.zero) ||
                !Approximately(visual.localRotation, Quaternion.identity) ||
                !Approximately(visual.localScale, Vector3.one))
            {
                report.AddError("The direct Visual child must be named 'Visual' with zero position, identity rotation, and unit scale.");
            }

            Vector3 expected = metadata.ExpectedDimensionsMeters;
            if (!PositiveFinite(expected.x) || !PositiveFinite(expected.y) ||
                !PositiveFinite(expected.z) ||
                !PositiveFinite(metadata.DimensionToleranceMeters))
            {
                report.AddError("Expected meter dimensions and dimension tolerance must be positive finite values.");
            }
            else if (!TryMeasureLocalBounds(metadata, out Bounds bounds))
            {
                report.AddError("The production prefab contains no measurable render mesh.");
            }
            else
            {
                Vector3 difference = bounds.size - expected;
                if (Mathf.Abs(difference.x) > metadata.DimensionToleranceMeters ||
                    Mathf.Abs(difference.y) > metadata.DimensionToleranceMeters ||
                    Mathf.Abs(difference.z) > metadata.DimensionToleranceMeters)
                {
                    report.AddError($"Measured size {bounds.size:F4} m does not match expected size {expected:F4} m within {metadata.DimensionToleranceMeters:F4} m.");
                }
            }

            foreach (Transform descendant in
                     visual.GetComponentsInChildren<Transform>(true))
            {
                if (!Approximately(descendant.localScale, Vector3.one))
                {
                    report.AddError($"Visual transform '{descendant.name}' must use unit local scale after Blender normalization.");
                }

                if (descendant.parent == visual &&
                    !Approximately(descendant.localRotation, Quaternion.identity))
                {
                    report.AddError($"Direct visual child '{descendant.name}' must use identity local rotation; axis-conversion rotations must be fixed during normalization.");
                }
            }
        }

        private static void MeasureMeshes(
            ProductionAssetMetadata metadata,
            ProductionAssetValidationReport report)
        {
            Transform visual = metadata.VisualRoot;
            if (visual == null)
            {
                return;
            }

            HashSet<Renderer> assigned = new();
            foreach (LODGroup group in visual.GetComponentsInChildren<LODGroup>(true))
            {
                LOD[] lods = group.GetLODs();
                for (int level = 0; level < lods.Length; level++)
                {
                    foreach (Renderer renderer in lods[level].renderers)
                    {
                        if (renderer != null && assigned.Add(renderer))
                        {
                            AddRendererMeasurement(level, renderer, report);
                        }
                    }
                }
            }

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (assigned.Add(renderer))
                {
                    AddRendererMeasurement(0, renderer, report);
                }
            }
        }

        private static void AddRendererMeasurement(
            int level,
            Renderer renderer,
            ProductionAssetValidationReport report)
        {
            if (level > 2)
            {
                return;
            }

            Mesh mesh = renderer switch
            {
                MeshRenderer => renderer.GetComponent<MeshFilter>()?.sharedMesh,
                SkinnedMeshRenderer skinned => skinned.sharedMesh,
                _ => null
            };
            if (mesh == null)
            {
                report.AddError($"Renderer '{renderer.name}' has no mesh to validate.");
                return;
            }

            ProductionAssetMeasurements measured = report.Measurements;
            measured.AddTriangles(level, TriangleCount(mesh));
            if (level != 0)
            {
                return;
            }

            Material[] materials = renderer.sharedMaterials;
            measured.Lod0MaterialSlots += materials.Length;
            foreach (Material material in materials)
            {
                if (material == null)
                {
                    report.AddError($"Renderer '{renderer.name}' has an unassigned material slot.");
                    continue;
                }

                if (measured.Lod0Materials.Add(material) && IsAlphaTested(material))
                {
                    measured.AlphaTestedMaterialCount++;
                }

                foreach (string propertyName in material.GetTexturePropertyNames())
                {
                    Texture texture = material.GetTexture(propertyName);
                    if (texture != null && measured.Lod0Textures.Add(texture))
                    {
                        measured.MaximumTextureDimension = Mathf.Max(
                            measured.MaximumTextureDimension,
                            Mathf.Max(texture.width, texture.height));
                    }
                }
            }

            if (renderer is not SkinnedMeshRenderer skinnedRenderer)
            {
                return;
            }

            measured.Lod0SkinnedRendererCount++;
            foreach (Transform bone in skinnedRenderer.bones)
            {
                if (bone != null)
                {
                    measured.Lod0Bones.Add(bone);
                }
            }

            using var influences = mesh.GetBonesPerVertex();
            foreach (byte influenceCount in influences)
            {
                measured.MaximumInfluencesPerVertex = Mathf.Max(
                    measured.MaximumInfluencesPerVertex,
                    influenceCount);
            }
        }

        private static Collider[] MeasureColliders(
            ProductionAssetMetadata metadata,
            ProductionAssetMeasurements measured)
        {
            Collider[] colliders = metadata.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                if (collider is MeshCollider meshCollider)
                {
                    measured.CollisionTriangles +=
                        TriangleCount(meshCollider.sharedMesh);
                }
            }

            return colliders;
        }

        private static bool TryMeasureLocalBounds(
            ProductionAssetMetadata metadata,
            out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            Transform visual = metadata.VisualRoot;
            if (visual == null)
            {
                return false;
            }

            Matrix4x4 worldToRoot = metadata.transform.worldToLocalMatrix;
            foreach (MeshFilter filter in visual.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null)
                {
                    Encapsulate(
                        filter.sharedMesh.bounds,
                        worldToRoot * filter.transform.localToWorldMatrix,
                        ref bounds,
                        ref found);
                }
            }

            foreach (SkinnedMeshRenderer renderer in
                     visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMesh != null)
                {
                    Encapsulate(
                        renderer.localBounds,
                        worldToRoot * renderer.transform.localToWorldMatrix,
                        ref bounds,
                        ref found);
                }
            }

            return found;
        }

        private static void Encapsulate(
            Bounds source,
            Matrix4x4 matrix,
            ref Bounds destination,
            ref bool found)
        {
            Vector3 min = source.min;
            Vector3 max = source.max;
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 point = matrix.MultiplyPoint3x4(new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z));
                        if (!found)
                        {
                            destination = new Bounds(point, Vector3.zero);
                            found = true;
                        }
                        else
                        {
                            destination.Encapsulate(point);
                        }
                    }
                }
            }
        }

        private static bool IsVegetation(ProductionAssetLedgerRecord record)
        {
            return Is(record.Get("technical_class"), "vegetation") ||
                   Normalize(record.Get("asset_bin")).Contains("vegetation");
        }

        private static bool IsCharacter(ProductionAssetLedgerRecord record)
        {
            return Is(record.Get("technical_class"), "character") ||
                   Normalize(record.Get("asset_bin")).Contains("character");
        }

        private static bool TryGetPositiveInt(
            ProductionAssetLedgerRecord record,
            string field,
            out int value)
        {
            return record.TryGetNonNegativeInt(field, out value) && value > 0;
        }

        private static bool TryGetPositiveFloat(
            ProductionAssetLedgerRecord record,
            string field,
            out float value)
        {
            return float.TryParse(
                       record.Get(field),
                       NumberStyles.Float,
                       CultureInfo.InvariantCulture,
                       out value) &&
                   value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsPrimitive(Collider collider)
        {
            return collider is BoxCollider || collider is SphereCollider ||
                   collider is CapsuleCollider;
        }

        private static int TriangleCount(Mesh mesh)
        {
            if (mesh == null)
            {
                return 0;
            }

            int triangles = 0;
            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                if (mesh.GetTopology(subMesh) == MeshTopology.Triangles)
                {
                    triangles += checked((int)(mesh.GetIndexCount(subMesh) / 3));
                }
            }

            return triangles;
        }

        private static bool IsAlphaTested(Material material)
        {
            return material.IsKeywordEnabled("_ALPHATEST_ON") ||
                   (material.HasProperty("_AlphaClip") &&
                    material.GetFloat("_AlphaClip") > 0.5f);
        }

        private static bool Is(string value, string expected)
        {
            return string.Equals(Normalize(value), expected, StringComparison.Ordinal);
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty)
                .Trim()
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();
        }

        private static bool PositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool Approximately(Vector3 left, Vector3 right)
        {
            return (left - right).sqrMagnitude <= 0.00000001f;
        }

        private static bool Approximately(Quaternion left, Quaternion right)
        {
            return Quaternion.Angle(left, right) <= 0.01f;
        }
    }
}
#endif
