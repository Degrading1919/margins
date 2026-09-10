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
        private const string ProductionAssetId = "AOF_001";

        private static readonly string[] LedgerColumns =
        {
            "asset_id", "source_type", "source_name", "creator_or_vendor",
            "source_url", "acquisition_date", "original_version", "license_name",
            "license_url_or_archive_path", "commercial_use_status",
            "modification_permission", "redistribution_restrictions",
            "attribution_requirement", "attribution_text",
            "seat_or_contractor_restrictions", "ai_involvement",
            "ai_tool_or_service", "ai_terms_archive", "source_files_retained",
            "modifications_summary", "lod0_measured_triangles",
            "lod1_measured_triangles", "lod2_measured_triangles",
            "collision_measured_triangles", "measured_material_slots",
            "measured_max_texture_px", "measured_bones",
            "measured_max_influences_per_vertex", "measured_skinned_mesh_renderers",
            "alpha_tested_material_count", "final_distance_representation",
            "normalization_status", "unity_import_status", "performance_review_status",
            "visual_consistency_review_status", "interaction_readability_review_status",
            "owner_acceptance_status", "disposition", "reviewer", "review_date", "notes"
        };

        private static readonly string[] CatalogColumns =
        {
            "asset_id", "asset_name", "asset_bin", "subcategory", "business_use",
            "procedural_category", "capabilities", "technical_class", "raw_source_tris",
            "lod0_ceiling_tris", "lod0_measured_tris", "lod1_ceiling_tris",
            "lod1_measured_tris", "lod2_ceiling_tris", "lod2_measured_tris",
            "collider_type", "collision_ceiling_tris", "collision_measured_tris",
            "material_slots_max", "material_slots_measured", "texture_max_px",
            "expected_visible_instances", "closest_view_m", "interaction_level",
            "animation_requirement", "source_pipeline", "source_or_license",
            "ai_involvement", "normalization_status", "production_status", "notes"
        };

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
        public void CsvParser_PreservesQuotedCommasAndRejectsDuplicateIds()
        {
            const string csv =
                "asset_id,asset_name,notes\nasset.one,\"Shelf, Small\",\"A \"\"quoted\"\" note\"\n";
            Assert.That(
                ProductionAssetLedger.TryParse(
                    csv,
                    out ProductionAssetLedger ledger,
                    out _),
                Is.True);
            Assert.That(
                ledger.TryGetRecord("asset.one", out ProductionAssetLedgerRecord record),
                Is.True);
            Assert.That(record.Get("asset_name"), Is.EqualTo("Shelf, Small"));
            Assert.That(record.Get("notes"), Is.EqualTo("A \"quoted\" note"));

            const string duplicate = "asset_id\nasset.one\nasset.one\n";
            Assert.That(
                ProductionAssetLedger.TryParse(
                    duplicate,
                    out _,
                    out IReadOnlyList<string> errors),
                Is.False);
            Assert.That(errors, Has.Some.Contains("duplicated"));
        }

        [Test]
        public void IntakeMode_ReturnsUnityMeasurementsBeforeLedgerMeasurementsExist()
        {
            ProductionAssetMetadata metadata = CreateAsset();
            ProductionAssetValidationReport report = Validate(
                metadata,
                CreateLedger(includeMeasurements: false),
                CreateCatalog(),
                ProductionAssetValidationMode.IntakeMeasurement);

            Assert.That(ErrorMessages(report), Is.Empty);
            Assert.That(report.Measurements.Lod0Triangles, Is.EqualTo(12));
            Assert.That(report.Measurements.Lod0MaterialSlots, Is.EqualTo(1));
            Assert.That(report.Measurements.CollisionTriangles, Is.Zero);
            Assert.That(report.TechnicalRequirementsPassed, Is.True);
            Assert.That(report.ProductionReady, Is.False);
        }

        [Test]
        public void IncrementalCatalogRow_PassesIntakeButFailsStrictReadiness()
        {
            ProductionAssetMetadata metadata = CreateAsset();
            ProductionAssetBudgetCatalog catalog =
                ParseCatalog(CreateIncrementalCatalogCsv());

            ProductionAssetValidationReport intake = Validate(
                metadata,
                CreateLedger(includeMeasurements: false),
                catalog,
                ProductionAssetValidationMode.IntakeMeasurement);
            ProductionAssetValidationReport strict = Validate(
                metadata,
                CreateLedger(includeMeasurements: true),
                catalog,
                ProductionAssetValidationMode.ProductionReadiness);

            Assert.That(ErrorMessages(intake), Is.Empty);
            Assert.That(intake.Measurements.Lod0Triangles, Is.EqualTo(12));
            Assert.That(intake.Measurements.Lod0MaterialSlots, Is.EqualTo(1));
            Assert.That(intake.TechnicalRequirementsPassed, Is.True);
            string strictErrors = string.Join("\n", ErrorMessages(strict));
            Assert.That(strictErrors, Does.Contain("material_slots_max"));
            Assert.That(strictErrors, Does.Contain("texture_max_px"));
            Assert.That(strictErrors, Does.Contain("collider_type"));
            Assert.That(strictErrors, Does.Contain("expected_visible_instances"));
            Assert.That(strict.ProductionReady, Is.False);
        }

        [Test]
        public void StrictMode_RequiresRecordedMeasurementsAfterIntake()
        {
            ProductionAssetMetadata metadata = CreateAsset();
            ProductionAssetValidationReport intake = Validate(
                metadata,
                CreateLedger(includeMeasurements: false),
                CreateCatalog(),
                ProductionAssetValidationMode.IntakeMeasurement);
            ProductionAssetValidationReport strict = Validate(
                metadata,
                CreateLedger(includeMeasurements: false),
                CreateCatalog(),
                ProductionAssetValidationMode.ProductionReadiness);

            Assert.That(ErrorMessages(intake), Is.Empty);
            Assert.That(
                ErrorMessages(strict),
                Has.Some.Contains("lod0_measured_triangles"));
            Assert.That(strict.ProductionReady, Is.False);
        }

        [Test]
        public void IntakeMode_StillReportsMeasurementsWhenCatalogIsUnavailable()
        {
            ProductionAssetValidationReport report = Validate(
                CreateAsset(),
                CreateLedger(includeMeasurements: false),
                null,
                ProductionAssetValidationMode.IntakeMeasurement);

            Assert.That(report.Measurements.Lod0Triangles, Is.EqualTo(12));
            Assert.That(
                ErrorMessages(report),
                Has.Some.Contains("budget catalog could not be loaded"));
        }

        [Test]
        public void StrictMode_AcceptsMatchingMeasurementsAndOwnerAcceptance()
        {
            ProductionAssetValidationReport report = Validate(
                CreateAsset(),
                CreateLedger(includeMeasurements: true),
                CreateCatalog(),
                ProductionAssetValidationMode.ProductionReadiness);

            Assert.That(ErrorMessages(report), Is.Empty);
            Assert.That(report.TechnicalRequirementsPassed, Is.True);
            Assert.That(report.ProductionReady, Is.True);
        }

        [Test]
        public void CatalogOwnsCeilingsAndLedgerContainsNoBudgetCeilings()
        {
            string ledgerCsv = CreateLedgerCsv(includeMeasurements: true);
            Assert.That(ledgerCsv, Does.Not.Contain("triangle_ceiling"));
            Assert.That(ledgerCsv, Does.Not.Contain("max_material_slots"));

            ProductionAssetValidationReport report = Validate(
                CreateAsset(),
                ParseLedger(ledgerCsv),
                ParseCatalog(CreateCatalogCsv(lod0Ceiling: 10)),
                ProductionAssetValidationMode.IntakeMeasurement);

            Assert.That(
                ErrorMessages(report),
                Has.Some.Contains("catalog ceiling of 10"));
        }

        [Test]
        public void ProceduralIdentityIsValidatedIndependentlyFromProductionIdentity()
        {
            ProductionAssetMetadata metadata = CreateAsset();
            ProceduralAssetComponent procedural =
                metadata.gameObject.AddComponent<ProceduralAssetComponent>();
            procedural.Configure(
                "procedural.display.fixture",
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

            ProductionAssetValidationReport report = Validate(
                metadata,
                CreateLedger(includeMeasurements: false),
                CreateCatalog(),
                ProductionAssetValidationMode.IntakeMeasurement);

            Assert.That(ErrorMessages(report), Is.Empty);
            Assert.That(procedural.StableAssetId, Is.Not.EqualTo(metadata.AssetId));
        }

        [Test]
        public void NonCharacterSkinnedAssetDoesNotInheritCharacterOnlyConstraints()
        {
            ProductionAssetMetadata metadata = CreateAsset();
            AddSkinnedPart(metadata);
            ProductionAssetValidationReport report = Validate(
                metadata,
                CreateLedger(includeMeasurements: false),
                ParseCatalog(CreateCatalogCsv(
                    lod0Ceiling: 30,
                    materialSlotsMaximum: 2)),
                ProductionAssetValidationMode.IntakeMeasurement);

            Assert.That(ErrorMessages(report), Is.Empty);
            Assert.That(report.Measurements.Lod0SkinnedRendererCount, Is.EqualTo(1));
        }

        [Test]
        public void CharacterStrictReadinessFailsWhenCatalogConstraintsAreUnresolved()
        {
            ProductionAssetMetadata metadata = CreateAsset();
            AddSkinnedPart(metadata);
            ProductionAssetValidationReport report = Validate(
                metadata,
                ParseLedger(CreateLedgerCsv(
                    includeMeasurements: true,
                    includeSkinnedMeasurements: true)),
                ParseCatalog(CreateCatalogCsv(
                    lod0Ceiling: 30,
                    assetBin: "Characters & Wearables",
                    technicalClass: "character",
                    materialSlotsMaximum: 2)),
                ProductionAssetValidationMode.ProductionReadiness);

            Assert.That(
                ErrorMessages(report),
                Has.Some.Contains("does not yet expose authoritative character"));
            Assert.That(report.ProductionReady, Is.False);
        }

        [Test]
        public void StrictMode_StillPreservesOwnerControlledGate()
        {
            ProductionAssetValidationReport report = Validate(
                CreateAsset(),
                CreateLedger(
                    includeMeasurements: true,
                    ownerStatus: "pending",
                    disposition: "intake"),
                CreateCatalog(),
                ProductionAssetValidationMode.ProductionReadiness);

            Assert.That(ErrorMessages(report), Is.Empty);
            Assert.That(report.TechnicalRequirementsPassed, Is.True);
            Assert.That(report.ProductionReady, Is.False);
            Assert.That(
                WarningMessages(report),
                Has.Some.Contains("only the owner can accept"));
        }

        [Test]
        public void VegetationClassificationComesFromCatalog()
        {
            ProductionAssetValidationReport report = Validate(
                CreateAsset(),
                CreateLedger(includeMeasurements: true),
                ParseCatalog(CreateCatalogCsv(assetBin: "Vegetation & Environmental Dressing")),
                ProductionAssetValidationMode.ProductionReadiness);
            string errors = string.Join("\n", ErrorMessages(report));

            Assert.That(errors, Does.Contain("final-distance renderer"));
            Assert.That(errors, Does.Contain("final-distance representation"));
        }

        [Test]
        public void StrictMode_RequiresCatalogProductionContext()
        {
            ProductionAssetValidationReport report = Validate(
                CreateAsset(),
                CreateLedger(includeMeasurements: true),
                ParseCatalog(CreateCatalogCsv(includeProductionContext: false)),
                ProductionAssetValidationMode.ProductionReadiness);
            string errors = string.Join("\n", ErrorMessages(report));

            Assert.That(errors, Does.Contain("expected_visible_instances"));
            Assert.That(errors, Does.Contain("closest_view_m"));
            Assert.That(errors, Does.Contain("interaction_level"));
            Assert.That(errors, Does.Contain("animation_requirement"));
        }

        [Test]
        public void Menu_AllowsMissingCatalogOnlyForIntakeMeasurement()
        {
            Type menuType = Type.GetType(
                "Margins.Editor.ProductionAssetValidationMenu, Assembly-CSharp-Editor");
            MethodInfo method = menuType?.GetMethod(
                "CanContinueWithoutCatalog",
                BindingFlags.Public | BindingFlags.Static);

            Assert.That(method, Is.Not.Null);
            Assert.That(
                method.Invoke(
                    null,
                    new object[] { ProductionAssetValidationMode.IntakeMeasurement }),
                Is.True);
            Assert.That(
                method.Invoke(
                    null,
                    new object[] { ProductionAssetValidationMode.ProductionReadiness }),
                Is.False);
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
            GameObject instance = Track(
                (GameObject)PrefabUtility.InstantiatePrefab(prefab));
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
            GameObject root = Track(new GameObject("AOF_Test"));
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

            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            visual.AddComponent<MeshRenderer>().sharedMaterial =
                Track(new Material(Shader.Find("Hidden/InternalErrorShader")));
            root.AddComponent<BoxCollider>();

            ProductionAssetMetadata metadata =
                root.AddComponent<ProductionAssetMetadata>();
            metadata.Configure(ProductionAssetId, Vector3.one, 0.001f, visual.transform);
            return metadata;
        }

        private void AddSkinnedPart(ProductionAssetMetadata metadata)
        {
            MeshFilter sourceFilter = metadata.VisualRoot.GetComponent<MeshFilter>();
            MeshRenderer sourceRenderer =
                metadata.VisualRoot.GetComponent<MeshRenderer>();
            GameObject animatedPart = Track(new GameObject("AnimatedPart"));
            animatedPart.transform.SetParent(metadata.VisualRoot, false);
            SkinnedMeshRenderer skinned =
                animatedPart.AddComponent<SkinnedMeshRenderer>();
            skinned.sharedMesh = sourceFilter.sharedMesh;
            skinned.sharedMaterial = sourceRenderer.sharedMaterial;
        }

        private static ProductionAssetValidationReport Validate(
            ProductionAssetMetadata metadata,
            ProductionAssetLedger ledger,
            ProductionAssetBudgetCatalog catalog,
            ProductionAssetValidationMode mode)
        {
            return ProductionAssetValidator.Validate(metadata, ledger, catalog, mode);
        }

        private static ProductionAssetLedger CreateLedger(
            bool includeMeasurements,
            string ownerStatus = "accepted",
            string disposition = "production")
        {
            return ParseLedger(CreateLedgerCsv(
                includeMeasurements,
                ownerStatus,
                disposition));
        }

        private static string CreateLedgerCsv(
            bool includeMeasurements,
            string ownerStatus = "accepted",
            string disposition = "production",
            bool includeSkinnedMeasurements = false)
        {
            Dictionary<string, string> values = new(StringComparer.Ordinal)
            {
                ["asset_id"] = ProductionAssetId,
                ["source_type"] = "self-created",
                ["source_name"] = "Margins",
                ["creator_or_vendor"] = "owner",
                ["source_url"] = "n/a",
                ["acquisition_date"] = "2026-09-10",
                ["original_version"] = "1",
                ["license_name"] = "project-owned",
                ["license_url_or_archive_path"] = "n/a",
                ["commercial_use_status"] = "documented",
                ["modification_permission"] = "allowed",
                ["redistribution_restrictions"] = "restricted",
                ["attribution_requirement"] = "none",
                ["attribution_text"] = "n/a",
                ["seat_or_contractor_restrictions"] = "n/a",
                ["ai_involvement"] = "none",
                ["ai_tool_or_service"] = "n/a",
                ["ai_terms_archive"] = "n/a",
                ["source_files_retained"] = "yes",
                ["modifications_summary"] = "normalized",
                ["normalization_status"] = "complete",
                ["unity_import_status"] = "complete",
                ["performance_review_status"] = "passed",
                ["visual_consistency_review_status"] = "accepted",
                ["interaction_readability_review_status"] = "accepted",
                ["owner_acceptance_status"] = ownerStatus,
                ["disposition"] = disposition,
                ["reviewer"] = "owner",
                ["review_date"] = "2026-09-10"
            };
            if (includeMeasurements)
            {
                values["lod0_measured_triangles"] = "12";
                values["collision_measured_triangles"] = "0";
                values["measured_material_slots"] = "1";
                values["measured_max_texture_px"] = "0";
                if (includeSkinnedMeasurements)
                {
                    values["lod0_measured_triangles"] = "24";
                    values["measured_material_slots"] = "2";
                    values["measured_bones"] = "0";
                    values["measured_max_influences_per_vertex"] = "0";
                    values["measured_skinned_mesh_renderers"] = "1";
                }
            }

            return CreateCsv(LedgerColumns, values);
        }

        private static ProductionAssetBudgetCatalog CreateCatalog()
        {
            return ParseCatalog(CreateCatalogCsv());
        }

        private static string CreateCatalogCsv(
            int lod0Ceiling = 20,
            string assetBin = "Business Fixtures",
            string technicalClass = "",
            int materialSlotsMaximum = 1,
            bool includeProductionContext = true)
        {
            Dictionary<string, string> values = new(StringComparer.Ordinal)
            {
                ["asset_id"] = ProductionAssetId,
                ["asset_name"] = "Test Asset",
                ["asset_bin"] = assetBin,
                ["subcategory"] = "Tests",
                ["business_use"] = "Shared",
                ["technical_class"] = technicalClass,
                ["lod0_ceiling_tris"] = lod0Ceiling.ToString(),
                ["collider_type"] = "primitive",
                ["material_slots_max"] = materialSlotsMaximum.ToString(),
                ["texture_max_px"] = "2048",
                ["production_status"] = "Planned"
            };
            if (includeProductionContext)
            {
                values["expected_visible_instances"] = "1";
                values["closest_view_m"] = "0.5";
                values["interaction_level"] = "None";
                values["animation_requirement"] = "None";
            }
            return CreateCsv(CatalogColumns, values);
        }

        private static string CreateIncrementalCatalogCsv()
        {
            Dictionary<string, string> values = new(StringComparer.Ordinal)
            {
                ["asset_id"] = ProductionAssetId,
                ["asset_name"] = "Test Asset",
                ["asset_bin"] = "Business Fixtures",
                ["subcategory"] = "Tests",
                ["business_use"] = "Shared",
                ["lod0_ceiling_tris"] = "20",
                ["production_status"] = "Planned"
            };
            return CreateCsv(CatalogColumns, values);
        }

        private static string CreateCsv(
            IReadOnlyList<string> columns,
            IReadOnlyDictionary<string, string> values)
        {
            List<string> row = new(columns.Count);
            foreach (string column in columns)
            {
                row.Add(values.TryGetValue(column, out string value) ? value : string.Empty);
            }

            return string.Join(",", columns) + "\n" + string.Join(",", row) + "\n";
        }

        private static ProductionAssetLedger ParseLedger(string csv)
        {
            Assert.That(
                ProductionAssetLedger.TryParse(
                    csv,
                    out ProductionAssetLedger ledger,
                    out IReadOnlyList<string> errors),
                Is.True,
                string.Join("\n", errors));
            return ledger;
        }

        private static ProductionAssetBudgetCatalog ParseCatalog(string csv)
        {
            Assert.That(
                ProductionAssetBudgetCatalog.TryParse(
                    csv,
                    out ProductionAssetBudgetCatalog catalog,
                    out IReadOnlyList<string> errors),
                Is.True,
                string.Join("\n", errors));
            return catalog;
        }

        private static List<string> ErrorMessages(
            ProductionAssetValidationReport report)
        {
            return Messages(report, ProductionAssetValidationSeverity.Error);
        }

        private static List<string> WarningMessages(
            ProductionAssetValidationReport report)
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
