using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Margins
{
    /// <summary>
    /// Runtime/editor host for a generated graybox commercial building. Existing
    /// first-store state and persistence do not depend on this component.
    /// </summary>
    public sealed class ProceduralCommercialBuilding : MonoBehaviour
    {
        private const string GeneratedRootName = "Generated Procedural Layout";

        [SerializeField] private ProceduralBuildingRequest request;
        [SerializeField] private bool generateOnStart = true;
        [SerializeField, HideInInspector] private string lastSignature;
        [SerializeField, TextArea(2, 8)] private string lastGenerationSummary;

        private ProceduralGenerationResult lastResult;

        public ProceduralBuildingRequest Request => request;
        public ProceduralGenerationResult LastResult => lastResult;
        public string LastSignature => lastSignature;
        public string LastGenerationSummary => lastGenerationSummary;

        public void Configure(ProceduralBuildingRequest configuration, bool autoGenerate)
        {
            request = configuration;
            generateOnStart = autoGenerate;
        }

        public bool TryGenerate(out string error)
        {
            ClearGenerated();
            bool generated = ProceduralCommercialGenerator.TryGenerate(
                request,
                out lastResult);
            if (!generated)
            {
                lastSignature = null;
                lastGenerationSummary = string.Join(
                    "\n",
                    lastResult.Diagnostics.Select(item =>
                        $"[{item.Severity}] {item.Code}: {item.Message}"));
                error = lastGenerationSummary;
                return false;
            }

            lastSignature = lastResult.CanonicalSignature();
            lastGenerationSummary =
                $"Seed {lastResult.Seed} | {lastResult.Archetype} | " +
                $"{lastResult.Footprint.Kind} | {lastResult.StoryCount} story | " +
                $"{lastResult.Zones.Count} zones | {lastResult.Placements.Count} assets | " +
                $"score {lastResult.SoftScore:0.0} | {lastSignature}";
            Render(lastResult);
            error = null;
            return true;
        }

        public bool RegenerateWithSeed(int seed, out string error)
        {
            if (request == null)
            {
                error = "Procedural generation request is missing.";
                return false;
            }

            request = new ProceduralBuildingRequest(
                seed,
                request.Archetype,
                request.FootprintKind,
                request.WidthFeet,
                request.DepthFeet,
                request.CornerExposure,
                request.AssetRegistry,
                request.BusinessRecipe);
            return TryGenerate(out error);
        }

        public void ClearGenerated()
        {
            Transform existing = transform.Find(GeneratedRootName);
            if (existing == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(existing.gameObject);
            }
            else
            {
                DestroyImmediate(existing.gameObject);
            }
        }

        private void Start()
        {
            if (generateOnStart && transform.Find(GeneratedRootName) == null)
            {
                TryGenerate(out _);
            }
        }

        private void Render(ProceduralGenerationResult result)
        {
            GameObject generatedRoot = new(GeneratedRootName);
            generatedRoot.transform.SetParent(transform, false);
            Transform shellRoot = CreateContainer("Shell", generatedRoot.transform);
            Transform partitionRoot = CreateContainer("Partitions", generatedRoot.transform);
            Transform openingRoot = CreateContainer("Opening Placeholders", generatedRoot.transform);
            Transform assetRoot = CreateContainer("Business Assets", generatedRoot.transform);
            Transform debugRoot = CreateContainer("Debug Overlays", generatedRoot.transform);

            RenderSlabs(result, shellRoot);
            RenderExteriorWalls(result, shellRoot);
            RenderPartitions(result, partitionRoot);
            RenderOpeningPanels(result, openingRoot);
            RenderPlacements(result, assetRoot);
            RenderDebugOverlays(result, debugRoot);
        }

        private static Transform CreateContainer(string name, Transform parent)
        {
            GameObject container = new(name);
            container.transform.SetParent(parent, false);
            return container.transform;
        }

        private static void RenderSlabs(
            ProceduralGenerationResult result,
            Transform shellRoot)
        {
            int index = 0;
            foreach (PlanRect rectangle in result.Footprint.Rectangles)
            {
                CreateBox(
                    $"Floor Slab {++index:00}",
                    shellRoot,
                    new Vector3(
                        rectangle.Center.x,
                        -CommercialGenerationDimensions.FloorSlabThicknessMeters * 0.5f,
                        rectangle.Center.y),
                    new Vector3(
                        rectangle.Width,
                        CommercialGenerationDimensions.FloorSlabThicknessMeters,
                        rectangle.Depth),
                    Quaternion.identity,
                    new Color(0.35f, 0.37f, 0.4f),
                    true);
                CreateBox(
                    $"Ceiling {index:00}",
                    shellRoot,
                    new Vector3(
                        rectangle.Center.x,
                        result.Profile.finishedCeilingHeightMeters,
                        rectangle.Center.y),
                    new Vector3(rectangle.Width, 0.06f, rectangle.Depth),
                    Quaternion.identity,
                    new Color(0.72f, 0.72f, 0.68f),
                    false);
            }

            if (result.StoryCount > 1)
            {
                for (int story = 1; story < result.StoryCount; story++)
                {
                    float height = result.Profile.structuralHeightMeters +
                                   CommercialGenerationDimensions.Feet((story - 1) * 10f);
                    int section = 0;
                    foreach (PlanRect rectangle in result.Footprint.Rectangles)
                    {
                        CreateBox(
                            $"Upper Floor Slab {story:00}-{++section:00}",
                            shellRoot,
                            new Vector3(
                                rectangle.Center.x,
                                height,
                                rectangle.Center.y),
                            new Vector3(
                                rectangle.Width,
                                CommercialGenerationDimensions.FloorSlabThicknessMeters,
                                rectangle.Depth),
                            Quaternion.identity,
                            new Color(0.3f, 0.32f, 0.35f),
                            true);
                    }
                }
            }
        }

        private static void RenderExteriorWalls(
            ProceduralGenerationResult result,
            Transform shellRoot)
        {
            foreach (GeneratedWallSegment wall in result.Walls)
            {
                List<GeneratedOpening> openings = result.Openings
                    .Where(item => item.WallId == wall.WallId &&
                                   item.Kind != ProceduralOpeningKind.InteriorDoor)
                    .OrderBy(item => item.StartOffsetMeters)
                    .ThenBy(item => item.SillMeters)
                    .ToList();
                List<float> horizontalCuts = new() { 0f, wall.LengthMeters };
                List<float> verticalCuts = new() { 0f, wall.HeightMeters };
                foreach (GeneratedOpening opening in openings)
                {
                    horizontalCuts.Add(Mathf.Clamp(
                        opening.StartOffsetMeters, 0f, wall.LengthMeters));
                    horizontalCuts.Add(Mathf.Clamp(
                        opening.EndOffsetMeters, 0f, wall.LengthMeters));
                    verticalCuts.Add(Mathf.Clamp(
                        opening.SillMeters, 0f, wall.HeightMeters));
                    verticalCuts.Add(Mathf.Clamp(
                        opening.SillMeters + opening.HeightMeters,
                        0f,
                        wall.HeightMeters));
                }

                horizontalCuts = horizontalCuts.Distinct().OrderBy(item => item).ToList();
                verticalCuts = verticalCuts.Distinct().OrderBy(item => item).ToList();
                for (int horizontal = 0;
                     horizontal < horizontalCuts.Count - 1;
                     horizontal++)
                {
                    float start = horizontalCuts[horizontal];
                    float end = horizontalCuts[horizontal + 1];
                    float centerOffset = (start + end) * 0.5f;
                    for (int vertical = 0;
                         vertical < verticalCuts.Count - 1;
                         vertical++)
                    {
                        float bottom = verticalCuts[vertical];
                        float top = verticalCuts[vertical + 1];
                        float centerHeight = (bottom + top) * 0.5f;
                        bool insideOpening = openings.Any(opening =>
                            centerOffset > opening.StartOffsetMeters &&
                            centerOffset < opening.EndOffsetMeters &&
                            centerHeight > opening.SillMeters &&
                            centerHeight < opening.SillMeters + opening.HeightMeters);
                        if (!insideOpening)
                        {
                            AddWallSpan(
                                wall,
                                start,
                                end,
                                bottom,
                                top - bottom,
                                shellRoot);
                        }
                    }
                }
            }
        }

        private static void AddWallSpan(
            GeneratedWallSegment wall,
            float startOffset,
            float endOffset,
            float bottom,
            float height,
            Transform parent)
        {
            float length = endOffset - startOffset;
            if (length <= 0.001f || height <= 0.001f)
            {
                return;
            }

            Vector2 center = wall.PointAt((startOffset + endOffset) * 0.5f);
            CreateBox(
                $"{wall.WallId} {startOffset:0.00}-{endOffset:0.00}",
                parent,
                new Vector3(center.x, bottom + height * 0.5f, center.y),
                new Vector3(length, height, wall.ThicknessMeters),
                Quaternion.Euler(0f, YawForNormal(wall.OutwardNormal), 0f),
                WallColor(wall.FrontageRole),
                true);
        }

        private static void RenderPartitions(
            ProceduralGenerationResult result,
            Transform partitionRoot)
        {
            foreach (GeneratedPartition partition in result.Partitions)
            {
                Vector2 direction = (partition.EndMeters - partition.StartMeters).normalized;
                float cursor = 0f;
                foreach (GeneratedOpening opening in partition.Openings.OrderBy(item =>
                             item.StartOffsetMeters))
                {
                    AddPartitionSpan(partition, direction, cursor,
                        opening.StartOffsetMeters, 0f, partition.HeightMeters,
                        partitionRoot);
                    float top = opening.SillMeters + opening.HeightMeters;
                    AddPartitionSpan(partition, direction, opening.StartOffsetMeters,
                        opening.EndOffsetMeters, top, partition.HeightMeters - top,
                        partitionRoot);
                    cursor = opening.EndOffsetMeters;
                }
                AddPartitionSpan(partition, direction, cursor,
                    partition.LengthMeters, 0f, partition.HeightMeters,
                    partitionRoot);
            }
        }

        private static void AddPartitionSpan(
            GeneratedPartition partition,
            Vector2 direction,
            float startOffset,
            float endOffset,
            float bottom,
            float height,
            Transform parent)
        {
            float length = endOffset - startOffset;
            if (length <= 0.001f || height <= 0.001f)
            {
                return;
            }

            Vector2 center = partition.StartMeters +
                             direction * ((startOffset + endOffset) * 0.5f);
            Vector2 normal = new(direction.y, -direction.x);
            CreateBox(
                $"{partition.PartitionId} {startOffset:0.00}-{endOffset:0.00}",
                parent,
                new Vector3(center.x, bottom + height * 0.5f, center.y),
                new Vector3(length, height, partition.ThicknessMeters),
                Quaternion.Euler(0f, YawForNormal(normal), 0f),
                new Color(0.62f, 0.58f, 0.52f),
                true);
        }

        private static void RenderOpeningPanels(
            ProceduralGenerationResult result,
            Transform openingRoot)
        {
            foreach (GeneratedOpening opening in result.Openings.Where(item =>
                         item.Kind == ProceduralOpeningKind.StorefrontWindow ||
                         item.Kind == ProceduralOpeningKind.UpperFloorWindow))
            {
                GeneratedWallSegment wall = result.Walls.FirstOrDefault(item =>
                    item.WallId == opening.WallId);
                if (wall == null)
                {
                    continue;
                }

                Vector2 center = wall.PointAt(opening.OffsetMeters) +
                                 wall.OutwardNormal *
                                 (CommercialGenerationDimensions.WallFaceOffset(
                                      wall.ThicknessMeters) + 0.012f);
                CreateBox(
                    opening.OpeningId,
                    openingRoot,
                    new Vector3(
                        center.x,
                        opening.SillMeters + opening.HeightMeters * 0.5f,
                        center.y),
                    new Vector3(opening.WidthMeters, opening.HeightMeters, 0.02f),
                    Quaternion.Euler(0f, YawForNormal(wall.OutwardNormal), 0f),
                    opening.Kind == ProceduralOpeningKind.UpperFloorWindow
                        ? new Color(0.18f, 0.28f, 0.36f)
                        : new Color(0.15f, 0.45f, 0.58f),
                    false);
            }
        }

        private void RenderPlacements(
            ProceduralGenerationResult result,
            Transform assetRoot)
        {
            Dictionary<string, Transform> instances = new(StringComparer.Ordinal);
            foreach (GeneratedAssetPlacement placement in result.Placements)
            {
                Transform parent = assetRoot;
                if (!string.IsNullOrEmpty(placement.ParentPlacementId) &&
                    instances.TryGetValue(placement.ParentPlacementId, out Transform parentInstance))
                {
                    parent = parentInstance;
                }

                GameObject instance = placement.Prefab == null
                    ? new GameObject(placement.PlacementId)
                    : Instantiate(placement.Prefab, parent);
                instance.name = placement.PlacementId;
                instance.transform.SetPositionAndRotation(
                    transform.TransformPoint(placement.LocalPositionMeters),
                    transform.rotation * Quaternion.Euler(0f, placement.YawDegrees, 0f));
                instances.Add(placement.PlacementId, instance.transform);

                ProceduralAssetComponent metadata =
                    instance.GetComponent<ProceduralAssetComponent>();
                if (metadata != null)
                {
                    ApplyColor(instance, metadata.DebugColor);
                }
            }
        }

        private static void RenderDebugOverlays(
            ProceduralGenerationResult result,
            Transform debugRoot)
        {
            foreach (GeneratedFunctionalZone zone in result.Zones)
            {
                CreateBox(
                    $"Zone {zone.ZoneType}",
                    debugRoot,
                    new Vector3(zone.BoundsMeters.Center.x, 0.015f,
                        zone.BoundsMeters.Center.y),
                    new Vector3(zone.BoundsMeters.Width, 0.02f,
                        zone.BoundsMeters.Depth),
                    Quaternion.identity,
                    ZoneColor(zone.ZoneType),
                    false);
            }

            foreach (GeneratedCirculationPath path in result.Circulation)
            {
                CreateBox(
                    $"Route {path.PathId}",
                    debugRoot,
                    new Vector3(path.BoundsMeters.Center.x, 0.035f,
                        path.BoundsMeters.Center.y),
                    new Vector3(path.BoundsMeters.Width, 0.025f,
                        path.BoundsMeters.Depth),
                    Quaternion.identity,
                    path.CustomerRoute
                        ? new Color(0.15f, 0.8f, 0.35f)
                        : new Color(0.95f, 0.65f, 0.15f),
                    false);
            }

            ProceduralGenerationDebugView debugView =
                debugRoot.parent.parent.GetComponent<ProceduralGenerationDebugView>();
            debugRoot.gameObject.SetActive(debugView == null || debugView.ShowRuntimeOverlays);
        }

        private static GameObject CreateBox(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Color color,
            bool keepCollider)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localRotation = localRotation;
            box.transform.localScale = localScale;
            if (!keepCollider)
            {
                Collider collider = box.GetComponent<Collider>();
                if (Application.isPlaying)
                {
                    Destroy(collider);
                }
                else
                {
                    DestroyImmediate(collider);
                }
            }
            ApplyColor(box, color);
            return box;
        }

        private static void ApplyColor(GameObject root, Color color)
        {
            MaterialPropertyBlock properties = new();
            properties.SetColor("_BaseColor", color);
            properties.SetColor("_Color", color);
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.SetPropertyBlock(properties);
            }
        }

        private static float YawForNormal(Vector2 normal)
        {
            return Mathf.Atan2(normal.x, normal.y) * Mathf.Rad2Deg;
        }

        private static Color WallColor(FrontageRole role)
        {
            switch (role)
            {
                case FrontageRole.PrimaryPublic:
                    return new Color(0.68f, 0.58f, 0.45f);
                case FrontageRole.SecondaryPublic:
                    return new Color(0.58f, 0.54f, 0.48f);
                case FrontageRole.RearService:
                    return new Color(0.42f, 0.43f, 0.45f);
                default:
                    return new Color(0.48f, 0.45f, 0.42f);
            }
        }

        private static Color ZoneColor(FunctionalZoneType zone)
        {
            switch (zone)
            {
                case FunctionalZoneType.Public:
                    return new Color(0.2f, 0.55f, 0.85f, 0.24f);
                case FunctionalZoneType.Transaction:
                    return new Color(0.95f, 0.65f, 0.15f, 0.3f);
                case FunctionalZoneType.Work:
                    return new Color(0.75f, 0.3f, 0.2f, 0.3f);
                case FunctionalZoneType.Storage:
                    return new Color(0.5f, 0.35f, 0.75f, 0.3f);
                case FunctionalZoneType.Service:
                    return new Color(0.45f, 0.45f, 0.48f, 0.3f);
                case FunctionalZoneType.Staff:
                    return new Color(0.25f, 0.7f, 0.65f, 0.3f);
                default:
                    return new Color(0.2f, 0.8f, 0.35f, 0.3f);
            }
        }
    }

}
