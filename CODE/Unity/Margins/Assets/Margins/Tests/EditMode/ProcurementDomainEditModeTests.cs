using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Margins.Tests.EditMode
{
    public sealed class ProcurementDomainEditModeTests
    {
        private static readonly StoreSessionTotals FirstShiftTotals = new(
            grossSalesCents: 348,
            costOfGoodsSoldCents: 150,
            includedOperatingExpensesCents: 90,
            contributionAfterCostOfGoodsCents: 108,
            unitsSold: 2,
            transactionCount: 1);

        [Test]
        public void LifecycleCreatesStableExactlyOnceEventsAndRoundTripsEveryReceivingStatus()
        {
            ProcurementLedger ledger = ProcurementLedger.CreateInitial(
                ConvenienceStoreProcurement.Catalog);

            Assert.That(
                ledger.TryPlaceOrder(
                    "location-test-market",
                    ConvenienceStoreProcurement.SupplierId,
                    ConvenienceStoreProcurement.DetailedCase,
                    out PurchaseOrderSnapshot placed,
                    out long chargeCents,
                    out string error),
                Is.True,
                error);
            Assert.That(placed.orderId, Is.EqualTo("purchase-order-000001"));
            Assert.That(placed.paymentId, Is.EqualTo("payment-purchase-order-000001"));
            Assert.That(placed.status, Is.EqualTo(PurchaseOrderStatus.Pending));
            Assert.That(placed.placedAtTick, Is.Zero);
            Assert.That(placed.fulfillAtTick, Is.EqualTo(2));
            Assert.That(placed.subtotalCostCents, Is.EqualTo(600));
            Assert.That(placed.deliveryFeeCents, Is.EqualTo(6_500));
            Assert.That(chargeCents, Is.EqualTo(7_100));
            Assert.That(
                placed.lines.Select(line => line.resourceId),
                Is.EqualTo(new[]
                {
                    ConvenienceStoreProcurement.ColaProductId,
                    ConvenienceStoreProcurement.ChipsProductId
                }));
            AssertStatusSurvivesJson(ledger.CreateSnapshot(), PurchaseOrderStatus.Pending);

            PurchaseOrderSnapshot exposed = ledger.Orders.Single();
            exposed.status = PurchaseOrderStatus.Canceled;
            Assert.That(
                ledger.Orders.Single().status,
                Is.EqualTo(PurchaseOrderStatus.Pending),
                "Read access must not bypass the procurement authority.");

            Assert.That(
                ledger.TryPlaceOrder(
                    "location-test-market",
                    ConvenienceStoreProcurement.SupplierId,
                    ConvenienceStoreProcurement.DetailedCase,
                    out _,
                    out _,
                    out error),
                Is.False);
            Assert.That(ledger.Orders.Count, Is.EqualTo(1));

            Assert.That(ledger.TryAdvanceTicks(1, out int fulfilled, out error), Is.True, error);
            Assert.That(fulfilled, Is.Zero);
            Assert.That(ledger.Orders.Single().status, Is.EqualTo(PurchaseOrderStatus.Pending));
            Assert.That(ledger.TryAdvanceTicks(1, out fulfilled, out error), Is.True, error);
            Assert.That(fulfilled, Is.EqualTo(1));
            PurchaseOrderSnapshot fulfilledOrder = ledger.Orders.Single();
            Assert.That(fulfilledOrder.status, Is.EqualTo(PurchaseOrderStatus.Fulfilled));
            Assert.That(
                fulfilledOrder.fulfillmentId,
                Is.EqualTo("fulfillment-purchase-order-000001"));
            AssertStatusSurvivesJson(ledger.CreateSnapshot(), PurchaseOrderStatus.Fulfilled);

            Assert.That(ledger.TryAdvanceTicks(3, out fulfilled, out error), Is.True, error);
            Assert.That(fulfilled, Is.Zero);
            Assert.That(
                ledger.Orders.Single().fulfillmentId,
                Is.EqualTo(fulfilledOrder.fulfillmentId));

            Assert.That(
                ledger.TryCreateDelivery(
                    placed.orderId,
                    out PurchaseOrderSnapshot delivered,
                    out bool unchanged,
                    out error),
                Is.True,
                error);
            Assert.That(unchanged, Is.False);
            Assert.That(delivered.status, Is.EqualTo(PurchaseOrderStatus.Delivered));
            Assert.That(delivered.deliveryId, Is.EqualTo("delivery-purchase-order-000001"));
            Assert.That(
                delivered.inventoryReceiptId,
                Is.EqualTo("inventory-receipt-purchase-order-000001"));
            AssertStatusSurvivesJson(ledger.CreateSnapshot(), PurchaseOrderStatus.Delivered);

            Assert.That(
                ledger.TryCreateDelivery(
                    placed.orderId,
                    out PurchaseOrderSnapshot sameDelivery,
                    out unchanged,
                    out error),
                Is.True,
                error);
            Assert.That(unchanged, Is.True);
            Assert.That(sameDelivery.deliveryId, Is.EqualTo(delivered.deliveryId));

            Dictionary<string, int> partialReceipt = Receipt(2, 0);
            Assert.That(
                ledger.TryRecordAbsoluteReceipt(
                    placed.orderId,
                    partialReceipt,
                    out PurchaseOrderSnapshot partial,
                    out unchanged,
                    out error),
                Is.True,
                error);
            Assert.That(unchanged, Is.False);
            Assert.That(partial.status, Is.EqualTo(PurchaseOrderStatus.PartiallyReceived));
            Assert.That(partial.ReceivedQuantityUnits, Is.EqualTo(2));
            AssertStatusSurvivesJson(
                ledger.CreateSnapshot(),
                PurchaseOrderStatus.PartiallyReceived);

            Assert.That(
                ledger.TryRecordAbsoluteReceipt(
                    placed.orderId,
                    partialReceipt,
                    out _,
                    out unchanged,
                    out error),
                Is.True,
                error);
            Assert.That(unchanged, Is.True);

            Assert.That(
                ledger.TryRecordAbsoluteReceipt(
                    placed.orderId,
                    Receipt(1, 0),
                    out _,
                    out _,
                    out error),
                Is.False);
            Assert.That(ledger.Orders.Single().ReceivedQuantityUnits, Is.EqualTo(2));

            Assert.That(
                ledger.TryRecordAbsoluteReceipt(
                    placed.orderId,
                    Receipt(4, 4),
                    out PurchaseOrderSnapshot completed,
                    out unchanged,
                    out error),
                Is.True,
                error);
            Assert.That(unchanged, Is.False);
            Assert.That(completed.status, Is.EqualTo(PurchaseOrderStatus.Completed));
            Assert.That(
                completed.completionReceiptId,
                Is.EqualTo("completion-receipt-purchase-order-000001"));
            AssertStatusSurvivesJson(ledger.CreateSnapshot(), PurchaseOrderStatus.Completed);

            Assert.That(
                ledger.TryRecordAbsoluteReceipt(
                    placed.orderId,
                    Receipt(4, 4),
                    out PurchaseOrderSnapshot sameCompletion,
                    out unchanged,
                    out error),
                Is.True,
                error);
            Assert.That(unchanged, Is.True);
            Assert.That(
                sameCompletion.completionReceiptId,
                Is.EqualTo(completed.completionReceiptId));

            ProcurementSnapshot tamperedEvent = ledger.CreateSnapshot();
            tamperedEvent.orders[0].completionReceiptId = "completion-receipt-tampered";
            Assert.That(
                ProcurementLedger.TryRestore(
                    ConvenienceStoreProcurement.Catalog,
                    tamperedEvent,
                    out _,
                    out error),
                Is.False);
        }

        [Test]
        public void CancellationRefundsOnceAndStableOrderIdsAreNeverReused()
        {
            ProcurementLedger ledger = ProcurementLedger.CreateInitial(
                ConvenienceStoreProcurement.Catalog);
            Assert.That(
                ledger.TryPlaceOrder(
                    "location-test-market",
                    ConvenienceStoreProcurement.SupplierId,
                    ConvenienceStoreProcurement.DetailedCase,
                    out PurchaseOrderSnapshot first,
                    out long charge,
                    out string error),
                Is.True,
                error);

            Assert.That(
                ledger.TryCancelOrder(
                    first.orderId,
                    out long refund,
                    out bool unchanged,
                    out error),
                Is.True,
                error);
            Assert.That(unchanged, Is.False);
            Assert.That(refund, Is.EqualTo(charge));
            Assert.That(
                ledger.Orders.Single().cancellationId,
                Is.EqualTo("cancellation-purchase-order-000001"));
            AssertStatusSurvivesJson(ledger.CreateSnapshot(), PurchaseOrderStatus.Canceled);

            Assert.That(
                ledger.TryCancelOrder(
                    first.orderId,
                    out refund,
                    out unchanged,
                    out error),
                Is.True,
                error);
            Assert.That(unchanged, Is.True);
            Assert.That(refund, Is.Zero);

            Assert.That(
                ledger.TryPlaceOrder(
                    "location-test-market",
                    ConvenienceStoreProcurement.SupplierId,
                    ConvenienceStoreProcurement.DetailedCase,
                    out PurchaseOrderSnapshot second,
                    out _,
                    out error),
                Is.True,
                error);
            Assert.That(second.orderId, Is.EqualTo("purchase-order-000002"));
            Assert.That(ledger.TryAdvanceTicks(2, out _, out error), Is.True, error);
            Assert.That(
                ledger.TryCancelOrder(
                    second.orderId,
                    out _,
                    out _,
                    out error),
                Is.False);
            Assert.That(
                ledger.Orders.Single(order => order.orderId == second.orderId).status,
                Is.EqualTo(PurchaseOrderStatus.Fulfilled));
        }

        [Test]
        public void UnavailableResourcesAndInvalidQuantitiesLeaveLedgerUnchanged()
        {
            ProcurementLedger ledger = ProcurementLedger.CreateInitial(
                ConvenienceStoreProcurement.Catalog);
            ProcurementSnapshot before = ledger.CreateSnapshot();

            Assert.That(
                ledger.TryPlaceOrder(
                    "location-test-market",
                    ConvenienceStoreProcurement.SupplierId,
                    new[] { new ProcurementOrderRequestLine("resource-unavailable", 1) },
                    out _,
                    out _,
                    out string error),
                Is.False);
            StringAssert.Contains("unavailable", error);
            Assert.That(
                ledger.TryPlaceOrder(
                    "location-test-market",
                    ConvenienceStoreProcurement.SupplierId,
                    new[]
                    {
                        new ProcurementOrderRequestLine(
                            ConvenienceStoreProcurement.ColaProductId,
                            0)
                    },
                    out _,
                    out _,
                    out error),
                Is.False);
            Assert.That(ledger.Orders, Is.Empty);
            Assert.That(ledger.CreateSnapshot().nextOrderOrdinal, Is.EqualTo(before.nextOrderOrdinal));
        }

        [Test]
        public void PortfolioPaymentAndCancellationPreserveCashExactlyOnce()
        {
            PortfolioProgression ready = CreateReadyProgression();
            long beforeIncompatibleOrder = ready.CashCents;
            Assert.That(
                ready.TryPlacePurchaseOrder(
                    PortfolioProgressionRules.FirstLocationId,
                    ConvenienceStoreProcurement.SupplierId,
                    new[]
                    {
                        new ProcurementOrderRequestLine(
                            ConvenienceStoreProcurement.AggregateResourceId,
                            1)
                    },
                    out _,
                    out string incompatibleError),
                Is.False);
            StringAssert.Contains("physical product", incompatibleError);
            Assert.That(ready.CashCents, Is.EqualTo(beforeIncompatibleOrder));

            PortfolioProgressionSnapshot constrained = ready.CreateSnapshot();
            const long OrderTotalCents = 7_100;
            constrained.cashCents =
                PortfolioProgressionRules.MinimumCashReserveCents +
                OrderTotalCents - 1;
            Assert.That(
                PortfolioProgression.TryRestore(
                    constrained,
                    out PortfolioProgression progression,
                    out string error),
                Is.True,
                error);
            long beforeFailedOrder = progression.CashCents;
            Assert.That(
                progression.TryPlacePurchaseOrder(
                    PortfolioProgressionRules.FirstLocationId,
                    ConvenienceStoreProcurement.SupplierId,
                    ConvenienceStoreProcurement.DetailedCase,
                    out _,
                    out error),
                Is.False);
            StringAssert.Contains("reserve", error);
            Assert.That(progression.CashCents, Is.EqualTo(beforeFailedOrder));
            Assert.That(progression.PurchaseOrders, Is.Empty);

            constrained.cashCents++;
            Assert.That(
                PortfolioProgression.TryRestore(
                    constrained,
                    out progression,
                    out error),
                Is.True,
                error);
            long beforePayment = progression.CashCents;
            Assert.That(
                progression.TryPlacePurchaseOrder(
                    PortfolioProgressionRules.FirstLocationId,
                    ConvenienceStoreProcurement.SupplierId,
                    ConvenienceStoreProcurement.DetailedCase,
                    out PurchaseOrderSnapshot order,
                    out error),
                Is.True,
                error);
            Assert.That(progression.CashCents, Is.EqualTo(beforePayment - order.totalCostCents));
            Assert.That(
                progression.CashCents,
                Is.EqualTo(PortfolioProgressionRules.MinimumCashReserveCents));

            long afterPayment = progression.CashCents;
            Assert.That(
                progression.TryPlacePurchaseOrder(
                    PortfolioProgressionRules.FirstLocationId,
                    ConvenienceStoreProcurement.SupplierId,
                    ConvenienceStoreProcurement.DetailedCase,
                    out _,
                    out error),
                Is.False);
            Assert.That(progression.CashCents, Is.EqualTo(afterPayment));
            Assert.That(progression.PurchaseOrders.Count, Is.EqualTo(1));

            Assert.That(
                progression.TryCancelPurchaseOrder(
                    order.orderId,
                    out bool unchanged,
                    out error),
                Is.True,
                error);
            Assert.That(unchanged, Is.False);
            Assert.That(progression.CashCents, Is.EqualTo(beforePayment));
            Assert.That(
                progression.TryCancelPurchaseOrder(
                    order.orderId,
                    out unchanged,
                    out error),
                Is.True,
                error);
            Assert.That(unchanged, Is.True);
            Assert.That(progression.CashCents, Is.EqualTo(beforePayment));
        }

        [Test]
        public void DeliveredDetailedInventoryIsNotChargedAgainDuringReconciliation()
        {
            PortfolioProgression progression = CreateReadyProgression();
            PortfolioLocationSnapshot beforeLocation = progression.Locations.Single();
            long beforePurchaseTotal = beforeLocation.lifetimeInventoryPurchaseCents;
            long cashBeforeOrder = progression.CashCents;

            Assert.That(
                progression.TryPlacePurchaseOrder(
                    PortfolioProgressionRules.FirstLocationId,
                    ConvenienceStoreProcurement.SupplierId,
                    ConvenienceStoreProcurement.DetailedCase,
                    out PurchaseOrderSnapshot order,
                    out string error),
                Is.True,
                error);
            long cashAfterOrder = progression.CashCents;
            Assert.That(cashAfterOrder, Is.EqualTo(cashBeforeOrder - order.totalCostCents));
            Assert.That(
                progression.TryAdvanceProcurementTicks(2, out int fulfilled, out error),
                Is.True,
                error);
            Assert.That(fulfilled, Is.EqualTo(1));
            Assert.That(
                progression.TryCreatePurchaseOrderDelivery(
                    order.orderId,
                    out _,
                    out _,
                    out error),
                Is.True,
                error);

            Assert.That(
                progression.TryReconcileDetailedOperation(
                    "session-procurement-test",
                    FirstShiftTotals,
                    remainingInventoryUnits: 15,
                    inventoryAssetValueCents: (7 * 135) + order.subtotalCostCents,
                    out bool unchanged,
                    out error),
                Is.True,
                error);
            Assert.That(unchanged, Is.False);
            Assert.That(progression.CashCents, Is.EqualTo(cashAfterOrder));
            PortfolioLocationSnapshot location = progression.Locations.Single();
            Assert.That(
                location.lifetimeInventoryPurchaseCents,
                Is.EqualTo(beforePurchaseTotal + order.subtotalCostCents));
            Assert.That(location.lifetimeDeliveryFeesCents, Is.EqualTo(order.deliveryFeeCents));
            Assert.That(
                location.lastReport.inventoryPurchaseCents,
                Is.EqualTo(beforePurchaseTotal + order.subtotalCostCents));
            Assert.That(location.lastReport.deliveryFeesCents, Is.EqualTo(order.deliveryFeeCents));

            Assert.That(
                progression.TryRecordPurchaseOrderReceipt(
                    order.orderId,
                    Receipt(4, 4),
                    out PurchaseOrderSnapshot completed,
                    out unchanged,
                    out error),
                Is.True,
                error);
            Assert.That(completed.status, Is.EqualTo(PurchaseOrderStatus.Completed));
            PortfolioProgressionSnapshot saved = progression.CreateSnapshot();
            string json = JsonUtility.ToJson(saved);
            Assert.That(
                PortfolioProgression.TryRestore(
                    JsonUtility.FromJson<PortfolioProgressionSnapshot>(json),
                    out PortfolioProgression restored,
                    out error),
                Is.True,
                error);
            Assert.That(restored.CashCents, Is.EqualTo(cashAfterOrder));
            Assert.That(restored.PurchaseOrders.Single().status, Is.EqualTo(PurchaseOrderStatus.Completed));

            PortfolioProgressionSnapshot tampered =
                JsonUtility.FromJson<PortfolioProgressionSnapshot>(json);
            tampered.locations[0].lifetimeDeliveryFeesCents++;
            Assert.That(
                PortfolioProgression.TryRestore(
                    tampered,
                    out _,
                    out error),
                Is.False);
            StringAssert.Contains("procurement costs", error);
        }

        [Test]
        public void AggregateReordersArePaidThenDeliveredOnALaterDayAndCanRecur()
        {
            PortfolioProgression progression = CreateReadyProgression(staffed: true);
            PortfolioProgressionSnapshot before = progression.CreateSnapshot();

            Assert.That(
                progression.TryAdvanceDelegatedDay(out string error),
                Is.True,
                error);
            PortfolioProgressionSnapshot orderedDay = progression.CreateSnapshot();
            PortfolioLocationSnapshot orderedLocation = orderedDay.locations.Single();
            PurchaseOrderSnapshot firstOrder = orderedDay.procurement.orders.Single();
            Assert.That(firstOrder.status, Is.EqualTo(PurchaseOrderStatus.Pending));
            Assert.That(orderedLocation.lastReport.reorderedUnits, Is.EqualTo(firstOrder.OrderedQuantityUnits));
            Assert.That(
                orderedLocation.inventoryUnits,
                Is.EqualTo(before.locations[0].inventoryUnits - orderedLocation.lastReport.unitsSold));

            string pendingJson = JsonUtility.ToJson(orderedDay);
            Assert.That(
                PortfolioProgression.TryRestore(
                    JsonUtility.FromJson<PortfolioProgressionSnapshot>(pendingJson),
                    out progression,
                    out error),
                Is.True,
                error);
            long cashBeforeDeliveryDay = progression.CashCents;
            long purchasesBeforeDeliveryDay = progression.Locations.Single()
                .lifetimeInventoryPurchaseCents;
            int inventoryBeforeDeliveryDay = progression.Locations.Single().inventoryUnits;
            Assert.That(progression.TryAdvanceDelegatedDay(out error), Is.True, error);

            PortfolioProgressionSnapshot deliveredDay = progression.CreateSnapshot();
            PortfolioLocationSnapshot deliveredLocation = deliveredDay.locations.Single();
            PurchaseOrderSnapshot completedFirst = deliveredDay.procurement.orders.Single(order =>
                order.orderId == firstOrder.orderId);
            Assert.That(completedFirst.status, Is.EqualTo(PurchaseOrderStatus.Completed));
            Assert.That(
                deliveredLocation.inventoryUnits,
                Is.EqualTo(
                    inventoryBeforeDeliveryDay +
                    firstOrder.OrderedQuantityUnits -
                    deliveredLocation.lastReport.unitsSold));
            Assert.That(deliveredLocation.lastReport.inventoryPurchaseCents, Is.Zero);
            Assert.That(deliveredLocation.lastReport.deliveryFeesCents, Is.Zero);
            Assert.That(
                deliveredLocation.lifetimeInventoryPurchaseCents,
                Is.EqualTo(purchasesBeforeDeliveryDay));
            Assert.That(
                progression.CashCents,
                Is.EqualTo(cashBeforeDeliveryDay + deliveredLocation.lastReport.cashChangeCents));

            int safetyDays = 0;
            while (progression.PurchaseOrders.Count < 2 && safetyDays++ < 5)
            {
                Assert.That(progression.TryAdvanceDelegatedDay(out error), Is.True, error);
            }

            Assert.That(progression.PurchaseOrders.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(
                progression.PurchaseOrders.Take(2).Select(order => order.orderId),
                Is.EqualTo(new[] { "purchase-order-000001", "purchase-order-000002" }));
        }

        [Test]
        public void PhysicalInventoryReceiptValidatesTheWholeBatchBeforeAddingUnits()
        {
            FirstStoreInventory inventory = new();
            Assert.That(
                inventory.TryRegisterProduct(
                    ConvenienceStoreProcurement.ColaProductId,
                    out string error),
                Is.True,
                error);
            Assert.That(
                inventory.TryRegisterProduct(
                    ConvenienceStoreProcurement.ChipsProductId,
                    out error),
                Is.True,
                error);
            Assert.That(
                inventory.TryRegisterLocation(
                    "loc-delivery-test",
                    InventoryLocationKind.DeliveryContainer,
                    3,
                    false,
                    out error),
                Is.True,
                error);
            Assert.That(
                inventory.TryRegisterLocation(
                    "loc-shelf-test",
                    InventoryLocationKind.Shelf,
                    3,
                    false,
                    out error),
                Is.True,
                error);

            Assert.That(
                inventory.TryReceiveDelivery(
                    "loc-delivery-test",
                    Receipt(2, 2),
                    out InventoryReceiptFailure failure),
                Is.False);
            Assert.That(failure, Is.EqualTo(InventoryReceiptFailure.DestinationCapacityExceeded));
            Assert.That(
                inventory.GetTotalQuantity(ConvenienceStoreProcurement.ColaProductId),
                Is.Zero);
            Assert.That(
                inventory.GetTotalQuantity(ConvenienceStoreProcurement.ChipsProductId),
                Is.Zero);

            Assert.That(
                inventory.TryReceiveDelivery(
                    "loc-shelf-test",
                    Receipt(1, 1),
                    out failure),
                Is.False);
            Assert.That(failure, Is.EqualTo(InventoryReceiptFailure.DestinationIsNotDeliveryContainer));

            Assert.That(
                inventory.TryReceiveDelivery(
                    "loc-delivery-test",
                    Receipt(2, 1),
                    out failure),
                Is.True);
            Assert.That(failure, Is.EqualTo(InventoryReceiptFailure.None));
            Assert.That(
                inventory.GetQuantity(
                    "loc-delivery-test",
                    ConvenienceStoreProcurement.ColaProductId),
                Is.EqualTo(2));
            Assert.That(
                inventory.GetQuantity(
                    "loc-delivery-test",
                    ConvenienceStoreProcurement.ChipsProductId),
                Is.EqualTo(1));
        }

        private static Dictionary<string, int> Receipt(int cola, int chips)
        {
            return new Dictionary<string, int>
            {
                [ConvenienceStoreProcurement.ColaProductId] = cola,
                [ConvenienceStoreProcurement.ChipsProductId] = chips
            };
        }

        private static void AssertStatusSurvivesJson(
            ProcurementSnapshot snapshot,
            PurchaseOrderStatus expectedStatus)
        {
            string json = JsonUtility.ToJson(snapshot);
            ProcurementSnapshot decoded = JsonUtility.FromJson<ProcurementSnapshot>(json);
            Assert.That(
                ProcurementLedger.TryRestore(
                    ConvenienceStoreProcurement.Catalog,
                    decoded,
                    out ProcurementLedger restored,
                    out string error),
                Is.True,
                error);
            Assert.That(restored.Orders.Single().status, Is.EqualTo(expectedStatus));
        }

        private static PortfolioProgression CreateReadyProgression(bool staffed = false)
        {
            PortfolioProgression progression = PortfolioProgression.CreateInitial();
            Assert.That(
                progression.TryPostDetailedShift(
                    "session-procurement-test",
                    FirstShiftTotals,
                    7,
                    out _,
                    out string error),
                Is.True,
                error);
            if (!staffed)
            {
                return progression;
            }

            Assert.That(
                progression.TryHireCandidate(
                    "employee-elena-ruiz",
                    PortfolioProgressionRules.FirstLocationId,
                    out error),
                Is.True,
                error);
            Assert.That(
                progression.TryHireCandidate(
                    "employee-marcus-reed",
                    PortfolioProgressionRules.FirstLocationId,
                    out error),
                Is.True,
                error);
            Assert.That(
                progression.TryHireCandidate(
                    "employee-priya-shah",
                    PortfolioProgressionRules.FirstLocationId,
                    out error),
                Is.True,
                error);
            return progression;
        }
    }
}
