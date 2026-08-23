using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Margins.Tests
{
    public sealed class ProceduralCommercialGenerationEditModeTests
    {
        private const string RegistryPath =
            "Assets/Margins/Content/Procedural/Resources/ProceduralAssetRegistry.asset";
        private const string ConveniencePath =
            "Assets/Margins/Content/Procedural/Resources/Recipes/GrayboxConvenienceRecipe.asset";
        private const string LaundromatPath =
            "Assets/Margins/Content/Procedural/Resources/Recipes/GrayboxLaundromatRecipe.asset";
        private const string FuelPath =
            "Assets/Margins/Content/Procedural/Resources/Recipes/GrayboxFuelKioskRecipe.asset";

        private ProceduralAssetRegistry registry;
        private ProceduralBusinessRecipe convenience;
        private ProceduralBusinessRecipe laundromat;
        private ProceduralBusinessRecipe fuel;

        [SetUp]
        public void SetUp()
        {
            registry = AssetDatabase.LoadAssetAtPath<ProceduralAssetRegistry>(RegistryPath);
            convenience = AssetDatabase.LoadAssetAtPath<ProceduralBusinessRecipe>(
                ConveniencePath);
            laundromat = AssetDatabase.LoadAssetAtPath<ProceduralBusinessRecipe>(
                LaundromatPath);
            fuel = AssetDatabase.LoadAssetAtPath<ProceduralBusinessRecipe>(FuelPath);
            Assert.That(registry, Is.Not.Null);
            Assert.That(convenience, Is.Not.Null);
            Assert.That(laundromat, Is.Not.Null);
            Assert.That(fuel, Is.Not.Null);
        }

        [Test]
        public void DimensionalConvention_RemainsMeterNativeAndCentralized()
        {
            Assert.That(CommercialGenerationDimensions.MetersPerUnityUnit, Is.EqualTo(1f));
            Assert.That(CommercialGenerationDimensions.MetersPerInch,
                Is.EqualTo(0.0254f).Within(0.000001f));
            Assert.That(CommercialGenerationDimensions.StructuralIncrementMeters,
                Is.EqualTo(0.3048f).Within(0.000001f));
            Assert.That(CommercialGenerationDimensions.OpeningIncrementMeters,
                Is.EqualTo(
                    CommercialGenerationDimensions.ArchitecturalOpeningIncrementInches *
                    CommercialGenerationDimensions.MetersPerInch).Within(0.000001f));
            Assert.That(CommercialGenerationDimensions.FixturePlacementIncrementMeters,
                Is.EqualTo(FixturePlacementGrid.PlacementIncrementMeters)
                    .Within(0.000001f));
            Assert.That(CommercialGenerationDimensions.ExteriorWallThicknessMeters,
                Is.EqualTo(0.2032f).Within(0.000001f));
            Assert.That(CommercialGenerationDimensions.InteriorPartitionThicknessMeters,
                Is.EqualTo(0.1143f).Within(0.000001f));
            Assert.That(CommercialGenerationDimensions.WallFaceOffset(
                    CommercialGenerationDimensions.ExteriorWallThicknessMeters),
                Is.EqualTo(0.1016f).Within(0.000001f));
            Assert.That(CommercialGenerationDimensions.WallFaceOffset(
                    CommercialGenerationDimensions.InteriorPartitionThicknessMeters),
                Is.EqualTo(0.05715f).Within(0.000001f));
        }

        [Test]
        public void ArchitecturalOpeningIncrement_HasItsOwnSixInchConvention()
        {
            Assert.That(
                CommercialGenerationDimensions.ArchitecturalOpeningIncrementInches,
                Is.EqualTo(6f));
            Assert.That(CommercialGenerationDimensions.OpeningIncrementMeters,
                Is.EqualTo(
                    CommercialGenerationDimensions.ArchitecturalOpeningIncrementInches *
                    CommercialGenerationDimensions.MetersPerInch).Within(0.000001f));
        }

        [Test]
        public void Registry_ProvidesAllNineCategoriesAndValidReplaceableRoots()
        {
            Assert.That(registry.TryValidate(true, out string error), Is.True, error);
            HashSet<ProceduralAssetCategory> categories = new();
            foreach (GameObject prefab in registry.AssetPrefabs)
            {
                ProceduralAssetComponent asset =
                    prefab.GetComponent<ProceduralAssetComponent>();
                Assert.That(asset, Is.Not.Null, prefab.name);
                Assert.That(asset.PrimaryCollider, Is.Not.Null, prefab.name);
                Assert.That(asset.PrimaryCollider.transform, Is.SameAs(prefab.transform));
                Assert.That(asset.VisualRoot, Is.Not.Null, prefab.name);
                Assert.That(asset.VisualRoot.parent, Is.SameAs(prefab.transform));
                Assert.That(asset.VisualRoot.name, Is.EqualTo("Visual"));
                Assert.That(asset.VisualRoot.GetComponentsInChildren<Renderer>(true),
                    Is.Not.Empty, prefab.name);
                categories.Add(asset.PrimaryCategory);
            }

            Assert.That(categories, Is.EquivalentTo(
                Enum.GetValues(typeof(ProceduralAssetCategory))
                    .Cast<ProceduralAssetCategory>()));
        }

        [Test]
        public void Registry_RepresentativeAssetsExerciseApprovedTraits()
        {
            List<ProceduralAssetComponent> assets = registry.AssetPrefabs
                .Select(item => item.GetComponent<ProceduralAssetComponent>())
                .ToList();
            Assert.That(assets.Any(item =>
                (item.MountingModes & ProceduralMountingMode.Wall) != 0), Is.True);
            Assert.That(assets.Any(item =>
                (item.MountingModes & ProceduralMountingMode.Socket) != 0), Is.True);
            Assert.That(assets.Any(item =>
                (item.MountingModes & ProceduralMountingMode.ExteriorPad) != 0), Is.True);
            Assert.That(assets.Any(item => item.AccessMode == ProceduralAccessMode.Customer),
                Is.True);
            Assert.That(assets.Any(item => item.AccessMode == ProceduralAccessMode.Staff),
                Is.True);
            Assert.That(assets.Any(item =>
                item.AssemblyBehavior == ProceduralAssemblyBehavior.BankOrRepeatedRow),
                Is.True);
            Assert.That(assets.Any(item =>
                item.ResizeBehavior == ProceduralResizeBehavior.Repeatable &&
                item.Anchors.Count >= 2), Is.True);
            Assert.That(assets.Any(item => item.Sockets.Count > 0), Is.True);
        }

        [Test]
        public void Recipes_AreCategoryCapabilityBasedWithMinPreferredMaxRequests()
        {
            foreach (ProceduralBusinessRecipe recipe in
                     new[] { convenience, laundromat, fuel })
            {
                Assert.That(recipe.TryValidate(out string error), Is.True, error);
                foreach (ProceduralAssetRequest request in recipe.AssetRequests)
                {
                    Assert.That(request.Minimum, Is.LessThanOrEqualTo(request.Preferred));
                    Assert.That(request.Preferred, Is.LessThanOrEqualTo(request.Maximum));
                    Assert.That(request.RequiredCapabilities, Is.Not.Empty);
                    Assert.That(registry.FindCandidates(
                        request.PrimaryCategory,
                        request.RequiredCapabilities,
                        request.Environment), Is.Not.Empty, request.RequestId);
                }
            }
        }

        [Test]
        public void SameSeedAndInputs_ProduceIdenticalCanonicalLayout()
        {
            ProceduralBuildingRequest request = Request(
                1337,
                CommercialBuildingArchetype.StandaloneSmallCommercial,
                OrthogonalFootprintKind.Rectangle,
                48,
                50,
                convenience);
            ProceduralGenerationResult first = Generate(request);
            ProceduralGenerationResult second = Generate(request);

            Assert.That(second.CanonicalSignature(), Is.EqualTo(first.CanonicalSignature()));
            Assert.That(second.Placements.Select(PlacementKey),
                Is.EqualTo(first.Placements.Select(PlacementKey)));
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentValidLayouts()
        {
            ProceduralGenerationResult first = Generate(Request(
                101,
                CommercialBuildingArchetype.StandaloneSmallCommercial,
                OrthogonalFootprintKind.LShape,
                60,
                58,
                convenience));
            ProceduralGenerationResult second = Generate(Request(
                202,
                CommercialBuildingArchetype.StandaloneSmallCommercial,
                OrthogonalFootprintKind.LShape,
                60,
                58,
                convenience));

            Assert.That(second.CanonicalSignature(), Is.Not.EqualTo(first.CanonicalSignature()));
            Assert.That(second.Placements.Select(PlacementKey),
                Is.Not.EqualTo(first.Placements.Select(PlacementKey)));
        }

        [Test]
        public void SeedSweep_ProducesDistinctValidLayoutsAcrossApprovedArchetypes()
        {
            HashSet<string> signatures = new();
            for (int seed = 1; seed <= 4; seed++)
            {
                signatures.Add(Generate(Request(
                    seed,
                    CommercialBuildingArchetype.StandaloneSmallCommercial,
                    OrthogonalFootprintKind.Rectangle,
                    48,
                    50,
                    convenience)).CanonicalSignature());
                signatures.Add(Generate(Request(
                    seed,
                    CommercialBuildingArchetype.StripCenterInlineRetail,
                    OrthogonalFootprintKind.Rectangle,
                    72,
                    50,
                    laundromat)).CanonicalSignature());
                signatures.Add(Generate(Request(
                    seed,
                    CommercialBuildingArchetype.OlderMainStreetMixedUse,
                    OrthogonalFootprintKind.SteppedRectangle,
                    60,
                    58,
                    convenience)).CanonicalSignature());
            }

            Assert.That(signatures.Count, Is.GreaterThanOrEqualTo(10));
        }

        [Test]
        public void ArchetypesGenerateRectangleMergedStripAndMixedUseLShape()
        {
            ProceduralGenerationResult standalone = Generate(Request(
                31,
                CommercialBuildingArchetype.StandaloneSmallCommercial,
                OrthogonalFootprintKind.Rectangle,
                48,
                50,
                convenience));
            ProceduralGenerationResult strip = Generate(Request(
                32,
                CommercialBuildingArchetype.StripCenterInlineRetail,
                OrthogonalFootprintKind.Rectangle,
                72,
                50,
                laundromat));
            ProceduralGenerationResult mixedUse = Generate(Request(
                33,
                CommercialBuildingArchetype.OlderMainStreetMixedUse,
                OrthogonalFootprintKind.LShape,
                60,
                58,
                convenience));

            Assert.That(standalone.StoryCount, Is.EqualTo(1));
            Assert.That(strip.Bays.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(strip.Units.Single().SourceBayIds.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(mixedUse.StoryCount, Is.InRange(2, 4));
            Assert.That(mixedUse.Footprint.Rectangles.Count, Is.EqualTo(2));
            Assert.That(mixedUse.Walls.Select(item => item.FrontageRole),
                Does.Contain(FrontageRole.PrimaryPublic));
            Assert.That(mixedUse.Walls.Select(item => item.FrontageRole),
                Does.Contain(FrontageRole.RearService));
        }

        [Test]
        public void GeneratedLayoutsRespectOpeningsClearancesAndTraversableCirculation()
        {
            foreach (ProceduralBuildingRequest request in new[]
                     {
                         Request(41, CommercialBuildingArchetype.StandaloneSmallCommercial,
                             OrthogonalFootprintKind.Rectangle, 48, 50, convenience),
                         Request(42, CommercialBuildingArchetype.StripCenterInlineRetail,
                             OrthogonalFootprintKind.Rectangle, 72, 50, laundromat),
                         Request(43, CommercialBuildingArchetype.OlderMainStreetMixedUse,
                             OrthogonalFootprintKind.SteppedRectangle, 60, 58, convenience)
                     })
            {
                ProceduralGenerationResult result = Generate(request);
                Assert.That(ProceduralCommercialGenerator.TryValidateGeneratedLayout(
                    request, result, out string error), Is.True, error);
                Assert.That(result.Openings.Where(item =>
                    item.Kind != ProceduralOpeningKind.InteriorDoor).All(item =>
                    CommercialGenerationDimensions.IsOpeningAligned(item.StartOffsetMeters) &&
                    CommercialGenerationDimensions.IsOpeningAligned(item.EndOffsetMeters)),
                    Is.True);
                Assert.That(result.Circulation.Any(item => item.CustomerRoute), Is.True);
                Assert.That(result.SoftScore, Is.GreaterThan(0f));
                AssertNoPlacementOverlap(result);
            }
        }

        [Test]
        public void TraitPlacementsUseWallFacesSocketsExteriorPadsAndRows()
        {
            ProceduralGenerationResult convenienceResult = Generate(Request(
                77,
                CommercialBuildingArchetype.StandaloneSmallCommercial,
                OrthogonalFootprintKind.Rectangle,
                48,
                50,
                convenience));
            GeneratedAssetPlacement socketPlacement = convenienceResult.Placements
                .Single(item => item.MountingMode == ProceduralMountingMode.Socket);
            Assert.That(socketPlacement.ParentPlacementId, Is.Not.Empty);
            Assert.That(socketPlacement.HostSocketId, Is.Not.Empty);
            Assert.That(
                CommercialGenerationDimensions.IsOpeningAligned(
                    socketPlacement.LocalPositionMeters.x) &&
                CommercialGenerationDimensions.IsOpeningAligned(
                    socketPlacement.LocalPositionMeters.z),
                Is.False, "Local sockets must retain exact authored transforms.");

            GeneratedAssetPlacement wallPlacement = convenienceResult.Placements
                .First(item => item.MountingMode == ProceduralMountingMode.Wall);
            GeneratedWallSegment hostWall = convenienceResult.Walls
                .Single(item => item.WallId == wallPlacement.HostWallId);
            Vector2 wallPosition = new(
                wallPlacement.LocalPositionMeters.x,
                wallPlacement.LocalPositionMeters.z);
            float wallOffset = Vector2.Dot(
                wallPosition - hostWall.StartMeters,
                hostWall.Direction);
            Assert.That(Vector2.Distance(
                    wallPosition,
                    hostWall.InteriorFacePointAt(wallOffset)),
                Is.LessThan(0.0001f),
                "Wall-mounted pivots must use the exact authored wall face.");

            ProceduralGenerationResult laundryResult = Generate(Request(
                78,
                CommercialBuildingArchetype.StripCenterInlineRetail,
                OrthogonalFootprintKind.Rectangle,
                72,
                50,
                laundromat));
            List<GeneratedAssetPlacement> bank = laundryResult.Placements
                .Where(item => item.RequestId == "wash-bank")
                .ToList();
            Assert.That(bank.Count, Is.GreaterThanOrEqualTo(4));
            Assert.That(bank.Select(item => item.YawDegrees).Distinct().Count(),
                Is.LessThanOrEqualTo(2));

            ProceduralGenerationResult fuelResult = Generate(Request(
                79,
                CommercialBuildingArchetype.StandaloneSmallCommercial,
                OrthogonalFootprintKind.Rectangle,
                36,
                36,
                fuel));
            List<GeneratedAssetPlacement> pumps = fuelResult.Placements
                .Where(item => item.RequestId == "fuel-pumps")
                .ToList();
            Assert.That(pumps.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(pumps.All(item =>
                item.MountingMode == ProceduralMountingMode.ExteriorPad &&
                !fuelResult.Footprint.Contains(item.PhysicalBoundsMeters)), Is.True);
        }

        [Test]
        public void InvalidBusinessUnitCombinationFailsWithDiagnostic()
        {
            bool success = ProceduralCommercialGenerator.TryGenerate(
                Request(
                    9,
                    CommercialBuildingArchetype.StandaloneSmallCommercial,
                    OrthogonalFootprintKind.Rectangle,
                    20,
                    20,
                    convenience),
                out ProceduralGenerationResult result);

            Assert.That(success, Is.False);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Any(item =>
                item.Severity == ProceduralDiagnosticSeverity.Error &&
                (item.Code == "business-unit-incompatible" ||
                 item.Code == "invalid-generation-input")), Is.True,
                Diagnostics(result));
        }

        [Test]
        public void RecipeWithoutPublicZone_GeneratesOnlyItsRequiredZones()
        {
            ProceduralBusinessRecipe recipe = CreateRecipe(
                "work-only-regression",
                new[]
                {
                    Zone(FunctionalZoneType.Work, 80f, 120f),
                    Zone(FunctionalZoneType.Circulation, 0f, 0f)
                },
                new[]
                {
                    AssetRequest(
                        "work-counter",
                        ProceduralAssetCategory.WorkSurface,
                        "staff-prep",
                        FunctionalZoneType.Work)
                });

            try
            {
                Assert.That(recipe.TryValidate(out string recipeError),
                    Is.True, recipeError);
                ProceduralGenerationResult result = Generate(new ProceduralBuildingRequest(
                    301,
                    CommercialBuildingArchetype.StandaloneSmallCommercial,
                    OrthogonalFootprintKind.Rectangle,
                    48,
                    40,
                    true,
                    registry,
                    recipe));

                Assert.That(result.Zones.Select(item => item.ZoneType),
                    Does.Contain(FunctionalZoneType.Work));
                Assert.That(result.Zones.Select(item => item.ZoneType),
                    Does.Contain(FunctionalZoneType.Circulation));
                Assert.That(result.Zones.Any(item =>
                    item.ZoneType == FunctionalZoneType.Public), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(recipe);
            }
        }

        [Test]
        public void AssetRequest_DoesNotFallBackToPublicWhenPreferredZoneIsMissing()
        {
            ProceduralBusinessRecipe recipe = CreateRecipe(
                "missing-work-zone-regression",
                new[]
                {
                    Zone(FunctionalZoneType.Public, 400f, 600f),
                    Zone(FunctionalZoneType.Circulation, 0f, 0f)
                },
                new[]
                {
                    AssetRequest(
                        "work-counter",
                        ProceduralAssetCategory.WorkSurface,
                        "staff-prep",
                        FunctionalZoneType.Work)
                });

            try
            {
                Assert.That(recipe.TryValidate(out string recipeError),
                    Is.True, recipeError);
                bool success = ProceduralCommercialGenerator.TryGenerate(
                    new ProceduralBuildingRequest(
                        302,
                        CommercialBuildingArchetype.StandaloneSmallCommercial,
                        OrthogonalFootprintKind.Rectangle,
                        48,
                        40,
                        true,
                        registry,
                        recipe),
                    out ProceduralGenerationResult result);

                Assert.That(success, Is.False, Diagnostics(result));
                Assert.That(result.Diagnostics.Any(item =>
                    item.Code == "required-asset-unplaceable"), Is.True,
                    Diagnostics(result));
                Assert.That(result.Placements.Any(item =>
                    item.RequestId == "work-counter"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(recipe);
            }
        }

        [Test]
        public void NestedSocket_UsesAssetRootTransformAndAllowsSingleOccupant()
        {
            GameObject sourcePrefab = registry.AssetPrefabs.Single(item =>
                item.GetComponent<ProceduralAssetComponent>().StableAssetId ==
                "proc-work-surface-prep-counter-6ft");
            GameObject nestedSocketPrefab = UnityEngine.Object.Instantiate(sourcePrefab);
            nestedSocketPrefab.name = "PROC_WorkSurface_NestedSocket_Test";
            ProceduralAssetComponent nestedAsset =
                nestedSocketPrefab.GetComponent<ProceduralAssetComponent>();
            ProceduralSocketComponent socket = nestedSocketPrefab
                .GetComponentInChildren<ProceduralSocketComponent>(true);
            float socketHeight = socket.transform.localPosition.y;

            GameObject hierarchyOne = new("Socket Hierarchy One");
            hierarchyOne.transform.SetParent(nestedSocketPrefab.transform, false);
            hierarchyOne.transform.localPosition = new Vector3(0.20f, 0f, 0.10f);
            hierarchyOne.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            GameObject hierarchyTwo = new("Socket Hierarchy Two");
            hierarchyTwo.transform.SetParent(hierarchyOne.transform, false);
            hierarchyTwo.transform.localPosition = new Vector3(0.05f, 0f, 0.04f);
            socket.transform.SetParent(hierarchyTwo.transform, false);
            socket.transform.localPosition = new Vector3(0.06f, socketHeight, 0.02f);
            socket.transform.localRotation = Quaternion.identity;

            ProceduralAssetRegistry nestedRegistry =
                ScriptableObject.CreateInstance<ProceduralAssetRegistry>();
            nestedRegistry.Configure(registry.AssetPrefabs.Select(item =>
                item == sourcePrefab ? nestedSocketPrefab : item).ToArray());
            ProceduralBusinessRecipe recipe = CreateRecipe(
                "nested-socket-regression",
                new[]
                {
                    Zone(FunctionalZoneType.Work, 100f, 160f),
                    Zone(FunctionalZoneType.Circulation, 0f, 0f)
                },
                new[]
                {
                    AssetRequest(
                        "socket-parent",
                        ProceduralAssetCategory.WorkSurface,
                        "staff-prep",
                        FunctionalZoneType.Work,
                        priority: 200),
                    AssetRequest(
                        "socket-child",
                        ProceduralAssetCategory.ProcessStation,
                        "coffee-prep",
                        FunctionalZoneType.Work,
                        minimum: 2,
                        preferred: 2,
                        maximum: 2,
                        priority: 100)
                });

            try
            {
                Assert.That(nestedRegistry.TryValidate(true, out string registryError),
                    Is.True, registryError);
                Assert.That(recipe.TryValidate(out string recipeError),
                    Is.True, recipeError);
                bool success = ProceduralCommercialGenerator.TryGenerate(
                    new ProceduralBuildingRequest(
                        303,
                        CommercialBuildingArchetype.StandaloneSmallCommercial,
                        OrthogonalFootprintKind.Rectangle,
                        48,
                        40,
                        true,
                        nestedRegistry,
                        recipe),
                    out ProceduralGenerationResult result);

                Assert.That(success, Is.False, Diagnostics(result));
                Assert.That(result.Diagnostics.Any(item =>
                    item.Code == "required-asset-unplaceable"), Is.True,
                    Diagnostics(result));

                GeneratedAssetPlacement parent = result.Placements.Single(item =>
                    item.RequestId == "socket-parent");
                GeneratedAssetPlacement child = result.Placements.Single(item =>
                    item.RequestId == "socket-child");
                Assert.That(child.ParentPlacementId, Is.EqualTo(parent.PlacementId));
                Assert.That(child.HostSocketId, Is.EqualTo(socket.SocketId));

                Vector3 socketPositionRelativeToRoot = nestedAsset.transform
                    .InverseTransformPoint(socket.transform.position);
                Quaternion socketRotationRelativeToRoot =
                    Quaternion.Inverse(nestedAsset.transform.rotation) *
                    socket.transform.rotation;
                Quaternion parentRotation = Quaternion.Euler(
                    0f,
                    parent.YawDegrees,
                    0f);
                Vector3 expectedPosition = parent.LocalPositionMeters +
                                           parentRotation * socketPositionRelativeToRoot;
                float expectedYaw = (parentRotation * socketRotationRelativeToRoot)
                    .eulerAngles.y;

                Assert.That(Vector3.Distance(
                        child.LocalPositionMeters,
                        expectedPosition),
                    Is.LessThan(0.0001f));
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(
                        child.YawDegrees,
                        expectedYaw)),
                    Is.LessThan(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(recipe);
                UnityEngine.Object.DestroyImmediate(nestedRegistry);
                UnityEngine.Object.DestroyImmediate(nestedSocketPrefab);
            }
        }

        private ProceduralBuildingRequest Request(
            int seed,
            CommercialBuildingArchetype archetype,
            OrthogonalFootprintKind footprint,
            int widthFeet,
            int depthFeet,
            ProceduralBusinessRecipe recipe)
        {
            return new ProceduralBuildingRequest(
                seed,
                archetype,
                footprint,
                widthFeet,
                depthFeet,
                true,
                registry,
                recipe);
        }

        private static ProceduralGenerationResult Generate(
            ProceduralBuildingRequest request)
        {
            bool success = ProceduralCommercialGenerator.TryGenerate(request, out var result);
            Assert.That(success, Is.True, Diagnostics(result));
            Assert.That(result.Success, Is.True, Diagnostics(result));
            return result;
        }

        private static string PlacementKey(GeneratedAssetPlacement placement)
        {
            Vector3 position = placement.LocalPositionMeters;
            return $"{placement.PlacementId}|{placement.AssetId}|" +
                   $"{position.x:R}|{position.y:R}|{position.z:R}|{placement.YawDegrees:R}|" +
                   $"{placement.ParentPlacementId}|{placement.HostSocketId}";
        }

        private static string Diagnostics(ProceduralGenerationResult result)
        {
            return result == null
                ? "No result returned."
                : string.Join("\n", result.Diagnostics.Select(item =>
                    $"[{item.Severity}] {item.Code}: {item.Message}"));
        }

        private static ProceduralBusinessRecipe CreateRecipe(
            string recipeId,
            ProceduralZoneRequest[] zones,
            ProceduralAssetRequest[] requests)
        {
            ProceduralBusinessRecipe recipe =
                ScriptableObject.CreateInstance<ProceduralBusinessRecipe>();
            recipe.Configure(recipeId, 20f, 20f, 400f, false, zones, requests);
            return recipe;
        }

        private static ProceduralZoneRequest Zone(
            FunctionalZoneType zoneType,
            float minimumArea,
            float preferredArea)
        {
            return new ProceduralZoneRequest(
                zoneType,
                true,
                false,
                minimumArea,
                preferredArea);
        }

        private static ProceduralAssetRequest AssetRequest(
            string requestId,
            ProceduralAssetCategory category,
            string capability,
            FunctionalZoneType hostZone,
            int minimum = 1,
            int preferred = 1,
            int maximum = 1,
            int priority = 100)
        {
            return new ProceduralAssetRequest(
                requestId,
                category,
                new[] { capability },
                minimum,
                preferred,
                maximum,
                true,
                hostZone,
                priority);
        }

        private static void AssertNoPlacementOverlap(ProceduralGenerationResult result)
        {
            for (int index = 0; index < result.Placements.Count; index++)
            {
                GeneratedAssetPlacement first = result.Placements[index];
                if (first.MountingMode != ProceduralMountingMode.ExteriorPad)
                {
                    Assert.That(result.Footprint.Contains(first.PhysicalBoundsMeters),
                        Is.True, first.PlacementId);
                }

                for (int otherIndex = index + 1;
                     otherIndex < result.Placements.Count;
                     otherIndex++)
                {
                    GeneratedAssetPlacement second = result.Placements[otherIndex];
                    if (first.ParentPlacementId == second.PlacementId ||
                        second.ParentPlacementId == first.PlacementId)
                    {
                        continue;
                    }

                    bool firstElevated =
                        first.MountingMode == ProceduralMountingMode.Ceiling ||
                        first.MountingMode == ProceduralMountingMode.Socket;
                    bool secondElevated =
                        second.MountingMode == ProceduralMountingMode.Ceiling ||
                        second.MountingMode == ProceduralMountingMode.Socket;
                    if (firstElevated || secondElevated)
                    {
                        continue;
                    }

                    Assert.That(first.PhysicalBoundsMeters.Overlaps(
                            second.PhysicalBoundsMeters),
                        Is.False, $"{first.PlacementId} vs {second.PlacementId}");
                }
            }
        }
    }
}
