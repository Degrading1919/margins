using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Margins.Editor
{
    public static class FirstStoreExperienceSceneSetup
    {
        private const string MaterialFolder =
            "Assets/Margins/Content/FirstStoreExperience";
        private const string PresentationRootName = "First Store Presentation";
        private const string ExperienceChildPrefix = "Experience ";

        public static void Apply(Scene scene)
        {
            EnsureFolder("Assets/Margins/Content", "FirstStoreExperience");

            Material wall = Material("Wall Warm Plaster", new Color(0.68f, 0.61f, 0.51f), 0f, 0.22f);
            Material wallLight = Material("Wall Light", new Color(0.84f, 0.79f, 0.69f), 0f, 0.2f);
            Material tile = Material("Store Tile", new Color(0.16f, 0.19f, 0.19f), 0f, 0.48f);
            Material ceiling = Material("Ceiling", new Color(0.49f, 0.47f, 0.42f), 0f, 0.25f);
            Material charcoal = Material("Fixture Charcoal", new Color(0.055f, 0.075f, 0.08f), 0.35f, 0.52f);
            Material metal = Material("Painted Metal", new Color(0.14f, 0.17f, 0.17f), 0.55f, 0.56f);
            Material laminate = Material("Warm Laminate", new Color(0.45f, 0.25f, 0.105f), 0f, 0.34f);
            Material cardboard = Material("Corrugated Cardboard", new Color(0.55f, 0.34f, 0.15f), 0f, 0.18f);
            Material teal = Material("Market Teal", new Color(0.025f, 0.34f, 0.31f), 0.08f, 0.42f);
            Material orange = Material("Market Orange", new Color(0.87f, 0.24f, 0.065f), 0f, 0.36f);
            Material cream = Material("Sign Cream", new Color(0.91f, 0.82f, 0.62f), 0f, 0.32f);
            Material cola = Material("Mile 7 Cola", new Color(0.69f, 0.055f, 0.07f), 0.52f, 0.68f);
            Material chips = Material("Sunset Chips", new Color(0.94f, 0.49f, 0.055f), 0.02f, 0.3f);
            Material spill = TransparentMaterial("Cleaning Spill", new Color(0.12f, 0.22f, 0.29f, 0.82f), 0.7f);
            Material glass = TransparentMaterial("Storefront Glass", new Color(0.08f, 0.19f, 0.24f, 0.38f), 0.88f);
            Material asphalt = Material("Parking Asphalt", new Color(0.055f, 0.065f, 0.07f), 0f, 0.18f);
            Material sidewalk = Material("Sidewalk", new Color(0.31f, 0.3f, 0.27f), 0f, 0.24f);
            Material amberEmission = EmissiveMaterial(
                "Objective Amber",
                new Color(1f, 0.36f, 0.055f),
                4.5f);
            Material tealEmission = EmissiveMaterial(
                "Open Sign Teal",
                new Color(0.08f, 0.88f, 0.63f),
                4f);
            Material warmEmission = EmissiveMaterial(
                "Warm Practical",
                new Color(1f, 0.55f, 0.22f),
                3.6f);

            ConfigureProductPrefab(
                "Assets/Margins/Content/FirstStoreValidation/ValidationColaUnit.prefab",
                PrimitiveType.Cylinder,
                new Vector3(0.16f, 0.18f, 0.16f),
                cola,
                cream,
                true);
            ConfigureProductPrefab(
                "Assets/Margins/Content/FirstStoreValidation/ValidationChipsUnit.prefab",
                PrimitiveType.Cube,
                new Vector3(0.34f, 0.42f, 0.1f),
                chips,
                cream,
                false);

            GameObject oldRoot = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == PresentationRootName);
            if (oldRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(oldRoot);
            }

            GameObject presentationRoot = new(PresentationRootName);
            presentationRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            ConfigureAtmosphere(presentationRoot, warmEmission);
            BuildExterior(
                presentationRoot.transform,
                wall,
                wallLight,
                charcoal,
                teal,
                orange,
                cream,
                glass,
                asphalt,
                sidewalk,
                warmEmission);
            Light[] interiorLights = BuildInterior(
                presentationRoot.transform,
                wall,
                wallLight,
                tile,
                ceiling,
                charcoal,
                metal,
                teal,
                orange,
                cream,
                warmEmission,
                out Renderer[] practicalRenderers);

            GameObject playerObject = Require("Validation Player");
            FirstPersonController player = playerObject.GetComponent<FirstPersonController>();
            Camera camera = Require("Validation Camera").GetComponent<Camera>();
            playerObject.transform.SetPositionAndRotation(
                new Vector3(0f, 1f, -7.65f),
                Quaternion.identity);
            camera.fieldOfView = 72f;
            camera.nearClipPlane = 0.04f;
            camera.farClipPlane = 140f;
            camera.allowHDR = true;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            GameObject validationFloor = Require("Validation Floor");
            SetRendererEnabled(validationFloor, false);

            GameObject colaShelf = Require("fixture-shelf-cola-validation");
            GameObject chipsShelf = Require("fixture-shelf-chips-validation");
            ConfigureShelf(
                presentationRoot.transform,
                colaShelf,
                new Vector3(-2.55f, 0f, 1.25f),
                "COLD DRINKS",
                teal,
                charcoal,
                metal,
                cream);
            ConfigureShelf(
                presentationRoot.transform,
                chipsShelf,
                new Vector3(2.55f, 0f, 1.25f),
                "SNACKS",
                orange,
                charcoal,
                metal,
                cream);

            GameObject deliveryObject = Require("Mixed Starter Delivery");
            DeliveryPresentation deliveryPresentation = ConfigureDelivery(
                presentationRoot.transform,
                deliveryObject,
                cardboard,
                charcoal,
                cola,
                chips,
                cream);

            GameObject fixtureHandle = Require("Essential Checkout Fixture Placement Handle");
            GameObject requiredFixture = Require("Essential Checkout Fixture");
            CheckoutPresentation checkoutPresentation = ConfigureCheckout(
                fixtureHandle,
                requiredFixture,
                charcoal,
                laminate,
                cardboard,
                teal,
                cream,
                cola,
                chips,
                amberEmission);

            GameObject checkoutInteraction = Require("World Checkout Interaction");
            GameObject storeControl = Require("World Store Operating Control");
            StoreControlPresentation storeControlPresentation = ConfigureStoreControl(
                storeControl,
                charcoal,
                tealEmission,
                cream);

            GameObject cleaningTarget = Require("World Cleaning Interaction");
            ConfigureCleaning(
                presentationRoot.transform,
                cleaningTarget,
                spill,
                teal,
                metal,
                cream);

            GameObject colaTarget = deliveryObject.transform
                .Find("Delivery Content Cola Target").gameObject;
            GameObject chipsTarget = deliveryObject.transform
                .Find("Delivery Content Chips Target").gameObject;
            Transform colaShelfTarget = colaShelf
                .GetComponentsInChildren<ShelfSnapWorldInteractionTarget>(true)
                .OrderBy(target => target.SnapPointId, StringComparer.Ordinal)
                .First().transform;
            Transform chipsShelfTarget = chipsShelf
                .GetComponentsInChildren<ShelfSnapWorldInteractionTarget>(true)
                .OrderBy(target => target.SnapPointId, StringComparer.Ordinal)
                .First().transform;

            GameObject objectiveBeacon = CreateObjectiveBeacon(
                presentationRoot.transform,
                amberEmission,
                cream);

            FirstStorePromptPresenter presenter = Require("First-Store Validation Controls")
                .GetComponent<FirstStorePromptPresenter>();
            SetObject(presenter, "storeControlTarget", storeControl.transform);
            SetObject(presenter, "fixtureHandleTarget", fixtureHandle.transform);
            SetObject(presenter, "deliveryTarget", deliveryObject.transform);
            SetObject(presenter, "colaDeliveryTarget", colaTarget.transform);
            SetObject(presenter, "chipsDeliveryTarget", chipsTarget.transform);
            SetObject(presenter, "colaShelfTarget", colaShelfTarget);
            SetObject(presenter, "chipsShelfTarget", chipsShelfTarget);
            SetObject(presenter, "checkoutTarget", checkoutInteraction.transform);
            SetObject(presenter, "cleaningTarget", cleaningTarget.transform);

            GameObject controlsObject = Require("First-Store Validation Controls");
            FirstStoreExperienceController experience =
                GetOrAdd<FirstStoreExperienceController>(controlsObject);
            SetObject(experience, "player", player);
            SetObject(experience, "interaction", playerObject.GetComponent<FirstStoreInteractionController>());
            SetObject(experience, "promptPresenter", presenter);
            SetObject(experience, "delivery", deliveryObject.GetComponent<DeliveryBoxComponent>());
            SetObject(experience, "stocking", Require("Stocking Controller").GetComponent<StockingController>());
            SetObject(experience, "checkout", Require("Checkout Station").GetComponent<CheckoutStationComponent>());
            SetObject(experience, "stagedCheckout", Require("Checkout Station").GetComponent<StagedCheckoutInteractionComponent>());
            SetObject(experience, "cleaning", Require("Cleaning Task").GetComponent<CleaningTaskComponent>());
            SetObject(experience, "store", Require("Store Operating Controller").GetComponent<StoreOperatingController>());
            SetObject(experience, "fixturePlacement", Require("Fixture Placement").GetComponent<FixturePlacementController>());
            SetObject(experience, "checkoutFixture", requiredFixture.GetComponent<PlaceableFixtureComponent>());
            SetObject(experience, "colaProduct", AssetDatabase.LoadAssetAtPath<ProductDefinition>(
                "Assets/Margins/Content/FirstStoreValidation/ValidationColaProduct.asset"));
            SetObject(experience, "chipsProduct", AssetDatabase.LoadAssetAtPath<ProductDefinition>(
                "Assets/Margins/Content/FirstStoreValidation/ValidationChipsProduct.asset"));
            SetObject(experience, "deliveryLidPivot", deliveryPresentation.LidPivot);
            SetObject(experience, "colaDeliveryCollider", colaTarget.GetComponent<Collider>());
            SetObject(experience, "chipsDeliveryCollider", chipsTarget.GetComponent<Collider>());
            SetObject(experience, "colaDeliveryRenderer", colaTarget.GetComponent<Renderer>());
            SetObject(experience, "chipsDeliveryRenderer", chipsTarget.GetComponent<Renderer>());
            SetObject(experience, "checkoutInteractionCollider", checkoutInteraction.GetComponent<Collider>());
            SetObject(experience, "storefrontStateText", storeControlPresentation.StateText);
            SetObject(experience, "storefrontStateRenderer", storeControlPresentation.EmissiveRenderer);
            SetObject(experience, "checkoutDisplayText", checkoutPresentation.DisplayText);
            SetObject(experience, "checkoutColaProp", checkoutPresentation.ColaProp);
            SetObject(experience, "checkoutChipsProp", checkoutPresentation.ChipsProp);
            SetObject(experience, "cleaningSpillVisual", cleaningTarget.transform);
            SetObject(experience, "cleaningSpillRenderer", cleaningTarget.GetComponent<Renderer>());
            SetObject(experience, "objectiveBeacon", objectiveBeacon.transform);
            SetArray(experience, "interiorLights", interiorLights);
            SetArray(experience, "practicalLightRenderers", practicalRenderers);

            EditorUtility.SetDirty(presentationRoot);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureAtmosphere(GameObject root, Material warmEmission)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.075f, 0.11f, 0.145f);
            RenderSettings.fogStartDistance = 28f;
            RenderSettings.fogEndDistance = 82f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.21f, 0.28f, 0.38f);
            RenderSettings.ambientEquatorColor = new Color(0.27f, 0.2f, 0.16f);
            RenderSettings.ambientGroundColor = new Color(0.055f, 0.06f, 0.075f);
            RenderSettings.reflectionIntensity = 0.7f;

            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                string path = $"{MaterialFolder}/Mile 7 Dusk Sky.mat";
                Material sky = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (sky == null)
                {
                    sky = new Material(skyShader) { name = "Mile 7 Dusk Sky" };
                    AssetDatabase.CreateAsset(sky, path);
                }
                sky.SetColor("_SkyTint", new Color(0.18f, 0.3f, 0.5f));
                sky.SetColor("_GroundColor", new Color(0.19f, 0.09f, 0.055f));
                sky.SetFloat("_SunSize", 0.035f);
                sky.SetFloat("_SunSizeConvergence", 5f);
                sky.SetFloat("_AtmosphereThickness", 0.72f);
                sky.SetFloat("_Exposure", 0.78f);
                RenderSettings.skybox = sky;
                EditorUtility.SetDirty(sky);
            }

            GameObject directionalObject = Require("Validation Directional Light");
            Light directional = directionalObject.GetComponent<Light>();
            directionalObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
            directional.type = LightType.Directional;
            directional.color = new Color(0.8f, 0.86f, 1f);
            directional.intensity = 0.58f;
            directional.shadows = LightShadows.Soft;
            directional.shadowStrength = 0.72f;

            Volume volume = root.GetComponent<Volume>() ?? root.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 2f;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                "Assets/Settings/SampleSceneProfile.asset");

            CreatePointLight(
                root.transform,
                "Exterior Sign Glow",
                new Vector3(0f, 3.55f, -5.85f),
                new Color(1f, 0.32f, 0.12f),
                6.5f,
                4.2f,
                false);
        }

        private static void BuildExterior(
            Transform root,
            Material wall,
            Material wallLight,
            Material charcoal,
            Material teal,
            Material orange,
            Material cream,
            Material glass,
            Material asphalt,
            Material sidewalk,
            Material warmEmission)
        {
            CreateShape(root, "Parking Lot", PrimitiveType.Cube,
                new Vector3(0f, -0.16f, -13.5f), new Vector3(24f, 0.22f, 15f), asphalt, true);
            CreateShape(root, "Front Sidewalk", PrimitiveType.Cube,
                new Vector3(0f, -0.05f, -6.75f), new Vector3(13f, 0.18f, 3.5f), sidewalk, true);
            CreateShape(root, "Curb", PrimitiveType.Cube,
                new Vector3(0f, 0.02f, -8.45f), new Vector3(13f, 0.25f, 0.22f), wallLight, true);
            CreateShape(root, "Front Fascia", PrimitiveType.Cube,
                new Vector3(0f, 3.12f, -5.2f), new Vector3(10.6f, 0.72f, 0.34f), wall, true);
            CreateShape(root, "Front Left Pier", PrimitiveType.Cube,
                new Vector3(-4.95f, 1.55f, -5.2f), new Vector3(0.68f, 3.2f, 0.34f), wall, true);
            CreateShape(root, "Front Right Pier", PrimitiveType.Cube,
                new Vector3(4.95f, 1.55f, -5.2f), new Vector3(0.68f, 3.2f, 0.34f), wall, true);
            CreateShape(root, "Door Left Frame", PrimitiveType.Cube,
                new Vector3(-0.86f, 1.25f, -5.16f), new Vector3(0.12f, 2.5f, 0.2f), charcoal, true);
            CreateShape(root, "Door Right Frame", PrimitiveType.Cube,
                new Vector3(0.86f, 1.25f, -5.16f), new Vector3(0.12f, 2.5f, 0.2f), charcoal, true);
            CreateShape(root, "Door Header", PrimitiveType.Cube,
                new Vector3(0f, 2.5f, -5.16f), new Vector3(1.85f, 0.12f, 0.2f), charcoal, true);
            CreateShape(root, "Left Storefront Glass", PrimitiveType.Cube,
                new Vector3(-2.88f, 1.5f, -5.16f), new Vector3(3.8f, 2.65f, 0.1f), glass, true);
            CreateShape(root, "Right Storefront Glass", PrimitiveType.Cube,
                new Vector3(2.88f, 1.5f, -5.16f), new Vector3(3.8f, 2.65f, 0.1f), glass, true);

            GameObject signBacking = CreateShape(root, "Mile 7 Sign Backing", PrimitiveType.Cube,
                new Vector3(0f, 3.7f, -5.42f), new Vector3(6.8f, 0.9f, 0.18f), charcoal, false);
            CreateShape(root, "Mile 7 Sign Teal Bar", PrimitiveType.Cube,
                new Vector3(-2.55f, 3.7f, -5.54f), new Vector3(1.25f, 0.72f, 0.08f), teal, false);
            CreateShape(root, "Mile 7 Sign Orange Bar", PrimitiveType.Cube,
                new Vector3(2.55f, 3.7f, -5.54f), new Vector3(1.25f, 0.72f, 0.08f), orange, false);
            CreateText(
                signBacking.transform,
                "Experience Store Name",
                "MILE 7 MARKET",
                new Vector3(0f, 0f, -0.58f),
                0.2f,
                cream.color,
                TextAnchor.MiddleCenter);
            CreateText(root, "Window Hours", "OPEN LATE  /  COLD DRINKS  /  SNACKS",
                new Vector3(-2.9f, 0.75f, -5.32f), 0.075f, cream.color, TextAnchor.MiddleCenter);

            CreateShape(root, "Entry Mat", PrimitiveType.Cube,
                new Vector3(0f, 0.015f, -4.72f), new Vector3(1.55f, 0.03f, 1.05f), teal, false);
            CreateText(root, "Entry Mat Text", "M7", new Vector3(0f, 0.045f, -4.72f),
                0.17f, cream.color, TextAnchor.MiddleCenter, Quaternion.Euler(90f, 0f, 0f));

            for (int index = 0; index < 3; index++)
            {
                float x = -7f + index * 7f;
                CreateShape(root, $"Parking Stripe {index}", PrimitiveType.Cube,
                    new Vector3(x, -0.035f, -12f), new Vector3(0.09f, 0.025f, 5.5f), cream, false);
            }
            CreateStylizedCar(root, "Parked Car A", new Vector3(-5.7f, 0.35f, -12.6f), teal, charcoal);
            CreateStylizedCar(root, "Parked Car B", new Vector3(5.8f, 0.35f, -13.5f), orange, charcoal);

            for (int index = 0; index < 5; index++)
            {
                float x = -18f + index * 9f;
                float height = 4.5f + (index % 3) * 1.6f;
                CreateShape(root, $"Distant Building {index}", PrimitiveType.Cube,
                    new Vector3(x, height * 0.5f - 0.2f, -29f - index % 2 * 4f),
                    new Vector3(6.5f, height, 5f),
                    index % 2 == 0 ? wall : charcoal,
                    false);
            }
        }

        private static Light[] BuildInterior(
            Transform root,
            Material wall,
            Material wallLight,
            Material tile,
            Material ceiling,
            Material charcoal,
            Material metal,
            Material teal,
            Material orange,
            Material cream,
            Material warmEmission,
            out Renderer[] practicalRenderers)
        {
            CreateShape(root, "Store Floor Visual", PrimitiveType.Cube,
                new Vector3(0f, -0.08f, 0.45f), new Vector3(10.2f, 0.15f, 11.1f), tile, false);
            CreateShape(root, "Back Wall", PrimitiveType.Cube,
                new Vector3(0f, 1.72f, 5.92f), new Vector3(10.55f, 3.45f, 0.28f), wall, true);
            CreateShape(root, "Left Wall", PrimitiveType.Cube,
                new Vector3(-5.14f, 1.72f, 0.45f), new Vector3(0.28f, 3.45f, 11.2f), wall, true);
            CreateShape(root, "Right Wall", PrimitiveType.Cube,
                new Vector3(5.14f, 1.72f, 0.45f), new Vector3(0.28f, 3.45f, 11.2f), wall, true);
            CreateShape(root, "Ceiling", PrimitiveType.Cube,
                new Vector3(0f, 3.5f, 0.45f), new Vector3(10.55f, 0.22f, 11.2f), ceiling, false);

            CreateShape(root, "Left Wall Teal Stripe", PrimitiveType.Cube,
                new Vector3(-4.98f, 1.95f, 0.45f), new Vector3(0.05f, 0.18f, 10.6f), teal, false);
            CreateShape(root, "Right Wall Orange Stripe", PrimitiveType.Cube,
                new Vector3(4.98f, 1.95f, 0.45f), new Vector3(0.05f, 0.18f, 10.6f), orange, false);
            CreateShape(root, "Back Wall Stripe", PrimitiveType.Cube,
                new Vector3(0f, 1.95f, 5.76f), new Vector3(9.9f, 0.18f, 0.05f), teal, false);

            CreateShape(root, "Receiving Floor Zone", PrimitiveType.Cube,
                new Vector3(-3.3f, 0.012f, 4.3f), new Vector3(3.2f, 0.025f, 2.5f), charcoal, false);
            CreateText(root, "Receiving Wall Sign", "RECEIVING  /  STARTER DELIVERY",
                new Vector3(-2.75f, 2.45f, 5.72f), 0.09f, cream.color, TextAnchor.MiddleCenter);
            CreateShape(root, "Receiving Rail", PrimitiveType.Cube,
                new Vector3(-2.75f, 2.16f, 5.74f), new Vector3(4.3f, 0.05f, 0.06f), orange, false);

            CreateShape(root, "Backroom Rack Left Post", PrimitiveType.Cube,
                new Vector3(-4.7f, 1f, 3.78f), new Vector3(0.1f, 2f, 0.1f), charcoal, true);
            CreateShape(root, "Backroom Rack Right Post", PrimitiveType.Cube,
                new Vector3(-2.1f, 1f, 3.78f), new Vector3(0.1f, 2f, 0.1f), charcoal, true);
            for (int index = 0; index < 3; index++)
            {
                CreateShape(root, $"Backroom Rack Shelf {index}", PrimitiveType.Cube,
                    new Vector3(-3.4f, 0.35f + index * 0.65f, 3.86f),
                    new Vector3(2.7f, 0.08f, 0.65f), metal, true);
            }

            CreateShape(root, "Checkout Floor Accent", PrimitiveType.Cube,
                new Vector3(3.25f, 0.015f, -1.95f), new Vector3(3.15f, 0.03f, 2.5f), teal, false);
            CreateText(root, "Checkout Overhead", "CHECKOUT 01",
                new Vector3(3.25f, 2.55f, -0.85f), 0.11f, cream.color, TextAnchor.MiddleCenter);

            List<Light> lights = new();
            List<Renderer> bulbs = new();
            Vector3[] positions =
            {
                new(-3.15f, 3.28f, -2.5f),
                new(0f, 3.28f, -2.5f),
                new(3.15f, 3.28f, -2.5f),
                new(-3.15f, 3.28f, 1.2f),
                new(0f, 3.28f, 1.2f),
                new(3.15f, 3.28f, 1.2f),
                new(-3.15f, 3.28f, 4.45f),
                new(2.6f, 3.28f, 4.45f)
            };
            for (int index = 0; index < positions.Length; index++)
            {
                GameObject panel = CreateShape(root, $"Ceiling Practical {index + 1}", PrimitiveType.Cube,
                    positions[index], new Vector3(1.2f, 0.06f, 0.38f), warmEmission, false);
                bulbs.Add(panel.GetComponent<Renderer>());
                Light light = CreatePointLight(
                    root,
                    $"Store Light {index + 1}",
                    positions[index] + Vector3.down * 0.18f,
                    index >= 6 ? new Color(0.72f, 0.82f, 1f) : new Color(1f, 0.69f, 0.38f),
                    index >= 6 ? 5.2f : 5.8f,
                    index >= 6 ? 5.5f : 7.5f,
                    index == 1 || index == 4 || index == 6);
                lights.Add(light);
            }
            Light checkoutFocus = CreateSpotLight(
                root,
                "Checkout Focus Light",
                new Vector3(3.2f, 3.18f, -1.55f),
                new Color(1f, 0.48f, 0.18f),
                6f,
                20f,
                72f);
            lights.Add(checkoutFocus);

            practicalRenderers = bulbs.ToArray();
            return lights.ToArray();
        }

        private static void ConfigureShelf(
            Transform presentationRoot,
            GameObject shelfObject,
            Vector3 position,
            string category,
            Material accent,
            Material charcoal,
            Material metal,
            Material cream)
        {
            shelfObject.transform.SetPositionAndRotation(position, Quaternion.identity);
            shelfObject.transform.localScale = Vector3.one;
            SetRendererEnabled(shelfObject, false);
            Collider rootCollider = shelfObject.GetComponent<Collider>();
            if (rootCollider != null)
            {
                rootCollider.enabled = false;
            }

            foreach (ShelfSnapWorldInteractionTarget target in
                     shelfObject.GetComponentsInChildren<ShelfSnapWorldInteractionTarget>(true))
            {
                target.transform.localPosition = new Vector3(
                    target.transform.localPosition.x,
                    0.62f,
                    -0.24f);
                target.transform.localScale = new Vector3(0.42f, 0.46f, 0.32f);
                SetRendererEnabled(target.gameObject, false);
                Collider targetCollider = target.GetComponent<Collider>();
                if (targetCollider != null)
                {
                    targetCollider.enabled = true;
                }
            }

            string prefix = shelfObject.name.Contains("cola", StringComparison.Ordinal)
                ? "Cola Shelf"
                : "Chips Shelf";
            CreateShape(presentationRoot, $"{prefix} Back", PrimitiveType.Cube,
                position + new Vector3(0f, 0.92f, 0.28f),
                new Vector3(2.25f, 1.84f, 0.12f), charcoal, true);
            CreateShape(presentationRoot, $"{prefix} Left Post", PrimitiveType.Cube,
                position + new Vector3(-1.08f, 0.93f, 0f),
                new Vector3(0.1f, 1.86f, 0.7f), metal, true);
            CreateShape(presentationRoot, $"{prefix} Right Post", PrimitiveType.Cube,
                position + new Vector3(1.08f, 0.93f, 0f),
                new Vector3(0.1f, 1.86f, 0.7f), metal, true);
            float[] shelfHeights = { 0.43f, 0.95f, 1.47f };
            for (int index = 0; index < shelfHeights.Length; index++)
            {
                CreateShape(presentationRoot, $"{prefix} Deck {index + 1}", PrimitiveType.Cube,
                    position + new Vector3(0f, shelfHeights[index], 0f),
                    new Vector3(2.24f, 0.08f, 0.74f), metal, true);
                CreateShape(presentationRoot, $"{prefix} Price Rail {index + 1}", PrimitiveType.Cube,
                    position + new Vector3(0f, shelfHeights[index] + 0.04f, -0.39f),
                    new Vector3(2.18f, 0.1f, 0.05f), accent, false);
            }
            CreateShape(presentationRoot, $"{prefix} Header", PrimitiveType.Cube,
                position + new Vector3(0f, 1.82f, 0f),
                new Vector3(2.3f, 0.34f, 0.18f), accent, false);
            CreateText(presentationRoot, $"{prefix} Category", category,
                position + new Vector3(0f, 1.82f, -0.13f),
                0.085f, cream.color, TextAnchor.MiddleCenter);

            for (int index = 0; index < 4; index++)
            {
                float x = -0.78f + index * 0.52f;
                CreateShape(presentationRoot, $"{prefix} Empty Bay {index + 1}", PrimitiveType.Cube,
                    position + new Vector3(x, 0.66f, 0.3f),
                    new Vector3(0.38f, 0.34f, 0.035f), accent, false);
            }
        }

        private static DeliveryPresentation ConfigureDelivery(
            Transform presentationRoot,
            GameObject deliveryObject,
            Material cardboard,
            Material charcoal,
            Material cola,
            Material chips,
            Material cream)
        {
            deliveryObject.transform.SetPositionAndRotation(
                new Vector3(-3.45f, 0.48f, 4.62f),
                Quaternion.identity);
            deliveryObject.transform.localScale = new Vector3(1.5f, 0.9f, 1.12f);
            SetRendererEnabled(deliveryObject, false);
            DestroyExperienceChildren(deliveryObject.transform);

            GameObject colaTarget = deliveryObject.transform
                .Find("Delivery Content Cola Target").gameObject;
            GameObject chipsTarget = deliveryObject.transform
                .Find("Delivery Content Chips Target").gameObject;
            DestroyAllTextChildren(colaTarget.transform);
            DestroyAllTextChildren(chipsTarget.transform);
            colaTarget.transform.localPosition = new Vector3(-0.29f, 0.56f, -0.61f);
            colaTarget.transform.localScale = new Vector3(0.5f, 0.5f, 0.28f);
            colaTarget.GetComponent<Renderer>().sharedMaterial = cola;
            chipsTarget.transform.localPosition = new Vector3(0.29f, 0.56f, -0.61f);
            chipsTarget.transform.localScale = new Vector3(0.5f, 0.5f, 0.28f);
            chipsTarget.GetComponent<Renderer>().sharedMaterial = chips;
            CreateText(colaTarget.transform, "Experience Cola Case Label", "COLA",
                new Vector3(0f, 0f, -0.57f), 0.14f, cream.color, TextAnchor.MiddleCenter);
            CreateText(chipsTarget.transform, "Experience Chips Case Label", "CHIPS",
                new Vector3(0f, 0f, -0.57f), 0.12f, cream.color, TextAnchor.MiddleCenter);

            Vector3 center = deliveryObject.transform.position;
            CreateShape(presentationRoot, "Delivery Pallet", PrimitiveType.Cube,
                new Vector3(center.x, 0.08f, center.z), new Vector3(1.8f, 0.16f, 1.35f), charcoal, true);
            CreateShape(presentationRoot, "Delivery Box Base", PrimitiveType.Cube,
                new Vector3(center.x, 0.52f, center.z), new Vector3(1.48f, 0.82f, 1.08f), cardboard, false);
            CreateShape(presentationRoot, "Delivery Box Front Band", PrimitiveType.Cube,
                new Vector3(center.x, 0.57f, center.z - 0.56f), new Vector3(1.25f, 0.28f, 0.04f), cream, false);
            CreateText(presentationRoot, "Delivery Case Name", "MILE 7 STARTER CASE",
                new Vector3(center.x, 0.57f, center.z - 0.6f), 0.07f, charcoal.color, TextAnchor.MiddleCenter);

            GameObject lidPivot = new("Experience Delivery Lid Pivot");
            lidPivot.transform.SetParent(presentationRoot, false);
            lidPivot.transform.position = new Vector3(center.x, 0.95f, center.z + 0.5f);
            GameObject lid = CreateShape(lidPivot.transform, "Experience Delivery Lid", PrimitiveType.Cube,
                new Vector3(0f, 0f, -0.5f), new Vector3(1.5f, 0.08f, 1.05f), cardboard, false);
            lid.transform.localPosition = new Vector3(0f, 0f, -0.5f);

            return new DeliveryPresentation(lidPivot.transform);
        }

        private static CheckoutPresentation ConfigureCheckout(
            GameObject fixtureHandle,
            GameObject requiredFixture,
            Material charcoal,
            Material laminate,
            Material cardboard,
            Material teal,
            Material cream,
            Material cola,
            Material chips,
            Material amberEmission)
        {
            DestroyAllTextChildren(fixtureHandle.transform);
            DestroyExperienceChildren(fixtureHandle.transform);
            fixtureHandle.transform.SetPositionAndRotation(
                new Vector3(2.95f, 0.11f, -0.45f),
                Quaternion.identity);
            fixtureHandle.transform.localScale = new Vector3(2.25f, 0.62f, 1.25f);
            fixtureHandle.GetComponent<Renderer>().sharedMaterial = cardboard;
            CreateText(fixtureHandle.transform, "Experience Checkout Kit Label", "CHECKOUT KIT  /  E TO PLACE",
                new Vector3(0f, 0.7f, -0.52f), 0.06f, cream.color, TextAnchor.MiddleCenter);

            DestroyExperienceChildren(requiredFixture.transform);
            requiredFixture.transform.SetPositionAndRotation(
                new Vector3(2.95f, 0f, -0.45f),
                Quaternion.identity);
            requiredFixture.transform.localScale = Vector3.one;
            SetRendererEnabled(requiredFixture, false);
            Collider rootCollider = requiredFixture.GetComponent<Collider>();
            if (rootCollider != null)
            {
                rootCollider.enabled = false;
            }

            GameObject footprint = CreateShape(requiredFixture.transform, "Experience Fixture Footprint", PrimitiveType.Cube,
                new Vector3(0f, 0.035f, 0f), new Vector3(2f, 0.07f, 1f), charcoal, false);
            GameObject body = CreateShape(requiredFixture.transform, "Experience Checkout Body", PrimitiveType.Cube,
                new Vector3(0f, 0.49f, 0f), new Vector3(2f, 0.92f, 0.9f), charcoal, true);
            CreateShape(requiredFixture.transform, "Experience Checkout Top", PrimitiveType.Cube,
                new Vector3(0f, 0.98f, 0f), new Vector3(2.08f, 0.12f, 0.98f), laminate, true);
            CreateShape(requiredFixture.transform, "Experience Checkout Accent", PrimitiveType.Cube,
                new Vector3(0f, 0.52f, -0.47f), new Vector3(1.8f, 0.16f, 0.04f), teal, false);

            PlaceableFixtureComponent placeable = requiredFixture.GetComponent<PlaceableFixtureComponent>();
            SerializedObject placeableSerialized = new(placeable);
            placeableSerialized.FindProperty("previewRenderer").objectReferenceValue = footprint.GetComponent<Renderer>();
            placeableSerialized.FindProperty("defaultMaterial").objectReferenceValue = charcoal;
            placeableSerialized.FindProperty("validMaterial").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/Margins/Content/FirstStoreValidation/ValidationValid.mat");
            placeableSerialized.FindProperty("invalidMaterial").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/Margins/Content/FirstStoreValidation/ValidationInvalid.mat");
            placeableSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject checkoutTarget = Require("World Checkout Interaction");
            DestroyAllTextChildren(checkoutTarget.transform);
            checkoutTarget.transform.SetParent(requiredFixture.transform, false);
            checkoutTarget.transform.localPosition = new Vector3(0.35f, 1.16f, -0.3f);
            checkoutTarget.transform.localRotation = Quaternion.identity;
            checkoutTarget.transform.localScale = new Vector3(0.42f, 0.28f, 0.32f);
            checkoutTarget.GetComponent<Renderer>().sharedMaterial = charcoal;

            CreateShape(checkoutTarget.transform, "Experience Scanner Glow", PrimitiveType.Cube,
                new Vector3(-0.78f, -0.05f, -0.57f), new Vector3(0.7f, 0.12f, 0.12f), amberEmission, false);
            TextMesh displayText = CreateText(checkoutTarget.transform, "Experience Register Display", "SET UP\nCHECKOUT",
                new Vector3(0f, 0.04f, -0.58f), 0.13f, cream.color, TextAnchor.MiddleCenter);

            GameObject colaProp = CreateShape(requiredFixture.transform, "Experience Checkout Cola Prop", PrimitiveType.Cylinder,
                new Vector3(-0.45f, 1.2f, -0.18f), new Vector3(0.14f, 0.18f, 0.14f), cola, false);
            GameObject chipsProp = CreateShape(requiredFixture.transform, "Experience Checkout Chips Prop", PrimitiveType.Cube,
                new Vector3(-0.65f, 1.2f, -0.18f), new Vector3(0.28f, 0.34f, 0.09f), chips, false);
            colaProp.SetActive(false);
            chipsProp.SetActive(false);

            return new CheckoutPresentation(displayText, colaProp, chipsProp);
        }

        private static StoreControlPresentation ConfigureStoreControl(
            GameObject storeControl,
            Material charcoal,
            Material tealEmission,
            Material cream)
        {
            DestroyAllTextChildren(storeControl.transform);
            DestroyExperienceChildren(storeControl.transform);
            storeControl.transform.SetPositionAndRotation(
                new Vector3(-3.95f, 1.35f, -4.72f),
                Quaternion.identity);
            storeControl.transform.localScale = new Vector3(1.15f, 0.82f, 0.18f);
            storeControl.GetComponent<Renderer>().sharedMaterial = charcoal;
            GameObject statePanel = CreateShape(storeControl.transform, "Experience Store State Glow", PrimitiveType.Cube,
                new Vector3(0f, 0.08f, -0.58f), new Vector3(0.86f, 0.38f, 0.08f), tealEmission, false);
            TextMesh stateText = CreateText(storeControl.transform, "Experience Store State Text", "CLOSED  /  CLOCK IN",
                new Vector3(0f, 0.08f, -0.66f), 0.115f, cream.color, TextAnchor.MiddleCenter);
            CreateText(storeControl.transform, "Experience Store Panel Label", "SHIFT CONTROL",
                new Vector3(0f, 0.48f, -0.58f), 0.08f, cream.color, TextAnchor.MiddleCenter);
            return new StoreControlPresentation(stateText, statePanel.GetComponent<Renderer>());
        }

        private static void ConfigureCleaning(
            Transform root,
            GameObject cleaningTarget,
            Material spill,
            Material teal,
            Material metal,
            Material cream)
        {
            DestroyAllTextChildren(cleaningTarget.transform);
            cleaningTarget.transform.SetPositionAndRotation(
                new Vector3(-1.45f, 0.035f, 2.72f),
                Quaternion.identity);
            cleaningTarget.transform.localScale = new Vector3(1.45f, 0.045f, 1.05f);
            cleaningTarget.GetComponent<Renderer>().sharedMaterial = spill;

            CreateShape(root, "Mop Bucket", PrimitiveType.Cylinder,
                new Vector3(-2.25f, 0.28f, 3.02f), new Vector3(0.34f, 0.28f, 0.34f), teal, true);
            CreateShape(root, "Mop Handle", PrimitiveType.Cylinder,
                new Vector3(-2.25f, 1.1f, 3.02f), new Vector3(0.035f, 0.82f, 0.035f), metal, true,
                Quaternion.Euler(0f, 0f, -8f));
            CreateText(root, "Cleaning Station Sign", "CLEANUP",
                new Vector3(-2.25f, 0.74f, 3.26f), 0.065f, cream.color, TextAnchor.MiddleCenter);
        }

        private static GameObject CreateObjectiveBeacon(
            Transform root,
            Material amberEmission,
            Material cream)
        {
            GameObject beacon = new("Objective Beacon");
            beacon.transform.SetParent(root, false);
            GameObject diamond = CreateShape(beacon.transform, "Objective Diamond", PrimitiveType.Cube,
                Vector3.zero, new Vector3(0.24f, 0.24f, 0.24f), amberEmission, false,
                Quaternion.Euler(0f, 45f, 45f));
            CreateShape(beacon.transform, "Objective Ring", PrimitiveType.Cylinder,
                new Vector3(0f, -0.28f, 0f), new Vector3(0.42f, 0.025f, 0.42f), amberEmission, false);
            CreateText(beacon.transform, "Objective Next Label", "NEXT",
                new Vector3(0f, 0.33f, 0f), 0.075f, cream.color, TextAnchor.MiddleCenter);
            return beacon;
        }

        private static void ConfigureProductPrefab(
            string prefabPath,
            PrimitiveType primitive,
            Vector3 rootScale,
            Material baseMaterial,
            Material accentMaterial,
            bool isCan)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                DestroyExperienceChildren(root.transform);
                GameObject temporary = GameObject.CreatePrimitive(primitive);
                Mesh mesh = temporary.GetComponent<MeshFilter>().sharedMesh;
                UnityEngine.Object.DestroyImmediate(temporary);
                root.GetComponent<MeshFilter>().sharedMesh = mesh;
                root.GetComponent<Renderer>().sharedMaterial = baseMaterial;
                root.transform.localScale = rootScale;

                if (isCan)
                {
                    CreateShape(root.transform, "Experience Can Band", PrimitiveType.Cylinder,
                        Vector3.zero, new Vector3(1.035f, 0.32f, 1.035f), accentMaterial, false);
                    CreateShape(root.transform, "Experience Can Top", PrimitiveType.Cylinder,
                        new Vector3(0f, 0.97f, 0f), new Vector3(1.01f, 0.025f, 1.01f), accentMaterial, false);
                    CreateShape(root.transform, "Experience Can Bottom", PrimitiveType.Cylinder,
                        new Vector3(0f, -0.97f, 0f), new Vector3(1.01f, 0.025f, 1.01f), accentMaterial, false);
                }
                else
                {
                    CreateShape(root.transform, "Experience Chips Label", PrimitiveType.Cube,
                        new Vector3(0f, -0.03f, -0.53f), new Vector3(0.72f, 0.46f, 0.05f), accentMaterial, false);
                    CreateShape(root.transform, "Experience Chips Top Seam", PrimitiveType.Cube,
                        new Vector3(0f, 0.47f, 0f), new Vector3(1.04f, 0.07f, 1.04f), accentMaterial, false);
                    CreateShape(root.transform, "Experience Chips Bottom Seam", PrimitiveType.Cube,
                        new Vector3(0f, -0.47f, 0f), new Vector3(1.04f, 0.07f, 1.04f), accentMaterial, false);
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CreateStylizedCar(
            Transform parent,
            string name,
            Vector3 position,
            Material bodyMaterial,
            Material trimMaterial)
        {
            GameObject car = new(name);
            car.transform.SetParent(parent, false);
            car.transform.localPosition = position;
            CreateShape(car.transform, "Experience Car Body", PrimitiveType.Cube,
                Vector3.zero, new Vector3(2.25f, 0.58f, 4.1f), bodyMaterial, false);
            CreateShape(car.transform, "Experience Car Cabin", PrimitiveType.Cube,
                new Vector3(0f, 0.54f, 0.25f), new Vector3(1.82f, 0.65f, 2.05f), trimMaterial, false);
            CreateShape(car.transform, "Experience Car Front Light", PrimitiveType.Cube,
                new Vector3(0f, 0.05f, -2.08f), new Vector3(1.45f, 0.15f, 0.05f), trimMaterial, false);

            Vector3[] wheelPositions =
            {
                new(-1.12f, -0.2f, -1.25f),
                new(1.12f, -0.2f, -1.25f),
                new(-1.12f, -0.2f, 1.25f),
                new(1.12f, -0.2f, 1.25f)
            };
            for (int index = 0; index < wheelPositions.Length; index++)
            {
                CreateShape(car.transform, $"Experience Car Wheel {index + 1}", PrimitiveType.Cylinder,
                    wheelPositions[index], new Vector3(0.3f, 0.16f, 0.3f), trimMaterial, false,
                    Quaternion.Euler(0f, 0f, 90f));
            }
        }

        private static Light CreatePointLight(
            Transform parent,
            string name,
            Vector3 position,
            Color color,
            float range,
            float intensity,
            bool shadows)
        {
            GameObject lightObject = new(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            light.shadowStrength = shadows ? 0.62f : 0f;
            return light;
        }

        private static Light CreateSpotLight(
            Transform parent,
            string name,
            Vector3 position,
            Color color,
            float range,
            float intensity,
            float spotAngle)
        {
            GameObject lightObject = new(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = position;
            lightObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.spotAngle = spotAngle;
            light.innerSpotAngle = spotAngle * 0.58f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.72f;
            return light;
        }

        private static GameObject CreateShape(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool collider)
        {
            return CreateShape(
                parent,
                name,
                primitive,
                position,
                scale,
                material,
                collider,
                Quaternion.identity);
        }

        private static GameObject CreateShape(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool collider,
            Quaternion rotation)
        {
            GameObject result = GameObject.CreatePrimitive(primitive);
            result.name = name;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = position;
            result.transform.localRotation = rotation;
            result.transform.localScale = scale;
            Renderer renderer = result.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
            if (!collider)
            {
                foreach (Collider component in result.GetComponents<Collider>())
                {
                    UnityEngine.Object.DestroyImmediate(component);
                }
            }
            return result;
        }

        private static TextMesh CreateText(
            Transform parent,
            string name,
            string text,
            Vector3 position,
            float size,
            Color color,
            TextAnchor anchor,
            Quaternion? rotation = null)
        {
            GameObject textObject = new(name);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = position;
            textObject.transform.localRotation = rotation ?? Quaternion.identity;

            Vector3 parentScale = parent.lossyScale;
            textObject.transform.localScale = new Vector3(
                size / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
                size / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)),
                size / Mathf.Max(0.0001f, Mathf.Abs(parentScale.z)));

            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = anchor;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 64;
            textMesh.characterSize = 0.12f;
            textMesh.color = color;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                textMesh.font = font;
                MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = font.material;
                }
            }
            return textMesh;
        }

        private static Material Material(
            string name,
            Color color,
            float metallic,
            float smoothness)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                                Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EmissiveMaterial(
            string name,
            Color color,
            float intensity)
        {
            Material material = Material(name, color * 0.45f, 0f, 0.35f);
            Color emission = color * intensity;
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", emission);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material TransparentMaterial(
            string name,
            Color color,
            float smoothness)
        {
            Material material = Material(name, color, 0f, smoothness);
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }
            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }
            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }
            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }
            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
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

        private static void SetRendererEnabled(GameObject gameObject, bool enabled)
        {
            foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = enabled;
            }
        }

        private static void DestroyExperienceChildren(Transform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                Transform child = parent.GetChild(index);
                if (child.name.StartsWith(ExperienceChildPrefix, StringComparison.Ordinal))
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void DestroyAllTextChildren(Transform parent)
        {
            TextMesh[] labels = parent.GetComponentsInChildren<TextMesh>(true);
            for (int index = labels.Length - 1; index >= 0; index--)
            {
                if (labels[index] != null && labels[index].transform != parent)
                {
                    UnityEngine.Object.DestroyImmediate(labels[index].gameObject);
                }
            }
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
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

        private static void SetArray<T>(
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
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private readonly struct DeliveryPresentation
        {
            public DeliveryPresentation(Transform lidPivot)
            {
                LidPivot = lidPivot;
            }

            public Transform LidPivot { get; }
        }

        private readonly struct CheckoutPresentation
        {
            public CheckoutPresentation(
                TextMesh displayText,
                GameObject colaProp,
                GameObject chipsProp)
            {
                DisplayText = displayText;
                ColaProp = colaProp;
                ChipsProp = chipsProp;
            }

            public TextMesh DisplayText { get; }
            public GameObject ColaProp { get; }
            public GameObject ChipsProp { get; }
        }

        private readonly struct StoreControlPresentation
        {
            public StoreControlPresentation(
                TextMesh stateText,
                Renderer emissiveRenderer)
            {
                StateText = stateText;
                EmissiveRenderer = emissiveRenderer;
            }

            public TextMesh StateText { get; }
            public Renderer EmissiveRenderer { get; }
        }
    }
}
