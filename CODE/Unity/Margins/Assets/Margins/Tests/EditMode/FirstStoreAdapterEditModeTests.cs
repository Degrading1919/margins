// Draft implementation — Unity verification pending
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Margins.Tests
{
    [Category("Authored_UnityUnverified")]
    public sealed class FirstStoreAdapterEditModeTests
    {
        private readonly List<Object> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object createdObject in createdObjects)
            {
                if (createdObject != null)
                {
                    Object.DestroyImmediate(createdObject);
                }
            }
            createdObjects.Clear();
        }

        [Test]
        public void InventoryAndDeliveryAdaptersTransferStateIntoDomain()
        {
            ProductDefinition product = CreateProductDefinition("prod-cola");
            FirstStoreInventoryComponent inventory = CreateInventoryComponent(
                product,
                ("loc-box", InventoryLocationKind.DeliveryContainer, 10, false),
                ("loc-loose", InventoryLocationKind.Loose, 10, false),
                ("loc-held", InventoryLocationKind.Held, 1, true),
                ("loc-shelf", InventoryLocationKind.Shelf, 10, true),
                ("loc-box", 4));
            DeliveryBoxComponent delivery = CreateDeliveryBox(
                inventory,
                product,
                false);

            Assert.That(delivery.TryOpen(out DeliveryContainerOpenResult opened, out _), Is.True);
            Assert.That(opened, Is.EqualTo(DeliveryContainerOpenResult.Opened));
            Assert.That(
                delivery.TryRemoveOneUnit(
                    out DeliveryContainerFailure failure,
                    out InventoryTransferResult transfer),
                Is.True);

            Assert.That(failure, Is.EqualTo(DeliveryContainerFailure.None));
            Assert.That(transfer.IsSuccess, Is.True);
            Assert.That(inventory.Inventory.GetQuantity("loc-box", "prod-cola"), Is.EqualTo(3));
            Assert.That(inventory.Inventory.GetQuantity("loc-loose", "prod-cola"), Is.EqualTo(1));
            Assert.That(inventory.Inventory.GetTotalQuantity("prod-cola"), Is.EqualTo(4));
        }

        [Test]
        public void InvalidInspectorConfigurationReturnsActionableError()
        {
            GameObject gameObject = CreateGameObject("Invalid Inventory");
            FirstStoreInventoryComponent inventory =
                gameObject.AddComponent<FirstStoreInventoryComponent>();

            Assert.That(inventory.TryValidateConfiguration(out string error), Is.False);
            StringAssert.Contains("product definition", error);
        }

        [Test]
        public void StockingAdapterMovesOnePhysicalAndDomainUnitWithoutDuplication()
        {
            ProductDefinition product = CreateProductDefinition("prod-cola");
            FirstStoreInventoryComponent inventory = CreateInventoryComponent(
                product,
                ("loc-box", InventoryLocationKind.DeliveryContainer, 10, false),
                ("loc-loose", InventoryLocationKind.Loose, 10, false),
                ("loc-held", InventoryLocationKind.Held, 1, true),
                ("loc-shelf", InventoryLocationKind.Shelf, 10, true),
                ("loc-box", 1));
            DeliveryBoxComponent delivery = CreateDeliveryBox(
                inventory,
                product,
                false);
            ShelfFixture shelf = CreateShelf("fixture-shelf", "slot-01");
            ProductItem item = CreateProductItem("Physical Cola", product);
            Transform holdPoint = CreateGameObject("Hold Point").transform;
            StockingController stocking = CreateStockingController(
                inventory,
                item,
                shelf,
                holdPoint);
            int totalBefore = inventory.Inventory.GetTotalQuantity("prod-cola");

            Assert.That(delivery.TryOpen(out _, out _), Is.True);
            Assert.That(delivery.TryRemoveOneUnit(out _, out _), Is.True);
            Assert.That(stocking.TryPickUpLooseUnit(out string error), Is.True, error);
            Assert.That(stocking.TryStockHeldUnit(1, out error), Is.True, error);

            Assert.That(item.IsSnapped, Is.True);
            Assert.That(item.QuarterTurns, Is.EqualTo(1));
            Assert.That(inventory.Inventory.GetQuantity("loc-box", "prod-cola"), Is.Zero);
            Assert.That(inventory.Inventory.GetQuantity("loc-loose", "prod-cola"), Is.Zero);
            Assert.That(inventory.Inventory.GetQuantity("loc-held", "prod-cola"), Is.Zero);
            Assert.That(inventory.Inventory.GetQuantity("loc-shelf", "prod-cola"), Is.EqualTo(1));
            Assert.That(inventory.Inventory.GetTotalQuantity("prod-cola"), Is.EqualTo(totalBefore));
        }

        [Test]
        public void FixturePlacementAdapterRejectsOverlapAndMarksPreviewInvalid()
        {
            PlaceableFixtureComponent first = CreatePlaceableFixture(
                "fixture-alpha",
                2,
                2);
            PlaceableFixtureComponent second = CreatePlaceableFixture(
                "fixture-beta",
                2,
                1);
            FixturePlacementController controller =
                CreateFixtureController(first, second);

            Assert.That(
                controller.TryPlace(first, new GridPosition(1, 1), 0).IsSuccess,
                Is.True);
            FixturePlacementResult rejected =
                controller.TryPlace(second, new GridPosition(2, 2), 0);

            Assert.That(rejected.Failure, Is.EqualTo(FixturePlacementFailure.Occupied));
            Assert.That(
                second.PreviewState,
                Is.EqualTo(FixturePlacementPreviewState.Invalid));
            Assert.That(controller.PlacedCount, Is.EqualTo(1));
        }

        [Test]
        public void CheckoutAdapterCompletesOnceAndConsumesOneUnit()
        {
            ProductDefinition product = CreateProductDefinition("prod-cola");
            FirstStoreInventoryComponent inventory = CreateInventoryComponent(
                product,
                ("loc-box", InventoryLocationKind.DeliveryContainer, 10, false),
                ("loc-loose", InventoryLocationKind.Loose, 10, false),
                ("loc-held", InventoryLocationKind.Held, 1, true),
                ("loc-shelf", InventoryLocationKind.Shelf, 10, true),
                ("loc-shelf", 2));
            CheckoutStationComponent checkout =
                CreateCheckout(inventory, product, 149);

            Assert.That(checkout.TryBeginSession("transaction-001", out _), Is.True);
            Assert.That(checkout.TryScan(product, 1, out _), Is.True);
            Assert.That(
                checkout.TryComplete(
                    out CheckoutTransactionSummary first,
                    out CheckoutFailure firstFailure),
                Is.True);
            Assert.That(
                checkout.TryComplete(
                    out CheckoutTransactionSummary second,
                    out CheckoutFailure secondFailure),
                Is.True);

            Assert.That(firstFailure, Is.EqualTo(CheckoutFailure.None));
            Assert.That(secondFailure, Is.EqualTo(CheckoutFailure.AlreadyCompleted));
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.subtotalCents, Is.EqualTo(149));
            Assert.That(inventory.Inventory.GetQuantity("loc-shelf", "prod-cola"), Is.EqualTo(1));
        }

        [Test]
        public void CleaningTaskCompletionIsBoundedAndIdempotent()
        {
            CleaningTaskComponent cleaning = CreateCleaningTask();

            Assert.That(
                cleaning.TryApplyProgress(2),
                Is.EqualTo(CleaningProgressResult.Progressed));
            Assert.That(
                cleaning.TryApplyProgress(10),
                Is.EqualTo(CleaningProgressResult.Completed));
            Assert.That(cleaning.CompletedProgressUnits, Is.EqualTo(4));
            Assert.That(
                cleaning.TryApplyProgress(1),
                Is.EqualTo(CleaningProgressResult.AlreadyComplete));
            Assert.That(cleaning.CompletedProgressUnits, Is.EqualTo(4));
        }

        [Test]
        public void StoreOpeningAndClosingAdaptersFollowDomainTransitions()
        {
            AdapterRig rig = CreateAdapterRig(shelfQuantity: 2, boxQuantity: 1);
            Assert.That(
                rig.FixturePlacement.TryPlace(
                    rig.PlaceableFixture,
                    new GridPosition(1, 1),
                    0).IsSuccess,
                Is.True);
            Assert.That(rig.Cleaning.TryApplyProgress(4), Is.EqualTo(CleaningProgressResult.Completed));

            Assert.That(rig.Store.TryBeginPreparation(out string error), Is.True, error);
            Assert.That(rig.Store.TryOpenStore(out error), Is.True, error);
            Assert.That(rig.Store.TryBeginClosing(out error), Is.True, error);
            Assert.That(
                rig.Store.TryFinishClosing(
                    new StoreSessionTotals(298, 2, 1, 90),
                    out error),
                Is.True,
                error);
            Assert.That(
                rig.Store.State,
                Is.EqualTo(StoreOperatingState.ClosedWithResultPending));
            Assert.That(rig.Store.TryAcknowledgeResult(out error), Is.True, error);
            Assert.That(rig.Store.State, Is.EqualTo(StoreOperatingState.Closed));
        }

        [Test]
        public void PersistenceMapperRoundTripRestoresAllAdapterState()
        {
            AdapterRig rig = CreateAdapterRig(shelfQuantity: 1, boxQuantity: 3);
            Assert.That(
                rig.FixturePlacement.TryPlace(
                    rig.PlaceableFixture,
                    new GridPosition(1, 1),
                    1).IsSuccess,
                Is.True);
            Assert.That(rig.Delivery.TryOpen(out _, out _), Is.True);
            Assert.That(rig.Checkout.TryBeginSession("transaction-restore-001", out _), Is.True);
            Assert.That(rig.Checkout.TryScan(rig.Product, 1, out _), Is.True);
            Assert.That(rig.Checkout.TryComplete(out _, out _), Is.True);
            Assert.That(
                rig.Cleaning.TryApplyProgress(2),
                Is.EqualTo(CleaningProgressResult.Progressed));
            FirstStorePersistenceMapperComponent mapper =
                CreatePersistenceMapper(rig);

            Assert.That(
                mapper.TryCapture(
                    out FirstStoreSnapshot before,
                    out string error),
                Is.True,
                error);

            Assert.That(
                rig.Inventory.Inventory.TryTransfer(
                    "prod-cola",
                    "loc-box",
                    "loc-loose",
                    1).IsSuccess,
                Is.True);
            Assert.That(
                rig.FixturePlacement.TryMove(
                    rig.PlaceableFixture,
                    new GridPosition(4, 3),
                    2).IsSuccess,
                Is.True);
            Assert.That(
                rig.Cleaning.TryApplyProgress(2),
                Is.EqualTo(CleaningProgressResult.Completed));

            Assert.That(mapper.TryRestore(before, out error), Is.True, error);
            Assert.That(
                mapper.TryCapture(
                    out FirstStoreSnapshot after,
                    out error),
                Is.True,
                error);

            Assert.That(after, Is.EqualTo(before));
        }

        private AdapterRig CreateAdapterRig(int shelfQuantity, int boxQuantity)
        {
            ProductDefinition product = CreateProductDefinition("prod-cola");
            FirstStoreInventoryComponent inventory = CreateInventoryComponent(
                product,
                ("loc-box", InventoryLocationKind.DeliveryContainer, 10, false),
                ("loc-loose", InventoryLocationKind.Loose, 10, false),
                ("loc-held", InventoryLocationKind.Held, 1, true),
                ("loc-shelf", InventoryLocationKind.Shelf, 10, true),
                ("loc-box", boxQuantity),
                ("loc-shelf", shelfQuantity));
            PlaceableFixtureComponent placeableFixture = CreatePlaceableFixture(
                "fixture-essential-01",
                2,
                1);
            FixturePlacementController fixturePlacement =
                CreateFixtureController(placeableFixture);
            ShelfFixture shelf = CreateShelf("fixture-shelf", "slot-01");
            ProductItem item = CreateProductItem("Physical Cola", product);
            Transform holdPoint = CreateGameObject("Hold Point").transform;
            StockingController stocking = CreateStockingController(
                inventory,
                item,
                shelf,
                holdPoint);
            CheckoutStationComponent checkout = CreateCheckout(
                inventory,
                product,
                149);
            CleaningTaskComponent cleaning = CreateCleaningTask();
            DeliveryBoxComponent delivery = CreateDeliveryBox(
                inventory,
                product,
                false);
            StoreOperatingController store = CreateStoreOperating(
                fixturePlacement,
                stocking,
                checkout,
                cleaning);

            return new AdapterRig(
                product,
                inventory,
                placeableFixture,
                fixturePlacement,
                stocking,
                checkout,
                cleaning,
                delivery,
                store);
        }

        private FirstStoreInventoryComponent CreateInventoryComponent(
            ProductDefinition product,
            (string id, InventoryLocationKind kind, int capacity, bool single) box,
            (string id, InventoryLocationKind kind, int capacity, bool single) loose,
            (string id, InventoryLocationKind kind, int capacity, bool single) held,
            (string id, InventoryLocationKind kind, int capacity, bool single) shelf,
            params (string locationId, int quantity)[] starting)
        {
            GameObject gameObject = CreateGameObject("Inventory");
            FirstStoreInventoryComponent component =
                gameObject.AddComponent<FirstStoreInventoryComponent>();
            SerializedObject serialized = new(component);
            SetObjectArray(
                serialized.FindProperty("productDefinitions"),
                product);

            SerializedProperty locations = serialized.FindProperty("locations");
            locations.arraySize = 4;
            SetLocation(locations.GetArrayElementAtIndex(0), box);
            SetLocation(locations.GetArrayElementAtIndex(1), loose);
            SetLocation(locations.GetArrayElementAtIndex(2), held);
            SetLocation(locations.GetArrayElementAtIndex(3), shelf);

            SerializedProperty startingQuantities =
                serialized.FindProperty("startingQuantities");
            startingQuantities.arraySize = starting.Length;
            for (int index = 0; index < starting.Length; index++)
            {
                SerializedProperty entry =
                    startingQuantities.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("productDefinition").objectReferenceValue =
                    product;
                entry.FindPropertyRelative("locationId").stringValue =
                    starting[index].locationId;
                entry.FindPropertyRelative("quantityUnits").intValue =
                    starting[index].quantity;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(component.TryInitialize(out string error), Is.True, error);
            return component;
        }

        private ProductDefinition CreateProductDefinition(string stableId)
        {
            ProductDefinition definition =
                ScriptableObject.CreateInstance<ProductDefinition>();
            definition.name = stableId;
            createdObjects.Add(definition);
            SerializedObject serialized = new(definition);
            serialized.FindProperty("stableProductId").stringValue = stableId;
            serialized.FindProperty("displayName").stringValue = "Cola";
            serialized.FindProperty("shelfFootprint").enumValueIndex =
                (int)ProductFootprint.Small;
            serialized.FindProperty("snapCompatibilityTag").stringValue =
                "shelf-small";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private PlaceableFixtureComponent CreatePlaceableFixture(
            string stableId,
            int width,
            int depth)
        {
            GameObject gameObject = CreateGameObject(stableId);
            PlaceableFixtureComponent fixture =
                gameObject.AddComponent<PlaceableFixtureComponent>();
            SerializedObject serialized = new(fixture);
            serialized.FindProperty("stableFixtureInstanceId").stringValue = stableId;
            serialized.FindProperty("footprintWidthCells").intValue = width;
            serialized.FindProperty("footprintDepthCells").intValue = depth;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return fixture;
        }

        private FixturePlacementController CreateFixtureController(
            params PlaceableFixtureComponent[] fixtures)
        {
            GameObject originObject = CreateGameObject("Grid Origin");
            GameObject controllerObject = CreateGameObject("Fixture Controller");
            FixturePlacementController controller =
                controllerObject.AddComponent<FixturePlacementController>();
            SerializedObject serialized = new(controller);
            serialized.FindProperty("gridOrigin").objectReferenceValue =
                originObject.transform;
            serialized.FindProperty("gridWidthCells").intValue = 8;
            serialized.FindProperty("gridDepthCells").intValue = 8;
            serialized.FindProperty("cellSize").floatValue = 0.5f;
            SetObjectArray(serialized.FindProperty("fixtures"), fixtures);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(controller.TryInitialize(out string error), Is.True, error);
            return controller;
        }

        private ShelfFixture CreateShelf(string fixtureId, string snapPointId)
        {
            GameObject gameObject = CreateGameObject("Shelf");
            ShelfFixture fixture = gameObject.AddComponent<ShelfFixture>();
            SerializedObject serialized = new(fixture);
            serialized.FindProperty("stableFixtureId").stringValue = fixtureId;
            SerializedProperty snapPoints = serialized.FindProperty("snapPoints");
            snapPoints.arraySize = 1;
            SerializedProperty snapPoint = snapPoints.GetArrayElementAtIndex(0);
            snapPoint.FindPropertyRelative("stableSnapPointId").stringValue =
                snapPointId;
            snapPoint.FindPropertyRelative("localPosition").vector3Value =
                Vector3.zero;
            snapPoint.FindPropertyRelative("localEulerAngles").vector3Value =
                Vector3.zero;
            SerializedProperty tags =
                snapPoint.FindPropertyRelative("acceptedCompatibilityTags");
            tags.arraySize = 1;
            tags.GetArrayElementAtIndex(0).stringValue = "shelf-small";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return fixture;
        }

        private ProductItem CreateProductItem(
            string name,
            ProductDefinition product)
        {
            GameObject gameObject = CreateGameObject(name);
            ProductItem item = gameObject.AddComponent<ProductItem>();
            SerializedObject serialized = new(item);
            serialized.FindProperty("definition").objectReferenceValue = product;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        private StockingController CreateStockingController(
            FirstStoreInventoryComponent inventory,
            ProductItem item,
            ShelfFixture shelf,
            Transform holdPoint)
        {
            GameObject gameObject = CreateGameObject("Stocking");
            StockingController stocking =
                gameObject.AddComponent<StockingController>();
            SerializedObject serialized = new(stocking);
            serialized.FindProperty("inventoryComponent").objectReferenceValue =
                inventory;
            serialized.FindProperty("productItem").objectReferenceValue = item;
            serialized.FindProperty("shelfFixture").objectReferenceValue = shelf;
            serialized.FindProperty("holdPoint").objectReferenceValue = holdPoint;
            serialized.FindProperty("looseLocationId").stringValue = "loc-loose";
            serialized.FindProperty("heldLocationId").stringValue = "loc-held";
            serialized.FindProperty("shelfLocationId").stringValue = "loc-shelf";
            serialized.FindProperty("snapPointId").stringValue = "slot-01";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(stocking.TryValidateConfiguration(out string error), Is.True, error);
            return stocking;
        }

        private CheckoutStationComponent CreateCheckout(
            FirstStoreInventoryComponent inventory,
            ProductDefinition product,
            int unitPriceCents)
        {
            GameObject gameObject = CreateGameObject("Checkout");
            CheckoutStationComponent checkout =
                gameObject.AddComponent<CheckoutStationComponent>();
            SerializedObject serialized = new(checkout);
            serialized.FindProperty("inventoryComponent").objectReferenceValue =
                inventory;
            serialized.FindProperty("shelfLocationId").stringValue = "loc-shelf";
            SerializedProperty prices = serialized.FindProperty("prices");
            prices.arraySize = 1;
            SerializedProperty price = prices.GetArrayElementAtIndex(0);
            price.FindPropertyRelative("productDefinition").objectReferenceValue =
                product;
            price.FindPropertyRelative("unitPriceCents").intValue =
                unitPriceCents;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(checkout.TryValidateConfiguration(out string error), Is.True, error);
            return checkout;
        }

        private CleaningTaskComponent CreateCleaningTask()
        {
            GameObject gameObject = CreateGameObject("Cleaning Task");
            CleaningTaskComponent cleaning =
                gameObject.AddComponent<CleaningTaskComponent>();
            SerializedObject serialized = new(cleaning);
            serialized.FindProperty("stableTaskId").stringValue =
                "task-floor-spill-01";
            serialized.FindProperty("requiredProgressUnits").intValue = 4;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(cleaning.TryValidateConfiguration(out string error), Is.True, error);
            return cleaning;
        }

        private DeliveryBoxComponent CreateDeliveryBox(
            FirstStoreInventoryComponent inventory,
            ProductDefinition product,
            bool startsOpen)
        {
            GameObject gameObject = CreateGameObject("Delivery Box");
            DeliveryBoxComponent delivery =
                gameObject.AddComponent<DeliveryBoxComponent>();
            SerializedObject serialized = new(delivery);
            serialized.FindProperty("stableContainerId").stringValue =
                "container-starter";
            serialized.FindProperty("inventoryLocationId").stringValue =
                "loc-box";
            serialized.FindProperty("looseDestinationLocationId").stringValue =
                "loc-loose";
            serialized.FindProperty("productDefinition").objectReferenceValue =
                product;
            serialized.FindProperty("inventoryComponent").objectReferenceValue =
                inventory;
            serialized.FindProperty("startsOpen").boolValue = startsOpen;
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
            GameObject gameObject = CreateGameObject("Store Operating");
            StoreOperatingController store =
                gameObject.AddComponent<StoreOperatingController>();
            SerializedObject serialized = new(store);
            serialized.FindProperty("stableSessionId").stringValue =
                "session-opening-001";
            serialized.FindProperty("fixturePlacement").objectReferenceValue =
                fixturePlacement;
            serialized.FindProperty("stocking").objectReferenceValue = stocking;
            serialized.FindProperty("checkout").objectReferenceValue = checkout;
            serialized.FindProperty("cleaningTask").objectReferenceValue = cleaning;
            SerializedProperty requiredFixtures =
                serialized.FindProperty("requiredFixtureInstanceIds");
            requiredFixtures.arraySize = 1;
            requiredFixtures.GetArrayElementAtIndex(0).stringValue =
                "fixture-essential-01";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(store.TryInitialize(out string error), Is.True, error);
            return store;
        }

        private FirstStorePersistenceMapperComponent CreatePersistenceMapper(
            AdapterRig rig)
        {
            GameObject gameObject = CreateGameObject("Persistence Mapper");
            FirstStorePersistenceMapperComponent mapper =
                gameObject.AddComponent<FirstStorePersistenceMapperComponent>();
            SerializedObject serialized = new(mapper);
            serialized.FindProperty("fixturePlacement").objectReferenceValue =
                rig.FixturePlacement;
            serialized.FindProperty("inventoryComponent").objectReferenceValue =
                rig.Inventory;
            SetObjectArray(
                serialized.FindProperty("deliveryBoxes"),
                rig.Delivery);
            serialized.FindProperty("checkout").objectReferenceValue = rig.Checkout;
            serialized.FindProperty("storeOperating").objectReferenceValue =
                rig.Store;
            serialized.FindProperty("cleaningTask").objectReferenceValue =
                rig.Cleaning;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(mapper.TryValidateConfiguration(out string error), Is.True, error);
            return mapper;
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject gameObject = new(name);
            createdObjects.Add(gameObject);
            return gameObject;
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
            public ProductDefinition Product { get; }
            public FirstStoreInventoryComponent Inventory { get; }
            public PlaceableFixtureComponent PlaceableFixture { get; }
            public FixturePlacementController FixturePlacement { get; }
            public StockingController Stocking { get; }
            public CheckoutStationComponent Checkout { get; }
            public CleaningTaskComponent Cleaning { get; }
            public DeliveryBoxComponent Delivery { get; }
            public StoreOperatingController Store { get; }

            public AdapterRig(
                ProductDefinition product,
                FirstStoreInventoryComponent inventory,
                PlaceableFixtureComponent placeableFixture,
                FixturePlacementController fixturePlacement,
                StockingController stocking,
                CheckoutStationComponent checkout,
                CleaningTaskComponent cleaning,
                DeliveryBoxComponent delivery,
                StoreOperatingController store)
            {
                Product = product;
                Inventory = inventory;
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
