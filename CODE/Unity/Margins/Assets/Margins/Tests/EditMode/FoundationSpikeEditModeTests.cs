using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Margins.Tests
{
    public sealed class FoundationSpikeEditModeTests
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
        public void DuplicateProductIdentifiersFailValidation()
        {
            ProductDefinition first = CreateProductDefinition("product-soda");
            ProductDefinition duplicate = CreateProductDefinition("product-soda");

            bool isValid = FoundationValidator.TryValidateProductDefinitions(
                new[] { first, duplicate },
                out string error);

            Assert.That(isValid, Is.False);
            StringAssert.Contains("Duplicate product identifier", error);
        }

        [Test]
        public void InvalidSnapPointReferenceIsRejectedWithoutOccupancy()
        {
            ProductDefinition definition = CreateProductDefinition("product-soda");
            ShelfFixture fixture = CreateShelf("fixture-main", ("slot-01", Vector3.zero));
            ProductItem product = CreateProduct("Product", definition);
            PlacementSaveController controller = CreateSaveController(definition, fixture, product);
            string savePath = Path.GetTempFileName();
            File.WriteAllText(savePath, FoundationSaveCodec.ToJson(new FoundationSaveData
            {
                placedProducts = new List<PlacedProductState>
                {
                    new("product-soda", "fixture-main", "slot-missing", 0)
                }
            }));

            LogAssert.Expect(LogType.Error, "Rejected saved placement: snap point id 'slot-missing' is missing from fixture 'fixture-main'.");
            bool loaded = controller.TryLoadFromPath(savePath);

            Assert.That(loaded, Is.False);
            Assert.That(fixture.HasOccupiedSnapPoints, Is.False);
            Assert.That(product.IsSnapped, Is.False);
            File.Delete(savePath);
        }

        [Test]
        public void OccupiedSlotRejectsSecondInstanceWithoutReplacingFirst()
        {
            ProductDefinition definition = CreateProductDefinition("product-soda");
            ShelfFixture fixture = CreateShelf("fixture-main", ("slot-01", Vector3.zero));
            ProductItem first = CreateProduct("First", definition);
            ProductItem second = CreateProduct("Second", definition);

            bool firstPlaced = fixture.TryPlaceAt(first, "slot-01", 1, out PlacementFailure firstFailure);
            bool secondPlaced = fixture.TryPlaceAt(second, "slot-01", 2, out PlacementFailure secondFailure);

            Assert.That(firstPlaced, Is.True);
            Assert.That(firstFailure, Is.EqualTo(PlacementFailure.None));
            Assert.That(secondPlaced, Is.False);
            Assert.That(secondFailure, Is.EqualTo(PlacementFailure.Occupied));
            Assert.That(fixture.GetOccupant("slot-01"), Is.SameAs(first));
            Assert.That(first.IsSnapped, Is.True);
            Assert.That(second.IsSnapped, Is.False);
        }

        [Test]
        public void SaveReloadPlacementEqualityPreservesAllRequiredFields()
        {
            PlacedProductState expected = new("product-soda", "fixture-main", "slot-02", 3);
            FoundationSaveData before = new()
            {
                placedProducts = new List<PlacedProductState> { expected }
            };

            string json = FoundationSaveCodec.ToJson(before);
            bool parsed = FoundationSaveCodec.TryFromJson(json, out FoundationSaveData after, out string error);

            Assert.That(parsed, Is.True, error);
            Assert.That(after.version, Is.EqualTo(PlacementSaveController.CurrentSaveVersion));
            Assert.That(after.placedProducts, Has.Count.EqualTo(1));
            Assert.That(after.placedProducts[0], Is.EqualTo(expected));
        }

        [Test]
        public void DeterministicTieUsesAscendingSnapIdentifier()
        {
            ProductDefinition definition = CreateProductDefinition("product-soda");
            ShelfFixture fixture = CreateShelf(
                "fixture-main",
                ("slot-02", new Vector3(0.5f, 0f, 0f)),
                ("slot-01", new Vector3(-0.5f, 0f, 0f)));
            ProductItem product = CreateProduct("Product", definition);

            bool found = fixture.TryFindNearestAvailable(
                product,
                Vector3.zero,
                out ShelfSnapPointDefinition selected,
                out PlacementFailure failure);

            Assert.That(found, Is.True);
            Assert.That(failure, Is.EqualTo(PlacementFailure.None));
            Assert.That(selected.StableSnapPointId, Is.EqualTo("slot-01"));
        }

        [Test]
        public void MalformedJsonFailsWithoutProducingSaveData()
        {
            bool parsed = FoundationSaveCodec.TryFromJson("{not-json", out FoundationSaveData saveData, out string error);

            Assert.That(parsed, Is.False);
            Assert.That(saveData, Is.Null);
            StringAssert.Contains("Malformed JSON", error);
        }

        [Test]
        public void DuplicateSavedTargetAcceptsFirstAndRejectsConflict()
        {
            ProductDefinition definition = CreateProductDefinition("product-soda");
            ShelfFixture fixture = CreateShelf("fixture-main", ("slot-01", Vector3.zero));
            ProductItem first = CreateProduct("First", definition);
            ProductItem second = CreateProduct("Second", definition);
            PlacementSaveController controller = CreateSaveController(definition, fixture, first, second);
            string savePath = Path.GetTempFileName();
            File.WriteAllText(savePath, FoundationSaveCodec.ToJson(new FoundationSaveData
            {
                placedProducts = new List<PlacedProductState>
                {
                    new("product-soda", "fixture-main", "slot-01", 1),
                    new("product-soda", "fixture-main", "slot-01", 2)
                }
            }));

            LogAssert.Expect(LogType.Error, "Rejected saved placement: target 'fixture-main/slot-01' rejected placement (Occupied).");
            bool loaded = controller.TryLoadFromPath(savePath);

            Assert.That(loaded, Is.False);
            Assert.That(fixture.GetOccupant("slot-01"), Is.SameAs(first));
            Assert.That(first.QuarterTurns, Is.EqualTo(1));
            Assert.That(second.IsSnapped, Is.False);
            File.Delete(savePath);
        }

        private ProductDefinition CreateProductDefinition(string stableId)
        {
            ProductDefinition definition = ScriptableObject.CreateInstance<ProductDefinition>();
            definition.name = stableId;
            createdObjects.Add(definition);
            SerializedObject serialized = new(definition);
            serialized.FindProperty("stableProductId").stringValue = stableId;
            serialized.FindProperty("displayName").stringValue = "Soda";
            serialized.FindProperty("shelfFootprint").enumValueIndex = (int)ProductFootprint.Small;
            serialized.FindProperty("snapCompatibilityTag").stringValue = "shelf-small";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private ShelfFixture CreateShelf(string fixtureId, params (string id, Vector3 position)[] definitions)
        {
            GameObject gameObject = new("Shelf");
            createdObjects.Add(gameObject);
            ShelfFixture fixture = gameObject.AddComponent<ShelfFixture>();
            SerializedObject serialized = new(fixture);
            serialized.FindProperty("stableFixtureId").stringValue = fixtureId;
            serialized.FindProperty("snapSearchRadius").floatValue = 0.75f;
            SerializedProperty snapPoints = serialized.FindProperty("snapPoints");
            snapPoints.arraySize = definitions.Length;
            for (int index = 0; index < definitions.Length; index++)
            {
                SerializedProperty snapPoint = snapPoints.GetArrayElementAtIndex(index);
                snapPoint.FindPropertyRelative("stableSnapPointId").stringValue = definitions[index].id;
                snapPoint.FindPropertyRelative("localPosition").vector3Value = definitions[index].position;
                snapPoint.FindPropertyRelative("localEulerAngles").vector3Value = Vector3.zero;
                SerializedProperty tags = snapPoint.FindPropertyRelative("acceptedCompatibilityTags");
                tags.arraySize = 1;
                tags.GetArrayElementAtIndex(0).stringValue = "shelf-small";
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return fixture;
        }

        private ProductItem CreateProduct(string name, ProductDefinition definition)
        {
            GameObject gameObject = new(name);
            createdObjects.Add(gameObject);
            ProductItem product = gameObject.AddComponent<ProductItem>();
            SerializedObject serialized = new(product);
            serialized.FindProperty("definition").objectReferenceValue = definition;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return product;
        }

        private PlacementSaveController CreateSaveController(
            ProductDefinition definition,
            ShelfFixture fixture,
            params ProductItem[] products)
        {
            GameObject gameObject = new("Save Controller");
            createdObjects.Add(gameObject);
            PlacementSaveController controller = gameObject.AddComponent<PlacementSaveController>();
            SerializedObject serialized = new(controller);
            SetObjectArray(serialized.FindProperty("productDefinitions"), definition);
            SetObjectArray(serialized.FindProperty("fixtures"), fixture);
            SetObjectArray(serialized.FindProperty("sceneProducts"), products);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        private static void SetObjectArray(SerializedProperty property, params Object[] values)
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }
    }
}
