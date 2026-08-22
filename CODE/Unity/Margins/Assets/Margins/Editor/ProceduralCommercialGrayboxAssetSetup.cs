using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Margins.Editor
{
    public static class ProceduralCommercialGrayboxAssetSetup
    {
        public const string RegistryAssetPath =
            "Assets/Margins/Content/Procedural/Resources/ProceduralAssetRegistry.asset";
        public const string ConvenienceRecipePath =
            "Assets/Margins/Content/Procedural/Resources/Recipes/GrayboxConvenienceRecipe.asset";
        public const string LaundromatRecipePath =
            "Assets/Margins/Content/Procedural/Resources/Recipes/GrayboxLaundromatRecipe.asset";
        public const string FuelKioskRecipePath =
            "Assets/Margins/Content/Procedural/Resources/Recipes/GrayboxFuelKioskRecipe.asset";
        public const string DemoPrefabPath =
            "Assets/Margins/Content/Procedural/Resources/ProceduralCommercialGrayboxDemo.prefab";

        private const string PlaceholderDirectory =
            "Assets/Margins/Content/Procedural/Placeholders";

        [MenuItem("Margins/Procedural Generation/Rebuild Graybox MVP Content")]
        public static void Apply()
        {
            EnsureDirectory(PlaceholderDirectory);
            EnsureDirectory("Assets/Margins/Content/Procedural/Resources/Recipes");

            List<GameObject> prefabs = PlaceholderSpecs()
                .Select(CreateOrUpdatePlaceholderPrefab)
                .ToList();
            ProceduralAssetRegistry registry = LoadOrCreate<ProceduralAssetRegistry>(
                RegistryAssetPath);
            registry.Configure(prefabs.ToArray());
            EditorUtility.SetDirty(registry);

            ProceduralBusinessRecipe convenience =
                LoadOrCreate<ProceduralBusinessRecipe>(ConvenienceRecipePath);
            ConfigureConvenienceRecipe(convenience);
            EditorUtility.SetDirty(convenience);

            ProceduralBusinessRecipe laundromat =
                LoadOrCreate<ProceduralBusinessRecipe>(LaundromatRecipePath);
            ConfigureLaundromatRecipe(laundromat);
            EditorUtility.SetDirty(laundromat);

            ProceduralBusinessRecipe fuelKiosk =
                LoadOrCreate<ProceduralBusinessRecipe>(FuelKioskRecipePath);
            ConfigureFuelKioskRecipe(fuelKiosk);
            EditorUtility.SetDirty(fuelKiosk);

            CreateOrUpdateDemoPrefab(registry, convenience);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            bool registryValid = registry.TryValidate(true, out string registryError);
            bool convenienceValid = convenience.TryValidate(out string convenienceError);
            bool laundromatValid = laundromat.TryValidate(out string laundromatError);
            bool fuelValid = fuelKiosk.TryValidate(out string fuelError);
            if (!registryValid || !convenienceValid || !laundromatValid || !fuelValid)
            {
                throw new InvalidOperationException(
                    registryError ?? convenienceError ?? laundromatError ?? fuelError);
            }

            Debug.Log(
                $"Procedural graybox content rebuilt: {prefabs.Count} placeholder prefabs, " +
                "one registry, three business recipes, and one debug demo prefab.");
        }

        private static IEnumerable<PlaceholderSpec> PlaceholderSpecs()
        {
            yield return new PlaceholderSpec(
                "PROC_Display_Gondola_4ft",
                "proc-display-gondola-4ft",
                ProceduralAssetCategory.Display,
                new[] { "dry-merchandise" },
                ProceduralMountingMode.Floor,
                ProceduralAccessMode.Customer,
                ProceduralInteractionSide.Front,
                ProceduralWallRelationship.Avoided,
                ProceduralEnvironment.IndoorOnly,
                ProceduralAssemblyBehavior.BankOrRepeatedRow,
                ProceduralResizeBehavior.Repeatable,
                ProceduralPivotConvention.FloorBottomCenter,
                SizeFeet(4f, 6f, 2f),
                0f,
                null,
                new[] { FrontClearance("customer-access", ProceduralClearanceKind.CustomerInteraction, 4f, 2f, 3f) },
                RepeatableAnchors(4f, "display-run"),
                Array.Empty<SocketSpec>(),
                new Color(0.92f, 0.58f, 0.18f));

            yield return new PlaceholderSpec(
                "PROC_Display_ColdCase_6ft",
                "proc-display-cold-case-6ft",
                ProceduralAssetCategory.Display,
                new[] { "cold-merchandise" },
                ProceduralMountingMode.Floor,
                ProceduralAccessMode.Customer,
                ProceduralInteractionSide.Front,
                ProceduralWallRelationship.StronglyPreferred,
                ProceduralEnvironment.IndoorOnly,
                ProceduralAssemblyBehavior.LinearRun,
                ProceduralResizeBehavior.Repeatable,
                ProceduralPivotConvention.FloorBottomCenter,
                SizeFeet(6f, 7f, 3f),
                0f,
                null,
                new[] { FrontClearance("customer-access", ProceduralClearanceKind.CustomerInteraction, 6f, 3f, 3f) },
                RepeatableAnchors(6f, "cold-case-run"),
                Array.Empty<SocketSpec>(),
                new Color(0.2f, 0.62f, 0.82f));

            yield return new PlaceholderSpec(
                "PROC_Transaction_CheckoutCounter_6ft",
                "proc-transaction-checkout-counter-6ft",
                ProceduralAssetCategory.Transaction,
                new[] { "payment" },
                ProceduralMountingMode.Floor,
                ProceduralAccessMode.CustomerAndStaff,
                ProceduralInteractionSide.Front | ProceduralInteractionSide.Back,
                ProceduralWallRelationship.Neutral,
                ProceduralEnvironment.IndoorOnly,
                ProceduralAssemblyBehavior.Independent,
                ProceduralResizeBehavior.Fixed,
                ProceduralPivotConvention.FloorBottomCenter,
                SizeFeet(6f, 3.5f, 2.5f),
                0f,
                null,
                new[]
                {
                    FrontClearance("customer-queue", ProceduralClearanceKind.Queue, 6f, 2.5f, 3f),
                    BackClearance("staff-access", ProceduralClearanceKind.StaffInteraction, 6f, 2.5f, 3f)
                },
                Array.Empty<AnchorSpec>(),
                new[]
                {
                    new SocketSpec(
                        "register-mount",
                        "countertop-equipment",
                        new Vector3(0.37f, CommercialGenerationDimensions.Feet(3.5f), 0.08f),
                        Vector3.zero)
                },
                new Color(0.9f, 0.28f, 0.22f));

            yield return new PlaceholderSpec(
                "PROC_WorkSurface_PrepCounter_6ft",
                "proc-work-surface-prep-counter-6ft",
                ProceduralAssetCategory.WorkSurface,
                new[] { "staff-prep" },
                ProceduralMountingMode.Floor,
                ProceduralAccessMode.Staff,
                ProceduralInteractionSide.Front,
                ProceduralWallRelationship.StronglyPreferred,
                ProceduralEnvironment.IndoorOnly,
                ProceduralAssemblyBehavior.LinearRun,
                ProceduralResizeBehavior.Repeatable,
                ProceduralPivotConvention.FloorBottomCenter,
                SizeFeet(6f, 3f, 2.5f),
                0f,
                null,
                new[] { FrontClearance("staff-work", ProceduralClearanceKind.StaffInteraction, 6f, 2.5f, 3f) },
                RepeatableAnchors(6f, "work-counter-run"),
                new[]
                {
                    new SocketSpec(
                        "countertop-mount",
                        "countertop-equipment",
                        new Vector3(0.37f, CommercialGenerationDimensions.Feet(3f), 0.11f),
                        Vector3.zero)
                },
                new Color(0.72f, 0.42f, 0.18f));

            yield return new PlaceholderSpec(
                "PROC_ProcessStation_CommercialWasher",
                "proc-process-station-commercial-washer",
                ProceduralAssetCategory.ProcessStation,
                new[] { "laundry-wash" },
                ProceduralMountingMode.Floor,
                ProceduralAccessMode.Customer,
                ProceduralInteractionSide.Front,
                ProceduralWallRelationship.StronglyPreferred,
                ProceduralEnvironment.IndoorOnly,
                ProceduralAssemblyBehavior.BankOrRepeatedRow,
                ProceduralResizeBehavior.Repeatable,
                ProceduralPivotConvention.FloorBottomCenter,
                SizeFeet(2.5f, 3.5f, 3f),
                0f,
                null,
                new[] { FrontClearance("customer-operation", ProceduralClearanceKind.CustomerInteraction, 2.5f, 3f, 3f) },
                RepeatableAnchors(2.5f, "laundry-bank"),
                Array.Empty<SocketSpec>(),
                new Color(0.18f, 0.56f, 0.9f));

            yield return new PlaceholderSpec(
                "PROC_ProcessStation_CommercialDryer",
                "proc-process-station-commercial-dryer",
                ProceduralAssetCategory.ProcessStation,
                new[] { "laundry-dry" },
                ProceduralMountingMode.Floor,
                ProceduralAccessMode.Customer,
                ProceduralInteractionSide.Front,
                ProceduralWallRelationship.StronglyPreferred,
                ProceduralEnvironment.IndoorOnly,
                ProceduralAssemblyBehavior.BankOrRepeatedRow,
                ProceduralResizeBehavior.Repeatable,
                ProceduralPivotConvention.FloorBottomCenter,
                SizeFeet(2.5f, 6f, 3f),
                0f,
                null,
                new[] { FrontClearance("customer-operation", ProceduralClearanceKind.CustomerInteraction, 2.5f, 3f, 3f) },
                RepeatableAnchors(2.5f, "laundry-bank"),
                Array.Empty<SocketSpec>(),
                new Color(0.24f, 0.46f, 0.78f));

            yield return new PlaceholderSpec(
                "PROC_ProcessStation_EspressoModule",
                "proc-process-station-espresso-module",
                ProceduralAssetCategory.ProcessStation,
                new[] { "coffee-prep" },
                ProceduralMountingMode.Socket,
                ProceduralAccessMode.Staff,
                ProceduralInteractionSide.Front,
                ProceduralWallRelationship.Neutral,
                ProceduralEnvironment.IndoorOnly,
                ProceduralAssemblyBehavior.SocketChild,
                ProceduralResizeBehavior.Fixed,
                ProceduralPivotConvention.FloorBottomCenter,
                SizeFeet(2f, 2f, 1.5f),
                0f,
                "countertop-equipment",
                Array.Empty<ProceduralClearanceDefinition>(),
                Array.Empty<AnchorSpec>(),
                Array.Empty<SocketSpec>(),
                new Color(0.3f, 0.22f, 0.18f));

            yield return new PlaceholderSpec(
                "PROC_ProcessStation_FuelPump",
                "proc-process-station-fuel-pump",
                ProceduralAssetCategory.ProcessStation,
                new[] { "fuel-dispense" },
                ProceduralMountingMode.ExteriorPad,
                ProceduralAccessMode.Customer,
                ProceduralInteractionSide.Front | ProceduralInteractionSide.Back,
                ProceduralWallRelationship.Neutral,
                ProceduralEnvironment.OutdoorOnly,
                ProceduralAssemblyBehavior.BankOrRepeatedRow,
                ProceduralResizeBehavior.Fixed,
                ProceduralPivotConvention.FloorBottomCenter,
                SizeFeet(3f, 5f, 2f),
                0f,
                null,
                new[]
                {
                    FrontClearance("customer-front", ProceduralClearanceKind.CustomerInteraction, 3f, 2f, 3f),
                    BackClearance("customer-back", ProceduralClearanceKind.CustomerInteraction, 3f, 2f, 3f)
                },
                Array.Empty<AnchorSpec>(),
                Array.Empty<SocketSpec>(),
                new Color(0.85f, 0.2f, 0.2f));

            yield return new PlaceholderSpec(
                "PROC_UseStation_ArcadeCabinet",
                "proc-use-station-arcade-cabinet",
                ProceduralAssetCategory.UseStation,
                new[] { "customer-use" },
                ProceduralMountingMode.Floor,
                ProceduralAccessMode.Customer,
                ProceduralInteractionSide.Front,
                ProceduralWallRelationship.Preferred,
                ProceduralEnvironment.IndoorOnly,
                ProceduralAssemblyBehavior.BankOrRepeatedRow,
                ProceduralResizeBehavior.Repeatable,
                ProceduralPivotConvention.FloorBottomCenter,
                SizeFeet(2.5f, 6f, 3f),
                0f,
                null,
                new[] { FrontClearance("customer-use", ProceduralClearanceKind.Occupancy, 2.5f, 3f, 3f) },
                RepeatableAnchors(2.5f, "use-station-bank"),
                Array.Empty<SocketSpec>(),
                new Color(0.65f, 0.22f, 0.8f));

            yield return new PlaceholderSpec(
                "PROC_Storage_StockRack_4ft",
                "proc-storage-stock-rack-4ft",
                ProceduralAssetCategory.Storage,
                new[] { "inventory-storage" },
                ProceduralMountingMode.Floor,
                ProceduralAccessMode.Staff,
                ProceduralInteractionSide.Front,
                ProceduralWallRelationship.StronglyPreferred,
                ProceduralEnvironment.IndoorOnly,
                ProceduralAssemblyBehavior.LinearRun,
                ProceduralResizeBehavior.Repeatable,
                ProceduralPivotConvention.FloorBottomCenter,
                SizeFeet(4f, 7f, 2f),
                0f,
                null,
                new[] { FrontClearance("staff-access", ProceduralClearanceKind.StaffInteraction, 4f, 2f, 3f) },
                RepeatableAnchors(4f, "storage-run"),
                Array.Empty<SocketSpec>(),
                new Color(0.32f, 0.48f, 0.28f));

            yield return new PlaceholderSpec(
                "PROC_Service_ReceivingTable_6ft",
                "proc-service-receiving-table-6ft",
                ProceduralAssetCategory.Service,
                new[] { "receiving" },
                ProceduralMountingMode.Floor,
                ProceduralAccessMode.Staff,
                ProceduralInteractionSide.Front,
                ProceduralWallRelationship.Preferred,
                ProceduralEnvironment.IndoorOnly,
                ProceduralAssemblyBehavior.Independent,
                ProceduralResizeBehavior.Fixed,
                ProceduralPivotConvention.FloorBottomCenter,
                SizeFeet(6f, 3f, 3f),
                0f,
                null,
                new[] { FrontClearance("receiving-access", ProceduralClearanceKind.Maintenance, 6f, 3f, 3f) },
                Array.Empty<AnchorSpec>(),
                Array.Empty<SocketSpec>(),
                new Color(0.42f, 0.44f, 0.47f));

            yield return new PlaceholderSpec(
                "PROC_Amenity_WaitingBench_6ft",
                "proc-amenity-waiting-bench-6ft",
                ProceduralAssetCategory.Amenity,
                new[] { "customer-seating" },
                ProceduralMountingMode.Floor,
                ProceduralAccessMode.Customer,
                ProceduralInteractionSide.Front,
                ProceduralWallRelationship.Preferred,
                ProceduralEnvironment.IndoorOnly,
                ProceduralAssemblyBehavior.Independent,
                ProceduralResizeBehavior.Fixed,
                ProceduralPivotConvention.FloorBottomCenter,
                SizeFeet(6f, 3f, 2f),
                0f,
                null,
                new[] { FrontClearance("occupancy", ProceduralClearanceKind.Occupancy, 6f, 2f, 3f) },
                Array.Empty<AnchorSpec>(),
                Array.Empty<SocketSpec>(),
                new Color(0.24f, 0.65f, 0.58f));

            yield return new PlaceholderSpec(
                "PROC_Decor_BulletinBoard_4ft",
                "proc-decor-bulletin-board-4ft",
                ProceduralAssetCategory.Decor,
                new[] { "wall-dressing" },
                ProceduralMountingMode.Wall,
                ProceduralAccessMode.Customer,
                ProceduralInteractionSide.None,
                ProceduralWallRelationship.Required,
                ProceduralEnvironment.IndoorOnly,
                ProceduralAssemblyBehavior.Independent,
                ProceduralResizeBehavior.Fixed,
                ProceduralPivotConvention.WallMountPlaneCenter,
                SizeFeet(4f, 3f, 0.125f),
                CommercialGenerationDimensions.Feet(5f),
                null,
                Array.Empty<ProceduralClearanceDefinition>(),
                Array.Empty<AnchorSpec>(),
                Array.Empty<SocketSpec>(),
                new Color(0.86f, 0.72f, 0.28f));

            yield return new PlaceholderSpec(
                "PROC_Decor_CeilingLight_2x4ft",
                "proc-decor-ceiling-light-2x4ft",
                ProceduralAssetCategory.Decor,
                new[] { "illumination" },
                ProceduralMountingMode.Ceiling,
                ProceduralAccessMode.CustomerAndStaff,
                ProceduralInteractionSide.None,
                ProceduralWallRelationship.Neutral,
                ProceduralEnvironment.IndoorOnly,
                ProceduralAssemblyBehavior.Independent,
                ProceduralResizeBehavior.Fixed,
                ProceduralPivotConvention.CeilingAttachmentCenter,
                SizeFeet(4f, 0.25f, 2f),
                0f,
                null,
                Array.Empty<ProceduralClearanceDefinition>(),
                Array.Empty<AnchorSpec>(),
                Array.Empty<SocketSpec>(),
                new Color(1f, 0.95f, 0.65f));
        }

        private static GameObject CreateOrUpdatePlaceholderPrefab(PlaceholderSpec spec)
        {
            GameObject root = new(spec.PrefabName);
            BoxCollider rootCollider = root.AddComponent<BoxCollider>();
            rootCollider.size = spec.SizeMeters;
            rootCollider.center = ColliderCenter(spec);

            GameObject visualRoot = new("Visual");
            visualRoot.transform.SetParent(root.transform, false);
            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            primitive.name = "Primitive Geometry (Replaceable)";
            primitive.transform.SetParent(visualRoot.transform, false);
            primitive.transform.localPosition = VisualCenter(spec);
            primitive.transform.localScale = spec.SizeMeters;
            UnityEngine.Object.DestroyImmediate(primitive.GetComponent<Collider>());

            foreach (AnchorSpec anchor in spec.Anchors)
            {
                GameObject anchorObject = new(anchor.DisplayName);
                anchorObject.transform.SetParent(root.transform, false);
                anchorObject.transform.localPosition = anchor.LocalPosition;
                anchorObject.transform.localEulerAngles = anchor.LocalEulerAngles;
                anchorObject.AddComponent<ProceduralAnchorComponent>()
                    .Configure(anchor.AnchorId, anchor.CompatibilityTag);
            }

            foreach (SocketSpec socket in spec.Sockets)
            {
                GameObject socketObject = new(ToDisplayName(socket.SocketId));
                socketObject.transform.SetParent(root.transform, false);
                socketObject.transform.localPosition = socket.LocalPosition;
                socketObject.transform.localEulerAngles = socket.LocalEulerAngles;
                socketObject.AddComponent<ProceduralSocketComponent>()
                    .Configure(socket.SocketId, socket.CompatibilityTag);
            }

            ProceduralAssetComponent metadata =
                root.AddComponent<ProceduralAssetComponent>();
            metadata.Configure(
                spec.AssetId,
                spec.Category,
                spec.Capabilities,
                spec.MountingModes,
                spec.AccessMode,
                spec.InteractionSides,
                spec.WallRelationship,
                spec.Environment,
                spec.AssemblyBehavior,
                spec.ResizeBehavior,
                spec.PivotConvention,
                spec.SizeMeters,
                spec.MountHeightMeters,
                spec.RequiredSocketCompatibility,
                spec.Clearances,
                visualRoot.transform,
                rootCollider,
                spec.Color);

            string path = $"{PlaceholderDirectory}/{spec.PrefabName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Failed to save placeholder prefab '{path}'.");
            }

            return prefab;
        }

        private static void ConfigureConvenienceRecipe(ProceduralBusinessRecipe recipe)
        {
            recipe.Configure(
                "graybox-convenience-store",
                20f,
                30f,
                1200f,
                true,
                new[]
                {
                    Zone(FunctionalZoneType.Public, true, false, 600f, 900f),
                    Zone(FunctionalZoneType.Transaction, true, false, 80f, 120f),
                    Zone(FunctionalZoneType.Work, true, false, 90f, 140f),
                    Zone(FunctionalZoneType.Storage, true, true, 100f, 160f),
                    Zone(FunctionalZoneType.Service, true, true, 70f, 100f),
                    Zone(FunctionalZoneType.Circulation, true, false, 0f, 0f)
                },
                new[]
                {
                    Request("dry-display", ProceduralAssetCategory.Display, "dry-merchandise", 2, 4, 6, true, FunctionalZoneType.Public, 100),
                    Request("cold-display", ProceduralAssetCategory.Display, "cold-merchandise", 1, 1, 2, true, FunctionalZoneType.Public, 95),
                    Request("checkout", ProceduralAssetCategory.Transaction, "payment", 1, 1, 1, true, FunctionalZoneType.Transaction, 120),
                    Request("prep-counter", ProceduralAssetCategory.WorkSurface, "staff-prep", 1, 1, 2, true, FunctionalZoneType.Work, 110),
                    Request("coffee-module", ProceduralAssetCategory.ProcessStation, "coffee-prep", 1, 1, 1, true, FunctionalZoneType.Work, 90),
                    Request("stock-rack", ProceduralAssetCategory.Storage, "inventory-storage", 1, 2, 3, true, FunctionalZoneType.Storage, 100),
                    Request("receiving", ProceduralAssetCategory.Service, "receiving", 1, 1, 1, true, FunctionalZoneType.Service, 100),
                    Request("customer-use", ProceduralAssetCategory.UseStation, "customer-use", 0, 1, 2, false, FunctionalZoneType.Public, 20),
                    Request("seating", ProceduralAssetCategory.Amenity, "customer-seating", 0, 1, 3, false, FunctionalZoneType.Public, 10),
                    Request("wall-decor", ProceduralAssetCategory.Decor, "wall-dressing", 0, 2, 4, false, FunctionalZoneType.Public, 5),
                    Request("ceiling-lights", ProceduralAssetCategory.Decor, "illumination", 0, 2, 4, false, FunctionalZoneType.Public, 4)
                });
        }

        private static void ConfigureLaundromatRecipe(ProceduralBusinessRecipe recipe)
        {
            recipe.Configure(
                "graybox-laundromat",
                40f,
                40f,
                1900f,
                true,
                new[]
                {
                    Zone(FunctionalZoneType.Public, true, false, 900f, 1400f),
                    Zone(FunctionalZoneType.Transaction, true, false, 80f, 120f),
                    Zone(FunctionalZoneType.Storage, true, true, 100f, 140f),
                    Zone(FunctionalZoneType.Service, true, true, 100f, 140f),
                    Zone(FunctionalZoneType.Circulation, true, false, 0f, 0f)
                },
                new[]
                {
                    Request("wash-bank", ProceduralAssetCategory.ProcessStation, "laundry-wash", 4, 6, 10, true, FunctionalZoneType.Public, 120),
                    Request("dry-bank", ProceduralAssetCategory.ProcessStation, "laundry-dry", 4, 6, 10, true, FunctionalZoneType.Public, 115),
                    Request("checkout", ProceduralAssetCategory.Transaction, "payment", 1, 1, 1, true, FunctionalZoneType.Transaction, 110),
                    Request("stock-rack", ProceduralAssetCategory.Storage, "inventory-storage", 1, 1, 2, true, FunctionalZoneType.Storage, 100),
                    Request("receiving", ProceduralAssetCategory.Service, "receiving", 1, 1, 1, true, FunctionalZoneType.Service, 95),
                    Request("seating", ProceduralAssetCategory.Amenity, "customer-seating", 1, 2, 4, true, FunctionalZoneType.Public, 20),
                    Request("wall-decor", ProceduralAssetCategory.Decor, "wall-dressing", 0, 2, 4, false, FunctionalZoneType.Public, 5),
                    Request("ceiling-lights", ProceduralAssetCategory.Decor, "illumination", 0, 3, 6, false, FunctionalZoneType.Public, 4)
                });
        }

        private static void ConfigureFuelKioskRecipe(ProceduralBusinessRecipe recipe)
        {
            recipe.Configure(
                "graybox-fuel-kiosk",
                20f,
                25f,
                600f,
                true,
                new[]
                {
                    Zone(FunctionalZoneType.Public, true, false, 300f, 500f),
                    Zone(FunctionalZoneType.Transaction, true, false, 80f, 100f),
                    Zone(FunctionalZoneType.Service, true, true, 100f, 140f),
                    Zone(FunctionalZoneType.Circulation, true, false, 0f, 0f)
                },
                new[]
                {
                    Request("checkout", ProceduralAssetCategory.Transaction, "payment", 1, 1, 1, true, FunctionalZoneType.Transaction, 110),
                    Request("fuel-pumps", ProceduralAssetCategory.ProcessStation, "fuel-dispense", 2, 4, 6, true, FunctionalZoneType.Service, 100, ProceduralEnvironment.OutdoorOnly),
                    Request("receiving", ProceduralAssetCategory.Service, "receiving", 1, 1, 1, true, FunctionalZoneType.Service, 90),
                    Request("wall-decor", ProceduralAssetCategory.Decor, "wall-dressing", 0, 1, 2, false, FunctionalZoneType.Public, 5)
                });
        }

        private static void CreateOrUpdateDemoPrefab(
            ProceduralAssetRegistry registry,
            ProceduralBusinessRecipe recipe)
        {
            GameObject root = new("Procedural Commercial Graybox Demo");
            ProceduralCommercialBuilding building =
                root.AddComponent<ProceduralCommercialBuilding>();
            root.AddComponent<ProceduralGenerationDebugView>();
            building.Configure(
                new ProceduralBuildingRequest(
                    1337,
                    CommercialBuildingArchetype.StandaloneSmallCommercial,
                    OrthogonalFootprintKind.Rectangle,
                    48,
                    50,
                    true,
                    registry,
                    recipe),
                true);
            PrefabUtility.SaveAsPrefabAsset(root, DemoPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static ProceduralZoneRequest Zone(
            FunctionalZoneType type,
            bool required,
            bool enclosed,
            float minimum,
            float preferred)
        {
            return new ProceduralZoneRequest(type, required, enclosed, minimum, preferred);
        }

        private static ProceduralAssetRequest Request(
            string id,
            ProceduralAssetCategory category,
            string capability,
            int minimum,
            int preferred,
            int maximum,
            bool required,
            FunctionalZoneType zone,
            int priority,
            ProceduralEnvironment environment = ProceduralEnvironment.IndoorOnly)
        {
            return new ProceduralAssetRequest(
                id,
                category,
                new[] { capability },
                minimum,
                preferred,
                maximum,
                required,
                zone,
                priority,
                environment);
        }

        private static ProceduralClearanceDefinition FrontClearance(
            string id,
            ProceduralClearanceKind kind,
            float assetWidthFeet,
            float assetDepthFeet,
            float clearanceDepthFeet)
        {
            return new ProceduralClearanceDefinition(
                id,
                kind,
                new Vector3(
                    0f,
                    CommercialGenerationDimensions.Feet(1f),
                    CommercialGenerationDimensions.Feet(
                        assetDepthFeet * 0.5f + clearanceDepthFeet * 0.5f)),
                SizeFeet(assetWidthFeet, 2f, clearanceDepthFeet));
        }

        private static ProceduralClearanceDefinition BackClearance(
            string id,
            ProceduralClearanceKind kind,
            float assetWidthFeet,
            float assetDepthFeet,
            float clearanceDepthFeet)
        {
            return new ProceduralClearanceDefinition(
                id,
                kind,
                new Vector3(
                    0f,
                    CommercialGenerationDimensions.Feet(1f),
                    -CommercialGenerationDimensions.Feet(
                        assetDepthFeet * 0.5f + clearanceDepthFeet * 0.5f)),
                SizeFeet(assetWidthFeet, 2f, clearanceDepthFeet));
        }

        private static AnchorSpec[] RepeatableAnchors(
            float widthFeet,
            string compatibility)
        {
            float halfWidth = CommercialGenerationDimensions.Feet(widthFeet) * 0.5f;
            return new[]
            {
                new AnchorSpec(
                    "LeftExtension",
                    "left-extension",
                    compatibility,
                    new Vector3(-halfWidth, 0f, 0f),
                    Vector3.zero),
                new AnchorSpec(
                    "RightExtension",
                    "right-extension",
                    compatibility,
                    new Vector3(halfWidth, 0f, 0f),
                    Vector3.zero)
            };
        }

        private static Vector3 SizeFeet(float width, float height, float depth)
        {
            return new Vector3(
                CommercialGenerationDimensions.Feet(width),
                CommercialGenerationDimensions.Feet(height),
                CommercialGenerationDimensions.Feet(depth));
        }

        private static Vector3 ColliderCenter(PlaceholderSpec spec)
        {
            switch (spec.PivotConvention)
            {
                case ProceduralPivotConvention.WallMountPlaneCenter:
                    return new Vector3(0f, 0f, spec.SizeMeters.z * 0.5f);
                case ProceduralPivotConvention.CeilingAttachmentCenter:
                    return new Vector3(0f, -spec.SizeMeters.y * 0.5f, 0f);
                default:
                    return new Vector3(0f, spec.SizeMeters.y * 0.5f, 0f);
            }
        }

        private static Vector3 VisualCenter(PlaceholderSpec spec)
        {
            return ColliderCenter(spec);
        }

        private static T LoadOrCreate<T>(string path)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureDirectory(string assetDirectory)
        {
            string absolute = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                assetDirectory));
            Directory.CreateDirectory(absolute);
            AssetDatabase.Refresh();
        }

        private static string ToDisplayName(string stableId)
        {
            return string.Join(
                " ",
                stableId.Split('-').Select(part =>
                    string.IsNullOrEmpty(part)
                        ? part
                        : char.ToUpperInvariant(part[0]) + part.Substring(1)));
        }

        private sealed class PlaceholderSpec
        {
            public readonly string PrefabName;
            public readonly string AssetId;
            public readonly ProceduralAssetCategory Category;
            public readonly string[] Capabilities;
            public readonly ProceduralMountingMode MountingModes;
            public readonly ProceduralAccessMode AccessMode;
            public readonly ProceduralInteractionSide InteractionSides;
            public readonly ProceduralWallRelationship WallRelationship;
            public readonly ProceduralEnvironment Environment;
            public readonly ProceduralAssemblyBehavior AssemblyBehavior;
            public readonly ProceduralResizeBehavior ResizeBehavior;
            public readonly ProceduralPivotConvention PivotConvention;
            public readonly Vector3 SizeMeters;
            public readonly float MountHeightMeters;
            public readonly string RequiredSocketCompatibility;
            public readonly ProceduralClearanceDefinition[] Clearances;
            public readonly AnchorSpec[] Anchors;
            public readonly SocketSpec[] Sockets;
            public readonly Color Color;

            public PlaceholderSpec(
                string prefabName,
                string assetId,
                ProceduralAssetCategory category,
                string[] capabilities,
                ProceduralMountingMode mountingModes,
                ProceduralAccessMode accessMode,
                ProceduralInteractionSide interactionSides,
                ProceduralWallRelationship wallRelationship,
                ProceduralEnvironment environment,
                ProceduralAssemblyBehavior assemblyBehavior,
                ProceduralResizeBehavior resizeBehavior,
                ProceduralPivotConvention pivotConvention,
                Vector3 sizeMeters,
                float mountHeightMeters,
                string requiredSocketCompatibility,
                ProceduralClearanceDefinition[] clearances,
                AnchorSpec[] anchors,
                SocketSpec[] sockets,
                Color color)
            {
                PrefabName = prefabName;
                AssetId = assetId;
                Category = category;
                Capabilities = capabilities;
                MountingModes = mountingModes;
                AccessMode = accessMode;
                InteractionSides = interactionSides;
                WallRelationship = wallRelationship;
                Environment = environment;
                AssemblyBehavior = assemblyBehavior;
                ResizeBehavior = resizeBehavior;
                PivotConvention = pivotConvention;
                SizeMeters = sizeMeters;
                MountHeightMeters = mountHeightMeters;
                RequiredSocketCompatibility = requiredSocketCompatibility;
                Clearances = clearances;
                Anchors = anchors;
                Sockets = sockets;
                Color = color;
            }
        }

        private readonly struct AnchorSpec
        {
            public readonly string DisplayName;
            public readonly string AnchorId;
            public readonly string CompatibilityTag;
            public readonly Vector3 LocalPosition;
            public readonly Vector3 LocalEulerAngles;

            public AnchorSpec(
                string displayName,
                string anchorId,
                string compatibilityTag,
                Vector3 localPosition,
                Vector3 localEulerAngles)
            {
                DisplayName = displayName;
                AnchorId = anchorId;
                CompatibilityTag = compatibilityTag;
                LocalPosition = localPosition;
                LocalEulerAngles = localEulerAngles;
            }
        }

        private readonly struct SocketSpec
        {
            public readonly string SocketId;
            public readonly string CompatibilityTag;
            public readonly Vector3 LocalPosition;
            public readonly Vector3 LocalEulerAngles;

            public SocketSpec(
                string socketId,
                string compatibilityTag,
                Vector3 localPosition,
                Vector3 localEulerAngles)
            {
                SocketId = socketId;
                CompatibilityTag = compatibilityTag;
                LocalPosition = localPosition;
                LocalEulerAngles = localEulerAngles;
            }
        }
    }
}
