using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Margins.Tests.PlayMode
{
    public sealed class PersistentPortfolioLocationSceneAdapterPlayModeTests
    {
        private const string ExpansionLocationId =
            "location-riverbend-market";

        [UnityTest]
        public IEnumerator LeaveEnterOperateLeaveAggregateReturnAndOperateReconcilesExactly()
        {
            yield return SceneManager.LoadSceneAsync(
                "FirstStoreValidation",
                LoadSceneMode.Single);
            yield return null;

            PortfolioProgressionController portfolio =
                Object.FindAnyObjectByType<PortfolioProgressionController>();
            PersistentPortfolioLocationSceneAdapter adapter =
                Object.FindAnyObjectByType<
                    PersistentPortfolioLocationSceneAdapter>();
            PersistentPortfolioLocationController locations =
                Object.FindAnyObjectByType<
                    PersistentPortfolioLocationController>();
            FirstStorePersistenceMapperComponent persistence =
                Object.FindAnyObjectByType<
                    FirstStorePersistenceMapperComponent>();
            FirstStoreDiskPersistenceController disk =
                Object.FindAnyObjectByType<
                    FirstStoreDiskPersistenceController>();
            StoreOperatingController store =
                Object.FindAnyObjectByType<StoreOperatingController>();
            CheckoutStationComponent checkout =
                Object.FindAnyObjectByType<CheckoutStationComponent>();
            FirstStoreMerchandisingComponent merchandising =
                Object.FindAnyObjectByType<
                    FirstStoreMerchandisingComponent>();
            StoreCustomerFlowController customerFlow =
                Object.FindAnyObjectByType<StoreCustomerFlowController>();
            InStoreEmployeeWorkController employeeWork =
                Object.FindAnyObjectByType<
                    InStoreEmployeeWorkController>();
            FirstPersonController player =
                Object.FindAnyObjectByType<FirstPersonController>();

            Assert.That(portfolio, Is.Not.Null);
            Assert.That(adapter, Is.Not.Null);
            Assert.That(locations, Is.Not.Null);
            Assert.That(persistence, Is.Not.Null);
            Assert.That(disk, Is.Not.Null);
            Assert.That(store, Is.Not.Null);
            Assert.That(checkout, Is.Not.Null);
            Assert.That(merchandising, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(adapter.TryValidateConfiguration(out string error),
                Is.True,
                error);

            if (customerFlow != null)
            {
                customerFlow.enabled = false;
            }
            if (employeeWork != null)
            {
                employeeWork.enabled = false;
            }

            CompletePhysicalFirstShift(portfolio, store);
            HireTeam(
                portfolio,
                PortfolioProgressionRules.FirstLocationId,
                "employee-elena-ruiz",
                "employee-marcus-reed",
                "employee-priya-shah");
            player.SetGameplayMode(false);
            Assert.That(
                portfolio.TryAdvanceDelegatedDay(out error),
                Is.True,
                error);
            Assert.That(
                portfolio.TryLeaseLocation(ExpansionLocationId, out error),
                Is.True,
                error);
            HireTeam(
                portfolio,
                ExpansionLocationId,
                "employee-jonah-brooks",
                "employee-nia-carter",
                "employee-luis-ortega");
            Assert.That(
                portfolio.Progression.TrySetPricingPolicy(
                    ExpansionLocationId,
                    PortfolioPricingPolicy.Premium,
                    out error),
                Is.True,
                error);
            Assert.That(
                persistence.TryCapture(
                    out FirstStoreSnapshot firstStoreBeforeTravel,
                    out error),
                Is.True,
                error);

            PortfolioProgressionSnapshot beforeVisit =
                portfolio.Progression.CreateSnapshot();
            PortfolioLocationSnapshot remoteBefore = beforeVisit.locations
                .Single(value => value.locationId == ExpansionLocationId);
            Assert.That(
                adapter.TryEnterLocation(ExpansionLocationId, out error),
                Is.True,
                error);
            yield return null;

            Assert.That(adapter.ActiveLocationId,
                Is.EqualTo(ExpansionLocationId));
            Assert.That(locations.ActiveBuilding, Is.Not.Null);
            Assert.That(merchandising.LocationId,
                Is.EqualTo(ExpansionLocationId));
            Assert.That(
                portfolio.Progression.CreateSnapshot().company
                    .activeDetailedLocationId,
                Is.EqualTo(ExpansionLocationId));
            AssertPlayerInsideSelectedUnit(player, locations.ActiveBuilding);
            string firstGeneratedSignature =
                locations.ActiveBuilding.LastSignature;

            CheckoutTransactionSummary firstRemoteSale =
                CompleteGeneratedCheckout(locations.ActiveBuilding, checkout);
            Assert.That(
                adapter.TrySynchronizeActiveLocation(out error),
                Is.True,
                error);
            PortfolioProgressionSnapshot afterFirstSale =
                portfolio.Progression.CreateSnapshot();
            PortfolioLocationSnapshot remoteAfterFirstSale =
                afterFirstSale.locations.Single(value =>
                    value.locationId == ExpansionLocationId);
            long remotePayroll = afterFirstSale.employees
                .Where(value => value.assignedLocationId == ExpansionLocationId)
                .Sum(value => value.dailyWageCents);
            Assert.That(remoteAfterFirstSale.inventoryUnits,
                Is.EqualTo(
                    remoteBefore.inventoryUnits - firstRemoteSale.unitsSold));
            Assert.That(remoteAfterFirstSale.lifetimeGrossSalesCents,
                Is.EqualTo(
                    remoteBefore.lifetimeGrossSalesCents +
                    firstRemoteSale.subtotalCents));
            Assert.That(afterFirstSale.cashCents,
                Is.EqualTo(
                    beforeVisit.cashCents + firstRemoteSale.subtotalCents -
                    remoteBefore.dailyRentCents - remotePayroll));

            Assert.That(
                adapter.TrySynchronizeActiveLocation(out error),
                Is.True,
                error);
            PortfolioProgressionSnapshot repeated =
                portfolio.Progression.CreateSnapshot();
            Assert.That(repeated.cashCents,
                Is.EqualTo(afterFirstSale.cashCents));
            Assert.That(
                repeated.locations.Single(value =>
                    value.locationId == ExpansionLocationId).inventoryUnits,
                Is.EqualTo(remoteAfterFirstSale.inventoryUnits));
            LogAssert.Expect(
                LogType.Warning,
                "Save rejected: Leave the generated location before saving, loading, or starting a new business.");
            Assert.That(
                disk.TrySaveToPath(System.IO.Path.Combine(
                    Application.temporaryCachePath,
                    $"margins-generated-location-{Guid.NewGuid():N}.json")),
                Is.False,
                "The parked first-store snapshot must not be replaced by a generated-location rig.");

            PersistentLocationExitWorldInteractionTarget exit = locations
                .ActiveBuilding.GetComponentInChildren<
                    PersistentLocationExitWorldInteractionTarget>();
            Assert.That(exit, Is.Not.Null);
            Assert.That(exit.TryPrimary(out error), Is.True, error);
            yield return null;

            Assert.That(adapter.ActiveLocationId, Is.Null);
            Assert.That(
                portfolio.Progression.CreateSnapshot().company
                    .activeDetailedLocationId,
                Is.Null);
            Assert.That(player.IsGameplayMode, Is.False);
            Assert.That(merchandising.LocationId,
                Is.EqualTo(PortfolioProgressionRules.FirstLocationId));
            Assert.That(
                persistence.TryCapture(
                    out FirstStoreSnapshot firstStoreAfterTravel,
                    out error),
                Is.True,
                error);
            Assert.That(firstStoreAfterTravel,
                Is.EqualTo(firstStoreBeforeTravel),
                "Reusing the detailed rig must not mutate the parked first-store state.");

            Assert.That(
                portfolio.TryAdvanceDelegatedDay(out error),
                Is.True,
                error);
            PortfolioProgressionSnapshot afterAggregate =
                portfolio.Progression.CreateSnapshot();
            PortfolioLocationSnapshot remoteAfterAggregate = afterAggregate
                .locations.Single(value =>
                    value.locationId == ExpansionLocationId);
            Assert.That(remoteAfterAggregate.lastReport.isDetailedOperation,
                Is.False);

            Assert.That(
                adapter.TryEnterLocation(ExpansionLocationId, out error),
                Is.True,
                error);
            yield return null;
            Assert.That(locations.ActiveBuilding.LastSignature,
                Is.EqualTo(firstGeneratedSignature));
            AssertPlayerInsideSelectedUnit(player, locations.ActiveBuilding);
            CheckoutTransactionSummary secondRemoteSale =
                CompleteGeneratedCheckout(locations.ActiveBuilding, checkout);
            Assert.That(
                adapter.TryLeaveToManagement(out error),
                Is.True,
                error);
            yield return null;

            PortfolioProgressionSnapshot afterSecondLeave =
                portfolio.Progression.CreateSnapshot();
            PortfolioLocationSnapshot remoteAfterSecondLeave =
                afterSecondLeave.locations.Single(value =>
                    value.locationId == ExpansionLocationId);
            Assert.That(remoteAfterSecondLeave.inventoryUnits,
                Is.EqualTo(
                    remoteAfterAggregate.inventoryUnits -
                    secondRemoteSale.unitsSold));
            Assert.That(remoteAfterSecondLeave.lifetimeGrossSalesCents,
                Is.EqualTo(
                    remoteAfterAggregate.lifetimeGrossSalesCents +
                    secondRemoteSale.subtotalCents));
            Assert.That(afterSecondLeave.cashCents,
                Is.EqualTo(
                    afterAggregate.cashCents +
                    secondRemoteSale.subtotalCents),
                "Aggregate daily costs were already charged before detailed return and must not post twice.");
            Assert.That(
                adapter.TryLeaveToManagement(out error),
                Is.True,
                error);
            PortfolioProgressionSnapshot afterRepeatedLeave =
                portfolio.Progression.CreateSnapshot();
            Assert.That(afterRepeatedLeave.cashCents,
                Is.EqualTo(afterSecondLeave.cashCents));
            Assert.That(
                afterRepeatedLeave.locations.Single(value =>
                    value.locationId == ExpansionLocationId).inventoryUnits,
                Is.EqualTo(remoteAfterSecondLeave.inventoryUnits));
        }

        private static CheckoutTransactionSummary CompleteGeneratedCheckout(
            ProceduralCommercialBuilding building,
            CheckoutStationComponent checkout)
        {
            StagedCheckoutWorldInteractionTarget register = building
                .GetComponentsInChildren<
                    StagedCheckoutWorldInteractionTarget>(true)
                .Single(value => value.StableTargetId.StartsWith(
                    "target-persistent-checkout-",
                    StringComparison.Ordinal));
            int transactionsBefore = checkout.CompletedTransactionCount;
            Assert.That(register.TryPrimary(out string error), Is.True, error);
            int safety = 8;
            while (checkout.HasActiveIncompleteSession && safety-- > 0)
            {
                CheckoutProductWorldInteractionTarget[] productTargets = building
                    .GetComponentsInChildren<
                        CheckoutProductWorldInteractionTarget>(true);
                StagedCheckoutInteractionComponent staged =
                    checkout.GetComponent<StagedCheckoutInteractionComponent>();
                Assert.That(
                    productTargets.Any(value => value.IsAvailable),
                    Is.True,
                    $"Generated checkout has no available physical product target; " +
                    $"targets={productTargets.Length}, action={staged.NextAction}, " +
                    $"active={staged.ActiveProduct?.StableProductId}, " +
                    $"store={checkout.GetComponent<StoreOperatingController>()?.State}/{staged.Checkout != null}.");
                CheckoutProductWorldInteractionTarget product =
                    productTargets.Single(value => value.IsAvailable);
                Assert.That(product.TryPrimary(out error), Is.True, error);
                if (!checkout.HasActiveIncompleteSession)
                {
                    break;
                }

                if (staged.NextAction == StagedCheckoutPrimaryAction.Complete)
                {
                    Assert.That(register.TryPrimary(out error), Is.True, error);
                }
            }

            if (checkout.HasActiveIncompleteSession)
            {
                Assert.That(register.TryPrimary(out error), Is.True, error);
            }
            Assert.That(checkout.CompletedTransactionCount,
                Is.EqualTo(transactionsBefore + 1));
            return checkout.CompletedTransactions.Last();
        }

        private static void AssertPlayerInsideSelectedUnit(
            FirstPersonController player,
            ProceduralCommercialBuilding building)
        {
            Vector3 local = building.transform.InverseTransformPoint(
                player.transform.position);
            Assert.That(
                building.LastResult.Units[0].BoundsMeters.Contains(
                    new Vector2(local.x, local.z),
                    0.02f),
                Is.True,
                "The scene adapter must place the player inside the selected persistent commercial unit.");
        }

        private static void CompletePhysicalFirstShift(
            PortfolioProgressionController portfolio,
            StoreOperatingController store)
        {
            FixturePlacementController placement =
                Object.FindAnyObjectByType<FixturePlacementController>();
            PlaceableFixtureComponent fixture = Resources
                .FindObjectsOfTypeAll<PlaceableFixtureComponent>()
                .Single(value => value.StableFixtureInstanceId ==
                                 "fixture-checkout-essential-01");
            if (!placement.IsPlaced(fixture.StableFixtureInstanceId))
            {
                FixturePlacementResult placed = placement.TryPlace(
                    fixture,
                    new GridPosition(1, 1),
                    0);
                Assert.That(placed.IsSuccess,
                    Is.True,
                    placed.Failure.ToString());
            }

            DeliveryBoxComponent delivery =
                Object.FindAnyObjectByType<DeliveryBoxComponent>();
            StockingController stocking =
                Object.FindAnyObjectByType<StockingController>();
            CheckoutStationComponent checkout =
                Object.FindAnyObjectByType<CheckoutStationComponent>();
            ProductDefinition cola = Resources
                .FindObjectsOfTypeAll<ProductDefinition>()
                .Single(value => value.StableProductId ==
                                 "prod-cola-can-355ml");
            Assert.That(delivery.TryOpen(out _, out string error),
                Is.True,
                error);
            Assert.That(
                delivery.TryRemoveOneUnit(
                    cola,
                    out ProductItem loose,
                    out _,
                    out _,
                    out error),
                Is.True,
                error);
            Assert.That(stocking.TryPickUpLooseUnit(loose, out _, out error),
                Is.True,
                error);
            Assert.That(stocking.TryStockHeldUnit(0, out error),
                Is.True,
                error);
            Assert.That(store.TryOpenStore(out error), Is.True, error);
            Assert.That(
                checkout.TryBeginSession(
                    "transaction-location-travel-first-001",
                    out error),
                Is.True,
                error);
            Assert.That(
                checkout.TryScan(
                    cola,
                    1,
                    out CheckoutFailure scanFailure),
                Is.True,
                scanFailure.ToString());
            Assert.That(
                checkout.TryComplete(
                    out _,
                    out CheckoutFailure completionFailure),
                Is.True,
                completionFailure.ToString());
            Assert.That(portfolio.TrySynchronizeDetailedShift(out error),
                Is.True,
                error);
        }

        private static void HireTeam(
            PortfolioProgressionController portfolio,
            string locationId,
            params string[] employeeIds)
        {
            foreach (string employeeId in employeeIds)
            {
                Assert.That(
                    portfolio.TryHireCandidate(
                        employeeId,
                        locationId,
                        out string error),
                    Is.True,
                    error);
            }
        }
    }
}
