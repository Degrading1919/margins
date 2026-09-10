#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Margins
{
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

    public sealed class ProductionAssetValidationReport
    {
        private readonly List<ProductionAssetValidationIssue> issues = new();

        public IReadOnlyList<ProductionAssetValidationIssue> Issues => issues;
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
        private static readonly string[] RequiredRecordFields =
        {
            "asset_name",
            "asset_class",
            "intended_use",
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
            "modifications_summary",
            "max_texture_resolution",
            "expected_max_visible_instances",
            "expected_closest_view_distance",
            "animation_or_interaction_requirements",
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
            ProductionAssetLedger ledger)
        {
            ProductionAssetValidationReport report = new();
            if (metadata == null)
            {
                report.AddError("The selected prefab requires ProductionAssetMetadata on its root.");
                return report;
            }

            if (ledger == null)
            {
                report.AddError("The production asset ledger could not be loaded.");
                return report;
            }

            string assetId = metadata.AssetId?.Trim();
            if (string.IsNullOrEmpty(assetId))
            {
                report.AddError("ProductionAssetMetadata requires an asset_id.");
                return report;
            }

            if (!ledger.TryGetRecord(assetId, out ProductionAssetLedgerRecord record))
            {
                report.AddError($"No production ledger row matches asset_id '{assetId}'.");
                return report;
            }

            ValidateProceduralContract(metadata, assetId, report);
            ValidateRecordCompleteness(record, report);
            ValidateTransformsAndDimensions(metadata, report);
            MeshMeasurements measurements = MeasureMeshes(metadata, report);
            ValidateMeshBudgets(record, measurements, report);
            ValidateMaterials(record, measurements, report);
            ValidateCharacterBudgets(record, measurements, report);
            ValidateVegetationBudgets(record, measurements, report);
            ValidateColliders(metadata, record, report);
            ValidateReviewState(record, report);

            report.TechnicalRequirementsPassed = !report.HasErrors();
            report.ProductionReady = report.TechnicalRequirementsPassed &&
                                     Is(record.Get("visual_consistency_review_status"), "accepted") &&
                                     Is(record.Get("interaction_readability_review_status"), "accepted") &&
                                     Is(record.Get("owner_acceptance_status"), "accepted") &&
                                     Is(record.Get("disposition"), "production");
            return report;
        }

        private static void ValidateProceduralContract(
            ProductionAssetMetadata metadata,
            string assetId,
            ProductionAssetValidationReport report)
        {
            ProceduralAssetComponent procedural =
                metadata.GetComponent<ProceduralAssetComponent>();
            if (procedural == null)
            {
                return;
            }

            if (!string.Equals(
                    procedural.StableAssetId,
                    assetId,
                    StringComparison.Ordinal))
            {
                report.AddError(
                    $"Production asset_id '{assetId}' must match procedural StableAssetId '{procedural.StableAssetId}'.");
            }

            if (!procedural.TryValidateConfiguration(out string error))
            {
                report.AddError($"Procedural asset configuration is invalid: {error}");
            }
        }

        private static void ValidateRecordCompleteness(
            ProductionAssetLedgerRecord record,
            ProductionAssetValidationReport report)
        {
            foreach (string field in RequiredRecordFields)
            {
                if (!record.HasColumn(field))
                {
                    report.AddError($"The ledger schema is missing required field '{field}'.");
                }
                else if (string.IsNullOrWhiteSpace(record.Get(field)))
                {
                    report.AddError($"Ledger row {record.RowNumber} requires '{field}' (use 'n/a' when applicable).");
                }
            }

            RequireNonNegativeInt(record, "lod0_triangle_ceiling", report);
            RequireNonNegativeInt(record, "lod0_measured_triangles", report);
            RequireNonNegativeInt(record, "max_material_slots", report);
            RequireNonNegativeInt(record, "measured_material_slots", report);
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
                return;
            }

            if (!TryMeasureLocalBounds(metadata, out Bounds bounds))
            {
                report.AddError("The production prefab contains no measurable render mesh.");
                return;
            }

            Vector3 difference = bounds.size - expected;
            if (Mathf.Abs(difference.x) > metadata.DimensionToleranceMeters ||
                Mathf.Abs(difference.y) > metadata.DimensionToleranceMeters ||
                Mathf.Abs(difference.z) > metadata.DimensionToleranceMeters)
            {
                report.AddError(
                    $"Measured size {bounds.size:F4} m does not match expected size {expected:F4} m within {metadata.DimensionToleranceMeters:F4} m.");
            }

            foreach (Transform descendant in visual.GetComponentsInChildren<Transform>(true))
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

        private static MeshMeasurements MeasureMeshes(
            ProductionAssetMetadata metadata,
            ProductionAssetValidationReport report)
        {
            Transform visual = metadata.VisualRoot;
            MeshMeasurements result = new();
            if (visual == null)
            {
                return result;
            }

            HashSet<Renderer> assigned = new();
            LODGroup[] groups = visual.GetComponentsInChildren<LODGroup>(true);
            foreach (LODGroup group in groups)
            {
                LOD[] lods = group.GetLODs();
                for (int level = 0; level < lods.Length; level++)
                {
                    foreach (Renderer renderer in lods[level].renderers)
                    {
                        if (renderer == null || !assigned.Add(renderer))
                        {
                            continue;
                        }

                        result.Add(level, renderer, report);
                    }
                }
            }

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (assigned.Add(renderer))
                {
                    result.Add(0, renderer, report);
                }
            }

            return result;
        }

        private static void ValidateMeshBudgets(
            ProductionAssetLedgerRecord record,
            MeshMeasurements measurements,
            ProductionAssetValidationReport report)
        {
            ValidateLod(record, measurements, 0, true, report);
            ValidateLod(record, measurements, 1, false, report);
            ValidateLod(record, measurements, 2, false, report);
        }

        private static void ValidateLod(
            ProductionAssetLedgerRecord record,
            MeshMeasurements measurements,
            int level,
            bool required,
            ProductionAssetValidationReport report)
        {
            string ceilingField = $"lod{level}_triangle_ceiling";
            string measuredField = $"lod{level}_measured_triangles";
            string ceilingText = record.Get(ceilingField);
            string measuredText = record.Get(measuredField);
            bool configured = !string.IsNullOrWhiteSpace(ceilingText) ||
                              !string.IsNullOrWhiteSpace(measuredText);
            if (!required && !configured)
            {
                return;
            }

            if (!record.TryGetNonNegativeInt(ceilingField, out int ceiling) || ceiling <= 0)
            {
                report.AddError($"Ledger field '{ceilingField}' requires a positive integer when LOD{level} is used.");
                return;
            }

            if (!record.TryGetNonNegativeInt(measuredField, out int recorded))
            {
                report.AddError($"Ledger field '{measuredField}' requires a non-negative integer.");
                return;
            }

            int actual = measurements.TrianglesFor(level);
            if (level > 0 && actual == 0)
            {
                report.AddError($"The ledger declares LOD{level}, but the prefab has no LOD{level} render mesh.");
            }

            if (actual > ceiling)
            {
                report.AddError($"LOD{level} uses {actual} triangles and exceeds its {ceiling} triangle ceiling.");
            }

            if (actual != recorded)
            {
                report.AddError($"LOD{level} ledger measurement is {recorded}, but Unity measured {actual} triangles.");
            }
        }

        private static void ValidateMaterials(
            ProductionAssetLedgerRecord record,
            MeshMeasurements measurements,
            ProductionAssetValidationReport report)
        {
            if (!record.TryGetNonNegativeInt("max_material_slots", out int maximum))
            {
                return;
            }

            if (measurements.Lod0MaterialSlots > maximum)
            {
                report.AddError($"LOD0 uses {measurements.Lod0MaterialSlots} material slots and exceeds its {maximum}-slot ceiling.");
            }

            if (record.TryGetNonNegativeInt("measured_material_slots", out int recorded) &&
                recorded != measurements.Lod0MaterialSlots)
            {
                report.AddError($"Ledger material measurement is {recorded}, but Unity measured {measurements.Lod0MaterialSlots} LOD0 slots.");
            }

            if (!TryReadPositiveLeadingInt(record.Get("max_texture_resolution"), out int textureMaximum))
            {
                report.AddError("Ledger field 'max_texture_resolution' must begin with a positive pixel dimension, such as 2048 or 2048x2048.");
            }
            else if (measurements.MaximumTextureDimension > textureMaximum)
            {
                report.AddError($"A material uses a {measurements.MaximumTextureDimension}px texture and exceeds the {textureMaximum}px ceiling.");
            }
        }

        private static void ValidateCharacterBudgets(
            ProductionAssetLedgerRecord record,
            MeshMeasurements measurements,
            ProductionAssetValidationReport report)
        {
            if (!Is(record.Get("asset_class"), "character"))
            {
                return;
            }

            if (measurements.Lod0SkinnedRendererCount == 0)
            {
                report.AddError("A character asset requires at least one LOD0 SkinnedMeshRenderer.");
                return;
            }

            ValidateMeasuredLimit(
                record,
                "max_bones",
                "measured_bones",
                measurements.Lod0BoneCount,
                "bones",
                report);
            ValidateMeasuredLimit(
                record,
                "max_skinned_mesh_renderers",
                "measured_skinned_mesh_renderers",
                measurements.Lod0SkinnedRendererCount,
                "skinned mesh renderers",
                report);

            if (!record.TryGetNonNegativeInt("max_influences_per_vertex", out int influenceLimit) ||
                influenceLimit <= 0)
            {
                report.AddError("Skinned assets require a positive max_influences_per_vertex.");
            }
            else if (measurements.MaximumInfluencesPerVertex > influenceLimit)
            {
                report.AddError($"The skinned asset uses {measurements.MaximumInfluencesPerVertex} influences per vertex and exceeds its {influenceLimit} limit.");
            }

            if (!record.TryGetNonNegativeInt("complete_character_max_material_slots", out int characterMaterialLimit) ||
                characterMaterialLimit <= 0)
            {
                report.AddError("Skinned assets require a positive complete_character_max_material_slots value.");
            }
            else if (measurements.Lod0MaterialSlots > characterMaterialLimit)
            {
                report.AddError($"The skinned asset uses {measurements.Lod0MaterialSlots} material slots and exceeds its {characterMaterialLimit}-slot character limit.");
            }
        }

        private static void ValidateVegetationBudgets(
            ProductionAssetLedgerRecord record,
            MeshMeasurements measurements,
            ProductionAssetValidationReport report)
        {
            if (!Is(record.Get("asset_class"), "vegetation"))
            {
                return;
            }

            if (!record.TryGetNonNegativeInt(
                    "alpha_tested_material_count",
                    out int recordedAlphaMaterials))
            {
                report.AddError("Vegetation assets require a non-negative alpha_tested_material_count.");
            }
            else if (recordedAlphaMaterials != measurements.AlphaTestedMaterialCount)
            {
                report.AddError($"Ledger alpha-tested material count is {recordedAlphaMaterials}, but Unity measured {measurements.AlphaTestedMaterialCount}.");
            }

            if (measurements.AlphaTestedMaterialCount > 1)
            {
                report.AddWarning("Vegetation uses more than one alpha-tested material; the approved default requires technical and visual exception review.");
            }

            string finalRepresentation = Normalize(record.Get("final_distance_representation"));
            if (string.IsNullOrEmpty(finalRepresentation) ||
                finalRepresentation == "na" || finalRepresentation == "none")
            {
                report.AddError("Vegetation assets require a recorded billboard, impostor, or very-low-cost final-distance representation.");
            }

            if (measurements.HighestLodLevel == 0)
            {
                report.AddError("Vegetation assets require an LODGroup with a final-distance renderer.");
            }
        }

        private static void ValidateMeasuredLimit(
            ProductionAssetLedgerRecord record,
            string maximumField,
            string measuredField,
            int actual,
            string label,
            ProductionAssetValidationReport report)
        {
            if (!record.TryGetNonNegativeInt(maximumField, out int maximum) || maximum <= 0)
            {
                report.AddError($"Skinned assets require a positive {maximumField} value.");
                return;
            }

            if (!record.TryGetNonNegativeInt(measuredField, out int measured))
            {
                report.AddError($"Skinned assets require a non-negative {measuredField} value.");
                return;
            }

            if (actual > maximum)
            {
                report.AddError($"The skinned asset uses {actual} {label} and exceeds its {maximum} limit.");
            }

            if (actual != measured)
            {
                report.AddError($"Ledger {label} measurement is {measured}, but Unity measured {actual}.");
            }
        }

        private static void ValidateColliders(
            ProductionAssetMetadata metadata,
            ProductionAssetLedgerRecord record,
            ProductionAssetValidationReport report)
        {
            Collider[] colliders = metadata.GetComponentsInChildren<Collider>(true);
            if (colliders.Length == 0)
            {
                report.AddError("The production prefab requires at least one Unity collider.");
                return;
            }

            string policy = Normalize(record.Get("collider_type"));
            int collisionTriangles = 0;
            foreach (Collider collider in colliders)
            {
                if (collider is MeshCollider meshCollider)
                {
                    collisionTriangles += TriangleCount(meshCollider.sharedMesh);
                }

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
                    report.AddError($"Collider '{collider.name}' does not match ledger collider_type '{record.Get("collider_type")}'.");
                }
            }

            if (policy == "primitive" && colliders.Length != 1)
            {
                report.AddError("A primitive collider record requires exactly one primitive collider; use compound primitive for multiple colliders.");
            }
            else if (policy == "compoundprimitive" && colliders.Length < 2)
            {
                report.AddError("A compound primitive collider record requires at least two primitive colliders.");
            }
            else if (policy == "staticmesh" && !metadata.gameObject.isStatic)
            {
                report.AddError("A static mesh collider asset must be marked Static in Unity.");
            }
            else if (string.IsNullOrEmpty(policy) ||
                     (policy != "primitive" && policy != "compoundprimitive" &&
                      policy != "convexmesh" && policy != "staticmesh"))
            {
                report.AddError("Ledger collider_type must be primitive, compound primitive, convex mesh, or static mesh.");
            }

            bool meshPolicy = policy == "convexmesh" || policy == "staticmesh";
            if (meshPolicy)
            {
                if (!record.TryGetNonNegativeInt("collision_triangle_ceiling", out int ceiling) ||
                    ceiling <= 0)
                {
                    report.AddError("Mesh collider records require a positive collision_triangle_ceiling.");
                }
                else if (collisionTriangles > ceiling)
                {
                    report.AddError($"Collision meshes use {collisionTriangles} triangles and exceed their {ceiling} triangle ceiling.");
                }
            }

            if (!record.TryGetNonNegativeInt("collision_measured_triangles", out int recorded))
            {
                report.AddError("Ledger field 'collision_measured_triangles' requires a non-negative integer.");
            }
            else if (recorded != collisionTriangles)
            {
                report.AddError($"Ledger collision measurement is {recorded}, but Unity measured {collisionTriangles} triangles.");
            }
        }

        private static void ValidateReviewState(
            ProductionAssetLedgerRecord record,
            ProductionAssetValidationReport report)
        {
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

        private static void RequireStatus(
            ProductionAssetLedgerRecord record,
            string field,
            string required,
            ProductionAssetValidationReport report)
        {
            if (!Is(record.Get(field), required))
            {
                report.AddError($"Ledger field '{field}' must be '{required}' for technical validation.");
            }
        }

        private static void RequireNonNegativeInt(
            ProductionAssetLedgerRecord record,
            string field,
            ProductionAssetValidationReport report)
        {
            if (!record.HasColumn(field))
            {
                report.AddError($"The ledger schema is missing required field '{field}'.");
            }
            else if (!record.TryGetNonNegativeInt(field, out _))
            {
                report.AddError($"Ledger field '{field}' requires a non-negative integer.");
            }
        }

        private static bool TryMeasureLocalBounds(
            ProductionAssetMetadata metadata,
            out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            Matrix4x4 worldToRoot = metadata.transform.worldToLocalMatrix;
            Transform visual = metadata.VisualRoot;
            if (visual == null)
            {
                return false;
            }

            foreach (MeshFilter filter in visual.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                Encapsulate(
                    filter.sharedMesh.bounds,
                    worldToRoot * filter.transform.localToWorldMatrix,
                    ref bounds,
                    ref found);
            }

            foreach (SkinnedMeshRenderer renderer in
                     visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMesh == null)
                {
                    continue;
                }

                Encapsulate(
                    renderer.localBounds,
                    worldToRoot * renderer.transform.localToWorldMatrix,
                    ref bounds,
                    ref found);
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

        private static bool TryReadPositiveLeadingInt(string value, out int result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            int length = 0;
            while (length < value.Length && char.IsDigit(value[length]))
            {
                length++;
            }

            return length > 0 && int.TryParse(value.Substring(0, length), out result) &&
                   result > 0;
        }

        private sealed class MeshMeasurements
        {
            private readonly int[] triangles = new int[3];
            private readonly HashSet<Transform> lod0Bones = new();
            private readonly HashSet<Texture> lod0Textures = new();
            private readonly HashSet<Material> lod0Materials = new();

            public int Lod0MaterialSlots { get; private set; }
            public int Lod0SkinnedRendererCount { get; private set; }
            public int Lod0BoneCount => lod0Bones.Count;
            public int MaximumInfluencesPerVertex { get; private set; }
            public int MaximumTextureDimension { get; private set; }
            public int AlphaTestedMaterialCount { get; private set; }
            public int HighestLodLevel { get; private set; }

            public int TrianglesFor(int level)
            {
                return level >= 0 && level < triangles.Length ? triangles[level] : 0;
            }

            public void Add(
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

                triangles[level] += TriangleCount(mesh);
                HighestLodLevel = Mathf.Max(HighestLodLevel, level);
                if (level == 0)
                {
                    Material[] materials = renderer.sharedMaterials;
                    Lod0MaterialSlots += materials.Length;
                    for (int index = 0; index < materials.Length; index++)
                    {
                        if (materials[index] == null)
                        {
                            report.AddError($"Renderer '{renderer.name}' has an unassigned material slot.");
                            continue;
                        }

                        if (lod0Materials.Add(materials[index]) &&
                            IsAlphaTested(materials[index]))
                        {
                            AlphaTestedMaterialCount++;
                        }

                        foreach (string propertyName in materials[index].GetTexturePropertyNames())
                        {
                            Texture texture = materials[index].GetTexture(propertyName);
                            if (texture != null && lod0Textures.Add(texture))
                            {
                                MaximumTextureDimension = Mathf.Max(
                                    MaximumTextureDimension,
                                    Mathf.Max(texture.width, texture.height));
                            }
                        }
                    }

                    if (renderer is SkinnedMeshRenderer skinnedRenderer)
                    {
                        Lod0SkinnedRendererCount++;
                        foreach (Transform bone in skinnedRenderer.bones)
                        {
                            if (bone != null)
                            {
                                lod0Bones.Add(bone);
                            }
                        }

                        using var influences = mesh.GetBonesPerVertex();
                        foreach (byte influenceCount in influences)
                        {
                            MaximumInfluencesPerVertex = Mathf.Max(
                                MaximumInfluencesPerVertex,
                                influenceCount);
                        }
                    }
                }
            }

            private static bool IsAlphaTested(Material material)
            {
                return material.IsKeywordEnabled("_ALPHATEST_ON") ||
                       (material.HasProperty("_AlphaClip") &&
                        material.GetFloat("_AlphaClip") > 0.5f);
            }
        }
    }
}
#endif
