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
                    out selectedUnit) ||
                !physicalUnits.IsAtLocation(selectedUnit, looseLocationId))
            {
                error = "No matching loose physical product unit is available.";
                selectedUnit = null;
                return false;
            }

            string productId = configuration.ProductDefinition.StableProductId;
            InventoryTransferResult transfer =
                inventoryComponent.Inventory.TryTransfer(
                    productId,
                    looseLocationId,
                    heldLocationId,
                    1);
            if (!transfer.IsSuccess)
            {
                error = $"Product pickup transfer rejected ({transfer.Failure}).";
                selectedUnit = null;
                return false;
            }

            selectedUnit.PickUp(holdPoint);
            if (!physicalUnits.TryChangeLocation(
                    selectedUnit,
                    looseLocationId,
                    heldLocationId,
                    out error))
            {
                InventoryTransferResult rollback =
                    inventoryComponent.Inventory.TryTransfer(
                        productId,
                        heldLocationId,
                        looseLocationId,
                        1);
                selectedUnit.ReleaseLoose();
                Debug.LogError(
                    $"Physical pickup rollback applied ({rollback.Failure}): {error}",
                    this);
                selectedUnit = null;
                return false;
            }

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
