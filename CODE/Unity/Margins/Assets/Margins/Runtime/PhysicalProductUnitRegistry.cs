using System;
using System.Collections.Generic;
using UnityEngine;

namespace Margins
{
    [Serializable]
    public sealed class PhysicalProductUnitSnapshot : IEquatable<PhysicalProductUnitSnapshot>
    {
        public string physicalUnitId;
        public string productId;
        public string inventoryLocationId;
        public string shelfFixtureId;
        public string shelfSnapPointId;
        public int quarterTurns;

        public PhysicalProductUnitSnapshot(
            string physicalUnitId,
            string productId,
            string inventoryLocationId,
            string shelfFixtureId,
            string shelfSnapPointId,
            int quarterTurns)
        {
            this.physicalUnitId = physicalUnitId;
            this.productId = productId;
            this.inventoryLocationId = inventoryLocationId;
            this.shelfFixtureId = shelfFixtureId;
            this.shelfSnapPointId = shelfSnapPointId;
            this.quarterTurns = quarterTurns;
        }

        public bool Equals(PhysicalProductUnitSnapshot other)
        {
            return other != null &&
                   string.Equals(physicalUnitId, other.physicalUnitId, StringComparison.Ordinal) &&
                   string.Equals(productId, other.productId, StringComparison.Ordinal) &&
                   string.Equals(inventoryLocationId, other.inventoryLocationId, StringComparison.Ordinal) &&
                   string.Equals(shelfFixtureId, other.shelfFixtureId, StringComparison.Ordinal) &&
                   string.Equals(shelfSnapPointId, other.shelfSnapPointId, StringComparison.Ordinal) &&
                   quarterTurns == other.quarterTurns;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PhysicalProductUnitSnapshot);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                physicalUnitId,
                productId,
                inventoryLocationId,
                shelfFixtureId,
                shelfSnapPointId,
                quarterTurns);
        }
    }

    [Serializable]
    public sealed class PhysicalProductUnitConfiguration
    {
        [SerializeField] private ProductDefinition productDefinition;
        [SerializeField] private ProductItem unitPrefab;
        [SerializeField] private Transform looseSpawnPoint;
        [SerializeField] private Vector3 looseUnitSpacing = new(0.2f, 0f, 0f);

        public ProductDefinition ProductDefinition => productDefinition;
        public ProductItem UnitPrefab => unitPrefab;
        public Transform LooseSpawnPoint => looseSpawnPoint;
        public Vector3 LooseUnitSpacing => looseUnitSpacing;
    }

    public sealed class PhysicalProductUnitRegistry : MonoBehaviour
    {
        private sealed class UnitRecord
        {
            public ProductItem Item { get; }
            public string InventoryLocationId { get; set; }

            public UnitRecord(ProductItem item, string inventoryLocationId)
            {
                Item = item;
                InventoryLocationId = inventoryLocationId;
            }
        }

        [SerializeField] private PhysicalProductUnitConfiguration[] products;

        private readonly SortedDictionary<string, UnitRecord> unitsById =
            new(StringComparer.Ordinal);
        private int nextUnitOrdinal = 1;

        public int VisibleUnitCount => unitsById.Count;

        public IReadOnlyList<ProductItem> VisibleUnits
        {
            get
            {
                List<ProductItem> result = new(unitsById.Count);
                foreach (UnitRecord record in unitsById.Values)
                {
                    result.Add(record.Item);
                }
                return result;
            }
        }

        public bool TryValidateConfiguration(out string error)
        {
            if (products == null || products.Length == 0)
            {
                error = "Physical product units require at least one product configuration.";
                return false;
            }

            HashSet<string> productIds = new(StringComparer.Ordinal);
            foreach (PhysicalProductUnitConfiguration configuration in products)
            {
                if (configuration == null ||
                    configuration.ProductDefinition == null ||
                    configuration.UnitPrefab == null ||
                    configuration.UnitPrefab.Definition == null ||
                    configuration.LooseSpawnPoint == null ||
                    !FirstStoreIdentifier.IsValid(
                        configuration.ProductDefinition.StableProductId) ||
                    !string.Equals(
                        configuration.UnitPrefab.Definition.StableProductId,
                        configuration.ProductDefinition.StableProductId,
                        StringComparison.Ordinal))
                {
                    error =
                        "Each physical-unit product requires matching product, prefab, and loose-spawn references.";
                    return false;
                }

                if (!productIds.Add(configuration.ProductDefinition.StableProductId))
                {
                    error =
                        $"Duplicate physical-unit product '{configuration.ProductDefinition.StableProductId}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public bool CanMaterialize(
            ProductDefinition productDefinition,
            out string error)
        {
            if (!TryValidateConfiguration(out error))
            {
                return false;
            }

            if (FindConfiguration(productDefinition?.StableProductId) == null)
            {
                error =
                    $"No physical-unit prefab is configured for '{productDefinition?.StableProductId}'.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryMaterializeLooseUnit(
            ProductDefinition productDefinition,
            string looseLocationId,
            out ProductItem item,
            out string error)
        {
            item = null;
            error = null;
            if (!FirstStoreIdentifier.IsValid(looseLocationId) ||
                !CanMaterialize(productDefinition, out error))
            {
                error ??= "Physical loose-unit location is invalid.";
                return false;
            }

            string unitId = $"physical-unit-{nextUnitOrdinal:D6}";
            if (!TryInstantiateUnit(
                    unitId,
                    productDefinition.StableProductId,
                    looseLocationId,
                    out item,
                    out error))
            {
                return false;
            }

            nextUnitOrdinal++;
            return true;
        }

        public bool TryGetOldestUnit(
            string productId,
            string inventoryLocationId,
            out ProductItem item)
        {
            foreach (UnitRecord record in unitsById.Values)
            {
                if (record.Item != null &&
                    record.Item.Definition != null &&
                    string.Equals(
                        record.Item.Definition.StableProductId,
                        productId,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        record.InventoryLocationId,
                        inventoryLocationId,
                        StringComparison.Ordinal))
                {
                    item = record.Item;
                    return true;
                }
            }

            item = null;
            return false;
        }

        public bool TryGetUnit(
            string physicalUnitId,
            out ProductItem item,
            out string inventoryLocationId)
        {
            if (FirstStoreIdentifier.IsValid(physicalUnitId) &&
                unitsById.TryGetValue(physicalUnitId, out UnitRecord record) &&
                record.Item != null)
            {
                item = record.Item;
                inventoryLocationId = record.InventoryLocationId;
                return true;
            }

            item = null;
            inventoryLocationId = null;
            return false;
        }

        public bool TryGetAvailableShelvedUnit(
            string productId,
            string shelfLocationId,
            out ProductItem item)
        {
            foreach (UnitRecord record in unitsById.Values)
            {
                if (record.Item?.Definition != null &&
                    record.Item.IsSnapped &&
                    !record.Item.IsReservedByCustomer &&
                    string.Equals(
                        record.Item.Definition.StableProductId,
                        productId,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        record.InventoryLocationId,
                        shelfLocationId,
                        StringComparison.Ordinal))
                {
                    item = record.Item;
                    return true;
                }
            }

            item = null;
            return false;
        }

        public bool TryGetOldestUnitAtLocation(
            string inventoryLocationId,
            out ProductItem item)
        {
            foreach (UnitRecord record in unitsById.Values)
            {
                if (record.Item != null &&
                    string.Equals(
                        record.InventoryLocationId,
                        inventoryLocationId,
                        StringComparison.Ordinal))
                {
                    item = record.Item;
                    return true;
                }
            }

            item = null;
            return false;
        }

        public bool IsAtLocation(ProductItem item, string inventoryLocationId)
        {
            return item != null &&
                   FirstStoreIdentifier.IsValid(item.PhysicalUnitId) &&
                   unitsById.TryGetValue(item.PhysicalUnitId, out UnitRecord record) &&
                   record.Item == item &&
                   string.Equals(
                       record.InventoryLocationId,
                       inventoryLocationId,
                       StringComparison.Ordinal);
        }

        public bool TryChangeLocation(
            ProductItem item,
            string expectedLocationId,
            string destinationLocationId,
            out string error)
        {
            if (item == null ||
                !FirstStoreIdentifier.IsValid(item.PhysicalUnitId) ||
                !unitsById.TryGetValue(item.PhysicalUnitId, out UnitRecord record) ||
                record.Item != item ||
                !string.Equals(
                    record.InventoryLocationId,
                    expectedLocationId,
                    StringComparison.Ordinal) ||
                !FirstStoreIdentifier.IsValid(destinationLocationId))
            {
                error = "Physical product unit location change is invalid.";
                return false;
            }

            record.InventoryLocationId = destinationLocationId;
            error = null;
            return true;
        }

        public bool CanConsumeShelvedUnits(
            IReadOnlyDictionary<string, string> shelfLocationIdsByProduct,
            IReadOnlyList<CheckoutLineSnapshot> lines,
            out string error)
        {
            if (shelfLocationIdsByProduct == null || lines == null || lines.Count == 0)
            {
                error = "Physical checkout consumption request is empty.";
                return false;
            }

            foreach (CheckoutLineSnapshot line in lines)
            {
                if (line == null || line.quantityUnits <= 0 ||
                    !shelfLocationIdsByProduct.TryGetValue(
                        line.productId,
                        out string shelfLocationId))
                {
                    error = "Physical checkout consumption request is invalid or unmapped.";
                    return false;
                }

                int available = 0;
                foreach (UnitRecord record in unitsById.Values)
                {
                    if (record.Item?.Definition != null &&
                        record.Item.IsSnapped &&
                        !record.Item.IsReservedByCustomer &&
                        string.Equals(
                            record.Item.Definition.StableProductId,
                            line.productId,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            record.InventoryLocationId,
                            shelfLocationId,
                            StringComparison.Ordinal))
                    {
                        available++;
                    }
                }

                if (available < line.quantityUnits)
                {
                    error =
                        $"Physical shelf units for '{line.productId}' do not match checkout stock.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public bool TryConsumeShelvedUnits(
            IReadOnlyDictionary<string, string> shelfLocationIdsByProduct,
            IReadOnlyList<CheckoutLineSnapshot> lines,
            out string error)
        {
            if (!CanConsumeShelvedUnits(
                    shelfLocationIdsByProduct,
                    lines,
                    out error))
            {
                return false;
            }

            List<string> consumedUnitIds = new();
            foreach (CheckoutLineSnapshot line in lines)
            {
                string shelfLocationId = shelfLocationIdsByProduct[line.productId];
                int remaining = line.quantityUnits;
                foreach (KeyValuePair<string, UnitRecord> pair in unitsById)
                {
                    if (remaining > 0 &&
                        pair.Value.Item?.Definition != null &&
                        pair.Value.Item.IsSnapped &&
                        !pair.Value.Item.IsReservedByCustomer &&
                        string.Equals(
                            pair.Value.Item.Definition.StableProductId,
                            line.productId,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            pair.Value.InventoryLocationId,
                            shelfLocationId,
                            StringComparison.Ordinal))
                    {
                        consumedUnitIds.Add(pair.Key);
                        remaining--;
                    }
                }
            }

            foreach (string unitId in consumedUnitIds)
            {
                UnitRecord record = unitsById[unitId];
                if (record.Item != null)
                {
                    record.Item.SnappedFixture?.ReleaseProduct(record.Item);
                    DestroyUnitObject(record.Item.gameObject);
                }
                unitsById.Remove(unitId);
            }

            error = null;
            return true;
        }

        public bool CanConsumeSpecificShelvedUnits(
            IReadOnlyDictionary<string, string> shelfLocationIdsByProduct,
            IReadOnlyList<CheckoutLineSnapshot> lines,
            IReadOnlyList<string> physicalUnitIds,
            out string error)
        {
            if (shelfLocationIdsByProduct == null || lines == null ||
                lines.Count == 0 || physicalUnitIds == null ||
                physicalUnitIds.Count == 0)
            {
                error = "Specific physical checkout consumption request is empty.";
                return false;
            }

            Dictionary<string, int> expectedByProduct =
                new(StringComparer.Ordinal);
            int expectedUnitCount = 0;
            foreach (CheckoutLineSnapshot line in lines)
            {
                if (line == null || line.quantityUnits <= 0 ||
                    !shelfLocationIdsByProduct.ContainsKey(line.productId))
                {
                    error = "Specific physical checkout consumption request is invalid or unmapped.";
                    return false;
                }

                expectedByProduct.TryGetValue(line.productId, out int expected);
                expectedByProduct[line.productId] = checked(expected + line.quantityUnits);
                expectedUnitCount = checked(expectedUnitCount + line.quantityUnits);
            }

            if (physicalUnitIds.Count != expectedUnitCount)
            {
                error = "Specific physical checkout unit count does not match scanned lines.";
                return false;
            }

            HashSet<string> uniqueIds = new(StringComparer.Ordinal);
            Dictionary<string, int> actualByProduct = new(StringComparer.Ordinal);
            foreach (string physicalUnitId in physicalUnitIds)
            {
                if (!FirstStoreIdentifier.IsValid(physicalUnitId) ||
                    !uniqueIds.Add(physicalUnitId) ||
                    !unitsById.TryGetValue(physicalUnitId, out UnitRecord record) ||
                    record.Item?.Definition == null ||
                    !record.Item.IsSnapped)
                {
                    error = "Specific physical checkout contains a missing, duplicate, or unshelved unit.";
                    return false;
                }

                string productId = record.Item.Definition.StableProductId;
                if (!shelfLocationIdsByProduct.TryGetValue(
                        productId,
                        out string shelfLocationId) ||
                    !string.Equals(
                        record.InventoryLocationId,
                        shelfLocationId,
                        StringComparison.Ordinal))
                {
                    error =
                        $"Physical unit '{physicalUnitId}' is not on the mapped shelf for '{productId}'.";
                    return false;
                }

                actualByProduct.TryGetValue(productId, out int actual);
                actualByProduct[productId] = actual + 1;
            }

            if (expectedByProduct.Count != actualByProduct.Count)
            {
                error = "Specific physical checkout products do not match scanned lines.";
                return false;
            }

            foreach (KeyValuePair<string, int> expected in expectedByProduct)
            {
                if (!actualByProduct.TryGetValue(expected.Key, out int actual) ||
                    actual != expected.Value)
                {
                    error =
                        $"Specific physical units for '{expected.Key}' do not match scanned quantity.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public bool TryConsumeSpecificShelvedUnits(
            IReadOnlyDictionary<string, string> shelfLocationIdsByProduct,
            IReadOnlyList<CheckoutLineSnapshot> lines,
            IReadOnlyList<string> physicalUnitIds,
            out string error)
        {
            if (!CanConsumeSpecificShelvedUnits(
                    shelfLocationIdsByProduct,
                    lines,
                    physicalUnitIds,
                    out error))
            {
                return false;
            }

            foreach (string physicalUnitId in physicalUnitIds)
            {
                UnitRecord record = unitsById[physicalUnitId];
                record.Item.SnappedFixture?.ReleaseProduct(record.Item);
                DestroyUnitObject(record.Item.gameObject);
                unitsById.Remove(physicalUnitId);
            }

            error = null;
            return true;
        }

        public bool TryInitializeFromInventory(
            FirstStoreInventory inventory,
            StockingController stocking,
            out string error)
        {
            error = null;
            if (unitsById.Count > 0)
            {
                return TryCapture(
                    inventory,
                    out _,
                    out _,
                    out error);
            }

            if (inventory == null || stocking == null ||
                !TryValidateConfiguration(out error))
            {
                error ??= "Physical inventory initialization is invalid.";
                return false;
            }

            foreach (InventoryLocationSnapshot location in inventory.CreateSnapshot().locations)
            {
                if (location.kind == InventoryLocationKind.DeliveryContainer)
                {
                    continue;
                }

                foreach (InventoryQuantitySnapshot quantity in location.quantities)
                {
                    ProductDefinition product =
                        FindConfiguration(quantity.productId)?.ProductDefinition;
                    if (product == null)
                    {
                        error =
                            $"Visible inventory product '{quantity.productId}' has no physical-unit configuration.";
                        ClearUnits();
                        stocking.ClearPhysicalShelfOccupancy();
                        return false;
                    }

                    for (int index = 0; index < quantity.quantityUnits; index++)
                    {
                        if (!TryMaterializeLooseUnit(
                                product,
                                location.locationId,
                                out ProductItem item,
                                out error))
                        {
                            ClearUnits();
                            stocking.ClearPhysicalShelfOccupancy();
                            return false;
                        }

                        if (location.kind == InventoryLocationKind.Held)
                        {
                            item.PickUp(stocking.HoldPoint);
                        }
                        else if (location.kind == InventoryLocationKind.Shelf &&
                                 !stocking.TryPlaceInitialUnit(
                                     item,
                                     location.locationId,
                                     out error))
                        {
                            ClearUnits();
                            stocking.ClearPhysicalShelfOccupancy();
                            return false;
                        }
                    }
                }
            }

            return TryCapture(inventory, out _, out _, out error);
        }

        public bool TryCapture(
            FirstStoreInventory inventory,
            out List<PhysicalProductUnitSnapshot> snapshots,
            out int capturedNextUnitOrdinal,
            out string error)
        {
            snapshots = new List<PhysicalProductUnitSnapshot>(unitsById.Count);
            capturedNextUnitOrdinal = nextUnitOrdinal;
            error = null;
            if (inventory == null || !TryValidateConfiguration(out error))
            {
                error ??= "Physical-unit capture requires valid inventory and configuration.";
                return false;
            }

            foreach (KeyValuePair<string, UnitRecord> pair in unitsById)
            {
                ProductItem item = pair.Value.Item;
                if (item == null || item.Definition == null)
                {
                    error = $"Physical unit '{pair.Key}' is missing its scene object or product.";
                    return false;
                }

                if (!inventory.TryGetLocationKind(
                        pair.Value.InventoryLocationId,
                        out InventoryLocationKind kind) ||
                    kind == InventoryLocationKind.DeliveryContainer)
                {
                    error = $"Physical unit '{pair.Key}' has a non-visible inventory location.";
                    return false;
                }

                string fixtureId = null;
                string snapPointId = null;
                int quarterTurns = 0;
                if (kind == InventoryLocationKind.Shelf)
                {
                    if (!item.TryGetPlacementState(out PlacedProductState placement))
                    {
                        error = $"Shelved physical unit '{pair.Key}' has no shelf placement.";
                        return false;
                    }

                    fixtureId = placement.fixtureId;
                    snapPointId = placement.snapPointId;
                    quarterTurns = placement.quarterTurns;
                }
                else if ((kind == InventoryLocationKind.Held) != item.IsHeld ||
                         item.IsSnapped)
                {
                    error = $"Physical unit '{pair.Key}' does not match its inventory location.";
                    return false;
                }

                snapshots.Add(
                    new PhysicalProductUnitSnapshot(
                        pair.Key,
                        item.Definition.StableProductId,
                        pair.Value.InventoryLocationId,
                        fixtureId,
                        snapPointId,
                        quarterTurns));
            }

            return ValidateCounts(inventory, snapshots, out error);
        }

        public bool CanApplySnapshot(
            FirstStoreInventory inventory,
            IReadOnlyList<PhysicalProductUnitSnapshot> snapshots,
            int restoredNextUnitOrdinal,
            StockingController stocking,
            out string error)
        {
            error = null;
            if (inventory == null || snapshots == null || stocking == null ||
                restoredNextUnitOrdinal <= 0 ||
                !TryValidateConfiguration(out error))
            {
                error ??= "Physical-unit restore data is missing or invalid.";
                return false;
            }

            HashSet<string> unitIds = new(StringComparer.Ordinal);
            HashSet<string> shelfPlacements = new(StringComparer.Ordinal);
            foreach (PhysicalProductUnitSnapshot snapshot in snapshots)
            {
                if (snapshot == null ||
                    !FirstStoreIdentifier.IsValid(snapshot.physicalUnitId) ||
                    !unitIds.Add(snapshot.physicalUnitId) ||
                    FindConfiguration(snapshot.productId) == null ||
                    !inventory.IsKnownProduct(snapshot.productId) ||
                    !inventory.TryGetLocationKind(
                        snapshot.inventoryLocationId,
                        out InventoryLocationKind kind) ||
                    kind == InventoryLocationKind.DeliveryContainer ||
                    snapshot.quarterTurns < 0 || snapshot.quarterTurns > 3)
                {
                    error = "Physical-unit restore entry is invalid or duplicated.";
                    return false;
                }

                const string generatedUnitPrefix = "physical-unit-";
                if (snapshot.physicalUnitId.StartsWith(
                        generatedUnitPrefix,
                        StringComparison.Ordinal) &&
                    int.TryParse(
                        snapshot.physicalUnitId.Substring(generatedUnitPrefix.Length),
                        out int generatedOrdinal) &&
                    generatedOrdinal >= restoredNextUnitOrdinal)
                {
                    error =
                        "Physical-unit next ordinal does not follow generated unit ids.";
                    return false;
                }

                bool hasShelfPlacement =
                    FirstStoreIdentifier.IsValid(snapshot.shelfFixtureId) &&
                    FirstStoreIdentifier.IsValid(snapshot.shelfSnapPointId);
                if ((kind == InventoryLocationKind.Shelf) != hasShelfPlacement ||
                    (kind != InventoryLocationKind.Shelf && snapshot.quarterTurns != 0))
                {
                    error =
                        $"Physical unit '{snapshot.physicalUnitId}' placement does not match its inventory location.";
                    return false;
                }

                if (kind == InventoryLocationKind.Shelf &&
                    !stocking.CanPlaceRestoredUnit(snapshot, out error))
                {
                    return false;
                }

                if (kind == InventoryLocationKind.Shelf &&
                    !shelfPlacements.Add(
                        $"{snapshot.shelfFixtureId}\n{snapshot.shelfSnapPointId}"))
                {
                    error =
                        $"Physical shelf placement '{snapshot.shelfFixtureId}/{snapshot.shelfSnapPointId}' is duplicated.";
                    return false;
                }
            }

            return ValidateCounts(inventory, snapshots, out error);
        }

        public bool TryApplySnapshot(
            FirstStoreInventory inventory,
            IReadOnlyList<PhysicalProductUnitSnapshot> snapshots,
            int restoredNextUnitOrdinal,
            StockingController stocking,
            out string error)
        {
            if (!CanApplySnapshot(
                    inventory,
                    snapshots,
                    restoredNextUnitOrdinal,
                    stocking,
                    out error))
            {
                return false;
            }

            stocking.ClearPhysicalShelfOccupancy();
            ClearUnits();
            nextUnitOrdinal = restoredNextUnitOrdinal;

            List<PhysicalProductUnitSnapshot> ordered = new(snapshots);
            ordered.Sort((left, right) =>
                string.CompareOrdinal(left.physicalUnitId, right.physicalUnitId));
            foreach (PhysicalProductUnitSnapshot snapshot in ordered)
            {
                if (!TryInstantiateUnit(
                        snapshot.physicalUnitId,
                        snapshot.productId,
                        snapshot.inventoryLocationId,
                        out ProductItem item,
                        out error))
                {
                    ClearUnits();
                    stocking.ClearPhysicalShelfOccupancy();
                    return false;
                }

                inventory.TryGetLocationKind(
                    snapshot.inventoryLocationId,
                    out InventoryLocationKind kind);
                if (kind == InventoryLocationKind.Held)
                {
                    item.PickUp(stocking.HoldPoint);
                }
                else if (kind == InventoryLocationKind.Shelf &&
                         !stocking.TryPlaceRestoredUnit(item, snapshot, out error))
                {
                    ClearUnits();
                    stocking.ClearPhysicalShelfOccupancy();
                    return false;
                }
            }

            error = null;
            return true;
        }

        private bool TryInstantiateUnit(
            string unitId,
            string productId,
            string inventoryLocationId,
            out ProductItem item,
            out string error)
        {
            item = null;
            PhysicalProductUnitConfiguration configuration =
                FindConfiguration(productId);
            if (configuration == null || unitsById.ContainsKey(unitId))
            {
                error = $"Physical unit '{unitId}' cannot be materialized.";
                return false;
            }

            try
            {
                int looseIndex = CountUnits(productId, inventoryLocationId);
                Vector3 position = configuration.LooseSpawnPoint.position +
                                   configuration.LooseSpawnPoint.TransformVector(
                                       configuration.LooseUnitSpacing * looseIndex);
                item = Instantiate(
                    configuration.UnitPrefab,
                    position,
                    configuration.LooseSpawnPoint.rotation,
                    configuration.LooseSpawnPoint.parent);
                item.name = $"{configuration.ProductDefinition.DisplayName} {unitId}";
                item.gameObject.SetActive(true);
                item.AssignPhysicalUnitId(unitId);
                item.ApplyLoosePlacement(
                    configuration.LooseSpawnPoint.parent,
                    position,
                    configuration.LooseSpawnPoint.rotation);
                unitsById.Add(unitId, new UnitRecord(item, inventoryLocationId));
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                if (item != null)
                {
                    DestroyUnitObject(item.gameObject);
                }
                item = null;
                error = $"Physical unit '{unitId}' could not be created: {exception.Message}";
                return false;
            }
        }

        private int CountUnits(string productId, string inventoryLocationId)
        {
            int count = 0;
            foreach (UnitRecord record in unitsById.Values)
            {
                if (record.Item?.Definition != null &&
                    string.Equals(
                        record.Item.Definition.StableProductId,
                        productId,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        record.InventoryLocationId,
                        inventoryLocationId,
                        StringComparison.Ordinal))
                {
                    count++;
                }
            }
            return count;
        }

        private PhysicalProductUnitConfiguration FindConfiguration(string productId)
        {
            if (!FirstStoreIdentifier.IsValid(productId) || products == null)
            {
                return null;
            }

            foreach (PhysicalProductUnitConfiguration configuration in products)
            {
                if (configuration?.ProductDefinition != null &&
                    string.Equals(
                        configuration.ProductDefinition.StableProductId,
                        productId,
                        StringComparison.Ordinal))
                {
                    return configuration;
                }
            }
            return null;
        }

        private static bool ValidateCounts(
            FirstStoreInventory inventory,
            IReadOnlyList<PhysicalProductUnitSnapshot> snapshots,
            out string error)
        {
            Dictionary<string, int> expected = new(StringComparer.Ordinal);
            foreach (InventoryLocationSnapshot location in inventory.CreateSnapshot().locations)
            {
                if (location.kind == InventoryLocationKind.DeliveryContainer)
                {
                    continue;
                }

                foreach (InventoryQuantitySnapshot quantity in location.quantities)
                {
                    expected.Add(
                        CreateCountKey(location.locationId, quantity.productId),
                        quantity.quantityUnits);
                }
            }

            Dictionary<string, int> actual = new(StringComparer.Ordinal);
            foreach (PhysicalProductUnitSnapshot snapshot in snapshots)
            {
                string key = CreateCountKey(
                    snapshot.inventoryLocationId,
                    snapshot.productId);
                actual.TryGetValue(key, out int count);
                actual[key] = count + 1;
            }

            if (expected.Count != actual.Count)
            {
                error = "Physical-unit counts do not match visible domain inventory.";
                return false;
            }

            foreach (KeyValuePair<string, int> pair in expected)
            {
                if (!actual.TryGetValue(pair.Key, out int count) || count != pair.Value)
                {
                    error =
                        $"Physical-unit count for '{pair.Key.Replace('\n', '/')}' does not match domain inventory.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static string CreateCountKey(string locationId, string productId)
        {
            return $"{locationId}\n{productId}";
        }

        private void ClearUnits()
        {
            foreach (UnitRecord record in unitsById.Values)
            {
                if (record.Item != null)
                {
                    DestroyUnitObject(record.Item.gameObject);
                }
            }
            unitsById.Clear();
        }

        private static void DestroyUnitObject(GameObject gameObject)
        {
            gameObject.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }
    }
}
