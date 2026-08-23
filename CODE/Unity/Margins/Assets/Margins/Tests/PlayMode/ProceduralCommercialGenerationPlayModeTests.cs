using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Margins.Tests
{
    public sealed class ProceduralCommercialGenerationPlayModeTests
    {
        [UnityTest]
        public IEnumerator GrayboxDemoGeneratesRuntimeShellAssetsAndDebugData()
        {
            GameObject prefab = Resources.Load<GameObject>(
                "ProceduralCommercialGrayboxDemo");
            Assert.That(prefab, Is.Not.Null);

            GameObject instance = Object.Instantiate(prefab);
            try
            {
                ProceduralCommercialBuilding building =
                    instance.GetComponent<ProceduralCommercialBuilding>();
                ProceduralGenerationDebugView debugView =
                    instance.GetComponent<ProceduralGenerationDebugView>();
                Assert.That(building, Is.Not.Null);
                Assert.That(debugView, Is.Not.Null);
                Assert.That(debugView.ShowRuntimeOverlays, Is.True);

                Assert.That(building.TryGenerate(out string error), Is.True, error);
                yield return null;

                ProceduralGenerationResult result = building.LastResult;
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Success, Is.True);
                Assert.That(building.LastSignature,
                    Is.EqualTo(result.CanonicalSignature()));
                Assert.That(ProceduralCommercialGenerator.TryValidateGeneratedLayout(
                    building.Request, result, out error), Is.True, error);

                Transform generated = instance.transform.Find(
                    "Generated Procedural Layout");
                Assert.That(generated, Is.Not.Null);
                Assert.That(generated.Find("Shell"), Is.Not.Null);
                Assert.That(generated.Find("Partitions"), Is.Not.Null);
                Assert.That(generated.Find("Opening Placeholders"), Is.Not.Null);
                Assert.That(generated.Find("Business Assets"), Is.Not.Null);
                Assert.That(generated.Find("Debug Overlays"), Is.Not.Null);

                ProceduralAssetComponent[] renderedAssets = generated
                    .GetComponentsInChildren<ProceduralAssetComponent>(true);
                Assert.That(renderedAssets.Length,
                    Is.EqualTo(result.Placements.Count));
                Assert.That(renderedAssets.All(asset =>
                    asset.PrimaryCollider != null &&
                    asset.VisualRoot != null &&
                    asset.VisualRoot.parent == asset.transform), Is.True);
                Assert.That(generated.GetComponentsInChildren<Collider>(true),
                    Is.Not.Empty);
            }
            finally
            {
                Object.Destroy(instance);
            }
        }

        [UnityTest]
        public IEnumerator RegenerationChangesSeedAndRetainsOneValidLayout()
        {
            GameObject instance = Object.Instantiate(Resources.Load<GameObject>(
                "ProceduralCommercialGrayboxDemo"));
            try
            {
                ProceduralCommercialBuilding building =
                    instance.GetComponent<ProceduralCommercialBuilding>();
                Assert.That(building.TryGenerate(out string error), Is.True, error);
                string originalSignature = building.LastSignature;
                int nextSeed = building.Request.Seed + 1;

                Assert.That(building.RegenerateWithSeed(nextSeed, out error),
                    Is.True, error);
                yield return null;

                Assert.That(building.LastResult.Seed, Is.EqualTo(nextSeed));
                Assert.That(building.LastSignature, Is.Not.EqualTo(originalSignature));
                Assert.That(ProceduralCommercialGenerator.TryValidateGeneratedLayout(
                    building.Request,
                    building.LastResult,
                    out error), Is.True, error);
                Assert.That(instance.transform.Cast<Transform>().Count(child =>
                    child.name == "Generated Procedural Layout"), Is.EqualTo(1));
            }
            finally
            {
                Object.Destroy(instance);
            }
        }

        [UnityTest]
        public IEnumerator MergedStripUnitRendersPartyWallAndRejectsAdjacentBayCirculation()
        {
            ProceduralAssetRegistry registry = Resources.Load<ProceduralAssetRegistry>(
                "ProceduralAssetRegistry");
            ProceduralBusinessRecipe recipe = Resources.Load<ProceduralBusinessRecipe>(
                "Recipes/GrayboxLaundromatRecipe");
            Assert.That(registry, Is.Not.Null);
            Assert.That(recipe, Is.Not.Null);

            GameObject instance = new("Merged Strip Unit Party Wall Regression");
            try
            {
                ProceduralCommercialBuilding building =
                    instance.AddComponent<ProceduralCommercialBuilding>();
                building.Configure(new ProceduralBuildingRequest(
                    32,
                    CommercialBuildingArchetype.StripCenterInlineRetail,
                    OrthogonalFootprintKind.Rectangle,
                    72,
                    50,
                    false,
                    registry,
                    recipe), false);

                Assert.That(building.TryGenerate(out string error), Is.True, error);
                yield return null;

                ProceduralGenerationResult result = building.LastResult;
                GeneratedCommercialUnit unit = result.Units.Single();
                HashSet<string> selectedBayIds = new(
                    unit.SourceBayIds,
                    StringComparer.Ordinal);
                List<GeneratedCommercialBay> selectedBays = result.Bays
                    .Where(item => selectedBayIds.Contains(item.BayId))
                    .OrderBy(item => item.BoundsMeters.MinX)
                    .ToList();
                List<GeneratedCommercialBay> adjacentBays = result.Bays
                    .Where(item => !selectedBayIds.Contains(item.BayId) &&
                                   (Mathf.Abs(item.BoundsMeters.MaxX -
                                              unit.BoundsMeters.MinX) < 0.0001f ||
                                    Mathf.Abs(item.BoundsMeters.MinX -
                                              unit.BoundsMeters.MaxX) < 0.0001f))
                    .ToList();

                Assert.That(selectedBays.Count, Is.GreaterThanOrEqualTo(2));
                Assert.That(selectedBays.Count, Is.LessThan(result.Bays.Count));
                Assert.That(adjacentBays, Is.Not.Empty);

                List<GeneratedWallSegment> partyWalls = result.Walls
                    .Where(item => item.WallId.StartsWith(
                        "wall-party-",
                        StringComparison.Ordinal))
                    .ToList();
                Assert.That(partyWalls.Count, Is.EqualTo(adjacentBays.Count));

                Transform shell = instance.transform.Find(
                    "Generated Procedural Layout/Shell");
                Assert.That(shell, Is.Not.Null);
                foreach (GeneratedWallSegment wall in partyWalls)
                {
                    bool leftBoundary = Mathf.Abs(
                        wall.StartMeters.x - unit.BoundsMeters.MinX) < 0.0001f;
                    bool rightBoundary = Mathf.Abs(
                        wall.StartMeters.x - unit.BoundsMeters.MaxX) < 0.0001f;
                    Assert.That(leftBoundary || rightBoundary, Is.True, wall.WallId);
                    Assert.That(wall.FrontageRole,
                        Is.EqualTo(FrontageRole.SharedInternal));
                    Assert.That(wall.StartMeters.y,
                        Is.EqualTo(unit.BoundsMeters.MinZ).Within(0.0001f));
                    Assert.That(wall.EndMeters.y,
                        Is.EqualTo(unit.BoundsMeters.MaxZ).Within(0.0001f));
                    Assert.That(adjacentBays.Any(item => leftBoundary
                            ? Mathf.Abs(item.BoundsMeters.MaxX -
                                        unit.BoundsMeters.MinX) < 0.0001f
                            : Mathf.Abs(item.BoundsMeters.MinX -
                                        unit.BoundsMeters.MaxX) < 0.0001f),
                        Is.True, wall.WallId);

                    Collider[] colliders = shell
                        .GetComponentsInChildren<Collider>(true)
                        .Where(item => item.name.StartsWith(
                            wall.WallId + " ",
                            StringComparison.Ordinal))
                        .ToArray();
                    Renderer[] renderers = shell
                        .GetComponentsInChildren<Renderer>(true)
                        .Where(item => item.name.StartsWith(
                            wall.WallId + " ",
                            StringComparison.Ordinal))
                        .ToArray();
                    Assert.That(colliders, Is.Not.Empty, wall.WallId);
                    Assert.That(renderers, Is.Not.Empty, wall.WallId);
                    Assert.That(colliders.All(item => item.enabled && !item.isTrigger),
                        Is.True, wall.WallId);
                }

                foreach (GeneratedCommercialBay mergedBay in selectedBays.Skip(1))
                {
                    Assert.That(partyWalls.Any(item => Mathf.Abs(
                            item.StartMeters.x - mergedBay.BoundsMeters.MinX) < 0.0001f),
                        Is.False,
                        "Merged source bays must not retain an internal party wall.");
                }

                Assert.That(ProceduralCommercialGenerator.TryValidateGeneratedLayout(
                    building.Request,
                    result,
                    out error), Is.True, error);

                PlanRect neighbor = adjacentBays[0].BoundsMeters;
                float inset = CommercialGenerationDimensions.Feet(1f);
                GeneratedCirculationPath adjacentBayProbe = new(
                    "circulation-adjacent-bay-probe",
                    new PlanRect(
                        neighbor.MinX + inset,
                        neighbor.MinZ + inset,
                        neighbor.Width - inset * 2f,
                        neighbor.Depth - inset * 2f),
                    CommercialGenerationDimensions.PrimaryRouteWidthMeters,
                    true);
                FieldInfo circulationField = typeof(ProceduralGenerationResult)
                    .GetField("circulation", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(circulationField, Is.Not.Null);
                List<GeneratedCirculationPath> circulation =
                    circulationField.GetValue(result) as List<GeneratedCirculationPath>;
                Assert.That(circulation, Is.Not.Null);
                circulation.Add(adjacentBayProbe);

                Assert.That(ProceduralCommercialGenerator.TryValidateGeneratedLayout(
                    building.Request,
                    result,
                    out error), Is.False);
                Assert.That(error, Does.Contain(adjacentBayProbe.PathId));
            }
            finally
            {
                Object.Destroy(instance);
            }
        }
    }
}
