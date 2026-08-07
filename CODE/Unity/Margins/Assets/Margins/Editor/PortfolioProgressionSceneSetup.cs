using System;
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

            if (player == null || store == null || inventory == null ||
                delivery == null ||
                disk == null || validation == null)
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
