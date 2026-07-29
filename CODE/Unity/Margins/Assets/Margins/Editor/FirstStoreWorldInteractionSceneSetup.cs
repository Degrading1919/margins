using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Margins.Editor
{
    public static class FirstStoreWorldInteractionSceneSetup
    {
        private const string ScenePath =
            "Assets/Margins/Scenes/FirstStoreValidation.unity";

        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject playerObject = Require("Validation Player");
            GameObject controlsObject = Require("First-Store Validation Controls");
            GameObject deliveryObject = Require("Mixed Starter Delivery");
            GameObject checkoutObject = Require("Checkout Station");
            GameObject cleaningObject = Require("Cleaning Task");
            GameObject fixtureControllerObject = Require("Fixture Placement");
            GameObject requiredFixtureObject = Require("Essential Checkout Fixture");
            GameObject placementFloorObject = Require("Validation Floor");
            GameObject colaShelfObject = Require("fixture-shelf-cola-validation");
            GameObject chipsShelfObject = Require("fixture-shelf-chips-validation");
            GameObject stockingObject = Require("Stocking Controller");
            GameObject storeObject = Require("Store Operating Controller");

            DeliveryBoxComponent delivery = deliveryObject.GetComponent<DeliveryBoxComponent>();
            CheckoutStationComponent checkout = checkoutObject.GetComponent<CheckoutStationComponent>();
            CleaningTaskComponent cleaning = cleaningObject.GetComponent<CleaningTaskComponent>();
            FixturePlacementController fixturePlacement =
                fixtureControllerObject.GetComponent<FixturePlacementController>();
            PlaceableFixtureComponent requiredFixture =
                requiredFixtureObject.GetComponent<PlaceableFixtureComponent>();
            ShelfFixture colaShelf = colaShelfObject.GetComponent<ShelfFixture>();
            ShelfFixture chipsShelf = chipsShelfObject.GetComponent<ShelfFixture>();
            StockingController stocking = stockingObject.GetComponent<StockingController>();
            StoreOperatingController store = storeObject.GetComponent<StoreOperatingController>();
            FirstStoreInteractionController interaction =
                playerObject.GetComponent<FirstStoreInteractionController>();
            Collider placementFloor = placementFloorObject.GetComponent<Collider>();

            ProductDefinition cola = AssetDatabase.LoadAssetAtPath<ProductDefinition>(
                "Assets/Margins/Content/FirstStoreValidation/ValidationColaProduct.asset");
            ProductDefinition chips = AssetDatabase.LoadAssetAtPath<ProductDefinition>(
                "Assets/Margins/Content/FirstStoreValidation/ValidationChipsProduct.asset");
            Material colaMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Margins/Content/FirstStoreValidation/ValidationCola.mat");
            Material chipsMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Margins/Content/FirstStoreValidation/ValidationChips.mat");
            Material fixtureMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Margins/Content/FirstStoreValidation/ValidationFixture.mat");
            Material validMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Margins/Content/FirstStoreValidation/ValidationValid.mat");

            RequireReferences(
                delivery,
                checkout,
                cleaning,
                fixturePlacement,
                requiredFixture,
                colaShelf,
                chipsShelf,
                stocking,
                store,
                interaction,
                placementFloor,
                cola,
                chips);

            FirstStoreFixturePlacementModeController placementMode =
                GetOrAdd<FirstStoreFixturePlacementModeController>(fixtureControllerObject);
            SetObject(placementMode, "stableTargetId", "target-fixture-placement-mode-01");
            SetObject(placementMode, "fixturePlacement", fixturePlacement);
            SetObject(placementMode, "placementFloor", placementFloor);
            SetObject(interaction, "fixturePlacementMode", placementMode);

            FixturePlacementWorldInteractionTarget placedFixtureTarget =
                GetOrAdd<FixturePlacementWorldInteractionTarget>(requiredFixtureObject);
            SetObject(
                placedFixtureTarget,
                "stableTargetId",
                "target-fixture-checkout-placed-01");
            SetObject(placedFixtureTarget, "placementMode", placementMode);
            SetObject(placedFixtureTarget, "fixture", requiredFixture);
            SetBoolean(placedFixtureTarget, "allowsUnplacedFixture", false);

            GameObject fixtureHandleObject = CreateWorldShape(
                "Essential Checkout Fixture Placement Handle",
                PrimitiveType.Cube,
                new Vector3(0f, 0.2f, -0.25f),
                new Vector3(0.8f, 0.4f, 0.8f),
                validMaterial,
                "FIXTURE HANDLE");
            FixturePlacementWorldInteractionTarget fixtureHandleTarget =
                GetOrAdd<FixturePlacementWorldInteractionTarget>(fixtureHandleObject);
            SetObject(
                fixtureHandleTarget,
                "stableTargetId",
                "target-fixture-checkout-handle-01");
            SetObject(fixtureHandleTarget, "placementMode", placementMode);
            SetObject(fixtureHandleTarget, "fixture", requiredFixture);
            SetBoolean(fixtureHandleTarget, "allowsUnplacedFixture", true);

            ConfigureDelivery(
                deliveryObject,
                delivery,
                cola,
                chips,
                colaMaterial,
                chipsMaterial);
            ConfigureShelfTargets(colaShelf, stocking, validMaterial);
            ConfigureShelfTargets(chipsShelf, stocking, validMaterial);

            GameObject checkoutTargetObject = CreateWorldShape(
                "World Checkout Interaction",
                PrimitiveType.Cube,
                new Vector3(2.1f, 0.75f, -2.1f),
                new Vector3(1.4f, 1.2f, 0.7f),
                fixtureMaterial,
                "STAGED CHECKOUT");
            StagedCheckoutInteractionComponent staged =
                GetOrAdd<StagedCheckoutInteractionComponent>(checkoutObject);
            ConfigureStagedBaskets(staged, checkout, cola, chips);
            StagedCheckoutWorldInteractionTarget checkoutTarget =
                GetOrAdd<StagedCheckoutWorldInteractionTarget>(checkoutTargetObject);
            SetObject(checkoutTarget, "stableTargetId", "target-checkout-staged-01");
            SetObject(checkoutTarget, "stagedCheckout", staged);
            SetObject(checkoutTarget, "operatingController", store);

            GameObject cleaningTargetObject = CreateWorldShape(
                "World Cleaning Interaction",
                PrimitiveType.Cylinder,
                new Vector3(-1.7f, 0.04f, 0.8f),
                new Vector3(1.15f, 0.04f, 1.15f),
                chipsMaterial,
                "FLOOR SPILL");
            CleaningWorldInteractionTarget cleaningTarget =
                GetOrAdd<CleaningWorldInteractionTarget>(cleaningTargetObject);
            SetObject(cleaningTarget, "stableTargetId", "target-cleaning-spill-01");
            SetObject(cleaningTarget, "cleaningTask", cleaning);

            GameObject operatingTargetObject = CreateWorldShape(
                "World Store Operating Control",
                PrimitiveType.Cube,
                new Vector3(-2.4f, 1.1f, -2.5f),
                new Vector3(0.55f, 1.1f, 0.25f),
                validMaterial,
                "STORE CONTROL");
            StoreOperatingWorldInteractionTarget operatingTarget =
                GetOrAdd<StoreOperatingWorldInteractionTarget>(operatingTargetObject);
            SetObject(operatingTarget, "stableTargetId", "target-store-control-01");
            SetObject(operatingTarget, "operatingController", store);

            FirstStorePromptPresenter presenter =
                GetOrAdd<FirstStorePromptPresenter>(controlsObject);
            SetObject(presenter, "interaction", interaction);
            SetObject(presenter, "fixturePlacement", fixturePlacement);
            SetArray(presenter, "requiredFixtures", requiredFixture);
            SetObject(presenter, "delivery", delivery);
            SetObject(presenter, "checkout", checkout);
            SetObject(presenter, "stagedCheckout", staged);
            SetObject(presenter, "cleaning", cleaning);
            SetObject(presenter, "store", store);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Configured explicit first-store world interactions and prompt presentation.");
        }

        private static void ConfigureDelivery(
            GameObject deliveryObject,
            DeliveryBoxComponent delivery,
            ProductDefinition cola,
            ProductDefinition chips,
            Material colaMaterial,
            Material chipsMaterial)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(deliveryObject);
            DeliveryOpenWorldInteractionTarget openTarget =
                GetOrAdd<DeliveryOpenWorldInteractionTarget>(deliveryObject);
            SetObject(openTarget, "stableTargetId", "target-delivery-open-01");
            SetObject(openTarget, "deliveryBox", delivery);

            GameObject colaTargetObject = CreateChildShape(
                deliveryObject.transform,
                "Delivery Content Cola Target",
                new Vector3(-0.32f, 0.45f, -0.58f),
                colaMaterial,
                "COLA");
            DeliveryProductWorldInteractionTarget colaTarget =
                GetOrAdd<DeliveryProductWorldInteractionTarget>(colaTargetObject);
            SetObject(colaTarget, "stableTargetId", "target-delivery-cola-01");
            SetObject(colaTarget, "deliveryBox", delivery);
            SetObject(colaTarget, "productDefinition", cola);

            GameObject chipsTargetObject = CreateChildShape(
                deliveryObject.transform,
                "Delivery Content Chips Target",
                new Vector3(0.32f, 0.45f, -0.58f),
                chipsMaterial,
                "CHIPS");
            DeliveryProductWorldInteractionTarget chipsTarget =
                GetOrAdd<DeliveryProductWorldInteractionTarget>(chipsTargetObject);
            SetObject(chipsTarget, "stableTargetId", "target-delivery-chips-01");
            SetObject(chipsTarget, "deliveryBox", delivery);
            SetObject(chipsTarget, "productDefinition", chips);
        }

        private static void ConfigureShelfTargets(
            ShelfFixture shelf,
            StockingController stocking,
            Material material)
        {
            foreach (ShelfSnapPointDefinition snapPoint in shelf.SnapPoints)
            {
                string objectName = $"Stock Target {snapPoint.StableSnapPointId}";
                Transform existing = shelf.transform.Find(objectName);
                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }

                GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                targetObject.name = objectName;
                targetObject.transform.SetParent(shelf.transform, false);
                targetObject.transform.localPosition =
                    snapPoint.LocalPosition + new Vector3(0f, 0f, -0.22f);
                targetObject.transform.localRotation =
                    Quaternion.Euler(snapPoint.LocalEulerAngles);
                targetObject.transform.localScale = new Vector3(0.32f, 0.24f, 0.18f);
                ApplyMaterial(targetObject, material);

                ShelfSnapWorldInteractionTarget target =
                    targetObject.AddComponent<ShelfSnapWorldInteractionTarget>();
                SetObject(
                    target,
                    "stableTargetId",
                    $"target-stock-{snapPoint.StableSnapPointId}");
                SetObject(target, "stocking", stocking);
                SetObject(target, "shelfFixture", shelf);
                SetObject(target, "snapPointId", snapPoint.StableSnapPointId);
            }
        }

        private static void ConfigureStagedBaskets(
            StagedCheckoutInteractionComponent staged,
            CheckoutStationComponent checkout,
            ProductDefinition cola,
            ProductDefinition chips)
        {
            SerializedObject serialized = new(staged);
            serialized.FindProperty("checkout").objectReferenceValue = checkout;
            SerializedProperty baskets = serialized.FindProperty("baskets");
            baskets.arraySize = 2;
            ConfigureBasket(
                baskets.GetArrayElementAtIndex(0),
                "staged-transaction-001",
                (cola, 1),
                (chips, 1));
            ConfigureBasket(
                baskets.GetArrayElementAtIndex(1),
                "staged-transaction-002",
                (cola, 1));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(staged);
        }

        private static void ConfigureBasket(
            SerializedProperty basket,
            string transactionId,
            params (ProductDefinition product, int quantity)[] lines)
        {
            basket.FindPropertyRelative("stableTransactionId").stringValue = transactionId;
            SerializedProperty products = basket.FindPropertyRelative("products");
            products.arraySize = lines.Length;
            for (int index = 0; index < lines.Length; index++)
            {
                SerializedProperty line = products.GetArrayElementAtIndex(index);
                line.FindPropertyRelative("productDefinition").objectReferenceValue =
                    lines[index].product;
                line.FindPropertyRelative("quantityUnits").intValue =
                    lines[index].quantity;
            }
        }

        private static GameObject CreateChildShape(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Material material,
            string label)
        {
            Transform existing = parent.Find(objectName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            result.name = objectName;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = localPosition;
            result.transform.localScale = new Vector3(0.42f, 0.3f, 0.18f);
            ApplyMaterial(result, material);
            AddLabel(result.transform, label, new Vector3(0f, 0.7f, 0f));
            return result;
        }

        private static GameObject CreateWorldShape(
            string objectName,
            PrimitiveType primitive,
            Vector3 position,
            Vector3 scale,
            Material material,
            string label)
        {
            GameObject existing = SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .FirstOrDefault(root => root.name == objectName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            GameObject result = GameObject.CreatePrimitive(primitive);
            result.name = objectName;
            result.transform.SetPositionAndRotation(position, Quaternion.identity);
            result.transform.localScale = scale;
            ApplyMaterial(result, material);
            AddLabel(result.transform, label, new Vector3(0f, 0.8f, 0f));
            return result;
        }

        private static void AddLabel(
            Transform parent,
            string text,
            Vector3 localPosition)
        {
            GameObject label = new($"{text} Label");
            label.transform.SetParent(parent, false);
            label.transform.localPosition = localPosition;
            label.transform.localRotation = Quaternion.identity;
            label.transform.localScale = Vector3.one * 0.12f;
            TextMesh textMesh = label.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.35f;
            textMesh.fontSize = 48;
            textMesh.color = Color.white;
        }

        private static void ApplyMaterial(GameObject gameObject, Material material)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static GameObject Require(string name)
        {
            GameObject result = GameObject.Find(name);
            if (result == null)
            {
                throw new InvalidOperationException($"Required scene object '{name}' is missing.");
            }
            return result;
        }

        private static void RequireReferences(params UnityEngine.Object[] references)
        {
            if (references.Any(reference => reference == null))
            {
                throw new InvalidOperationException(
                    "First-store world interaction setup is missing a required reference or asset.");
            }
        }

        private static void SetObject(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                throw new InvalidOperationException(
                    $"Serialized property '{propertyName}' is missing on '{target.name}'.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetObject(
            UnityEngine.Object target,
            string propertyName,
            string value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                throw new InvalidOperationException(
                    $"Serialized property '{propertyName}' is missing on '{target.name}'.");
            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetArray<T>(
            UnityEngine.Object target,
            string propertyName,
            params T[] values) where T : UnityEngine.Object
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                throw new InvalidOperationException(
                    $"Serialized property '{propertyName}' is missing on '{target.name}'.");
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetBoolean(
            UnityEngine.Object target,
            string propertyName,
            bool value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                throw new InvalidOperationException(
                    $"Serialized property '{propertyName}' is missing on '{target.name}'.");
            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
