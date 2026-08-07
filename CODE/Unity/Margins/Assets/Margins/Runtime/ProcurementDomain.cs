using System;
using System.Collections.Generic;
using System.Linq;

namespace Margins
{
    public enum PurchaseOrderStatus
    {
        Pending = 0,
        Fulfilled = 1,
        Delivered = 2,
        PartiallyReceived = 3,
        Canceled = 4,
        Completed = 5
    }

    public sealed class ProcurementResourceDefinition
    {
        public ProcurementResourceDefinition(
            string resourceId,
            long unitCostCents,
            int orderMultipleUnits = 1)
        {
            if (!StableIdentifier.IsValid(resourceId) ||
                unitCostCents < 0 || orderMultipleUnits <= 0)
            {
                throw new ArgumentException(
                    "Procurement resources require a stable id, nonnegative price, and positive order multiple.");
            }

            ResourceId = resourceId;
            UnitCostCents = unitCostCents;
            OrderMultipleUnits = orderMultipleUnits;
        }

        public string ResourceId { get; }
        public long UnitCostCents { get; }
        public int OrderMultipleUnits { get; }
    }

    public sealed class ProcurementSupplierDefinition
    {
        private readonly Dictionary<string, ProcurementResourceDefinition> resources;

        public ProcurementSupplierDefinition(
            string supplierId,
            string containerDefinitionId,
            long deliveryFeeCents,
            int fulfillmentDelayTicks,
            int maximumOrderUnits,
            IEnumerable<ProcurementResourceDefinition> resources)
        {
            if (!StableIdentifier.IsValid(supplierId) ||
                !StableIdentifier.IsValid(containerDefinitionId) ||
                deliveryFeeCents < 0 || fulfillmentDelayTicks < 0 ||
                maximumOrderUnits <= 0 || resources == null)
            {
                throw new ArgumentException(
                    "Suppliers require stable ids and nonnegative bounded fulfillment rules.");
            }

            Dictionary<string, ProcurementResourceDefinition> captured =
                new(StringComparer.Ordinal);
            foreach (ProcurementResourceDefinition resource in resources)
            {
                if (resource == null ||
                    !captured.TryAdd(resource.ResourceId, resource))
                {
                    throw new ArgumentException(
                        "Supplier resources cannot be null or duplicated.",
                        nameof(resources));
                }
            }

            if (captured.Count == 0)
            {
                throw new ArgumentException(
                    "A supplier must expose at least one configured resource.",
                    nameof(resources));
            }

            SupplierId = supplierId;
            ContainerDefinitionId = containerDefinitionId;
            DeliveryFeeCents = deliveryFeeCents;
            FulfillmentDelayTicks = fulfillmentDelayTicks;
            MaximumOrderUnits = maximumOrderUnits;
            this.resources = captured;
        }

        public string SupplierId { get; }
        public string ContainerDefinitionId { get; }
        public long DeliveryFeeCents { get; }
        public int FulfillmentDelayTicks { get; }
        public int MaximumOrderUnits { get; }
        public IReadOnlyDictionary<string, ProcurementResourceDefinition> Resources =>
            resources;

        public bool TryGetResource(
            string resourceId,
            out ProcurementResourceDefinition resource)
        {
            resource = null;
            return StableIdentifier.IsValid(resourceId) &&
                   resources.TryGetValue(resourceId, out resource);
        }
    }

    public sealed class ProcurementCatalog
    {
        private readonly Dictionary<string, ProcurementSupplierDefinition> suppliers;

        public ProcurementCatalog(
            IEnumerable<ProcurementSupplierDefinition> suppliers)
        {
            if (suppliers == null)
            {
                throw new ArgumentNullException(nameof(suppliers));
            }

            this.suppliers = new Dictionary<string, ProcurementSupplierDefinition>(
                StringComparer.Ordinal);
            foreach (ProcurementSupplierDefinition supplier in suppliers)
            {
                if (supplier == null ||
                    !this.suppliers.TryAdd(supplier.SupplierId, supplier))
                {
                    throw new ArgumentException(
                        "Procurement suppliers cannot be null or duplicated.",
                        nameof(suppliers));
                }
            }

            if (this.suppliers.Count == 0)
            {
                throw new ArgumentException(
                    "A procurement catalog requires at least one supplier.",
                    nameof(suppliers));
            }
        }

        public bool TryGetSupplier(
            string supplierId,
            out ProcurementSupplierDefinition supplier)
        {
            supplier = null;
            return StableIdentifier.IsValid(supplierId) &&
                   suppliers.TryGetValue(supplierId, out supplier);
        }
    }

    public readonly struct ProcurementOrderRequestLine
    {
        public ProcurementOrderRequestLine(string resourceId, int quantityUnits)
        {
            ResourceId = resourceId;
            QuantityUnits = quantityUnits;
        }

        public string ResourceId { get; }
        public int QuantityUnits { get; }
    }

    [Serializable]
    public sealed class PurchaseOrderLineSnapshot
    {
        public string resourceId;
        public int orderedQuantityUnits;
        public int receivedQuantityUnits;
        public long unitCostCents;
        public long lineCostCents;
    }

    [Serializable]
    public sealed class PurchaseOrderSnapshot
    {
        public string orderId;
        public string locationId;
        public string supplierId;
        public string containerDefinitionId;
        public long placedAtTick;
        public long fulfillAtTick;
        public PurchaseOrderStatus status;
        public long subtotalCostCents;
        public long deliveryFeeCents;
        public long totalCostCents;
        public long refundedCents;
        public string paymentId;
        public string fulfillmentId;
        public string deliveryId;
        public string inventoryReceiptId;
        public string completionReceiptId;
        public string cancellationId;
        public List<PurchaseOrderLineSnapshot> lines = new();

        public int OrderedQuantityUnits => lines?.Sum(line =>
            line?.orderedQuantityUnits ?? 0) ?? 0;

        public int ReceivedQuantityUnits => lines?.Sum(line =>
            line?.receivedQuantityUnits ?? 0) ?? 0;

        public bool IsTerminal =>
            status == PurchaseOrderStatus.Canceled ||
            status == PurchaseOrderStatus.Completed;
    }

    [Serializable]
    public sealed class ProcurementSnapshot
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public long currentTick;
        public int nextOrderOrdinal = 1;
        public List<PurchaseOrderSnapshot> orders = new();
    }

    /// <summary>
    /// Owns purchase-order identity and lifecycle. Cash, inventory, physical
    /// containers, and employee work remain owned by their existing systems.
    /// Callers commit this snapshot together with those authorities.
    /// </summary>
    public sealed class ProcurementLedger
    {
        private readonly ProcurementCatalog catalog;
        private ProcurementSnapshot state;

        private ProcurementLedger(
            ProcurementCatalog catalog,
            ProcurementSnapshot state)
        {
            this.catalog = catalog;
            this.state = CopySnapshot(state);
        }

        public long CurrentTick => state.currentTick;
        public IReadOnlyList<PurchaseOrderSnapshot> Orders =>
            state.orders.Select(Clone).ToList().AsReadOnly();

        public static ProcurementLedger CreateInitial(ProcurementCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            return new ProcurementLedger(catalog, new ProcurementSnapshot());
        }

        public static bool TryRestore(
            ProcurementCatalog catalog,
            ProcurementSnapshot snapshot,
            out ProcurementLedger ledger,
            out string error)
        {
            ledger = null;
            if (!TryValidateSnapshot(catalog, snapshot, out error))
            {
                return false;
            }

            ledger = new ProcurementLedger(catalog, snapshot);
            return true;
        }

        public static bool TryValidateSnapshot(
            ProcurementCatalog catalog,
            ProcurementSnapshot snapshot,
            out string error)
        {
            if (catalog == null || snapshot == null ||
                snapshot.version != ProcurementSnapshot.CurrentVersion ||
                snapshot.currentTick < 0 || snapshot.nextOrderOrdinal < 1 ||
                snapshot.orders == null)
            {
                error = "Procurement snapshot version, clock, or order collection is invalid.";
                return false;
            }

            HashSet<string> orderIds = new(StringComparer.Ordinal);
            HashSet<string> eventIds = new(StringComparer.Ordinal);
            HashSet<string> activeLocations = new(StringComparer.Ordinal);
            int greatestOrdinal = 0;
            foreach (PurchaseOrderSnapshot order in snapshot.orders)
            {
                if (!TryValidateOrder(
                        catalog,
                        order,
                        snapshot.currentTick,
                        eventIds,
                        out int ordinal,
                        out error) ||
                    !orderIds.Add(order.orderId))
                {
                    error ??= "Procurement snapshot contains duplicate order ids.";
                    return false;
                }

                greatestOrdinal = Math.Max(greatestOrdinal, ordinal);
                if (!order.IsTerminal && !activeLocations.Add(order.locationId))
                {
                    error =
                        $"Location '{order.locationId}' has more than one active purchase order.";
                    return false;
                }
            }

            if (snapshot.nextOrderOrdinal <= greatestOrdinal)
            {
                error = "Procurement next-order ordinal would reuse a stable order id.";
                return false;
            }

            error = null;
            return true;
        }

        public ProcurementSnapshot CreateSnapshot()
        {
            return CopySnapshot(state);
        }

        public bool TryGetOrder(
            string orderId,
            out PurchaseOrderSnapshot order)
        {
            PurchaseOrderSnapshot found = state.orders.FirstOrDefault(value =>
                string.Equals(value.orderId, orderId, StringComparison.Ordinal));
            order = Clone(found);
            return order != null;
        }

        public bool TryGetActiveOrderForLocation(
            string locationId,
            out PurchaseOrderSnapshot order)
        {
            PurchaseOrderSnapshot found = state.orders.FirstOrDefault(value =>
                !value.IsTerminal &&
                string.Equals(
                    value.locationId,
                    locationId,
                    StringComparison.Ordinal));
            order = Clone(found);
            return order != null;
        }

        public bool TryPlaceOrder(
            string locationId,
            string supplierId,
            IReadOnlyList<ProcurementOrderRequestLine> requestedLines,
            out PurchaseOrderSnapshot order,
            out long chargeCents,
            out string error)
        {
            order = null;
            chargeCents = 0;
            if (!StableIdentifier.IsValid(locationId) ||
                !catalog.TryGetSupplier(
                    supplierId,
                    out ProcurementSupplierDefinition supplier) ||
                requestedLines == null || requestedLines.Count == 0)
            {
                error = "Purchase orders require a valid location, supplier, and at least one line.";
                return false;
            }

            if (TryGetActiveOrderForLocation(locationId, out _))
            {
                error = $"Location '{locationId}' already has an active purchase order.";
                return false;
            }

            if (state.nextOrderOrdinal == int.MaxValue)
            {
                error = "Purchase order identity storage is exhausted.";
                return false;
            }

            Dictionary<string, int> quantities = new(StringComparer.Ordinal);
            int totalUnits = 0;
            foreach (ProcurementOrderRequestLine line in requestedLines)
            {
                if (!supplier.TryGetResource(
                        line.ResourceId,
                        out ProcurementResourceDefinition resource) ||
                    line.QuantityUnits <= 0 ||
                    line.QuantityUnits % resource.OrderMultipleUnits != 0 ||
                    !quantities.TryAdd(line.ResourceId, line.QuantityUnits))
                {
                    error = "Purchase order lines contain an unavailable, duplicated, or invalid resource quantity.";
                    return false;
                }

                try
                {
                    totalUnits = checked(totalUnits + line.QuantityUnits);
                }
                catch (OverflowException)
                {
                    error = "Purchase order quantity exceeds supported integer storage.";
                    return false;
                }
            }

            if (totalUnits > supplier.MaximumOrderUnits)
            {
                error =
                    $"Supplier '{supplier.SupplierId}' accepts at most {supplier.MaximumOrderUnits} units per order.";
                return false;
            }

            PurchaseOrderSnapshot candidate = new()
            {
                orderId = $"purchase-order-{state.nextOrderOrdinal:000000}",
                locationId = locationId,
                supplierId = supplier.SupplierId,
                containerDefinitionId = supplier.ContainerDefinitionId,
                placedAtTick = state.currentTick,
                status = PurchaseOrderStatus.Pending,
                deliveryFeeCents = supplier.DeliveryFeeCents
            };
            try
            {
                candidate.fulfillAtTick = checked(
                    state.currentTick + supplier.FulfillmentDelayTicks);
                foreach (KeyValuePair<string, int> quantity in quantities
                             .OrderBy(value => value.Key, StringComparer.Ordinal))
                {
                    ProcurementResourceDefinition resource =
                        supplier.Resources[quantity.Key];
                    long lineCost = checked(
                        resource.UnitCostCents * quantity.Value);
                    candidate.lines.Add(new PurchaseOrderLineSnapshot
                    {
                        resourceId = resource.ResourceId,
                        orderedQuantityUnits = quantity.Value,
                        receivedQuantityUnits = 0,
                        unitCostCents = resource.UnitCostCents,
                        lineCostCents = lineCost
                    });
                    candidate.subtotalCostCents = checked(
                        candidate.subtotalCostCents + lineCost);
                }
                candidate.totalCostCents = checked(
                    candidate.subtotalCostCents + candidate.deliveryFeeCents);
            }
            catch (OverflowException)
            {
                error = "Purchase order cost or fulfillment timing overflowed supported storage.";
                return false;
            }

            candidate.paymentId = $"payment-{candidate.orderId}";
            state.orders.Add(candidate);
            state.orders.Sort((left, right) =>
                string.CompareOrdinal(left.orderId, right.orderId));
            state.nextOrderOrdinal++;
            order = Clone(candidate);
            chargeCents = candidate.totalCostCents;
            error = null;
            return true;
        }

        public bool TryCancelOrder(
            string orderId,
            out long refundCents,
            out bool unchanged,
            out string error)
        {
            refundCents = 0;
            unchanged = false;
            PurchaseOrderSnapshot order = FindOrder(orderId);
            if (order == null)
            {
                error = "Purchase order is unavailable.";
                return false;
            }

            if (order.status == PurchaseOrderStatus.Canceled)
            {
                unchanged = true;
                error = null;
                return true;
            }

            if (order.status != PurchaseOrderStatus.Pending)
            {
                error = "Only a pending purchase order can be canceled.";
                return false;
            }

            order.status = PurchaseOrderStatus.Canceled;
            order.cancellationId = $"cancellation-{order.orderId}";
            order.refundedCents = order.totalCostCents;
            refundCents = order.refundedCents;
            error = null;
            return true;
        }

        public bool TryAdvanceTicks(
            int elapsedTicks,
            out int fulfilledOrderCount,
            out string error)
        {
            fulfilledOrderCount = 0;
            if (elapsedTicks <= 0)
            {
                error = "Procurement clock advances require positive elapsed ticks.";
                return false;
            }

            long targetTick;
            try
            {
                targetTick = checked(state.currentTick + elapsedTicks);
            }
            catch (OverflowException)
            {
                error = "Procurement clock overflowed supported tick storage.";
                return false;
            }

            foreach (PurchaseOrderSnapshot order in state.orders
                         .OrderBy(value => value.orderId, StringComparer.Ordinal))
            {
                if (order.status != PurchaseOrderStatus.Pending ||
                    order.fulfillAtTick > targetTick)
                {
                    continue;
                }

                order.status = PurchaseOrderStatus.Fulfilled;
                order.fulfillmentId = $"fulfillment-{order.orderId}";
                fulfilledOrderCount++;
            }

            state.currentTick = targetTick;
            error = null;
            return true;
        }

        public bool TryCreateDelivery(
            string orderId,
            out PurchaseOrderSnapshot orderSnapshot,
            out bool unchanged,
            out string error)
        {
            orderSnapshot = null;
            unchanged = false;
            PurchaseOrderSnapshot order = FindOrder(orderId);
            if (order == null)
            {
                error = "Purchase order is unavailable.";
                return false;
            }

            if (order.status == PurchaseOrderStatus.Delivered ||
                order.status == PurchaseOrderStatus.PartiallyReceived ||
                order.status == PurchaseOrderStatus.Completed)
            {
                orderSnapshot = Clone(order);
                unchanged = true;
                error = null;
                return true;
            }

            if (order.status != PurchaseOrderStatus.Fulfilled)
            {
                error = "Only a fulfilled purchase order can create a delivery.";
                return false;
            }

            order.status = PurchaseOrderStatus.Delivered;
            order.deliveryId = $"delivery-{order.orderId}";
            order.inventoryReceiptId = $"inventory-receipt-{order.orderId}";
            orderSnapshot = Clone(order);
            error = null;
            return true;
        }

        public bool TryRecordAbsoluteReceipt(
            string orderId,
            IReadOnlyDictionary<string, int> receivedQuantities,
            out PurchaseOrderSnapshot orderSnapshot,
            out bool unchanged,
            out string error)
        {
            orderSnapshot = null;
            unchanged = false;
            PurchaseOrderSnapshot order = FindOrder(orderId);
            if (order == null || receivedQuantities == null)
            {
                error = "Purchase order receipt is unavailable.";
                return false;
            }

            if (order.status != PurchaseOrderStatus.Delivered &&
                order.status != PurchaseOrderStatus.PartiallyReceived &&
                order.status != PurchaseOrderStatus.Completed)
            {
                error = "Only a delivered purchase order can be received.";
                return false;
            }

            if (receivedQuantities.Count != order.lines.Count)
            {
                error = "Purchase order receipt must reconcile every order line.";
                return false;
            }

            bool changed = false;
            bool allReceived = true;
            foreach (PurchaseOrderLineSnapshot line in order.lines)
            {
                if (!receivedQuantities.TryGetValue(
                        line.resourceId,
                        out int received) ||
                    received < line.receivedQuantityUnits ||
                    received > line.orderedQuantityUnits)
                {
                    error = "Purchase order receipt moved backward or exceeded an ordered quantity.";
                    return false;
                }

                changed |= received != line.receivedQuantityUnits;
                allReceived &= received == line.orderedQuantityUnits;
            }

            if (order.status == PurchaseOrderStatus.Completed && !allReceived)
            {
                error = "A completed purchase order cannot return to partial receipt.";
                return false;
            }

            foreach (PurchaseOrderLineSnapshot line in order.lines)
            {
                line.receivedQuantityUnits = receivedQuantities[line.resourceId];
            }

            PurchaseOrderStatus targetStatus = allReceived
                ? PurchaseOrderStatus.Completed
                : order.lines.Any(line => line.receivedQuantityUnits > 0)
                    ? PurchaseOrderStatus.PartiallyReceived
                    : PurchaseOrderStatus.Delivered;
            changed |= targetStatus != order.status;
            order.status = targetStatus;
            if (allReceived && NoEvent(order.completionReceiptId))
            {
                order.completionReceiptId =
                    $"completion-receipt-{order.orderId}";
            }

            unchanged = !changed;
            orderSnapshot = Clone(order);
            error = null;
            return true;
        }

        private PurchaseOrderSnapshot FindOrder(string orderId)
        {
            if (!StableIdentifier.IsValid(orderId))
            {
                return null;
            }

            return state.orders.FirstOrDefault(value => string.Equals(
                value.orderId,
                orderId,
                StringComparison.Ordinal));
        }

        private static bool TryValidateOrder(
            ProcurementCatalog catalog,
            PurchaseOrderSnapshot order,
            long currentTick,
            ISet<string> eventIds,
            out int ordinal,
            out string error)
        {
            ordinal = 0;
            if (order == null ||
                !StableIdentifier.IsValid(order.orderId) ||
                !TryReadOrderOrdinal(order.orderId, out ordinal) ||
                !StableIdentifier.IsValid(order.locationId) ||
                !catalog.TryGetSupplier(
                    order.supplierId,
                    out ProcurementSupplierDefinition supplier) ||
                !string.Equals(
                    order.containerDefinitionId,
                    supplier.ContainerDefinitionId,
                    StringComparison.Ordinal) ||
                order.placedAtTick < 0 ||
                order.fulfillAtTick < order.placedAtTick ||
                (order.fulfillAtTick > currentTick &&
                 order.status != PurchaseOrderStatus.Pending &&
                 order.status != PurchaseOrderStatus.Canceled) ||
                !Enum.IsDefined(typeof(PurchaseOrderStatus), order.status) ||
                order.subtotalCostCents < 0 || order.deliveryFeeCents < 0 ||
                order.totalCostCents < 0 || order.refundedCents < 0 ||
                order.lines == null || order.lines.Count == 0)
            {
                error = "Purchase order identity, supplier, timing, status, or totals are invalid.";
                return false;
            }

            HashSet<string> resourceIds = new(StringComparer.Ordinal);
            long subtotal = 0;
            int totalUnits = 0;
            try
            {
                foreach (PurchaseOrderLineSnapshot line in order.lines)
                {
                    if (line == null ||
                        !supplier.TryGetResource(line.resourceId, out _) ||
                        !resourceIds.Add(line.resourceId) ||
                        line.orderedQuantityUnits <= 0 ||
                        line.receivedQuantityUnits < 0 ||
                        line.receivedQuantityUnits > line.orderedQuantityUnits ||
                        line.unitCostCents < 0 ||
                        line.lineCostCents != checked(
                            line.unitCostCents * line.orderedQuantityUnits))
                    {
                        error = "Purchase order line identity, quantity, or historical cost is invalid.";
                        return false;
                    }

                    subtotal = checked(subtotal + line.lineCostCents);
                    totalUnits = checked(totalUnits + line.orderedQuantityUnits);
                }
            }
            catch (OverflowException)
            {
                error = "Purchase order line totals overflowed supported storage.";
                return false;
            }

            if (totalUnits > supplier.MaximumOrderUnits ||
                subtotal != order.subtotalCostCents ||
                order.totalCostCents !=
                order.subtotalCostCents + order.deliveryFeeCents ||
                !AddExpectedEvent(
                    eventIds,
                    $"payment-{order.orderId}",
                    order.paymentId))
            {
                error = "Purchase order subtotal, total, capacity, or payment identity is invalid.";
                return false;
            }

            bool anyReceived = order.lines.Any(line =>
                line.receivedQuantityUnits > 0);
            bool allReceived = order.lines.All(line =>
                line.receivedQuantityUnits == line.orderedQuantityUnits);
            bool valid = order.status switch
            {
                PurchaseOrderStatus.Pending =>
                    order.fulfillAtTick >= currentTick &&
                    !anyReceived && order.refundedCents == 0 &&
                    NoEvent(order.fulfillmentId) && NoEvent(order.deliveryId) &&
                    NoEvent(order.inventoryReceiptId) &&
                    NoEvent(order.completionReceiptId) &&
                    NoEvent(order.cancellationId),
                PurchaseOrderStatus.Fulfilled =>
                    order.fulfillAtTick <= currentTick &&
                    !anyReceived && order.refundedCents == 0 &&
                    AddExpectedEvent(
                        eventIds,
                        $"fulfillment-{order.orderId}",
                        order.fulfillmentId) &&
                    NoEvent(order.deliveryId) && NoEvent(order.inventoryReceiptId) &&
                    NoEvent(order.completionReceiptId) &&
                    NoEvent(order.cancellationId),
                PurchaseOrderStatus.Delivered =>
                    !anyReceived && order.refundedCents == 0 &&
                    AddExpectedEvent(eventIds, $"fulfillment-{order.orderId}", order.fulfillmentId) &&
                    AddExpectedEvent(eventIds, $"delivery-{order.orderId}", order.deliveryId) &&
                    AddExpectedEvent(eventIds, $"inventory-receipt-{order.orderId}", order.inventoryReceiptId) &&
                    NoEvent(order.completionReceiptId) &&
                    NoEvent(order.cancellationId),
                PurchaseOrderStatus.PartiallyReceived =>
                    anyReceived && !allReceived && order.refundedCents == 0 &&
                    AddExpectedEvent(eventIds, $"fulfillment-{order.orderId}", order.fulfillmentId) &&
                    AddExpectedEvent(eventIds, $"delivery-{order.orderId}", order.deliveryId) &&
                    AddExpectedEvent(eventIds, $"inventory-receipt-{order.orderId}", order.inventoryReceiptId) &&
                    NoEvent(order.completionReceiptId) &&
                    NoEvent(order.cancellationId),
                PurchaseOrderStatus.Canceled =>
                    !anyReceived && order.refundedCents == order.totalCostCents &&
                    NoEvent(order.fulfillmentId) && NoEvent(order.deliveryId) &&
                    NoEvent(order.inventoryReceiptId) &&
                    NoEvent(order.completionReceiptId) &&
                    AddExpectedEvent(eventIds, $"cancellation-{order.orderId}", order.cancellationId),
                PurchaseOrderStatus.Completed =>
                    allReceived && order.refundedCents == 0 &&
                    AddExpectedEvent(eventIds, $"fulfillment-{order.orderId}", order.fulfillmentId) &&
                    AddExpectedEvent(eventIds, $"delivery-{order.orderId}", order.deliveryId) &&
                    AddExpectedEvent(eventIds, $"inventory-receipt-{order.orderId}", order.inventoryReceiptId) &&
                    AddExpectedEvent(eventIds, $"completion-receipt-{order.orderId}", order.completionReceiptId) &&
                    NoEvent(order.cancellationId),
                _ => false
            };
            if (!valid)
            {
                error =
                    $"Purchase order '{order.orderId}' status '{order.status}' contradicts its payment, fulfillment, delivery, cancellation, or receipt events " +
                    $"(received {order.ReceivedQuantityUnits}/{order.OrderedQuantityUnits}, refund {order.refundedCents}, " +
                    $"fulfillment '{order.fulfillmentId}', delivery '{order.deliveryId}', inventory receipt '{order.inventoryReceiptId}', " +
                    $"completion '{order.completionReceiptId}', cancellation '{order.cancellationId}').";
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryReadOrderOrdinal(
            string orderId,
            out int ordinal)
        {
            const string Prefix = "purchase-order-";
            ordinal = 0;
            return orderId.StartsWith(Prefix, StringComparison.Ordinal) &&
                   int.TryParse(
                       orderId.Substring(Prefix.Length),
                       out ordinal) &&
                   ordinal > 0 &&
                   string.Equals(
                       orderId,
                       $"{Prefix}{ordinal:000000}",
                       StringComparison.Ordinal);
        }

        private static bool NoEvent(string eventId)
        {
            return string.IsNullOrEmpty(eventId);
        }

        private static bool AddExpectedEvent(
            ISet<string> eventIds,
            string expectedEventId,
            string eventId)
        {
            return string.Equals(
                       eventId,
                       expectedEventId,
                       StringComparison.Ordinal) &&
                   StableIdentifier.IsValid(eventId) &&
                   TryAddEventId(eventIds, eventId);
        }

        private static bool TryAddEventId(
            ISet<string> eventIds,
            string eventId)
        {
            return eventIds.Add(eventId);
        }

        internal static ProcurementSnapshot CopySnapshot(ProcurementSnapshot source)
        {
            return new ProcurementSnapshot
            {
                version = source.version,
                currentTick = source.currentTick,
                nextOrderOrdinal = source.nextOrderOrdinal,
                orders = source.orders?
                    .Select(Clone)
                    .OrderBy(
                        order => order?.orderId,
                        StringComparer.Ordinal)
                    .ToList() ?? new List<PurchaseOrderSnapshot>()
            };
        }

        private static PurchaseOrderSnapshot Clone(PurchaseOrderSnapshot source)
        {
            if (source == null)
            {
                return null;
            }

            return new PurchaseOrderSnapshot
            {
                orderId = source.orderId,
                locationId = source.locationId,
                supplierId = source.supplierId,
                containerDefinitionId = source.containerDefinitionId,
                placedAtTick = source.placedAtTick,
                fulfillAtTick = source.fulfillAtTick,
                status = source.status,
                subtotalCostCents = source.subtotalCostCents,
                deliveryFeeCents = source.deliveryFeeCents,
                totalCostCents = source.totalCostCents,
                refundedCents = source.refundedCents,
                paymentId = source.paymentId,
                fulfillmentId = source.fulfillmentId,
                deliveryId = source.deliveryId,
                inventoryReceiptId = source.inventoryReceiptId,
                completionReceiptId = source.completionReceiptId,
                cancellationId = source.cancellationId,
                lines = source.lines?
                    .Select(line => line == null
                        ? null
                        : new PurchaseOrderLineSnapshot
                        {
                            resourceId = line.resourceId,
                            orderedQuantityUnits = line.orderedQuantityUnits,
                            receivedQuantityUnits = line.receivedQuantityUnits,
                            unitCostCents = line.unitCostCents,
                            lineCostCents = line.lineCostCents
                        })
                    .OrderBy(
                        line => line?.resourceId,
                        StringComparer.Ordinal)
                    .ToList() ?? new List<PurchaseOrderLineSnapshot>()
            };
        }
    }

    /// <summary>
    /// Current convenience-store content configuration. The procurement domain
    /// itself remains independent of product, supplier, and container ids.
    /// </summary>
    public static class ConvenienceStoreProcurement
    {
        public const string SupplierId = "supplier-regional-wholesale-alpha";
        public const string ContainerDefinitionId = "container-mixed-case-small";
        public const string AggregateResourceId = "resource-convenience-assortment";
        public const string ColaProductId = "prod-cola-can-355ml";
        public const string ChipsProductId = "prod-potato-chips-small";
        public const int FulfillmentDelayTicks = 2;
        public const int TicksPerDelegatedDay = 2;
        public const int DetailedCaseUnitsPerProduct = 4;
        public const long DeliveryFeeCents = 6_500;

        public static readonly ProcurementCatalog Catalog = new(
            new[]
            {
                new ProcurementSupplierDefinition(
                    SupplierId,
                    ContainerDefinitionId,
                    DeliveryFeeCents,
                    FulfillmentDelayTicks,
                    1_200,
                    new[]
                    {
                        new ProcurementResourceDefinition(ColaProductId, 70),
                        new ProcurementResourceDefinition(ChipsProductId, 80),
                        new ProcurementResourceDefinition(
                            AggregateResourceId,
                            PortfolioProgressionRules.AggregateUnitCostCents)
                    })
            });

        public static IReadOnlyList<ProcurementOrderRequestLine> DetailedCase =>
            new[]
            {
                new ProcurementOrderRequestLine(
                    ColaProductId,
                    DetailedCaseUnitsPerProduct),
                new ProcurementOrderRequestLine(
                    ChipsProductId,
                    DetailedCaseUnitsPerProduct)
            };

        public static bool IsDetailedResource(string resourceId)
        {
            return string.Equals(
                       resourceId,
                       ColaProductId,
                       StringComparison.Ordinal) ||
                   string.Equals(
                       resourceId,
                       ChipsProductId,
                       StringComparison.Ordinal);
        }
    }
}
