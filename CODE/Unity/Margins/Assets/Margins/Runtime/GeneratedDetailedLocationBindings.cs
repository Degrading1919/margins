using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Margins
{
    /// <summary>
    /// Resolves the small set of physical anchors needed by the existing
    /// detailed convenience-store rig from procedural asset metadata. It does
    /// not own customer, employee, inventory, or interaction state.
    /// </summary>
    public sealed class GeneratedDetailedLocationBindings
    {
        private const float ActorHeightMeters = 0f;
        private const float EntranceInsideDistanceMeters = 0.8f;
        private const float ExitInsideDistanceMeters = 0.35f;
        private const float ReachableInteriorOffsetMeters = 1.5f;
        private const float QueueEndInsetMeters = 0.35f;
        private const float QueueSpacingMeters = 0.75f;
        private const int QueuePointCount = 4;

        public sealed class FixtureBinding
        {
            internal FixtureBinding(
                GeneratedAssetPlacement placement,
                Transform instance,
                ProceduralAssetComponent metadata)
            {
                Placement = placement;
                Instance = instance;
                Metadata = metadata;
            }

            public GeneratedAssetPlacement Placement { get; }
            public Transform Instance { get; }
            public ProceduralAssetComponent Metadata { get; }
        }

        private GeneratedDetailedLocationBindings()
        {
        }

        public Transform EntrancePoint { get; private set; }
        public Transform ExitPoint { get; private set; }
        public Transform CheckoutCustomerPoint { get; private set; }
        public IReadOnlyList<Transform> CheckoutItemPoints { get; private set; }
        public IReadOnlyList<Transform> BrowsePoints { get; private set; }
        public IReadOnlyList<Transform> QueuePoints { get; private set; }
        public Transform CashierWorkPoint { get; private set; }
        public Transform DeliveryWorkPoint { get; private set; }
        public Transform DeliveryDropPoint { get; private set; }
        public Transform ManagerWorkPoint { get; private set; }
        public Transform CleaningPoint { get; private set; }
        public Transform ToolRestPoint { get; private set; }
        public Transform OperatingControlPoint { get; private set; }
        public FixtureBinding CheckoutFixture { get; private set; }
        public IReadOnlyList<FixtureBinding> DisplayFixtures { get; private set; }
        public GeneratedCommercialUnit SelectedUnit { get; private set; }

        public IEnumerable<Transform> RequiredNavigationPoints
        {
            get
            {
                yield return EntrancePoint;
                foreach (Transform browse in BrowsePoints)
                {
                    yield return browse;
                }
                foreach (Transform queue in QueuePoints)
                {
                    yield return queue;
                }
                yield return CheckoutCustomerPoint;
                yield return CashierWorkPoint;
                yield return DeliveryWorkPoint;
                yield return DeliveryDropPoint;
                yield return ManagerWorkPoint;
                yield return CleaningPoint;
                yield return ToolRestPoint;
                yield return OperatingControlPoint;
                yield return ExitPoint;
            }
        }

        public static bool TryCreate(
            ProceduralCommercialBuilding building,
            int requiredDisplayFixtureCount,
            out GeneratedDetailedLocationBindings bindings,
            out string error)
        {
            bindings = null;
            error = null;
            ProceduralGenerationResult result = building?.LastResult;
            if (building == null || result == null || !result.Success ||
                result.Units.Count != 1 || requiredDisplayFixtureCount <= 0)
            {
                error =
                    "Generated detailed operation requires one valid selected commercial unit and at least one authored display fixture.";
                return false;
            }

            GeneratedOpening entrance = result.Openings.FirstOrDefault(value =>
                value.Kind == ProceduralOpeningKind.PrimaryEntrance);
            GeneratedWallSegment entranceWall = entrance == null
                ? null
                : result.Walls.FirstOrDefault(value => string.Equals(
                    value.WallId,
                    entrance.WallId,
                    StringComparison.Ordinal));
            if (entranceWall == null)
            {
                error = "Generated detailed operation requires a primary entrance wall.";
                return false;
            }
            GeneratedOpening serviceDoor = result.Openings.FirstOrDefault(value =>
                value.Kind == ProceduralOpeningKind.ServiceDoor);
            GeneratedWallSegment serviceWall = serviceDoor == null
                ? null
                : result.Walls.FirstOrDefault(value => string.Equals(
                    value.WallId,
                    serviceDoor.WallId,
                    StringComparison.Ordinal));
            if (serviceWall == null)
            {
                error = "Generated detailed operation requires a service entrance wall.";
                return false;
            }

            Dictionary<string, Transform> instances = building
                .GetComponentsInChildren<Transform>(true)
                .Where(value => result.Placements.Any(placement =>
                    string.Equals(
                        placement.PlacementId,
                        value.name,
                        StringComparison.Ordinal)))
                .GroupBy(value => value.name, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.First(),
                    StringComparer.Ordinal);

            List<FixtureBinding> displays = new();
            foreach (GeneratedAssetPlacement placement in result.Placements
                         .Where(value =>
                             value.Category == ProceduralAssetCategory.Display)
                         .OrderBy(value => value.PlacementId, StringComparer.Ordinal))
            {
                if (!TryCreateFixtureBinding(
                        placement,
                        instances,
                        out FixtureBinding display) ||
                    !TryGetClearance(
                        placement,
                        ProceduralClearanceKind.CustomerInteraction,
                        out _))
                {
                    continue;
                }
                displays.Add(display);
            }

            GeneratedAssetPlacement checkoutPlacement = result.Placements
                .Where(value =>
                    value.Category == ProceduralAssetCategory.Transaction)
                .OrderBy(value => value.PlacementId, StringComparer.Ordinal)
                .FirstOrDefault(value =>
                    HasCapability(value, "payment") &&
                    TryGetClearance(
                        value,
                        ProceduralClearanceKind.Queue,
                        out _) &&
                    TryGetClearance(
                        value,
                        ProceduralClearanceKind.StaffInteraction,
                        out _));
            GeneratedAssetPlacement receivingPlacement = result.Placements
                .OrderBy(value => value.PlacementId, StringComparer.Ordinal)
                .FirstOrDefault(value => HasCapability(value, "receiving"));
            GeneratedAssetPlacement storagePlacement = result.Placements
                .OrderBy(value => value.PlacementId, StringComparer.Ordinal)
                .FirstOrDefault(value =>
                    HasCapability(value, "inventory-storage"));
            GeneratedAssetPlacement workPlacement = result.Placements
                .OrderBy(value => value.PlacementId, StringComparer.Ordinal)
                .FirstOrDefault(value =>
                    HasCapability(value, "staff-prep") &&
                    TryGetClearance(
                        value,
                        ProceduralClearanceKind.StaffInteraction,
                        out _));

            if (displays.Count < requiredDisplayFixtureCount ||
                checkoutPlacement == null || receivingPlacement == null ||
                storagePlacement == null || workPlacement == null ||
                !TryCreateFixtureBinding(
                    checkoutPlacement,
                    instances,
                    out FixtureBinding checkout) ||
                !TryGetAnyClearance(receivingPlacement, out GeneratedAssetClearance receiving) ||
                !TryGetAnyClearance(storagePlacement, out GeneratedAssetClearance storage) ||
                !TryGetClearance(
                    workPlacement,
                    ProceduralClearanceKind.StaffInteraction,
                    out GeneratedAssetClearance work) ||
                !TryGetClearance(
                    checkoutPlacement,
                    ProceduralClearanceKind.Queue,
                    out GeneratedAssetClearance queue) ||
                !TryGetClearance(
                    checkoutPlacement,
                    ProceduralClearanceKind.StaffInteraction,
                    out GeneratedAssetClearance cashier))
            {
                error =
                    "The generated convenience recipe is missing metadata-backed display, payment, receiving, storage, or staff interaction space.";
                return false;
            }

            List<FixtureBinding> selectedDisplays = displays
                .Take(requiredDisplayFixtureCount)
                .ToList();
            List<Transform> browsePoints = new(selectedDisplays.Count);
            foreach (FixtureBinding display in selectedDisplays)
            {
                TryGetClearance(
                    display.Placement,
                    ProceduralClearanceKind.CustomerInteraction,
                    out GeneratedAssetClearance clearance);
                browsePoints.Add(CreatePlanPoint(
                    building.transform,
                    $"Detailed Browse {display.Placement.PlacementId}",
                    clearance.BoundsMeters.Center,
                    ActorHeightMeters));
            }

            Vector2 entranceFace =
                entranceWall.InteriorFacePointAt(entrance.OffsetMeters);
            Vector2 entranceInside = entranceFace +
                                     entranceWall.InwardNormal *
                                     EntranceInsideDistanceMeters;
            Vector2 exitInside = entranceFace +
                                 entranceWall.InwardNormal *
                                 ExitInsideDistanceMeters;
            Vector2 serviceInside =
                serviceWall.InteriorFacePointAt(serviceDoor.OffsetMeters) +
                serviceWall.InwardNormal * EntranceInsideDistanceMeters;
            Vector2 receivingWork = serviceInside +
                                    serviceWall.InwardNormal *
                                    ReachableInteriorOffsetMeters;
            List<Transform> queuePoints = CreateQueuePoints(
                building.transform,
                queue.BoundsMeters,
                checkoutPlacement.PhysicalBoundsMeters.Center);

            Vector2 storageDirection = DirectionFromAssetToClearance(
                storagePlacement,
                storage);
            Vector2 unitCenter = result.Units[0].BoundsMeters.Center;
            Vector2 managerPoint = MoveToward(
                work.BoundsMeters.Center,
                unitCenter,
                ReachableInteriorOffsetMeters);
            Vector2 storagePoint = MoveToward(
                storage.BoundsMeters.Center,
                unitCenter,
                ReachableInteriorOffsetMeters);
            Vector2 storageSide = new(-storageDirection.y, storageDirection.x);

            ProceduralAssetComponent checkoutMetadata = checkout.Metadata;
            float counterTop = Mathf.Max(
                0.8f,
                checkoutMetadata.PhysicalSizeMeters.y + 0.06f);
            Transform[] itemPoints =
            {
                CreatePlacementPoint(
                    building.transform,
                    checkoutPlacement,
                    "Detailed Checkout Item 1",
                    new Vector3(-0.23f, counterTop, 0f)),
                CreatePlacementPoint(
                    building.transform,
                    checkoutPlacement,
                    "Detailed Checkout Item 2",
                    new Vector3(0.23f, counterTop, 0f))
            };

            bindings = new GeneratedDetailedLocationBindings
            {
                SelectedUnit = result.Units[0],
                EntrancePoint = CreatePlanPoint(
                    building.transform,
                    "Detailed Customer Entrance",
                    entranceInside,
                    ActorHeightMeters),
                ExitPoint = CreatePlanPoint(
                    building.transform,
                    "Detailed Customer Exit",
                    exitInside,
                    ActorHeightMeters),
                CheckoutCustomerPoint = queuePoints[0],
                CheckoutItemPoints = itemPoints,
                BrowsePoints = browsePoints,
                QueuePoints = queuePoints,
                CashierWorkPoint = CreatePlanPoint(
                    building.transform,
                    "Detailed Cashier Work",
                    cashier.BoundsMeters.Center,
                    ActorHeightMeters),
                DeliveryWorkPoint = CreatePlanPoint(
                    building.transform,
                    "Detailed Receiving Work",
                    receivingWork,
                    ActorHeightMeters),
                DeliveryDropPoint = CreatePlanPoint(
                    building.transform,
                    "Detailed Delivery Drop",
                    serviceInside,
                    ActorHeightMeters),
                ManagerWorkPoint = CreatePlanPoint(
                    building.transform,
                    "Detailed Manager Work",
                    managerPoint,
                    ActorHeightMeters),
                CleaningPoint = CreatePlanPoint(
                    building.transform,
                    "Detailed Cleaning Task",
                    receivingWork,
                    0.04f),
                ToolRestPoint = CreatePlanPoint(
                    building.transform,
                    "Detailed Cleaning Tool",
                    storagePoint + storageSide * 0.35f,
                    ActorHeightMeters),
                OperatingControlPoint = CreatePlanPoint(
                    building.transform,
                    "Detailed Store Control",
                    storagePoint - storageSide * 0.35f,
                    1.1f),
                CheckoutFixture = checkout,
                DisplayFixtures = selectedDisplays
            };

            if (bindings.RequiredNavigationPoints.Any(value => value == null))
            {
                bindings = null;
                error = "Generated detailed bindings contain a missing navigation point.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryCreateFixtureBinding(
            GeneratedAssetPlacement placement,
            IReadOnlyDictionary<string, Transform> instances,
            out FixtureBinding binding)
        {
            binding = null;
            if (placement == null ||
                !instances.TryGetValue(
                    placement.PlacementId,
                    out Transform instance))
            {
                return false;
            }

            ProceduralAssetComponent metadata =
                instance.GetComponent<ProceduralAssetComponent>();
            if (metadata == null || !metadata.TryValidateConfiguration(out _))
            {
                return false;
            }

            binding = new FixtureBinding(placement, instance, metadata);
            return true;
        }

        private static bool HasCapability(
            GeneratedAssetPlacement placement,
            string capability)
        {
            return placement?.Prefab != null &&
                   placement.Prefab.TryGetComponent(
                       out ProceduralAssetComponent metadata) &&
                   metadata.HasCapability(capability);
        }

        private static bool TryGetClearance(
            GeneratedAssetPlacement placement,
            ProceduralClearanceKind kind,
            out GeneratedAssetClearance clearance)
        {
            clearance = placement?.Clearances
                .Where(value => value != null && value.Required &&
                                value.Kind == kind)
                .OrderBy(value => value.ClearanceId, StringComparer.Ordinal)
                .FirstOrDefault();
            return clearance != null;
        }

        private static bool TryGetAnyClearance(
            GeneratedAssetPlacement placement,
            out GeneratedAssetClearance clearance)
        {
            clearance = placement?.Clearances
                .Where(value => value != null && value.Required)
                .OrderBy(value => value.Kind)
                .ThenBy(value => value.ClearanceId, StringComparer.Ordinal)
                .FirstOrDefault();
            return clearance != null;
        }

        private static List<Transform> CreateQueuePoints(
            Transform parent,
            PlanRect bounds,
            Vector2 counterCenter)
        {
            Vector2 direction = bounds.Center - counterCenter;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = bounds.Depth >= bounds.Width
                    ? Vector2.up
                    : Vector2.right;
            }
            else if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                direction = new Vector2(Mathf.Sign(direction.x), 0f);
            }
            else
            {
                direction = new Vector2(0f, Mathf.Sign(direction.y));
            }

            float extent = ExtentAlong(bounds, direction);
            float usableLength = Mathf.Max(0f, extent * 2f -
                                                QueueEndInsetMeters * 2f);
            Vector2 nearest = bounds.Center - direction *
                Mathf.Max(0f, extent - QueueEndInsetMeters);
            List<Transform> points = new(QueuePointCount);
            for (int index = 0; index < QueuePointCount; index++)
            {
                float distance = Mathf.Min(
                    usableLength,
                    index * QueueSpacingMeters);
                points.Add(CreatePlanPoint(
                    parent,
                    $"Detailed Checkout Queue {index + 1}",
                    nearest + direction * distance,
                    ActorHeightMeters));
            }
            return points;
        }

        private static Vector2 DirectionFromAssetToClearance(
            GeneratedAssetPlacement placement,
            GeneratedAssetClearance clearance)
        {
            Vector2 direction = clearance.BoundsMeters.Center -
                                placement.PhysicalBoundsMeters.Center;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = clearance.BoundsMeters.Depth >=
                            clearance.BoundsMeters.Width
                    ? Vector2.up
                    : Vector2.right;
            }
            return direction.normalized;
        }

        private static float ExtentAlong(PlanRect bounds, Vector2 direction)
        {
            return Mathf.Abs(direction.x) * bounds.Width * 0.5f +
                   Mathf.Abs(direction.y) * bounds.Depth * 0.5f;
        }

        private static Vector2 MoveToward(
            Vector2 start,
            Vector2 destination,
            float distance)
        {
            Vector2 delta = destination - start;
            return delta.sqrMagnitude <= distance * distance
                ? destination
                : start + delta.normalized * distance;
        }

        private static Transform CreatePlanPoint(
            Transform parent,
            string name,
            Vector2 localPlanPosition,
            float heightMeters)
        {
            GameObject marker = new(name);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = new Vector3(
                localPlanPosition.x,
                heightMeters,
                localPlanPosition.y);
            return marker.transform;
        }

        private static Transform CreatePlacementPoint(
            Transform parent,
            GeneratedAssetPlacement placement,
            string name,
            Vector3 placementLocalPosition)
        {
            Quaternion rotation = Quaternion.Euler(
                0f,
                placement.YawDegrees,
                0f);
            Vector3 local = placement.LocalPositionMeters +
                            rotation * placementLocalPosition;
            GameObject marker = new(name);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = local;
            marker.transform.localRotation = rotation;
            return marker.transform;
        }
    }
}
