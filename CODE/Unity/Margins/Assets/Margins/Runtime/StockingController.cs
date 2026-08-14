using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly struct ResolvedStockingDestination
        {
            public ResolvedStockingDestination(
                ProductDefinition productDefinition,
                ShelfFixture shelfFixture,
                string shelfLocationId,
                IReadOnlyList<string> snapPointIds)
            {
                ProductDefinition = productDefinition;
                ShelfFixture = shelfFixture;
                ShelfLocationId = shelfLocationId;
                SnapPointIds = snapPointIds;
            }

            public ProductDefinition ProductDefinition { get; }
            public ShelfFixture ShelfFixture { get; }
            public string ShelfLocationId { get; }
            public IReadOnlyList<string> SnapPointIds { get; }
        }

        [SerializeField] private FirstStoreInventoryComponent inventoryComponent;
        [SerializeField] private PhysicalProductUnitRegistry physicalUnits;
        [SerializeField] private FirstStoreMerchandisingComponent merchandising;
        [SerializeField] private Transform holdPoint;
        [SerializeField] private string looseLocationId;
        [SerializeField] private string heldLocationId;
        [SerializeField] private StockingProductConfiguration[] products;

        public FirstStoreInventoryComponent InventoryComponent => inventoryComponent;
        public PhysicalProductUnitRegistry PhysicalUnits => physicalUnits;
        public FirstStoreMerchandisingComponent Merchandising => merchandising;
        public Transform HoldPoint => holdPoint;
        public ProductItem HeldPhysicalUnit
        {
            get
            {
                if (physicalUnits != null &&
                    physicalUnits.TryGetOldestUnitAtLocation(
                        heldLocationId,
                        out ProductItem item) &&
                    item.IsHeld &&
                    holdPoint != null &&
                    item.transform.IsChildOf(holdPoint))
                {
                    return item;
                }

                return null;
            }
        }
        public bool PlayerHasHeldUnit => HeldPhysicalUnit != null;
        public bool IsAnotherCarrierUsingHeldInventory =>
            HasHeldUnit && !PlayerHasHeldUnit;

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

            if (physicalUnits == null || holdPoint == null || merchandising == null ||
                !physicalUnits.TryValidateConfiguration(out error))
            {
                error ??=
                    "Stocking requires explicit physical-unit, merchandising, and hold-point references.";
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

            if (!merchandising.TryValidateStockingConfiguration(this, out error))
            {
                return false;
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
                holdPoint,
                out selectedUnit,
                out error);
        }

        public bool TryPickUpLooseUnit(
            ProductItem targetedUnit,
            out ProductItem selectedUnit,
            out string error)
        {
            return TryPickUpLooseUnit(
                targetedUnit,
                holdPoint,
                out selectedUnit,
                out error);
        }

        public bool TryPickUpLooseUnit(
            ProductItem targetedUnit,
            Transform carrierPoint,
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
            if (carrierPoint == null ||
                configuration == null ||
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
                carrierPoint,
                out selectedUnit,
                out error);
        }

        private bool TryPickUpLooseUnit(
            StockingProductConfiguration configuration,
            ProductItem unit,
            Transform carrierPoint,
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

            unit.PickUp(carrierPoint);
            selectedUnit = unit;
            error = null;
            return true;
        }

        public bool TrySetDownPlayerHeldUnit(
            out ProductItem releasedUnit,
            out string error)
        {
            releasedUnit = null;
            if (!TryInitializeDependencies(out error))
            {
                return false;
            }

            ProductItem item = HeldPhysicalUnit;
            if (item == null || item.Definition == null)
            {
                error = IsAnotherCarrierUsingHeldInventory
                    ? "A team member is moving stock. Try again in a moment."
                    : "You are not holding a product.";
                return false;
            }

            string productId = item.Definition.StableProductId;
            InventoryTransferResult preview =
                inventoryComponent.Inventory.CanTransfer(
                    productId,
                    heldLocationId,
                    looseLocationId,
                    1);
            if (!preview.IsSuccess)
            {
                error = "There is no safe place to put this product down.";
                return false;
            }

            if (!physicalUnits.TryChangeLocation(
                    item,
                    heldLocationId,
                    looseLocationId,
                    out error))
            {
                return false;
            }

            InventoryTransferResult transfer =
                inventoryComponent.Inventory.TryTransfer(
                    productId,
                    heldLocationId,
                    looseLocationId,
                    1);
            if (!transfer.IsSuccess)
            {
                bool rolledBack = physicalUnits.TryChangeLocation(
                    item,
                    looseLocationId,
                    heldLocationId,
                    out string rollbackError);
                Debug.LogError(
                    $"Held-product set-down failed after physical reservation " +
                    $"({transfer.Failure}); physical rollback {rolledBack}: " +
                    $"{rollbackError ?? "ok"}.",
                    this);
                error = "The product could not be put down. It is still in your hands.";
                return false;
            }

            item.ReleaseLoose(showInvalidPlacementFeedback: false);
            releasedUnit = item;
            error = null;
            return true;
        }

        public bool HasAvailableShelfPosition(
            ProductDefinition productDefinition,
            out string reason)
        {
            if (!TryValidateConfiguration(out reason))
            {
                reason = "Stocking is not ready.";
                return false;
            }

            if (!TryResolveDestination(
                    productDefinition?.StableProductId,
                    out ResolvedStockingDestination destination))
            {
                reason = "This product has no assigned shelf.";
                return false;
            }

            if (!TryFindAvailableSnapPoint(destination, out _))
            {
                reason = "The assigned shelf is full.";
                return false;
            }

            reason = null;
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

            if (!TryResolveDestination(
                    item.Definition?.StableProductId,
                    out ResolvedStockingDestination destination) ||
                !TryFindAvailableSnapPoint(
                    destination,
                    out string snapPointId))
            {
                item.SetPlacementPreview(false);
                error = "No configured shelf snap point is available for the held product.";
                return false;
            }

            return TryStockHeldUnit(
                item,
                destination,
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

            if (!TryResolveDestination(
                    item.Definition?.StableProductId,
                    out ResolvedStockingDestination destination))
            {
                error = "The held product no longer has an assigned shelf.";
                return false;
            }
            return TryStockHeldUnit(
                item,
                destination,
                targetedSnapPointId,
                quarterTurns,
                out error);
        }

        public bool TryStockHeldUnit(
            ShelfFixture targetedShelf,
            int quarterTurns,
            out string error)
        {
            if (!TryGetAvailableShelfPosition(
                    targetedShelf,
                    out _,
                    out string snapPointId,
                    out error))
            {
                return false;
            }

            return TryStockHeldUnit(
                targetedShelf,
                snapPointId,
                quarterTurns,
                out error);
        }

        public bool TryGetAvailableShelfPosition(
            ShelfFixture targetedShelf,
            out ProductDefinition productDefinition,
            out string snapPointId,
            out string reason)
        {
            productDefinition = null;
            snapPointId = null;
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

            bool resolved = TryResolveDestination(
                item.Definition?.StableProductId,
                out ResolvedStockingDestination destination);
            productDefinition = resolved ? destination.ProductDefinition : null;
            if (!resolved || destination.ShelfFixture != targetedShelf)
            {
                reason = "This shelf does not accept the held product.";
                return false;
            }

            if (!TryFindAvailableSnapPoint(destination, out snapPointId))
            {
                reason = "This shelf is full.";
                return false;
            }

            InventoryTransferResult transfer = inventoryComponent.Inventory.CanTransfer(
                destination.ProductDefinition.StableProductId,
                heldLocationId,
                destination.ShelfLocationId,
                1);
            if (!transfer.IsSuccess)
            {
                snapPointId = null;
                reason = "This shelf cannot accept another product.";
                return false;
            }

            reason = null;
            return true;
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

            if (!TryResolveDestination(
                    item.Definition?.StableProductId,
                    out ResolvedStockingDestination destination) ||
                destination.ShelfFixture != targetedShelf ||
                !ContainsSnapPoint(destination, targetedSnapPointId) ||
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
                destination.ProductDefinition.StableProductId,
                heldLocationId,
                destination.ShelfLocationId,
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
            ResolvedStockingDestination destination,
            string snapPointId,
            int quarterTurns,
            out string error)
        {

            string productId = destination.ProductDefinition.StableProductId;
            InventoryTransferResult preview =
                inventoryComponent.Inventory.CanTransfer(
                    productId,
                    heldLocationId,
                    destination.ShelfLocationId,
                    1);
            if (!preview.IsSuccess)
            {
                item.SetPlacementPreview(false);
                error = $"Stocking transfer rejected ({preview.Failure}).";
                return false;
            }

            if (!destination.ShelfFixture.TryPlaceAt(
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
                    destination.ShelfLocationId,
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
                    destination.ShelfLocationId,
                    out error))
            {
                InventoryTransferResult rollback =
                    inventoryComponent.Inventory.TryTransfer(
                        productId,
                        destination.ShelfLocationId,
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
            bool resolved = TryResolveDestination(
                productId,
                out ResolvedStockingDestination destination);
            shelfLocationId = resolved ? destination.ShelfLocationId : null;
            return resolved;
        }

        public bool TryGetShelfFixture(
            string productId,
            out ShelfFixture shelfFixture)
        {
            bool resolved = TryResolveDestination(
                productId,
                out ResolvedStockingDestination destination);
            shelfFixture = resolved ? destination.ShelfFixture : null;
            return resolved;
        }

        internal bool TryPlaceInitialUnit(
            ProductItem item,
            string shelfLocationId,
            out string error)
        {
            bool resolved = TryResolveDestination(
                item?.Definition?.StableProductId,
                out ResolvedStockingDestination destination);
            PlacementFailure failure = PlacementFailure.None;
            if (!resolved ||
                !string.Equals(
                    destination.ShelfLocationId,
                    shelfLocationId,
                    StringComparison.Ordinal) ||
                !TryFindAvailableSnapPoint(destination, out string snapPointId) ||
                !destination.ShelfFixture.TryPlaceAt(
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
            StockingProductConfiguration product = FindProduct(snapshot?.productId);
            StockingProductConfiguration shelf =
                FindShelfConfiguration(snapshot?.shelfFixtureId);
            if (product == null || shelf == null || snapshot == null ||
                !string.Equals(
                    shelf.ShelfLocationId,
                    snapshot.inventoryLocationId,
                    StringComparison.Ordinal) ||
                !ContainsSnapPoint(shelf.SnapPointIds, snapshot.shelfSnapPointId) ||
                !shelf.ShelfFixture.TryGetSnapPoint(
                    snapshot.shelfSnapPointId,
                    out ShelfSnapPointDefinition snapPoint) ||
                !snapPoint.Accepts(
                    product.ProductDefinition.SnapCompatibilityTag))
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

            StockingProductConfiguration shelf =
                FindShelfConfiguration(snapshot.shelfFixtureId);
            if (!shelf.ShelfFixture.TryPlaceAt(
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

        public IReadOnlyList<StockingProductConfiguration> AuthoredProductMappings =>
            products ?? Array.Empty<StockingProductConfiguration>();

        public bool TryGetAuthoredProduct(
            string productId,
            out ProductDefinition productDefinition)
        {
            StockingProductConfiguration configuration = FindProduct(productId);
            productDefinition = configuration?.ProductDefinition;
            return productDefinition != null;
        }

        public bool TryGetAuthoredShelf(
            string shelfFixtureId,
            out ShelfFixture shelfFixture,
            out string shelfLocationId,
            out IReadOnlyList<string> snapPointIds)
        {
            StockingProductConfiguration configuration =
                FindShelfConfiguration(shelfFixtureId);
            shelfFixture = configuration?.ShelfFixture;
            shelfLocationId = configuration?.ShelfLocationId;
            snapPointIds = configuration?.SnapPointIds ?? Array.Empty<string>();
            return configuration != null;
        }

        public int GetShelfInventoryQuantity(string shelfFixtureId)
        {
            if (inventoryComponent == null || !inventoryComponent.IsInitialized ||
                !TryGetAuthoredShelf(
                    shelfFixtureId,
                    out _,
                    out string locationId,
                    out _))
            {
                return 0;
            }

            InventoryLocationSnapshot location = inventoryComponent.Inventory
                .CreateSnapshot().locations.Find(value => string.Equals(
                    value.locationId,
                    locationId,
                    StringComparison.Ordinal));
            return location?.quantities?.Sum(value => value.quantityUnits) ?? 0;
        }

        public bool IsProductCompatibleWithShelf(
            string productId,
            string shelfFixtureId,
            out string error)
        {
            if (!TryGetAuthoredProduct(productId, out ProductDefinition product) ||
                !TryGetAuthoredShelf(
                    shelfFixtureId,
                    out ShelfFixture shelf,
                    out _,
                    out IReadOnlyList<string> snapPointIds))
            {
                error = "The product or shelf is not in the stocking catalog.";
                return false;
            }

            foreach (string snapPointId in snapPointIds)
            {
                if (!shelf.TryGetSnapPoint(
                        snapPointId,
                        out ShelfSnapPointDefinition snapPoint) ||
                    !snapPoint.Accepts(product.SnapCompatibilityTag))
                {
                    error =
                        $"{product.DisplayName} is not physically compatible with every position on this shelf.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private bool TryResolveDestination(
            string productId,
            out ResolvedStockingDestination destination)
        {
            destination = default;
            StockingProductConfiguration product = FindProduct(productId);
            if (product == null || merchandising == null ||
                !merchandising.TryGetOfferForProduct(
                    productId,
                    out MerchandiseOffer offer))
            {
                return false;
            }

            StockingProductConfiguration shelf =
                FindShelfConfiguration(offer.ShelfFixtureId);
            if (shelf == null ||
                !string.Equals(
                    shelf.ShelfLocationId,
                    offer.InventoryLocationId,
                    StringComparison.Ordinal) ||
                !IsProductCompatibleWithShelf(
                    productId,
                    offer.ShelfFixtureId,
                    out _))
            {
                return false;
            }

            destination = new ResolvedStockingDestination(
                product.ProductDefinition,
                shelf.ShelfFixture,
                shelf.ShelfLocationId,
                shelf.SnapPointIds);
            return true;
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

        private StockingProductConfiguration FindShelfConfiguration(
            string shelfFixtureId)
        {
            if (!FirstStoreIdentifier.IsValid(shelfFixtureId) || products == null)
            {
                return null;
            }

            foreach (StockingProductConfiguration configuration in products)
            {
                if (configuration?.ShelfFixture != null && string.Equals(
                        configuration.ShelfFixture.StableFixtureId,
                        shelfFixtureId,
                        StringComparison.Ordinal))
                {
                    return configuration;
                }
            }
            return null;
        }

        private static bool TryFindAvailableSnapPoint(
            ResolvedStockingDestination destination,
            out string snapPointId)
        {
            foreach (string candidate in destination.SnapPointIds)
            {
                if (!destination.ShelfFixture.IsOccupied(candidate))
                {
                    snapPointId = candidate;
                    return true;
                }
            }

            snapPointId = null;
            return false;
        }

        private static bool ContainsSnapPoint(
            ResolvedStockingDestination destination,
            string snapPointId)
        {
            return ContainsSnapPoint(destination.SnapPointIds, snapPointId);
        }

        private static bool ContainsSnapPoint(
            IReadOnlyList<string> snapPointIds,
            string snapPointId)
        {
            foreach (string candidate in snapPointIds)
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
