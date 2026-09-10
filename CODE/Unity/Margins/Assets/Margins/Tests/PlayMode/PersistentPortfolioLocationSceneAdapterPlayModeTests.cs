using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Margins.Tests.PlayMode
{
    public sealed class PersistentPortfolioLocationSceneAdapterPlayModeTests
    {
        private const string RiverbendLocationId =
            "location-riverbend-market";
        private const string DowntownLocationId =
            "location-downtown-market";
        private static readonly string[] FirstTeam =
        {
            "employee-elena-ruiz",
            "employee-marcus-reed",
            "employee-priya-shah"
        };
        private static readonly string[] ExpansionTeam =
        {
            "employee-jonah-brooks",
            "employee-nia-carter",
            "employee-luis-ortega"
        };
        private sealed class SceneContext
        {
            public PortfolioProgressionController Portfolio;
            public FirstStoreDiskPersistenceController Disk;
            public PersistentPortfolioLocationSceneAdapter Adapter;
            public PersistentPortfolioLocationController Locations;
            public FirstStorePersistenceMapperComponent Persistence;
            public StoreOperatingController Store;
            public CheckoutStationComponent Checkout;
            public FirstStoreInventoryComponent Inventory;
            public PhysicalProductUnitRegistry PhysicalUnits;
            public StockingController Stocking;
            public FirstStoreMerchandisingComponent Merchandising;
            public StoreCustomerFlowController CustomerFlow;
            public InStoreEmployeeWorkController EmployeeWork;
            public DeliveryBoxComponent Delivery;
            public CleaningTaskComponent Cleaning;
            public CleaningWorldInteractionTarget CleaningTarget;
            public CarryableToolComponent CleaningTool;
            public StoreOperatingWorldInteractionTarget OperatingControl;
            public FirstPersonController Player;
            public ProductDefinition[] Products;
        }

        [UnityTest]
        public IEnumerator GeneratedStoreRunsSharedCustomersEmployeesDeliveryStockingCleaningAndNavigation()
        {
            SceneContext context = null;
            yield return LoadContext(value => context = value);
            PrepareRiverbendPortfolio(context);
            Assert.That(
                context.Persistence.TryCapture(
                    out FirstStoreSnapshot firstStoreBeforeTravel,
                    out string error),
                Is.True,
                error);

            PortfolioLocationSnapshot beforeVisit = Location(
                context.Portfolio.Progression.CreateSnapshot(),
                RiverbendLocationId);
            Assert.That(
                context.Adapter.TryEnterLocation(
                    RiverbendLocationId,
                    out error),
                Is.True,
                error);
            yield return null;

            AssertGeneratedDetailedAuthorities(
                context,
                RiverbendLocationId,
                CommercialBuildingArchetype.StripCenterInlineRetail);
            string generatedSignature = context.Locations.ActiveBuilding
                .LastSignature;
            SuppressAutomaticArrivalForControlledSale(context);
            int shelvedBeforeSale = context.PhysicalUnits.VisibleUnits.Count(
                value => value != null && value.IsSnapped);
            CheckoutTransactionSummary employeeSale = null;
            yield return CompleteLiveCustomerSale(
                context,
                employeeCheckout: true,
                value => employeeSale = value);
            Assert.That(employeeSale, Is.Not.Null);
            Assert.That(
                context.PhysicalUnits.VisibleUnits.Count(value =>
                    value != null && value.IsSnapped),
                Is.EqualTo(shelvedBeforeSale - employeeSale.unitsSold));

            Assert.That(
                context.Cleaning.NeedsCleaning ||
                context.Cleaning.TryCreateMess(),
                Is.True);
            Assert.That(context.CleaningTool.StorageTool.TryPrimary(out error), Is.True, error);
            Assert.That(
                Object.FindAnyObjectByType<PlayerCarryableToolController>()
                    .TrySetDownHeldTool(out error), Is.True, error);
            Assert.That(context.CleaningTool.TryPrimary(out error), Is.True, error);
            while (!context.Cleaning.IsComplete)
            {
                Assert.That(
                    context.CleaningTarget.TryPrimary(out error),
                    Is.True,
                    error);
            }
            Assert.That(context.Cleaning.IsComplete, Is.True);
            Assert.That(context.CleaningTool.TryPrimary(out error), Is.True, error);

            Assert.That(
                context.Portfolio.TryPlaceManualPurchaseOrder(
                    RiverbendLocationId,
                    out error),
                Is.True,
                error);
            Assert.That(
                context.Portfolio.Progression.TryAdvanceProcurementTicks(
                    ConvenienceStoreProcurement.FulfillmentDelayTicks,
                    out int fulfilled,
                    out error),
                Is.True,
                error);
            Assert.That(fulfilled, Is.EqualTo(1));
            Assert.That(
                context.Portfolio.TrySynchronizeDetailedProcurement(out error),
                Is.True,
                error);
            PurchaseOrderSnapshot order = context.Portfolio.Progression
                .PurchaseOrders.Single(value =>
                    value.locationId == RiverbendLocationId &&
                    !value.IsTerminal);
            Assert.That(order.status, Is.EqualTo(PurchaseOrderStatus.Delivered));
            Assert.That(context.Delivery.IsSealed, Is.True);
            int deliveryUnitsBeforeWork = RemainingDeliveryUnits(context);
            Assert.That(deliveryUnitsBeforeWork, Is.EqualTo(
                ConvenienceStoreProcurement.DetailedCaseUnitsPerProduct *
                context.Products.Length));

            yield return WaitUntil(
                () => RemainingDeliveryUnits(context) < deliveryUnitsBeforeWork,
                25f,
                "The assigned stock clerk did not navigate, open the physical delivery, and remove stock.");
            yield return WaitUntil(
                () => !context.EmployeeWork.IsHandlingInventory,
                20f,
                "The stock clerk did not finish the physical shelf placement.");
            Assert.That(
                context.PhysicalUnits.VisibleUnits.Count(value =>
                    value != null && value.IsSnapped),
                Is.EqualTo(shelvedBeforeSale),
                "Employee stocking must refill the shelf position freed by the live customer sale.");

            context.EmployeeWork.enabled = false;
            DrainDeliveryToGeneratedLooseStock(context);
            Assert.That(RemainingDeliveryUnits(context), Is.Zero);
            Assert.That(
                context.Portfolio.TrySynchronizeDetailedProcurement(out error),
                Is.True,
                error);
            Assert.That(
                context.Portfolio.Progression.PurchaseOrders.Single(value =>
                    value.orderId == order.orderId).status,
                Is.EqualTo(PurchaseOrderStatus.Completed));
            context.EmployeeWork.enabled = true;

            Assert.That(
                context.Adapter.TrySynchronizeActiveLocation(out error),
                Is.True,
                error);
            PortfolioProgressionSnapshot afterDetailedWork =
                context.Portfolio.Progression.CreateSnapshot();
            PortfolioLocationSnapshot afterVisit = Location(
                afterDetailedWork,
                RiverbendLocationId);
            Assert.That(afterVisit.inventoryUnits,
                Is.EqualTo(
                    beforeVisit.inventoryUnits - employeeSale.unitsSold +
                    ConvenienceStoreProcurement.DetailedCaseUnitsPerProduct *
                    context.Products.Length));
            Assert.That(afterVisit.lifetimeGrossSalesCents,
                Is.EqualTo(
                    beforeVisit.lifetimeGrossSalesCents +
                    employeeSale.subtotalCents));

            Assert.That(
                context.Adapter.TryLeaveToManagement(out error),
                Is.True,
                error);
            Assert.That(context.Adapter.ActiveLocationId, Is.Null);
            Assert.That(context.Merchandising.LocationId,
                Is.EqualTo(PortfolioProgressionRules.FirstLocationId));
            Assert.That(
                context.Persistence.TryCapture(
                    out FirstStoreSnapshot firstStoreAfterTravel,
                    out error),
                Is.True,
                error);
            Assert.That(firstStoreAfterTravel,
                Is.EqualTo(firstStoreBeforeTravel),
                "Generated detailed operation must not mutate the parked first-store state.");
            context.CustomerFlow.enabled = false;
            context.EmployeeWork.enabled = false;
            yield return null;

            Assert.That(
                context.Portfolio.TryAdvanceDelegatedDay(out error),
                Is.True,
                error);
            PortfolioProgressionSnapshot afterAggregate =
                context.Portfolio.Progression.CreateSnapshot();
            PortfolioLocationSnapshot afterAggregateLocation = Location(
                afterAggregate,
                RiverbendLocationId);
            Assert.That(
                afterAggregateLocation.lastReport.isDetailedOperation,
                Is.False);
            PurchaseOrderSnapshot aggregateReturnOrder = afterAggregate
                .procurement.orders.Single(value =>
                    value.locationId == RiverbendLocationId &&
                    !value.IsTerminal);
            int deliveryCapacity = context.Inventory.Inventory.CreateSnapshot()
                .locations.Single(value =>
                    value.locationId == context.Delivery.InventoryLocationId)
                .capacityUnits;
            Assert.That(
                aggregateReturnOrder.OrderedQuantityUnits,
                Is.GreaterThan(deliveryCapacity),
                "This regression must cross the aggregate-order/physical-container boundary.");

            context.CustomerFlow.enabled = true;
            context.EmployeeWork.enabled = true;
            Assert.That(
                context.Adapter.TryEnterLocation(
                    RiverbendLocationId,
                    out error),
                Is.True,
                error);
            yield return null;
            Assert.That(
                context.Locations.ActiveBuilding.LastSignature,
                Is.EqualTo(generatedSignature));
            SuppressAutomaticArrivalForControlledSale(context);
            CheckoutTransactionSummary returnSale = null;
            yield return CompleteLiveCustomerSale(
                context,
                employeeCheckout: true,
                value => returnSale = value);
            yield return WaitUntil(
                () => !context.EmployeeWork.IsHandlingInventory,
                20f,
                "The stock clerk did not finish the aggregate-return delivery move.");
            context.EmployeeWork.enabled = false;
            if (context.Delivery.IsSealed)
            {
                Assert.That(
                    context.Delivery.TryOpen(out _, out error),
                    Is.True,
                    error);
            }
            DrainDeliveryToGeneratedLooseStock(context);
            Assert.That(
                context.Portfolio.TrySynchronizeDetailedProcurement(out error),
                Is.True,
                error);
            Assert.That(
                context.Portfolio.Progression.PurchaseOrders.Single(value =>
                    value.orderId == aggregateReturnOrder.orderId).status,
                Is.EqualTo(PurchaseOrderStatus.Completed));
            context.EmployeeWork.enabled = true;
            Assert.That(
                context.Adapter.TryLeaveToManagement(out error),
                Is.True,
                error);
            PortfolioProgressionSnapshot afterDetailedReturn =
                context.Portfolio.Progression.CreateSnapshot();
            PortfolioLocationSnapshot returnedLocation = Location(
                afterDetailedReturn,
                RiverbendLocationId);
            Assert.That(
                returnedLocation.inventoryUnits,
                Is.EqualTo(
                    afterAggregateLocation.inventoryUnits -
                    returnSale.unitsSold +
                    aggregateReturnOrder.OrderedQuantityUnits));
            Assert.That(
                returnedLocation.lifetimeGrossSalesCents,
                Is.EqualTo(
                    afterAggregateLocation.lifetimeGrossSalesCents +
                    returnSale.subtotalCents));
            Assert.That(
                afterDetailedReturn.cashCents,
                Is.EqualTo(afterAggregate.cashCents + returnSale.subtotalCents),
                "Aggregate daily costs must not post again on detailed return.");
            Assert.That(
                context.Adapter.TryLeaveToManagement(out error),
                Is.True,
                error);
            Assert.That(
                context.Portfolio.Progression.CreateSnapshot().cashCents,
                Is.EqualTo(afterDetailedReturn.cashCents));
        }

        [UnityTest]
        public IEnumerator TwoGeneratedStoresOperatePhysicallyWithoutSharingLocationState()
        {
            SceneContext context = null;
            yield return LoadContext(value => context = value);
            PrepareRiverbendPortfolio(context);

            Assert.That(
                context.Adapter.TryEnterLocation(
                    RiverbendLocationId,
                    out string error),
                Is.True,
                error);
            yield return null;
            string riverbendSignature = context.Locations.ActiveBuilding
                .LastSignature;
            SuppressAutomaticArrivalForControlledSale(context);
            CheckoutTransactionSummary firstRiverbendSale = null;
            yield return CompleteLiveCustomerSale(
                context,
                employeeCheckout: true,
                value => firstRiverbendSale = value);
            Assert.That(firstRiverbendSale, Is.Not.Null);
            Assert.That(
                context.Adapter.TryLeaveToManagement(out error),
                Is.True,
                error);
            yield return null;

            SeedReconciledHistoricalEarnings(context, 1_000_000);
            Assert.That(
                context.Portfolio.TryLeaseLocation(
                    DowntownLocationId,
                    out error),
                Is.True,
                error);
            ReassignTeam(
                context.Portfolio.Progression,
                DowntownLocationId,
                ExpansionTeam);

            PortfolioProgressionSnapshot beforeDowntown =
                context.Portfolio.Progression.CreateSnapshot();
            PortfolioLocationSnapshot riverbendBeforeDowntown = Location(
                beforeDowntown,
                RiverbendLocationId);
            PortfolioLocationSnapshot downtownBeforeVisit = Location(
                beforeDowntown,
                DowntownLocationId);
            Assert.That(
                context.Adapter.TryEnterLocation(
                    DowntownLocationId,
                    out error),
                Is.True,
                error);
            yield return null;
            AssertGeneratedDetailedAuthorities(
                context,
                DowntownLocationId,
                CommercialBuildingArchetype.OlderMainStreetMixedUse);
            Assert.That(context.Locations.ActiveBuilding.LastSignature,
                Is.Not.EqualTo(riverbendSignature));
            SuppressAutomaticArrivalForControlledSale(context);

            CheckoutTransactionSummary downtownSale = null;
            yield return CompleteLiveCustomerSale(
                context,
                employeeCheckout: false,
                value => downtownSale = value);
            Assert.That(
                context.Adapter.TrySynchronizeActiveLocation(out error),
                Is.True,
                error);
            PortfolioProgressionSnapshot afterDowntown =
                context.Portfolio.Progression.CreateSnapshot();
            PortfolioLocationSnapshot riverbendAfterDowntown = Location(
                afterDowntown,
                RiverbendLocationId);
            PortfolioLocationSnapshot downtownAfterVisit = Location(
                afterDowntown,
                DowntownLocationId);
            AssertLocationBusinessStateEqual(
                riverbendBeforeDowntown,
                riverbendAfterDowntown,
                "Operating Downtown must not mutate Riverbend state.");
            Assert.That(downtownAfterVisit.inventoryUnits,
                Is.EqualTo(
                    downtownBeforeVisit.inventoryUnits -
                    downtownSale.unitsSold));
            Assert.That(downtownAfterVisit.lifetimeGrossSalesCents,
                Is.EqualTo(
                    downtownBeforeVisit.lifetimeGrossSalesCents +
                    downtownSale.subtotalCents));

            Assert.That(
                context.Adapter.TryLeaveToManagement(out error),
                Is.True,
                error);
            yield return null;
            ReassignTeam(
                context.Portfolio.Progression,
                RiverbendLocationId,
                ExpansionTeam);
            PortfolioLocationSnapshot downtownBeforeReturn = Location(
                context.Portfolio.Progression.CreateSnapshot(),
                DowntownLocationId);

            Assert.That(
                context.Adapter.TryEnterLocation(
                    RiverbendLocationId,
                    out error),
                Is.True,
                error);
            yield return null;
            Assert.That(context.Locations.ActiveBuilding.LastSignature,
                Is.EqualTo(riverbendSignature));
            Assert.That(context.EmployeeWork.DetailedLocationId,
                Is.EqualTo(RiverbendLocationId));
            SuppressAutomaticArrivalForControlledSale(context);
            CheckoutTransactionSummary secondRiverbendSale = null;
            yield return CompleteLiveCustomerSale(
                context,
                employeeCheckout: true,
                value => secondRiverbendSale = value);
            Assert.That(
                context.Adapter.TrySynchronizeActiveLocation(out error),
                Is.True,
                error);
            PortfolioProgressionSnapshot afterRiverbendReturn =
                context.Portfolio.Progression.CreateSnapshot();
            AssertLocationBusinessStateEqual(
                downtownBeforeReturn,
                Location(afterRiverbendReturn, DowntownLocationId),
                "Returning physically to Riverbend must not mutate Downtown state.");
            PortfolioLocationSnapshot riverbendAfterReturn = Location(
                afterRiverbendReturn,
                RiverbendLocationId);
            Assert.That(riverbendAfterReturn.inventoryUnits,
                Is.EqualTo(
                    riverbendBeforeDowntown.inventoryUnits -
                    secondRiverbendSale.unitsSold));
            Assert.That(riverbendAfterReturn.lifetimeGrossSalesCents,
                Is.EqualTo(
                    riverbendBeforeDowntown.lifetimeGrossSalesCents +
                    secondRiverbendSale.subtotalCents));
            Assert.That(
                context.Adapter.TryLeaveToManagement(out error),
                Is.True,
                error);
        }

        [UnityTest]
        public IEnumerator GeneratedLocationSaveRestoresDetailedStateAndReconcilesNextSaleOnce()
        {
            string directory = Path.Combine(
                Application.temporaryCachePath,
                $"generated-location-save-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "company.json");
            try
            {
                SceneContext context = null;
                yield return LoadContext(value => context = value);
                PrepareRiverbendPortfolio(context);
                Assert.That(
                    context.Adapter.TryEnterLocation(
                        RiverbendLocationId,
                        out string error),
                    Is.True,
                    error);
                yield return null;
                SuppressAutomaticArrivalForControlledSale(context);

                CheckoutTransactionSummary saleBeforeSave = null;
                yield return CompleteLiveCustomerSale(
                    context,
                    employeeCheckout: false,
                    value => saleBeforeSave = value);
                Assert.That(saleBeforeSave, Is.Not.Null);
                Assert.That(
                    context.CustomerFlow.TryAdmitCustomerNow(
                        out string savedCustomerId,
                        out error),
                    Is.True,
                    error);
                context.CustomerFlow.enabled = false;
                Assert.That(
                    context.CustomerFlow.TryCaptureSnapshot(
                        out StoreCustomerFlowSnapshot customersBeforeSave,
                        out error),
                    Is.True,
                    error);
                Assert.That(
                    customersBeforeSave.customers.Select(value =>
                        value.customerId),
                    Does.Contain(savedCustomerId));

                FirstStorePlayerTransformSnapshot savedPose =
                    context.Player.CaptureTransformSnapshot();
                savedPose.worldPosition +=
                    context.Locations.ActiveBuilding.transform.right * 0.15f;
                Assert.That(
                    context.Player.TryApplyTransformSnapshot(
                        savedPose,
                        out error),
                    Is.True,
                    error);
                string signature = context.Locations.ActiveBuilding.LastSignature;
                Assert.That(
                    context.Adapter.TryCapturePersistenceState(
                        out FirstStoreSnapshot parkedStore,
                        out PersistentGeneratedLocationDiskSnapshot activeStore,
                        out error),
                    Is.True,
                    error);
                Assert.That(
                    context.Portfolio.TryCaptureSnapshot(
                        out PortfolioProgressionSnapshot capturedPortfolio,
                        out error),
                    Is.True,
                    error);
                Assert.That(
                    context.Portfolio.TryValidateDetailedMerchandisingReconciliation(
                        parkedStore,
                        capturedPortfolio,
                        out error),
                    Is.True,
                    $"Parked first store: {error}");
                Assert.That(
                    context.Portfolio.TryValidateDetailedMerchandisingReconciliation(
                        RiverbendLocationId,
                        activeStore.detailedStore,
                        capturedPortfolio,
                        out error),
                    Is.True,
                    $"Active generated store: {error}");
                Assert.That(
                    context.Disk.TrySaveToPath(path),
                    Is.True,
                    context.Disk.LastDiagnostic);
                Assert.That(
                    FirstStoreDiskSaveCodec.TryFromJson(
                        File.ReadAllText(path),
                        out FirstStoreDiskSaveData accepted,
                        out error),
                    Is.True,
                    error);
                Assert.That(accepted.version,
                    Is.EqualTo(FirstStoreDiskPersistenceController.CurrentFileVersion));
                Assert.That(accepted.hasGeneratedLocation, Is.True);
                Assert.That(
                    accepted.sourceState,
                    Is.EqualTo(
                        FirstStoreDiskSourceState.GeneratedDetailed));
                Assert.That(accepted.generatedLocation, Is.Not.Null);
                Assert.That(accepted.generatedLocation.locationId,
                    Is.EqualTo(RiverbendLocationId));
                Assert.That(
                    accepted.generatedLocation.detailedStore.customerFlow.customers
                        .Select(value => value.customerId),
                    Does.Contain(savedCustomerId));

                string acceptedJson = File.ReadAllText(path);
                Assert.That(
                    context.Persistence.TryCapture(
                        out FirstStoreSnapshot beforeRejectedLoad,
                        out error),
                    Is.True,
                    error);
                PortfolioProgressionSnapshot portfolioBeforeRejectedLoad =
                    context.Portfolio.Progression.CreateSnapshot();
                FirstStorePlayerTransformSnapshot poseBeforeRejectedLoad =
                    context.Player.CaptureTransformSnapshot();
                Assert.That(
                    FirstStoreDiskSaveCodec.TryFromJson(
                        acceptedJson,
                        out FirstStoreDiskSaveData contradictory,
                        out error),
                    Is.True,
                    error);
                contradictory.generatedLocation.locationId = DowntownLocationId;
                File.WriteAllText(
                    path,
                    FirstStoreDiskSaveCodec.ToJson(contradictory));
                Assert.That(
                    context.Disk.TryLoadFromPath(path),
                    Is.False,
                    "Contradictory active generated-location identity must reject before mutation.");
                Assert.That(context.Adapter.ActiveLocationId,
                    Is.EqualTo(RiverbendLocationId));
                Assert.That(context.Locations.ActiveBuilding.LastSignature,
                    Is.EqualTo(signature));
                Assert.That(
                    context.Persistence.TryCapture(
                        out FirstStoreSnapshot afterRejectedLoad,
                        out error),
                    Is.True,
                    error);
                AssertJsonEqual(
                    beforeRejectedLoad,
                    afterRejectedLoad,
                    "Detailed state after rejected generated-location load");
                AssertJsonEqual(
                    portfolioBeforeRejectedLoad,
                    context.Portfolio.Progression.CreateSnapshot(),
                    "Portfolio after rejected generated-location load");
                FirstStorePlayerTransformSnapshot poseAfterRejectedLoad =
                    context.Player.CaptureTransformSnapshot();
                Assert.That(
                    Vector3.Distance(
                        poseAfterRejectedLoad.worldPosition,
                        poseBeforeRejectedLoad.worldPosition),
                    Is.LessThan(0.001f));
                Assert.That(poseAfterRejectedLoad.bodyYawDegrees,
                    Is.EqualTo(poseBeforeRejectedLoad.bodyYawDegrees).Within(0.001f));
                Assert.That(poseAfterRejectedLoad.cameraPitchDegrees,
                    Is.EqualTo(poseBeforeRejectedLoad.cameraPitchDegrees).Within(0.001f));
                File.WriteAllText(path, acceptedJson);

                PortfolioLocationSnapshot savedLocation = Location(
                    accepted.portfolio,
                    RiverbendLocationId);
                context.CustomerFlow.ResetTransientStateForRestore();
                Assert.That(
                    context.Portfolio.Progression.TrySetReorderPolicy(
                        RiverbendLocationId,
                        PortfolioReorderPolicy.Resilient,
                        out error),
                    Is.True,
                    error);
                Assert.That(
                    context.Player.TryApplyTransformSnapshot(
                        new FirstStorePlayerTransformSnapshot(
                            savedPose.worldPosition + Vector3.forward,
                            11f,
                            -9f),
                        out error),
                    Is.True,
                    error);
                StagedCheckoutInteractionComponent transientCheckout =
                    Object.FindAnyObjectByType<
                        StagedCheckoutInteractionComponent>();
                Assert.That(
                    transientCheckout.TryPrimary(
                        out _,
                        out CheckoutFailure transientFailure,
                        out error),
                    Is.True,
                    error);
                Assert.That(transientFailure, Is.EqualTo(CheckoutFailure.None));
                Assert.That(
                    transientCheckout.TryPrimary(
                        out _,
                        out transientFailure,
                        out error),
                    Is.True,
                    error);
                Assert.That(context.Checkout.HasActiveIncompleteSession, Is.True);

                Assert.That(
                    context.Disk.TryLoadFromPath(path),
                    Is.True,
                    context.Disk.LastDiagnostic);
                Assert.That(context.Adapter.ActiveLocationId,
                    Is.EqualTo(RiverbendLocationId));
                Assert.That(context.Checkout.HasActiveIncompleteSession, Is.False);
                Assert.That(context.Locations.ActiveBuilding.LastSignature,
                    Is.EqualTo(signature));
                Assert.That(context.EmployeeWork.DetailedLocationId,
                    Is.EqualTo(RiverbendLocationId));
                Assert.That(context.Adapter.HasActiveGeneratedNavigation,
                    Is.True);
                Assert.That(
                    context.Persistence.TryCapture(
                        out FirstStoreSnapshot restoredDetailed,
                        out error),
                    Is.True,
                    error);
                FirstStoreSnapshot expectedDetailed =
                    accepted.generatedLocation.detailedStore;
                Assert.That(restoredDetailed.customerFlow.customers.Count,
                    Is.EqualTo(expectedDetailed.customerFlow.customers.Count));
                for (int index = 0;
                     index < expectedDetailed.customerFlow.customers.Count;
                     index++)
                {
                    StoreCustomerSnapshot expectedCustomer =
                        expectedDetailed.customerFlow.customers[index];
                    StoreCustomerSnapshot restoredCustomer =
                        restoredDetailed.customerFlow.customers[index];
                    Assert.That(restoredCustomer.customerId,
                        Is.EqualTo(expectedCustomer.customerId));
                    Assert.That(restoredCustomer.state,
                        Is.EqualTo(expectedCustomer.state));
                    Assert.That(restoredCustomer.requestedProductIds,
                        Is.EqualTo(expectedCustomer.requestedProductIds));
                    Assert.That(restoredCustomer.reservedPhysicalUnitIds,
                        Is.EqualTo(expectedCustomer.reservedPhysicalUnitIds));
                    AssertInsideSelectedUnit(
                        context.Locations.ActiveBuilding,
                        new Vector3(
                            restoredCustomer.positionX,
                            restoredCustomer.positionY,
                            restoredCustomer.positionZ));
                    expectedCustomer.positionX = restoredCustomer.positionX;
                    expectedCustomer.positionY = restoredCustomer.positionY;
                    expectedCustomer.positionZ = restoredCustomer.positionZ;
                }
                AssertJsonEqual(
                    expectedDetailed,
                    restoredDetailed,
                    "Generated detailed snapshot");
                FirstStorePlayerTransformSnapshot restoredPose =
                    context.Player.CaptureTransformSnapshot();
                Assert.That(
                    Vector3.Distance(
                        restoredPose.worldPosition,
                        savedPose.worldPosition),
                    Is.LessThan(0.001f));
                Assert.That(restoredPose.bodyYawDegrees,
                    Is.EqualTo(savedPose.bodyYawDegrees).Within(0.001f));
                Assert.That(restoredPose.cameraPitchDegrees,
                    Is.EqualTo(savedPose.cameraPitchDegrees).Within(0.001f));
                PortfolioProgressionSnapshot restoredPortfolio =
                    context.Portfolio.Progression.CreateSnapshot();
                Location(accepted.portfolio, RiverbendLocationId)
                    .detailedReconciliation = PortfolioOperationsRules.Clone(
                    Location(restoredPortfolio, RiverbendLocationId)
                        .detailedReconciliation);
                AssertJsonEqual(
                    accepted.portfolio,
                    restoredPortfolio,
                    "Restored generated-location portfolio");

                context.CustomerFlow.enabled = false;
                context.CustomerFlow.ResetTransientStateForRestore();
                context.CustomerFlow.enabled = true;
                SuppressAutomaticArrivalForControlledSale(context);
                CheckoutTransactionSummary saleAfterLoad = null;
                yield return CompleteLiveCustomerSale(
                    context,
                    employeeCheckout: false,
                    value => saleAfterLoad = value);
                Assert.That(
                    context.Adapter.TrySynchronizeActiveLocation(out error),
                    Is.True,
                    error);
                PortfolioProgressionSnapshot afterSale =
                    context.Portfolio.Progression.CreateSnapshot();
                PortfolioLocationSnapshot afterLocation = Location(
                    afterSale,
                    RiverbendLocationId);
                Assert.That(afterSale.cashCents,
                    Is.EqualTo(accepted.portfolio.cashCents +
                               saleAfterLoad.subtotalCents));
                Assert.That(afterLocation.inventoryUnits,
                    Is.EqualTo(savedLocation.inventoryUnits -
                               saleAfterLoad.unitsSold));
                Assert.That(afterLocation.lifetimeGrossSalesCents,
                    Is.EqualTo(savedLocation.lifetimeGrossSalesCents +
                               saleAfterLoad.subtotalCents));
                string once = JsonUtility.ToJson(afterSale);
                Assert.That(
                    context.Adapter.TrySynchronizeActiveLocation(out error),
                    Is.True,
                    error);
                Assert.That(
                    JsonUtility.ToJson(
                        context.Portfolio.Progression.CreateSnapshot()),
                    Is.EqualTo(once));
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [UnityTest]
        public IEnumerator PersistenceSourceMatrixRestoresGeneratedAThenBAndFirstStoreThenManagement()
        {
            string directory = Path.Combine(
                Application.temporaryCachePath,
                $"location-source-matrix-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            string firstPath = Path.Combine(directory, "first.json");
            string managementPath = Path.Combine(directory, "management.json");
            string riverbendPath = Path.Combine(directory, "riverbend.json");
            string downtownPath = Path.Combine(directory, "downtown.json");
            try
            {
                SceneContext context = null;
                yield return LoadContext(value => context = value);
                Assert.That(
                    context.Disk.TrySaveToPath(firstPath),
                    Is.True,
                    context.Disk.LastDiagnostic);
                FirstStoreDiskSaveData first = ReadSave(firstPath);
                Assert.That(first.sourceState,
                    Is.EqualTo(FirstStoreDiskSourceState.FirstStoreDetailed));
                Assert.That(first.generatedLocation, Is.Null);
                Assert.That(first.portfolio.company.activeDetailedLocationId,
                    Is.EqualTo(PortfolioProgressionRules.FirstLocationId));

                PrepareRiverbendPortfolio(context);
                Assert.That(
                    context.Disk.TrySaveToPath(managementPath),
                    Is.True,
                    context.Disk.LastDiagnostic);
                FirstStoreDiskSaveData management = ReadSave(managementPath);
                Assert.That(management.sourceState,
                    Is.EqualTo(FirstStoreDiskSourceState.Management));
                Assert.That(management.generatedLocation, Is.Null);
                Assert.That(
                    management.portfolio.company.activeDetailedLocationId,
                    Is.Null.Or.Empty);
                Assert.That(context.CustomerFlow.enabled, Is.False);
                Assert.That(context.EmployeeWork.enabled, Is.False);

                Assert.That(
                    context.Adapter.TryEnterLocation(
                        RiverbendLocationId,
                        out string error),
                    Is.True,
                    error);
                yield return null;
                string riverbendSignature = context.Locations.ActiveBuilding
                    .LastSignature;
                Assert.That(
                    context.Disk.TrySaveToPath(riverbendPath),
                    Is.True,
                    context.Disk.LastDiagnostic);
                FirstStoreDiskSaveData riverbend = ReadSave(riverbendPath);
                Assert.That(riverbend.sourceState,
                    Is.EqualTo(FirstStoreDiskSourceState.GeneratedDetailed));
                Assert.That(riverbend.generatedLocation.locationId,
                    Is.EqualTo(RiverbendLocationId));

                Assert.That(
                    context.Adapter.TryLeaveToManagement(out error),
                    Is.True,
                    error);
                SeedReconciledHistoricalEarnings(context, 1_000_000);
                Assert.That(
                    context.Portfolio.TryLeaseLocation(
                        DowntownLocationId,
                        out error),
                    Is.True,
                    error);
                ReassignTeam(
                    context.Portfolio.Progression,
                    DowntownLocationId,
                    ExpansionTeam);
                Assert.That(
                    context.Adapter.TryEnterLocation(
                        DowntownLocationId,
                        out error),
                    Is.True,
                    error);
                yield return null;
                string downtownSignature = context.Locations.ActiveBuilding
                    .LastSignature;
                Assert.That(downtownSignature,
                    Is.Not.EqualTo(riverbendSignature));
                Assert.That(
                    context.Disk.TrySaveToPath(downtownPath),
                    Is.True,
                    context.Disk.LastDiagnostic);
                FirstStoreDiskSaveData downtown = ReadSave(downtownPath);
                Assert.That(downtown.sourceState,
                    Is.EqualTo(FirstStoreDiskSourceState.GeneratedDetailed));
                Assert.That(downtown.generatedLocation.locationId,
                    Is.EqualTo(DowntownLocationId));

                Assert.That(
                    context.Disk.TryLoadFromPath(riverbendPath),
                    Is.True,
                    context.Disk.LastDiagnostic);
                Assert.That(context.Adapter.ActiveLocationId,
                    Is.EqualTo(RiverbendLocationId));
                Assert.That(context.Locations.ActiveBuilding.LastSignature,
                    Is.EqualTo(riverbendSignature));
                Assert.That(context.EmployeeWork.DetailedLocationId,
                    Is.EqualTo(RiverbendLocationId));

                Assert.That(
                    context.Disk.TryLoadFromPath(downtownPath),
                    Is.True,
                    context.Disk.LastDiagnostic);
                Assert.That(context.Adapter.ActiveLocationId,
                    Is.EqualTo(DowntownLocationId));
                Assert.That(context.Locations.ActiveBuilding.LastSignature,
                    Is.EqualTo(downtownSignature));
                Assert.That(context.EmployeeWork.DetailedLocationId,
                    Is.EqualTo(DowntownLocationId));

                Assert.That(
                    context.Disk.TryLoadFromPath(firstPath),
                    Is.True,
                    context.Disk.LastDiagnostic);
                Assert.That(context.Adapter.HasActiveGeneratedLocation,
                    Is.False);
                Assert.That(
                    context.Adapter.IsFirstStoreDetailedSimulationActive,
                    Is.True);
                Assert.That(context.Adapter.ActiveLocationId,
                    Is.EqualTo(PortfolioProgressionRules.FirstLocationId));
                Assert.That(context.CustomerFlow.enabled, Is.True);
                Assert.That(context.EmployeeWork.enabled, Is.True);
                Assert.That(context.Portfolio.Progression.Locations.Count,
                    Is.EqualTo(1));

                Assert.That(
                    context.Disk.TryLoadFromPath(managementPath),
                    Is.True,
                    context.Disk.LastDiagnostic);
                Assert.That(context.Adapter.IsDetailedSimulationActive,
                    Is.False);
                Assert.That(context.Adapter.ActiveLocationId, Is.Null);
                Assert.That(context.Player.IsGameplayMode, Is.False);
                Assert.That(context.CustomerFlow.enabled, Is.False);
                Assert.That(context.EmployeeWork.enabled, Is.False);
                Assert.That(context.Portfolio.Progression.Locations.Count,
                    Is.EqualTo(2));
                AssertJsonEqual(
                    management.portfolio,
                    context.Portfolio.Progression.CreateSnapshot(),
                    "Management-source portfolio");
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [UnityTest]
        public IEnumerator GeneratedApplyStageFailureRollsBackDetailedFirstStoreExactly()
        {
            string directory = Path.Combine(
                Application.temporaryCachePath,
                $"location-apply-rollback-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "generated.json");
            try
            {
                SceneContext context = null;
                yield return LoadContext(value => context = value);
                PrepareRiverbendPortfolio(context);
                Assert.That(
                    context.Adapter.TryEnterLocation(
                        RiverbendLocationId,
                        out string error),
                    Is.True,
                    error);
                yield return null;
                Assert.That(
                    context.Disk.TrySaveToPath(path),
                    Is.True,
                    context.Disk.LastDiagnostic);
                Assert.That(
                    context.Adapter.TryLeaveToManagement(out error),
                    Is.True,
                    error);
                Assert.That(
                    context.Adapter.TryEnterLocation(
                        PortfolioProgressionRules.FirstLocationId,
                        out error),
                    Is.True,
                    error);
                yield return null;

                Assert.That(
                    context.Persistence.TryCapture(
                        out FirstStoreSnapshot beforeStore,
                        out error),
                    Is.True,
                    error);
                PortfolioProgressionSnapshot beforePortfolio =
                    context.Portfolio.Progression.CreateSnapshot();
                FirstStorePlayerTransformSnapshot beforePose =
                    context.Player.CaptureTransformSnapshot();
                Assert.That(
                    FirstStoreDiskSaveCodec.TryFromJson(
                        File.ReadAllText(path),
                        out FirstStoreDiskSaveData invalidAtApply,
                        out error),
                    Is.True,
                    error);
                PortfolioCommercialUnitSnapshot generatedUnit =
                    invalidAtApply.portfolio.company.properties
                        .SelectMany(property => property.commercialUnits)
                        .Single(unit => string.Equals(
                            unit.occupyingLocationId,
                            RiverbendLocationId,
                            StringComparison.Ordinal));
                Assert.That(
                    generatedUnit.generatedLayout.canonicalSignature,
                    Is.Not.Empty);
                generatedUnit.generatedLayout.canonicalSignature =
                    string.Equals(
                        generatedUnit.generatedLayout.canonicalSignature,
                        "deadbeef",
                        StringComparison.OrdinalIgnoreCase)
                        ? "cafebabe"
                        : "deadbeef";
                File.WriteAllText(
                    path,
                    FirstStoreDiskSaveCodec.ToJson(invalidAtApply));

                Assert.That(
                    context.Disk.TryLoadFromPath(path),
                    Is.False,
                    "The target must pass snapshot preflight and fail while materializing its contradictory generated layout.");
                StringAssert.Contains(
                    "authoritative layout",
                    context.Disk.LastDiagnostic);
                Assert.That(context.Adapter.HasActiveGeneratedLocation,
                    Is.False);
                Assert.That(
                    context.Adapter.IsFirstStoreDetailedSimulationActive,
                    Is.True);
                Assert.That(context.Adapter.ActiveLocationId,
                    Is.EqualTo(PortfolioProgressionRules.FirstLocationId));
                Assert.That(context.CustomerFlow.enabled, Is.True);
                Assert.That(context.EmployeeWork.enabled, Is.True);
                Assert.That(
                    context.Persistence.TryCapture(
                        out FirstStoreSnapshot afterStore,
                        out error),
                    Is.True,
                    error);
                AssertJsonEqual(
                    beforeStore,
                    afterStore,
                    "First-store rollback after apply-stage failure");
                AssertJsonEqual(
                    beforePortfolio,
                    context.Portfolio.Progression.CreateSnapshot(),
                    "Portfolio rollback after apply-stage failure");
                FirstStorePlayerTransformSnapshot afterPose =
                    context.Player.CaptureTransformSnapshot();
                Assert.That(
                    Vector3.Distance(
                        afterPose.worldPosition,
                        beforePose.worldPosition),
                    Is.LessThan(0.001f));
                Assert.That(afterPose.bodyYawDegrees,
                    Is.EqualTo(beforePose.bodyYawDegrees).Within(0.001f));
                Assert.That(afterPose.cameraPitchDegrees,
                    Is.EqualTo(beforePose.cameraPitchDegrees).Within(0.001f));
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [UnityTest]
        public IEnumerator FailedOverflowMaterializationRollsBackAndRetriesExactlyOnce()
        {
            SceneContext context = null;
            yield return LoadContext(value => context = value);
            PrepareRiverbendPortfolio(context);

            Assert.That(
                context.Portfolio.TryAdvanceDelegatedDay(out string error),
                Is.True,
                error);
            PurchaseOrderSnapshot pendingOrder = context.Portfolio.Progression
                .PurchaseOrders.Single(value =>
                    value.locationId == RiverbendLocationId &&
                    !value.IsTerminal);
            int deliveryCapacity = context.Inventory.Inventory.CreateSnapshot()
                .locations.Single(value =>
                    value.locationId == context.Delivery.InventoryLocationId)
                .capacityUnits;
            Assert.That(
                pendingOrder.lines.All(value =>
                    ConvenienceStoreProcurement.IsDetailedResource(
                        value.resourceId)),
                Is.True,
                "The detailed overflow regression requires product-specific procurement lines.");
            Assert.That(
                pendingOrder.OrderedQuantityUnits,
                Is.GreaterThan(deliveryCapacity),
                "The regression must stage units beyond the physical receiving container.");
            Assert.That(
                context.Portfolio.Progression.TryAdvanceProcurementTicks(
                    ConvenienceStoreProcurement.FulfillmentDelayTicks,
                    out _,
                    out error),
                Is.True,
                error);
            PurchaseOrderSnapshot fulfilledOrder = context.Portfolio.Progression
                .PurchaseOrders.Single(value =>
                    value.orderId == pendingOrder.orderId);
            Assert.That(
                fulfilledOrder.status,
                Is.EqualTo(PurchaseOrderStatus.Fulfilled));

            Assert.That(
                context.Adapter.TryEnterLocation(
                    RiverbendLocationId,
                    out error),
                Is.True,
                error);
            FirstStoreInventorySnapshot inventoryBeforeFailure =
                context.Inventory.Inventory.CreateSnapshot();
            DeliveryContainerSnapshot containerBeforeFailure =
                GetProperty<DeliveryContainer>(
                    context.Delivery,
                    "Container").CreateSnapshot();
            PortfolioProgressionSnapshot portfolioBeforeFailure =
                context.Portfolio.Progression.CreateSnapshot();
            List<PortfolioProductInventorySnapshot> baselineBeforeFailure =
                CapturePortfolioInventoryBaseline(context.Adapter);
            Dictionary<string, int> localBaselineBeforeFailure =
                CaptureLocalInventoryBaseline(context.Adapter);

            Dictionary<string, int> localBaseline = GetField<
                Dictionary<string, int>>(
                context.Adapter,
                "localInventoryBaseline");
            localBaseline[fulfilledOrder.lines[0].resourceId] = 10_000;
            Assert.That(
                context.Portfolio.TrySynchronizeDetailedProcurement(out error),
                Is.False);
            StringAssert.Contains("below its persistent reserve", error);

            Assert.That(
                context.Inventory.Inventory.CreateSnapshot(),
                Is.EqualTo(inventoryBeforeFailure),
                "Failed materialization must restore detailed inventory.");
            Assert.That(
                GetProperty<DeliveryContainer>(
                    context.Delivery,
                    "Container").CreateSnapshot(),
                Is.EqualTo(containerBeforeFailure),
                "Failed materialization must restore the physical container.");
            Assert.That(
                JsonUtility.ToJson(
                    context.Portfolio.Progression.CreateSnapshot()),
                Is.EqualTo(JsonUtility.ToJson(portfolioBeforeFailure)),
                "Failed materialization must restore procurement and financial state.");
            AssertProductInventoryEqual(
                baselineBeforeFailure,
                CapturePortfolioInventoryBaseline(context.Adapter),
                "Failed materialization must restore generated-location overflow state.");

            RestoreLocalInventoryBaseline(
                context.Adapter,
                localBaselineBeforeFailure);
            Assert.That(
                context.Portfolio.TrySynchronizeDetailedProcurement(out error),
                Is.True,
                error);
            PortfolioProgressionSnapshot afterRetry =
                context.Portfolio.Progression.CreateSnapshot();
            PurchaseOrderSnapshot partiallyReceived = afterRetry.procurement
                .orders.Single(value => value.orderId == fulfilledOrder.orderId);
            int physicalDeliveryUnits = RemainingDeliveryUnits(context);
            Assert.That(
                partiallyReceived.status,
                Is.EqualTo(PurchaseOrderStatus.PartiallyReceived));
            Assert.That(
                partiallyReceived.ReceivedQuantityUnits,
                Is.EqualTo(
                    fulfilledOrder.OrderedQuantityUnits -
                    physicalDeliveryUnits));
            Assert.That(physicalDeliveryUnits, Is.EqualTo(deliveryCapacity));

            PortfolioLocationSnapshot beforeLocation = Location(
                portfolioBeforeFailure,
                RiverbendLocationId);
            PortfolioLocationSnapshot afterLocation = Location(
                afterRetry,
                RiverbendLocationId);
            Assert.That(
                afterLocation.inventoryUnits,
                Is.EqualTo(
                    beforeLocation.inventoryUnits +
                    fulfilledOrder.OrderedQuantityUnits));
            foreach (PurchaseOrderLineSnapshot line in fulfilledOrder.lines)
            {
                Assert.That(
                    afterLocation.productInventory.Single(value =>
                        value.productId == line.resourceId).quantityUnits,
                    Is.EqualTo(
                        beforeLocation.productInventory.Single(value =>
                            value.productId == line.resourceId).quantityUnits +
                        line.orderedQuantityUnits));
            }
            Assert.That(afterRetry.cashCents,
                Is.EqualTo(portfolioBeforeFailure.cashCents));
            Assert.That(afterLocation.lifetimeInventoryPurchaseCents,
                Is.EqualTo(beforeLocation.lifetimeInventoryPurchaseCents));
            Assert.That(afterLocation.lifetimeDeliveryFeesCents,
                Is.EqualTo(beforeLocation.lifetimeDeliveryFeesCents));
            Assert.That(afterLocation.lifetimeCashChangeCents,
                Is.EqualTo(beforeLocation.lifetimeCashChangeCents));
            Assert.That(afterLocation.lifetimeOperatingProfitCents,
                Is.EqualTo(beforeLocation.lifetimeOperatingProfitCents));

            string successfulState = JsonUtility.ToJson(afterRetry);
            Assert.That(
                context.Portfolio.TrySynchronizeDetailedProcurement(out error),
                Is.True,
                error);
            Assert.That(
                JsonUtility.ToJson(
                    context.Portfolio.Progression.CreateSnapshot()),
                Is.EqualTo(successfulState),
                "Repeating detailed procurement sync must not duplicate inventory, receipts, or money.");
        }

        [UnityTest]
        public IEnumerator PointThreeMeterAgentTraversesAuthoredAndGeneratedStores()
        {
            SceneContext context = null;
            yield return LoadContext(value => context = value);

            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
            Assert.That(settings.agentTypeID, Is.EqualTo(0));
            Assert.That(
                settings.agentRadius,
                Is.EqualTo(0.3f).Within(0.0001f),
                "The default Humanoid NavMesh bake must match the established detailed actor radius.");
            StoreCustomerFlowLocationBindings customerBindings =
                context.CustomerFlow.CaptureLocationBindings();
            InStoreEmployeeLocationBindings employeeBindings =
                context.EmployeeWork.CaptureLocationBindings();
            Transform exteriorArrival = GameObject.Find(
                "Customer Exterior Arrival Boundary")?.transform;
            Assert.That(exteriorArrival, Is.Not.Null);
            PlaceableFixtureComponent checkoutFixture = customerBindings
                .CheckoutCustomerPoint
                .GetComponentInParent<PlaceableFixtureComponent>();
            Assert.That(checkoutFixture, Is.Not.Null);
            Vector3 customerLocal = checkoutFixture.transform
                .InverseTransformPoint(
                    customerBindings.CheckoutCustomerPoint.position);
            Vector3 cashierLocal = checkoutFixture.transform
                .InverseTransformPoint(employeeBindings.CashierWorkPoint.position);
            Assert.That(
                customerLocal.z,
                Is.LessThan(-0.5f),
                "The authored checkout customer point must stay on the storefront/customer side.");
            Assert.That(
                cashierLocal.z,
                Is.GreaterThan(0.5f),
                "The cashier workplace must stay behind the authored checkout.");
            Assert.That(
                customerLocal.z * cashierLocal.z,
                Is.LessThan(0f),
                "Customer and employee approach points must be on opposite checkout sides.");
            AssertCompleteNavigationRoutes(
                exteriorArrival,
                new[]
                    {
                        customerBindings.EntrancePoint,
                        customerBindings.CheckoutCustomerPoint,
                        employeeBindings.CashierWorkPoint,
                        employeeBindings.DeliveryWorkPoint,
                        employeeBindings.DeliveryDropPoint,
                        employeeBindings.ShelfWorkPoint,
                        employeeBindings.ManagerWorkPoint,
                        customerBindings.ExitPoint
                    }
                    .Concat(customerBindings.BrowsePoints)
                    .Concat(customerBindings.QueuePoints),
                "authored first store");
            AssertConfiguredDetailedAgentRadii(settings.agentRadius);

            PrepareRiverbendPortfolio(context);
            Assert.That(
                context.Adapter.TryEnterLocation(
                    RiverbendLocationId,
                    out string error),
                Is.True,
                error);
            yield return null;

            Assert.That(
                context.Adapter.TryValidateActiveNavigation(out error),
                Is.True,
                error);
            GeneratedDetailedLocationBindings generated =
                context.Adapter.ActiveBindings;
            Assert.That(generated, Is.Not.Null);
            GeneratedOpening entrance = context.Locations.ActiveBuilding
                .LastResult.Openings.Single(value =>
                    value.Kind == ProceduralOpeningKind.PrimaryEntrance);
            Assert.That(
                entrance.WidthMeters,
                Is.GreaterThan(settings.agentRadius * 2f),
                "The approved 3 ft opening must provide physical width for the configured detailed actor.");
            AssertCompleteNavigationRoutes(
                generated.EntrancePoint,
                generated.RequiredNavigationPoints,
                "generated store");
            AssertConfiguredDetailedAgentRadii(settings.agentRadius);
        }

        private static IEnumerator LoadContext(Action<SceneContext> assign)
        {
            yield return SceneManager.LoadSceneAsync(
                "FirstStoreValidation",
                LoadSceneMode.Single);
            yield return null;

            SceneContext context = new()
            {
                Portfolio = Object.FindAnyObjectByType<
                    PortfolioProgressionController>(),
                Disk = Object.FindAnyObjectByType<
                    FirstStoreDiskPersistenceController>(),
                Adapter = Object.FindAnyObjectByType<
                    PersistentPortfolioLocationSceneAdapter>(),
                Locations = Object.FindAnyObjectByType<
                    PersistentPortfolioLocationController>(),
                Persistence = Object.FindAnyObjectByType<
                    FirstStorePersistenceMapperComponent>(),
                Store = Object.FindAnyObjectByType<StoreOperatingController>(),
                Checkout = Object.FindAnyObjectByType<CheckoutStationComponent>(),
                Inventory = Object.FindAnyObjectByType<
                    FirstStoreInventoryComponent>(),
                PhysicalUnits = Object.FindAnyObjectByType<
                    PhysicalProductUnitRegistry>(),
                Stocking = Object.FindAnyObjectByType<StockingController>(),
                Merchandising = Object.FindAnyObjectByType<
                    FirstStoreMerchandisingComponent>(),
                CustomerFlow = Object.FindAnyObjectByType<
                    StoreCustomerFlowController>(),
                EmployeeWork = Object.FindAnyObjectByType<
                    InStoreEmployeeWorkController>(),
                Delivery = Object.FindAnyObjectByType<DeliveryBoxComponent>(),
                Cleaning = Object.FindAnyObjectByType<CleaningTaskComponent>(),
                CleaningTarget = Object.FindAnyObjectByType<
                    CleaningWorldInteractionTarget>(),
                CleaningTool = Object.FindObjectsByType<CarryableToolComponent>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .Single(value => value.CapabilityId == "clean-floor"),
                OperatingControl = Object.FindAnyObjectByType<
                    StoreOperatingWorldInteractionTarget>(),
                Player = Object.FindAnyObjectByType<FirstPersonController>(),
                Products = Resources.FindObjectsOfTypeAll<ProductDefinition>()
                    .Where(value => value != null &&
                                    (value.StableProductId ==
                                         ConvenienceStoreProcurement.ColaProductId ||
                                     value.StableProductId ==
                                         ConvenienceStoreProcurement.ChipsProductId))
                    .OrderBy(value => value.StableProductId, StringComparer.Ordinal)
                    .ToArray()
            };
            Assert.That(context.Portfolio, Is.Not.Null);
            Assert.That(context.Disk, Is.Not.Null);
            Assert.That(context.Adapter, Is.Not.Null);
            Assert.That(context.Locations, Is.Not.Null);
            Assert.That(context.Persistence, Is.Not.Null);
            Assert.That(context.Store, Is.Not.Null);
            Assert.That(context.Checkout, Is.Not.Null);
            Assert.That(context.Inventory, Is.Not.Null);
            Assert.That(context.PhysicalUnits, Is.Not.Null);
            Assert.That(context.Stocking, Is.Not.Null);
            Assert.That(context.Merchandising, Is.Not.Null);
            Assert.That(context.CustomerFlow, Is.Not.Null);
            Assert.That(context.EmployeeWork, Is.Not.Null);
            Assert.That(context.Delivery, Is.Not.Null);
            Assert.That(context.Cleaning, Is.Not.Null);
            Assert.That(context.CleaningTarget, Is.Not.Null);
            Assert.That(context.CleaningTool, Is.Not.Null);
            Assert.That(context.OperatingControl, Is.Not.Null);
            Assert.That(context.Player, Is.Not.Null);
            Assert.That(context.Products, Has.Length.EqualTo(2));
            Assert.That(
                context.Adapter.TryValidateConfiguration(out string error),
                Is.True,
                error);
            SetField(context.CustomerFlow, "arrivalIntervalSeconds", 1_000f);
            assign(context);
        }

        private static void PrepareRiverbendPortfolio(SceneContext context)
        {
            CompletePhysicalFirstShift(context.Portfolio, context.Store);
            context.Player.SetGameplayMode(false);
            Assert.That(
                context.Portfolio.TryLeaveVisitedLocation(out string error),
                Is.True,
                error);
            HireTeam(
                context.Portfolio,
                PortfolioProgressionRules.FirstLocationId,
                FirstTeam);
            Assert.That(
                context.Portfolio.TryAdvanceDelegatedDay(out error),
                Is.True,
                error);
            Assert.That(
                context.Portfolio.TryLeaseLocation(
                    RiverbendLocationId,
                    out error),
                Is.True,
                error);
            HireTeam(context.Portfolio, RiverbendLocationId, ExpansionTeam);
        }

        private static void SuppressAutomaticArrivalForControlledSale(
            SceneContext context)
        {
            SetField(context.CustomerFlow, "secondsUntilNextArrival", 1_000f);
        }

        private static void SeedReconciledHistoricalEarnings(
            SceneContext context,
            long grossSalesCents)
        {
            PortfolioProgressionSnapshot snapshot =
                context.Portfolio.Progression.CreateSnapshot();
            PortfolioLocationSnapshot firstStore = Location(
                snapshot,
                PortfolioProgressionRules.FirstLocationId);
            firstStore.lifetimeGrossSalesCents = checked(
                firstStore.lifetimeGrossSalesCents + grossSalesCents);
            firstStore.lifetimeOperatingProfitCents = checked(
                firstStore.lifetimeOperatingProfitCents + grossSalesCents);
            firstStore.lifetimeCashChangeCents = checked(
                firstStore.lifetimeCashChangeCents + grossSalesCents);
            snapshot.cashCents = checked(snapshot.cashCents + grossSalesCents);
            Assert.That(
                context.Portfolio.TryRestoreSnapshot(
                    snapshot,
                    out string error),
                Is.True,
                error);
        }

        private static void AssertGeneratedDetailedAuthorities(
            SceneContext context,
            string locationId,
            CommercialBuildingArchetype archetype)
        {
            ProceduralCommercialBuilding building = context.Locations
                .ActiveBuilding;
            Assert.That(context.Adapter.ActiveLocationId, Is.EqualTo(locationId));
            Assert.That(building, Is.Not.Null);
            Assert.That(building.LastResult.Archetype, Is.EqualTo(archetype));
            Assert.That(context.Adapter.ActiveBindings, Is.Not.Null);
            Assert.That(context.Adapter.HasActiveGeneratedNavigation, Is.True);
            Assert.That(
                context.Adapter.TryValidateActiveNavigation(out string error),
                Is.True,
                error);
            Assert.That(context.CustomerFlow.enabled, Is.True);
            Assert.That(context.EmployeeWork.enabled, Is.True);
            Assert.That(context.EmployeeWork.DetailedLocationId,
                Is.EqualTo(locationId));
            Assert.That(
                context.CustomerFlow.TryValidateConfiguration(out error),
                Is.True,
                error);
            Assert.That(
                context.EmployeeWork.TryValidateConfiguration(out error),
                Is.True,
                error);
            Assert.That(context.Merchandising.LocationId, Is.EqualTo(locationId));
            Assert.That(
                context.Portfolio.Progression.CreateSnapshot().company
                    .activeDetailedLocationId,
                Is.EqualTo(locationId));

            PlaceableFixtureComponent checkoutFixture = Resources
                .FindObjectsOfTypeAll<PlaceableFixtureComponent>()
                .Single(value => value.StableFixtureInstanceId ==
                                 "fixture-checkout-essential-01");
            AssertInsideSelectedUnit(building, checkoutFixture.transform.position);
            Assert.That(
                checkoutFixture.GetComponent<CustomerCheckoutWorldInteractionTarget>(),
                Is.Not.Null);
            foreach (ShelfFixture shelf in context.Stocking.AuthoredProductMappings
                         .Where(value => value?.ShelfFixture != null)
                         .Select(value => value.ShelfFixture)
                         .Distinct())
            {
                AssertInsideSelectedUnit(building, shelf.transform.position);
                Assert.That(
                    shelf.GetComponent<ShelfFixtureWorldInteractionTarget>(),
                    Is.Not.Null);
                NavMeshObstacle obstacle = shelf.GetComponent<NavMeshObstacle>();
                Assert.That(obstacle, Is.Not.Null);
                Assert.That(obstacle.carving, Is.True);
            }
            AssertInsideSelectedUnit(building, context.Delivery.transform.position);
            AssertInsideSelectedUnit(
                building,
                context.CleaningTarget.transform.position);
            AssertInsideSelectedUnit(
                building,
                context.CleaningTool.transform.position);
            AssertInsideSelectedUnit(
                building,
                context.OperatingControl.transform.position);
            foreach (PhysicalProductUnitConfiguration configuration in
                     context.PhysicalUnits.ProductConfigurations)
            {
                AssertInsideSelectedUnit(
                    building,
                    configuration.LooseSpawnPoint.position);
            }
            Assert.That(
                context.Delivery.GetComponentsInChildren<
                    DeliveryProductWorldInteractionTarget>(true),
                Has.Length.EqualTo(context.Products.Length));
            Assert.That(context.Adapter.ActiveBindings.BrowsePoints.Count,
                Is.EqualTo(2));
            Assert.That(context.Adapter.ActiveBindings.QueuePoints.Count,
                Is.GreaterThanOrEqualTo(2));
            AssertPlayerInsideSelectedUnit(context.Player, building);
        }

        private static IEnumerator CompleteLiveCustomerSale(
            SceneContext context,
            bool employeeCheckout,
            Action<CheckoutTransactionSummary> completed)
        {
            int transactionsBefore = context.Checkout.CompletedTransactionCount;
            if (!employeeCheckout)
            {
                context.EmployeeWork.enabled = false;
            }
            Assert.That(
                context.CustomerFlow.TryAdmitCustomerNow(
                    out string customerId,
                    out string error),
                Is.True,
                error);
            Assert.That(
                context.CustomerFlow.TryGetCustomerNavigationAgent(
                    customerId,
                    out LocalNavigationAgent navigation),
                Is.True);
            yield return WaitUntil(
                () => navigation != null && navigation.Agent.isOnNavMesh &&
                      navigation.RepathCount > 0,
                5f,
                "The generated customer never acquired a NavMesh path.");
            Assert.That(navigation.State,
                Is.Not.EqualTo(LocalNavigationState.PathUnavailable));

            if (employeeCheckout)
            {
                yield return WaitUntil(
                    () => context.Checkout.CompletedTransactionCount >
                          transactionsBefore,
                    30f,
                    "The assigned cashier did not complete the live generated-store checkout.");
            }
            else
            {
                yield return WaitUntil(
                    () => context.CustomerFlow.CanStartCheckout,
                    25f,
                    "The generated customer did not reach the physical checkout queue.");
                Assert.That(
                    context.CustomerFlow.TryStartCheckout(out error),
                    Is.True,
                    error);
                foreach (string physicalUnitId in context.CustomerFlow
                             .ActiveCheckoutPhysicalUnitIds.ToArray())
                {
                    Assert.That(
                        context.CustomerFlow.TryScanCustomerItem(
                            physicalUnitId,
                            out error),
                        Is.True,
                        error);
                }
                Assert.That(
                    context.CustomerFlow.TryCompleteCheckout(out error),
                    Is.True,
                    error);
            }

            Assert.That(context.Checkout.CompletedTransactionCount,
                Is.EqualTo(transactionsBefore + 1));
            CheckoutTransactionSummary summary = context.Checkout
                .CompletedTransactions.Last();
            Assert.That(summary.isCompleted, Is.True);
            Assert.That(summary.unitsSold, Is.GreaterThan(0));
            yield return WaitUntil(
                () => !context.CustomerFlow.HasCustomersInStore,
                20f,
                "Served generated customers did not navigate back to the exit.");
            if (!employeeCheckout)
            {
                context.EmployeeWork.enabled = true;
            }
            completed(summary);
        }

        private static IEnumerator WaitUntil(
            Func<bool> condition,
            float timeoutSeconds,
            string failure)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(condition(), Is.True, failure);
        }

        private static int RemainingDeliveryUnits(SceneContext context)
        {
            int result = 0;
            foreach (ProductDefinition product in context.Products)
            {
                Assert.That(
                    context.Delivery.TryGetConfiguredProductRemaining(
                        product,
                        out _,
                        out int remaining,
                        out string error),
                    Is.True,
                    error);
                result += remaining;
            }
            return result;
        }

        private static void DrainDeliveryToGeneratedLooseStock(
            SceneContext context)
        {
            foreach (ProductDefinition product in context.Products)
            {
                Assert.That(
                    context.Delivery.TryGetConfiguredProductRemaining(
                        product,
                        out _,
                        out int remaining,
                        out string error),
                    Is.True,
                    error);
                for (int index = 0; index < remaining; index++)
                {
                    Assert.That(
                        context.Delivery.TryRemoveOneUnit(
                            product,
                            out ProductItem loose,
                            out DeliveryContainerFailure failure,
                            out _,
                            out error),
                        Is.True,
                        $"{error} ({failure})");
                    Assert.That(loose, Is.Not.Null);
                    AssertInsideSelectedUnit(
                        context.Locations.ActiveBuilding,
                        loose.transform.position);
                }
            }
        }

        private static void ReassignTeam(
            PortfolioProgression progression,
            string locationId,
            IEnumerable<string> employeeIds)
        {
            foreach (string employeeId in employeeIds)
            {
                Assert.That(
                    progression.TryReassignEmployee(
                        employeeId,
                        locationId,
                        out string error),
                    Is.True,
                    error);
            }
        }

        private static PortfolioLocationSnapshot Location(
            PortfolioProgressionSnapshot snapshot,
            string locationId)
        {
            return snapshot.locations.Single(value =>
                value.locationId == locationId);
        }

        private static FirstStoreDiskSaveData ReadSave(string path)
        {
            Assert.That(
                FirstStoreDiskSaveCodec.TryFromJson(
                    File.ReadAllText(path),
                    out FirstStoreDiskSaveData saveData,
                    out string error),
                Is.True,
                error);
            return saveData;
        }

        private static void AssertLocationBusinessStateEqual(
            PortfolioLocationSnapshot expected,
            PortfolioLocationSnapshot actual,
            string message)
        {
            Assert.That(actual.inventoryUnits,
                Is.EqualTo(expected.inventoryUnits),
                message);
            Assert.That(actual.lifetimeGrossSalesCents,
                Is.EqualTo(expected.lifetimeGrossSalesCents),
                message);
            Assert.That(actual.lifetimeCostOfGoodsSoldCents,
                Is.EqualTo(expected.lifetimeCostOfGoodsSoldCents),
                message);
            Assert.That(
                actual.productInventory
                    .OrderBy(value => value.productId, StringComparer.Ordinal)
                    .Select(value => (value.productId, value.quantityUnits)),
                Is.EqualTo(expected.productInventory
                    .OrderBy(value => value.productId, StringComparer.Ordinal)
                    .Select(value => (value.productId, value.quantityUnits))),
                message);
        }

        private static List<PortfolioProductInventorySnapshot>
            CapturePortfolioInventoryBaseline(
                PersistentPortfolioLocationSceneAdapter adapter)
        {
            return GetField<List<PortfolioProductInventorySnapshot>>(
                    adapter,
                    "portfolioInventoryBaseline")
                .Select(PortfolioOperationsRules.Clone)
                .ToList();
        }

        private static Dictionary<string, int> CaptureLocalInventoryBaseline(
            PersistentPortfolioLocationSceneAdapter adapter)
        {
            return new Dictionary<string, int>(
                GetField<Dictionary<string, int>>(
                    adapter,
                    "localInventoryBaseline"),
                StringComparer.Ordinal);
        }

        private static void RestoreLocalInventoryBaseline(
            PersistentPortfolioLocationSceneAdapter adapter,
            IReadOnlyDictionary<string, int> snapshot)
        {
            Dictionary<string, int> baseline = GetField<
                Dictionary<string, int>>(
                adapter,
                "localInventoryBaseline");
            baseline.Clear();
            foreach (KeyValuePair<string, int> pair in snapshot)
            {
                baseline.Add(pair.Key, pair.Value);
            }
        }

        private static void AssertProductInventoryEqual(
            IEnumerable<PortfolioProductInventorySnapshot> expected,
            IEnumerable<PortfolioProductInventorySnapshot> actual,
            string message)
        {
            Assert.That(
                actual
                    .OrderBy(value => value.productId, StringComparer.Ordinal)
                    .Select(value =>
                        (value.productId,
                         value.quantityUnits,
                         value.unitCostCents)),
                Is.EqualTo(expected
                    .OrderBy(value => value.productId, StringComparer.Ordinal)
                    .Select(value =>
                        (value.productId,
                         value.quantityUnits,
                         value.unitCostCents))),
                message);
        }

        private static void AssertJsonEqual(
            object expected,
            object actual,
            string label)
        {
            string expectedJson = JsonUtility.ToJson(expected);
            string actualJson = JsonUtility.ToJson(actual);
            if (string.Equals(
                    expectedJson,
                    actualJson,
                    StringComparison.Ordinal))
            {
                return;
            }

            int limit = Math.Min(expectedJson.Length, actualJson.Length);
            int index = 0;
            while (index < limit && expectedJson[index] == actualJson[index])
            {
                index++;
            }
            int start = Math.Max(0, index - 90);
            int expectedLength = Math.Min(220, expectedJson.Length - start);
            int actualLength = Math.Min(220, actualJson.Length - start);
            Assert.Fail(
                $"{label} differs at JSON character {index}. " +
                $"Expected: {expectedJson.Substring(start, expectedLength)} " +
                $"Actual: {actualJson.Substring(start, actualLength)}");
        }

        private static void AssertCompleteNavigationRoutes(
            Transform origin,
            IEnumerable<Transform> targets,
            string locationLabel)
        {
            Assert.That(origin, Is.Not.Null, locationLabel);
            Assert.That(
                NavMesh.SamplePosition(
                    origin.position,
                    out NavMeshHit originHit,
                    1.5f,
                    NavMesh.AllAreas),
                Is.True,
                $"{locationLabel} origin has no NavMesh.");
            foreach (Transform target in targets
                         .Where(value => value != null)
                         .Distinct())
            {
                Assert.That(
                    NavMesh.SamplePosition(
                        target.position,
                        out NavMeshHit targetHit,
                        1.5f,
                        NavMesh.AllAreas),
                    Is.True,
                    $"{locationLabel} target '{target.name}' has no NavMesh.");
                NavMeshPath path = new();
                Assert.That(
                    NavMesh.CalculatePath(
                        originHit.position,
                        targetHit.position,
                        NavMesh.AllAreas,
                        path),
                    Is.True,
                    $"{locationLabel} target '{target.name}' rejected path calculation.");
                Assert.That(
                    path.status,
                    Is.EqualTo(NavMeshPathStatus.PathComplete),
                    $"{locationLabel} target '{target.name}' is not traversable.");
            }
        }

        private static void AssertConfiguredDetailedAgentRadii(
            float expectedRadius)
        {
            LocalNavigationAgent[] agents = Object
                .FindObjectsByType<LocalNavigationAgent>(
                    FindObjectsInactive.Include)
                .Where(value => value.name.StartsWith(
                    "Detailed ",
                    StringComparison.Ordinal))
                .ToArray();
            Assert.That(agents, Has.Length.GreaterThanOrEqualTo(3));
            foreach (LocalNavigationAgent agent in agents)
            {
                Assert.That(
                    agent.Agent.radius,
                    Is.EqualTo(expectedRadius).Within(0.0001f),
                    agent.name);
            }
        }

        private static void AssertPlayerInsideSelectedUnit(
            FirstPersonController player,
            ProceduralCommercialBuilding building)
        {
            AssertInsideSelectedUnit(building, player.transform.position);
        }

        private static void AssertInsideSelectedUnit(
            ProceduralCommercialBuilding building,
            Vector3 worldPosition)
        {
            Vector3 local = building.transform.InverseTransformPoint(
                worldPosition);
            Assert.That(
                building.LastResult.Units[0].BoundsMeters.Contains(
                    new Vector2(local.x, local.z),
                    0.05f),
                Is.True,
                $"Expected ({local.x:0.00}, {local.z:0.00}) inside the selected persistent unit.");
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
                                 ConvenienceStoreProcurement.ColaProductId);
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
            IEnumerable<string> employeeIds)
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

        private static void SetField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(target);
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, propertyName);
            return (T)property.GetValue(target);
        }
    }
}
