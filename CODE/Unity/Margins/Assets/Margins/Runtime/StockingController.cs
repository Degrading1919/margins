// Draft implementation — Unity verification pending
using UnityEngine;

namespace Margins
{
    public sealed class StockingController : MonoBehaviour
    {
        [SerializeField] private FirstStoreInventoryComponent inventoryComponent;
        [SerializeField] private ProductItem productItem;
        [SerializeField] private ShelfFixture shelfFixture;
        [SerializeField] private Transform holdPoint;
        [SerializeField] private string looseLocationId;
        [SerializeField] private string heldLocationId;
        [SerializeField] private string shelfLocationId;
        [SerializeField] private string snapPointId;

        public FirstStoreInventoryComponent InventoryComponent => inventoryComponent;
        public bool HasHeldUnit =>
            inventoryComponent != null &&
            inventoryComponent.IsInitialized &&
            productItem != null &&
            productItem.Definition != null &&
            inventoryComponent.Inventory.GetQuantity(
                heldLocationId,
                productItem.Definition.StableProductId) > 0;

        public bool TryValidateConfiguration(out string error)
        {
            if (inventoryComponent == null || !inventoryComponent.IsInitialized)
            {
                error = "Stocking requires an initialized inventory component.";
                return false;
            }

            if (productItem == null ||
                productItem.Definition == null ||
                shelfFixture == null ||
                holdPoint == null)
            {
                error =
                    "Stocking requires explicit product, shelf, and hold-point references.";
                return false;
            }

            if (!FirstStoreIdentifier.IsValid(looseLocationId) ||
                !FirstStoreIdentifier.IsValid(heldLocationId) ||
                !FirstStoreIdentifier.IsValid(shelfLocationId) ||
                !FirstStoreIdentifier.IsValid(snapPointId))
            {
                error = "Stocking location and snap-point identifiers are invalid.";
                return false;
            }

            if (!inventoryComponent.Inventory.IsKnownProduct(
                    productItem.Definition.StableProductId))
            {
                error =
                    $"Stocking product '{productItem.Definition.StableProductId}' is not registered.";
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

            if (!inventoryComponent.Inventory.TryGetLocationKind(
                    shelfLocationId,
                    out InventoryLocationKind shelfKind) ||
                shelfKind != InventoryLocationKind.Shelf)
            {
                error = $"Stocking destination '{shelfLocationId}' is not a shelf location.";
                return false;
            }

            if (!shelfFixture.TryGetSnapPoint(snapPointId, out _))
            {
                error =
                    $"Stocking snap point '{snapPointId}' is missing from fixture '{shelfFixture.StableFixtureId}'.";
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

            return TryValidateConfiguration(out error);
        }

        public bool TryPickUpLooseUnit(out string error)
        {
            if (!TryInitializeDependencies(out error))
            {
                return false;
            }

            if (productItem.IsHeld || productItem.IsSnapped)
            {
                error = "The configured physical product is not loose.";
                return false;
            }

            string productId = productItem.Definition.StableProductId;
            InventoryTransferResult transfer =
                inventoryComponent.Inventory.TryTransfer(
                    productId,
                    looseLocationId,
                    heldLocationId,
                    1);
            if (!transfer.IsSuccess)
            {
                error = $"Product pickup transfer rejected ({transfer.Failure}).";
                return false;
            }

            productItem.PickUp(holdPoint);
            error = null;
            return true;
        }

        public bool TryStockHeldUnit(
            int quarterTurns,
            out string error)
        {
            if (!TryInitializeDependencies(out error))
            {
                return false;
            }

            if (!productItem.IsHeld)
            {
                error = "The configured physical product is not held.";
                return false;
            }

            string productId = productItem.Definition.StableProductId;
            InventoryTransferResult domainPreview =
                inventoryComponent.Inventory.CanTransfer(
                    productId,
                    heldLocationId,
                    shelfLocationId,
                    1);
            if (!domainPreview.IsSuccess)
            {
                productItem.SetPlacementPreview(false);
                error = $"Stocking transfer rejected ({domainPreview.Failure}).";
                return false;
            }

            if (!shelfFixture.TryPlaceAt(
                    productItem,
                    snapPointId,
                    quarterTurns,
                    out PlacementFailure placementFailure))
            {
                productItem.SetPlacementPreview(false);
                error = $"Physical shelf placement rejected ({placementFailure}).";
                return false;
            }

            InventoryTransferResult transfer =
                inventoryComponent.Inventory.TryTransfer(
                    productId,
                    heldLocationId,
                    shelfLocationId,
                    1);
            if (!transfer.IsSuccess)
            {
                productItem.PickUp(holdPoint);
                Debug.LogError(
                    $"Stocking rollback applied after domain transfer failed ({transfer.Failure}).",
                    this);
                error = $"Stocking transfer failed after placement ({transfer.Failure}).";
                return false;
            }

            error = null;
            return true;
        }
    }
}
