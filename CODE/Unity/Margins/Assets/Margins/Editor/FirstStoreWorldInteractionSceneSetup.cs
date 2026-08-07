using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
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
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");

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
                chips,
                inputActions);

            DestroySceneObjectsNamed("Essential Checkout Fixture Placement Handle");
            DestroySceneObjectsNamed("Stockroom Delivery Drop");
            GameObject deliveryDropObject = new("Stockroom Delivery Drop");
            deliveryDropObject.transform.SetPositionAndRotation(
                new Vector3(-0.5f, 0f, 2.5f),
                Quaternion.identity);

            PlaceableFixtureComponent colaShelfFixture =
                GetOrAdd<PlaceableFixtureComponent>(colaShelfObject);
            PlaceableFixtureComponent chipsShelfFixture =
                GetOrAdd<PlaceableFixtureComponent>(chipsShelfObject);
            PlaceableFixtureComponent deliveryDropFixture =
                GetOrAdd<PlaceableFixtureComponent>(deliveryDropObject);
            ConfigurePlaceableFixture(
                requiredFixture,
                "fixture-checkout-essential-01",
                2,
                1);
            ConfigurePlaceableFixture(
                colaShelfFixture,
                colaShelf.StableFixtureId,
                3,
                1);
            ConfigurePlaceableFixture(
                chipsShelfFixture,
                chipsShelf.StableFixtureId,
                3,
                1);
            ConfigurePlaceableFixture(
                deliveryDropFixture,
                "fixture-delivery-drop-01",
                1,
                1);

            ConfigurePropertyGrid(
                fixtureControllerObject,
                fixturePlacement,
                placementFloorObject,
                requiredFixture,
                colaShelfFixture,
                chipsShelfFixture,
                deliveryDropFixture);
            ConfigureInitialFixturePlacement(
                fixturePlacement,
                requiredFixture,
                colaShelfFixture,
                chipsShelfFixture,
                deliveryDropFixture);

            SetBoolean(store, "continuousOperation", false);
            SetInteger(store, "includedOperatingExpensesCents", 9_000);
            SetBoolean(cleaning, "startsDirty", true);

            OwnedPropertyPlacementArea propertyArea =
                ConfigureOwnedPropertyArea(
                    fixtureControllerObject,
                    placementFloor,
                    playerObject.transform);

            FirstStoreFixturePlacementModeController placementMode =
                GetOrAdd<FirstStoreFixturePlacementModeController>(fixtureControllerObject);
            SetObject(placementMode, "stableTargetId", "target-fixture-placement-mode-01");
            SetObject(placementMode, "fixturePlacement", fixturePlacement);
            SetObject(placementMode, "placementFloor", placementFloor);
            SetObject(placementMode, "propertyArea", propertyArea);
            SetObject(interaction, "fixturePlacementMode", placementMode);
            SetObject(interaction, "inputActions", inputActions);

            FixturePlacementWorldInteractionTarget placedFixtureTarget =
                GetOrAdd<FixturePlacementWorldInteractionTarget>(requiredFixtureObject);
            SetObject(
                placedFixtureTarget,
                "stableTargetId",
                "target-fixture-checkout-placed-01");
            SetObject(placedFixtureTarget, "placementMode", placementMode);
            SetObject(placedFixtureTarget, "fixture", requiredFixture);
            SetBoolean(placedFixtureTarget, "allowsUnplacedFixture", false);

            ConfigureFixtureTarget(
                colaShelfObject,
                "target-fixture-shelf-cola-01",
                placementMode,
                colaShelfFixture);
            ConfigureFixtureTarget(
                chipsShelfObject,
                "target-fixture-shelf-chips-01",
                placementMode,
                chipsShelfFixture);
            ConfigureFixtureTarget(
                deliveryDropObject,
                "target-fixture-delivery-drop-01",
                placementMode,
                deliveryDropFixture);

            ConfigureDelivery(
                deliveryObject,
                delivery,
                stocking,
                playerObject.transform,
                playerObject.GetComponentInChildren<Camera>().transform,
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
                "CHECKOUT");
            StagedCheckoutInteractionComponent staged =
                GetOrAdd<StagedCheckoutInteractionComponent>(checkoutObject);
            ConfigureStagedBaskets(staged, checkout, cola, chips);
            StagedCheckoutWorldInteractionTarget checkoutTarget =
                GetOrAdd<StagedCheckoutWorldInteractionTarget>(checkoutTargetObject);
            SetObject(checkoutTarget, "stableTargetId", "target-checkout-staged-01");
            SetObject(checkoutTarget, "stagedCheckout", staged);
            SetObject(checkoutTarget, "operatingController", store);
            SetObject(checkoutTarget, "fixturePlacement", fixturePlacement);
            SetObject(checkoutTarget, "requiredFixture", requiredFixture);

            FirstStoreDiskPersistenceController diskPersistence =
                UnityEngine.Object.FindAnyObjectByType<FirstStoreDiskPersistenceController>();
            if (diskPersistence != null)
            {
                SetObject(
                    diskPersistence,
                    "persistenceMapper",
                    UnityEngine.Object.FindAnyObjectByType<FirstStorePersistenceMapperComponent>());
                SetObject(
                    diskPersistence,
                    "firstPersonController",
                    playerObject.GetComponent<FirstPersonController>());
                SetObject(diskPersistence, "interactionController", interaction);
                SetObject(diskPersistence, "stagedCheckout", staged);
                SetObject(diskPersistence, "stagedCheckoutWorldTarget", checkoutTarget);
            }

            GamePauseMenuController gameMenu =
                GetOrAdd<GamePauseMenuController>(controlsObject);
            SetObject(
                gameMenu,
                "firstPersonController",
                playerObject.GetComponent<FirstPersonController>());
            SetObject(gameMenu, "persistence", diskPersistence);

            Transform toolHoldPoint = CreateCarryPoint(
                playerObject.GetComponentInChildren<Camera>().transform,
                "Tool Carry Point",
                new Vector3(0.42f, -0.62f, 1.05f));
            PlayerCarryableToolController toolCarrier =
                GetOrAdd<PlayerCarryableToolController>(playerObject);
            SetObject(toolCarrier, "holdPoint", toolHoldPoint);
            SetObject(toolCarrier, "playerBody", playerObject.transform);
            SetObject(toolCarrier, "stocking", stocking);
            SetObject(interaction, "toolCarrier", toolCarrier);

            PortfolioProgressionController portfolio =
                UnityEngine.Object.FindAnyObjectByType<PortfolioProgressionController>() ??
                throw new InvalidOperationException(
                    "The first-store scene requires portfolio progression.");
            InStoreEmployeeWorkController employeeWork = ConfigureInStoreEmployees(
                controlsObject,
                portfolio,
                store,
                delivery,
                stocking,
                cleaning,
                cola,
                chips,
                requiredFixtureObject.transform,
                deliveryDropObject.transform,
                colaShelfObject.transform,
                validMaterial,
                fixtureMaterial,
                chipsMaterial);
            FirstStorePersistenceMapperComponent persistenceMapper =
                UnityEngine.Object.FindAnyObjectByType<FirstStorePersistenceMapperComponent>();
            if (persistenceMapper != null)
            {
                SetObject(persistenceMapper, "employeeWork", employeeWork);
            }

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
            SetObject(cleaningTarget, "toolCarrier", toolCarrier);
            SetObject(cleaningTarget, "requiredToolCapabilityId", "clean-floor");

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
            SetObject(presenter, "fixturePlacementMode", placementMode);
            SetArray(presenter, "requiredFixtures", requiredFixture);
            SetObject(presenter, "delivery", delivery);
            SetObject(presenter, "stocking", stocking);
            SetObject(presenter, "toolCarrier", toolCarrier);
            SetObject(presenter, "checkout", checkout);
            SetObject(presenter, "stagedCheckout", staged);
            SetObject(presenter, "cleaning", cleaning);
            SetObject(presenter, "store", store);
            SetObject(presenter, "portfolio", portfolio);
            SetObject(presenter, "persistence", diskPersistence);
            SetObject(presenter, "colaProduct", cola);
            SetObject(presenter, "chipsProduct", chips);

            FirstStoreExperienceSceneSetup.Apply(scene);

            ConfigureCarryableMop(
                Require("Mop Tool"),
                toolCarrier);
            ConfigureOwnedPropertyObstacles(
                propertyArea,
                Require("First Store Presentation").transform,
                requiredFixtureObject.transform,
                colaShelfObject.transform,
                chipsShelfObject.transform,
                deliveryDropObject.transform,
                Require("Mop Tool").transform);

            ConfigureCheckoutProductTarget(
                requiredFixtureObject.transform.Find("Experience Checkout Cola Prop")?.gameObject,
                "target-checkout-item-cola-01",
                cola,
                staged,
                store);
            ConfigureCheckoutProductTarget(
                requiredFixtureObject.transform.Find("Experience Checkout Chips Prop")?.gameObject,
                "target-checkout-item-chips-01",
                chips,
                staged,
                store);

            FirstStoreCustomerSceneSetup.Configure(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Configured explicit first-store world interactions and prompt presentation.");
        }

        private static void ConfigureDelivery(
            GameObject deliveryObject,
            DeliveryBoxComponent delivery,
            StockingController stocking,
            Transform playerBody,
            Transform cameraTransform,
            ProductDefinition cola,
            ProductDefinition chips,
            Material colaMaterial,
            Material chipsMaterial)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(deliveryObject);
            DeliveryOpenWorldInteractionTarget oldOpenTarget =
                deliveryObject.GetComponent<DeliveryOpenWorldInteractionTarget>();
            if (oldOpenTarget != null)
            {
                UnityEngine.Object.DestroyImmediate(oldOpenTarget);
            }

            Transform existingCarryPoint = cameraTransform.Find("Delivery Carry Point");
            GameObject carryPointObject = existingCarryPoint == null
                ? new GameObject("Delivery Carry Point")
                : existingCarryPoint.gameObject;
            carryPointObject.transform.SetParent(cameraTransform, false);
            carryPointObject.transform.localPosition = new Vector3(0f, -0.5f, 1.5f);
            carryPointObject.transform.localRotation = Quaternion.identity;

            DeliveryBoxWorldInteractionTarget boxTarget =
                GetOrAdd<DeliveryBoxWorldInteractionTarget>(deliveryObject);
            SetObject(boxTarget, "stableTargetId", "target-delivery-box-01");
            SetObject(boxTarget, "deliveryBox", delivery);
            SetObject(boxTarget, "stocking", stocking);
            SetObject(boxTarget, "carryPoint", carryPointObject.transform);
            SetObject(boxTarget, "playerBody", playerBody);

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
            SetObject(colaTarget, "stocking", stocking);
            SetBoolean(colaTarget, "autoHoldOnTake", true);

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
            SetObject(chipsTarget, "stocking", stocking);
            SetBoolean(chipsTarget, "autoHoldOnTake", true);
        }

        private static void ConfigureShelfTargets(
            ShelfFixture shelf,
            StockingController stocking,
            Material material)
        {
            foreach (ShelfSnapWorldInteractionTarget target in
                     shelf.GetComponentsInChildren<ShelfSnapWorldInteractionTarget>(true))
            {
                Collider targetCollider = target.GetComponent<Collider>();
                if (targetCollider != null)
                {
                    targetCollider.enabled = false;
                }
                Renderer targetRenderer = target.GetComponent<Renderer>();
                if (targetRenderer != null)
                {
                    targetRenderer.enabled = false;
                }
            }

            BoxCollider fixtureCollider = shelf.GetComponent<BoxCollider>() ??
                                          shelf.gameObject.AddComponent<BoxCollider>();
            fixtureCollider.center = new Vector3(0f, 0.95f, 0f);
            fixtureCollider.size = new Vector3(2.4f, 1.95f, 0.95f);
            fixtureCollider.enabled = true;

            ShelfFixtureWorldInteractionTarget fixtureTarget =
                GetOrAdd<ShelfFixtureWorldInteractionTarget>(shelf.gameObject);
            SetObject(
                fixtureTarget,
                "stableTargetId",
                $"target-stock-fixture-{shelf.StableFixtureId}");
            SetObject(fixtureTarget, "stocking", stocking);
            SetObject(fixtureTarget, "shelfFixture", shelf);
        }

        private static void ConfigureCheckoutProductTarget(
            GameObject targetObject,
            string stableTargetId,
            ProductDefinition product,
            StagedCheckoutInteractionComponent staged,
            StoreOperatingController store)
        {
            if (targetObject == null)
            {
                throw new InvalidOperationException(
                    $"Checkout product target '{stableTargetId}' is missing.");
            }

            Collider collider = targetObject.GetComponent<Collider>() ??
                                targetObject.AddComponent<BoxCollider>();
            collider.enabled = true;
            CheckoutProductWorldInteractionTarget target =
                GetOrAdd<CheckoutProductWorldInteractionTarget>(targetObject);
            SetObject(target, "stableTargetId", stableTargetId);
            SetObject(target, "productDefinition", product);
            SetObject(target, "stagedCheckout", staged);
            SetObject(target, "operatingController", store);
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
            baskets.arraySize = 3;
            ConfigureBasket(
                baskets.GetArrayElementAtIndex(0),
                "staged-transaction-001",
                (cola, 1),
                (chips, 1));
            ConfigureBasket(
                baskets.GetArrayElementAtIndex(1),
                "staged-transaction-002",
                (cola, 1));
            ConfigureBasket(
                baskets.GetArrayElementAtIndex(2),
                "staged-transaction-003",
                (chips, 1));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(staged);
        }

        private static InStoreEmployeeWorkController ConfigureInStoreEmployees(
            GameObject controlsObject,
            PortfolioProgressionController portfolio,
            StoreOperatingController store,
            DeliveryBoxComponent delivery,
            StockingController stocking,
            CleaningTaskComponent cleaning,
            ProductDefinition cola,
            ProductDefinition chips,
            Transform checkoutFixture,
            Transform deliveryDropFixture,
            Transform fallbackShelfFixture,
            Material cashierMaterial,
            Material stockerMaterial,
            Material managerMaterial)
        {
            Transform cashierAvatar = CreateEmployeeAvatar(
                "Detailed Cashier Employee",
                new Vector3(4.2f, 0.9f, -1.1f),
                cashierMaterial,
                out TextMesh cashierLabel);
            Transform stockerAvatar = CreateEmployeeAvatar(
                "Detailed Stock Employee",
                new Vector3(-3.45f, 0.9f, 4.62f),
                stockerMaterial,
                out TextMesh stockerLabel);
            Transform managerAvatar = CreateEmployeeAvatar(
                "Detailed Manager Employee",
                new Vector3(-3.8f, 0.9f, -1.3f),
                managerMaterial,
                out TextMesh managerLabel);

            Transform cashierWork = CreateAttachedWorkPoint(
                checkoutFixture,
                "Cashier Work Point",
                new Vector3(-0.15f, 0.9f, -1.55f));
            Transform deliveryWork = CreateWorkPoint(
                "Receiving Work Point",
                new Vector3(-3.45f, 0.9f, 4.62f));
            Transform deliveryDrop = CreateAttachedWorkPoint(
                deliveryDropFixture,
                "Stockroom Delivery Setdown Point",
                new Vector3(0f, 0.48f, 0f));
            Transform shelfWork = CreateAttachedWorkPoint(
                fallbackShelfFixture,
                "Shelf Stocking Work Point",
                new Vector3(0f, 0.9f, -1f));
            Transform managerWork = CreateWorkPoint(
                "Manager Work Point",
                new Vector3(-2.6f, 0.9f, -2.25f));
            Transform boxCarry = CreateCarryPoint(
                stockerAvatar,
                "Employee Box Carry Point",
                new Vector3(0f, -0.3f, 0.95f));
            Transform unitCarry = CreateCarryPoint(
                stockerAvatar,
                "Employee Product Carry Point",
                new Vector3(0.38f, 0.08f, 0.58f));

            InStoreEmployeeWorkController controller =
                GetOrAdd<InStoreEmployeeWorkController>(controlsObject);
            SetObject(controller, "portfolio", portfolio);
            SetObject(controller, "store", store);
            SetObject(controller, "deliveryBox", delivery);
            SetObject(controller, "stocking", stocking);
            SetObject(controller, "cleaning", cleaning);
            SetArray(controller, "products", cola, chips);
            SetObject(controller, "cashierAvatar", cashierAvatar);
            SetObject(controller, "stockerAvatar", stockerAvatar);
            SetObject(controller, "managerAvatar", managerAvatar);
            SetObject(controller, "cashierLabel", cashierLabel);
            SetObject(controller, "stockerLabel", stockerLabel);
            SetObject(controller, "managerLabel", managerLabel);
            SetObject(controller, "cashierWorkPoint", cashierWork);
            SetObject(controller, "deliveryWorkPoint", deliveryWork);
            SetObject(controller, "deliveryDropPoint", deliveryDrop);
            SetObject(controller, "shelfWorkPoint", shelfWork);
            SetObject(controller, "managerWorkPoint", managerWork);
            SetObject(controller, "stockerBoxCarryPoint", boxCarry);
            SetObject(controller, "stockerUnitCarryPoint", unitCarry);
            return controller;
        }

        private static void ConfigurePlaceableFixture(
            PlaceableFixtureComponent fixture,
            string stableFixtureInstanceId,
            int footprintWidth,
            int footprintDepth)
        {
            SetObject(fixture, "stableFixtureInstanceId", stableFixtureInstanceId);
            SetInteger(fixture, "footprintWidthCells", footprintWidth);
            SetInteger(fixture, "footprintDepthCells", footprintDepth);
        }

        private static void ConfigureFixtureTarget(
            GameObject targetObject,
            string stableTargetId,
            FirstStoreFixturePlacementModeController placementMode,
            PlaceableFixtureComponent fixture)
        {
            FixturePlacementWorldInteractionTarget target =
                GetOrAdd<FixturePlacementWorldInteractionTarget>(targetObject);
            SetObject(target, "stableTargetId", stableTargetId);
            SetObject(target, "placementMode", placementMode);
            SetObject(target, "fixture", fixture);
            SetBoolean(target, "allowsUnplacedFixture", false);
        }

        private static void ConfigurePropertyGrid(
            GameObject fixtureControllerObject,
            FixturePlacementController fixturePlacement,
            GameObject placementFloorObject,
            params PlaceableFixtureComponent[] fixtures)
        {
            Transform origin = fixtureControllerObject.transform.Find(
                "Owned Property Grid Origin");
            if (origin == null)
            {
                origin = new GameObject("Owned Property Grid Origin").transform;
                origin.SetParent(fixtureControllerObject.transform, false);
            }
            origin.SetPositionAndRotation(
                new Vector3(-12f, 0f, -21f),
                Quaternion.identity);

            placementFloorObject.transform.SetPositionAndRotation(
                new Vector3(0f, -0.05f, -7.5f),
                Quaternion.identity);
            placementFloorObject.transform.localScale =
                new Vector3(24f, 0.1f, 27f);

            SerializedObject serialized = new(fixturePlacement);
            serialized.FindProperty("gridOrigin").objectReferenceValue = origin;
            serialized.FindProperty("gridWidthCells").intValue = 24;
            serialized.FindProperty("gridDepthCells").intValue = 27;
            serialized.FindProperty("cellSize").floatValue = 1f;
            SerializedProperty fixtureReferences = serialized.FindProperty("fixtures");
            fixtureReferences.arraySize = fixtures.Length;
            for (int index = 0; index < fixtures.Length; index++)
            {
                fixtureReferences.GetArrayElementAtIndex(index).objectReferenceValue =
                    fixtures[index];
            }
            serialized.FindProperty("legacyGridWidthCells").intValue = 8;
            serialized.FindProperty("legacyGridDepthCells").intValue = 6;
            SerializedProperty legacyOffset = serialized.FindProperty("legacyGridOffset");
            legacyOffset.FindPropertyRelative("x").intValue = 8;
            legacyOffset.FindPropertyRelative("z").intValue = 19;
            SerializedProperty legacyIds = serialized.FindProperty(
                "legacyFixtureInstanceIds");
            legacyIds.arraySize = 1;
            legacyIds.GetArrayElementAtIndex(0).stringValue =
                "fixture-checkout-essential-01";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(fixturePlacement);
        }

        private static OwnedPropertyPlacementArea ConfigureOwnedPropertyArea(
            GameObject fixtureControllerObject,
            Collider placementSurface,
            Transform player)
        {
            DestroySceneObjectsNamed("Owned Property Bounds");
            GameObject boundsObject = new("Owned Property Bounds");
            boundsObject.transform.SetParent(fixtureControllerObject.transform, false);
            boundsObject.transform.SetPositionAndRotation(
                new Vector3(0f, 1.75f, -7.5f),
                Quaternion.identity);
            BoxCollider bounds = boundsObject.AddComponent<BoxCollider>();
            bounds.size = new Vector3(24f, 3.5f, 27f);
            bounds.isTrigger = true;

            OwnedPropertyPlacementArea area =
                GetOrAdd<OwnedPropertyPlacementArea>(fixtureControllerObject);
            SetObject(area, "ownedPropertyBounds", bounds);
            SetObject(area, "placementSurface", placementSurface);
            SetObject(area, "player", player);
            SetArray<Collider>(area, "structuralObstacles");
            return area;
        }

        private static void ConfigureOwnedPropertyObstacles(
            OwnedPropertyPlacementArea propertyArea,
            Transform presentationRoot,
            params Transform[] movableRoots)
        {
            Collider[] obstacles = presentationRoot
                .GetComponentsInChildren<Collider>(true)
                .Where(collider =>
                    collider != null &&
                    collider.enabled &&
                    !collider.isTrigger &&
                    !string.Equals(collider.name, "Parking Lot", StringComparison.Ordinal) &&
                    !string.Equals(collider.name, "Front Sidewalk", StringComparison.Ordinal) &&
                    !movableRoots.Any(root =>
                        root != null && collider.transform.IsChildOf(root)))
                .Distinct()
                .ToArray();
            SetArray(propertyArea, "structuralObstacles", obstacles);
        }

        private static void ConfigureCarryableMop(
            GameObject mopObject,
            PlayerCarryableToolController carrier)
        {
            CarryableToolComponent mop = GetOrAdd<CarryableToolComponent>(mopObject);
            SerializedObject serialized = new(mop);
            serialized.FindProperty("stableToolId").stringValue = "tool-mop-01";
            serialized.FindProperty("capabilityId").stringValue = "clean-floor";
            serialized.FindProperty("displayName").stringValue = "mop";
            serialized.FindProperty("carrier").objectReferenceValue = carrier;
            serialized.FindProperty("carriedLocalPosition").vector3Value =
                new Vector3(0.15f, -0.15f, 0.2f);
            serialized.FindProperty("carriedLocalEulerAngles").vector3Value =
                new Vector3(12f, 0f, -18f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mop);
        }

        private static void ConfigureInitialFixturePlacement(
            FixturePlacementController fixturePlacement,
            PlaceableFixtureComponent checkoutFixture,
            PlaceableFixtureComponent colaShelfFixture,
            PlaceableFixtureComponent chipsShelfFixture,
            PlaceableFixtureComponent deliveryDropFixture)
        {
            SerializedObject serialized = new(fixturePlacement);
            SerializedProperty placements = serialized.FindProperty("initialPlacements") ??
                throw new InvalidOperationException(
                    "Fixture placement initial-placement configuration is missing.");
            placements.arraySize = 4;
            ConfigureInitialPlacement(
                placements.GetArrayElementAtIndex(0),
                checkoutFixture,
                14,
                20,
                0);
            ConfigureInitialPlacement(
                placements.GetArrayElementAtIndex(1),
                colaShelfFixture,
                8,
                22,
                0);
            ConfigureInitialPlacement(
                placements.GetArrayElementAtIndex(2),
                chipsShelfFixture,
                13,
                22,
                0);
            ConfigureInitialPlacement(
                placements.GetArrayElementAtIndex(3),
                deliveryDropFixture,
                11,
                23,
                0);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(fixturePlacement);
        }

        private static void ConfigureInitialPlacement(
            SerializedProperty placement,
            PlaceableFixtureComponent fixture,
            int x,
            int z,
            int quarterTurns)
        {
            placement.FindPropertyRelative("fixture").objectReferenceValue = fixture;
            SerializedProperty grid = placement.FindPropertyRelative("gridPosition");
            grid.FindPropertyRelative("x").intValue = x;
            grid.FindPropertyRelative("z").intValue = z;
            placement.FindPropertyRelative("quarterTurns").intValue = quarterTurns;
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

        private static Transform CreateEmployeeAvatar(
            string objectName,
            Vector3 position,
            Material material,
            out TextMesh label)
        {
            DestroySceneObjectsNamed(objectName);
            GameObject root = new(objectName);
            root.transform.SetPositionAndRotation(position, Quaternion.identity);

            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = $"{objectName} Torso";
            torso.transform.SetParent(root.transform, false);
            torso.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            torso.transform.localScale = new Vector3(0.43f, 0.62f, 0.34f);
            Collider torsoCollider = torso.GetComponent<Collider>();
            if (torsoCollider != null)
            {
                UnityEngine.Object.DestroyImmediate(torsoCollider);
            }
            ApplyMaterial(torso, material);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = $"{objectName} Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.92f, 0f);
            head.transform.localScale = Vector3.one * 0.38f;
            Collider headCollider = head.GetComponent<Collider>();
            if (headCollider != null)
            {
                UnityEngine.Object.DestroyImmediate(headCollider);
            }
            ApplyMaterial(head, material);

            for (int index = 0; index < 2; index++)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = $"{objectName} Leg {index + 1}";
                leg.transform.SetParent(root.transform, false);
                leg.transform.localPosition = new Vector3(
                    index == 0 ? -0.17f : 0.17f,
                    -0.72f,
                    0f);
                leg.transform.localScale = new Vector3(0.2f, 0.58f, 0.24f);
                Collider legCollider = leg.GetComponent<Collider>();
                if (legCollider != null)
                {
                    UnityEngine.Object.DestroyImmediate(legCollider);
                }
                ApplyMaterial(leg, material);
            }

            GameObject badge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            badge.name = $"{objectName} Role Badge";
            badge.transform.SetParent(root.transform, false);
            badge.transform.localPosition = new Vector3(0f, 0.3f, -0.35f);
            badge.transform.localScale = new Vector3(0.32f, 0.17f, 0.035f);
            Collider badgeCollider = badge.GetComponent<Collider>();
            if (badgeCollider != null)
            {
                UnityEngine.Object.DestroyImmediate(badgeCollider);
            }
            ApplyMaterial(badge, material);

            GameObject labelObject = new($"{objectName} Label");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.3f, -0.375f);
            labelObject.transform.localRotation = Quaternion.identity;
            labelObject.transform.localScale = Vector3.one * 0.035f;
            label = labelObject.AddComponent<TextMesh>();
            label.text = string.Empty;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.22f;
            label.fontSize = 48;
            label.color = Color.white;

            root.SetActive(false);
            return root.transform;
        }

        private static Transform CreateWorkPoint(string objectName, Vector3 position)
        {
            DestroySceneObjectsNamed(objectName);
            GameObject point = new(objectName);
            point.transform.SetPositionAndRotation(position, Quaternion.identity);
            return point.transform;
        }

        private static Transform CreateAttachedWorkPoint(
            Transform parent,
            string objectName,
            Vector3 localPosition)
        {
            DestroySceneObjectsNamed(objectName);
            GameObject point = new(objectName);
            point.transform.SetParent(parent, false);
            point.transform.localPosition = localPosition;
            point.transform.localRotation = Quaternion.identity;
            return point.transform;
        }

        private static Transform CreateCarryPoint(
            Transform parent,
            string objectName,
            Vector3 localPosition)
        {
            Transform existing = parent.Find(objectName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
            GameObject point = new(objectName);
            point.transform.SetParent(parent, false);
            point.transform.localPosition = localPosition;
            point.transform.localRotation = Quaternion.identity;
            return point.transform;
        }

        private static void DestroySceneObjectsNamed(string objectName)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            foreach (GameObject existing in Resources
                         .FindObjectsOfTypeAll<GameObject>()
                         .Where(candidate =>
                             candidate.scene == activeScene &&
                             candidate.name == objectName)
                         .ToArray())
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static GameObject CreateWorldShape(
            string objectName,
            PrimitiveType primitive,
            Vector3 position,
            Vector3 scale,
            Material material,
            string label)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] existingObjects = Resources
                .FindObjectsOfTypeAll<GameObject>()
                .Where(candidate =>
                    candidate.scene == activeScene &&
                    candidate.name == objectName)
                .ToArray();
            foreach (GameObject existing in existingObjects)
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

        private static void SetInteger(
            UnityEngine.Object target,
            string propertyName,
            int value)
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                throw new InvalidOperationException(
                    $"Serialized property '{propertyName}' is missing on '{target.name}'.");
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
