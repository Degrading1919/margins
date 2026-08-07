using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Margins.Editor
{
    public static class FirstStoreCustomerSceneSetup
    {
        private const string ScenePath =
            "Assets/Margins/Scenes/FirstStoreValidation.unity";
        private const string RootName = "Autonomous Customer Flow";

        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            Configure(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        internal static void Configure(Scene scene)
        {
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "Autonomous customer setup requires the first-store validation scene.");
            }

            DestroySceneObjectsNamed(RootName, scene);
            GameObject root = new(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);

            PlaceableFixtureComponent requiredFixture =
                GameObject.Find("Essential Checkout Fixture")
                    ?.GetComponent<PlaceableFixtureComponent>() ??
                throw new InvalidOperationException(
                    "Autonomous customer setup requires the checkout fixture.");
            Transform colaShelf = GameObject.Find("fixture-shelf-cola-validation")
                ?.transform ?? throw new InvalidOperationException(
                    "Autonomous customer setup requires the cola shelf fixture.");
            Transform chipsShelf = GameObject.Find("fixture-shelf-chips-validation")
                ?.transform ?? throw new InvalidOperationException(
                    "Autonomous customer setup requires the chips shelf fixture.");

            Transform entrance = CreatePoint(
                root.transform,
                "Customer Exterior Arrival Boundary",
                new Vector3(-10.5f, 0f, -8.25f));
            Transform exit = CreatePoint(
                root.transform,
                "Customer Exterior Departure Boundary",
                new Vector3(10.5f, 0f, -8.25f));
            Transform checkoutCustomer = CreateLocalPoint(
                requiredFixture.transform,
                "Customer Checkout Position",
                new Vector3(0f, 0f, 0.95f));
            Transform[] browsePoints =
            {
                CreateLocalPoint(colaShelf, "Customer Browse Cola", new Vector3(0f, 0f, -1.15f)),
                CreateLocalPoint(chipsShelf, "Customer Browse Chips", new Vector3(0f, 0f, -1.15f))
            };
            Transform[] queuePoints =
            {
                CreateLocalPoint(requiredFixture.transform, "Customer Queue 1", new Vector3(0f, 0f, 1.95f)),
                CreateLocalPoint(requiredFixture.transform, "Customer Queue 2", new Vector3(0f, 0f, 2.95f)),
                CreateLocalPoint(requiredFixture.transform, "Customer Queue 3", new Vector3(-1.1f, 0f, 3.75f)),
                CreateLocalPoint(requiredFixture.transform, "Customer Queue 4", new Vector3(-2.25f, 0f, 3.75f))
            };
            Transform[] checkoutItems =
            {
                CreateLocalPoint(requiredFixture.transform, "Customer Checkout Item 1", new Vector3(-0.23f, 1.12f, 0.02f)),
                CreateLocalPoint(requiredFixture.transform, "Customer Checkout Item 2", new Vector3(0.23f, 1.12f, 0.02f))
            };

            StoreOperatingController store = Require<StoreOperatingController>();
            CheckoutStationComponent checkout = Require<CheckoutStationComponent>();
            PhysicalProductUnitRegistry physicalUnits = Require<PhysicalProductUnitRegistry>();
            FixturePlacementController fixturePlacement = Require<FixturePlacementController>();

            StoreCustomerFlowController flow =
                root.AddComponent<StoreCustomerFlowController>();
            SetObject(flow, "storeOperating", store);
            SetObject(flow, "checkout", checkout);
            SetObject(flow, "physicalUnits", physicalUnits);
            SetObject(flow, "entrancePoint", entrance);
            SetObject(flow, "exitPoint", exit);
            SetObject(flow, "checkoutCustomerPoint", checkoutCustomer);
            SetObjectArray(flow, "browsePoints", browsePoints);
            SetObjectArray(flow, "queuePoints", queuePoints);
            SetObjectArray(flow, "checkoutItemPoints", checkoutItems);
            SetObjectArray(
                flow,
                "customerMaterials",
                new[]
                {
                    AssetDatabase.LoadAssetAtPath<Material>(
                        "Assets/Margins/Content/FirstStoreValidation/ValidationValid.mat"),
                    AssetDatabase.LoadAssetAtPath<Material>(
                        "Assets/Margins/Content/FirstStoreValidation/ValidationFixture.mat"),
                    AssetDatabase.LoadAssetAtPath<Material>(
                        "Assets/Margins/Content/FirstStoreValidation/ValidationChips.mat")
                });
            SetInteger(flow, "maximumActiveCustomers", 5);
            SetFloat(flow, "arrivalIntervalSeconds", 5f);
            SetFloat(flow, "initialArrivalDelaySeconds", 0.75f);
            SetFloat(flow, "movementSpeed", 2.4f);
            SetFloat(flow, "shoppingSeconds", 1f);
            SetFloat(flow, "queuePatienceSeconds", 35f);
            SetFloat(flow, "checkoutPatienceSeconds", 45f);
            SetBoolean(flow, "showDeveloperStatusLabels", false);

            GameObject checkoutTargetObject = GameObject.Find("World Checkout Interaction") ??
                throw new InvalidOperationException(
                    "Autonomous customer setup requires the world checkout target.");
            StagedCheckoutWorldInteractionTarget stagedTarget =
                checkoutTargetObject.GetComponent<StagedCheckoutWorldInteractionTarget>();
            if (stagedTarget != null)
            {
                stagedTarget.enabled = false;
                EditorUtility.SetDirty(stagedTarget);
            }

            CustomerCheckoutWorldInteractionTarget oldCustomerTarget =
                checkoutTargetObject.GetComponent<CustomerCheckoutWorldInteractionTarget>();
            if (oldCustomerTarget != null)
            {
                UnityEngine.Object.DestroyImmediate(oldCustomerTarget);
            }

            CustomerCheckoutWorldInteractionTarget customerTarget =
                requiredFixture.GetComponent<CustomerCheckoutWorldInteractionTarget>() ??
                requiredFixture.gameObject.AddComponent<CustomerCheckoutWorldInteractionTarget>();
            SetString(customerTarget, "stableTargetId", "target-checkout-customers-01");
            SetObject(customerTarget, "customerFlow", flow);
            SetObject(customerTarget, "operatingController", store);
            SetObject(customerTarget, "fixturePlacement", fixturePlacement);
            SetObject(customerTarget, "requiredFixture", requiredFixture);
            customerTarget.enabled = true;

            foreach (CheckoutProductWorldInteractionTarget stagedProduct in
                     UnityEngine.Object.FindObjectsByType<CheckoutProductWorldInteractionTarget>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                stagedProduct.enabled = false;
                EditorUtility.SetDirty(stagedProduct);
            }

            StagedCheckoutInteractionComponent stagedCheckout =
                checkout.GetComponent<StagedCheckoutInteractionComponent>();
            if (stagedCheckout != null)
            {
                stagedCheckout.enabled = false;
                EditorUtility.SetDirty(stagedCheckout);
            }

            InStoreEmployeeWorkController employeeWork =
                UnityEngine.Object.FindAnyObjectByType<InStoreEmployeeWorkController>();
            if (employeeWork != null)
            {
                SetObject(employeeWork, "customerFlow", flow);
                employeeWork.enabled = true;
                EditorUtility.SetDirty(employeeWork);
            }

            SetObject(store, "customerFlow", flow);
            FirstStorePersistenceMapperComponent mapper =
                Require<FirstStorePersistenceMapperComponent>();
            SetObject(mapper, "customerFlow", flow);

            FirstStoreExperienceController experience =
                UnityEngine.Object.FindAnyObjectByType<FirstStoreExperienceController>();
            if (experience != null)
            {
                SetObject(experience, "customerFlow", flow);
            }

            FirstStorePromptPresenter presenter =
                UnityEngine.Object.FindAnyObjectByType<FirstStorePromptPresenter>();
            if (presenter != null)
            {
                SetObject(presenter, "customerFlow", flow);
            }

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log(
                "Configured autonomous customers and shared player/employee live checkout; retired staged checkout interactions.");
        }

        private static T Require<T>() where T : UnityEngine.Object
        {
            return UnityEngine.Object.FindAnyObjectByType<T>() ??
                   throw new InvalidOperationException(
                       $"Autonomous customer setup requires '{typeof(T).Name}'.");
        }

        private static Transform CreatePoint(
            Transform parent,
            string name,
            Vector3 position)
        {
            DestroySceneObjectsNamed(name, SceneManager.GetActiveScene());
            GameObject point = new(name);
            point.transform.SetParent(parent, false);
            point.transform.position = position;
            return point.transform;
        }

        private static Transform CreateLocalPoint(
            Transform parent,
            string name,
            Vector3 localPosition)
        {
            DestroySceneObjectsNamed(name, SceneManager.GetActiveScene());
            GameObject point = new(name);
            point.transform.SetParent(parent, false);
            point.transform.localPosition = localPosition;
            point.transform.localRotation = Quaternion.identity;
            return point.transform;
        }

        private static void DestroySceneObjectsNamed(string name, Scene scene)
        {
            foreach (GameObject existing in Resources
                         .FindObjectsOfTypeAll<GameObject>()
                         .Where(candidate =>
                             candidate.scene == scene && candidate.name == name)
                         .ToArray())
            {
                UnityEngine.Object.DestroyImmediate(existing);
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

        private static void SetObjectArray<T>(
            UnityEngine.Object target,
            string propertyName,
            IReadOnlyList<T> values) where T : UnityEngine.Object
        {
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                throw new InvalidOperationException(
                    $"Serialized property '{propertyName}' is missing on '{target.name}'.");
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++)
            {
                if (values[index] == null)
                {
                    throw new InvalidOperationException(
                        $"Serialized property '{propertyName}' contains a missing reference.");
                }
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetString(
            UnityEngine.Object target,
            string propertyName,
            string value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetInteger(
            UnityEngine.Object target,
            string propertyName,
            int value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(propertyName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetFloat(
            UnityEngine.Object target,
            string propertyName,
            float value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(propertyName).floatValue = value;
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
