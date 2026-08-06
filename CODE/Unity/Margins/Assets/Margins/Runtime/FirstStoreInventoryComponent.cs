// Draft implementation — Unity verification pending
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Margins
{
    [Serializable]
    public sealed class InventoryLocationConfiguration
    {
        [SerializeField] private string locationId;
        [SerializeField] private InventoryLocationKind kind;
        [SerializeField] private int capacityUnits = 1;
        [SerializeField] private bool singleProductOnly;

        public string LocationId => locationId;
        public InventoryLocationKind Kind => kind;
        public int CapacityUnits => capacityUnits;
        public bool SingleProductOnly => singleProductOnly;
    }

    [Serializable]
    public sealed class StartingInventoryConfiguration
    {
        [SerializeField] private ProductDefinition productDefinition;
        [SerializeField] private string locationId;
        [SerializeField, Min(1)] private int quantityUnits = 1;

        public ProductDefinition ProductDefinition => productDefinition;
        public string LocationId => locationId;
        public int QuantityUnits => quantityUnits;
    }

    public sealed class FirstStoreInventoryComponent : MonoBehaviour
    {
        [SerializeField] private ProductDefinition[] productDefinitions;
        [SerializeField] private InventoryLocationConfiguration[] locations;
        [SerializeField] private StartingInventoryConfiguration[] startingQuantities;

        public FirstStoreInventory Inventory { get; private set; }
        public bool IsInitialized => Inventory != null;

        private void Start()
        {
            if (!TryInitialize(out string error))
            {
                Debug.LogError($"First-store inventory initialization failed: {error}", this);
            }
        }

        public bool TryValidateConfiguration(out string error)
        {
            if (productDefinitions == null || productDefinitions.Length == 0)
            {
                error = "At least one product definition is required.";
                return false;
            }

            HashSet<string> productIds = new(StringComparer.Ordinal);
            foreach (ProductDefinition definition in productDefinitions)
            {
                if (definition == null ||
                    !FirstStoreIdentifier.IsValid(definition.StableProductId))
                {
                    error = "Every first-store product requires a valid stable identifier.";
                    return false;
                }

                if (!productIds.Add(definition.StableProductId))
                {
                    error = $"Duplicate first-store product id '{definition.StableProductId}'.";
                    return false;
                }
            }

            if (locations == null || locations.Length == 0)
            {
                error = "At least one inventory location is required.";
                return false;
            }

            HashSet<string> locationIds = new(StringComparer.Ordinal);
            foreach (InventoryLocationConfiguration location in locations)
            {
                if (location == null ||
                    !FirstStoreIdentifier.IsValid(location.LocationId) ||
                    location.CapacityUnits < -1)
                {
                    error = "Every inventory location requires a valid id and capacity.";
                    return false;
                }

                if (!locationIds.Add(location.LocationId))
                {
                    error = $"Duplicate inventory location id '{location.LocationId}'.";
                    return false;
                }
            }

            if (startingQuantities == null)
            {
                error = "Starting inventory configuration array is missing.";
                return false;
            }

            HashSet<string> seededPairs = new(StringComparer.Ordinal);
            foreach (StartingInventoryConfiguration startingQuantity in startingQuantities)
            {
                if (startingQuantity == null ||
                    startingQuantity.ProductDefinition == null ||
                    !productIds.Contains(startingQuantity.ProductDefinition.StableProductId) ||
                    !locationIds.Contains(startingQuantity.LocationId) ||
                    startingQuantity.QuantityUnits <= 0)
                {
                    error = "A starting inventory entry has an invalid product, location, or quantity.";
                    return false;
                }

                string pair =
                    $"{startingQuantity.LocationId}\n{startingQuantity.ProductDefinition.StableProductId}";
                if (!seededPairs.Add(pair))
                {
                    error =
                        $"Duplicate starting quantity for '{startingQuantity.ProductDefinition.StableProductId}' at '{startingQuantity.LocationId}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public bool TryInitialize(out string error)
        {
            if (Inventory != null)
            {
                error = null;
                return true;
            }

            if (!TryValidateConfiguration(out error))
            {
                return false;
            }

            FirstStoreInventory candidate = new();
            foreach (ProductDefinition definition in productDefinitions)
            {
                if (!candidate.TryRegisterProduct(definition.StableProductId, out error))
                {
                    return false;
                }
            }

            foreach (InventoryLocationConfiguration location in locations)
            {
                if (!candidate.TryRegisterLocation(
                        location.LocationId,
                        location.Kind,
                        location.CapacityUnits,
                        location.SingleProductOnly,
                        out error))
                {
                    return false;
                }
            }

            foreach (StartingInventoryConfiguration startingQuantity in startingQuantities)
            {
                if (!candidate.TrySeedQuantity(
                        startingQuantity.LocationId,
                        startingQuantity.ProductDefinition.StableProductId,
                        startingQuantity.QuantityUnits,
                        out error))
                {
                    return false;
                }
            }

            Inventory = candidate;
            error = null;
            return true;
        }

        public bool CanApplyRestoredInventory(
            FirstStoreInventory restored,
            out string error)
        {
            error = null;
            if (restored == null || !TryValidateConfiguration(out error))
            {
                error ??= "Restored inventory is missing.";
                return false;
            }

            FirstStoreInventorySnapshot snapshot = restored.CreateSnapshot();
            if (snapshot.productIds.Count != productDefinitions.Length ||
                snapshot.locations.Count != locations.Length)
            {
                error = "Restored inventory does not match configured products or locations.";
                return false;
            }

            HashSet<string> configuredProducts = new(StringComparer.Ordinal);
            foreach (ProductDefinition definition in productDefinitions)
            {
                configuredProducts.Add(definition.StableProductId);
            }

            foreach (string productId in snapshot.productIds)
            {
                if (!configuredProducts.Contains(productId))
                {
                    error = $"Restored inventory contains unconfigured product '{productId}'.";
                    return false;
                }
            }

            Dictionary<string, InventoryLocationConfiguration> configuredLocations =
                new(StringComparer.Ordinal);
            foreach (InventoryLocationConfiguration location in locations)
            {
                configuredLocations.Add(location.LocationId, location);
            }

            foreach (InventoryLocationSnapshot location in snapshot.locations)
            {
                if (!configuredLocations.TryGetValue(
                        location.locationId,
                        out InventoryLocationConfiguration configured) ||
                    location.kind != configured.Kind ||
                    location.capacityUnits != configured.CapacityUnits ||
                    location.singleProductOnly != configured.SingleProductOnly)
                {
                    error =
                        $"Restored inventory location '{location.locationId}' does not match inspector configuration.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public bool TryApplyRestoredInventory(
            FirstStoreInventory restored,
            out string error)
        {
            if (!CanApplyRestoredInventory(restored, out error))
            {
                return false;
            }

            Inventory = restored;
            return true;
        }
    }
}
