using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Margins.Tests
{
    [Category("FirstStoreAdapters")]
    public sealed class FirstStoreAdapterPlayModeTests
    {
        private readonly List<Object> createdObjects = new();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    Object.Destroy(createdObjects[index]);
                }
            }
            createdObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator MixedDeliveryMaterializesDistinctActiveUnitsAtRuntime()
        {
            ProductDefinition cola = CreateProduct("prod-cola", "Cola");
            ProductDefinition chips = CreateProduct("prod-chips", "Chips");
            FirstStoreInventoryComponent inventory = CreateInventory(cola, chips);
            PhysicalProductUnitRegistry physicalUnits =
                CreatePhysicalUnits(cola, chips);
            DeliveryBoxComponent delivery = CreateDelivery(
                inventory,
                physicalUnits,
                cola,
                chips);

            Assert.That(delivery.TryOpen(out _, out string error), Is.True, error);
            Assert.That(
                delivery.TryRemoveOneUnit(
                    cola,
                    out ProductItem colaUnit,
                    out _,
                    out _,
                    out error),
                Is.True,
                error);
            Assert.That(
                delivery.TryRemoveOneUnit(
                    chips,
                    out ProductItem chipsUnit,
                    out _,
                    out _,
                    out error),
                Is.True,
                error);
            yield return null;

            Assert.That(colaUnit, Is.Not.SameAs(chipsUnit));
            Assert.That(colaUnit.gameObject.activeInHierarchy, Is.True);
            Assert.That(chipsUnit.gameObject.activeInHierarchy, Is.True);
            Assert.That(colaUnit.PhysicalUnitId, Is.Not.EqualTo(chipsUnit.PhysicalUnitId));
            Assert.That(physicalUnits.VisibleUnitCount, Is.EqualTo(2));
            Assert.That(inventory.Inventory.GetQuantity("loc-loose", "prod-cola"), Is.EqualTo(1));
            Assert.That(inventory.Inventory.GetQuantity("loc-loose", "prod-chips"), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ValidationSceneLoadsWithExplicitInitializedReferences()
        {
            yield return SceneManager.LoadSceneAsync(
                "FirstStoreValidation",
                LoadSceneMode.Single);
            yield return null;

            FirstStoreValidationController validation =
                Object.FindAnyObjectByType<FirstStoreValidationController>();
            StoreOperatingController store =
                Object.FindAnyObjectByType<StoreOperatingController>();
            DeliveryBoxComponent delivery =
                Object.FindAnyObjectByType<DeliveryBoxComponent>();
            PhysicalProductUnitRegistry physicalUnits =
                Object.FindAnyObjectByType<PhysicalProductUnitRegistry>();
            FirstStoreInteractionController interaction =
                Object.FindAnyObjectByType<FirstStoreInteractionController>();
            FirstPersonController player =
                Object.FindAnyObjectByType<FirstPersonController>();

            Assert.That(validation, Is.Not.Null);
            Assert.That(store, Is.Not.Null);
            Assert.That(store.IsInitialized, Is.True);
            Assert.That(delivery, Is.Not.Null);
            Assert.That(delivery.IsInitialized, Is.True);
            Assert.That(physicalUnits, Is.Not.Null);
            Assert.That(physicalUnits.VisibleUnitCount, Is.Zero);
            Assert.That(interaction, Is.Not.Null);
            Assert.That(
                interaction.TryValidateConfiguration(out string error),
                Is.True,
                error);
            Assert.That(player, Is.Not.Null);
            Assert.That(player.IsGameplayMode, Is.True);
        }

        private ProductDefinition CreateProduct(string productId, string displayName)
        {
            ProductDefinition product =
                ScriptableObject.CreateInstance<ProductDefinition>();
            createdObjects.Add(product);
            SetField(product, "stableProductId", productId);
            SetField(product, "displayName", displayName);
            SetField(product, "shelfFootprint", ProductFootprint.Small);
            SetField(product, "snapCompatibilityTag", "shelf-small");
            return product;
        }

        private FirstStoreInventoryComponent CreateInventory(
            ProductDefinition cola,
            ProductDefinition chips)
        {
            FirstStoreInventoryComponent inventory =
                CreateGameObject("Inventory").AddComponent<FirstStoreInventoryComponent>();
            SetField(inventory, "productDefinitions", new[] { cola, chips });
            SetField(
                inventory,
                "locations",
                new[]
                {
                    CreateLocation(
                        "loc-box",
                        InventoryLocationKind.DeliveryContainer,
                        4,
                        false),
                    CreateLocation(
                        "loc-loose",
                        InventoryLocationKind.Loose,
                        4,
                        false)
                });
            SetField(
                inventory,
                "startingQuantities",
                new[]
                {
                    CreateStartingQuantity(cola, "loc-box", 1),
                    CreateStartingQuantity(chips, "loc-box", 1)
                });
            Assert.That(inventory.TryInitialize(out string error), Is.True, error);
            return inventory;
        }

        private PhysicalProductUnitRegistry CreatePhysicalUnits(
            params ProductDefinition[] products)
        {
            GameObject root = CreateGameObject("Physical Units");
            PhysicalProductUnitRegistry registry =
                root.AddComponent<PhysicalProductUnitRegistry>();
            PhysicalProductUnitConfiguration[] configurations =
                new PhysicalProductUnitConfiguration[products.Length];
            for (int index = 0; index < products.Length; index++)
            {
                ProductItem prefab =
                    CreateGameObject($"{products[index].StableProductId} Prefab")
                        .AddComponent<ProductItem>();
                SetField(prefab, "definition", products[index]);
                prefab.gameObject.SetActive(false);
                Transform spawn =
                    CreateGameObject($"{products[index].StableProductId} Spawn").transform;
                spawn.SetParent(root.transform, false);
                spawn.localPosition = new Vector3(0f, 0f, index);

                PhysicalProductUnitConfiguration configuration = new();
                SetField(configuration, "productDefinition", products[index]);
                SetField(configuration, "unitPrefab", prefab);
                SetField(configuration, "looseSpawnPoint", spawn);
                SetField(configuration, "looseUnitSpacing", new Vector3(0.2f, 0f, 0f));
                configurations[index] = configuration;
            }
            SetField(registry, "products", configurations);
            Assert.That(registry.TryValidateConfiguration(out string error), Is.True, error);
            return registry;
        }

        private DeliveryBoxComponent CreateDelivery(
            FirstStoreInventoryComponent inventory,
            PhysicalProductUnitRegistry physicalUnits,
            params ProductDefinition[] products)
        {
            DeliveryBoxComponent delivery =
                CreateGameObject("Delivery").AddComponent<DeliveryBoxComponent>();
            SetField(delivery, "stableContainerId", "container-starter");
            SetField(delivery, "inventoryLocationId", "loc-box");
            SetField(delivery, "looseDestinationLocationId", "loc-loose");
            SetField(delivery, "productDefinitions", products);
            SetField(delivery, "inventoryComponent", inventory);
            SetField(delivery, "physicalUnits", physicalUnits);
            SetField(delivery, "startsOpen", false);
            Assert.That(delivery.TryInitialize(out string error), Is.True, error);
            return delivery;
        }

        private static InventoryLocationConfiguration CreateLocation(
            string locationId,
            InventoryLocationKind kind,
            int capacity,
            bool singleProductOnly)
        {
            InventoryLocationConfiguration location = new();
            SetField(location, "locationId", locationId);
            SetField(location, "kind", kind);
            SetField(location, "capacityUnits", capacity);
            SetField(location, "singleProductOnly", singleProductOnly);
            return location;
        }

        private static StartingInventoryConfiguration CreateStartingQuantity(
            ProductDefinition product,
            string locationId,
            int quantity)
        {
            StartingInventoryConfiguration starting = new();
            SetField(starting, "productDefinition", product);
            SetField(starting, "locationId", locationId);
            SetField(starting, "quantityUnits", quantity);
            return starting;
        }

        private GameObject CreateGameObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
