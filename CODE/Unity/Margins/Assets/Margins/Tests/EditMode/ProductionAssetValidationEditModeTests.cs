using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Margins.Tests
{
    public sealed class ProductionAssetValidationEditModeTests
    {
        private readonly List<UnityEngine.Object> created = new();
        private readonly List<string> createdAssetPaths = new();

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object item in created)
            {
                if (item != null)
                {
                    UnityEngine.Object.DestroyImmediate(item);
                }
            }

            created.Clear();
            foreach (string path in createdAssetPaths)
            {
                AssetDatabase.DeleteAsset(path);
            }

            createdAssetPaths.Clear();
        }

        [Test]
        public void LedgerParser_PreservesQuotedCommasAndRejectsDuplicateIds()
        {
            const string csv = "asset_id,asset_name,notes\nasset.one,\"Shelf, Small\",\"A \"\"quoted\"\" note\"\n";
            Assert.That(
                ProductionAssetLedger.TryParse(csv, out ProductionAssetLedger ledger, out _),
                Is.True);
            Assert.That(ledger.TryGetRecord("asset.one", out ProductionAssetLedgerRecord record), Is.True);
            Assert.That(record.Get("asset_name"), Is.EqualTo("Shelf, Small"));
            Assert.That(record.Get("notes"), Is.EqualTo("A \"quoted\" note"));

            const string duplicate = "asset_id\nasset.one\nasset.one\n";
            Assert.That(
                ProductionAssetLedger.TryParse(duplicate, out _, out IReadOnlyList<string> errors),
                Is.False);
            Assert.That(errors, Has.Some.Contains("duplicated"));
        }

        [Test]
        public void Validator_AcceptsCompliantPrimitiveAssetWithOwnerAcceptance()
        {
            ProductionAssetMetadata metadata = CreateAsset();
            ProductionAssetLedger ledger = ParseLedger(CreateRecord(
                lod0Ceiling: 20,
                lod0Measured: 12,
                materialCeiling: 1,
                measuredMaterials: 1,
                colliderType: "primitive",
                collisionCeiling: "0",
                collisionMeasured: 0,
                ownerStatus: "accepted",
                disposition: "production"));

            ProductionAssetValidationReport report =
                ProductionAssetValidator.Validate(metadata, ledger);

            Assert.That(ErrorMessages(report), Is.Empty);
            Assert.That(report.TechnicalRequirementsPassed, Is.True);
            Assert.That(report.ProductionReady, Is.True);
        }

        [Test]
        public void Validator_ReportsBudgetMeasurementTransformAndColliderFailures()
        {
            ProductionAssetMetadata metadata = CreateAsset();
            metadata.transform.localScale = new Vector3(2f, 1f, 1f);
            ProductionAssetLedger ledger = ParseLedger(CreateRecord(
                lod0Ceiling: 10,
                lod0Measured: 10,
                materialCeiling: 0,
                measuredMaterials: 0,
                colliderType: "static mesh",
                collisionCeiling: "5",
                collisionMeasured: 5,
                ownerStatus: "accepted",
                disposition: "production"));

            ProductionAssetValidationReport report =
                ProductionAssetValidator.Validate(metadata, ledger);
            string errors = string.Join("\n", ErrorMessages(report));

            Assert.That(errors, Does.Contain("unit local scale"));
            Assert.That(errors, Does.Contain("exceeds its 10 triangle ceiling"));
            Assert.That(errors, Does.Contain("exceeds its 0-slot ceiling"));
            Assert.That(errors, Does.Contain("does not match ledger collider_type"));
            Assert.That(report.ProductionReady, Is.False);
        }

        [Test]
        public void Validator_LeavesManualReviewAsOwnerControlledGate()
        {
            ProductionAssetMetadata metadata = CreateAsset();
            ProductionAssetLedger ledger = ParseLedger(CreateRecord(
                lod0Ceiling: 20,
                lod0Measured: 12,
                materialCeiling: 1,
                measuredMaterials: 1,
                colliderType: "primitive",
                collisionCeiling: "0",
                collisionMeasured: 0,
                ownerStatus: "pending",
                disposition: "intake"));

            ProductionAssetValidationReport report =
                ProductionAssetValidator.Validate(metadata, ledger);

            Assert.That(ErrorMessages(report), Is.Empty);
            Assert.That(report.TechnicalRequirementsPassed, Is.True);
            Assert.That(report.ProductionReady, Is.False);
            Assert.That(WarningMessages(report), Has.Some.Contains("only the owner can accept"));
        }

        [Test]
        public void Validator_RequiresMatchingValidProceduralContractWhenPresent()
        {
            ProductionAssetMetadata metadata = CreateAsset();
            ProceduralAssetComponent procedural =
                metadata.gameObject.AddComponent<ProceduralAssetComponent>();
            procedural.Configure(
                "asset.other",
                ProceduralAssetCategory.Display,
                Array.Empty<string>(),
                ProceduralMountingMode.Floor,
                ProceduralAccessMode.Customer,
                ProceduralInteractionSide.Front,
                ProceduralWallRelationship.Neutral,
                ProceduralEnvironment.IndoorOnly,
                ProceduralAssemblyBehavior.Independent,
                ProceduralResizeBehavior.Fixed,
                ProceduralPivotConvention.FloorBottomCenter,
                Vector3.one,
                0f,
                string.Empty,
                Array.Empty<ProceduralClearanceDefinition>(),
                metadata.VisualRoot,
                metadata.GetComponent<BoxCollider>(),
                Color.gray);
            ProductionAssetLedger ledger = ParseLedger(CreateRecord(
                20, 12, 1, 1, "primitive", "0", 0, "accepted", "production"));

            ProductionAssetValidationReport report =
                ProductionAssetValidator.Validate(metadata, ledger);

            Assert.That(
                ErrorMessages(report),
                Has.Some.Contains("must match procedural StableAssetId"));
        }

        [Test]
        public void Validator_DoesNotApplyCharacterLimitsToSkinnedNonCharacterAsset()
        {
            ProductionAssetMetadata metadata = CreateAsset();
            MeshRenderer meshRenderer =
                metadata.VisualRoot.GetComponent<MeshRenderer>();
            GameObject animatedPart = Track(new GameObject("AnimatedPart"));
            animatedPart.transform.SetParent(metadata.VisualRoot, false);
            SkinnedMeshRenderer skinned = animatedPart.AddComponent<SkinnedMeshRenderer>();
            skinned.sharedMesh = metadata.VisualRoot.GetComponent<MeshFilter>().sharedMesh;
            skinned.sharedMaterial = meshRenderer.sharedMaterial;
            ProductionAssetLedger ledger = ParseLedger(CreateRecord(
                30, 24, 2, 2, "primitive", "0", 0, "accepted", "production",
                "animated_prop"));

            ProductionAssetValidationReport report =
                ProductionAssetValidator.Validate(metadata, ledger);

            Assert.That(ErrorMessages(report), Is.Empty);
            Assert.That(report.ProductionReady, Is.True);
        }

        [Test]
        public void Validator_RequiresVegetationSpecificRecordsAndDistanceLod()
        {
            ProductionAssetMetadata metadata = CreateAsset();
            ProductionAssetLedger ledger = ParseLedger(CreateRecord(
                20, 12, 1, 1, "primitive", "0", 0, "accepted", "production",
                "vegetation"));

            ProductionAssetValidationReport report =
                ProductionAssetValidator.Validate(metadata, ledger);
            string errors = string.Join("\n", ErrorMessages(report));

            Assert.That(errors, Does.Contain("alpha_tested_material_count"));
            Assert.That(errors, Does.Contain("final-distance representation"));
            Assert.That(errors, Does.Contain("LODGroup"));
            Assert.That(report.ProductionReady, Is.False);
        }

        [Test]
        public void Menu_ResolvesRotatedPrefabInstanceToSourceAsset()
        {
            const string prefabPath =
                "Assets/Margins/Tests/TempProductionAssetValidation.prefab";
            ProductionAssetMetadata metadata = CreateAsset();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                metadata.gameObject,
                prefabPath);
            createdAssetPaths.Add(prefabPath);
            GameObject instance = Track((GameObject)PrefabUtility.InstantiatePrefab(prefab));
            instance.transform.SetPositionAndRotation(
                new Vector3(4f, 0f, 3f),
                Quaternion.Euler(0f, 90f, 0f));

            Type menuType = Type.GetType(
                "Margins.Editor.ProductionAssetValidationMenu, Assembly-CSharp-Editor");
            MethodInfo resolver = menuType?.GetMethod(
                "ResolvePrefabAssetPath",
                BindingFlags.Public | BindingFlags.Static);

            Assert.That(menuType, Is.Not.Null);
            Assert.That(resolver, Is.Not.Null);
            Assert.That(
                resolver.Invoke(null, new object[] { instance }),
                Is.EqualTo(prefabPath));
        }

        private ProductionAssetMetadata CreateAsset()
        {
            GameObject root = Track(new GameObject("PROP_Test"));
            GameObject visual = Track(new GameObject("Visual"));
            visual.transform.SetParent(root.transform, false);

            Mesh mesh = Track(new Mesh { name = "TestMesh" });
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 1f, -0.5f), new Vector3(-0.5f, 1f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f),
                new Vector3(0.5f, 1f, 0.5f), new Vector3(-0.5f, 1f, 0.5f)
            };
            mesh.triangles = new[]
            {
                0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4, 2, 3, 7, 2, 7, 6,
                0, 4, 7, 0, 7, 3, 1, 2, 6, 1, 6, 5
            };
            mesh.RecalculateBounds();

            MeshFilter filter = visual.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Track(new Material(Shader.Find("Hidden/InternalErrorShader")));
            root.AddComponent<BoxCollider>();

            ProductionAssetMetadata metadata = root.AddComponent<ProductionAssetMetadata>();
            metadata.Configure("asset.test", Vector3.one, 0.001f, visual.transform);
            return metadata;
        }

        private static ProductionAssetLedger ParseLedger(string csv)
        {
            Assert.That(
                ProductionAssetLedger.TryParse(csv, out ProductionAssetLedger ledger, out IReadOnlyList<string> errors),
                Is.True,
                string.Join("\n", errors));
            return ledger;
        }

        private static string CreateRecord(
            int lod0Ceiling,
            int lod0Measured,
            int materialCeiling,
            int measuredMaterials,
            string colliderType,
            string collisionCeiling,
            int collisionMeasured,
            string ownerStatus,
            string disposition,
            string assetClass = "prop")
        {
            const string header =
                "asset_id,asset_name,asset_class,intended_use,source_type,source_name,creator_or_vendor," +
                "source_url,acquisition_date,original_version,license_name,license_url_or_archive_path," +
                "commercial_use_status,modification_permission,redistribution_restrictions," +
                "attribution_requirement,attribution_text,seat_or_contractor_restrictions,ai_involvement," +
                "ai_tool_or_service,ai_terms_archive,source_files_retained," +
                "modifications_summary,lod0_triangle_ceiling,lod0_measured_triangles,lod1_triangle_ceiling," +
                "lod1_measured_triangles,lod2_triangle_ceiling,lod2_measured_triangles,collider_type," +
                "collision_triangle_ceiling,collision_measured_triangles,max_material_slots," +
                "measured_material_slots,max_texture_resolution,expected_max_visible_instances," +
                "expected_closest_view_distance,animation_or_interaction_requirements,normalization_status," +
                "unity_import_status,performance_review_status,visual_consistency_review_status," +
                "interaction_readability_review_status,owner_acceptance_status,disposition,reviewer,review_date";
            string row = string.Join(",", new[]
            {
                "asset.test", "Test Asset", assetClass, "tests", "self-created", "Margins", "owner",
                "n/a", "2026-09-10", "1", "project-owned", "n/a", "documented", "allowed",
                "restricted", "none", "n/a", "n/a", "none", "n/a", "n/a", "yes", "normalized",
                lod0Ceiling.ToString(), lod0Measured.ToString(), "", "", "", "",
                colliderType, collisionCeiling, collisionMeasured.ToString(), materialCeiling.ToString(),
                measuredMaterials.ToString(), "2048", "1", "0.5 m", "none", "complete", "complete",
                "passed", "accepted", "accepted", ownerStatus, disposition, "owner", "2026-09-10"
            });
            return header + "\n" + row + "\n";
        }

        private static List<string> ErrorMessages(ProductionAssetValidationReport report)
        {
            return Messages(report, ProductionAssetValidationSeverity.Error);
        }

        private static List<string> WarningMessages(ProductionAssetValidationReport report)
        {
            return Messages(report, ProductionAssetValidationSeverity.Warning);
        }

        private static List<string> Messages(
            ProductionAssetValidationReport report,
            ProductionAssetValidationSeverity severity)
        {
            List<string> messages = new();
            foreach (ProductionAssetValidationIssue issue in report.Issues)
            {
                if (issue.Severity == severity)
                {
                    messages.Add(issue.Message);
                }
            }

            return messages;
        }

        private T Track<T>(T item) where T : UnityEngine.Object
        {
            created.Add(item);
            return item;
        }
    }
}
