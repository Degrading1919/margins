using System;
using System.Collections;
using System.Collections.Generic;
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
    [Category("FirstStoreDiskPersistence")]
    public sealed class FirstStoreDiskPersistencePlayModeTests
    {
        private string temporaryDirectory;
        private string savePath;
        private FirstStoreDiskPersistenceController diskPersistence;
        private FirstStorePersistenceMapperComponent mapper;
        private FirstPersonController player;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            temporaryDirectory = Path.Combine(
                Application.temporaryCachePath,
                $"first-store-disk-persistence-{Guid.NewGuid():N}");
            Directory.CreateDirectory(temporaryDirectory);
            savePath = Path.Combine(temporaryDirectory, "first-store.json");

            yield return SceneManager.LoadSceneAsync(
                "FirstStoreValidation",
                LoadSceneMode.Single);
            yield return null;

            diskPersistence = Object.FindAnyObjectByType<FirstStoreDiskPersistenceController>();
            mapper = Object.FindAnyObjectByType<FirstStorePersistenceMapperComponent>();
            player = Object.FindAnyObjectByType<FirstPersonController>();
            Assert.That(diskPersistence, Is.Not.Null);
            Assert.That(mapper, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(diskPersistence.TryValidateConfiguration(out string error), Is.True, error);
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
        public IEnumerator SaveLoadRoundTripRestoresPlayerPoseAndFirstStoreSnapshot()
        {
            Assert.That(mapper.TryCapture(out FirstStoreSnapshot expectedState, out string error), Is.True, error);
            FirstStorePlayerTransformSnapshot expectedPose = player.CaptureTransformSnapshot();
            Assert.That(diskPersistence.TrySaveToPath(savePath), Is.True, diskPersistence.LastDiagnostic);

            MutateFixtureLayout();
            Assert.That(
                player.TryApplyTransformSnapshot(
                    new FirstStorePlayerTransformSnapshot(
                        expectedPose.worldPosition + new Vector3(2f, 0f, 1f),
                        137f,
                        23f),
                    out error),
                Is.True,
                error);

            Assert.That(diskPersistence.TryLoadFromPath(savePath), Is.True, diskPersistence.LastDiagnostic);
            Assert.That(mapper.TryCapture(out FirstStoreSnapshot restoredState, out error), Is.True, error);
            Assert.That(restoredState, Is.EqualTo(expectedState));
            AssertPoseEqual(player.CaptureTransformSnapshot(), expectedPose);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RepeatedLoadRestoresIdenticalSnapshotAndDoesNotReplayLedger()
        {
            CompleteOneColaSale();
            Assert.That(mapper.TryCapture(out FirstStoreSnapshot savedState, out string error), Is.True, error);
            Assert.That(diskPersistence.TrySaveToPath(savePath), Is.True, diskPersistence.LastDiagnostic);
            CloseStoreForLayoutMutation();
            MutateFixtureLayout();

            Assert.That(diskPersistence.TryLoadFromPath(savePath), Is.True, diskPersistence.LastDiagnostic);
            Assert.That(mapper.TryCapture(out FirstStoreSnapshot firstLoad, out error), Is.True, error);
            CheckoutStationComponent checkout = Object.FindAnyObjectByType<CheckoutStationComponent>();
            int transactionCount = checkout.CompletedTransactionCount;
            int unitsSold = checkout.UnitsSold;
            long grossSales = checkout.GrossSalesCents;
            Assert.That(transactionCount, Is.EqualTo(1));
            Assert.That(unitsSold, Is.EqualTo(1));
            Assert.That(grossSales, Is.GreaterThan(0));

            Assert.That(diskPersistence.TryLoadFromPath(savePath), Is.True, diskPersistence.LastDiagnostic);
            Assert.That(mapper.TryCapture(out FirstStoreSnapshot secondLoad, out error), Is.True, error);
            Assert.That(firstLoad, Is.EqualTo(savedState));
            Assert.That(secondLoad, Is.EqualTo(firstLoad));
            Assert.That(checkout.CompletedTransactionCount, Is.EqualTo(transactionCount));
            Assert.That(checkout.UnitsSold, Is.EqualTo(unitsSold));
            Assert.That(checkout.GrossSalesCents, Is.EqualTo(grossSales));
            yield return null;
        }

        [UnityTest]
        public IEnumerator LaterConfiguredCostChangeDoesNotAlterRestoredHistoricalResult()
        {
            CompleteOneColaSale();
            StockOneColaUnit();
            StoreOperatingController store =
                Object.FindAnyObjectByType<StoreOperatingController>();
            Assert.That(store.State, Is.EqualTo(StoreOperatingState.Open));
            long historicalCost = store.CurrentTotals.costOfGoodsSoldCents;
            long historicalContribution =
                store.CurrentTotals.contributionAfterCostOfGoodsCents;
            Assert.That(historicalCost, Is.GreaterThan(0));
            Assert.That(diskPersistence.TrySaveToPath(savePath), Is.True, diskPersistence.LastDiagnostic);

            CheckoutStationComponent checkout =
                Object.FindAnyObjectByType<CheckoutStationComponent>();
            SetConfiguredUnitCost(
                checkout,
                "prod-cola-can-355ml",
                checked((int)historicalCost + 900));

            Assert.That(diskPersistence.TryLoadFromPath(savePath), Is.True, diskPersistence.LastDiagnostic);
            Assert.That(store.CurrentTotals.costOfGoodsSoldCents, Is.EqualTo(historicalCost));
            Assert.That(
                store.CurrentTotals.contributionAfterCostOfGoodsCents,
                Is.EqualTo(historicalContribution));
            Assert.That(
                checkout.CompletedTransactions[0].lines[0].unitCostCents,
                Is.EqualTo(historicalCost));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TamperedPhysicalReconciliationRejectsWithoutLiveMutation()
        {
            Assert.That(diskPersistence.TrySaveToPath(savePath), Is.True, diskPersistence.LastDiagnostic);
            Assert.That(mapper.TryCapture(out FirstStoreSnapshot before, out string error), Is.True, error);
            FirstStorePlayerTransformSnapshot poseBefore = player.CaptureTransformSnapshot();

            Assert.That(
                FirstStoreDiskSaveCodec.TryFromJson(
                    File.ReadAllText(savePath),
                    out FirstStoreDiskSaveData tampered,
                    out error),
                Is.True,
                error);
            tampered.firstStore.physicalProductUnits = new List<PhysicalProductUnitSnapshot>
            {
                new(
                    "physical-unit-999999",
                    "prod-cola-can-355ml",
                    "loc-loose",
                    null,
                    null,
                    0)
            };
            tampered.firstStore.nextPhysicalUnitOrdinal = 1_000_000;
            File.WriteAllText(savePath, FirstStoreDiskSaveCodec.ToJson(tampered));

            Assert.That(diskPersistence.TryLoadFromPath(savePath), Is.False);
            Assert.That(mapper.TryCapture(out FirstStoreSnapshot after, out error), Is.True, error);
            Assert.That(after, Is.EqualTo(before));
            AssertPoseEqual(player.CaptureTransformSnapshot(), poseBefore);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnsupportedVersionAndMalformedJsonRejectWithoutMutation()
        {
            Assert.That(diskPersistence.TrySaveToPath(savePath), Is.True, diskPersistence.LastDiagnostic);
            Assert.That(mapper.TryCapture(out FirstStoreSnapshot before, out string error), Is.True, error);
            FirstStorePlayerTransformSnapshot poseBefore = player.CaptureTransformSnapshot();

            Assert.That(
                FirstStoreDiskSaveCodec.TryFromJson(
                    File.ReadAllText(savePath),
                    out FirstStoreDiskSaveData unsupported,
                    out error),
                Is.True,
                error);
            unsupported.version = FirstStoreDiskPersistenceController.CurrentFileVersion + 1;
            File.WriteAllText(savePath, FirstStoreDiskSaveCodec.ToJson(unsupported));
            Assert.That(diskPersistence.TryLoadFromPath(savePath), Is.False);
            AssertUnchanged(before, poseBefore);

            File.WriteAllText(savePath, "{not-json");
            Assert.That(diskPersistence.TryLoadFromPath(savePath), Is.False);
            AssertUnchanged(before, poseBefore);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LoadDiscardsIncompleteStagedSessionAndAllowsCheckoutToRestart()
        {
            DeliveryBoxComponent delivery = Object.FindAnyObjectByType<DeliveryBoxComponent>();
            Assert.That(delivery.TryOpen(out _, out string error), Is.True, error);
            StockOneColaUnit();
            Assert.That(diskPersistence.TrySaveToPath(savePath), Is.True, diskPersistence.LastDiagnostic);

            StagedCheckoutInteractionComponent stagedCheckout =
                Object.FindAnyObjectByType<StagedCheckoutInteractionComponent>();
            CheckoutStationComponent checkout = stagedCheckout.Checkout;
            Assert.That(
                stagedCheckout.TryPrimary(out _, out CheckoutFailure failure, out error),
                Is.True,
                error);
            Assert.That(failure, Is.EqualTo(CheckoutFailure.None));
            Assert.That(stagedCheckout.TryPrimary(out _, out failure, out error), Is.True, error);
            Assert.That(checkout.HasActiveIncompleteSession, Is.True);
            Assert.That(stagedCheckout.NextAction, Is.EqualTo(StagedCheckoutPrimaryAction.Scan));

            Assert.That(diskPersistence.TryLoadFromPath(savePath), Is.True, diskPersistence.LastDiagnostic);

            Assert.That(checkout.HasActiveIncompleteSession, Is.False);
            Assert.That(stagedCheckout.NextAction, Is.EqualTo(StagedCheckoutPrimaryAction.Begin));
            Assert.That(
                stagedCheckout.TryPrimary(out _, out failure, out error),
                Is.True,
                error);
            Assert.That(failure, Is.EqualTo(CheckoutFailure.None));
            Assert.That(stagedCheckout.NextAction, Is.EqualTo(StagedCheckoutPrimaryAction.Scan));
            yield return null;
        }

        [UnityTest]
        public IEnumerator LoadCancelsFixturePreviewWithoutChangingRestoredPlacement()
        {
            FixturePlacementController placement =
                Object.FindAnyObjectByType<FixturePlacementController>();
            FirstStoreFixturePlacementModeController mode =
                Object.FindAnyObjectByType<FirstStoreFixturePlacementModeController>();
            PlaceableFixtureComponent fixture = Resources
                .FindObjectsOfTypeAll<PlaceableFixtureComponent>()
                .Single(item => item.StableFixtureInstanceId == "fixture-checkout-essential-01");
            FixturePlacementResult accepted = placement.IsPlaced(fixture.StableFixtureInstanceId)
                ? placement.TryMove(fixture, new GridPosition(1, 1), 0)
                : placement.TryPlace(fixture, new GridPosition(1, 1), 0);
            Assert.That(accepted.IsSuccess, Is.True, accepted.Failure.ToString());
            Assert.That(mapper.TryCapture(out FirstStoreSnapshot expectedState, out string error), Is.True, error);
            Assert.That(diskPersistence.TrySaveToPath(savePath), Is.True, diskPersistence.LastDiagnostic);

            Assert.That(mode.TrySetBuildMode(true, out error), Is.True, error);
            Assert.That(mode.TryBegin(fixture, out error), Is.True, error);
            Assert.That(
                mode.TryPreviewAtWorldPoint(GridCellCenter(placement, 4, 3), out error),
                Is.True,
                error);
            Assert.That(mode.IsActive, Is.True);
            Assert.That(fixture.PreviewState, Is.EqualTo(FixturePlacementPreviewState.Valid));

            Assert.That(diskPersistence.TryLoadFromPath(savePath), Is.True, diskPersistence.LastDiagnostic);

            Assert.That(mode.IsActive, Is.False);
            Assert.That(fixture.PreviewState, Is.EqualTo(FixturePlacementPreviewState.None));
            Assert.That(mapper.TryCapture(out FirstStoreSnapshot restoredState, out error), Is.True, error);
            Assert.That(restoredState, Is.EqualTo(expectedState));
            Assert.That(mode.TryConfirm(out error), Is.False);
            yield return null;
        }

        private void AssertUnchanged(
            FirstStoreSnapshot expectedState,
            FirstStorePlayerTransformSnapshot expectedPose)
        {
            Assert.That(mapper.TryCapture(out FirstStoreSnapshot actualState, out string error), Is.True, error);
            Assert.That(actualState, Is.EqualTo(expectedState));
            AssertPoseEqual(player.CaptureTransformSnapshot(), expectedPose);
        }

        private static void AssertPoseEqual(
            FirstStorePlayerTransformSnapshot actual,
            FirstStorePlayerTransformSnapshot expected)
        {
            Assert.That(Vector3.Distance(actual.worldPosition, expected.worldPosition), Is.LessThan(0.001f));
            Assert.That(Mathf.DeltaAngle(actual.bodyYawDegrees, expected.bodyYawDegrees), Is.EqualTo(0f).Within(0.01f));
            Assert.That(actual.cameraPitchDegrees, Is.EqualTo(expected.cameraPitchDegrees).Within(0.01f));
        }

        private static void MutateFixtureLayout()
        {
            FixturePlacementController placement =
                Object.FindAnyObjectByType<FixturePlacementController>();
            PlaceableFixtureComponent fixture = Resources
                .FindObjectsOfTypeAll<PlaceableFixtureComponent>()
                .Single(item => item.StableFixtureInstanceId == "fixture-checkout-essential-01");
            Assert.That(placement, Is.Not.Null);
            FixturePlacementResult result = placement.IsPlaced(fixture.StableFixtureInstanceId)
                ? placement.TryMove(fixture, new GridPosition(4, 3), 1)
                : placement.TryPlace(fixture, new GridPosition(1, 1), 0);
            Assert.That(result.IsSuccess, Is.True, result.Failure.ToString());
        }

        private static Vector3 GridCellCenter(
            FixturePlacementController placement,
            int x,
            int z)
        {
            return placement.GridOrigin.TransformPoint(
                new Vector3(
                    (x + 0.5f) * placement.CellSize,
                    0f,
                    (z + 0.5f) * placement.CellSize));
        }

        private static void CompleteOneColaSale()
        {
            DeliveryBoxComponent delivery = Object.FindAnyObjectByType<DeliveryBoxComponent>();
            StockingController stocking = Object.FindAnyObjectByType<StockingController>();
            CheckoutStationComponent checkout = Object.FindAnyObjectByType<CheckoutStationComponent>();
            ProductDefinition cola = Resources.FindObjectsOfTypeAll<ProductDefinition>()
                .Single(product => product.StableProductId == "prod-cola-can-355ml");
            Assert.That(delivery.TryOpen(out _, out string error), Is.True, error);
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
            StoreOperatingController store =
                Object.FindAnyObjectByType<StoreOperatingController>();
            if (store.State == StoreOperatingState.Closed)
            {
                Assert.That(store.TryOpenStore(out error), Is.True, error);
            }
            Assert.That(checkout.TryBeginSession("transaction-disk-replay-01", out error), Is.True, error);
            Assert.That(checkout.TryScan(cola, 1, out CheckoutFailure scanFailure), Is.True, scanFailure.ToString());
            Assert.That(checkout.TryComplete(out _, out CheckoutFailure completionFailure), Is.True, completionFailure.ToString());
        }

        private static void StockOneColaUnit()
        {
            DeliveryBoxComponent delivery = Object.FindAnyObjectByType<DeliveryBoxComponent>();
            StockingController stocking = Object.FindAnyObjectByType<StockingController>();
            ProductDefinition cola = Resources.FindObjectsOfTypeAll<ProductDefinition>()
                .Single(product => product.StableProductId == "prod-cola-can-355ml");
            Assert.That(
                delivery.TryRemoveOneUnit(
                    cola,
                    out ProductItem loose,
                    out _,
                    out _,
                    out string error),
                Is.True,
                error);
            Assert.That(stocking.TryPickUpLooseUnit(loose, out _, out error), Is.True, error);
            Assert.That(stocking.TryStockHeldUnit(0, out error), Is.True, error);
        }

        private static void CloseStoreForLayoutMutation()
        {
            StoreOperatingController store =
                Object.FindAnyObjectByType<StoreOperatingController>();
            CleaningTaskComponent cleaning =
                Object.FindAnyObjectByType<CleaningTaskComponent>();
            Assert.That(store.TryBeginClosing(out string error), Is.True, error);
            while (cleaning.NeedsCleaning)
            {
                cleaning.TryApplyProgress(1);
            }
            Assert.That(store.TryFinishClosing(out error), Is.True, error);
            Assert.That(store.State, Is.EqualTo(StoreOperatingState.Closed));
        }

        private static void SetConfiguredUnitCost(
            CheckoutStationComponent checkout,
            string productId,
            int unitCostCents)
        {
            FieldInfo pricesField = typeof(CheckoutStationComponent).GetField(
                "prices",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(pricesField, Is.Not.Null);
            CheckoutPriceConfiguration[] prices =
                (CheckoutPriceConfiguration[])pricesField.GetValue(checkout);
            CheckoutPriceConfiguration target = prices.Single(
                price => price.ProductDefinition.StableProductId == productId);
            FieldInfo costField = typeof(CheckoutPriceConfiguration).GetField(
                "unitCostCents",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(costField, Is.Not.Null);
            costField.SetValue(target, unitCostCents);
        }
    }
}
