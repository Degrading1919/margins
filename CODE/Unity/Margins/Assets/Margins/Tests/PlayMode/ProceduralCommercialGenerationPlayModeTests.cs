using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
    }
}
