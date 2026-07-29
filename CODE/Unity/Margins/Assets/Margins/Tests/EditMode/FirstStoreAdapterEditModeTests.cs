using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Margins.Tests
{
    [Category("FirstStoreAdapters")]
    public sealed class FirstStoreAdapterEditModeTests
    {
        private readonly List<Object> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    Object.DestroyImmediate(createdObjects[index]);
                }
            }
            createdObjects.Clear();
        }

        [Test]
        public void MixedProductDeliveryCreatesRequestedDistinctUnitsAndRejectsExhaustion()
        {
            AdapterRig rig = CreateAdapterRig(colaBoxQuantity: 2, chipsBoxQuantity: 1);
            Assert.That(rig.Delivery.TryOpen(out _, out string error), Is.True, error);

            Assert.That(
                rig.Delivery.TryRemoveOneUnit(
                    rig.Chips,
                    out ProductItem chips,
                    out _,
                    out _,
                    out error),
                Is.True,
                error);
            Assert.That(
                rig.Delivery.TryRemoveOneUnit(
                    rig.Cola,
                    out ProductItem firstCola,
                    out _,
                    out _,
                    out error),
                Is.True,
                error);
            Assert.That(
                rig.Delivery.TryRemoveOneUnit(
                    rig.Cola,
                    out ProductItem secondCola,
                    out _,
                    out _,
                    out error),
                Is.True,
                error);

            Assert.That(chips, Is.Not.SameAs(firstCola));
            Assert.That(firstCola, Is.Not.SameAs(secondCola));
            Assert.That(firstCola.PhysicalUnitId, Is.Not.EqualTo(secondCola.PhysicalUnitId));
            Assert.That(rig.PhysicalUnits.VisibleUnitCount, Is.EqualTo(3));
            Assert.That(rig.Inventory.Inventory.GetQuantity("loc-loose", "prod-cola"), Is.EqualTo(2));
            Assert.That(rig.Inventory.Inventory.GetQuantity("loc-loose", "prod-chips"), Is.EqualTo(1));

            FirstStoreInventorySnapshot before = rig.Inventory.Inventory.CreateSnapshot();
            Assert.That(
                rig.Delivery.TryRemoveOneUnit(
                    rig.Chips,
                    out _,
                    out DeliveryContainerFailure failure,
                    out _,
                    out _),
                Is.False);
            Assert.That(failure, Is.EqualTo(DeliveryContainerFailure.TransferRejected));
            Assert.That(rig.Inventory.Inventory.CreateSnapshot(), Is.EqualTo(before));
            Assert.That(rig.PhysicalUnits.VisibleUnitCount, Is.EqualTo(3));
        }

        [Test]
        public void ProductSpecificShelvesFeedTwoCompletedTransactions()
        {
            AdapterRig rig = CreateAdapterRig(colaBoxQuantity: 1, chipsBoxQuantity: 1);
            ProductItem cola = StockOne(rig, rig.Cola);
            ProductItem chips = StockOne(rig, rig.Chips);

            Assert.That(cola.SnappedFixture, Is.EqualTo(rig.ColaShelf));
            Assert.That(chips.SnappedFixture, Is.EqualTo(rig.ChipsShelf));
            Assert.That(rig.Inventory.Inventory.GetQuantity("loc-shelf-cola", "prod-cola"), Is.EqualTo(1));
            Assert.That(rig.Inventory.Inventory.GetQuantity("loc-shelf-chips", "prod-chips"), Is.EqualTo(1));

            CompleteSale(rig, "transaction-002", rig.Cola, 1);
            CompleteSale(rig, "transaction-001", rig.Chips, 1);

            Assert.That(rig.Checkout.CompletedTransactionCount, Is.EqualTo(2));
            Assert.That(rig.Checkout.GrossSalesCents, Is.EqualTo(448));
            Assert.That(rig.Checkout.UnitsSold, Is.EqualTo(2));
            Assert.That(
                rig.Checkout.CompletedTransactions[0].transactionId,
                Is.EqualTo("transaction-001"));
            Assert.That(rig.Inventory.Inventory.GetQuantity("loc-shelf-cola", "prod-cola"), Is.Zero);
            Assert.That(rig.Inventory.Inventory.GetQuantity("loc-shelf-chips", "prod-chips"), Is.Zero);
            Assert.That(rig.PhysicalUnits.VisibleUnitCount, Is.Zero);
        }

        [Test]
        public void DuplicateTransactionIdCannotConsumeRemainingStockOrPhysicalUnit()
        {
            AdapterRig rig = CreateAdapterRig(colaBoxQuantity: 2, chipsBoxQuantity: 0);
            StockOne(rig, rig.Cola);
            StockOne(rig, rig.Cola);
            CompleteSale(rig, "transaction-duplicate", rig.Cola, 1);

            Assert.That(
                rig.Checkout.TryBeginSession("transaction-duplicate", out string error),
                Is.True,
                error);
            Assert.That(rig.Checkout.TryScan(rig.Cola, 1, out _), Is.True);
            int stockBefore =
                rig.Inventory.Inventory.GetQuantity("loc-shelf-cola", "prod-cola");
            int visibleBefore = rig.PhysicalUnits.VisibleUnitCount;

            Assert.That(
                rig.Checkout.TryComplete(
                    out _,
                    out CheckoutFailure failure),
                Is.False);
            Assert.That(failure, Is.EqualTo(CheckoutFailure.DuplicateTransactionId));
            Assert.That(rig.Checkout.CompletedTransactionCount, Is.EqualTo(1));
            Assert.That(
                rig.Inventory.Inventory.GetQuantity("loc-shelf-cola", "prod-cola"),
                Is.EqualTo(stockBefore));
            Assert.That(rig.PhysicalUnits.VisibleUnitCount, Is.EqualTo(visibleBefore));
        }

        [Test]
        public void RepeatedRemovalAndStockingUsesDistinctPhysicalUnits()
        {
            AdapterRig rig = CreateAdapterRig(colaBoxQuantity: 2, chipsBoxQuantity: 0);
            ProductItem first = StockOne(rig, rig.Cola);
            ProductItem second = StockOne(rig, rig.Cola);

            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.PhysicalUnitId, Is.Not.EqualTo(second.PhysicalUnitId));
            Assert.That(first.IsSnapped, Is.True);
            Assert.That(second.IsSnapped, Is.True);
            Assert.That(first.SnappedPointId, Is.Not.EqualTo(second.SnappedPointId));
            Assert.That(rig.PhysicalUnits.VisibleUnitCount, Is.EqualTo(2));
            Assert.That(rig.Inventory.Inventory.GetQuantity("loc-shelf-cola", "prod-cola"), Is.EqualTo(2));
            Assert.That(rig.Inventory.Inventory.GetTotalQuantity("prod-cola"), Is.EqualTo(2));
        }

        [Test]
        public void FailedPhysicalPlacementKeepsOneHeldUnitWithoutDuplication()
        {
            AdapterRig rig = CreateAdapterRig(
                colaBoxQuantity: 2,
                chipsBoxQuantity: 0,
                shelfSnapPointCount: 1);
            ProductItem shelved = StockOne(rig, rig.Cola);
            Assert.That(rig.Delivery.TryOpen(out _, out _), Is.True);
            Assert.That(
                rig.Delivery.TryRemoveOneUnit(
                    rig.Cola,
                    out ProductItem loose,
                    out _,
                    out _,
                    out string error),
                Is.True,
                error);
            Assert.That(
                rig.Stocking.TryPickUpLooseUnit(
                    rig.Cola,
                    out ProductItem held,
                    out error),
                Is.True,
                error);
            Assert.That(held, Is.SameAs(loose));

            Assert.That(rig.Stocking.TryStockHeldUnit(0, out error), Is.False);
            StringAssert.Contains("snap point", error);
            Assert.That(held.IsHeld, Is.True);
            Assert.That(shelved.IsSnapped, Is.True);
            Assert.That(rig.Inventory.Inventory.GetQuantity("loc-held", "prod-cola"), Is.EqualTo(1));
            Assert.That(rig.Inventory.Inventory.GetQuantity("loc-shelf-cola", "prod-cola"), Is.EqualTo(1));
            Assert.That(rig.Inventory.Inventory.GetTotalQuantity("prod-cola"), Is.EqualTo(2));
            Assert.That(rig.PhysicalUnits.VisibleUnitCount, Is.EqualTo(2));
        }

        [Test]
        public void EssentialFixtureMoveAndRemovalAreRejectedWhileOpenAndClosing()
        {
            AdapterRig rig = CreateAdapterRig(colaBoxQuantity: 1, chipsBoxQuantity: 0);
            StockOne(rig, rig.Cola);
            Assert.That(
                rig.FixturePlacement.TryPlace(
                    rig.PlaceableFixture,
                    new GridPosition(1, 1),
                    0).IsSuccess,
                Is.True);
            Assert.That(rig.Cleaning.TryApplyProgress(4), Is.EqualTo(CleaningProgressResult.Completed));
            Assert.That(rig.Store.TryBeginPreparation(out string error), Is.True, error);
            Assert.That(rig.Store.TryOpenStore(out error), Is.True, error);

            AssertRestrictedFixtureChanges(rig);
            Assert.That(rig.Store.TryBeginClosing(out error), Is.True, error);
            AssertRestrictedFixtureChanges(rig);
        }

        [Test]
        public void ClosingDerivesGrossCogsExpensesContributionAndCounts()
        {
            AdapterRig rig = CreateAdapterRig(colaBoxQuantity: 2, chipsBoxQuantity: 1);
            StockOne(rig, rig.Cola);
            StockOne(rig, rig.Cola);
            StockOne(rig, rig.Chips);
            Assert.That(
                rig.FixturePlacement.TryPlace(
                    rig.PlaceableFixture,
                    new GridPosition(1, 1),
                    0).IsSuccess,
                Is.True);
            Assert.That(rig.Cleaning.TryApplyProgress(4), Is.EqualTo(CleaningProgressResult.Completed));
            Assert.That(rig.Store.TryBeginPreparation(out string error), Is.True, error);
            Assert.That(rig.Store.TryOpenStore(out error), Is.True, error);

            CompleteSale(rig, "transaction-result-001", rig.Cola, 2);
            CompleteSale(rig, "transaction-result-002", rig.Chips, 1);
            Assert.That(rig.Store.TryBeginClosing(out error), Is.True, error);
            Assert.That(rig.Store.TryFinishClosing(out error), Is.True, error);

            StoreSessionTotals totals = rig.Store.ResultTotals;
            Assert.That(totals.grossSalesCents, Is.EqualTo(597));
            Assert.That(totals.costOfGoodsSoldCents, Is.EqualTo(220));
            Assert.That(totals.includedOperatingExpensesCents, Is.EqualTo(90));
            Assert.That(totals.contributionAfterCostOfGoodsCents, Is.EqualTo(287));
            Assert.That(totals.unitsSold, Is.EqualTo(3));
            Assert.That(totals.transactionCount, Is.EqualTo(2));
            Assert.That(rig.Store.State, Is.EqualTo(StoreOperatingState.ClosedWithResultPending));
        }

        [Test]
        public void DuplicateOrNonShelfCheckoutMappingsBlockInitialization()
        {
            AdapterRig rig = CreateAdapterRig(colaBoxQuantity: 1, chipsBoxQuantity: 1);
            CheckoutStationComponent duplicate = CreateCheckout(
                rig.Inventory,
                rig.PhysicalUnits,
                (rig.Cola, "loc-shelf-cola", 149, 60),
                (rig.Cola, "loc-shelf-cola", 149, 60));
            Assert.That(duplicate.TryValidateConfiguration(out string duplicateError), Is.False);
            StringAssert.Contains("duplicate", duplicateError.ToLowerInvariant());

            CheckoutStationComponent nonShelf = CreateCheckout(
                rig.Inventory,
                rig.PhysicalUnits,
                (rig.Cola, "loc-loose", 149, 60));
            Assert.That(nonShelf.TryValidateConfiguration(out string mappingError), Is.False);
            StringAssert.Contains("shelf mapping", mappingError);
        }

        [Test]
        public void PersistenceRestoreReconcilesPhysicalUnitsAndLedgerWithoutReplay()
        {
            AdapterRig rig = CreateAdapterRig(colaBoxQuantity: 2, chipsBoxQuantity: 1);
            StockOne(rig, rig.Cola);
            StockOne(rig, rig.Chips);
            CompleteSale(rig, "transaction-restore-001", rig.Cola, 1);
            Assert.That(
                rig.FixturePlacement.TryPlace(
                    rig.PlaceableFixture,
                    new GridPosition(1, 1),
                    1).IsSuccess,
                Is.True);
            Assert.That(rig.Cleaning.TryApplyProgress(2), Is.EqualTo(CleaningProgressResult.Progressed));
            FirstStorePersistenceMapperComponent mapper = CreatePersistenceMapper(rig);

            Assert.That(mapper.TryCapture(out FirstStoreSnapshot before, out string error), Is.True, error);
            Assert.That(before.physicalProductUnits.Count, Is.EqualTo(1));
            Assert.That(before.transactionLedger.transactions.Count, Is.EqualTo(1));

            Assert.That(
                rig.Delivery.TryRemoveOneUnit(
                    rig.Cola,
                    out _,
                    out _,
                    out _,
                    out error),
                Is.True,
                error);
            Assert.That(
                rig.FixturePlacement.TryMove(
                    rig.PlaceableFixture,
                    new GridPosition(4, 3),
                    2).IsSuccess,
                Is.True);
            Assert.That(rig.Cleaning.TryApplyProgress(2), Is.EqualTo(CleaningProgressResult.Completed));

            Assert.That(mapper.TryRestore(before, out error), Is.True, error);
            Assert.That(mapper.TryRestore(before, out error), Is.True, error);
            Assert.That(mapper.TryCapture(out FirstStoreSnapshot after, out error), Is.True, error);

            Assert.That(after, Is.EqualTo(before));
            Assert.That(rig.Checkout.CompletedTransactionCount, Is.EqualTo(1));
            Assert.That(rig.Checkout.GrossSalesCents, Is.EqualTo(149));
            Assert.That(rig.PhysicalUnits.VisibleUnitCount, Is.EqualTo(1));
            Assert.That(rig.Inventory.Inventory.GetQuantity("loc-shelf-chips", "prod-chips"), Is.EqualTo(1));
            Assert.That(rig.Inventory.Inventory.GetQuantity("loc-loose", "prod-cola"), Is.Zero);
        }

        private AdapterRig CreateAdapterRig(
            int colaBoxQuantity,
            int chipsBoxQuantity,
            int shelfSnapPointCount = 4)
        {
            ProductDefinition cola = CreateProductDefinition("prod-cola", "Cola");
            ProductDefinition chips = CreateProductDefinition("prod-chips", "Chips");
            FirstStoreInventoryComponent inventory = CreateInventoryComponent(
                new[] { cola, chips },
                new[]
                {
                    ("loc-box", InventoryLocationKind.DeliveryContainer, 20, false),
                    ("loc-loose", InventoryLocationKind.Loose, 20, false),
                    ("loc-held", InventoryLocationKind.Held, 1, true),
                    ("loc-shelf-cola", InventoryLocationKind.Shelf, 10, true),
                    ("loc-shelf-chips", InventoryLocationKind.Shelf, 10, true)
                },
                CreateStartingInventory(
                    cola,
                    colaBoxQuantity,
                    chips,
                    chipsBoxQuantity));
            PhysicalProductUnitRegistry physicalUnits =
                CreatePhysicalUnitRegistry(cola, chips);
            ShelfFixture colaShelf = CreateShelf(
                "fixture-shelf-cola",
                "slot-cola",
                shelfSnapPointCount);
            ShelfFixture chipsShelf = CreateShelf(
                "fixture-shelf-chips",
                "slot-chips",
                shelfSnapPointCount);
            Transform holdPoint = CreateGameObject("Hold Point").transform;
            StockingController stocking = CreateStockingController(
                inventory,
                physicalUnits,
                holdPoint,
                (cola, colaShelf, "loc-shelf-cola", CreateSnapPointIds("slot-cola", shelfSnapPointCount)),
                (chips, chipsShelf, "loc-shelf-chips", CreateSnapPointIds("slot-chips", shelfSnapPointCount)));
            CheckoutStationComponent checkout = CreateCheckout(
                inventory,
                physicalUnits,
                (cola, "loc-shelf-cola", 149, 60),
                (chips, "loc-shelf-chips", 299, 100));
            PlaceableFixtureComponent placeableFixture =
                CreatePlaceableFixture("fixture-essential-01", 2, 1);
            FixturePlacementController fixturePlacement =
                CreateFixtureController(placeableFixture);
            CleaningTaskComponent cleaning = CreateCleaningTask();
            DeliveryBoxComponent delivery = CreateDeliveryBox(
                inventory,
                physicalUnits,
                cola,
                chips);
            StoreOperatingController store = CreateStoreOperating(
                fixturePlacement,
                stocking,
                checkout,
                cleaning);

            return new AdapterRig(
                cola,
                chips,
                inventory,
                physicalUnits,
                colaShelf,
                chipsShelf,
                placeableFixture,
                fixturePlacement,
                stocking,
                checkout,
                cleaning,
                delivery,
                store);
        }

        private static (ProductDefinition product, string locationId, int quantity)[]
            CreateStartingInventory(
                ProductDefinition cola,
                int colaQuantity,
                ProductDefinition chips,
                int chipsQuantity)
        {
            List<(ProductDefinition product, string locationId, int quantity)> result = new();
            if (colaQuantity > 0)
            {
                result.Add((cola, "loc-box", colaQuantity));
            }
            if (chipsQuantity > 0)
            {
                result.Add((chips, "loc-box", chipsQuantity));
            }
            return result.ToArray();
        }

        private FirstStoreInventoryComponent CreateInventoryComponent(
            ProductDefinition[] products,
            (string id, InventoryLocationKind kind, int capacity, bool single)[] locations,
            (ProductDefinition product, string locationId, int quantity)[] starting)
        {
            FirstStoreInventoryComponent component =
                CreateGameObject("Inventory").AddComponent<FirstStoreInventoryComponent>();
            SerializedObject serialized = new(component);
            SetObjectArray(serialized.FindProperty("productDefinitions"), products);

            SerializedProperty locationArray = serialized.FindProperty("locations");
            locationArray.arraySize = locations.Length;
            for (int index = 0; index < locations.Length; index++)
            {
                SetLocation(locationArray.GetArrayElementAtIndex(index), locations[index]);
            }

            SerializedProperty startingArray = serialized.FindProperty("startingQuantities");
            startingArray.arraySize = starting.Length;
            for (int index = 0; index < starting.Length; index++)
            {
                SerializedProperty entry = startingArray.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("productDefinition").objectReferenceValue =
                    starting[index].product;
                entry.FindPropertyRelative("locationId").stringValue =
                    starting[index].locationId;
                entry.FindPropertyRelative("quantityUnits").intValue =
                    starting[index].quantity;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(component.TryInitialize(out string error), Is.True, error);
            return component;
        }

        private PhysicalProductUnitRegistry CreatePhysicalUnitRegistry(
            params ProductDefinition[] products)
        {
            GameObject root = CreateGameObject("Physical Units");
            PhysicalProductUnitRegistry registry =
                root.AddComponent<PhysicalProductUnitRegistry>();
            SerializedObject serialized = new(registry);
            SerializedProperty configurations = serialized.FindProperty("products");
            configurations.arraySize = products.Length;
            for (int index = 0; index < products.Length; index++)
            {
                ProductItem prefab = CreateProductItem(
                    $"{products[index].StableProductId} Unit Prefab",
                    products[index]);
                prefab.gameObject.SetActive(false);
                Transform spawn = CreateGameObject(
                    $"{products[index].StableProductId} Loose Spawn").transform;
                spawn.SetParent(root.transform, false);
                spawn.localPosition = new Vector3(0f, 0f, index * 0.5f);

                SerializedProperty configuration =
                    configurations.GetArrayElementAtIndex(index);
                configuration.FindPropertyRelative("productDefinition").objectReferenceValue =
                    products[index];
                configuration.FindPropertyRelative("unitPrefab").objectReferenceValue = prefab;
                configuration.FindPropertyRelative("looseSpawnPoint").objectReferenceValue = spawn;
                configuration.FindPropertyRelative("looseUnitSpacing").vector3Value =
                    new Vector3(0.2f, 0f, 0f);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(registry.TryValidateConfiguration(out string error), Is.True, error);
            return registry;
        }

        private StockingController CreateStockingController(
            FirstStoreInventoryComponent inventory,
            PhysicalProductUnitRegistry physicalUnits,
            Transform holdPoint,
            params (ProductDefinition product, ShelfFixture shelf, string shelfLocation, string[] snapPoints)[] products)
        {
            StockingController stocking =
                CreateGameObject("Stocking").AddComponent<StockingController>();
            SerializedObject serialized = new(stocking);
            serialized.FindProperty("inventoryComponent").objectReferenceValue = inventory;
            serialized.FindProperty("physicalUnits").objectReferenceValue = physicalUnits;
            serialized.FindProperty("holdPoint").objectReferenceValue = holdPoint;
            serialized.FindProperty("looseLocationId").stringValue = "loc-loose";
            serialized.FindProperty("heldLocationId").stringValue = "loc-held";
            SerializedProperty configurations = serialized.FindProperty("products");
            configurations.arraySize = products.Length;
            for (int index = 0; index < products.Length; index++)
            {
                SerializedProperty configuration =
                    configurations.GetArrayElementAtIndex(index);
                configuration.FindPropertyRelative("productDefinition").objectReferenceValue =
                    products[index].product;
                configuration.FindPropertyRelative("shelfFixture").objectReferenceValue =
                    products[index].shelf;
                configuration.FindPropertyRelative("shelfLocationId").stringValue =
                    products[index].shelfLocation;
                SerializedProperty snapPoints =
                    configuration.FindPropertyRelative("snapPointIds");
                snapPoints.arraySize = products[index].snapPoints.Length;
                for (int snapIndex = 0; snapIndex < products[index].snapPoints.Length; snapIndex++)
                {
                    snapPoints.GetArrayElementAtIndex(snapIndex).stringValue =
                        products[index].snapPoints[snapIndex];
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(stocking.TryValidateConfiguration(out string error), Is.True, error);
            return stocking;
        }

        private CheckoutStationComponent CreateCheckout(
            FirstStoreInventoryComponent inventory,
            PhysicalProductUnitRegistry physicalUnits,
            params (ProductDefinition product, string shelfLocation, int price, int cost)[] products)
        {
            CheckoutStationComponent checkout =
                CreateGameObject("Checkout").AddComponent<CheckoutStationComponent>();
            SerializedObject serialized = new(checkout);
            serialized.FindProperty("inventoryComponent").objectReferenceValue = inventory;
            serialized.FindProperty("physicalUnits").objectReferenceValue = physicalUnits;
            serialized.FindProperty("maximumCompletedTransactions").intValue = 32;
            SerializedProperty prices = serialized.FindProperty("prices");
            prices.arraySize = products.Length;
            for (int index = 0; index < products.Length; index++)
            {
                SerializedProperty price = prices.GetArrayElementAtIndex(index);
                price.FindPropertyRelative("productDefinition").objectReferenceValue =
                    products[index].product;
                price.FindPropertyRelative("shelfLocationId").stringValue =
                    products[index].shelfLocation;
                price.FindPropertyRelative("unitPriceCents").intValue =
                    products[index].price;
                price.FindPropertyRelative("unitCostCents").intValue =
                    products[index].cost;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return checkout;
        }

        private DeliveryBoxComponent CreateDeliveryBox(
            FirstStoreInventoryComponent inventory,
            PhysicalProductUnitRegistry physicalUnits,
            params ProductDefinition[] products)
        {
            DeliveryBoxComponent delivery =
                CreateGameObject("Delivery Box").AddComponent<DeliveryBoxComponent>();
            SerializedObject serialized = new(delivery);
            serialized.FindProperty("stableContainerId").stringValue =
                "container-starter";
            serialized.FindProperty("inventoryLocationId").stringValue = "loc-box";
            serialized.FindProperty("looseDestinationLocationId").stringValue =
                "loc-loose";
            SetObjectArray(serialized.FindProperty("productDefinitions"), products);
            serialized.FindProperty("inventoryComponent").objectReferenceValue = inventory;
            serialized.FindProperty("physicalUnits").objectReferenceValue = physicalUnits;
            serialized.FindProperty("startsOpen").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(delivery.TryInitialize(out string error), Is.True, error);
            return delivery;
        }

        private StoreOperatingController CreateStoreOperating(
            FixturePlacementController fixturePlacement,
            StockingController stocking,
            CheckoutStationComponent checkout,
            CleaningTaskComponent cleaning)
        {
            StoreOperatingController store =
                CreateGameObject("Store Operating").AddComponent<StoreOperatingController>();
            SerializedObject serialized = new(store);
            serialized.FindProperty("stableSessionId").stringValue =
                "session-opening-001";
            serialized.FindProperty("fixturePlacement").objectReferenceValue =
                fixturePlacement;
            serialized.FindProperty("stocking").objectReferenceValue = stocking;
            serialized.FindProperty("checkout").objectReferenceValue = checkout;
            serialized.FindProperty("cleaningTask").objectReferenceValue = cleaning;
            serialized.FindProperty("includedOperatingExpensesCents").intValue = 90;
            SerializedProperty required =
                serialized.FindProperty("requiredFixtureInstanceIds");
            required.arraySize = 1;
            required.GetArrayElementAtIndex(0).stringValue = "fixture-essential-01";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(store.TryInitialize(out string error), Is.True, error);
            return store;
        }

        private FirstStorePersistenceMapperComponent CreatePersistenceMapper(AdapterRig rig)
        {
            FirstStorePersistenceMapperComponent mapper =
                CreateGameObject("Persistence Mapper")
                    .AddComponent<FirstStorePersistenceMapperComponent>();
            SerializedObject serialized = new(mapper);
            serialized.FindProperty("fixturePlacement").objectReferenceValue =
                rig.FixturePlacement;
            serialized.FindProperty("inventoryComponent").objectReferenceValue =
                rig.Inventory;
            SetObjectArray(serialized.FindProperty("deliveryBoxes"), rig.Delivery);
            serialized.FindProperty("physicalUnits").objectReferenceValue =
                rig.PhysicalUnits;
            serialized.FindProperty("checkout").objectReferenceValue = rig.Checkout;
            serialized.FindProperty("storeOperating").objectReferenceValue = rig.Store;
            serialized.FindProperty("cleaningTask").objectReferenceValue = rig.Cleaning;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(mapper.TryValidateConfiguration(out string error), Is.True, error);
            return mapper;
        }

        private FixturePlacementController CreateFixtureController(
            params PlaceableFixtureComponent[] fixtures)
        {
            Transform origin = CreateGameObject("Grid Origin").transform;
            FixturePlacementController controller =
                CreateGameObject("Fixture Placement")
                    .AddComponent<FixturePlacementController>();
            SerializedObject serialized = new(controller);
            serialized.FindProperty("gridOrigin").objectReferenceValue = origin;
            serialized.FindProperty("gridWidthCells").intValue = 8;
            serialized.FindProperty("gridDepthCells").intValue = 8;
            serialized.FindProperty("cellSize").floatValue = 0.5f;
            SetObjectArray(serialized.FindProperty("fixtures"), fixtures);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(controller.TryInitialize(out string error), Is.True, error);
            return controller;
        }

        private PlaceableFixtureComponent CreatePlaceableFixture(
            string stableId,
            int width,
            int depth)
        {
            PlaceableFixtureComponent fixture =
                CreateGameObject(stableId).AddComponent<PlaceableFixtureComponent>();
            SerializedObject serialized = new(fixture);
            serialized.FindProperty("stableFixtureInstanceId").stringValue = stableId;
            serialized.FindProperty("footprintWidthCells").intValue = width;
            serialized.FindProperty("footprintDepthCells").intValue = depth;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return fixture;
        }

        private ShelfFixture CreateShelf(
            string fixtureId,
            string snapPointPrefix,
            int count)
        {
            ShelfFixture shelf =
                CreateGameObject(fixtureId).AddComponent<ShelfFixture>();
            SerializedObject serialized = new(shelf);
            serialized.FindProperty("stableFixtureId").stringValue = fixtureId;
            SerializedProperty snapPoints = serialized.FindProperty("snapPoints");
            snapPoints.arraySize = count;
            for (int index = 0; index < count; index++)
            {
                SerializedProperty snapPoint = snapPoints.GetArrayElementAtIndex(index);
                snapPoint.FindPropertyRelative("stableSnapPointId").stringValue =
                    $"{snapPointPrefix}-{index + 1:00}";
                snapPoint.FindPropertyRelative("localPosition").vector3Value =
                    new Vector3(index * 0.25f, 0f, 0f);
                SerializedProperty tags =
                    snapPoint.FindPropertyRelative("acceptedCompatibilityTags");
                tags.arraySize = 1;
                tags.GetArrayElementAtIndex(0).stringValue = "shelf-small";
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return shelf;
        }

        private ProductDefinition CreateProductDefinition(
            string stableId,
            string displayName)
        {
            ProductDefinition definition =
                ScriptableObject.CreateInstance<ProductDefinition>();
            createdObjects.Add(definition);
            SerializedObject serialized = new(definition);
            serialized.FindProperty("stableProductId").stringValue = stableId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("shelfFootprint").enumValueIndex =
                (int)ProductFootprint.Small;
            serialized.FindProperty("snapCompatibilityTag").stringValue =
                "shelf-small";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private ProductItem CreateProductItem(
            string objectName,
            ProductDefinition product)
        {
            ProductItem item =
                CreateGameObject(objectName).AddComponent<ProductItem>();
            SerializedObject serialized = new(item);
            serialized.FindProperty("definition").objectReferenceValue = product;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        private CleaningTaskComponent CreateCleaningTask()
        {
            CleaningTaskComponent cleaning =
                CreateGameObject("Cleaning Task").AddComponent<CleaningTaskComponent>();
            SerializedObject serialized = new(cleaning);
            serialized.FindProperty("stableTaskId").stringValue =
                "task-floor-spill-01";
            serialized.FindProperty("requiredProgressUnits").intValue = 4;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return cleaning;
        }

        private ProductItem StockOne(AdapterRig rig, ProductDefinition product)
        {
            Assert.That(rig.Delivery.TryOpen(out _, out string error), Is.True, error);
            Assert.That(
                rig.Delivery.TryRemoveOneUnit(
                    product,
                    out ProductItem removed,
                    out _,
                    out _,
                    out error),
                Is.True,
                error);
            Assert.That(
                rig.Stocking.TryPickUpLooseUnit(
                    product,
                    out ProductItem selected,
                    out error),
                Is.True,
                error);
            Assert.That(selected, Is.SameAs(removed));
            Assert.That(rig.Stocking.TryStockHeldUnit(0, out error), Is.True, error);
            return removed;
        }

        private static void CompleteSale(
            AdapterRig rig,
            string transactionId,
            ProductDefinition product,
            int quantity)
        {
            Assert.That(rig.Checkout.TryBeginSession(transactionId, out string error), Is.True, error);
            Assert.That(rig.Checkout.TryScan(product, quantity, out _), Is.True);
            Assert.That(
                rig.Checkout.TryComplete(
                    out _,
                    out CheckoutFailure failure),
                Is.True,
                failure.ToString());
        }

        private static void AssertRestrictedFixtureChanges(AdapterRig rig)
        {
            Assert.That(
                rig.FixturePlacement.TryMove(
                    rig.PlaceableFixture,
                    new GridPosition(3, 3),
                    1).Failure,
                Is.EqualTo(FixturePlacementFailure.OperatingStateRestricted));
            Assert.That(
                rig.FixturePlacement.TryRemove(rig.PlaceableFixture).Failure,
                Is.EqualTo(FixturePlacementFailure.OperatingStateRestricted));
            Assert.That(rig.FixturePlacement.IsPlaced("fixture-essential-01"), Is.True);
        }

        private GameObject CreateGameObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static string[] CreateSnapPointIds(string prefix, int count)
        {
            string[] result = new string[count];
            for (int index = 0; index < count; index++)
            {
                result[index] = $"{prefix}-{index + 1:00}";
            }
            return result;
        }

        private static void SetLocation(
            SerializedProperty property,
            (string id, InventoryLocationKind kind, int capacity, bool single) value)
        {
            property.FindPropertyRelative("locationId").stringValue = value.id;
            property.FindPropertyRelative("kind").enumValueIndex = (int)value.kind;
            property.FindPropertyRelative("capacityUnits").intValue = value.capacity;
            property.FindPropertyRelative("singleProductOnly").boolValue = value.single;
        }

        private static void SetObjectArray(
            SerializedProperty property,
            params Object[] values)
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
            }
        }

        private sealed class AdapterRig
        {
            public ProductDefinition Cola { get; }
            public ProductDefinition Chips { get; }
            public FirstStoreInventoryComponent Inventory { get; }
            public PhysicalProductUnitRegistry PhysicalUnits { get; }
            public ShelfFixture ColaShelf { get; }
            public ShelfFixture ChipsShelf { get; }
            public PlaceableFixtureComponent PlaceableFixture { get; }
            public FixturePlacementController FixturePlacement { get; }
            public StockingController Stocking { get; }
            public CheckoutStationComponent Checkout { get; }
            public CleaningTaskComponent Cleaning { get; }
            public DeliveryBoxComponent Delivery { get; }
            public StoreOperatingController Store { get; }

            public AdapterRig(
                ProductDefinition cola,
                ProductDefinition chips,
                FirstStoreInventoryComponent inventory,
                PhysicalProductUnitRegistry physicalUnits,
                ShelfFixture colaShelf,
                ShelfFixture chipsShelf,
                PlaceableFixtureComponent placeableFixture,
                FixturePlacementController fixturePlacement,
                StockingController stocking,
                CheckoutStationComponent checkout,
                CleaningTaskComponent cleaning,
                DeliveryBoxComponent delivery,
                StoreOperatingController store)
            {
                Cola = cola;
                Chips = chips;
                Inventory = inventory;
                PhysicalUnits = physicalUnits;
                ColaShelf = colaShelf;
                ChipsShelf = chipsShelf;
                PlaceableFixture = placeableFixture;
                FixturePlacement = fixturePlacement;
                Stocking = stocking;
                Checkout = checkout;
                Cleaning = cleaning;
                Delivery = delivery;
                Store = store;
            }
        }
    }
}
