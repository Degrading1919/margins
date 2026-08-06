using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            StoreCustomerFlowController customerFlow =
                Object.FindAnyObjectByType<StoreCustomerFlowController>();
            CustomerCheckoutWorldInteractionTarget customerCheckout =
                Object.FindAnyObjectByType<CustomerCheckoutWorldInteractionTarget>();
            InStoreEmployeeWorkController employeeWork =
                Object.FindAnyObjectByType<InStoreEmployeeWorkController>();

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
            Assert.That(customerFlow, Is.Not.Null);
            Assert.That(
                customerFlow.TryValidateConfiguration(out error),
                Is.True,
                error);
            Assert.That(store.CustomerFlow, Is.SameAs(customerFlow));
            Assert.That(customerCheckout, Is.Not.Null);
            Assert.That(customerCheckout.enabled, Is.True);
            Assert.That(employeeWork, Is.Not.Null);
            Assert.That(employeeWork.CustomerFlow, Is.SameAs(customerFlow));
            Assert.That(
                employeeWork.TryValidateConfiguration(out error),
                Is.True,
                error);
            Assert.That(
                Object.FindAnyObjectByType<StagedCheckoutWorldInteractionTarget>()
                    .enabled,
                Is.False);
            Assert.That(
                Object.FindAnyObjectByType<StagedCheckoutInteractionComponent>()
                    .enabled,
                Is.False);
        }

        [UnityTest]
        public IEnumerator ValidationSceneCustomersQueueRoundTripAndCompleteExactItemSale()
        {
            yield return SceneManager.LoadSceneAsync(
                "FirstStoreValidation",
                LoadSceneMode.Single);
            yield return null;

            StoreCustomerFlowController flow =
                Object.FindAnyObjectByType<StoreCustomerFlowController>();
            CheckoutStationComponent checkout =
                Object.FindAnyObjectByType<CheckoutStationComponent>();
            DeliveryBoxComponent delivery =
                Object.FindAnyObjectByType<DeliveryBoxComponent>();
            StockingController stocking =
                Object.FindAnyObjectByType<StockingController>();
            PhysicalProductUnitRegistry physicalUnits =
                Object.FindAnyObjectByType<PhysicalProductUnitRegistry>();
            FirstStorePersistenceMapperComponent mapper =
                Object.FindAnyObjectByType<FirstStorePersistenceMapperComponent>();
            StoreOperatingController store =
                Object.FindAnyObjectByType<StoreOperatingController>();

            Assert.That(flow, Is.Not.Null);
            Assert.That(checkout, Is.Not.Null);
            Assert.That(delivery, Is.Not.Null);
            Assert.That(stocking, Is.Not.Null);
            Assert.That(physicalUnits, Is.Not.Null);
            Assert.That(mapper, Is.Not.Null);
            Assert.That(store, Is.Not.Null);
            SetField(flow, "secondsUntilNextArrival", 1_000f);
            SetField(flow, "arrivalIntervalSeconds", 1_000f);
            SetField(flow, "queuePatienceSeconds", 60f);

            Assert.That(delivery.TryOpen(out _, out string error), Is.True, error);
            foreach (string productId in checkout.ConfiguredProductIds)
            {
                Assert.That(
                    checkout.TryGetProductDefinition(
                        productId,
                        out ProductDefinition product),
                    Is.True);
                for (int unitIndex = 0; unitIndex < 2; unitIndex++)
                {
                    Assert.That(
                        delivery.TryRemoveOneUnit(
                            product,
                            out ProductItem loose,
                            out _,
                            out _,
                            out error),
                        Is.True,
                        error);
                    Assert.That(
                        stocking.TryPickUpLooseUnit(
                            loose,
                            out _,
                            out error),
                        Is.True,
                        error);
                    Assert.That(
                        stocking.TryStockHeldUnit(0, out error),
                        Is.True,
                        error);
                }
            }

            Assert.That(physicalUnits.VisibleUnitCount, Is.EqualTo(4));
            Assert.That(TotalShelfQuantity(checkout), Is.EqualTo(4));
            for (int index = 0; index < 3; index++)
            {
                Assert.That(
                    flow.TryAdmitCustomerNow(out _, out error),
                    Is.True,
                    error);
            }

            float deadline = Time.realtimeSinceStartup + 14f;
            while ((!flow.CanStartCheckout || flow.QueuedCustomerCount < 3) &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(flow.ActiveCustomerCount, Is.EqualTo(3));
            Assert.That(flow.QueuedCustomerCount, Is.EqualTo(3));
            Assert.That(flow.CanStartCheckout, Is.True);
            Assert.That(TotalShelfQuantity(checkout), Is.EqualTo(4));
            Assert.That(physicalUnits.VisibleUnitCount, Is.EqualTo(4));

            Assert.That(
                mapper.TryCapture(
                    out FirstStoreSnapshot queuedSnapshot,
                    out error),
                Is.True,
                error);
            Assert.That(queuedSnapshot.customerFlow, Is.Not.Null);
            Assert.That(queuedSnapshot.customerFlow.customers.Count, Is.EqualTo(3));
            string expectedFrontCustomerId =
                queuedSnapshot.customerFlow.customers[0].customerId;
            Assert.That(
                mapper.TryRestore(queuedSnapshot, out error),
                Is.True,
                error);
            yield return null;

            Assert.That(flow.ActiveCustomerCount, Is.EqualTo(3));
            Assert.That(flow.QueuedCustomerCount, Is.EqualTo(3));
            Assert.That(flow.CanStartCheckout, Is.True);
            Assert.That(TotalShelfQuantity(checkout), Is.EqualTo(4));
            Assert.That(checkout.CompletedTransactionCount, Is.Zero);

            Assert.That(flow.TryStartCheckout(out error), Is.True, error);
            IReadOnlyList<string> activeItemIds =
                flow.ActiveCheckoutPhysicalUnitIds;
            string[] actualItemIds = new string[activeItemIds.Count];
            for (int index = 0; index < activeItemIds.Count; index++)
            {
                actualItemIds[index] = activeItemIds[index];
            }
            Assert.That(actualItemIds.Length, Is.GreaterThan(0));
            foreach (string physicalUnitId in actualItemIds)
            {
                Assert.That(
                    flow.TryScanCustomerItem(physicalUnitId, out error),
                    Is.True,
                    error);
            }

            int unitsBeforePayment = TotalShelfQuantity(checkout);
            Assert.That(flow.TryCompleteCheckout(out error), Is.True, error);
            yield return null;
            Assert.That(checkout.CompletedTransactionCount, Is.EqualTo(1));
            Assert.That(
                checkout.CompletedTransactions.Any(transaction =>
                    string.Equals(
                        transaction.transactionId,
                        $"sale-{expectedFrontCustomerId}",
                        System.StringComparison.Ordinal)),
                Is.True,
                "Queue order must survive snapshot reconstruction.");
            Assert.That(
                TotalShelfQuantity(checkout),
                Is.EqualTo(unitsBeforePayment - actualItemIds.Length));
            Assert.That(
                physicalUnits.VisibleUnitCount,
                Is.EqualTo(4 - actualItemIds.Length));

            Assert.That(flow.TryCompleteCheckout(out _), Is.False);
            Assert.That(checkout.CompletedTransactionCount, Is.EqualTo(1));

            Assert.That(store.TryBeginClosing(out error), Is.True, error);
            Assert.That(
                store.TryGetFirstFinalCloseBlocker(out string closeBlocker),
                Is.True);
            StringAssert.Contains("customers", closeBlocker);
        }

        [UnityTest]
        public IEnumerator QueuedCustomerAbandonmentReturnsReservedShelfStock()
        {
            yield return SceneManager.LoadSceneAsync(
                "FirstStoreValidation",
                LoadSceneMode.Single);
            yield return null;

            StoreCustomerFlowController flow =
                Object.FindAnyObjectByType<StoreCustomerFlowController>();
            CheckoutStationComponent checkout =
                Object.FindAnyObjectByType<CheckoutStationComponent>();
            DeliveryBoxComponent delivery =
                Object.FindAnyObjectByType<DeliveryBoxComponent>();
            StockingController stocking =
                Object.FindAnyObjectByType<StockingController>();
            PhysicalProductUnitRegistry physicalUnits =
                Object.FindAnyObjectByType<PhysicalProductUnitRegistry>();
            FirstStorePersistenceMapperComponent mapper =
                Object.FindAnyObjectByType<FirstStorePersistenceMapperComponent>();

            SetField(flow, "secondsUntilNextArrival", 1_000f);
            SetField(flow, "arrivalIntervalSeconds", 1_000f);
            SetField(flow, "queuePatienceSeconds", 0.35f);
            Assert.That(delivery.TryOpen(out _, out string error), Is.True, error);
            string productId = checkout.ConfiguredProductIds[0];
            Assert.That(
                checkout.TryGetProductDefinition(
                    productId,
                    out ProductDefinition product),
                Is.True);
            Assert.That(
                delivery.TryRemoveOneUnit(
                    product,
                    out ProductItem loose,
                    out _,
                    out _,
                    out error),
                Is.True,
                error);
            Assert.That(
                stocking.TryPickUpLooseUnit(loose, out _, out error),
                Is.True,
                error);
            Assert.That(stocking.TryStockHeldUnit(0, out error), Is.True, error);
            Assert.That(flow.TryAdmitCustomerNow(out _, out error), Is.True, error);

            float queueDeadline = Time.realtimeSinceStartup + 12f;
            while (flow.QueuedCustomerCount == 0 &&
                   Time.realtimeSinceStartup < queueDeadline)
            {
                yield return null;
            }
            Assert.That(flow.QueuedCustomerCount, Is.EqualTo(1));
            Assert.That(
                mapper.TryCapture(
                    out FirstStoreSnapshot queued,
                    out error),
                Is.True,
                error);
            string reservedUnitId =
                queued.customerFlow.customers[0].reservedPhysicalUnitIds[0];
            Assert.That(
                physicalUnits.TryGetUnit(
                    reservedUnitId,
                    out ProductItem reserved,
                    out _),
                Is.True);
            Assert.That(reserved.IsReservedByCustomer, Is.True);

            float abandonmentDeadline = Time.realtimeSinceStartup + 2f;
            while (flow.QueuedCustomerCount > 0 &&
                   Time.realtimeSinceStartup < abandonmentDeadline)
            {
                yield return null;
            }

            Assert.That(flow.QueuedCustomerCount, Is.Zero);
            Assert.That(checkout.CompletedTransactionCount, Is.Zero);
            Assert.That(
                physicalUnits.TryGetUnit(
                    reservedUnitId,
                    out ProductItem returned,
                    out string locationId),
                Is.True);
            Assert.That(returned.IsReservedByCustomer, Is.False);
            Assert.That(returned.IsSnapped, Is.True);
            Assert.That(
                locationId,
                Is.EqualTo(
                    checkout.TryGetShelfLocation(productId, out string shelfLocation)
                        ? shelfLocation
                        : null));
            Assert.That(TotalShelfQuantity(checkout), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator CustomerWithNoAvailableProductLeavesWithoutSale()
        {
            yield return SceneManager.LoadSceneAsync(
                "FirstStoreValidation",
                LoadSceneMode.Single);
            yield return null;

            StoreCustomerFlowController flow =
                Object.FindAnyObjectByType<StoreCustomerFlowController>();
            CheckoutStationComponent checkout =
                Object.FindAnyObjectByType<CheckoutStationComponent>();
            PhysicalProductUnitRegistry physicalUnits =
                Object.FindAnyObjectByType<PhysicalProductUnitRegistry>();
            SetField(flow, "secondsUntilNextArrival", 1_000f);
            SetField(flow, "arrivalIntervalSeconds", 1_000f);

            Assert.That(
                flow.TryAdmitCustomerNow(out _, out string error),
                Is.True,
                error);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (flow.LeavingWithoutPurchaseCount == 0 &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(flow.LeavingWithoutPurchaseCount, Is.EqualTo(1));
            Assert.That(flow.QueuedCustomerCount, Is.Zero);
            Assert.That(checkout.CompletedTransactionCount, Is.Zero);
            Assert.That(checkout.GrossSalesCents, Is.Zero);
            Assert.That(physicalUnits.VisibleUnitCount, Is.Zero);
            Assert.That(TotalShelfQuantity(checkout), Is.Zero);
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

        private static int TotalShelfQuantity(CheckoutStationComponent checkout)
        {
            int total = 0;
            foreach (string productId in checkout.ConfiguredProductIds)
            {
                Assert.That(
                    checkout.TryGetShelfLocation(productId, out string shelfLocation),
                    Is.True);
                total += checkout.InventoryComponent.Inventory.GetQuantity(
                    shelfLocation,
                    productId);
            }
            return total;
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
