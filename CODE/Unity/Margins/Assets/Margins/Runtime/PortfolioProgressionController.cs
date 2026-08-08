using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Margins
{
    /// <summary>
    /// Connects the first in-world shift to persistent company management and
    /// presents the same domain used by delegated simulation and disk saves.
    /// </summary>
    public sealed class PortfolioProgressionController : MonoBehaviour
    {
        private static readonly Color Ink =
            new(0.94f, 0.96f, 0.95f, 1f);
        private static readonly Color MutedInk =
            new(0.62f, 0.69f, 0.69f, 1f);
        private static readonly Color NightDeep =
            new(0.012f, 0.022f, 0.03f, 1f);
        private static readonly Color Night =
            new(0.025f, 0.04f, 0.052f, 1f);
        private static readonly Color NightSoft =
            new(0.055f, 0.08f, 0.092f, 1f);
        private static readonly Color NightRaised =
            new(0.082f, 0.11f, 0.12f, 1f);
        private static readonly Color Teal =
            new(0.12f, 0.78f, 0.68f, 1f);
        private static readonly Color Amber =
            new(1f, 0.58f, 0.2f, 1f);
        private static readonly Color Error =
            new(0.95f, 0.3f, 0.23f, 1f);

        private enum DeskPage
        {
            Overview,
            People,
            Locations,
            Reports
        }

        [SerializeField] private FirstPersonController firstPersonController;
        [SerializeField] private StoreOperatingController firstStore;
        [SerializeField] private FirstStoreInventoryComponent firstStoreInventory;
        [SerializeField] private DeliveryBoxComponent firstStoreDeliveryBox;
        [SerializeField, Min(0.25f)] private float procurementTickSeconds = 5f;

        private PortfolioProgression progression;
        private DeskPage page;
        private string selectedLocationId = PortfolioProgressionRules.FirstLocationId;
        private string lastAction = "Complete the hands-on first shift to unlock company management.";
        private bool lastActionSucceeded = true;
        private bool hasOpenedDeskAfterFirstShift;
        private Vector2 peopleScroll;
        private Vector2 reportScroll;
        private float lastActionUntil;
        private float nextProcurementTickAt;
        private GUIStyle humanBrandStyle;
        private GUIStyle humanTitleStyle;
        private GUIStyle humanSectionStyle;
        private GUIStyle humanBodyStyle;
        private GUIStyle humanSmallStyle;
        private GUIStyle humanMetricStyle;
        private GUIStyle humanMetricValueStyle;
        private GUIStyle humanButtonStyle;
        private GUIStyle humanButtonPrimaryStyle;
        private GUIStyle humanCenteredStyle;

        public PortfolioProgression Progression => progression;
        public bool IsInitialized => progression != null;
        public bool OwnsManagementDesk =>
            progression != null &&
            firstPersonController != null &&
            !GamePauseMenuController.IsAnyMenuOpen &&
            !firstPersonController.IsGameplayMode;
        public string LastAction => lastAction;
        public string SelectedLocationId => selectedLocationId;

        private void Awake()
        {
            progression = PortfolioProgression.CreateInitial();
        }

        private void Start()
        {
            if (!TryValidateConfiguration(out string error))
            {
                Debug.LogError(error, this);
                enabled = false;
            }
            nextProcurementTickAt = Time.unscaledTime + procurementTickSeconds;
        }

        private void Update()
        {
            if (!TrySynchronizeLivePayroll(out string payrollError))
            {
                Record(payrollError, false);
            }
            TrySynchronizeDetailedShift(out _);
            if (!TrySynchronizeDetailedProcurement(out string procurementError))
            {
                Record(procurementError, false);
            }
            if (progression != null && progression.FirstShiftCompleted &&
                !GamePauseMenuController.IsAnyMenuOpen &&
                Time.unscaledTime >= nextProcurementTickAt)
            {
                nextProcurementTickAt = Time.unscaledTime + procurementTickSeconds;
                if (!progression.TryAdvanceProcurementTicks(
                        1,
                        out _,
                        out procurementError) ||
                    !TrySynchronizeDetailedProcurement(out procurementError) ||
                    !TryApplyDetailedReorderPolicy(out procurementError))
                {
                    Record(procurementError, false);
                }
            }
            if (!hasOpenedDeskAfterFirstShift &&
                progression != null &&
                progression.FirstShiftCompleted &&
                firstPersonController != null)
            {
                hasOpenedDeskAfterFirstShift = true;
                Record(
                    "Company management is now available from Tab without ending store operations.",
                    true);
            }

            if (!OwnsManagementDesk || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                page = DeskPage.Overview;
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                page = DeskPage.People;
            }
            else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                page = DeskPage.Locations;
            }
            else if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                page = DeskPage.Reports;
            }
        }

        public bool TrySynchronizeLivePayroll(out string error)
        {
            if (progression == null || firstStore == null)
            {
                error = "Company or first-store payroll state is unavailable.";
                return false;
            }

            long payrollCents;
            try
            {
                payrollCents = progression.Employees
                    .Where(employee => string.Equals(
                        employee.assignedLocationId,
                        PortfolioProgressionRules.FirstLocationId,
                        StringComparison.Ordinal))
                    .Sum(employee => employee.dailyWageCents);
            }
            catch (OverflowException)
            {
                error = "Assigned payroll exceeds supported cent storage.";
                return false;
            }

            return firstStore.TrySetLivePayrollCents(payrollCents, out error);
        }

        public bool TryValidateConfiguration(out string error)
        {
            error = null;
            if (firstPersonController == null ||
                firstStore == null ||
                firstStoreInventory == null ||
                firstStoreDeliveryBox == null ||
                procurementTickSeconds <= 0f ||
                !firstStoreInventory.TryInitialize(out error) ||
                !firstStoreDeliveryBox.TryInitialize(out error) ||
                firstStoreDeliveryBox.InventoryComponent != firstStoreInventory)
            {
                error ??= "Portfolio progression requires explicit player, first-store, inventory, and delivery references.";
                return false;
            }

            if (progression == null ||
                !PortfolioProgression.TryValidateSnapshot(
                    progression.CreateSnapshot(),
                    out error))
            {
                error = $"Portfolio progression is not initialized: {error}";
                return false;
            }

            error = null;
            return true;
        }

        public bool TrySynchronizeDetailedShift(out string error)
        {
            if (progression == null || firstStore == null)
            {
                error = "Portfolio progression or first-store state is unavailable.";
                return false;
            }

            PortfolioLocationSnapshot firstLocation = progression.Locations.FirstOrDefault(
                location => string.Equals(
                    location.locationId,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal));
            if (firstLocation != null && firstLocation.delegatedDaysOperating > 0)
            {
                // Once an off-site day has advanced, its aggregate inventory and
                // report are authoritative. Do not repost the still-loaded detailed
                // scene on top of delegated sales, purchasing, payroll, or rent.
                error = null;
                return true;
            }

            StoreSessionTotals totals = firstStore.CurrentTotals;
            if (totals == null)
            {
                error = null;
                return true;
            }

            if (!TryGetDetailedInventory(
                    firstStoreInventory.Inventory?.CreateSnapshot(),
                    firstStore.Checkout.ProductUnitCostsCents,
                    out int remainingInventoryUnits,
                    out long inventoryAssetValueCents,
                    out error))
            {
                return false;
            }

            bool success = progression.TryReconcileDetailedOperation(
                firstStore.StableSessionId,
                totals,
                remainingInventoryUnits,
                inventoryAssetValueCents,
                MerchandisingRules.AggregateCompletedSales(
                    firstStore.Checkout.CompletedTransactions),
                out bool unchanged,
                out error);
            if (success && !unchanged && totals.transactionCount > 0)
            {
                Record(
                    $"Live operation reconciled: cash {FormatCents(progression.CashCents)}, " +
                    $"sales {FormatCents(totals.grossSalesCents)}, COGS {FormatCents(totals.costOfGoodsSoldCents)}.",
                    true);
            }
            return success;
        }

        public bool TrySynchronizeDetailedProcurement(out string error)
        {
            if (progression == null || !progression.FirstShiftCompleted)
            {
                error = null;
                return true;
            }

            PortfolioLocationSnapshot firstLocation = progression.Locations
                .FirstOrDefault(location => string.Equals(
                    location.locationId,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal));
            if (firstLocation == null)
            {
                error = "The first location is unavailable for procurement reconciliation.";
                return false;
            }

            if (firstLocation.delegatedDaysOperating > 0)
            {
                // Aggregate inventory is authoritative after delegated operation.
                error = null;
                return true;
            }

            PurchaseOrderSnapshot order = progression.PurchaseOrders
                .FirstOrDefault(value =>
                    !value.IsTerminal &&
                    string.Equals(
                        value.locationId,
                        PortfolioProgressionRules.FirstLocationId,
                        StringComparison.Ordinal));
            if (order == null || order.status == PurchaseOrderStatus.Pending)
            {
                error = null;
                return true;
            }

            if (order.status == PurchaseOrderStatus.Fulfilled)
            {
                if (!TryMaterializeDetailedDelivery(order, out error))
                {
                    return false;
                }

                order = progression.PurchaseOrders.First(value =>
                    string.Equals(
                        value.orderId,
                        order.orderId,
                        StringComparison.Ordinal));
            }

            if (order.status != PurchaseOrderStatus.Delivered &&
                order.status != PurchaseOrderStatus.PartiallyReceived)
            {
                error = null;
                return true;
            }

            Dictionary<string, int> received = new(StringComparer.Ordinal);
            foreach (PurchaseOrderLineSnapshot line in order.lines)
            {
                int remaining = firstStoreInventory.Inventory.GetQuantity(
                    firstStoreDeliveryBox.InventoryLocationId,
                    line.resourceId);
                if (remaining < 0 || remaining > line.orderedQuantityUnits)
                {
                    error =
                        $"Physical delivery quantity for '{line.resourceId}' does not reconcile to order '{order.orderId}'.";
                    return false;
                }
                received.Add(
                    line.resourceId,
                    line.orderedQuantityUnits - remaining);
            }

            if (!progression.TryRecordPurchaseOrderReceipt(
                    order.orderId,
                    received,
                    out PurchaseOrderSnapshot updated,
                    out bool unchanged,
                    out error))
            {
                return false;
            }

            if (!unchanged && updated.status == PurchaseOrderStatus.Completed)
            {
                Record($"Received {updated.orderId}; its physical units are ready to stock.", true);
            }
            return true;
        }

        public bool TryPlaceManualPurchaseOrder(
            string locationId,
            out string error)
        {
            if (progression == null)
            {
                error = "Company procurement is unavailable.";
                return false;
            }

            PortfolioLocationSnapshot location = progression.Locations
                .FirstOrDefault(value => string.Equals(
                    value.locationId,
                    locationId,
                    StringComparison.Ordinal));
            if (location == null)
            {
                error = "Purchase order location is unavailable.";
                return false;
            }

            IReadOnlyList<ProcurementOrderRequestLine> lines;
            bool detailedFirstLocation =
                string.Equals(
                    locationId,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal) &&
                location.delegatedDaysOperating == 0;
            if (detailedFirstLocation)
            {
                lines = ConvenienceStoreProcurement.DetailedCase;
            }
            else
            {
                float targetRatio = location.reorderPolicy switch
                {
                    PortfolioReorderPolicy.Lean => 0.48f,
                    PortfolioReorderPolicy.Resilient => 0.9f,
                    _ => 0.72f
                };
                int reorderTarget = Math.Min(
                    location.inventoryCapacityUnits,
                    (int)Math.Round(
                        location.inventoryCapacityUnits * targetRatio));
                int requestedUnits = Math.Max(
                    0,
                    reorderTarget - location.inventoryUnits);
                if (requestedUnits == 0)
                {
                    error = "Current inventory already meets the configured reorder target.";
                    Record(error, false);
                    return false;
                }

                lines = new[]
                {
                    new ProcurementOrderRequestLine(
                        ConvenienceStoreProcurement.AggregateResourceId,
                        requestedUnits)
                };
            }

            bool success = progression.TryPlacePurchaseOrder(
                locationId,
                ConvenienceStoreProcurement.SupplierId,
                lines,
                out PurchaseOrderSnapshot order,
                out error);
            RecordResult(
                success,
                success
                    ? $"Placed {order.orderId} for {LocationName(locationId)}; charged {FormatCents(order.totalCostCents)} once."
                    : error);
            return success;
        }

        public bool TryCancelPurchaseOrder(
            string orderId,
            out string error)
        {
            error = null;
            bool unchanged = false;
            bool success = progression != null &&
                           progression.TryCancelPurchaseOrder(
                               orderId,
                               out unchanged,
                               out error);
            if (progression == null)
            {
                error = "Company procurement is unavailable.";
            }
            RecordResult(
                success,
                success
                    ? unchanged
                        ? $"{orderId} was already canceled."
                        : $"Canceled {orderId}; its payment was refunded once."
                    : error);
            return success;
        }

        private bool TryApplyDetailedReorderPolicy(out string error)
        {
            error = null;
            if (progression == null || !progression.FirstShiftCompleted)
            {
                return true;
            }

            PortfolioLocationSnapshot location = progression.Locations
                .First(value => string.Equals(
                    value.locationId,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal));
            if (location.delegatedDaysOperating > 0 ||
                progression.PurchaseOrders.Any(order =>
                    !order.IsTerminal &&
                    string.Equals(
                        order.locationId,
                        location.locationId,
                        StringComparison.Ordinal)))
            {
                return true;
            }

            int totalUnits = 0;
            foreach (InventoryLocationSnapshot inventoryLocation in
                     firstStoreInventory.Inventory.CreateSnapshot().locations)
            {
                totalUnits = checked(totalUnits +
                    inventoryLocation.quantities.Sum(quantity =>
                        quantity.quantityUnits));
            }

            int reorderPoint = location.reorderPolicy switch
            {
                PortfolioReorderPolicy.Lean => 2,
                PortfolioReorderPolicy.Resilient => 6,
                _ => 4
            };
            if (totalUnits > reorderPoint)
            {
                return true;
            }

            bool success = progression.TryPlacePurchaseOrder(
                location.locationId,
                ConvenienceStoreProcurement.SupplierId,
                ConvenienceStoreProcurement.DetailedCase,
                out PurchaseOrderSnapshot order,
                out error);
            if (success)
            {
                Record(
                    $"{location.reorderPolicy} policy placed {order.orderId}; charged {FormatCents(order.totalCostCents)}.",
                    true);
            }
            return success;
        }

        private bool TryMaterializeDetailedDelivery(
            PurchaseOrderSnapshot order,
            out string error)
        {
            if (order == null ||
                order.status != PurchaseOrderStatus.Fulfilled)
            {
                error = "The fulfilled order is unavailable for physical delivery.";
                return false;
            }

            if (firstStoreDeliveryBox.IsCarried)
            {
                error = null;
                return true;
            }

            FirstStoreInventorySnapshot previousInventory =
                firstStoreInventory.Inventory.CreateSnapshot();
            InventoryLocationSnapshot deliveryLocation = previousInventory.locations
                .FirstOrDefault(location => string.Equals(
                    location.locationId,
                    firstStoreDeliveryBox.InventoryLocationId,
                    StringComparison.Ordinal));
            if (deliveryLocation == null ||
                deliveryLocation.quantities.Any(quantity =>
                    quantity.quantityUnits > 0))
            {
                error = null;
                return true;
            }

            Dictionary<string, int> received = new(StringComparer.Ordinal);
            foreach (PurchaseOrderLineSnapshot line in order.lines)
            {
                received.Add(line.resourceId, line.orderedQuantityUnits);
            }

            InventoryReceiptFailure receiptFailure = InventoryReceiptFailure.None;
            if (!FirstStoreInventory.TryRestore(
                    previousInventory,
                    out FirstStoreInventory candidateInventory,
                    out error) ||
                !candidateInventory.TryReceiveDelivery(
                    firstStoreDeliveryBox.InventoryLocationId,
                    received,
                    out receiptFailure))
            {
                error ??=
                    $"The receiving container rejected order '{order.orderId}' ({receiptFailure}).";
                return false;
            }

            PortfolioProgressionSnapshot previousPortfolio =
                progression.CreateSnapshot();
            DeliveryContainerSnapshot previousContainer =
                firstStoreDeliveryBox.Container.CreateSnapshot();
            if (!firstStoreInventory.TryApplyRestoredInventory(
                    candidateInventory,
                    out error) ||
                !firstStoreDeliveryBox.TryPrepareProcurementDelivery(out error) ||
                !progression.TryCreatePurchaseOrderDelivery(
                    order.orderId,
                    out _,
                    out _,
                    out error) ||
                !TrySynchronizeDetailedShift(out error))
            {
                string materializationError = error;
                if (!TryRollbackDetailedDelivery(
                        previousInventory,
                        previousContainer,
                        previousPortfolio,
                        out string rollbackError))
                {
                    throw new InvalidOperationException(
                        $"Detailed delivery failed ('{materializationError}') and rollback failed ('{rollbackError}').");
                }

                error = materializationError;
                return false;
            }

            Record($"{order.orderId} arrived in a sealed physical container.", true);
            error = null;
            return true;
        }

        private bool TryRollbackDetailedDelivery(
            FirstStoreInventorySnapshot inventorySnapshot,
            DeliveryContainerSnapshot containerSnapshot,
            PortfolioProgressionSnapshot portfolioSnapshot,
            out string error)
        {
            if (!FirstStoreInventory.TryRestore(
                    inventorySnapshot,
                    out FirstStoreInventory restoredInventory,
                    out error) ||
                !firstStoreInventory.TryApplyRestoredInventory(
                    restoredInventory,
                    out error) ||
                !DeliveryContainer.TryRestore(
                    restoredInventory,
                    containerSnapshot,
                    out DeliveryContainer restoredContainer,
                    out error) ||
                !firstStoreDeliveryBox.TryApplyRestoredContainer(
                    restoredContainer,
                    out error) ||
                !PortfolioProgression.TryRestore(
                    portfolioSnapshot,
                    out PortfolioProgression restoredPortfolio,
                    out error))
            {
                return false;
            }

            progression = restoredPortfolio;
            error = null;
            return true;
        }

        public bool TryCaptureSnapshot(
            out PortfolioProgressionSnapshot snapshot,
            out string error)
        {
            snapshot = null;
            if (progression == null)
            {
                error = "Portfolio progression is not initialized.";
                return false;
            }

            if (!TrySynchronizeDetailedProcurement(out error))
            {
                return false;
            }

            snapshot = progression.CreateSnapshot();
            return PortfolioProgression.TryValidateSnapshot(snapshot, out error);
        }

        public bool TryValidateSnapshot(
            PortfolioProgressionSnapshot snapshot,
            out string error)
        {
            return PortfolioProgression.TryValidateSnapshot(snapshot, out error);
        }

        public bool TryValidateDetailedProcurementReconciliation(
            FirstStoreSnapshot firstStoreSnapshot,
            PortfolioProgressionSnapshot portfolioSnapshot,
            out string error)
        {
            error = null;
            if (firstStoreSnapshot?.inventory == null ||
                !PortfolioProgression.TryRestore(
                    portfolioSnapshot,
                    out PortfolioProgression restored,
                    out error))
            {
                error ??= "Detailed procurement reconciliation is missing store or portfolio state.";
                return false;
            }

            PortfolioLocationSnapshot firstLocation = restored.Locations
                .First(location => string.Equals(
                    location.locationId,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal));
            if (firstLocation.delegatedDaysOperating > 0)
            {
                error = null;
                return true;
            }

            PurchaseOrderSnapshot order = restored.PurchaseOrders
                .FirstOrDefault(value =>
                    !value.IsTerminal &&
                    string.Equals(
                        value.locationId,
                        firstLocation.locationId,
                        StringComparison.Ordinal));
            if (order == null ||
                (order.status != PurchaseOrderStatus.Delivered &&
                 order.status != PurchaseOrderStatus.PartiallyReceived))
            {
                error = null;
                return true;
            }

            InventoryLocationSnapshot delivery = firstStoreSnapshot.inventory.locations?.FirstOrDefault(
                location => string.Equals(
                    location.locationId,
                    firstStoreDeliveryBox.InventoryLocationId,
                    StringComparison.Ordinal));
            if (delivery?.quantities == null)
            {
                error = "Saved detailed delivery location is missing.";
                return false;
            }

            Dictionary<string, int> expected = order.lines.ToDictionary(
                line => line.resourceId,
                line => line.orderedQuantityUnits - line.receivedQuantityUnits,
                StringComparer.Ordinal);
            foreach (InventoryQuantitySnapshot quantity in delivery.quantities)
            {
                if (quantity == null ||
                    !expected.TryGetValue(
                        quantity.productId,
                        out int expectedQuantity) ||
                    quantity.quantityUnits != expectedQuantity)
                {
                    error =
                        $"Saved physical delivery does not reconcile to purchase order '{order.orderId}'.";
                    return false;
                }
                expected.Remove(quantity.productId);
            }

            if (expected.Any(value => value.Value != 0))
            {
                error =
                    $"Saved physical delivery is missing units from purchase order '{order.orderId}'.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryValidateDetailedMerchandisingReconciliation(
            FirstStoreSnapshot firstStoreSnapshot,
            PortfolioProgressionSnapshot portfolioSnapshot,
            out string error)
        {
            error = null;
            if (firstStoreSnapshot?.inventory?.locations == null ||
                firstStoreSnapshot.physicalProductUnits == null ||
                !PortfolioProgression.TryRestore(
                    portfolioSnapshot,
                    out PortfolioProgression restored,
                    out error))
            {
                error ??=
                    "Detailed merchandising reconciliation is missing store or portfolio state.";
                return false;
            }

            PortfolioLocationSnapshot location = restored.Locations.First(value =>
                string.Equals(
                    value.locationId,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal));
            Dictionary<string, ShelfMerchandiseAssignmentSnapshot> assignments =
                location.shelfMerchandiseAssignments.ToDictionary(
                    value => value.shelfFixtureId,
                    StringComparer.Ordinal);
            foreach (PhysicalProductUnitSnapshot unit in
                     firstStoreSnapshot.physicalProductUnits)
            {
                if (unit == null || string.IsNullOrWhiteSpace(unit.shelfFixtureId))
                {
                    continue;
                }
                if (!assignments.TryGetValue(
                        unit.shelfFixtureId,
                        out ShelfMerchandiseAssignmentSnapshot assignment) ||
                    !string.Equals(
                        assignment.inventoryLocationId,
                        unit.inventoryLocationId,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        assignment.assignedProductId,
                        unit.productId,
                        StringComparison.Ordinal))
                {
                    error =
                        $"Physical unit '{unit.physicalUnitId}' contradicts the saved merchandise assignment for shelf '{unit.shelfFixtureId}'.";
                    return false;
                }
            }

            foreach (ShelfMerchandiseAssignmentSnapshot assignment in
                     assignments.Values)
            {
                InventoryLocationSnapshot inventory =
                    firstStoreSnapshot.inventory.locations.FirstOrDefault(value =>
                        string.Equals(
                            value.locationId,
                            assignment.inventoryLocationId,
                            StringComparison.Ordinal));
                if (inventory?.quantities == null)
                {
                    error =
                        $"Saved shelf inventory '{assignment.inventoryLocationId}' is missing.";
                    return false;
                }
                if (inventory.quantities.Any(value =>
                        value.quantityUnits > 0 &&
                        !string.Equals(
                            value.productId,
                            assignment.assignedProductId,
                            StringComparison.Ordinal)))
                {
                    error =
                        $"Saved inventory at '{assignment.inventoryLocationId}' contradicts its merchandise assignment.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public bool TryRestoreSnapshot(
            PortfolioProgressionSnapshot snapshot,
            out string error)
        {
            if (!PortfolioProgression.TryRestore(
                    snapshot,
                    out PortfolioProgression restored,
                    out error))
            {
                return false;
            }

            progression = restored;
            nextProcurementTickAt = Time.unscaledTime + procurementTickSeconds;
            if (!progression.Locations.Any(location => string.Equals(
                    location.locationId,
                    selectedLocationId,
                    StringComparison.Ordinal)))
            {
                selectedLocationId = PortfolioProgressionRules.FirstLocationId;
            }
            Record("Restored company, employees, locations, policies, and reports.", true);
            return true;
        }

        public bool TryCreateLegacyMigrationSnapshot(
            FirstStoreSnapshot firstStoreSnapshot,
            out PortfolioProgressionSnapshot migrated,
            out string error)
        {
            PortfolioProgression migration = PortfolioProgression.CreateInitial();
            StoreOperatingSnapshot operating = firstStoreSnapshot?.storeOperating;
            StoreSessionTotals migratedTotals = operating?.hasResult == true
                ? operating.totals
                : null;
            if (migratedTotals == null && firstStoreSnapshot != null)
            {
                if (!CompletedTransactionLedger.TryRestore(
                        firstStoreSnapshot.transactionLedger,
                        out CompletedTransactionLedger ledger,
                        out error) ||
                    !StoreSessionTotals.TryCreateFromLedger(
                        ledger,
                        firstStore.IncludedOperatingExpensesCents,
                        out migratedTotals,
                        out error))
                {
                    migrated = null;
                    return false;
                }
            }
            if (migratedTotals != null)
            {
                if (!TryGetDetailedInventory(
                        firstStoreSnapshot.inventory,
                        firstStore.Checkout.ProductUnitCostsCents,
                        out int remainingInventoryUnits,
                        out long inventoryAssetValueCents,
                        out error))
                {
                    migrated = null;
                    return false;
                }
                if (!migration.TryReconcileDetailedOperation(
                        operating.sessionId,
                        migratedTotals,
                        remainingInventoryUnits,
                        inventoryAssetValueCents,
                        MerchandisingRules.AggregateCompletedSales(
                            firstStoreSnapshot.transactionLedger?.transactions),
                        out _,
                        out error))
                {
                    migrated = null;
                    return false;
                }
            }

            migrated = migration.CreateSnapshot();
            error = null;
            return true;
        }

        private static bool TryGetDetailedInventory(
            FirstStoreInventorySnapshot inventory,
            System.Collections.Generic.IReadOnlyDictionary<string, int> unitCostsCents,
            out int totalUnits,
            out long inventoryAssetValueCents,
            out string error)
        {
            totalUnits = 0;
            inventoryAssetValueCents = 0;
            if (inventory?.locations == null || unitCostsCents == null)
            {
                error = "Detailed first-store inventory is missing.";
                return false;
            }

            try
            {
                foreach (InventoryLocationSnapshot location in inventory.locations)
                {
                    if (location?.quantities == null)
                    {
                        error = "Detailed first-store inventory contains a missing location quantity list.";
                        return false;
                    }
                    foreach (InventoryQuantitySnapshot quantity in location.quantities)
                    {
                        if (quantity == null || quantity.quantityUnits < 0)
                        {
                            error = "Detailed first-store inventory contains an invalid quantity.";
                            return false;
                        }
                        if (!unitCostsCents.TryGetValue(
                                quantity.productId,
                                out int unitCostCents) ||
                            unitCostCents < 0)
                        {
                            error =
                                $"Detailed inventory product '{quantity.productId}' has no authoritative unit cost.";
                            return false;
                        }
                        totalUnits = checked(totalUnits + quantity.quantityUnits);
                        inventoryAssetValueCents = checked(
                            inventoryAssetValueCents +
                            (long)quantity.quantityUnits * unitCostCents);
                    }
                }
            }
            catch (OverflowException)
            {
                error = "Detailed first-store inventory total overflowed integer storage.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryHireCandidate(
            string employeeId,
            string locationId,
            out string error)
        {
            bool success = progression.TryHireCandidate(
                employeeId,
                locationId,
                out error);
            RecordResult(
                success,
                success && PortfolioProgressionRules.TryGetCandidate(
                    employeeId,
                    out PortfolioCandidateDefinition candidate)
                    ? $"Hired {candidate.DisplayName} for {LocationName(locationId)}."
                    : error);
            return success;
        }

        public bool TrySetPricingPreset(
            string locationId,
            PortfolioPricingPolicy preset,
            out string error)
        {
            if (progression == null)
            {
                error = "Company pricing is not initialized.";
                return false;
            }

            if (string.Equals(
                    locationId,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal))
            {
                FirstStoreMerchandisingComponent merchandising =
                    firstStore?.Checkout?.Merchandising;
                if (merchandising == null)
                {
                    error =
                        "The loaded first store has no merchandising safety adapter.";
                    return false;
                }
                return merchandising.TryApplyPricePreset(preset, out error);
            }

            return progression.TrySetPricingPolicy(
                locationId,
                preset,
                out error);
        }

        public bool TryAdvanceDelegatedDay(out string error)
        {
            if (!TrySynchronizeLivePayroll(out error) ||
                !TrySynchronizeDetailedProcurement(out error) ||
                !TrySynchronizeDetailedShift(out error))
            {
                RecordResult(false, error);
                return false;
            }

            bool success = progression.TryAdvanceDelegatedDay(out error);
            RecordResult(
                success,
                success
                    ? $"Delegated day {progression.CurrentDay} completed and all location reports posted."
                    : error);
            return success;
        }

        public bool TryLeaseLocation(string locationId, out string error)
        {
            bool success = progression.TryLeaseLocation(locationId, out error);
            if (success)
            {
                selectedLocationId = locationId;
                page = DeskPage.People;
            }
            RecordResult(
                success,
                success ? $"Leased and stocked {LocationName(locationId)}." : error);
            return success;
        }

        private void OnGUI()
        {
            if (!OwnsManagementDesk)
            {
                return;
            }

            EnsureHumanStyles();
            float scale = Mathf.Max(
                0.68f,
                Mathf.Min(Screen.width / 1920f, Screen.height / 1080f) *
                GamePauseMenuController.UserInterfaceScale);
            Matrix4x4 priorMatrix = GUI.matrix;
            Color priorColor = GUI.color;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            DrawHumanPanel(new Rect(0f, 0f, width, height), NightDeep);
            DrawHumanHeader(width);
            DrawHumanNavigation(height);
            Rect content = new(258f, 132f, width - 286f, height - 164f);
            switch (page)
            {
                case DeskPage.People:
                    DrawHumanPeople(content);
                    break;
                case DeskPage.Locations:
                    DrawHumanLocations(content);
                    break;
                case DeskPage.Reports:
                    DrawHumanReports(content);
                    break;
                default:
                    DrawHumanOverview(content);
                    break;
            }
            DrawHumanActionToast(width, height);
            GUI.matrix = priorMatrix;
            GUI.color = priorColor;
        }

        private void DrawHeader(float width)
        {
            PortfolioProgressionSnapshot snapshot = progression.CreateSnapshot();
            GUI.Box(new Rect(24f, 18f, width - 48f, 75f), "");
            GUI.Label(
                new Rect(45f, 29f, 540f, 30f),
                "MILE 7 HOLDINGS  /  COMPANY DESK");
            GUI.Label(
                new Rect(45f, 57f, 760f, 24f),
                $"DAY {snapshot.currentDay}   CASH {FormatCents(snapshot.cashCents)}   " +
                $"REPUTATION {snapshot.companyReputation}/100   " +
                $"LOCATIONS {snapshot.locations.Count}/2");
            GUI.Label(
                new Rect(width - 470f, 38f, 430f, 28f),
                "TAB return to store  |  F5 save  |  F9 load");
        }

        private void DrawNavigation(float width)
        {
            float buttonWidth = (width - 68f) / 4f;
            float x = 34f;
            if (GUI.Button(new Rect(x, 105f, buttonWidth, 34f), "OVERVIEW"))
            {
                page = DeskPage.Overview;
            }
            x += buttonWidth;
            if (GUI.Button(new Rect(x, 105f, buttonWidth, 34f), "PEOPLE"))
            {
                page = DeskPage.People;
            }
            x += buttonWidth;
            if (GUI.Button(new Rect(x, 105f, buttonWidth, 34f), "LOCATIONS"))
            {
                page = DeskPage.Locations;
            }
            x += buttonWidth;
            if (GUI.Button(new Rect(x, 105f, buttonWidth, 34f), "REPORTS"))
            {
                page = DeskPage.Reports;
            }
        }

        private void DrawOverview()
        {
            PortfolioProgressionSnapshot snapshot = progression.CreateSnapshot();
            GUILayout.Label("OWNER TO PORTFOLIO PROGRESSION");
            GUILayout.Space(6f);
            DrawCheck(true, "Hands-on first shift completed and posted once to company cash");
            bool firstStaffed = IsFullyStaffed(
                snapshot,
                PortfolioProgressionRules.FirstLocationId);
            DrawCheck(firstStaffed, "First store has cashier, stock clerk, and manager");
            PortfolioLocationSnapshot first = snapshot.locations.First(location =>
                location.locationId == PortfolioProgressionRules.FirstLocationId);
            DrawCheck(
                first.delegatedDaysOperating > 0,
                "Manager has proven at least one delegated operating day");
            DrawCheck(snapshot.locations.Count > 1, "Second market selected and lease signed");
            bool portfolioStaffed = snapshot.locations.All(location =>
                IsFullyStaffed(snapshot, location.locationId));
            DrawCheck(
                snapshot.locations.Count > 1 && portfolioStaffed,
                "Both locations can operate without the owner present");

            GUILayout.Space(16f);
            GUILayout.Label("NEXT COMPANY ACTION");
            if (progression.CanAdvanceDelegatedDay(out string blocker))
            {
                if (GUILayout.Button(
                        $"RUN DELEGATED DAY {snapshot.currentDay + 1}",
                        GUILayout.Height(48f)))
                {
                    TryAdvanceDelegatedDay(out _);
                }
                GUILayout.Label(
                    "Managers will use each location's current shelf prices and reorder policy. Sales, COGS, payroll, rent, purchases, skills, satisfaction, reputation, and cash all resolve together.");
            }
            else
            {
                GUILayout.Box($"BLOCKED: {blocker}", GUILayout.Height(46f));
                if (!firstStaffed)
                {
                    GUILayout.Label("Open PEOPLE and hire one worker in each role for Mile 7 Market.");
                }
                else if (snapshot.locations.Count == 1 && first.daysOperating > 0)
                {
                    GUILayout.Label("Open LOCATIONS to compare the two expansion markets.");
                }
            }

            GUILayout.Space(16f);
            DrawPortfolioSummary(snapshot);
        }

        private void DrawPeople()
        {
            PortfolioProgressionSnapshot snapshot = progression.CreateSnapshot();
            DrawLocationSelector(snapshot);
            PortfolioLocationSnapshot selected = SelectedLocation(snapshot);
            GUILayout.Space(8f);
            GUILayout.Label($"TEAM  /  {selected.displayName.ToUpperInvariant()}");
            peopleScroll = GUILayout.BeginScrollView(peopleScroll);
            PortfolioEmployeeSnapshot[] assigned = snapshot.employees
                .Where(employee => employee.assignedLocationId == selected.locationId)
                .OrderBy(employee => employee.role)
                .ToArray();
            if (assigned.Length == 0)
            {
                GUILayout.Box("No employees assigned. Hire a cashier, stock clerk, and manager to delegate this location.");
            }
            foreach (PortfolioEmployeeSnapshot employee in assigned)
            {
                DrawEmployee(employee, snapshot);
            }

            GUILayout.Space(12f);
            GUILayout.Label("AVAILABLE CANDIDATES");
            foreach (PortfolioCandidateDefinition candidate in
                     PortfolioProgressionRules.Candidates)
            {
                if (snapshot.employees.Any(employee =>
                        employee.employeeId == candidate.EmployeeId))
                {
                    continue;
                }
                GUILayout.BeginHorizontal("box");
                GUILayout.Label(
                    $"{candidate.DisplayName}  |  {FriendlyRole(candidate.Role)}  |  " +
                    $"skill {candidate.Skill}  reliability {candidate.Reliability}\n" +
                    $"{candidate.Trait}  |  wage {FormatCents(candidate.DailyWageCents)}/day  |  " +
                    $"hire {FormatCents(candidate.HiringCostCents)}",
                    GUILayout.ExpandWidth(true));
                if (GUILayout.Button("HIRE HERE", GUILayout.Width(120f), GUILayout.Height(42f)))
                {
                    TryHireCandidate(candidate.EmployeeId, selected.locationId, out _);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        private void DrawEmployee(
            PortfolioEmployeeSnapshot employee,
            PortfolioProgressionSnapshot snapshot)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label(
                $"{employee.displayName}  |  {FriendlyRole(employee.role)}  |  " +
                $"focus {employee.taskFocus}\n" +
                $"skill {employee.skill}  reliability {employee.reliability}  " +
                $"satisfaction {employee.satisfaction}  |  {employee.trait}  |  " +
                $"{FormatCents(employee.dailyWageCents)}/day");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(
                    $"TRAIN {FormatCents(PortfolioProgressionRules.TrainingCostCents)}"))
            {
                bool success = progression.TryTrainEmployee(
                    employee.employeeId,
                    out string error);
                RecordResult(
                    success,
                    success ? $"Trained {employee.displayName}; skill and satisfaction improved." : error);
            }
            if (employee.role != PortfolioEmployeeRole.Manager &&
                GUILayout.Button(
                    $"PROMOTE {FormatCents(PortfolioProgressionRules.PromotionCostCents)}"))
            {
                bool success = progression.TryPromoteToManager(
                    employee.employeeId,
                    out string error);
                RecordResult(
                    success,
                    success ? $"Promoted {employee.displayName} to manager." : error);
            }
            if (GUILayout.Button("CYCLE FOCUS"))
            {
                PortfolioTaskFocus next = (PortfolioTaskFocus)(
                    ((int)employee.taskFocus + 1) %
                    Enum.GetValues(typeof(PortfolioTaskFocus)).Length);
                bool success = progression.TrySetTaskFocus(
                    employee.employeeId,
                    next,
                    out string error);
                RecordResult(
                    success,
                    success ? $"Set {employee.displayName}'s focus to {next}." : error);
            }
            PortfolioLocationSnapshot other = snapshot.locations.FirstOrDefault(location =>
                location.locationId != employee.assignedLocationId);
            if (other != null && GUILayout.Button($"MOVE TO {other.displayName.ToUpperInvariant()}"))
            {
                bool success = progression.TryReassignEmployee(
                    employee.employeeId,
                    other.locationId,
                    out string error);
                RecordResult(
                    success,
                    success ? $"Reassigned {employee.displayName} to {other.displayName}." : error);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawLocations()
        {
            PortfolioProgressionSnapshot snapshot = progression.CreateSnapshot();
            DrawLocationSelector(snapshot);
            PortfolioLocationSnapshot location = SelectedLocation(snapshot);
            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical("box", GUILayout.Width(600f));
            GUILayout.Label($"{location.displayName.ToUpperInvariant()}  /  {location.districtName.ToUpperInvariant()}");
            GUILayout.Label(location.marketSummary);
            GUILayout.Label(
                $"BUSINESS SYSTEM  /  {location.businessTypeId}\n{location.operatingModel}");
            GUILayout.Label(
                $"Demand index {location.baseDemandUnits}  |  competition {location.competitionIndex}/100  |  " +
                $"reputation {location.reputation}/100\n" +
                $"Inventory {location.inventoryUnits}/{location.inventoryCapacityUnits}  |  " +
                $"rent {FormatCents(location.dailyRentCents)}/day  |  delegated days {location.delegatedDaysOperating}");
            GUILayout.Space(8f);
            GUILayout.Label("PRICE PRESETS");
            GUILayout.BeginHorizontal();
            foreach (PortfolioPricingPolicy policy in
                     Enum.GetValues(typeof(PortfolioPricingPolicy)))
            {
                bool selected = IsPricePresetApplied(location, policy);
                if (GUILayout.Button(
                        selected ? $"[{policy}]" : policy.ToString()))
                {
                    bool success = TrySetPricingPreset(
                        location.locationId,
                        policy,
                        out string error);
                    RecordResult(
                        success,
                        success ? $"{location.displayName} pricing set to {policy}." : error);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(
                $"Current: {FormatCurrentMerchandisePrices(location)}\n" +
                "Presets update every item. Individual prices are edited at shelf tags.");
            GUILayout.Space(8f);
            GUILayout.Label("MANAGER REORDER POLICY");
            GUILayout.BeginHorizontal();
            foreach (PortfolioReorderPolicy policy in
                     Enum.GetValues(typeof(PortfolioReorderPolicy)))
            {
                if (GUILayout.Button(
                        policy == location.reorderPolicy ? $"[{policy}]" : policy.ToString()))
                {
                    bool success = progression.TrySetReorderPolicy(
                        location.locationId,
                        policy,
                        out string error);
                    RecordResult(
                        success,
                        success ? $"{location.displayName} reorder policy set to {policy}." : error);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Lean protects cash; Resilient protects availability by tying up more cash in stock.");
            GUILayout.EndVertical();

            GUILayout.Space(16f);
            GUILayout.BeginVertical("box");
            if (snapshot.locations.Count == 1)
            {
                GUILayout.Label("EXPANSION SITES");
                foreach (PortfolioLocationDefinition option in
                         PortfolioProgressionRules.ExpansionOptions)
                {
                    long committed = option.LeaseCostCents + option.OpeningInventoryCostCents;
                    GUILayout.BeginVertical("box");
                    GUILayout.Label(
                        $"{option.DisplayName}  /  {option.DistrictName}\n" +
                        $"{option.MarketSummary}\n" +
                        $"Demand {option.BaseDemandUnits}  competition {option.CompetitionIndex}/100  " +
                        $"rent {FormatCents(option.DailyRentCents)}/day\n" +
                        $"Lease + opening stock {FormatCents(committed)}");
                    if (GUILayout.Button($"LEASE {option.DisplayName.ToUpperInvariant()}"))
                    {
                        TryLeaseLocation(option.LocationId, out _);
                    }
                    GUILayout.EndVertical();
                }
            }
            else
            {
                GUILayout.Label("TWO-LOCATION PORTFOLIO ACTIVE");
                GUILayout.Label(
                    "Both sites share the employee, policy, finance, reporting, and aggregate-simulation foundation. Staff every role before advancing the next delegated day.");
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawReports()
        {
            PortfolioProgressionSnapshot snapshot = progression.CreateSnapshot();
            GUILayout.Label($"PORTFOLIO REPORT  /  THROUGH DAY {snapshot.currentDay}");
            GUILayout.Space(8f);
            reportScroll = GUILayout.BeginScrollView(reportScroll);
            long totalSales = 0;
            long totalProfit = 0;
            foreach (PortfolioLocationSnapshot location in snapshot.locations)
            {
                totalSales += location.lifetimeGrossSalesCents;
                totalProfit += location.lifetimeOperatingProfitCents;
                GUILayout.BeginVertical("box");
                GUILayout.Label(
                    $"{location.displayName.ToUpperInvariant()}  /  {location.districtName}\n" +
                    $"Lifetime sales {FormatCents(location.lifetimeGrossSalesCents)}  |  " +
                    $"COGS {FormatCents(location.lifetimeCostOfGoodsSoldCents)}  |  " +
                    $"purchases {FormatCents(location.lifetimeInventoryPurchaseCents)}\n" +
                    $"Payroll {FormatCents(location.lifetimePayrollCents)}  |  " +
                    $"rent {FormatCents(location.lifetimeRentCents)}  |  " +
                    $"operating profit {FormatCents(location.lifetimeOperatingProfitCents)}  |  " +
                    $"reputation {location.reputation}/100  |  inventory {location.inventoryUnits}/{location.inventoryCapacityUnits}");
                PortfolioLocationReportSnapshot report = location.lastReport;
                if (report == null)
                {
                    GUILayout.Label("No delegated day has been reported yet.");
                }
                else
                {
                    GUILayout.Label(
                        $"DAY {report.day}: demand {report.demandUnits}, sold {report.unitsSold}, lost {report.lostDemandUnits}, " +
                        $"prices {FormatReportMerchandisePrices(report)}\n" +
                        $"Sales {FormatCents(report.grossSalesCents)} - COGS {FormatCents(report.costOfGoodsSoldCents)} - " +
                        $"payroll {FormatCents(report.payrollCents)} - rent {FormatCents(report.rentCents)} = " +
                        $"operating profit {FormatCents(report.operatingProfitCents)}\n" +
                        $"Ordered {report.reorderedUnits} units for {FormatCents(report.inventoryPurchaseCents)} plus {FormatCents(report.deliveryFeesCents)} delivery; " +
                        $"cash change {FormatCents(report.cashChangeCents)}.\n" +
                        $"CAUSE: {report.primaryCause}");
                }
                GUILayout.EndVertical();
            }
            GUILayout.Space(8f);
            GUILayout.Box(
                $"PORTFOLIO LIFETIME SALES {FormatCents(totalSales)}  |  " +
                $"OPERATING PROFIT {FormatCents(totalProfit)}  |  CASH {FormatCents(snapshot.cashCents)}");
            GUILayout.EndScrollView();
        }

        private void DrawPortfolioSummary(PortfolioProgressionSnapshot snapshot)
        {
            GUILayout.Label("CURRENT LOCATIONS");
            foreach (PortfolioLocationSnapshot location in snapshot.locations)
            {
                int staff = snapshot.employees.Count(employee =>
                    employee.assignedLocationId == location.locationId);
                string report = location.lastReport == null
                    ? "no delegated report"
                    : $"day {location.lastReport.day} profit {FormatCents(location.lastReport.operatingProfitCents)}";
                GUILayout.Box(
                    $"{location.displayName} / {location.districtName}  |  staff {staff}/3  |  " +
                    $"prices {FormatCurrentMerchandisePrices(location)}  |  reorder {location.reorderPolicy}  |  {report}");
            }
        }

        private void DrawLocationSelector(PortfolioProgressionSnapshot snapshot)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("ACTIVE LOCATION", GUILayout.Width(130f));
            foreach (PortfolioLocationSnapshot location in snapshot.locations)
            {
                if (GUILayout.Button(
                        selectedLocationId == location.locationId
                            ? $"[{location.displayName}]"
                            : location.displayName,
                        GUILayout.Width(210f)))
                {
                    selectedLocationId = location.locationId;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawFooter(float width, float height)
        {
            Color prior = GUI.color;
            GUI.color = lastActionSucceeded
                ? new Color(0.72f, 1f, 0.84f)
                : new Color(1f, 0.66f, 0.58f);
            GUI.Box(
                new Rect(24f, height - 70f, width - 48f, 44f),
                lastAction);
            GUI.color = prior;
        }

        private void EnsureHumanStyles()
        {
            if (humanTitleStyle != null)
            {
                return;
            }

            humanBrandStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Teal }
            };
            humanTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Ink }
            };
            humanSectionStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Ink }
            };
            humanBodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = Ink }
            };
            humanSmallStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = MutedInk }
            };
            humanMetricStyle = new GUIStyle(humanSmallStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            humanMetricValueStyle = new GUIStyle(humanTitleStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 23
            };
            humanButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { background = null, textColor = Ink },
                hover = { background = null, textColor = Color.white },
                active = { background = null, textColor = Color.white },
                focused = { background = null, textColor = Ink }
            };
            humanButtonPrimaryStyle = new GUIStyle(humanButtonStyle)
            {
                normal = { background = null, textColor = NightDeep },
                hover = { background = null, textColor = NightDeep },
                active = { background = null, textColor = NightDeep },
                focused = { background = null, textColor = NightDeep }
            };
            humanCenteredStyle = new GUIStyle(humanBodyStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }

        private static void DrawHumanPanel(Rect rect, Color color)
        {
            Color prior = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = prior;
        }

        private void DrawHumanHeader(float width)
        {
            PortfolioProgressionSnapshot snapshot = progression.CreateSnapshot();
            DrawHumanPanel(new Rect(0f, 0f, width, 106f), Night);
            DrawHumanPanel(new Rect(0f, 103f, width, 3f), Teal);
            GUI.Label(
                new Rect(32f, 19f, 430f, 28f),
                "MARGINS  /  COMPANY",
                humanBrandStyle);
            GUI.Label(
                new Rect(32f, 48f, 520f, 38f),
                "Your business",
                humanTitleStyle);

            DrawHeaderMetric(
                new Rect(width - 704f, 18f, 146f, 68f),
                "DAY",
                snapshot.currentDay.ToString());
            DrawHeaderMetric(
                new Rect(width - 546f, 18f, 176f, 68f),
                "AVAILABLE CASH",
                FormatCents(snapshot.cashCents));
            DrawHeaderMetric(
                new Rect(width - 358f, 18f, 146f, 68f),
                "LOCATIONS",
                snapshot.locations.Count.ToString());
            DrawHeaderMetric(
                new Rect(width - 200f, 18f, 168f, 68f),
                "REPUTATION",
                $"{snapshot.companyReputation}/100");
        }

        private void DrawHeaderMetric(Rect rect, string label, string value)
        {
            DrawHumanPanel(rect, NightSoft);
            GUI.Label(
                new Rect(rect.x + 14f, rect.y + 8f, rect.width - 28f, 18f),
                label,
                humanMetricStyle);
            GUI.Label(
                new Rect(rect.x + 14f, rect.y + 27f, rect.width - 28f, 31f),
                value,
                humanSectionStyle);
        }

        private void DrawHumanNavigation(float height)
        {
            DrawHumanPanel(new Rect(0f, 106f, 232f, height - 106f), Night);
            GUI.Label(
                new Rect(24f, 132f, 180f, 24f),
                "COMPANY DESK",
                humanMetricStyle);

            DrawHumanNavButton(
                new Rect(18f, 172f, 196f, 56f),
                "1",
                "Overview",
                DeskPage.Overview);
            DrawHumanNavButton(
                new Rect(18f, 236f, 196f, 56f),
                "2",
                "Team",
                DeskPage.People);
            DrawHumanNavButton(
                new Rect(18f, 300f, 196f, 56f),
                "3",
                "Locations",
                DeskPage.Locations);
            DrawHumanNavButton(
                new Rect(18f, 364f, 196f, 56f),
                "4",
                "Reports",
                DeskPage.Reports);

            GUI.Label(
                new Rect(24f, height - 116f, 184f, 76f),
                "TAB  Return to store\nESC  Pause\nF5  Quick save",
                humanSmallStyle);
        }

        private void DrawHumanNavButton(
            Rect rect,
            string shortcut,
            string label,
            DeskPage destination)
        {
            bool selected = page == destination;
            bool hovered = rect.Contains(Event.current.mousePosition);
            DrawHumanPanel(
                rect,
                selected
                    ? new Color(Teal.r, Teal.g, Teal.b, 0.18f)
                    : hovered
                        ? NightRaised
                        : Night);
            if (selected)
            {
                DrawHumanPanel(new Rect(rect.x, rect.y, 4f, rect.height), Teal);
            }

            if (GUI.Button(rect, GUIContent.none, humanButtonStyle))
            {
                page = destination;
            }
            GUI.Label(
                new Rect(rect.x + 18f, rect.y + 17f, 22f, 24f),
                shortcut,
                humanMetricStyle);
            GUI.Label(
                new Rect(rect.x + 49f, rect.y + 14f, rect.width - 60f, 30f),
                label,
                humanSectionStyle);
        }

        private void DrawHumanOverview(Rect content)
        {
            PortfolioProgressionSnapshot snapshot = progression.CreateSnapshot();
            GUI.Label(
                new Rect(content.x, content.y, content.width, 40f),
                "Company at a glance",
                humanTitleStyle);
            GUI.Label(
                new Rect(content.x, content.y + 42f, content.width, 24f),
                "The owner view: what is working, what needs attention, and what to do next.",
                humanSmallStyle);

            float gap = 14f;
            float metricWidth = (content.width - gap * 3f) / 4f;
            int assigned = snapshot.employees.Count;
            int required = snapshot.locations.Count * 3;
            DrawHumanMetricCard(
                new Rect(content.x, content.y + 80f, metricWidth, 94f),
                "CASH",
                FormatCents(snapshot.cashCents),
                snapshot.cashCents >= 0 ? Teal : Error);
            DrawHumanMetricCard(
                new Rect(
                    content.x + metricWidth + gap,
                    content.y + 80f,
                    metricWidth,
                    94f),
                "TEAM",
                $"{assigned} / {required}",
                assigned >= required ? Teal : Amber);
            DrawHumanMetricCard(
                new Rect(
                    content.x + (metricWidth + gap) * 2f,
                    content.y + 80f,
                    metricWidth,
                    94f),
                "LOCATIONS",
                snapshot.locations.Count.ToString(),
                snapshot.locations.Count > 1 ? Teal : Ink);
            DrawHumanMetricCard(
                new Rect(
                    content.x + (metricWidth + gap) * 3f,
                    content.y + 80f,
                    metricWidth,
                    94f),
                "REPUTATION",
                $"{snapshot.companyReputation}/100",
                snapshot.companyReputation >= 60 ? Teal : Amber);

            GUI.Label(
                new Rect(content.x, content.y + 198f, content.width, 24f),
                "GROWTH",
                humanMetricStyle);
            DrawHumanGrowthPath(
                new Rect(content.x, content.y + 230f, content.width, 92f),
                snapshot);

            float actionWidth = Mathf.Max(510f, content.width * 0.54f);
            Rect actionCard = new(
                content.x,
                content.y + 346f,
                actionWidth,
                216f);
            DrawHumanNextAction(actionCard, snapshot);

            Rect portfolioCard = new(
                actionCard.xMax + gap,
                actionCard.y,
                content.xMax - actionCard.xMax - gap,
                actionCard.height);
            DrawHumanPortfolioPulse(portfolioCard, snapshot);

            GUI.Label(
                new Rect(content.x, content.y + 590f, content.width, 24f),
                "LOCATIONS",
                humanMetricStyle);
            float locationWidth = Mathf.Min(
                480f,
                (content.width - gap * Mathf.Max(0, snapshot.locations.Count - 1)) /
                Mathf.Max(1, snapshot.locations.Count));
            float x = content.x;
            foreach (PortfolioLocationSnapshot location in snapshot.locations)
            {
                DrawHumanLocationSummary(
                    new Rect(x, content.y + 620f, locationWidth, 132f),
                    location,
                    snapshot);
                x += locationWidth + gap;
            }
        }

        private void DrawHumanMetricCard(
            Rect rect,
            string label,
            string value,
            Color accent)
        {
            DrawHumanPanel(rect, NightSoft);
            DrawHumanPanel(new Rect(rect.x, rect.y, 4f, rect.height), accent);
            GUI.Label(
                new Rect(rect.x + 18f, rect.y + 14f, rect.width - 30f, 20f),
                label,
                humanMetricStyle);
            GUI.Label(
                new Rect(rect.x + 18f, rect.y + 40f, rect.width - 30f, 39f),
                value,
                humanMetricValueStyle);
        }

        private void DrawHumanGrowthPath(
            Rect rect,
            PortfolioProgressionSnapshot snapshot)
        {
            PortfolioLocationSnapshot first = snapshot.locations.First(location =>
                location.locationId == PortfolioProgressionRules.FirstLocationId);
            bool[] completed =
            {
                progression.FirstShiftCompleted,
                IsFullyStaffed(snapshot, PortfolioProgressionRules.FirstLocationId),
                first.delegatedDaysOperating > 0,
                snapshot.locations.Count > 1,
                snapshot.locations.Count > 1 &&
                snapshot.locations.All(location => IsFullyStaffed(snapshot, location.locationId))
            };
            string[] labels =
            {
                "Owner",
                "Core team",
                "Delegated",
                "Expanded",
                "Portfolio"
            };
            float segment = rect.width / labels.Length;
            float centerY = rect.y + 26f;
            for (int i = 0; i < labels.Length; i++)
            {
                float centerX = rect.x + segment * (i + 0.5f);
                if (i < labels.Length - 1)
                {
                    DrawHumanPanel(
                        new Rect(centerX + 13f, centerY - 2f, segment - 26f, 4f),
                        completed[i + 1] ? Teal : NightRaised);
                }
                DrawHumanPanel(
                    new Rect(centerX - 13f, centerY - 13f, 26f, 26f),
                    completed[i] ? Teal : NightRaised);
                GUI.Label(
                    new Rect(centerX - segment * 0.45f, rect.y + 52f, segment * 0.9f, 26f),
                    labels[i],
                    humanCenteredStyle);
            }
        }

        private void DrawHumanNextAction(
            Rect rect,
            PortfolioProgressionSnapshot snapshot)
        {
            DrawHumanPanel(rect, NightSoft);
            GUI.Label(
                new Rect(rect.x + 22f, rect.y + 18f, rect.width - 44f, 20f),
                "NEXT MOVE",
                humanMetricStyle);

            PortfolioLocationSnapshot first = snapshot.locations.First(location =>
                location.locationId == PortfolioProgressionRules.FirstLocationId);
            bool firstStaffed = IsFullyStaffed(
                snapshot,
                PortfolioProgressionRules.FirstLocationId);
            bool portfolioStaffed = snapshot.locations.All(location =>
                IsFullyStaffed(snapshot, location.locationId));

            string title;
            string detail;
            string action;
            Action onAction;
            if (!firstStaffed)
            {
                title = "Build the first store team";
                detail = "Hire a cashier, stock clerk, and manager before operating off-site.";
                action = "Open team";
                onAction = () => page = DeskPage.People;
            }
            else if (first.delegatedDaysOperating == 0)
            {
                title = "Let the team run a day";
                detail = "Your manager will use the current shelf prices and inventory policy.";
                action = $"Run day {snapshot.currentDay + 1}";
                onAction = () => TryAdvanceDelegatedDay(out _);
            }
            else if (snapshot.locations.Count == 1)
            {
                title = "Choose the next market";
                detail = "Compare demand, competition, rent, and opening cost before expanding.";
                action = "Compare locations";
                onAction = () => page = DeskPage.Locations;
            }
            else if (!portfolioStaffed)
            {
                title = "Staff every location";
                detail = "A complete team keeps each location operating without the owner.";
                action = "Open team";
                onAction = () => page = DeskPage.People;
            }
            else
            {
                title = "Run the portfolio";
                detail = "Advance a day, then use Reports to understand why performance changed.";
                action = $"Run day {snapshot.currentDay + 1}";
                onAction = () => TryAdvanceDelegatedDay(out _);
            }

            GUI.Label(
                new Rect(rect.x + 22f, rect.y + 52f, rect.width - 44f, 36f),
                title,
                humanTitleStyle);
            GUI.Label(
                new Rect(rect.x + 22f, rect.y + 96f, rect.width - 44f, 48f),
                detail,
                humanBodyStyle);
            if (DrawHumanButton(
                    new Rect(rect.x + 22f, rect.yMax - 56f, 230f, 38f),
                    action,
                    true))
            {
                onAction();
            }
        }

        private void DrawHumanPortfolioPulse(
            Rect rect,
            PortfolioProgressionSnapshot snapshot)
        {
            DrawHumanPanel(rect, NightSoft);
            GUI.Label(
                new Rect(rect.x + 22f, rect.y + 18f, rect.width - 44f, 20f),
                "PORTFOLIO PULSE",
                humanMetricStyle);
            long sales = snapshot.locations.Sum(location =>
                location.lifetimeGrossSalesCents);
            long profit = snapshot.locations.Sum(location =>
                location.lifetimeOperatingProfitCents);
            int inventory = snapshot.locations.Sum(location =>
                location.inventoryUnits);
            int capacity = snapshot.locations.Sum(location =>
                location.inventoryCapacityUnits);
            GUI.Label(
                new Rect(rect.x + 22f, rect.y + 52f, rect.width - 44f, 29f),
                $"Lifetime sales   {FormatCents(sales)}",
                humanSectionStyle);
            GUI.Label(
                new Rect(rect.x + 22f, rect.y + 91f, rect.width - 44f, 29f),
                $"Operating profit   {FormatCents(profit)}",
                humanSectionStyle);
            GUI.Label(
                new Rect(rect.x + 22f, rect.y + 130f, rect.width - 44f, 24f),
                $"Inventory   {inventory} / {capacity}",
                humanBodyStyle);
            DrawHumanBar(
                new Rect(rect.x + 22f, rect.y + 163f, rect.width - 44f, 10f),
                capacity > 0 ? inventory / (float)capacity : 0f,
                inventory < capacity * 0.2f ? Amber : Teal);
        }

        private void DrawHumanLocationSummary(
            Rect rect,
            PortfolioLocationSnapshot location,
            PortfolioProgressionSnapshot snapshot)
        {
            DrawHumanPanel(rect, NightSoft);
            int staff = snapshot.employees.Count(employee =>
                employee.assignedLocationId == location.locationId);
            Color status = staff >= 3 ? Teal : Amber;
            GUI.Label(
                new Rect(rect.x + 18f, rect.y + 14f, rect.width - 126f, 27f),
                location.displayName,
                humanSectionStyle);
            DrawHumanPill(
                new Rect(rect.xMax - 102f, rect.y + 13f, 84f, 25f),
                staff >= 3 ? "READY" : "STAFF",
                status);
            GUI.Label(
                new Rect(rect.x + 18f, rect.y + 46f, rect.width - 36f, 22f),
                location.districtName,
                humanSmallStyle);
            string latest = location.lastReport == null
                ? "No delegated report yet"
                : $"Last day  {FormatCents(location.lastReport.operatingProfitCents)} profit";
            GUI.Label(
                new Rect(rect.x + 18f, rect.y + 78f, rect.width - 36f, 22f),
                latest,
                humanBodyStyle);
            GUI.Label(
                new Rect(rect.x + 18f, rect.y + 104f, rect.width - 36f, 18f),
                $"{staff}/3 team   •   {location.inventoryUnits}/{location.inventoryCapacityUnits} stock",
                humanSmallStyle);
        }

        private void DrawHumanPeople(Rect content)
        {
            PortfolioProgressionSnapshot snapshot = progression.CreateSnapshot();
            GUI.Label(
                new Rect(content.x, content.y, content.width, 40f),
                "Team",
                humanTitleStyle);
            GUI.Label(
                new Rect(content.x, content.y + 42f, content.width, 24f),
                "Assign clear roles, develop people, and keep each location ready to operate.",
                humanSmallStyle);
            DrawHumanLocationSelector(
                new Rect(content.x, content.y + 76f, content.width, 42f),
                snapshot);

            PortfolioLocationSnapshot selected = SelectedLocation(snapshot);
            PortfolioEmployeeSnapshot[] assigned = snapshot.employees
                .Where(employee => employee.assignedLocationId == selected.locationId)
                .OrderBy(employee => employee.role)
                .ToArray();
            PortfolioCandidateDefinition[] available =
                PortfolioProgressionRules.Candidates
                    .Where(candidate => !snapshot.employees.Any(employee =>
                        employee.employeeId == candidate.EmployeeId))
                    .ToArray();

            float employeeHeight = 172f;
            float candidateHeight = 142f;
            float scrollHeight =
                54f +
                Mathf.Max(1, assigned.Length) * employeeHeight +
                62f +
                Mathf.Max(1, available.Length) * candidateHeight +
                30f;
            Rect viewport = new(
                content.x,
                content.y + 132f,
                content.width,
                content.height - 132f);
            Rect view = new(0f, 0f, content.width - 22f, scrollHeight);
            peopleScroll = GUI.BeginScrollView(viewport, peopleScroll, view);

            GUI.Label(
                new Rect(0f, 0f, view.width, 26f),
                $"{selected.displayName.ToUpperInvariant()}  /  CURRENT TEAM",
                humanMetricStyle);
            float y = 38f;
            if (assigned.Length == 0)
            {
                DrawHumanPanel(new Rect(0f, y, view.width, 124f), NightSoft);
                GUI.Label(
                    new Rect(22f, y + 22f, view.width - 44f, 30f),
                    "No one is assigned here yet.",
                    humanSectionStyle);
                GUI.Label(
                    new Rect(22f, y + 58f, view.width - 44f, 44f),
                    "Hire a cashier, stock clerk, and manager to make this location independent.",
                    humanBodyStyle);
                y += employeeHeight;
            }
            else
            {
                foreach (PortfolioEmployeeSnapshot employee in assigned)
                {
                    DrawHumanEmployeeCard(
                        new Rect(0f, y, view.width, employeeHeight - 14f),
                        employee,
                        snapshot);
                    y += employeeHeight;
                }
            }

            GUI.Label(
                new Rect(0f, y + 6f, view.width, 26f),
                "AVAILABLE PEOPLE",
                humanMetricStyle);
            y += 44f;
            if (available.Length == 0)
            {
                DrawHumanPanel(new Rect(0f, y, view.width, 76f), NightSoft);
                GUI.Label(
                    new Rect(22f, y + 24f, view.width - 44f, 28f),
                    "Every available candidate is on your team.",
                    humanBodyStyle);
            }
            else
            {
                foreach (PortfolioCandidateDefinition candidate in available)
                {
                    DrawHumanCandidateCard(
                        new Rect(0f, y, view.width, candidateHeight - 14f),
                        candidate,
                        selected);
                    y += candidateHeight;
                }
            }
            GUI.EndScrollView();
        }

        private void DrawHumanEmployeeCard(
            Rect rect,
            PortfolioEmployeeSnapshot employee,
            PortfolioProgressionSnapshot snapshot)
        {
            DrawHumanPanel(rect, NightSoft);
            GUI.Label(
                new Rect(rect.x + 20f, rect.y + 14f, 300f, 30f),
                employee.displayName,
                humanSectionStyle);
            DrawHumanPill(
                new Rect(rect.x + 326f, rect.y + 15f, 120f, 25f),
                FriendlyRole(employee.role).ToUpperInvariant(),
                Teal);
            GUI.Label(
                new Rect(rect.x + 20f, rect.y + 48f, 430f, 21f),
                $"{employee.trait}   •   {FormatCents(employee.dailyWageCents)}/day",
                humanSmallStyle);

            float statX = rect.x + 20f;
            DrawHumanLabeledBar(
                new Rect(statX, rect.y + 82f, 220f, 38f),
                "SKILL",
                employee.skill);
            DrawHumanLabeledBar(
                new Rect(statX + 242f, rect.y + 82f, 220f, 38f),
                "RELIABILITY",
                employee.reliability);
            DrawHumanLabeledBar(
                new Rect(statX + 484f, rect.y + 82f, 220f, 38f),
                "MORALE",
                employee.satisfaction);

            float buttonsX = rect.xMax - 526f;
            if (DrawHumanButton(
                    new Rect(buttonsX, rect.y + 18f, 122f, 39f),
                    $"Train\n{FormatCents(PortfolioProgressionRules.TrainingCostCents)}"))
            {
                bool success = progression.TryTrainEmployee(
                    employee.employeeId,
                    out string error);
                RecordResult(
                    success,
                    success ? $"{employee.displayName} completed training." : error);
            }
            if (employee.role != PortfolioEmployeeRole.Manager &&
                DrawHumanButton(
                    new Rect(buttonsX + 132f, rect.y + 18f, 122f, 39f),
                    $"Promote\n{FormatCents(PortfolioProgressionRules.PromotionCostCents)}"))
            {
                bool success = progression.TryPromoteToManager(
                    employee.employeeId,
                    out string error);
                RecordResult(
                    success,
                    success ? $"{employee.displayName} is now a manager." : error);
            }
            if (DrawHumanButton(
                    new Rect(buttonsX + 264f, rect.y + 18f, 122f, 39f),
                    $"Focus\n{FriendlyFocus(employee.taskFocus)}"))
            {
                PortfolioTaskFocus next = (PortfolioTaskFocus)(
                    ((int)employee.taskFocus + 1) %
                    Enum.GetValues(typeof(PortfolioTaskFocus)).Length);
                bool success = progression.TrySetTaskFocus(
                    employee.employeeId,
                    next,
                    out string error);
                RecordResult(
                    success,
                    success
                        ? $"{employee.displayName} will focus on {FriendlyFocus(next).ToLowerInvariant()}."
                        : error);
            }
            PortfolioLocationSnapshot other = snapshot.locations.FirstOrDefault(location =>
                location.locationId != employee.assignedLocationId);
            if (other != null &&
                DrawHumanButton(
                    new Rect(buttonsX + 396f, rect.y + 18f, 122f, 39f),
                    $"Move\n{other.displayName}"))
            {
                bool success = progression.TryReassignEmployee(
                    employee.employeeId,
                    other.locationId,
                    out string error);
                RecordResult(
                    success,
                    success ? $"{employee.displayName} moved to {other.displayName}." : error);
            }
        }

        private void DrawHumanCandidateCard(
            Rect rect,
            PortfolioCandidateDefinition candidate,
            PortfolioLocationSnapshot location)
        {
            DrawHumanPanel(rect, NightSoft);
            GUI.Label(
                new Rect(rect.x + 20f, rect.y + 14f, 300f, 29f),
                candidate.DisplayName,
                humanSectionStyle);
            DrawHumanPill(
                new Rect(rect.x + 326f, rect.y + 15f, 120f, 25f),
                FriendlyRole(candidate.Role).ToUpperInvariant(),
                Amber);
            GUI.Label(
                new Rect(rect.x + 20f, rect.y + 48f, 520f, 22f),
                $"{candidate.Trait}   •   {FormatCents(candidate.DailyWageCents)}/day",
                humanSmallStyle);
            DrawHumanLabeledBar(
                new Rect(rect.x + 20f, rect.y + 80f, 220f, 38f),
                "SKILL",
                candidate.Skill);
            DrawHumanLabeledBar(
                new Rect(rect.x + 262f, rect.y + 80f, 220f, 38f),
                "RELIABILITY",
                candidate.Reliability);
            if (DrawHumanButton(
                    new Rect(rect.xMax - 214f, rect.y + 39f, 190f, 48f),
                    $"Hire for {location.displayName}\n{FormatCents(candidate.HiringCostCents)}",
                    true))
            {
                TryHireCandidate(candidate.EmployeeId, location.locationId, out _);
            }
        }

        private void DrawHumanLocations(Rect content)
        {
            PortfolioProgressionSnapshot snapshot = progression.CreateSnapshot();
            GUI.Label(
                new Rect(content.x, content.y, content.width, 40f),
                "Locations",
                humanTitleStyle);
            GUI.Label(
                new Rect(content.x, content.y + 42f, content.width, 24f),
                "Shape each operation with merchandise prices, inventory, staffing, and reporting controls.",
                humanSmallStyle);
            DrawHumanLocationSelector(
                new Rect(content.x, content.y + 76f, content.width, 42f),
                snapshot);

            PortfolioLocationSnapshot location = SelectedLocation(snapshot);
            float gap = 16f;
            float leftWidth = content.width * 0.58f;
            Rect operation = new(
                content.x,
                content.y + 138f,
                leftWidth,
                Mathf.Min(604f, content.height - 138f));
            Rect side = new(
                operation.xMax + gap,
                operation.y,
                content.xMax - operation.xMax - gap,
                operation.height);
            DrawHumanOperationCard(operation, location, snapshot);
            DrawHumanExpansionCard(side, snapshot);
        }

        private void DrawHumanOperationCard(
            Rect rect,
            PortfolioLocationSnapshot location,
            PortfolioProgressionSnapshot snapshot)
        {
            DrawHumanPanel(rect, NightSoft);
            GUI.Label(
                new Rect(rect.x + 24f, rect.y + 20f, rect.width - 48f, 31f),
                location.displayName,
                humanTitleStyle);
            GUI.Label(
                new Rect(rect.x + 24f, rect.y + 58f, rect.width - 48f, 22f),
                location.districtName,
                humanSmallStyle);
            GUI.Label(
                new Rect(rect.x + 24f, rect.y + 96f, rect.width - 48f, 56f),
                location.marketSummary,
                humanBodyStyle);

            int staff = snapshot.employees.Count(employee =>
                employee.assignedLocationId == location.locationId);
            float metricWidth = (rect.width - 64f) / 3f;
            DrawHumanCompactMetric(
                new Rect(rect.x + 24f, rect.y + 164f, metricWidth, 72f),
                "TEAM",
                $"{staff}/3");
            DrawHumanCompactMetric(
                new Rect(rect.x + 32f + metricWidth, rect.y + 164f, metricWidth, 72f),
                "INVENTORY",
                $"{location.inventoryUnits}/{location.inventoryCapacityUnits}");
            DrawHumanCompactMetric(
                new Rect(rect.x + 40f + metricWidth * 2f, rect.y + 164f, metricWidth, 72f),
                "DAILY RENT",
                FormatCents(location.dailyRentCents));

            GUI.Label(
                new Rect(rect.x + 24f, rect.y + 264f, rect.width - 48f, 24f),
                "PRICE PRESETS",
                humanMetricStyle);
            DrawHumanPricingControls(
                new Rect(rect.x + 24f, rect.y + 296f, rect.width - 48f, 44f),
                location);
            GUI.Label(
                new Rect(rect.x + 24f, rect.y + 347f, rect.width - 48f, 42f),
                $"{FormatCurrentMerchandisePrices(location)}\nPresets update all items; shelf tags edit one item.",
                humanSmallStyle);

            GUI.Label(
                new Rect(rect.x + 24f, rect.y + 410f, rect.width - 48f, 24f),
                "REORDERING",
                humanMetricStyle);
            DrawHumanReorderControls(
                new Rect(rect.x + 24f, rect.y + 442f, rect.width - 48f, 44f),
                location);
            GUI.Label(
                new Rect(rect.x + 24f, rect.y + 493f, rect.width - 48f, 42f),
                "Lean protects cash. Resilient protects availability. Managers follow this policy off-site.",
                humanSmallStyle);

            PurchaseOrderSnapshot activeOrder = snapshot.procurement?.orders?.FirstOrDefault(
                order =>
                    !order.IsTerminal &&
                    string.Equals(
                        order.locationId,
                        location.locationId,
                        StringComparison.Ordinal));
            GUI.Label(
                new Rect(rect.x + 24f, rect.y + 538f, rect.width - 220f, 22f),
                activeOrder == null
                    ? "No active purchase order"
                    : $"{activeOrder.orderId}  •  {FriendlyPolicy(activeOrder.status.ToString())}  •  due tick {activeOrder.fulfillAtTick}",
                humanSmallStyle);
            if (activeOrder?.status == PurchaseOrderStatus.Pending)
            {
                if (DrawHumanButton(
                        new Rect(rect.xMax - 184f, rect.y + 532f, 160f, 36f),
                        "Cancel order"))
                {
                    TryCancelPurchaseOrder(activeOrder.orderId, out _);
                }
            }
            else if (activeOrder == null &&
                     DrawHumanButton(
                         new Rect(rect.xMax - 184f, rect.y + 532f, 160f, 36f),
                         "Place order",
                         true))
            {
                TryPlaceManualPurchaseOrder(location.locationId, out _);
            }
        }

        private void DrawHumanExpansionCard(
            Rect rect,
            PortfolioProgressionSnapshot snapshot)
        {
            DrawHumanPanel(rect, NightSoft);
            if (snapshot.locations.Count > 1)
            {
                GUI.Label(
                    new Rect(rect.x + 24f, rect.y + 20f, rect.width - 48f, 32f),
                    "Portfolio active",
                    humanTitleStyle);
                GUI.Label(
                    new Rect(rect.x + 24f, rect.y + 66f, rect.width - 48f, 76f),
                    "Both locations use the same operating foundation while keeping their own market, team, policies, inventory, and results.",
                    humanBodyStyle);
                GUI.Label(
                    new Rect(rect.x + 24f, rect.y + 172f, rect.width - 48f, 22f),
                    "READINESS",
                    humanMetricStyle);
                float y = rect.y + 210f;
                foreach (PortfolioLocationSnapshot location in snapshot.locations)
                {
                    int staff = snapshot.employees.Count(employee =>
                        employee.assignedLocationId == location.locationId);
                    DrawHumanPanel(
                        new Rect(rect.x + 24f, y, rect.width - 48f, 82f),
                        NightRaised);
                    GUI.Label(
                        new Rect(rect.x + 40f, y + 13f, rect.width - 80f, 24f),
                        location.displayName,
                        humanSectionStyle);
                    GUI.Label(
                        new Rect(rect.x + 40f, y + 44f, rect.width - 80f, 21f),
                        staff >= 3 ? "Ready to operate off-site" : $"{staff}/3 roles filled",
                        humanSmallStyle);
                    y += 96f;
                }
                return;
            }

            GUI.Label(
                new Rect(rect.x + 24f, rect.y + 20f, rect.width - 48f, 32f),
                "Expansion",
                humanTitleStyle);
            GUI.Label(
                new Rect(rect.x + 24f, rect.y + 66f, rect.width - 48f, 54f),
                "Compare the market before committing company cash.",
                humanBodyStyle);

            float yPosition = rect.y + 134f;
            foreach (PortfolioLocationDefinition option in
                     PortfolioProgressionRules.ExpansionOptions)
            {
                Rect optionRect = new(
                    rect.x + 24f,
                    yPosition,
                    rect.width - 48f,
                    196f);
                DrawHumanPanel(optionRect, NightRaised);
                GUI.Label(
                    new Rect(optionRect.x + 18f, optionRect.y + 14f, optionRect.width - 36f, 28f),
                    option.DisplayName,
                    humanSectionStyle);
                GUI.Label(
                    new Rect(optionRect.x + 18f, optionRect.y + 43f, optionRect.width - 36f, 21f),
                    option.DistrictName,
                    humanSmallStyle);
                GUI.Label(
                    new Rect(optionRect.x + 18f, optionRect.y + 72f, optionRect.width - 36f, 44f),
                    $"Demand {option.BaseDemandUnits}   •   Competition {option.CompetitionIndex}/100\n" +
                    $"Rent {FormatCents(option.DailyRentCents)}/day",
                    humanBodyStyle);
                long committed = option.LeaseCostCents + option.OpeningInventoryCostCents;
                if (DrawHumanButton(
                        new Rect(
                            optionRect.x + 18f,
                            optionRect.yMax - 54f,
                            optionRect.width - 36f,
                            38f),
                        $"Open here  •  {FormatCents(committed)}",
                        true))
                {
                    TryLeaseLocation(option.LocationId, out _);
                }
                yPosition += 210f;
            }
        }

        private void DrawHumanPricingControls(
            Rect rect,
            PortfolioLocationSnapshot location)
        {
            PortfolioPricingPolicy[] policies =
                (PortfolioPricingPolicy[])Enum.GetValues(typeof(PortfolioPricingPolicy));
            float gap = 8f;
            float width = (rect.width - gap * (policies.Length - 1)) / policies.Length;
            for (int i = 0; i < policies.Length; i++)
            {
                PortfolioPricingPolicy policy = policies[i];
                bool selected = IsPricePresetApplied(location, policy);
                if (DrawHumanButton(
                        new Rect(rect.x + (width + gap) * i, rect.y, width, rect.height),
                        FriendlyPolicy(policy.ToString()),
                        selected,
                        true,
                        selected))
                {
                    bool success = TrySetPricingPreset(
                        location.locationId,
                        policy,
                        out string error);
                    RecordResult(
                        success,
                        success
                            ? $"{location.displayName} pricing changed to {FriendlyPolicy(policy.ToString()).ToLowerInvariant()}."
                            : error);
                }
            }
        }

        private void DrawHumanReorderControls(
            Rect rect,
            PortfolioLocationSnapshot location)
        {
            PortfolioReorderPolicy[] policies =
                (PortfolioReorderPolicy[])Enum.GetValues(typeof(PortfolioReorderPolicy));
            float gap = 8f;
            float width = (rect.width - gap * (policies.Length - 1)) / policies.Length;
            for (int i = 0; i < policies.Length; i++)
            {
                PortfolioReorderPolicy policy = policies[i];
                bool selected = policy == location.reorderPolicy;
                if (DrawHumanButton(
                        new Rect(rect.x + (width + gap) * i, rect.y, width, rect.height),
                        FriendlyPolicy(policy.ToString()),
                        selected,
                        true,
                        selected))
                {
                    bool success = progression.TrySetReorderPolicy(
                        location.locationId,
                        policy,
                        out string error);
                    RecordResult(
                        success,
                        success
                            ? $"{location.displayName} reordering changed to {FriendlyPolicy(policy.ToString()).ToLowerInvariant()}."
                            : error);
                }
            }
        }

        private void DrawHumanReports(Rect content)
        {
            PortfolioProgressionSnapshot snapshot = progression.CreateSnapshot();
            GUI.Label(
                new Rect(content.x, content.y, content.width, 40f),
                "Reports",
                humanTitleStyle);
            GUI.Label(
                new Rect(content.x, content.y + 42f, content.width, 24f),
                "Performance first, with the causes close enough to act on.",
                humanSmallStyle);

            long sales = snapshot.locations.Sum(location =>
                location.lifetimeGrossSalesCents);
            long profit = snapshot.locations.Sum(location =>
                location.lifetimeOperatingProfitCents);
            long payroll = snapshot.locations.Sum(location =>
                location.lifetimePayrollCents);
            float gap = 14f;
            float metricWidth = (content.width - gap * 3f) / 4f;
            DrawHumanMetricCard(
                new Rect(content.x, content.y + 80f, metricWidth, 94f),
                "LIFETIME SALES",
                FormatCents(sales),
                Teal);
            DrawHumanMetricCard(
                new Rect(content.x + metricWidth + gap, content.y + 80f, metricWidth, 94f),
                "OPERATING PROFIT",
                FormatCents(profit),
                profit >= 0 ? Teal : Error);
            DrawHumanMetricCard(
                new Rect(content.x + (metricWidth + gap) * 2f, content.y + 80f, metricWidth, 94f),
                "PAYROLL",
                FormatCents(payroll),
                Ink);
            DrawHumanMetricCard(
                new Rect(content.x + (metricWidth + gap) * 3f, content.y + 80f, metricWidth, 94f),
                "CASH",
                FormatCents(snapshot.cashCents),
                snapshot.cashCents >= 0 ? Teal : Error);

            Rect viewport = new(
                content.x,
                content.y + 198f,
                content.width,
                content.height - 198f);
            float cardHeight = 284f;
            Rect view = new(
                0f,
                0f,
                content.width - 22f,
                snapshot.locations.Count * (cardHeight + 14f));
            reportScroll = GUI.BeginScrollView(viewport, reportScroll, view);
            float y = 0f;
            foreach (PortfolioLocationSnapshot location in snapshot.locations)
            {
                DrawHumanReportCard(
                    new Rect(0f, y, view.width, cardHeight),
                    location);
                y += cardHeight + 14f;
            }
            GUI.EndScrollView();
        }

        private void DrawHumanReportCard(
            Rect rect,
            PortfolioLocationSnapshot location)
        {
            DrawHumanPanel(rect, NightSoft);
            GUI.Label(
                new Rect(rect.x + 22f, rect.y + 17f, 360f, 29f),
                location.displayName,
                humanSectionStyle);
            GUI.Label(
                new Rect(rect.x + 22f, rect.y + 47f, 360f, 20f),
                location.districtName,
                humanSmallStyle);
            PortfolioLocationReportSnapshot report = location.lastReport;
            if (report == null)
            {
                GUI.Label(
                    new Rect(rect.x + 22f, rect.y + 101f, rect.width - 44f, 64f),
                    "No delegated day has been reported yet. Once a manager runs this location, its operating results will appear here.",
                    humanBodyStyle);
                return;
            }

            DrawHumanPill(
                new Rect(rect.xMax - 108f, rect.y + 18f, 86f, 25f),
                $"DAY {report.day}",
                Teal);
            float metricWidth = (rect.width - 88f) / 4f;
            DrawHumanCompactMetric(
                new Rect(rect.x + 22f, rect.y + 84f, metricWidth, 66f),
                "SALES",
                FormatCents(report.grossSalesCents));
            DrawHumanCompactMetric(
                new Rect(rect.x + 36f + metricWidth, rect.y + 84f, metricWidth, 66f),
                "PROFIT",
                FormatCents(report.operatingProfitCents));
            DrawHumanCompactMetric(
                new Rect(rect.x + 50f + metricWidth * 2f, rect.y + 84f, metricWidth, 66f),
                "SOLD",
                report.unitsSold.ToString());
            DrawHumanCompactMetric(
                new Rect(rect.x + 64f + metricWidth * 3f, rect.y + 84f, metricWidth, 66f),
                "LOST DEMAND",
                report.lostDemandUnits.ToString());

            GUI.Label(
                new Rect(rect.x + 22f, rect.y + 172f, 116f, 19f),
                "DEMAND MET",
                humanMetricStyle);
            float demandMet = report.demandUnits > 0
                ? report.unitsSold / (float)report.demandUnits
                : 1f;
            DrawHumanBar(
                new Rect(rect.x + 144f, rect.y + 177f, rect.width - 166f, 10f),
                demandMet,
                demandMet >= 0.85f ? Teal : Amber);
            GUI.Label(
                new Rect(rect.x + 22f, rect.y + 207f, rect.width - 44f, 50f),
                $"What moved the result: {FriendlySentence(report.primaryCause)}",
                humanBodyStyle);
        }

        private void DrawHumanLocationSelector(
            Rect rect,
            PortfolioProgressionSnapshot snapshot)
        {
            float x = rect.x;
            foreach (PortfolioLocationSnapshot location in snapshot.locations)
            {
                float width = Mathf.Min(
                    230f,
                    Mathf.Max(170f, (rect.width - 10f) / snapshot.locations.Count));
                bool selected = selectedLocationId == location.locationId;
                if (DrawHumanButton(
                        new Rect(x, rect.y, width, rect.height),
                        location.displayName,
                        selected,
                        true,
                        selected))
                {
                    selectedLocationId = location.locationId;
                }
                x += width + 10f;
            }
        }

        private void DrawHumanCompactMetric(
            Rect rect,
            string label,
            string value)
        {
            DrawHumanPanel(rect, NightRaised);
            GUI.Label(
                new Rect(rect.x + 12f, rect.y + 9f, rect.width - 24f, 17f),
                label,
                humanMetricStyle);
            GUI.Label(
                new Rect(rect.x + 12f, rect.y + 29f, rect.width - 24f, 27f),
                value,
                humanSectionStyle);
        }

        private void DrawHumanLabeledBar(Rect rect, string label, int value)
        {
            GUI.Label(
                new Rect(rect.x, rect.y, rect.width, 18f),
                $"{label}  {value}",
                humanMetricStyle);
            DrawHumanBar(
                new Rect(rect.x, rect.y + 23f, rect.width, 9f),
                value / 100f,
                value >= 65 ? Teal : value >= 40 ? Amber : Error);
        }

        private static void DrawHumanBar(Rect rect, float amount, Color color)
        {
            DrawHumanPanel(rect, NightRaised);
            DrawHumanPanel(
                new Rect(
                    rect.x,
                    rect.y,
                    rect.width * Mathf.Clamp01(amount),
                    rect.height),
                color);
        }

        private void DrawHumanPill(Rect rect, string text, Color color)
        {
            DrawHumanPanel(
                rect,
                new Color(color.r, color.g, color.b, 0.2f));
            Color prior = GUI.contentColor;
            GUI.contentColor = color;
            GUI.Label(rect, text, humanCenteredStyle);
            GUI.contentColor = prior;
        }

        private bool DrawHumanButton(
            Rect rect,
            string label,
            bool primary = false,
            bool enabled = true,
            bool selected = false)
        {
            bool hovered = enabled && rect.Contains(Event.current.mousePosition);
            Color fill = selected
                ? new Color(Teal.r, Teal.g, Teal.b, 0.32f)
                : primary
                    ? hovered
                        ? new Color(0.2f, 0.92f, 0.8f, 1f)
                        : Teal
                    : hovered
                        ? NightRaised * 1.2f
                        : NightRaised;
            if (!enabled)
            {
                fill = new Color(
                    NightRaised.r,
                    NightRaised.g,
                    NightRaised.b,
                    0.5f);
            }
            DrawHumanPanel(rect, fill);
            bool priorEnabled = GUI.enabled;
            GUI.enabled = enabled;
            bool clicked = GUI.Button(
                rect,
                label,
                primary && !selected
                    ? humanButtonPrimaryStyle
                    : humanButtonStyle);
            GUI.enabled = priorEnabled;
            return clicked;
        }

        private void DrawHumanActionToast(float width, float height)
        {
            if (Time.unscaledTime > lastActionUntil ||
                string.IsNullOrWhiteSpace(lastAction))
            {
                return;
            }

            float toastWidth = Mathf.Min(680f, width - 300f);
            Rect rect = new(
                width - toastWidth - 28f,
                height - 76f,
                toastWidth,
                50f);
            Color accent = lastActionSucceeded ? Teal : Error;
            DrawHumanPanel(rect, NightRaised);
            DrawHumanPanel(new Rect(rect.x, rect.y, 4f, rect.height), accent);
            GUI.Label(
                new Rect(rect.x + 18f, rect.y + 12f, rect.width - 32f, 28f),
                FriendlySentence(lastAction),
                humanBodyStyle);
        }

        private static string FriendlyFocus(PortfolioTaskFocus focus)
        {
            string value = focus.ToString();
            return FriendlyPolicy(value);
        }

        private static string FriendlyPolicy(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Balanced";
            }

            System.Text.StringBuilder builder = new();
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (i > 0 &&
                    char.IsUpper(character) &&
                    !char.IsUpper(value[i - 1]))
                {
                    builder.Append(' ');
                }
                builder.Append(character);
            }
            return builder.ToString();
        }

        private static string FriendlySentence(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "No additional detail is available.";
            }

            string friendly = value
                .Replace("CAUSE:", string.Empty)
                .Replace("[DONE]", string.Empty)
                .Trim();
            if (friendly.Length == 0)
            {
                return "No additional detail is available.";
            }
            return char.ToUpperInvariant(friendly[0]) + friendly.Substring(1);
        }

        private PortfolioLocationSnapshot SelectedLocation(
            PortfolioProgressionSnapshot snapshot)
        {
            PortfolioLocationSnapshot selected = snapshot.locations.FirstOrDefault(location =>
                location.locationId == selectedLocationId);
            if (selected != null)
            {
                return selected;
            }

            selected = snapshot.locations[0];
            selectedLocationId = selected.locationId;
            return selected;
        }

        private string LocationName(string locationId)
        {
            PortfolioLocationSnapshot location = progression.Locations.FirstOrDefault(value =>
                value.locationId == locationId);
            if (location != null)
            {
                return location.displayName;
            }

            return PortfolioProgressionRules.TryGetLocationDefinition(
                locationId,
                out PortfolioLocationDefinition definition)
                    ? definition.DisplayName
                    : locationId;
        }

        private static bool IsFullyStaffed(
            PortfolioProgressionSnapshot snapshot,
            string locationId)
        {
            PortfolioEmployeeRole[] roles =
            {
                PortfolioEmployeeRole.Cashier,
                PortfolioEmployeeRole.StockClerk,
                PortfolioEmployeeRole.Manager
            };
            return roles.All(role => snapshot.employees.Any(employee =>
                employee.role == role && employee.assignedLocationId == locationId));
        }

        private static void DrawCheck(bool complete, string text)
        {
            GUILayout.Label($"{(complete ? "[DONE]" : "[    ]")}  {text}");
        }

        private void RecordResult(bool success, string message)
        {
            Record(string.IsNullOrWhiteSpace(message) ? "Action unavailable." : message, success);
        }

        private void Record(string message, bool success)
        {
            lastAction = message;
            lastActionSucceeded = success;
            lastActionUntil = Time.unscaledTime + (success ? 4.5f : 7f);
            if (success)
            {
                Debug.Log(message, this);
            }
            else
            {
                Debug.LogWarning(message, this);
            }
        }

        private static string FriendlyRole(PortfolioEmployeeRole role)
        {
            return role switch
            {
                PortfolioEmployeeRole.StockClerk => "Stock clerk",
                PortfolioEmployeeRole.Manager => "Manager",
                _ => "Cashier"
            };
        }

        private static string FormatCents(long cents)
        {
            bool negative = cents < 0;
            ulong absolute = negative
                ? (ulong)(-(cents + 1)) + 1UL
                : (ulong)cents;
            string dollars = (absolute / 100).ToString(
                "N0",
                System.Globalization.CultureInfo.InvariantCulture);
            return negative
                ? $"-${dollars}.{absolute % 100:00}"
                : $"${dollars}.{absolute % 100:00}";
        }

        private static string FormatCurrentMerchandisePrices(
            PortfolioLocationSnapshot location)
        {
            if (location?.merchandisePrices == null ||
                location.merchandisePrices.Count == 0)
            {
                return "no merchandise prices";
            }

            return string.Join(
                "  •  ",
                location.merchandisePrices
                    .Where(price => price != null)
                    .OrderBy(price => price.productId, StringComparer.Ordinal)
                    .Select(price =>
                        $"{FriendlyProduct(price.productId)} {FormatCents(price.salePriceCents)}"));
        }

        private static bool IsPricePresetApplied(
            PortfolioLocationSnapshot location,
            PortfolioPricingPolicy preset)
        {
            return location?.merchandisePrices != null &&
                   location.merchandisePrices.Count > 0 &&
                   location.merchandisePrices.All(price =>
                       price != null &&
                       price.salePriceCents ==
                       MerchandisingRules.CalculatePresetSalePrice(
                           price.referencePriceCents,
                           preset));
        }

        private static string FormatReportMerchandisePrices(
            PortfolioLocationReportSnapshot report)
        {
            if (report?.hasExactMerchandiseSales == true &&
                report.merchandiseSales != null &&
                report.merchandiseSales.Count > 0)
            {
                return string.Join(
                    ", ",
                    report.merchandiseSales
                        .Where(line => line != null)
                        .OrderBy(line => line.productId, StringComparer.Ordinal)
                        .ThenBy(line => line.unitPriceCents)
                        .Select(line =>
                            $"{FriendlyProduct(line.productId)} {FormatCents(line.unitPriceCents)} × {line.quantityUnits}"));
            }

            return report == null || report.unitPriceCents <= 0
                ? "not recorded"
                : FormatCents(report.unitPriceCents);
        }

        private static string FriendlyProduct(string productId)
        {
            return productId switch
            {
                "prod-cola-can-355ml" => "Cola",
                "prod-potato-chips-small" => "Chips",
                _ => productId ?? "Product"
            };
        }
    }
}
