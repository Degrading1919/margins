using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Margins.Editor
{
    public static class PortfolioProgressionSceneSetup
    {
        private const string ScenePath =
            "Assets/Margins/Scenes/FirstStoreValidation.unity";

        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            GameObject controls = Require("First-Store Validation Controls");
            FirstPersonController player = Require("Validation Player")
                .GetComponent<FirstPersonController>();
            StoreOperatingController store = Require("Store Operating Controller")
                .GetComponent<StoreOperatingController>();
            FirstStoreInventoryComponent inventory = Require("First-Store Inventory")
                .GetComponent<FirstStoreInventoryComponent>();
            DeliveryBoxComponent delivery = Require("Mixed Starter Delivery")
                .GetComponent<DeliveryBoxComponent>();
            FirstStoreDiskPersistenceController disk =
                UnityEngine.Object.FindAnyObjectByType<FirstStoreDiskPersistenceController>();
            FirstStoreValidationController validation =
                controls.GetComponent<FirstStoreValidationController>();
            FirstStorePersistenceMapperComponent persistenceMapper =
                UnityEngine.Object.FindAnyObjectByType<
                    FirstStorePersistenceMapperComponent>();
            PhysicalProductUnitRegistry physicalUnits =
                UnityEngine.Object.FindAnyObjectByType<
                    PhysicalProductUnitRegistry>();
            StockingController stocking =
                UnityEngine.Object.FindAnyObjectByType<StockingController>();
            FirstStoreMerchandisingComponent merchandising =
                UnityEngine.Object.FindAnyObjectByType<
                    FirstStoreMerchandisingComponent>();
            StagedCheckoutInteractionComponent stagedCheckout =
                UnityEngine.Object.FindAnyObjectByType<
                    StagedCheckoutInteractionComponent>();
            StoreCustomerFlowController customerFlow =
                UnityEngine.Object.FindAnyObjectByType<
                    StoreCustomerFlowController>();
            InStoreEmployeeWorkController employeeWork =
                UnityEngine.Object.FindAnyObjectByType<
                    InStoreEmployeeWorkController>();
            CleaningTaskComponent cleaning = store.CleaningTask;
            CleaningWorldInteractionTarget cleaningTarget =
                UnityEngine.Object.FindAnyObjectByType<
                    CleaningWorldInteractionTarget>();
            CarryableToolComponent cleaningTool = UnityEngine.Object
                .FindObjectsByType<CarryableToolComponent>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(value => string.Equals(
                    value.CapabilityId,
                    "clean-floor",
                    StringComparison.Ordinal));
            StoreOperatingWorldInteractionTarget operatingControl =
                UnityEngine.Object.FindAnyObjectByType<
                    StoreOperatingWorldInteractionTarget>();

            if (player == null || store == null || inventory == null ||
                delivery == null ||
                disk == null || validation == null ||
                persistenceMapper == null || physicalUnits == null ||
                stocking == null || merchandising == null ||
                stagedCheckout == null || customerFlow == null ||
                employeeWork == null || cleaning == null ||
                cleaningTarget == null || cleaningTool == null ||
                operatingControl == null)
            {
                throw new InvalidOperationException(
                    "Portfolio scene setup requires the existing player, store, disk, and validation components.");
            }

            PortfolioProgressionController portfolio =
                controls.GetComponent<PortfolioProgressionController>() ??
                controls.AddComponent<PortfolioProgressionController>();
            SetObject(portfolio, "firstPersonController", player);
            SetObject(portfolio, "firstStore", store);
            SetObject(portfolio, "firstStoreInventory", inventory);
            SetObject(portfolio, "firstStoreDeliveryBox", delivery);
            SetObject(disk, "portfolioProgression", portfolio);
            SetObject(validation, "portfolioProgression", portfolio);

            ProceduralAssetRegistry registry =
                AssetDatabase.LoadAssetAtPath<ProceduralAssetRegistry>(
                    "Assets/Margins/Content/Procedural/Resources/ProceduralAssetRegistry.asset");
            ProceduralBusinessRecipe recipe =
                AssetDatabase.LoadAssetAtPath<ProceduralBusinessRecipe>(
                    "Assets/Margins/Content/Procedural/Resources/Recipes/GrayboxConvenienceRecipe.asset");
            if (registry == null || recipe == null)
            {
                throw new InvalidOperationException(
                    "Persistent location travel requires the approved procedural registry and convenience recipe assets.");
            }

            GameObject travelObject = GameObject.Find(
                "Persistent Portfolio Location Travel");
            if (travelObject == null)
            {
                travelObject = new GameObject(
                    "Persistent Portfolio Location Travel");
            }
            travelObject.transform.SetPositionAndRotation(
                new Vector3(48f, 0f, 0f),
                Quaternion.identity);
            PersistentPortfolioLocationController materializer =
                travelObject.GetComponent<
                    PersistentPortfolioLocationController>() ??
                travelObject.AddComponent<
                    PersistentPortfolioLocationController>();
            materializer.Configure(portfolio, registry, recipe);
            PersistentPortfolioLocationSceneAdapter sceneAdapter =
                travelObject.GetComponent<
                    PersistentPortfolioLocationSceneAdapter>() ??
                travelObject.AddComponent<
                    PersistentPortfolioLocationSceneAdapter>();
            sceneAdapter.Configure(
                portfolio,
                materializer,
                persistenceMapper,
                store,
                inventory,
                physicalUnits,
                stocking,
                merchandising,
                stagedCheckout,
                customerFlow,
                employeeWork,
                delivery,
                cleaning,
                cleaningTarget,
                cleaningTool,
                operatingControl,
                player);
            SetObject(portfolio, "locationSceneAdapter", sceneAdapter);
            EditorUtility.SetDirty(materializer);
            EditorUtility.SetDirty(sceneAdapter);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Configured persistent portfolio progression and company desk.");
        }

        private static GameObject Require(string objectName)
        {
            GameObject result = GameObject.Find(objectName);
            if (result == null)
            {
                throw new InvalidOperationException(
                    $"Required scene object '{objectName}' is missing.");
            }
            return result;
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
    }
}
