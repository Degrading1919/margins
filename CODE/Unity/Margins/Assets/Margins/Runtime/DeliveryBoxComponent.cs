using System;
using System.Collections.Generic;
using UnityEngine;

namespace Margins
{
    public sealed class DeliveryBoxComponent : MonoBehaviour
    {
        [SerializeField] private string stableContainerId;
        [SerializeField] private string inventoryLocationId;
        [SerializeField] private string looseDestinationLocationId;
        [SerializeField] private ProductDefinition[] productDefinitions;
        [SerializeField] private FirstStoreInventoryComponent inventoryComponent;
        [SerializeField] private PhysicalProductUnitRegistry physicalUnits;
        [SerializeField] private bool startsOpen;

        internal DeliveryContainer Container { get; private set; }
        public string StableContainerId => stableContainerId;
        public FirstStoreInventoryComponent InventoryComponent => inventoryComponent;
        public PhysicalProductUnitRegistry PhysicalUnits => physicalUnits;
        public bool IsInitialized => Container != null;
        public bool IsOpen => Container != null && Container.IsOpen;
        public bool IsSealed => Container != null && !Container.IsOpen;

        private void Start()
        {
            if (!TryInitialize(out string error))
            {
                Debug.LogError($"Delivery box initialization failed: {error}", this);
            }
        }

        public bool TryValidateConfiguration(out string error)
        {
            error = null;
            if (!FirstStoreIdentifier.IsValid(stableContainerId) ||
                !FirstStoreIdentifier.IsValid(inventoryLocationId) ||
                !FirstStoreIdentifier.IsValid(looseDestinationLocationId))
            {
                error = $"Delivery box '{name}' has an invalid stable or location id.";
                return false;
            }

            if (inventoryComponent == null || !inventoryComponent.IsInitialized ||
                physicalUnits == null ||
                !physicalUnits.TryValidateConfiguration(out error))
            {
                error ??=
                    $"Delivery box '{stableContainerId}' requires initialized inventory and physical-unit references.";
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

            if (productDefinitions == null || productDefinitions.Length == 0)
            {
                error = $"Delivery box '{stableContainerId}' requires configured products.";
                return false;
            }

            HashSet<string> productIds = new(StringComparer.Ordinal);
            foreach (ProductDefinition productDefinition in productDefinitions)
            {
                if (productDefinition == null ||
                    !inventoryComponent.Inventory.IsKnownProduct(
                        productDefinition.StableProductId) ||
                    !productIds.Add(productDefinition.StableProductId) ||
                    !physicalUnits.CanMaterialize(productDefinition, out error))
                {
                    error ??=
                        $"Delivery box '{stableContainerId}' contains an invalid, duplicate, or non-physical product.";
                    return false;
                }
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
            ProductDefinition requestedProduct,
            out ProductItem physicalUnit,
            out DeliveryContainerFailure failure,
            out InventoryTransferResult transfer,
            out string error)
        {
            physicalUnit = null;
            transfer = null;
            error = null;
            if (Container == null ||
                requestedProduct == null ||
                FindConfiguredProduct(requestedProduct.StableProductId) == null ||
                !physicalUnits.CanMaterialize(requestedProduct, out error))
            {
                failure = DeliveryContainerFailure.InvalidConfiguration;
                error ??= "Delivery product request is invalid or unconfigured.";
                return false;
            }

            if (!Container.TryRemoveTo(
                    requestedProduct.StableProductId,
                    looseDestinationLocationId,
                    1,
                    out failure,
                    out transfer))
            {
                error = $"Delivery removal rejected ({failure}).";
                return false;
            }

            if (physicalUnits.TryMaterializeLooseUnit(
                    requestedProduct,
                    looseDestinationLocationId,
                    out physicalUnit,
                    out error))
            {
                return true;
            }

            InventoryTransferResult rollback =
                inventoryComponent.Inventory.TryTransfer(
                    requestedProduct.StableProductId,
                    looseDestinationLocationId,
                    inventoryLocationId,
                    1);
            if (!rollback.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Physical-unit creation failed and delivery rollback was rejected ({rollback.Failure}).");
            }

            failure = DeliveryContainerFailure.TransferRejected;
            transfer = rollback;
            physicalUnit = null;
            return false;
        }

        public bool TryGetConfiguredProductRemaining(
            ProductDefinition requestedProduct,
            out string productName,
            out int remainingUnits,
            out string error)
        {
            productName = null;
            remainingUnits = 0;
            if (Container == null || requestedProduct == null ||
                FindConfiguredProduct(requestedProduct.StableProductId) == null ||
                inventoryComponent == null || !inventoryComponent.IsInitialized)
            {
                error = "Delivery product request is invalid or unconfigured.";
                return false;
            }

            productName = string.IsNullOrWhiteSpace(requestedProduct.DisplayName)
                ? requestedProduct.StableProductId
                : requestedProduct.DisplayName;
            remainingUnits = inventoryComponent.Inventory.GetQuantity(
                inventoryLocationId,
                requestedProduct.StableProductId);
            error = null;
            return true;
        }

        public bool CanApplyRestoredContainer(
            DeliveryContainer restored,
            out string error)
        {
            if (restored == null ||
                !string.Equals(
                    restored.ContainerId,
                    stableContainerId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    restored.InventoryLocationId,
                    inventoryLocationId,
                    StringComparison.Ordinal))
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

        private ProductDefinition FindConfiguredProduct(string productId)
        {
            if (!FirstStoreIdentifier.IsValid(productId) || productDefinitions == null)
            {
                return null;
            }

            foreach (ProductDefinition productDefinition in productDefinitions)
            {
                if (productDefinition != null &&
                    string.Equals(
                        productDefinition.StableProductId,
                        productId,
                        StringComparison.Ordinal))
                {
                    return productDefinition;
                }
            }
            return null;
        }
    }
}
