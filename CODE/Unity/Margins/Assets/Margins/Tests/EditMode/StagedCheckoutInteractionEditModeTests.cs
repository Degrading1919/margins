using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Margins.Tests
{
    [Category("FirstStoreInteractions")]
    public sealed class StagedCheckoutInteractionEditModeTests
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
        public void PrimaryBeginsAndScansConfiguredProductsInDeterministicOrder()
        {
            CheckoutRig rig = CreateRig();

            Assert.That(rig.Staged.NextAction, Is.EqualTo(StagedCheckoutPrimaryAction.Begin));
            Assert.That(rig.Staged.TryPrimary(out _, out CheckoutFailure failure, out string error), Is.True, error);
            Assert.That(failure, Is.EqualTo(CheckoutFailure.None));
            Assert.That(rig.Staged.NextAction, Is.EqualTo(StagedCheckoutPrimaryAction.Scan));
            Assert.That(rig.Staged.ActiveProduct, Is.SameAs(rig.Cola));
            Assert.That(rig.Staged.ActiveProductDisplayName, Is.EqualTo("Cola"));
            Assert.That(rig.Staged.ActiveLineQuantity, Is.EqualTo(2));
            Assert.That(rig.Staged.ActiveLineScannedQuantity, Is.Zero);

            Assert.That(rig.Staged.TryPrimary(out _, out failure, out error), Is.True, error);
            Assert.That(rig.Staged.ActiveProduct, Is.SameAs(rig.Cola));
            Assert.That(rig.Staged.ActiveLineScannedQuantity, Is.EqualTo(1));
            Assert.That(rig.Staged.SubtotalCents, Is.EqualTo(149));
            Assert.That(rig.Staged.TryPrimary(out _, out failure, out error), Is.True, error);
            Assert.That(rig.Staged.ActiveProduct, Is.SameAs(rig.Chips));
            Assert.That(rig.Staged.ActiveLineQuantity, Is.EqualTo(1));
            Assert.That(rig.Staged.ActiveLineScannedQuantity, Is.Zero);
            Assert.That(rig.Staged.SubtotalCents, Is.EqualTo(298));
        }

        [Test]
        public void CorrectionRemovesOneMostRecentCorrectableScanWithoutUnderflow()
        {
            CheckoutRig rig = CreateRig();
            Begin(rig.Staged);
            Scan(rig.Staged);
            Scan(rig.Staged);

            Assert.That(rig.Staged.ActiveProduct, Is.SameAs(rig.Chips));
            Assert.That(rig.Staged.TryCorrect(out CheckoutFailure failure, out string error), Is.True, error);
            Assert.That(failure, Is.EqualTo(CheckoutFailure.None));
            Assert.That(rig.Staged.ActiveProduct, Is.SameAs(rig.Cola));
            Assert.That(rig.Staged.ActiveLineScannedQuantity, Is.EqualTo(1));

            Assert.That(rig.Staged.TryCorrect(out failure, out error), Is.True, error);
            Assert.That(rig.Staged.ActiveLineScannedQuantity, Is.Zero);
            Assert.That(rig.Staged.TryCorrect(out failure, out error), Is.False);
            Assert.That(failure, Is.EqualTo(CheckoutFailure.InvalidSession));
            StringAssert.Contains("No scanned product", error);
        }

        [Test]
        public void CompletionReplaysExistingSummaryWithoutDoubleConsumption()
        {
            CheckoutRig rig = CreateRig();
            Begin(rig.Staged);
            Scan(rig.Staged);
            Scan(rig.Staged);
            Scan(rig.Staged);

            Assert.That(rig.Staged.NextAction, Is.EqualTo(StagedCheckoutPrimaryAction.Complete));
            Assert.That(rig.Staged.TryPrimary(out CheckoutTransactionSummary completed, out CheckoutFailure failure, out string error), Is.True, error);
            Assert.That(failure, Is.EqualTo(CheckoutFailure.None));
            Assert.That(completed.transactionId, Is.EqualTo("transaction-basket-01"));
            Assert.That(completed.subtotalCents, Is.EqualTo(497));
            Assert.That(rig.Staged.SubtotalCents, Is.EqualTo(497));
            Assert.That(rig.Checkout.CompletedTransactionCount, Is.EqualTo(1));
            Assert.That(rig.Checkout.UnitsSold, Is.EqualTo(3));
            Assert.That(rig.Staged.NextAction, Is.EqualTo(StagedCheckoutPrimaryAction.Replay));

            Assert.That(rig.Staged.TryPrimary(out CheckoutTransactionSummary replay, out failure, out error), Is.True, error);
            Assert.That(failure, Is.EqualTo(CheckoutFailure.AlreadyCompleted));
            Assert.That(replay, Is.EqualTo(completed));
            Assert.That(rig.Checkout.CompletedTransactionCount, Is.EqualTo(1));
            Assert.That(rig.Checkout.UnitsSold, Is.EqualTo(3));
        }

        [Test]
        public void ContinueAdvancesOnlyAfterCompletionAndCompletesConfiguredSequence()
        {
            CheckoutRig rig = CreateRig();
            Assert.That(rig.Staged.TryContinue(out string error), Is.False);
            StringAssert.Contains("Complete", error);

            Begin(rig.Staged);
            Scan(rig.Staged);
            Scan(rig.Staged);
            Scan(rig.Staged);
            Assert.That(rig.Staged.TryPrimary(out _, out _, out error), Is.True, error);
            Assert.That(rig.Staged.TryContinue(out error), Is.True, error);
            Assert.That(rig.Staged.CurrentBasketNumber, Is.EqualTo(2));
            Assert.That(rig.Staged.NextAction, Is.EqualTo(StagedCheckoutPrimaryAction.Begin));

            Begin(rig.Staged);
            Scan(rig.Staged);
            Assert.That(rig.Staged.TryPrimary(out CheckoutTransactionSummary summary, out _, out error), Is.True, error);
            Assert.That(summary.transactionId, Is.EqualTo("transaction-basket-02"));
            Assert.That(rig.Staged.TryContinue(out error), Is.True, error);
            Assert.That(rig.Staged.AllBasketsComplete, Is.True);
            Assert.That(rig.Staged.NextAction, Is.EqualTo(StagedCheckoutPrimaryAction.None));
            Assert.That(rig.Checkout.CompletedTransactionCount, Is.EqualTo(2));
        }

        private static void Begin(StagedCheckoutInteractionComponent staged)
        {
            Assert.That(staged.TryPrimary(out _, out CheckoutFailure failure, out string error), Is.True, error);
            Assert.That(failure, Is.EqualTo(CheckoutFailure.None));
        }

        private static void Scan(StagedCheckoutInteractionComponent staged)
        {
            Assert.That(staged.TryPrimary(out _, out CheckoutFailure failure, out string error), Is.True, error);
            Assert.That(failure, Is.EqualTo(CheckoutFailure.None));
        }

        private CheckoutRig CreateRig()
        {
            ProductDefinition cola = CreateProduct("prod-cola", "Cola");
            ProductDefinition chips = CreateProduct("prod-chips", "Chips");
            FirstStoreInventoryComponent inventory = CreateInventory(cola, chips);
            ShelfFixture colaShelf = CreateShelf("fixture-shelf-cola", "slot-cola", 3);
            ShelfFixture chipsShelf = CreateShelf("fixture-shelf-chips", "slot-chips", 2);
            PhysicalProductUnitRegistry physicalUnits = CreatePhysicalUnits(cola, chips);
            StockPhysicalUnits(physicalUnits, colaShelf, cola, "loc-shelf-cola", 3);
            StockPhysicalUnits(physicalUnits, chipsShelf, chips, "loc-shelf-chips", 2);
            CheckoutStationComponent checkout = CreateCheckout(inventory, physicalUnits, cola, chips);
            StagedCheckoutInteractionComponent staged = CreateStagedCheckout(checkout, cola, chips);
            return new CheckoutRig(cola, chips, checkout, staged);
        }

        private FirstStoreInventoryComponent CreateInventory(ProductDefinition cola, ProductDefinition chips)
        {
            FirstStoreInventoryComponent inventory = CreateGameObject("Inventory")
                .AddComponent<FirstStoreInventoryComponent>();
            SerializedObject serialized = new(inventory);
            SetObjectArray(serialized.FindProperty("productDefinitions"), cola, chips);
            SerializedProperty locations = serialized.FindProperty("locations");
            locations.arraySize = 2;
            SetLocation(locations.GetArrayElementAtIndex(0), "loc-shelf-cola");
            SetLocation(locations.GetArrayElementAtIndex(1), "loc-shelf-chips");
            SerializedProperty starting = serialized.FindProperty("startingQuantities");
            starting.arraySize = 2;
            SetStartingQuantity(starting.GetArrayElementAtIndex(0), cola, "loc-shelf-cola", 3);
            SetStartingQuantity(starting.GetArrayElementAtIndex(1), chips, "loc-shelf-chips", 2);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(inventory.TryInitialize(out string error), Is.True, error);
            return inventory;
        }

        private PhysicalProductUnitRegistry CreatePhysicalUnits(
            ProductDefinition cola,
            ProductDefinition chips)
        {
            PhysicalProductUnitRegistry registry = CreateGameObject("Physical Units")
                .AddComponent<PhysicalProductUnitRegistry>();
            SerializedObject serialized = new(registry);
            SerializedProperty products = serialized.FindProperty("products");
            products.arraySize = 2;
            ConfigurePhysicalProduct(products.GetArrayElementAtIndex(0), cola);
            ConfigurePhysicalProduct(products.GetArrayElementAtIndex(1), chips);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(registry.TryValidateConfiguration(out string error), Is.True, error);
            return registry;
        }

        private void ConfigurePhysicalProduct(SerializedProperty configuration, ProductDefinition product)
        {
            ProductItem prefab = CreateGameObject($"{product.StableProductId} Prefab")
                .AddComponent<ProductItem>();
            SerializedObject prefabSerialized = new(prefab);
            prefabSerialized.FindProperty("definition").objectReferenceValue = product;
            prefabSerialized.ApplyModifiedPropertiesWithoutUndo();
            prefab.gameObject.SetActive(false);
            Transform spawn = CreateGameObject($"{product.StableProductId} Spawn").transform;
            configuration.FindPropertyRelative("productDefinition").objectReferenceValue = product;
            configuration.FindPropertyRelative("unitPrefab").objectReferenceValue = prefab;
            configuration.FindPropertyRelative("looseSpawnPoint").objectReferenceValue = spawn;
        }

        private static void StockPhysicalUnits(
            PhysicalProductUnitRegistry registry,
            ShelfFixture shelf,
            ProductDefinition product,
            string locationId,
            int count)
        {
            string pointPrefix = product.StableProductId == "prod-cola"
                ? "slot-cola"
                : "slot-chips";
            for (int index = 0; index < count; index++)
            {
                Assert.That(
                    registry.TryMaterializeLooseUnit(product, locationId, out ProductItem item, out string error),
                    Is.True,
                    error);
                Assert.That(
                    shelf.TryPlaceAt(item, $"{pointPrefix}-{index + 1:00}", 0, out PlacementFailure failure),
                    Is.True,
                    failure.ToString());
            }
        }

        private CheckoutStationComponent CreateCheckout(
            FirstStoreInventoryComponent inventory,
            PhysicalProductUnitRegistry physicalUnits,
            ProductDefinition cola,
            ProductDefinition chips)
        {
            CheckoutStationComponent checkout = CreateGameObject("Checkout")
                .AddComponent<CheckoutStationComponent>();
            SerializedObject serialized = new(checkout);
            serialized.FindProperty("inventoryComponent").objectReferenceValue = inventory;
            serialized.FindProperty("physicalUnits").objectReferenceValue = physicalUnits;
            SerializedProperty prices = serialized.FindProperty("prices");
            prices.arraySize = 2;
            SetPrice(prices.GetArrayElementAtIndex(0), cola, "loc-shelf-cola", 149);
            SetPrice(prices.GetArrayElementAtIndex(1), chips, "loc-shelf-chips", 199);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(checkout.TryInitializeDependencies(out string error), Is.True, error);
            return checkout;
        }

        private StagedCheckoutInteractionComponent CreateStagedCheckout(
            CheckoutStationComponent checkout,
            ProductDefinition cola,
            ProductDefinition chips)
        {
            StagedCheckoutInteractionComponent staged = CreateGameObject("Staged Checkout")
                .AddComponent<StagedCheckoutInteractionComponent>();
            SerializedObject serialized = new(staged);
            serialized.FindProperty("checkout").objectReferenceValue = checkout;
            SerializedProperty baskets = serialized.FindProperty("baskets");
            baskets.arraySize = 2;
            SetBasket(baskets.GetArrayElementAtIndex(0), "transaction-basket-01", (cola, 2), (chips, 1));
            SetBasket(baskets.GetArrayElementAtIndex(1), "transaction-basket-02", (cola, 1));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(staged.TryValidateConfiguration(out string error), Is.True, error);
            return staged;
        }

        private static void SetBasket(
            SerializedProperty basket,
            string transactionId,
            params (ProductDefinition product, int quantity)[] products)
        {
            basket.FindPropertyRelative("stableTransactionId").stringValue = transactionId;
            SerializedProperty lines = basket.FindPropertyRelative("products");
            lines.arraySize = products.Length;
            for (int index = 0; index < products.Length; index++)
            {
                SerializedProperty line = lines.GetArrayElementAtIndex(index);
                line.FindPropertyRelative("productDefinition").objectReferenceValue = products[index].product;
                line.FindPropertyRelative("quantityUnits").intValue = products[index].quantity;
            }
        }

        private static void SetPrice(
            SerializedProperty price,
            ProductDefinition product,
            string locationId,
            int unitPriceCents)
        {
            price.FindPropertyRelative("productDefinition").objectReferenceValue = product;
            price.FindPropertyRelative("shelfLocationId").stringValue = locationId;
            price.FindPropertyRelative("unitPriceCents").intValue = unitPriceCents;
            price.FindPropertyRelative("unitCostCents").intValue = 0;
        }

        private static void SetLocation(SerializedProperty location, string locationId)
        {
            location.FindPropertyRelative("locationId").stringValue = locationId;
            location.FindPropertyRelative("kind").enumValueIndex = (int)InventoryLocationKind.Shelf;
            location.FindPropertyRelative("capacityUnits").intValue = 10;
            location.FindPropertyRelative("singleProductOnly").boolValue = true;
        }

        private static void SetStartingQuantity(
            SerializedProperty quantity,
            ProductDefinition product,
            string locationId,
            int units)
        {
            quantity.FindPropertyRelative("productDefinition").objectReferenceValue = product;
            quantity.FindPropertyRelative("locationId").stringValue = locationId;
            quantity.FindPropertyRelative("quantityUnits").intValue = units;
        }

        private ShelfFixture CreateShelf(string fixtureId, string pointPrefix, int pointCount)
        {
            ShelfFixture shelf = CreateGameObject(fixtureId).AddComponent<ShelfFixture>();
            SerializedObject serialized = new(shelf);
            serialized.FindProperty("stableFixtureId").stringValue = fixtureId;
            SerializedProperty points = serialized.FindProperty("snapPoints");
            points.arraySize = pointCount;
            for (int index = 0; index < pointCount; index++)
            {
                SerializedProperty point = points.GetArrayElementAtIndex(index);
                point.FindPropertyRelative("stableSnapPointId").stringValue = $"{pointPrefix}-{index + 1:00}";
                SerializedProperty tags = point.FindPropertyRelative("acceptedCompatibilityTags");
                tags.arraySize = 1;
                tags.GetArrayElementAtIndex(0).stringValue = "shelf-small";
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return shelf;
        }

        private ProductDefinition CreateProduct(string stableId, string displayName)
        {
            ProductDefinition product = ScriptableObject.CreateInstance<ProductDefinition>();
            createdObjects.Add(product);
            SerializedObject serialized = new(product);
            serialized.FindProperty("stableProductId").stringValue = stableId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("shelfFootprint").enumValueIndex = (int)ProductFootprint.Small;
            serialized.FindProperty("snapCompatibilityTag").stringValue = "shelf-small";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return product;
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject gameObject = new(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void SetObjectArray(SerializedProperty property, params Object[] values)
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        private sealed class CheckoutRig
        {
            public CheckoutRig(
                ProductDefinition cola,
                ProductDefinition chips,
                CheckoutStationComponent checkout,
                StagedCheckoutInteractionComponent staged)
            {
                Cola = cola;
                Chips = chips;
                Checkout = checkout;
                Staged = staged;
            }

            public ProductDefinition Cola { get; }
            public ProductDefinition Chips { get; }
            public CheckoutStationComponent Checkout { get; }
            public StagedCheckoutInteractionComponent Staged { get; }
        }
    }
}
