// Draft implementation — Unity verification pending
using System;
using System.Collections.Generic;
using System.Linq;

namespace Margins
{
    public enum InventoryLocationKind
    {
        DeliveryContainer,
        Loose,
        Held,
        Shelf
    }

    [Serializable]
    public sealed class InventoryQuantitySnapshot : IEquatable<InventoryQuantitySnapshot>
    {
        public string productId;
        public int quantityUnits;

        public InventoryQuantitySnapshot(string productId, int quantityUnits)
        {
            this.productId = productId;
            this.quantityUnits = quantityUnits;
        }

        public bool Equals(InventoryQuantitySnapshot other)
        {
            return other != null &&
                   string.Equals(productId, other.productId, StringComparison.Ordinal) &&
                   quantityUnits == other.quantityUnits;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as InventoryQuantitySnapshot);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(productId, quantityUnits);
        }
    }

    [Serializable]
    public sealed class InventoryLocationSnapshot : IEquatable<InventoryLocationSnapshot>
    {
        public string locationId;
        public InventoryLocationKind kind;
        public int capacityUnits;
        public bool singleProductOnly;
        public List<InventoryQuantitySnapshot> quantities = new();

        public InventoryLocationSnapshot(
            string locationId,
            InventoryLocationKind kind,
            int capacityUnits,
            bool singleProductOnly)
        {
            this.locationId = locationId;
            this.kind = kind;
            this.capacityUnits = capacityUnits;
            this.singleProductOnly = singleProductOnly;
        }

        public bool Equals(InventoryLocationSnapshot other)
        {
            if (other == null ||
                !string.Equals(locationId, other.locationId, StringComparison.Ordinal) ||
                kind != other.kind ||
                capacityUnits != other.capacityUnits ||
                singleProductOnly != other.singleProductOnly ||
                quantities == null ||
                other.quantities == null ||
                quantities.Count != other.quantities.Count)
            {
                return false;
            }

            for (int index = 0; index < quantities.Count; index++)
            {
                if (!quantities[index].Equals(other.quantities[index]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as InventoryLocationSnapshot);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(locationId);
            hash.Add(kind);
            hash.Add(capacityUnits);
            hash.Add(singleProductOnly);
            if (quantities != null)
            {
                foreach (InventoryQuantitySnapshot quantity in quantities)
                {
                    hash.Add(quantity);
                }
            }
            return hash.ToHashCode();
        }
    }

    [Serializable]
    public sealed class FirstStoreInventorySnapshot : IEquatable<FirstStoreInventorySnapshot>
    {
        public List<string> productIds = new();
        public List<InventoryLocationSnapshot> locations = new();

        public bool Equals(FirstStoreInventorySnapshot other)
        {
            if (other == null ||
                productIds == null ||
                other.productIds == null ||
                locations == null ||
                other.locations == null ||
                productIds.Count != other.productIds.Count ||
                locations.Count != other.locations.Count)
            {
                return false;
            }

            for (int index = 0; index < productIds.Count; index++)
            {
                if (!string.Equals(productIds[index], other.productIds[index], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            for (int index = 0; index < locations.Count; index++)
            {
                if (!locations[index].Equals(other.locations[index]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FirstStoreInventorySnapshot);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            if (productIds != null)
            {
                foreach (string productId in productIds)
                {
                    hash.Add(productId);
                }
            }

            if (locations != null)
            {
                foreach (InventoryLocationSnapshot location in locations)
                {
                    hash.Add(location);
                }
            }
            return hash.ToHashCode();
        }
    }

    public enum InventoryTransferFailure
    {
        None,
        InvalidIdentifier,
        UnknownProduct,
        InvalidQuantity,
        SameLocation,
        MissingSourceLocation,
        MissingDestinationLocation,
        InsufficientQuantity,
        DestinationCapacityExceeded,
        DestinationOccupiedByOtherProduct
    }

    public sealed class InventoryTransferResult
    {
        public InventoryTransferFailure Failure { get; }
        public string ProductId { get; }
        public string SourceLocationId { get; }
        public string DestinationLocationId { get; }
        public int QuantityUnits { get; }
        public bool IsSuccess => Failure == InventoryTransferFailure.None;

        public InventoryTransferResult(
            InventoryTransferFailure failure,
            string productId,
            string sourceLocationId,
            string destinationLocationId,
            int quantityUnits)
        {
            Failure = failure;
            ProductId = productId;
            SourceLocationId = sourceLocationId;
            DestinationLocationId = destinationLocationId;
            QuantityUnits = quantityUnits;
        }
    }

    public enum InventorySaleFailure
    {
        None,
        InvalidSourceLocation,
        SourceIsNotShelf,
        EmptyRequest,
        InvalidProduct,
        InvalidQuantity,
        InsufficientQuantity
    }

    public enum InventoryReceiptFailure
    {
        None,
        InvalidLocation,
        DestinationIsNotDeliveryContainer,
        EmptyRequest,
        UnknownProduct,
        InvalidQuantity,
        DestinationCapacityExceeded,
        DestinationOccupiedByOtherProduct
    }

    public sealed class FirstStoreInventory
    {
        private sealed class LocationState
        {
            public string LocationId { get; }
            public InventoryLocationKind Kind { get; }
            public int CapacityUnits { get; }
            public bool SingleProductOnly { get; }
            public Dictionary<string, int> Quantities { get; } = new(StringComparer.Ordinal);

            public LocationState(
                string locationId,
                InventoryLocationKind kind,
                int capacityUnits,
                bool singleProductOnly)
            {
                LocationId = locationId;
                Kind = kind;
                CapacityUnits = capacityUnits;
                SingleProductOnly = singleProductOnly;
            }

            public int TotalQuantity
            {
                get
                {
                    int total = 0;
                    foreach (int quantity in Quantities.Values)
                    {
                        total = checked(total + quantity);
                    }
                    return total;
                }
            }
        }

        private readonly HashSet<string> productIds = new(StringComparer.Ordinal);
        private readonly Dictionary<string, LocationState> locations = new(StringComparer.Ordinal);

        public int ProductCount => productIds.Count;
        public int LocationCount => locations.Count;

        public bool TryRegisterProduct(string productId, out string error)
        {
            if (!FirstStoreIdentifier.IsValid(productId))
            {
                error = $"Product id '{productId}' is invalid.";
                return false;
            }

            if (!productIds.Add(productId))
            {
                error = $"Duplicate product id '{productId}'.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryRegisterLocation(
            string locationId,
            InventoryLocationKind kind,
            int capacityUnits,
            bool singleProductOnly,
            out string error)
        {
            if (!FirstStoreIdentifier.IsValid(locationId))
            {
                error = $"Inventory location id '{locationId}' is invalid.";
                return false;
            }

            if (!Enum.IsDefined(typeof(InventoryLocationKind), kind))
            {
                error = $"Inventory location '{locationId}' has invalid kind '{kind}'.";
                return false;
            }

            if (capacityUnits < -1)
            {
                error = $"Inventory location '{locationId}' has invalid capacity {capacityUnits}.";
                return false;
            }

            if (locations.ContainsKey(locationId))
            {
                error = $"Duplicate inventory location id '{locationId}'.";
                return false;
            }

            locations.Add(
                locationId,
                new LocationState(locationId, kind, capacityUnits, singleProductOnly));
            error = null;
            return true;
        }

        public bool TrySeedQuantity(
            string locationId,
            string productId,
            int quantityUnits,
            out string error)
        {
            if (!productIds.Contains(productId))
            {
                error = $"Cannot seed unknown product '{productId}'.";
                return false;
            }

            if (!locations.TryGetValue(locationId, out LocationState location))
            {
                error = $"Cannot seed missing inventory location '{locationId}'.";
                return false;
            }

            if (quantityUnits <= 0)
            {
                error = "Seed quantity must be positive.";
                return false;
            }

            if (location.Quantities.ContainsKey(productId))
            {
                error = $"Product '{productId}' is already seeded at '{locationId}'.";
                return false;
            }

            if (location.CapacityUnits >= 0 && quantityUnits > location.CapacityUnits)
            {
                error = $"Seed quantity exceeds capacity at '{locationId}'.";
                return false;
            }

            if (location.SingleProductOnly && location.Quantities.Count > 0)
            {
                error = $"Inventory location '{locationId}' already contains another product.";
                return false;
            }

            location.Quantities.Add(productId, quantityUnits);
            error = null;
            return true;
        }

        public bool IsKnownProduct(string productId)
        {
            return FirstStoreIdentifier.IsValid(productId) && productIds.Contains(productId);
        }

        public bool TryGetLocationKind(string locationId, out InventoryLocationKind kind)
        {
            if (locations.TryGetValue(locationId, out LocationState location))
            {
                kind = location.Kind;
                return true;
            }

            kind = default;
            return false;
        }

        public int GetQuantity(string locationId, string productId)
        {
            if (!locations.TryGetValue(locationId, out LocationState location))
            {
                return 0;
            }

            return location.Quantities.TryGetValue(productId, out int quantity)
                ? quantity
                : 0;
        }

        public int GetTotalQuantity(string productId)
        {
            int total = 0;
            foreach (LocationState location in locations.Values)
            {
                total = checked(total + GetQuantity(location.LocationId, productId));
            }
            return total;
        }

        /// <summary>
        /// The explicit inventory-acquisition boundary. Purchase-order state
        /// supplies idempotency; this authority validates the complete batch
        /// before creating any units in a delivery-container location.
        /// </summary>
        public bool TryReceiveDelivery(
            string destinationLocationId,
            IReadOnlyDictionary<string, int> receivedQuantities,
            out InventoryReceiptFailure failure)
        {
            if (!ValidateDeliveryReceipt(
                    destinationLocationId,
                    receivedQuantities,
                    out failure))
            {
                return false;
            }

            LocationState destination = locations[destinationLocationId];
            foreach (string productId in receivedQuantities.Keys
                         .OrderBy(value => value, StringComparer.Ordinal))
            {
                destination.Quantities.TryGetValue(productId, out int existing);
                destination.Quantities[productId] = checked(
                    existing + receivedQuantities[productId]);
            }

            return true;
        }

        public InventoryTransferResult CanTransfer(
            string productId,
            string sourceLocationId,
            string destinationLocationId,
            int quantityUnits)
        {
            return ValidateTransfer(
                productId,
                sourceLocationId,
                destinationLocationId,
                quantityUnits);
        }

        public InventoryTransferResult TryTransfer(
            string productId,
            string sourceLocationId,
            string destinationLocationId,
            int quantityUnits)
        {
            InventoryTransferResult validation = ValidateTransfer(
                productId,
                sourceLocationId,
                destinationLocationId,
                quantityUnits);
            if (!validation.IsSuccess)
            {
                return validation;
            }

            LocationState source = locations[sourceLocationId];
            LocationState destination = locations[destinationLocationId];

            int remaining = source.Quantities[productId] - quantityUnits;
            if (remaining == 0)
            {
                source.Quantities.Remove(productId);
            }
            else
            {
                source.Quantities[productId] = remaining;
            }

            destination.Quantities.TryGetValue(productId, out int existing);
            destination.Quantities[productId] = checked(existing + quantityUnits);
            return validation;
        }

        public bool TryConsumeForSale(
            string sourceLocationId,
            IReadOnlyDictionary<string, int> requestedQuantities,
            out InventorySaleFailure failure)
        {
            if (!locations.TryGetValue(sourceLocationId, out LocationState source))
            {
                failure = InventorySaleFailure.InvalidSourceLocation;
                return false;
            }

            if (source.Kind != InventoryLocationKind.Shelf)
            {
                failure = InventorySaleFailure.SourceIsNotShelf;
                return false;
            }

            if (requestedQuantities == null || requestedQuantities.Count == 0)
            {
                failure = InventorySaleFailure.EmptyRequest;
                return false;
            }

            List<string> orderedProductIds = new(requestedQuantities.Keys);
            orderedProductIds.Sort(StringComparer.Ordinal);
            foreach (string productId in orderedProductIds)
            {
                if (!IsKnownProduct(productId))
                {
                    failure = InventorySaleFailure.InvalidProduct;
                    return false;
                }

                int requested = requestedQuantities[productId];
                if (requested <= 0)
                {
                    failure = InventorySaleFailure.InvalidQuantity;
                    return false;
                }

                if (GetQuantity(sourceLocationId, productId) < requested)
                {
                    failure = InventorySaleFailure.InsufficientQuantity;
                    return false;
                }
            }

            foreach (string productId in orderedProductIds)
            {
                int remaining = source.Quantities[productId] - requestedQuantities[productId];
                if (remaining == 0)
                {
                    source.Quantities.Remove(productId);
                }
                else
                {
                    source.Quantities[productId] = remaining;
                }
            }

            failure = InventorySaleFailure.None;
            return true;
        }

        public bool TryConsumeMappedSale(
            IReadOnlyDictionary<string, string> sourceLocationIdsByProduct,
            IReadOnlyDictionary<string, int> requestedQuantities,
            out InventorySaleFailure failure)
        {
            if (requestedQuantities == null || requestedQuantities.Count == 0)
            {
                failure = InventorySaleFailure.EmptyRequest;
                return false;
            }

            if (sourceLocationIdsByProduct == null)
            {
                failure = InventorySaleFailure.InvalidSourceLocation;
                return false;
            }

            List<string> orderedProductIds = new(requestedQuantities.Keys);
            orderedProductIds.Sort(StringComparer.Ordinal);
            foreach (string productId in orderedProductIds)
            {
                if (!IsKnownProduct(productId))
                {
                    failure = InventorySaleFailure.InvalidProduct;
                    return false;
                }

                if (!sourceLocationIdsByProduct.TryGetValue(
                        productId,
                        out string sourceLocationId) ||
                    !locations.TryGetValue(sourceLocationId, out LocationState source))
                {
                    failure = InventorySaleFailure.InvalidSourceLocation;
                    return false;
                }

                if (source.Kind != InventoryLocationKind.Shelf)
                {
                    failure = InventorySaleFailure.SourceIsNotShelf;
                    return false;
                }

                int requested = requestedQuantities[productId];
                if (requested <= 0)
                {
                    failure = InventorySaleFailure.InvalidQuantity;
                    return false;
                }

                if (GetQuantity(sourceLocationId, productId) < requested)
                {
                    failure = InventorySaleFailure.InsufficientQuantity;
                    return false;
                }
            }

            foreach (string productId in orderedProductIds)
            {
                string sourceLocationId = sourceLocationIdsByProduct[productId];
                LocationState source = locations[sourceLocationId];
                int remaining = source.Quantities[productId] - requestedQuantities[productId];
                if (remaining == 0)
                {
                    source.Quantities.Remove(productId);
                }
                else
                {
                    source.Quantities[productId] = remaining;
                }
            }

            failure = InventorySaleFailure.None;
            return true;
        }

        public FirstStoreInventorySnapshot CreateSnapshot()
        {
            FirstStoreInventorySnapshot snapshot = new();
            snapshot.productIds.AddRange(productIds);
            snapshot.productIds.Sort(StringComparer.Ordinal);

            List<LocationState> orderedLocations = new(locations.Values);
            orderedLocations.Sort((left, right) =>
                string.CompareOrdinal(left.LocationId, right.LocationId));
            foreach (LocationState location in orderedLocations)
            {
                InventoryLocationSnapshot locationSnapshot = new(
                    location.LocationId,
                    location.Kind,
                    location.CapacityUnits,
                    location.SingleProductOnly);

                List<string> orderedProducts = new(location.Quantities.Keys);
                orderedProducts.Sort(StringComparer.Ordinal);
                foreach (string productId in orderedProducts)
                {
                    locationSnapshot.quantities.Add(
                        new InventoryQuantitySnapshot(
                            productId,
                            location.Quantities[productId]));
                }

                snapshot.locations.Add(locationSnapshot);
            }

            return snapshot;
        }

        public static bool TryRestore(
            FirstStoreInventorySnapshot snapshot,
            out FirstStoreInventory inventory,
            out string error)
        {
            inventory = null;
            if (snapshot == null ||
                snapshot.productIds == null ||
                snapshot.locations == null)
            {
                error = "Inventory snapshot is incomplete.";
                return false;
            }

            FirstStoreInventory candidate = new();
            foreach (string productId in snapshot.productIds)
            {
                if (!candidate.TryRegisterProduct(productId, out error))
                {
                    return false;
                }
            }

            foreach (InventoryLocationSnapshot location in snapshot.locations)
            {
                if (location == null || location.quantities == null)
                {
                    error = "Inventory snapshot contains an incomplete location.";
                    return false;
                }

                if (!candidate.TryRegisterLocation(
                        location.locationId,
                        location.kind,
                        location.capacityUnits,
                        location.singleProductOnly,
                        out error))
                {
                    return false;
                }

                HashSet<string> locationProducts = new(StringComparer.Ordinal);
                foreach (InventoryQuantitySnapshot quantity in location.quantities)
                {
                    if (quantity == null)
                    {
                        error = $"Inventory location '{location.locationId}' contains a null quantity record.";
                        return false;
                    }

                    if (!locationProducts.Add(quantity.productId))
                    {
                        error =
                            $"Inventory location '{location.locationId}' contains duplicate product '{quantity.productId}'.";
                        return false;
                    }

                    if (!candidate.TrySeedQuantity(
                            location.locationId,
                            quantity.productId,
                            quantity.quantityUnits,
                            out error))
                    {
                        return false;
                    }
                }
            }

            inventory = candidate;
            error = null;
            return true;
        }

        private InventoryTransferResult ValidateTransfer(
            string productId,
            string sourceLocationId,
            string destinationLocationId,
            int quantityUnits)
        {
            InventoryTransferFailure failure = InventoryTransferFailure.None;
            if (!FirstStoreIdentifier.IsValid(productId) ||
                !FirstStoreIdentifier.IsValid(sourceLocationId) ||
                !FirstStoreIdentifier.IsValid(destinationLocationId))
            {
                failure = InventoryTransferFailure.InvalidIdentifier;
            }
            else if (!productIds.Contains(productId))
            {
                failure = InventoryTransferFailure.UnknownProduct;
            }
            else if (quantityUnits <= 0)
            {
                failure = InventoryTransferFailure.InvalidQuantity;
            }
            else if (string.Equals(sourceLocationId, destinationLocationId, StringComparison.Ordinal))
            {
                failure = InventoryTransferFailure.SameLocation;
            }
            else if (!locations.TryGetValue(sourceLocationId, out LocationState source))
            {
                failure = InventoryTransferFailure.MissingSourceLocation;
            }
            else if (!locations.TryGetValue(destinationLocationId, out LocationState destination))
            {
                failure = InventoryTransferFailure.MissingDestinationLocation;
            }
            else if (GetQuantity(sourceLocationId, productId) < quantityUnits)
            {
                failure = InventoryTransferFailure.InsufficientQuantity;
            }
            else if (destination.Quantities.TryGetValue(
                         productId,
                         out int existingDestinationQuantity) &&
                     existingDestinationQuantity > int.MaxValue - quantityUnits)
            {
                failure = InventoryTransferFailure.DestinationCapacityExceeded;
            }
            else if (destination.CapacityUnits >= 0 &&
                     destination.TotalQuantity > destination.CapacityUnits - quantityUnits)
            {
                failure = InventoryTransferFailure.DestinationCapacityExceeded;
            }
            else if (destination.SingleProductOnly &&
                     ContainsOtherProduct(destination, productId))
            {
                failure = InventoryTransferFailure.DestinationOccupiedByOtherProduct;
            }

            return new InventoryTransferResult(
                failure,
                productId,
                sourceLocationId,
                destinationLocationId,
                quantityUnits);
        }

        private bool ValidateDeliveryReceipt(
            string destinationLocationId,
            IReadOnlyDictionary<string, int> receivedQuantities,
            out InventoryReceiptFailure failure)
        {
            if (!FirstStoreIdentifier.IsValid(destinationLocationId) ||
                !locations.TryGetValue(
                    destinationLocationId,
                    out LocationState destination))
            {
                failure = InventoryReceiptFailure.InvalidLocation;
                return false;
            }

            if (destination.Kind != InventoryLocationKind.DeliveryContainer)
            {
                failure = InventoryReceiptFailure.DestinationIsNotDeliveryContainer;
                return false;
            }

            if (receivedQuantities == null || receivedQuantities.Count == 0)
            {
                failure = InventoryReceiptFailure.EmptyRequest;
                return false;
            }

            int requestedTotal = 0;
            int resultingProductKinds = destination.Quantities.Count;
            foreach (string productId in receivedQuantities.Keys
                         .OrderBy(value => value, StringComparer.Ordinal))
            {
                if (!IsKnownProduct(productId))
                {
                    failure = InventoryReceiptFailure.UnknownProduct;
                    return false;
                }

                int requested = receivedQuantities[productId];
                if (requested <= 0)
                {
                    failure = InventoryReceiptFailure.InvalidQuantity;
                    return false;
                }

                destination.Quantities.TryGetValue(productId, out int existing);
                if (existing > int.MaxValue - requested ||
                    requestedTotal > int.MaxValue - requested)
                {
                    failure = InventoryReceiptFailure.DestinationCapacityExceeded;
                    return false;
                }

                requestedTotal += requested;
                if (existing == 0)
                {
                    resultingProductKinds++;
                }
            }

            if (destination.CapacityUnits >= 0 &&
                destination.TotalQuantity >
                destination.CapacityUnits - requestedTotal)
            {
                failure = InventoryReceiptFailure.DestinationCapacityExceeded;
                return false;
            }

            if (destination.SingleProductOnly && resultingProductKinds > 1)
            {
                failure = InventoryReceiptFailure.DestinationOccupiedByOtherProduct;
                return false;
            }

            failure = InventoryReceiptFailure.None;
            return true;
        }

        private static bool ContainsOtherProduct(LocationState location, string productId)
        {
            foreach (KeyValuePair<string, int> quantity in location.Quantities)
            {
                if (quantity.Value > 0 &&
                    !string.Equals(quantity.Key, productId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public enum DeliveryContainerOpenResult
    {
        Opened,
        AlreadyOpen
    }

    public enum DeliveryContainerFailure
    {
        None,
        InvalidConfiguration,
        Sealed,
        TransferRejected
    }

    [Serializable]
    public sealed class DeliveryContainerSnapshot : IEquatable<DeliveryContainerSnapshot>
    {
        public string containerId;
        public string inventoryLocationId;
        public bool isOpen;

        public DeliveryContainerSnapshot(
            string containerId,
            string inventoryLocationId,
            bool isOpen)
        {
            this.containerId = containerId;
            this.inventoryLocationId = inventoryLocationId;
            this.isOpen = isOpen;
        }

        public bool Equals(DeliveryContainerSnapshot other)
        {
            return other != null &&
                   string.Equals(containerId, other.containerId, StringComparison.Ordinal) &&
                   string.Equals(
                       inventoryLocationId,
                       other.inventoryLocationId,
                       StringComparison.Ordinal) &&
                   isOpen == other.isOpen;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DeliveryContainerSnapshot);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(containerId, inventoryLocationId, isOpen);
        }
    }

    public sealed class DeliveryContainer
    {
        private readonly FirstStoreInventory inventory;

        public string ContainerId { get; }
        public string InventoryLocationId { get; }
        public bool IsOpen { get; private set; }

        private DeliveryContainer(
            FirstStoreInventory inventory,
            string containerId,
            string inventoryLocationId,
            bool isOpen)
        {
            this.inventory = inventory;
            ContainerId = containerId;
            InventoryLocationId = inventoryLocationId;
            IsOpen = isOpen;
        }

        public static bool TryCreate(
            FirstStoreInventory inventory,
            string containerId,
            string inventoryLocationId,
            bool isOpen,
            out DeliveryContainer container,
            out string error)
        {
            container = null;
            if (inventory == null ||
                !FirstStoreIdentifier.IsValid(containerId) ||
                !FirstStoreIdentifier.IsValid(inventoryLocationId) ||
                !inventory.TryGetLocationKind(
                    inventoryLocationId,
                    out InventoryLocationKind kind) ||
                kind != InventoryLocationKind.DeliveryContainer)
            {
                error = "Delivery container configuration is invalid.";
                return false;
            }

            container = new DeliveryContainer(
                inventory,
                containerId,
                inventoryLocationId,
                isOpen);
            error = null;
            return true;
        }

        public DeliveryContainerOpenResult TryOpen()
        {
            if (IsOpen)
            {
                return DeliveryContainerOpenResult.AlreadyOpen;
            }

            IsOpen = true;
            return DeliveryContainerOpenResult.Opened;
        }

        public bool TryRemoveTo(
            string productId,
            string destinationLocationId,
            int quantityUnits,
            out DeliveryContainerFailure failure,
            out InventoryTransferResult transfer)
        {
            if (!IsOpen)
            {
                transfer = null;
                failure = DeliveryContainerFailure.Sealed;
                return false;
            }

            transfer = inventory.TryTransfer(
                productId,
                InventoryLocationId,
                destinationLocationId,
                quantityUnits);
            failure = transfer.IsSuccess
                ? DeliveryContainerFailure.None
                : DeliveryContainerFailure.TransferRejected;
            return transfer.IsSuccess;
        }

        public DeliveryContainerSnapshot CreateSnapshot()
        {
            return new DeliveryContainerSnapshot(
                ContainerId,
                InventoryLocationId,
                IsOpen);
        }

        public static bool TryRestore(
            FirstStoreInventory inventory,
            DeliveryContainerSnapshot snapshot,
            out DeliveryContainer container,
            out string error)
        {
            if (snapshot == null)
            {
                container = null;
                error = "Delivery container snapshot is missing.";
                return false;
            }

            return TryCreate(
                inventory,
                snapshot.containerId,
                snapshot.inventoryLocationId,
                snapshot.isOpen,
                out container,
                out error);
        }
    }
}
