using System;
using System.Collections.Generic;
using UnityEngine;

namespace Margins
{
    [Serializable]
    public sealed class StockingProductConfiguration
    {
        [SerializeField] private ProductDefinition productDefinition;
        [SerializeField] private ShelfFixture shelfFixture;
        [SerializeField] private string shelfLocationId;
        [SerializeField] private string[] snapPointIds;

        public ProductDefinition ProductDefinition => productDefinition;
        public ShelfFixture ShelfFixture => shelfFixture;
        public string ShelfLocationId => shelfLocationId;
        public IReadOnlyList<string> SnapPointIds => snapPointIds;
    }

    public sealed class StockingController : MonoBehaviour
    {
        [SerializeField] private FirstStoreInventoryComponent inventoryComponent;
        [SerializeField] private PhysicalProductUnitRegistry physicalUnits;
        [SerializeField] private Transform holdPoint;
        [SerializeField] private string looseLocationId;
        [SerializeField] private string heldLocationId;
        [SerializeField] private StockingProductConfiguration[] products;

        public FirstStoreInventoryComponent InventoryComponent => inventoryComponent;
        public PhysicalProductUnitRegistry PhysicalUnits => physicalUnits;
        public Transform HoldPoint => holdPoint;
        public ProductItem HeldPhysicalUnit
        {
            get
            {
                if (physicalUnits != null &&
                    physicalUnits.TryGetOldestUnitAtLocation(
                        heldLocationId,
                        out ProductItem item) &&
                    item.IsHeld)
                {
                    return item;
                }

                return null;
            }
        }

        public bool HasHeldUnit
        {
            get
            {
                if (inventoryComponent == null || !inventoryComponent.IsInitialized)
                {
                    return false;
                }

                foreach (InventoryLocationSnapshot location in
                         inventoryComponent.Inventory.CreateSnapshot().locations)
                {
                    if (string.Equals(
                            location.locationId,
                            heldLocationId,
                            StringComparison.Ordinal))
                    {
                        return location.quantities.Count > 0;
                    }
                }
                return false;
            }
        }

        public bool TryValidateConfiguration(out string error)
        {
            error = null;
            if (inventoryComponent == null || !inventoryComponent.IsInitialized)
            {
                error = "Stocking requires an initialized inventory component.";
                return false;
            }

            if (physicalUnits == null || holdPoint == null ||
                !physicalUnits.TryValidateConfiguration(out error))
            {
                error ??=
                    "Stocking requires explicit physical-unit and hold-point references.";
                return false;
            }

            if (!FirstStoreIdentifier.IsValid(looseLocationId) ||
                !FirstStoreIdentifier.IsValid(heldLocationId))
            {
                error = "Stocking loose and held location identifiers are invalid.";
                return false;
            }

            if (!inventoryComponent.Inventory.TryGetLocationKind(
                    looseLocationId,
                    out InventoryLocationKind looseKind) ||
                looseKind != InventoryLocationKind.Loose)
            {
                error = $"Stocking source '{looseLocationId}' is not a loose location.";
                return false;
            }

            if (!inventoryComponent.Inventory.TryGetLocationKind(
                    heldLocationId,
                    out InventoryLocationKind heldKind) ||
                heldKind != InventoryLocationKind.Held)
            {
                error = $"Stocking source '{heldLocationId}' is not a held location.";
                return false;
            }

            if (products == null || products.Length == 0)
            {
                error = "Stocking requires at least one explicit product mapping.";
                return false;
            }

            HashSet<string> productIds = new(StringComparer.Ordinal);
            HashSet<string> configuredSnapPoints = new(StringComparer.Ordinal);
            foreach (StockingProductConfiguration configuration in products)
            {
                if (configuration == null ||
                    configuration.ProductDefinition == null ||
                    configuration.ShelfFixture == null ||
                    !inventoryComponent.Inventory.IsKnownProduct(
                        configuration.ProductDefinition.StableProductId) ||
                    !productIds.Add(
                        configuration.ProductDefinition.StableProductId) ||
                    !physicalUnits.CanMaterialize(
                        configuration.ProductDefinition,
                        out error))
                {
                    error ??=
                        "Each stocking product requires one registered product, physical prefab, and shelf.";
                    return false;
                }

                if (!FirstStoreIdentifier.IsValid(configuration.ShelfLocationId) ||
                    !inventoryComponent.Inventory.TryGetLocationKind(
                        configuration.ShelfLocationId,
                        out InventoryLocationKind shelfKind) ||
                    shelfKind != InventoryLocationKind.Shelf)
                {
                    error =
                        $"Stocking product '{configuration.ProductDefinition.StableProductId}' requires a valid shelf location.";
                    return false;
                }

                if (configuration.SnapPointIds == null ||
                    configuration.SnapPointIds.Count == 0)
                {
                    error =
                        $"Stocking product '{configuration.ProductDefinition.StableProductId}' requires at least one shelf snap point.";
                    return false;
                }

                foreach (string snapPointId in configuration.SnapPointIds)
                {
                    string uniqueSnapPoint =
                        $"{configuration.ShelfFixture.StableFixtureId}\n{snapPointId}";
                    if (!FirstStoreIdentifier.IsValid(snapPointId) ||
                        !configuredSnapPoints.Add(uniqueSnapPoint) ||
                        !configuration.ShelfFixture.TryGetSnapPoint(
                            snapPointId,
                            out ShelfSnapPointDefinition snapPoint) ||
                        !snapPoint.Accepts(
                            configuration.ProductDefinition.SnapCompatibilityTag))
                    {
                        error =
                            $"Stocking snap point '{snapPointId}' is missing, duplicated, or incompatible.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        public bool TryInitializeDependencies(out string error)
        {
            if (inventoryComponent == null)
            {
                error = "Stocking requires an inventory component.";
                return false;
            }

            if (!inventoryComponent.TryInitialize(out error))
            {
                error = $"Stocking could not initialize inventory: {error}";
                return false;
            }

            if (!TryValidateConfiguration(out error))
            {
                return false;
            }

            if (!physicalUnits.TryInitializeFromInventory(
                    inventoryComponent.Inventory,
                    this,
                    out error))
            {
                error = $"Stocking could not reconcile visible inventory: {error}";
                return false;
            }

            return true;
        }

        public bool TryPickUpLooseUnit(
            ProductDefinition productDefinition,
            out ProductItem selectedUnit,
            out string error)
        {
            selectedUnit = null;
            if (!TryInitializeDependencies(out error))
            {
                return false;
            }

            StockingProductConfiguration configuration =
                FindProduct(productDefinition?.StableProductId);
            if (configuration == null ||
                !physicalUnits.TryGetOldestUnit(
                    configuration.ProductDefinition.StableProductId,
                    looseLocationId,
                    out ProductItem oldestUnit) ||
                !physicalUnits.IsAtLocation(oldestUnit, looseLocationId))
            {
                error = "No matching loose physical product unit is available.";
                return false;
            }

            return TryPickUpLooseUnit(
                configuration,
                oldestUnit,
                out selectedUnit,
                out error);
        }

        public bool TryPickUpLooseUnit(
            ProductItem targetedUnit,
            out ProductItem selectedUnit,
            out string error)
        {
            selectedUnit = null;
            if (!TryInitializeDependencies(out error))
            {
                return false;
            }

            StockingProductConfiguration configuration =
                FindProduct(targetedUnit?.Definition?.StableProductId);
            if (configuration == null ||
                !physicalUnits.IsAtLocation(targetedUnit, looseLocationId) ||
                targetedUnit.IsHeld ||
                targetedUnit.IsSnapped)
            {
                error = "The targeted physical product unit is not available at the loose location.";
                return false;
            }

            return TryPickUpLooseUnit(
                configuration,
                targetedUnit,
                out selectedUnit,
                out error);
        }

        private bool TryPickUpLooseUnit(
            StockingProductConfiguration configuration,
            ProductItem unit,
            out ProductItem selectedUnit,
            out string error)
        {
            selectedUnit = null;
            string productId = configuration.ProductDefinition.StableProductId;
            InventoryTransferResult preview =
                inventoryComponent.Inventory.CanTransfer(
                    productId,
                    looseLocationId,
                    heldLocationId,
                    1);
            if (!preview.IsSuccess)
            {
                error = $"Product pickup transfer rejected ({preview.Failure}).";
                return false;
            }

            if (!physicalUnits.TryChangeLocation(
                    unit,
                    looseLocationId,
                    heldLocationId,
                    out error))
            {
                return false;
            }

            InventoryTransferResult transfer =
                inventoryComponent.Inventory.TryTransfer(
                    productId,
                    looseLocationId,
                    heldLocationId,
                    1);
            if (!transfer.IsSuccess)
            {
                bool rolledBack = physicalUnits.TryChangeLocation(
                    unit,
                    heldLocationId,
                    looseLocationId,
                    out string rollbackError);
                Debug.LogError(
                    $"Domain pickup rejected after physical reservation " +
                    $"({transfer.Failure}); physical rollback {rolledBack}: " +
                    $"{rollbackError ?? "ok"}.",
                    this);
                error = $"Product pickup transfer rejected ({transfer.Failure}).";
                return false;
            }

            unit.PickUp(holdPoint);
            selectedUnit = unit;
            error = null;
            return true;
        }

        public bool TryStockHeldUnit(int quarterTurns, out string error)
        {
            if (!TryInitializeDependencies(out error))
            {
                return false;
            }

            if (!physicalUnits.TryGetOldestUnitAtLocation(
                    heldLocationId,
                    out ProductItem item) ||
                !item.IsHeld)
            {
                error = "No physical product unit is held.";
                return false;
            }

            StockingProductConfiguration configuration =
                FindProduct(item.Definition?.StableProductId);
            if (configuration == null ||
                !TryFindAvailableSnapPoint(
                    configuration,
                    out string snapPointId))
            {
                item.SetPlacementPreview(false);
                error = "No configured shelf snap point is available for the held product.";
                return false;
            }

            return TryStockHeldUnit(
                item,
                configuration,
                snapPointId,
                quarterTurns,
                out error);
        }

        public bool TryStockHeldUnit(
            ShelfFixture targetedShelf,
            string targetedSnapPointId,
            int quarterTurns,
            out string error)
        {
            if (!TryInitializeDependencies(out error))
            {
                return false;
            }

            if (!physicalUnits.TryGetOldestUnitAtLocation(
                    heldLocationId,
                    out ProductItem item) ||
                !item.IsHeld)
            {
                error = "No physical product unit is held.";
                return false;
            }

            if (!CanStockHeldUnit(
                    targetedShelf,
                    targetedSnapPointId,
                    out error))
            {
                return false;
            }

            StockingProductConfiguration configuration =
                FindProduct(item.Definition?.StableProductId);
            return TryStockHeldUnit(
                item,
                configuration,
                targetedSnapPointId,
                quarterTurns,
                out error);
        }

        public bool CanStockHeldUnit(
            ShelfFixture targetedShelf,
            string targetedSnapPointId,
            out string reason)
        {
            if (!TryValidateConfiguration(out reason))
            {
                reason = "Stocking is not ready.";
                return false;
            }

            if (!physicalUnits.TryGetOldestUnitAtLocation(
                    heldLocationId,
                    out ProductItem item) ||
                !item.IsHeld)
            {
                reason = "No product is held.";
                return false;
            }

            StockingProductConfiguration configuration =
                FindProduct(item.Definition?.StableProductId);
            if (configuration == null ||
                configuration.ShelfFixture != targetedShelf ||
                !ContainsSnapPoint(configuration, targetedSnapPointId) ||
                !targetedShelf.TryGetSnapPoint(
                    targetedSnapPointId,
                    out ShelfSnapPointDefinition snapPoint) ||
                !snapPoint.Accepts(item.Definition.SnapCompatibilityTag))
            {
                reason = "The targeted shelf position does not accept the held product.";
                return false;
            }

            if (targetedShelf.IsOccupied(targetedSnapPointId))
            {
                reason = "That shelf position is occupied.";
                return false;
            }

            InventoryTransferResult transfer = inventoryComponent.Inventory.CanTransfer(
                configuration.ProductDefinition.StableProductId,
                heldLocationId,
                configuration.ShelfLocationId,
                1);
            if (!transfer.IsSuccess)
            {
                reason = "That shelf cannot accept another product.";
                return false;
            }

            reason = null;
            return true;
        }

        private bool TryStockHeldUnit(
            ProductItem item,
            StockingProductConfiguration configuration,
            string snapPointId,
            int quarterTurns,
            out string error)
        {

            string productId = configuration.ProductDefinition.StableProductId;
            InventoryTransferResult preview =
                inventoryComponent.Inventory.CanTransfer(
                    productId,
                    heldLocationId,
                    configuration.ShelfLocationId,
                    1);
            if (!preview.IsSuccess)
            {
                item.SetPlacementPreview(false);
                error = $"Stocking transfer rejected ({preview.Failure}).";
                return false;
            }

            if (!configuration.ShelfFixture.TryPlaceAt(
                    item,
                    snapPointId,
                    quarterTurns,
                    out PlacementFailure placementFailure))
            {
                item.SetPlacementPreview(false);
                error = $"Physical shelf placement rejected ({placementFailure}).";
                return false;
            }

            InventoryTransferResult transfer =
                inventoryComponent.Inventory.TryTransfer(
                    productId,
                    heldLocationId,
                    configuration.ShelfLocationId,
                    1);
            if (!transfer.IsSuccess)
            {
                item.PickUp(holdPoint);
                error = $"Stocking transfer failed after placement ({transfer.Failure}).";
                return false;
            }

            if (!physicalUnits.TryChangeLocation(
                    item,
                    heldLocationId,
                    configuration.ShelfLocationId,
                    out error))
            {
                InventoryTransferResult rollback =
                    inventoryComponent.Inventory.TryTransfer(
                        productId,
                        configuration.ShelfLocationId,
                        heldLocationId,
                        1);
                item.PickUp(holdPoint);
                Debug.LogError(
                    $"Physical stocking rollback applied ({rollback.Failure}): {error}",
                    this);
                return false;
            }

            error = null;
            return true;
        }

        public bool TryGetShelfLocation(string productId, out string shelfLocationId)
        {
            StockingProductConfiguration configuration = FindProduct(productId);
            shelfLocationId = configuration?.ShelfLocationId;
            return configuration != null;
        }

        internal bool TryPlaceInitialUnit(
            ProductItem item,
            string shelfLocationId,
            out string error)
        {
            StockingProductConfiguration configuration =
                FindProduct(item?.Definition?.StableProductId);
            PlacementFailure failure = PlacementFailure.None;
            if (configuration == null ||
                !string.Equals(
                    configuration.ShelfLocationId,
                    shelfLocationId,
                    StringComparison.Ordinal) ||
                !TryFindAvailableSnapPoint(configuration, out string snapPointId) ||
                !configuration.ShelfFixture.TryPlaceAt(
                    item,
                    snapPointId,
                    0,
                    out failure))
            {
                error =
                    $"Initial physical shelf placement failed ({failure}).";
                return false;
            }

            error = null;
            return true;
        }

        internal bool CanPlaceRestoredUnit(
            PhysicalProductUnitSnapshot snapshot,
            out string error)
        {
            StockingProductConfiguration configuration =
                FindProduct(snapshot?.productId);
            if (configuration == null || snapshot == null ||
                !string.Equals(
                    configuration.ShelfLocationId,
                    snapshot.inventoryLocationId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    configuration.ShelfFixture.StableFixtureId,
                    snapshot.shelfFixtureId,
                    StringComparison.Ordinal) ||
                !ContainsSnapPoint(configuration, snapshot.shelfSnapPointId))
            {
                error =
                    $"Restored physical shelf placement for '{snapshot?.physicalUnitId}' is not configured.";
                return false;
            }

            error = null;
            return true;
        }

        internal bool TryPlaceRestoredUnit(
            ProductItem item,
            PhysicalProductUnitSnapshot snapshot,
            out string error)
        {
            if (!CanPlaceRestoredUnit(snapshot, out error))
            {
                return false;
            }

            StockingProductConfiguration configuration =
                FindProduct(snapshot.productId);
            if (!configuration.ShelfFixture.TryPlaceAt(
                    item,
                    snapshot.shelfSnapPointId,
                    snapshot.quarterTurns,
                    out PlacementFailure failure))
            {
                error =
                    $"Restored physical shelf placement failed ({failure}).";
                return false;
            }

            error = null;
            return true;
        }

        internal void ClearPhysicalShelfOccupancy()
        {
            if (products == null)
            {
                return;
            }

            HashSet<ShelfFixture> cleared = new();
            foreach (StockingProductConfiguration configuration in products)
            {
                if (configuration?.ShelfFixture != null &&
                    cleared.Add(configuration.ShelfFixture))
                {
                    configuration.ShelfFixture.ClearRuntimeOccupancy();
                }
            }
        }

        private StockingProductConfiguration FindProduct(string productId)
        {
            if (!FirstStoreIdentifier.IsValid(productId) || products == null)
            {
                return null;
            }

            foreach (StockingProductConfiguration configuration in products)
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

        private static bool TryFindAvailableSnapPoint(
            StockingProductConfiguration configuration,
            out string snapPointId)
        {
            foreach (string candidate in configuration.SnapPointIds)
            {
                if (!configuration.ShelfFixture.IsOccupied(candidate))
                {
                    snapPointId = candidate;
                    return true;
                }
            }

            snapPointId = null;
            return false;
        }

        private static bool ContainsSnapPoint(
            StockingProductConfiguration configuration,
            string snapPointId)
        {
            foreach (string candidate in configuration.SnapPointIds)
            {
                if (string.Equals(candidate, snapPointId, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
