using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Margins.Tests.PlayMode
{
    public sealed class PersistentPortfolioLocationPlayModeTests
    {
        [UnityTest]
        public IEnumerator OperateDelegateReturnSwitchAndReloadKeepsOnePersistentLayoutPerLocation()
        {
            ProceduralAssetRegistry registry =
                Resources.Load<ProceduralAssetRegistry>(
                    "ProceduralAssetRegistry");
            ProceduralBusinessRecipe recipe =
                Resources.Load<ProceduralBusinessRecipe>(
                    "Recipes/GrayboxConvenienceRecipe");
            Assert.That(registry, Is.Not.Null);
            Assert.That(recipe, Is.Not.Null);

            PortfolioProgression progression = CreateReadyPortfolio();
            GameObject host = new("Persistent Portfolio E2E");
            PersistentPortfolioLocationController locations =
                host.AddComponent<PersistentPortfolioLocationController>();
            locations.ConfigureForDomain(progression, registry, recipe);
            try
            {
                Assert.That(
                    locations.TryMaterializeLocation(
                        PortfolioProgressionRules.FirstLocationId,
                        out string error),
                    Is.True,
                    error);
                yield return null;
                string firstSignature =
                    locations.ActiveBuilding.LastSignature;
                string targetId = locations.ActiveBuilding.LastResult.Placements
                    .First().PlacementId;
                Vector3 overridePosition = new(1.25f, 0.1f, 1.75f);
                Assert.That(
                    locations.TryRecordTransformOverride(
                        "modification-owner-layout-001",
                        targetId,
                        overridePosition,
                        90f,
                        out error),
                    Is.True,
                    error);
                Assert.That(locations.TryLeaveActiveLocation(out error), Is.True, error);
                yield return null;

                Assert.That(progression.TryAdvanceDelegatedDay(out error), Is.True, error);
                PortfolioLocationReportSnapshot aggregate = progression
                    .CreateSnapshot().locations.Single().lastReport;
                Assert.That(aggregate.isDetailedOperation, Is.False);
                Assert.That(aggregate.serviceQuality, Is.InRange(0, 100));
                PortfolioDelegationPolicySnapshot policy =
                    PortfolioOperationsRules.CreateDefaultDelegationPolicy();
                policy.minimumServiceQuality = 80;
                policy.maintenancePolicy = PortfolioMaintenancePolicy.Preventive;
                Assert.That(
                    progression.TrySetDelegationPolicy(
                        PortfolioProgressionRules.FirstLocationId,
                        policy,
                        out error),
                    Is.True,
                    error);

                Assert.That(
                    locations.TryMaterializeLocation(
                        PortfolioProgressionRules.FirstLocationId,
                        out error),
                    Is.True,
                    error);
                yield return null;
                Assert.That(locations.ActiveBuilding.LastSignature,
                    Is.EqualTo(firstSignature));
                Transform restoredTarget = host
                    .GetComponentsInChildren<Transform>(true)
                    .Single(value => value.name == targetId);
                Assert.That(
                    Vector3.Distance(
                        restoredTarget.localPosition,
                        overridePosition),
                    Is.LessThan(0.0001f));
                Assert.That(locations.TryLeaveActiveLocation(out error), Is.True, error);
                yield return null;

                Assert.That(
                    progression.TryLeaseLocation(
                        "location-riverbend-market",
                        out error),
                    Is.True,
                    error);
                HireSecondTeam(progression);
                Assert.That(
                    locations.TryMaterializeLocation(
                        "location-riverbend-market",
                        out error),
                    Is.True,
                    error);
                yield return null;
                string secondSignature =
                    locations.ActiveBuilding.LastSignature;
                Assert.That(secondSignature, Is.Not.EqualTo(firstSignature));
                Assert.That(locations.ActiveBuilding.LastResult.Units,
                    Has.Count.EqualTo(1));
                Assert.That(locations.TryLeaveActiveLocation(out error), Is.True, error);
                yield return null;

                string json = JsonUtility.ToJson(progression.CreateSnapshot());
                PortfolioProgressionSnapshot serialized =
                    JsonUtility.FromJson<PortfolioProgressionSnapshot>(json);
                Assert.That(
                    PortfolioProgression.TryRestore(
                        serialized,
                        out PortfolioProgression reloaded,
                        out error),
                    Is.True,
                    error);
                PortfolioProgressionSnapshot afterReload =
                    reloaded.CreateSnapshot();
                Assert.That(afterReload.locations, Has.Count.EqualTo(2));
                Assert.That(afterReload.company.properties, Has.Count.EqualTo(2));
                Assert.That(
                    afterReload.company.properties.SelectMany(property =>
                            property.commercialUnits)
                        .Single(unit => unit.occupyingLocationId ==
                                        PortfolioProgressionRules.FirstLocationId)
                        .generatedLayout.canonicalSignature,
                    Is.EqualTo(firstSignature));
                Assert.That(
                    afterReload.company.properties.SelectMany(property =>
                            property.commercialUnits)
                        .Single(unit => unit.occupyingLocationId ==
                                        "location-riverbend-market")
                        .generatedLayout.canonicalSignature,
                    Is.EqualTo(secondSignature));
                Assert.That(
                    afterReload.company.properties.SelectMany(property =>
                            property.commercialUnits)
                        .Single(unit => unit.occupyingLocationId ==
                                        PortfolioProgressionRules.FirstLocationId)
                        .generatedLayout.modifications,
                    Has.Count.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.Destroy(host);
            }
        }

        private static PortfolioProgression CreateReadyPortfolio()
        {
            PortfolioProgression progression =
                PortfolioProgression.CreateInitial();
            Assert.That(
                progression.TryPostDetailedShift(
                    "session-persistent-location-e2e",
                    new StoreSessionTotals(348, 150, 9_000, -8_802, 2, 1),
                    40,
                    out _,
                    out string error),
                Is.True,
                error);
            foreach (string employeeId in new[]
                     {
                         "employee-elena-ruiz",
                         "employee-marcus-reed",
                         "employee-priya-shah"
                     })
            {
                Assert.That(
                    progression.TryHireCandidate(
                        employeeId,
                        PortfolioProgressionRules.FirstLocationId,
                        out error),
                    Is.True,
                    error);
            }
            return progression;
        }

        private static void HireSecondTeam(PortfolioProgression progression)
        {
            foreach (string employeeId in new[]
                     {
                         "employee-jonah-brooks",
                         "employee-nia-carter",
                         "employee-luis-ortega"
                     })
            {
                Assert.That(
                    progression.TryHireCandidate(
                        employeeId,
                        "location-riverbend-market",
                        out string error),
                    Is.True,
                    error);
            }
        }
    }
}
