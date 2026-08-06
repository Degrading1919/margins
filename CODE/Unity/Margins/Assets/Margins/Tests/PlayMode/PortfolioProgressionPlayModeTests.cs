using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
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
        public IEnumerator SceneStartsWithInitializedManagementProgression()
        {
            PortfolioProgressionSnapshot snapshot =
                portfolio.Progression.CreateSnapshot();
            Assert.That(snapshot.firstShiftCompleted, Is.False);
            Assert.That(snapshot.detailedOperationInitialized, Is.True);
            Assert.That(
                snapshot.cashCents,
                Is.EqualTo(
                    PortfolioProgressionRules.StartingCashCents - 600 - 9_000));
            Assert.That(snapshot.locations.Count, Is.EqualTo(1));
            Assert.That(snapshot.locations[0].inventoryUnits, Is.EqualTo(8));
            Assert.That(snapshot.employees, Is.Empty);
            Assert.That(portfolio.OwnsManagementDesk, Is.False);

            player.SetGameplayMode(false);
            Assert.That(portfolio.OwnsManagementDesk, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PhysicalFirstShiftPostsMoneyOnceAndDeskRemainsAvailable()
        {
            CompletePhysicalFirstShift();
            PortfolioProgressionSnapshot completed =
                portfolio.Progression.CreateSnapshot();
            long expectedCash =
                PortfolioProgressionRules.StartingCashCents +
                completed.locations[0].lastReport.cashChangeCents;

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
            Assert.That(store.State, Is.EqualTo(StoreOperatingState.Open));
            Assert.That(store.ResultTotals, Is.Null);
            player.SetGameplayMode(false);
            Assert.That(portfolio.OwnsManagementDesk, Is.True);
            yield return null;
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
        public IEnumerator HiredTeamServesLiveCustomerRestocksCleansAndRoundTripsOnce()
        {
            CompletePhysicalFirstShift();
            CheckoutStationComponent checkout =
                Object.FindAnyObjectByType<CheckoutStationComponent>();
            FirstStoreInventoryComponent inventory =
                Object.FindAnyObjectByType<FirstStoreInventoryComponent>();
            DeliveryBoxComponent delivery =
                Object.FindAnyObjectByType<DeliveryBoxComponent>();
            StockingController stocking =
                Object.FindAnyObjectByType<StockingController>();
            StoreCustomerFlowController flow =
                Object.FindAnyObjectByType<StoreCustomerFlowController>();
            CleaningTaskComponent cleaning =
                Object.FindAnyObjectByType<CleaningTaskComponent>();
            ProductDefinition cola = GetProduct("prod-cola-can-355ml");
            ProductDefinition chips = GetProduct("prod-potato-chips-small");
            StockOneProduct(delivery, stocking, cola);
            StockOneProduct(delivery, stocking, chips);

            HireFirstTeam();
            Assert.That(
                portfolio.TrySynchronizeLivePayroll(out string error),
                Is.True,
                error);

            InStoreEmployeeWorkController employeeWork =
                Object.FindAnyObjectByType<InStoreEmployeeWorkController>();
            StagedCheckoutInteractionComponent staged =
                Object.FindAnyObjectByType<StagedCheckoutInteractionComponent>();
            Assert.That(employeeWork, Is.Not.Null);
            Assert.That(
                employeeWork.TryValidateConfiguration(out error),
                Is.True,
                error);
            Assert.That(staged.enabled, Is.False);

            SetField(flow, "secondsUntilNextArrival", 1_000f);
            SetField(flow, "arrivalIntervalSeconds", 1_000f);
            SetField(flow, "nextCustomerOrdinal", 3);
            DeliveryBoxWorldInteractionTarget deliveryTarget =
                Object.FindAnyObjectByType<DeliveryBoxWorldInteractionTarget>();
            float deadline = Time.realtimeSinceStartup + 8f;
            while (!delivery.IsCarried &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(delivery.IsCarried, Is.True);
            Assert.That(
                deliveryTarget.TryCancel(out error),
                Is.True,
                error);
            employeeWork.enabled = false;
            yield return null;

            int transactionCountBefore = checkout.CompletedTransactionCount;
            int unitsSoldBefore = checkout.UnitsSold;
            int inventoryBefore = TotalInventory(inventory, checkout);
            if (!cleaning.NeedsCleaning)
            {
                Assert.That(cleaning.TryCreateMess(), Is.True);
            }
            Assert.That(cleaning.NeedsCleaning, Is.True);

            Assert.That(
                flow.TryAdmitCustomerNow(out string customerId, out error),
                Is.True,
                error);
            Assert.That(customerId, Is.EqualTo("store-customer-000003"));

            deadline = Time.realtimeSinceStartup + 12f;
            while (!flow.CanStartCheckout &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(flow.CanStartCheckout, Is.True, flow.CheckoutBlocker);
            Assert.That(flow.TryStartCheckout(out error), Is.True, error);
            Assert.That(flow.ActiveCheckoutItemCount, Is.EqualTo(2));
            string playerScannedUnit = flow.ActiveCheckoutPhysicalUnitIds[0];
            Assert.That(
                flow.TryScanCustomerItem(playerScannedUnit, out error),
                Is.True,
                error);
            Assert.That(flow.ActiveCheckoutScannedCount, Is.EqualTo(1));
            employeeWork.enabled = true;

            deadline = Time.realtimeSinceStartup + 20f;
            while ((checkout.CompletedTransactionCount < transactionCountBefore + 1 ||
                    !cleaning.IsComplete ||
                    !HasAnyShelfStock(inventory, checkout)) &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(
                checkout.CompletedTransactionCount,
                Is.EqualTo(transactionCountBefore + 1));
            Assert.That(checkout.UnitsSold, Is.EqualTo(unitsSoldBefore + 2));
            Assert.That(
                checkout.CompletedTransactions.Count(transaction =>
                    transaction.transactionId == "sale-store-customer-000003"),
                Is.EqualTo(1));
            Assert.That(
                TotalInventory(inventory, checkout),
                Is.EqualTo(inventoryBefore - 2));
            Assert.That(cleaning.IsComplete, Is.True);
            Assert.That(HasAnyShelfStock(inventory, checkout), Is.True);
            Assert.That(delivery.IsCarried, Is.False);
            Assert.That(
                GameObject.Find("Detailed Cashier Employee"),
                Is.Not.Null);
            Assert.That(
                GameObject.Find("Detailed Stock Employee"),
                Is.Not.Null);
            Assert.That(
                GameObject.Find("Detailed Manager Employee"),
                Is.Not.Null);
            Assert.That(store.LivePayrollCents, Is.EqualTo(42_000));

            deadline = Time.realtimeSinceStartup + 20f;
            while ((flow.HasCustomersInStore || employeeWork.IsHandlingInventory) &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(flow.HasCustomersInStore, Is.False);
            Assert.That(employeeWork.IsHandlingInventory, Is.False);
            employeeWork.enabled = false;
            flow.enabled = false;

            Assert.That(
                portfolio.TrySynchronizeDetailedShift(out error),
                Is.True,
                error);
            PortfolioLocationSnapshot firstLocation =
                portfolio.Progression.Locations.Single();
            Assert.That(firstLocation.lastReport.payrollCents, Is.EqualTo(42_000));
            Assert.That(firstLocation.lastReport.rentCents, Is.EqualTo(9_000));
            Assert.That(
                firstLocation.lastReport.operatingProfitCents,
                Is.EqualTo(
                    firstLocation.lastReport.grossSalesCents -
                    firstLocation.lastReport.costOfGoodsSoldCents -
                    firstLocation.lastReport.payrollCents -
                    firstLocation.lastReport.rentCents));

            Assert.That(disk.TrySaveToPath(savePath), Is.True, disk.LastDiagnostic);
            int savedTransactions = checkout.CompletedTransactionCount;
            int savedInventory = TotalInventory(inventory, checkout);
            long savedSales = checkout.GrossSalesCents;
            Assert.That(disk.TryLoadFromPath(savePath), Is.True, disk.LastDiagnostic);
            yield return null;
            Assert.That(checkout.CompletedTransactionCount, Is.EqualTo(savedTransactions));
            Assert.That(TotalInventory(inventory, checkout), Is.EqualTo(savedInventory));
            Assert.That(checkout.GrossSalesCents, Is.EqualTo(savedSales));
            Assert.That(portfolio.Progression.Employees.Count, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator StaffFinishesQueuedSaleAndStandardsWorkDuringClosing()
        {
            CompletePhysicalFirstShift();
            CheckoutStationComponent checkout =
                Object.FindAnyObjectByType<CheckoutStationComponent>();
            FirstStoreInventoryComponent inventory =
                Object.FindAnyObjectByType<FirstStoreInventoryComponent>();
            DeliveryBoxComponent delivery =
                Object.FindAnyObjectByType<DeliveryBoxComponent>();
            StockingController stocking =
                Object.FindAnyObjectByType<StockingController>();
            StoreCustomerFlowController flow =
                Object.FindAnyObjectByType<StoreCustomerFlowController>();
            CleaningTaskComponent cleaning =
                Object.FindAnyObjectByType<CleaningTaskComponent>();
            InStoreEmployeeWorkController employeeWork =
                Object.FindAnyObjectByType<InStoreEmployeeWorkController>();
            StockOneProduct(
                delivery,
                stocking,
                GetProduct("prod-cola-can-355ml"));
            HireFirstTeam();

            SetField(flow, "secondsUntilNextArrival", 1_000f);
            SetField(flow, "arrivalIntervalSeconds", 1_000f);
            int transactionCountBefore = checkout.CompletedTransactionCount;
            int inventoryBefore = TotalInventory(inventory, checkout);
            employeeWork.enabled = false;
            Assert.That(cleaning.TryCreateMess(), Is.True);
            Assert.That(
                flow.TryAdmitCustomerNow(out _, out string error),
                Is.True,
                error);

            float deadline = Time.realtimeSinceStartup + 12f;
            while (!flow.CanStartCheckout &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(flow.CanStartCheckout, Is.True, flow.CheckoutBlocker);
            Assert.That(store.TryBeginClosing(out error), Is.True, error);
            employeeWork.enabled = true;

            deadline = Time.realtimeSinceStartup + 18f;
            while ((flow.HasCustomersInStore || !cleaning.IsComplete) &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(flow.HasCustomersInStore, Is.False);
            Assert.That(cleaning.IsComplete, Is.True);
            Assert.That(
                checkout.CompletedTransactionCount,
                Is.EqualTo(transactionCountBefore + 1));
            Assert.That(
                TotalInventory(inventory, checkout),
                Is.EqualTo(inventoryBefore - 1));
            Assert.That(store.TryFinishClosing(out error), Is.True, error);
            Assert.That(
                store.State,
                Is.EqualTo(StoreOperatingState.ClosedWithResultPending));
        }

        [UnityTest]
        public IEnumerator StaffSetsDownInFlightDeliveryAfterFinalClose()
        {
            CompletePhysicalFirstShift();
            DeliveryBoxComponent delivery =
                Object.FindAnyObjectByType<DeliveryBoxComponent>();
            StockingController stocking =
                Object.FindAnyObjectByType<StockingController>();
            StoreCustomerFlowController flow =
                Object.FindAnyObjectByType<StoreCustomerFlowController>();
            CleaningTaskComponent cleaning =
                Object.FindAnyObjectByType<CleaningTaskComponent>();
            InStoreEmployeeWorkController employeeWork =
                Object.FindAnyObjectByType<InStoreEmployeeWorkController>();
            StockOneProduct(
                delivery,
                stocking,
                GetProduct("prod-cola-can-355ml"));
            HireFirstTeam();
            SetField(flow, "secondsUntilNextArrival", 1_000f);
            SetField(flow, "arrivalIntervalSeconds", 1_000f);

            float deadline = Time.realtimeSinceStartup + 8f;
            while (!delivery.IsCarried &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(delivery.IsCarried, Is.True);
            Assert.That(employeeWork.IsHandlingInventory, Is.True);
            Assert.That(store.TryBeginClosing(out string error), Is.True, error);
            for (int step = 0; step < 8 && !cleaning.IsComplete; step++)
            {
                cleaning.TryApplyProgress(1);
            }
            Assert.That(cleaning.IsComplete, Is.True);
            Assert.That(store.TryFinishClosing(out error), Is.True, error);
            Assert.That(
                store.State,
                Is.EqualTo(StoreOperatingState.ClosedWithResultPending));

            deadline = Time.realtimeSinceStartup + 8f;
            while (employeeWork.IsHandlingInventory &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(delivery.IsCarried, Is.False);
            Assert.That(employeeWork.IsHandlingInventory, Is.False);
        }

        [UnityTest]
        public IEnumerator PhysicalShiftBuildsAndRestoresTwoLocationPortfolio()
        {
            CompletePhysicalFirstShift();
            Assert.That(
                portfolio.TrySynchronizeDetailedShift(out string error),
                Is.True,
                error);
            player.SetGameplayMode(false);
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
            yield return null;
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
            Assert.That(
                migrated.cashCents,
                Is.EqualTo(
                    PortfolioProgressionRules.StartingCashCents - 600 - 9_000));
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

            Assert.That(store.State, Is.EqualTo(StoreOperatingState.Open));
            string error;
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
            Assert.That(checkout.TryBeginSession("transaction-portfolio-001", out error), Is.True, error);
            Assert.That(
                checkout.TryScan(cola, 1, out CheckoutFailure scanFailure),
                Is.True,
                scanFailure.ToString());
            Assert.That(
                checkout.TryComplete(out _, out CheckoutFailure completionFailure),
                Is.True,
                completionFailure.ToString());

            Assert.That(store.CurrentTotals, Is.Not.Null);
            Assert.That(store.CurrentTotals.transactionCount, Is.EqualTo(1));
            Assert.That(
                portfolio.TrySynchronizeDetailedShift(out error),
                Is.True,
                error);
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

        private static ProductDefinition GetProduct(string productId)
        {
            return Resources
                .FindObjectsOfTypeAll<ProductDefinition>()
                .Single(product => product.StableProductId == productId);
        }

        private static void StockOneProduct(
            DeliveryBoxComponent delivery,
            StockingController stocking,
            ProductDefinition product)
        {
            Assert.That(
                delivery.TryOpen(out _, out string error),
                Is.True,
                error);
            Assert.That(
                delivery.TryRemoveOneUnit(
                    product,
                    out ProductItem loose,
                    out _,
                    out _,
                    out error),
                Is.True,
                error);
            Assert.That(
                stocking.TryPickUpLooseUnit(loose, out _, out error),
                Is.True,
                error);
            Assert.That(stocking.TryStockHeldUnit(0, out error), Is.True, error);
        }

        private static int TotalInventory(
            FirstStoreInventoryComponent inventory,
            CheckoutStationComponent checkout)
        {
            return checkout.ConfiguredProductIds.Sum(
                inventory.Inventory.GetTotalQuantity);
        }

        private static bool HasAnyShelfStock(
            FirstStoreInventoryComponent inventory,
            CheckoutStationComponent checkout)
        {
            foreach (string productId in checkout.ConfiguredProductIds)
            {
                if (checkout.TryGetShelfLocation(
                        productId,
                        out string shelfLocationId) &&
                    inventory.Inventory.GetQuantity(
                        shelfLocationId,
                        productId) > 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static void SetField<T>(
            object target,
            string fieldName,
            T value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
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
