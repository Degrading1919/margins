using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Margins.Tests
{
    [Category("PortfolioProgression")]
    public sealed class PortfolioProgressionPlayModeTests
    {
        private string temporaryDirectory;
        private string savePath;
        private PortfolioProgressionController portfolio;
        private FirstStoreDiskPersistenceController disk;
        private StoreOperatingController store;
        private FirstPersonController player;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            temporaryDirectory = Path.Combine(
                Application.temporaryCachePath,
                $"portfolio-progression-{Guid.NewGuid():N}");
            Directory.CreateDirectory(temporaryDirectory);
            savePath = Path.Combine(temporaryDirectory, "portfolio.json");

            yield return SceneManager.LoadSceneAsync(
                "FirstStoreValidation",
                LoadSceneMode.Single);
            yield return null;

            portfolio = Object.FindAnyObjectByType<PortfolioProgressionController>();
            disk = Object.FindAnyObjectByType<FirstStoreDiskPersistenceController>();
            store = Object.FindAnyObjectByType<StoreOperatingController>();
            player = Object.FindAnyObjectByType<FirstPersonController>();
            Assert.That(portfolio, Is.Not.Null);
            Assert.That(disk, Is.Not.Null);
            Assert.That(store, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(portfolio.TryValidateConfiguration(out string error), Is.True, error);
            Assert.That(disk.TryValidateConfiguration(out error), Is.True, error);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (!string.IsNullOrEmpty(temporaryDirectory) &&
                Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, true);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneStartsWithLockedManagementProgression()
        {
            PortfolioProgressionSnapshot snapshot =
                portfolio.Progression.CreateSnapshot();
            Assert.That(snapshot.firstShiftCompleted, Is.False);
            Assert.That(snapshot.cashCents, Is.EqualTo(PortfolioProgressionRules.StartingCashCents));
            Assert.That(snapshot.locations.Count, Is.EqualTo(1));
            Assert.That(snapshot.employees, Is.Empty);
            Assert.That(portfolio.OwnsManagementDesk, Is.False);

            player.SetGameplayMode(false);
            Assert.That(portfolio.OwnsManagementDesk, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PhysicalFirstShiftUnlocksDeskAndPostsMoneyOnce()
        {
            CompletePhysicalFirstShift();
            long expectedCash =
                PortfolioProgressionRules.StartingCashCents +
                store.ResultTotals.contributionAfterCostOfGoodsCents;

            yield return null;

            Assert.That(portfolio.Progression.FirstShiftCompleted, Is.True);
            Assert.That(portfolio.Progression.CashCents, Is.EqualTo(expectedCash));
            Assert.That(
                portfolio.Progression.CreateSnapshot().processedDetailedSessionId,
                Is.EqualTo(store.StableSessionId));

            Assert.That(
                portfolio.TrySynchronizeDetailedShift(out string error),
                Is.True,
                error);
            Assert.That(portfolio.Progression.CashCents, Is.EqualTo(expectedCash));

            Assert.That(player.IsGameplayMode, Is.True);
            Assert.That(store.TryAcknowledgeResult(out error), Is.True, error);
            yield return null;

            Assert.That(player.IsGameplayMode, Is.False);
            Assert.That(portfolio.OwnsManagementDesk, Is.True);
        }

        [UnityTest]
        public IEnumerator DiskRoundTripRestoresEmployeesPoliciesDelegatedReportAndCash()
        {
            CompletePhysicalFirstShift();
            Assert.That(portfolio.TrySynchronizeDetailedShift(out string error), Is.True, error);
            HireFirstTeam();
            Assert.That(portfolio.TryAdvanceDelegatedDay(out error), Is.True, error);
            Assert.That(
                portfolio.Progression.TrySetPricingPolicy(
                    PortfolioProgressionRules.FirstLocationId,
                    PortfolioPricingPolicy.Premium,
                    out error),
                Is.True,
                error);
            PortfolioProgressionSnapshot expected =
                portfolio.Progression.CreateSnapshot();

            Assert.That(disk.TrySaveToPath(savePath), Is.True, disk.LastDiagnostic);
            Assert.That(
                FirstStoreDiskSaveCodec.TryFromJson(
                    File.ReadAllText(savePath),
                    out FirstStoreDiskSaveData encoded,
                    out error),
                Is.True,
                error);
            Assert.That(encoded.version, Is.EqualTo(FirstStoreDiskPersistenceController.CurrentFileVersion));
            Assert.That(encoded.portfolio, Is.Not.Null);
            Assert.That(encoded.portfolio.locations[0].hasLastReport, Is.True);

            Assert.That(
                portfolio.Progression.TrySetPricingPolicy(
                    PortfolioProgressionRules.FirstLocationId,
                    PortfolioPricingPolicy.Value,
                    out error),
                Is.True,
                error);
            Assert.That(
                portfolio.TryLeaseLocation("location-riverbend-market", out error),
                Is.True,
                error);
            Assert.That(portfolio.Progression.Locations.Count, Is.EqualTo(2));

            Assert.That(disk.TryLoadFromPath(savePath), Is.True, disk.LastDiagnostic);
            PortfolioProgressionSnapshot restored =
                portfolio.Progression.CreateSnapshot();
            Assert.That(restored.cashCents, Is.EqualTo(expected.cashCents));
            Assert.That(restored.currentDay, Is.EqualTo(expected.currentDay));
            Assert.That(restored.employees.Count, Is.EqualTo(3));
            Assert.That(restored.locations.Count, Is.EqualTo(1));
            Assert.That(
                restored.locations[0].pricingPolicy,
                Is.EqualTo(PortfolioPricingPolicy.Premium));
            Assert.That(
                restored.locations[0].lastReport.grossSalesCents,
                Is.EqualTo(expected.locations[0].lastReport.grossSalesCents));
            Assert.That(
                restored.locations[0].lastReport.primaryCause,
                Is.EqualTo(expected.locations[0].lastReport.primaryCause));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PhysicalShiftBuildsAndRestoresTwoLocationPortfolio()
        {
            CompletePhysicalFirstShift();
            Assert.That(
                portfolio.TrySynchronizeDetailedShift(out string error),
                Is.True,
                error);
            Assert.That(store.TryAcknowledgeResult(out error), Is.True, error);
            yield return null;
            Assert.That(portfolio.OwnsManagementDesk, Is.True);

            HireFirstTeam();
            Assert.That(portfolio.TryAdvanceDelegatedDay(out error), Is.True, error);
            Assert.That(
                portfolio.TryLeaseLocation("location-riverbend-market", out error),
                Is.True,
                error);
            HireSecondTeam("location-riverbend-market");
            Assert.That(
                portfolio.Progression.TrySetPricingPolicy(
                    "location-riverbend-market",
                    PortfolioPricingPolicy.Value,
                    out error),
                Is.True,
                error);
            Assert.That(
                portfolio.Progression.TrySetReorderPolicy(
                    PortfolioProgressionRules.FirstLocationId,
                    PortfolioReorderPolicy.Lean,
                    out error),
                Is.True,
                error);
            Assert.That(portfolio.TryAdvanceDelegatedDay(out error), Is.True, error);

            PortfolioProgressionSnapshot expected =
                portfolio.Progression.CreateSnapshot();
            Assert.That(expected.currentDay, Is.EqualTo(3));
            Assert.That(expected.locations.Count, Is.EqualTo(2));
            Assert.That(expected.employees.Count, Is.EqualTo(6));
            Assert.That(
                expected.locations.All(location =>
                    location.hasLastReport && location.lastReport.day == 3),
                Is.True);
            Assert.That(
                expected.cashCents,
                Is.GreaterThanOrEqualTo(
                    PortfolioProgressionRules.MinimumCashReserveCents));

            Assert.That(disk.TrySaveToPath(savePath), Is.True, disk.LastDiagnostic);
            Assert.That(
                portfolio.Progression.TrySetPricingPolicy(
                    "location-riverbend-market",
                    PortfolioPricingPolicy.Premium,
                    out error),
                Is.True,
                error);
            Assert.That(disk.TryLoadFromPath(savePath), Is.True, disk.LastDiagnostic);

            PortfolioProgressionSnapshot restored =
                portfolio.Progression.CreateSnapshot();
            Assert.That(restored.currentDay, Is.EqualTo(expected.currentDay));
            Assert.That(restored.cashCents, Is.EqualTo(expected.cashCents));
            Assert.That(restored.locations.Count, Is.EqualTo(2));
            Assert.That(restored.employees.Count, Is.EqualTo(6));
            Assert.That(
                restored.locations.Single(location =>
                    location.locationId == "location-riverbend-market")
                    .pricingPolicy,
                Is.EqualTo(PortfolioPricingPolicy.Value));
            Assert.That(
                restored.locations.Sum(location => location.lastReport.cashChangeCents),
                Is.EqualTo(
                    expected.locations.Sum(location =>
                        location.lastReport.cashChangeCents)));
        }

        [UnityTest]
        public IEnumerator LegacyFileMigratesCompanyWithoutInventingProgress()
        {
            Assert.That(disk.TrySaveToPath(savePath), Is.True, disk.LastDiagnostic);
            Assert.That(
                FirstStoreDiskSaveCodec.TryFromJson(
                    File.ReadAllText(savePath),
                    out FirstStoreDiskSaveData legacy,
                    out string error),
                Is.True,
                error);
            legacy.version = FirstStoreDiskPersistenceController.LegacyFileVersion;
            legacy.portfolio = null;
            File.WriteAllText(savePath, FirstStoreDiskSaveCodec.ToJson(legacy));

            CompletePhysicalFirstShift();
            Assert.That(portfolio.TrySynchronizeDetailedShift(out error), Is.True, error);
            HireFirstTeam();
            Assert.That(portfolio.Progression.Employees.Count, Is.EqualTo(3));

            Assert.That(disk.TryLoadFromPath(savePath), Is.True, disk.LastDiagnostic);
            StringAssert.Contains("migrated legacy", disk.LastDiagnostic);
            PortfolioProgressionSnapshot migrated =
                portfolio.Progression.CreateSnapshot();
            Assert.That(migrated.firstShiftCompleted, Is.False);
            Assert.That(migrated.employees, Is.Empty);
            Assert.That(migrated.locations.Count, Is.EqualTo(1));
            Assert.That(migrated.cashCents, Is.EqualTo(PortfolioProgressionRules.StartingCashCents));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TamperedPortfolioRejectsBeforeAnyLiveMutation()
        {
            CompletePhysicalFirstShift();
            Assert.That(portfolio.TrySynchronizeDetailedShift(out string error), Is.True, error);
            HireFirstTeam();
            Assert.That(portfolio.TryAdvanceDelegatedDay(out error), Is.True, error);
            Assert.That(disk.TrySaveToPath(savePath), Is.True, disk.LastDiagnostic);

            PortfolioProgressionSnapshot companyBefore =
                portfolio.Progression.CreateSnapshot();
            FirstStorePersistenceMapperComponent mapper =
                Object.FindAnyObjectByType<FirstStorePersistenceMapperComponent>();
            Assert.That(mapper.TryCapture(out FirstStoreSnapshot storeBefore, out error), Is.True, error);
            FirstStorePlayerTransformSnapshot poseBefore = player.CaptureTransformSnapshot();

            Assert.That(
                FirstStoreDiskSaveCodec.TryFromJson(
                    File.ReadAllText(savePath),
                    out FirstStoreDiskSaveData tampered,
                    out error),
                Is.True,
                error);
            tampered.portfolio.locations[0].lastReport.cashChangeCents++;
            File.WriteAllText(savePath, FirstStoreDiskSaveCodec.ToJson(tampered));

            Assert.That(disk.TryLoadFromPath(savePath), Is.False);
            Assert.That(
                disk.LastDiagnostic,
                Does.Contain("report does not reconcile"));
            PortfolioProgressionSnapshot companyAfter =
                portfolio.Progression.CreateSnapshot();
            Assert.That(companyAfter.cashCents, Is.EqualTo(companyBefore.cashCents));
            Assert.That(companyAfter.currentDay, Is.EqualTo(companyBefore.currentDay));
            Assert.That(
                companyAfter.locations[0].lastReport.cashChangeCents,
                Is.EqualTo(companyBefore.locations[0].lastReport.cashChangeCents));
            Assert.That(mapper.TryCapture(out FirstStoreSnapshot storeAfter, out error), Is.True, error);
            Assert.That(storeAfter, Is.EqualTo(storeBefore));
            AssertPoseEqual(player.CaptureTransformSnapshot(), poseBefore);
            yield return null;
        }

        private void CompletePhysicalFirstShift()
        {
            FixturePlacementController placement =
                Object.FindAnyObjectByType<FixturePlacementController>();
            PlaceableFixtureComponent fixture = Resources
                .FindObjectsOfTypeAll<PlaceableFixtureComponent>()
                .Single(item =>
                    item.StableFixtureInstanceId == "fixture-checkout-essential-01");
            if (!placement.IsPlaced(fixture.StableFixtureInstanceId))
            {
                FixturePlacementResult placementResult = placement.TryPlace(
                    fixture,
                    new GridPosition(1, 1),
                    0);
                Assert.That(
                    placementResult.IsSuccess,
                    Is.True,
                    placementResult.Failure.ToString());
            }

            Assert.That(store.TryBeginPreparation(out string error), Is.True, error);
            DeliveryBoxComponent delivery =
                Object.FindAnyObjectByType<DeliveryBoxComponent>();
            StockingController stocking =
                Object.FindAnyObjectByType<StockingController>();
            CheckoutStationComponent checkout =
                Object.FindAnyObjectByType<CheckoutStationComponent>();
            ProductDefinition cola = Resources
                .FindObjectsOfTypeAll<ProductDefinition>()
                .Single(product =>
                    product.StableProductId == "prod-cola-can-355ml");
            Assert.That(delivery.TryOpen(out _, out error), Is.True, error);
            Assert.That(
                delivery.TryRemoveOneUnit(
                    cola,
                    out ProductItem loose,
                    out _,
                    out _,
                    out error),
                Is.True,
                error);
            Assert.That(stocking.TryPickUpLooseUnit(loose, out _, out error), Is.True, error);
            Assert.That(stocking.TryStockHeldUnit(0, out error), Is.True, error);
            Assert.That(store.TryOpenStore(out error), Is.True, error);
            Assert.That(checkout.TryBeginSession("transaction-portfolio-001", out error), Is.True, error);
            Assert.That(
                checkout.TryScan(cola, 1, out CheckoutFailure scanFailure),
                Is.True,
                scanFailure.ToString());
            Assert.That(
                checkout.TryComplete(out _, out CheckoutFailure completionFailure),
                Is.True,
                completionFailure.ToString());

            CleaningTaskComponent cleaning =
                Object.FindAnyObjectByType<CleaningTaskComponent>();
            cleaning.TryApplyProgress(cleaning.RequiredProgressUnits);
            Assert.That(cleaning.IsComplete, Is.True);
            Assert.That(store.TryBeginClosing(out error), Is.True, error);
            Assert.That(store.TryFinishClosing(out error), Is.True, error);
            Assert.That(store.ResultTotals, Is.Not.Null);
        }

        private void HireFirstTeam()
        {
            Assert.That(
                portfolio.TryHireCandidate(
                    "employee-elena-ruiz",
                    PortfolioProgressionRules.FirstLocationId,
                    out string error),
                Is.True,
                error);
            Assert.That(
                portfolio.TryHireCandidate(
                    "employee-marcus-reed",
                    PortfolioProgressionRules.FirstLocationId,
                    out error),
                Is.True,
                error);
            Assert.That(
                portfolio.TryHireCandidate(
                    "employee-priya-shah",
                    PortfolioProgressionRules.FirstLocationId,
                    out error),
                Is.True,
                error);
        }

        private void HireSecondTeam(string locationId)
        {
            Assert.That(
                portfolio.TryHireCandidate(
                    "employee-jonah-brooks",
                    locationId,
                    out string error),
                Is.True,
                error);
            Assert.That(
                portfolio.TryHireCandidate(
                    "employee-nia-carter",
                    locationId,
                    out error),
                Is.True,
                error);
            Assert.That(
                portfolio.TryHireCandidate(
                    "employee-luis-ortega",
                    locationId,
                    out error),
                Is.True,
                error);
        }

        private static void AssertPoseEqual(
            FirstStorePlayerTransformSnapshot actual,
            FirstStorePlayerTransformSnapshot expected)
        {
            Assert.That(actual.worldPosition.x, Is.EqualTo(expected.worldPosition.x).Within(0.0001f));
            Assert.That(actual.worldPosition.y, Is.EqualTo(expected.worldPosition.y).Within(0.0001f));
            Assert.That(actual.worldPosition.z, Is.EqualTo(expected.worldPosition.z).Within(0.0001f));
            Assert.That(actual.bodyYawDegrees, Is.EqualTo(expected.bodyYawDegrees).Within(0.0001f));
            Assert.That(actual.cameraPitchDegrees, Is.EqualTo(expected.cameraPitchDegrees).Within(0.0001f));
        }
    }
}
