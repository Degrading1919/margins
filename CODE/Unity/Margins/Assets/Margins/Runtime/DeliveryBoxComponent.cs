// Draft implementation — Unity verification pending
using UnityEngine;

namespace Margins
{
    public sealed class DeliveryBoxComponent : MonoBehaviour
    {
        [SerializeField] private string stableContainerId;
        [SerializeField] private string inventoryLocationId;
        [SerializeField] private string looseDestinationLocationId;
        [SerializeField] private ProductDefinition productDefinition;
        [SerializeField] private FirstStoreInventoryComponent inventoryComponent;
        [SerializeField] private bool startsOpen;

        public DeliveryContainer Container { get; private set; }
        public string StableContainerId => stableContainerId;
        public FirstStoreInventoryComponent InventoryComponent => inventoryComponent;
        public bool IsInitialized => Container != null;

        private void Start()
        {
            if (!TryInitialize(out string error))
            {
                Debug.LogError($"Delivery box initialization failed: {error}", this);
            }
        }

        public bool TryValidateConfiguration(out string error)
        {
            if (!FirstStoreIdentifier.IsValid(stableContainerId) ||
                !FirstStoreIdentifier.IsValid(inventoryLocationId) ||
                !FirstStoreIdentifier.IsValid(looseDestinationLocationId))
            {
                error = $"Delivery box '{name}' has an invalid stable or location id.";
                return false;
            }

            if (productDefinition == null ||
                !FirstStoreIdentifier.IsValid(productDefinition.StableProductId))
            {
                error = $"Delivery box '{stableContainerId}' requires a product definition.";
                return false;
            }

            if (inventoryComponent == null || !inventoryComponent.IsInitialized)
            {
                error =
                    $"Delivery box '{stableContainerId}' requires an initialized inventory component.";
                return false;
            }

            if (!inventoryComponent.Inventory.IsKnownProduct(
                    productDefinition.StableProductId))
            {
                error =
                    $"Delivery box product '{productDefinition.StableProductId}' is not registered.";
                return false;
            }

            if (!inventoryComponent.Inventory.TryGetLocationKind(
                    inventoryLocationId,
                    out InventoryLocationKind sourceKind) ||
                sourceKind != InventoryLocationKind.DeliveryContainer)
            {
                error =
                    $"Delivery box source '{inventoryLocationId}' is not a delivery-container location.";
                return false;
            }

            if (!inventoryComponent.Inventory.TryGetLocationKind(
                    looseDestinationLocationId,
                    out InventoryLocationKind destinationKind) ||
                destinationKind != InventoryLocationKind.Loose)
            {
                error =
                    $"Delivery box destination '{looseDestinationLocationId}' is not a loose location.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryInitialize(out string error)
        {
            if (Container != null)
            {
                error = null;
                return true;
            }

            if (inventoryComponent == null)
            {
                error =
                    $"Delivery box '{stableContainerId}' requires an inventory component.";
                return false;
            }

            if (!inventoryComponent.TryInitialize(out error))
            {
                error =
                    $"Delivery box '{stableContainerId}' could not initialize inventory: {error}";
                return false;
            }

            if (!TryValidateConfiguration(out error))
            {
                return false;
            }

            if (!DeliveryContainer.TryCreate(
                    inventoryComponent.Inventory,
                    stableContainerId,
                    inventoryLocationId,
                    startsOpen,
                    out DeliveryContainer container,
                    out error))
            {
                return false;
            }

            Container = container;
            return true;
        }

        public bool TryOpen(
            out DeliveryContainerOpenResult result,
            out string error)
        {
            if (Container == null)
            {
                result = default;
                error = "Delivery box is not initialized.";
                return false;
            }

            result = Container.TryOpen();
            error = null;
            return true;
        }

        public bool TryRemoveOneUnit(
            out DeliveryContainerFailure failure,
            out InventoryTransferResult transfer)
        {
            if (Container == null)
            {
                failure = DeliveryContainerFailure.InvalidConfiguration;
                transfer = null;
                return false;
            }

            return Container.TryRemoveTo(
                productDefinition.StableProductId,
                looseDestinationLocationId,
                1,
                out failure,
                out transfer);
        }

        public bool CanApplyRestoredContainer(
            DeliveryContainer restored,
            out string error)
        {
            if (restored == null ||
                !string.Equals(
                    restored.ContainerId,
                    stableContainerId,
                    System.StringComparison.Ordinal) ||
                !string.Equals(
                    restored.InventoryLocationId,
                    inventoryLocationId,
                    System.StringComparison.Ordinal))
            {
                error =
                    $"Restored delivery container does not match '{stableContainerId}'.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryApplyRestoredContainer(
            DeliveryContainer restored,
            out string error)
        {
            if (!CanApplyRestoredContainer(restored, out error))
            {
                return false;
            }

            Container = restored;
            return true;
        }
    }
}
