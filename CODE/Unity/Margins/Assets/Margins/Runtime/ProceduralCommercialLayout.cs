using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Margins
{
    [Serializable]
    public sealed class ProceduralDiagnostic
    {
        [SerializeField] private string code;
        [SerializeField] private ProceduralDiagnosticSeverity severity;
        [SerializeField] private string message;

        public string Code => code;
        public ProceduralDiagnosticSeverity Severity => severity;
        public string Message => message;

        public ProceduralDiagnostic(
            string code,
            ProceduralDiagnosticSeverity severity,
            string message)
        {
            this.code = code;
            this.severity = severity;
            this.message = message;
        }
    }

    [Serializable]
    public sealed class GeneratedFootprint
    {
        [SerializeField] private OrthogonalFootprintKind kind;
        [SerializeField] private int widthFeet;
        [SerializeField] private int depthFeet;
        [SerializeField] private List<PlanRect> rectangles = new();

        public OrthogonalFootprintKind Kind => kind;
        public int WidthFeet => widthFeet;
        public int DepthFeet => depthFeet;
        public IReadOnlyList<PlanRect> Rectangles => rectangles;
        public float AreaSquareMeters => rectangles.Sum(item => item.Area);
        public PlanRect Bounds => new(
            0f,
            0f,
            CommercialGenerationDimensions.Feet(widthFeet),
            CommercialGenerationDimensions.Feet(depthFeet));

        public GeneratedFootprint(
            OrthogonalFootprintKind kind,
            int widthFeet,
            int depthFeet,
            IEnumerable<PlanRect> rectangles)
        {
            this.kind = kind;
            this.widthFeet = widthFeet;
            this.depthFeet = depthFeet;
            this.rectangles = new List<PlanRect>(rectangles ?? Array.Empty<PlanRect>());
        }

        public bool Contains(Vector2 point, float tolerance = 0.0001f)
        {
            foreach (PlanRect rectangle in rectangles)
            {
                if (rectangle.Contains(point, tolerance))
                {
                    return true;
                }
            }

            return false;
        }

        public bool Contains(PlanRect candidate, float tolerance = 0.0001f)
        {
            if (!candidate.IsValid)
            {
                return false;
            }

            Vector2[] requiredPoints =
            {
                new(candidate.MinX, candidate.MinZ),
                new(candidate.MaxX, candidate.MinZ),
                new(candidate.MinX, candidate.MaxZ),
                new(candidate.MaxX, candidate.MaxZ),
                candidate.Center
            };
            foreach (Vector2 point in requiredPoints)
            {
                if (!Contains(point, tolerance))
                {
                    return false;
                }
            }

            float sample = CommercialGenerationDimensions.OpeningIncrementMeters;
            for (float z = candidate.MinZ; z <= candidate.MaxZ + tolerance; z += sample)
            {
                float clampedZ = Mathf.Min(z, candidate.MaxZ);
                for (float x = candidate.MinX; x <= candidate.MaxX + tolerance; x += sample)
                {
                    if (!Contains(new Vector2(Mathf.Min(x, candidate.MaxX), clampedZ), tolerance))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool IsContiguous()
        {
            if (widthFeet <= 0 || depthFeet <= 0 || rectangles.Count == 0)
            {
                return false;
            }

            bool[,] occupied = new bool[widthFeet, depthFeet];
            int occupiedCount = 0;
            for (int z = 0; z < depthFeet; z++)
            {
                for (int x = 0; x < widthFeet; x++)
                {
                    Vector2 center = new(
                        CommercialGenerationDimensions.Feet(x + 0.5f),
                        CommercialGenerationDimensions.Feet(z + 0.5f));
                    occupied[x, z] = Contains(center);
                    if (occupied[x, z])
                    {
                        occupiedCount++;
                    }
                }
            }

            if (occupiedCount == 0)
            {
                return false;
            }

            Queue<Vector2Int> open = new();
            bool[,] visited = new bool[widthFeet, depthFeet];
            for (int z = 0; z < depthFeet && open.Count == 0; z++)
            {
                for (int x = 0; x < widthFeet; x++)
                {
                    if (occupied[x, z])
                    {
                        open.Enqueue(new Vector2Int(x, z));
                        visited[x, z] = true;
                        break;
                    }
                }
            }

            int reached = 0;
            Vector2Int[] directions =
            {
                Vector2Int.left,
                Vector2Int.right,
                Vector2Int.up,
                Vector2Int.down
            };
            while (open.Count > 0)
            {
                Vector2Int current = open.Dequeue();
                reached++;
                foreach (Vector2Int direction in directions)
                {
                    Vector2Int next = current + direction;
                    if (next.x < 0 || next.x >= widthFeet ||
                        next.y < 0 || next.y >= depthFeet ||
                        visited[next.x, next.y] || !occupied[next.x, next.y])
                    {
                        continue;
                    }

                    visited[next.x, next.y] = true;
                    open.Enqueue(next);
                }
            }

            return reached == occupiedCount;
        }
    }

    [Serializable]
    public sealed class GeneratedWallSegment
    {
        [SerializeField] private string wallId;
        [SerializeField] private Vector2 startMeters;
        [SerializeField] private Vector2 endMeters;
        [SerializeField] private Vector2 outwardNormal;
        [SerializeField] private FrontageRole frontageRole;
        [SerializeField] private float thicknessMeters;
        [SerializeField] private float heightMeters;

        public string WallId => wallId;
        public Vector2 StartMeters => startMeters;
        public Vector2 EndMeters => endMeters;
        public Vector2 OutwardNormal => outwardNormal;
        public Vector2 InwardNormal => -outwardNormal;
        public FrontageRole FrontageRole => frontageRole;
        public float ThicknessMeters => thicknessMeters;
        public float HeightMeters => heightMeters;
        public float LengthMeters => Vector2.Distance(startMeters, endMeters);
        public Vector2 Direction => (endMeters - startMeters).normalized;

        public GeneratedWallSegment(
            string wallId,
            Vector2 startMeters,
            Vector2 endMeters,
            Vector2 outwardNormal,
            FrontageRole frontageRole,
            float thicknessMeters,
            float heightMeters)
        {
            this.wallId = wallId;
            this.startMeters = startMeters;
            this.endMeters = endMeters;
            this.outwardNormal = outwardNormal;
            this.frontageRole = frontageRole;
            this.thicknessMeters = thicknessMeters;
            this.heightMeters = heightMeters;
        }

        public Vector2 PointAt(float offsetMeters)
        {
            return startMeters + Direction * offsetMeters;
        }

        public Vector2 InteriorFacePointAt(float offsetMeters)
        {
            return PointAt(offsetMeters) +
                   InwardNormal * CommercialGenerationDimensions.WallFaceOffset(
                       thicknessMeters);
        }
    }

    [Serializable]
    public sealed class GeneratedOpening
    {
        [SerializeField] private string openingId;
        [SerializeField] private string wallId;
        [SerializeField] private string unitId;
        [SerializeField] private ProceduralOpeningKind kind;
        [SerializeField] private float offsetMeters;
        [SerializeField] private float widthMeters;
        [SerializeField] private float heightMeters;
        [SerializeField] private float sillMeters;

        public string OpeningId => openingId;
        public string WallId => wallId;
        public string UnitId => unitId;
        public ProceduralOpeningKind Kind => kind;
        public float OffsetMeters => offsetMeters;
        public float WidthMeters => widthMeters;
        public float HeightMeters => heightMeters;
        public float SillMeters => sillMeters;
        public float StartOffsetMeters => offsetMeters - widthMeters * 0.5f;
        public float EndOffsetMeters => offsetMeters + widthMeters * 0.5f;

        public GeneratedOpening(
            string openingId,
            string wallId,
            string unitId,
            ProceduralOpeningKind kind,
            float offsetMeters,
            float widthMeters,
            float heightMeters,
            float sillMeters)
        {
            this.openingId = openingId;
            this.wallId = wallId;
            this.unitId = unitId;
            this.kind = kind;
            this.offsetMeters = offsetMeters;
            this.widthMeters = widthMeters;
            this.heightMeters = heightMeters;
            this.sillMeters = sillMeters;
        }
    }

    [Serializable]
    public sealed class GeneratedCommercialBay
    {
        [SerializeField] private string bayId;
        [SerializeField] private PlanRect boundsMeters;

        public string BayId => bayId;
        public PlanRect BoundsMeters => boundsMeters;

        public GeneratedCommercialBay(string bayId, PlanRect boundsMeters)
        {
            this.bayId = bayId;
            this.boundsMeters = boundsMeters;
        }
    }

    [Serializable]
    public sealed class GeneratedCommercialUnit
    {
        [SerializeField] private string unitId;
        [SerializeField] private PlanRect boundsMeters;
        [SerializeField] private List<string> sourceBayIds = new();
        [SerializeField] private string businessRecipeId;

        public string UnitId => unitId;
        public PlanRect BoundsMeters => boundsMeters;
        public IReadOnlyList<string> SourceBayIds => sourceBayIds;
        public string BusinessRecipeId => businessRecipeId;

        public GeneratedCommercialUnit(
            string unitId,
            PlanRect boundsMeters,
            IEnumerable<string> sourceBayIds,
            string businessRecipeId)
        {
            this.unitId = unitId;
            this.boundsMeters = boundsMeters;
            this.sourceBayIds = new List<string>(sourceBayIds ?? Array.Empty<string>());
            this.businessRecipeId = businessRecipeId;
        }
    }

    [Serializable]
    public sealed class GeneratedFunctionalZone
    {
        [SerializeField] private string zoneId;
        [SerializeField] private FunctionalZoneType zoneType;
        [SerializeField] private PlanRect boundsMeters;
        [SerializeField] private bool enclosed;

        public string ZoneId => zoneId;
        public FunctionalZoneType ZoneType => zoneType;
        public PlanRect BoundsMeters => boundsMeters;
        public bool Enclosed => enclosed;

        public GeneratedFunctionalZone(
            string zoneId,
            FunctionalZoneType zoneType,
            PlanRect boundsMeters,
            bool enclosed)
        {
            this.zoneId = zoneId;
            this.zoneType = zoneType;
            this.boundsMeters = boundsMeters;
            this.enclosed = enclosed;
        }
    }

    [Serializable]
    public sealed class GeneratedCirculationPath
    {
        [SerializeField] private string pathId;
        [SerializeField] private PlanRect boundsMeters;
        [SerializeField] private float requiredWidthMeters;
        [SerializeField] private bool customerRoute;

        public string PathId => pathId;
        public PlanRect BoundsMeters => boundsMeters;
        public float RequiredWidthMeters => requiredWidthMeters;
        public bool CustomerRoute => customerRoute;

        public GeneratedCirculationPath(
            string pathId,
            PlanRect boundsMeters,
            float requiredWidthMeters,
            bool customerRoute)
        {
            this.pathId = pathId;
            this.boundsMeters = boundsMeters;
            this.requiredWidthMeters = requiredWidthMeters;
            this.customerRoute = customerRoute;
        }
    }

    [Serializable]
    public sealed class GeneratedPartition
    {
        [SerializeField] private string partitionId;
        [SerializeField] private Vector2 startMeters;
        [SerializeField] private Vector2 endMeters;
        [SerializeField] private float thicknessMeters;
        [SerializeField] private float heightMeters;
        [SerializeField] private List<GeneratedOpening> openings = new();

        public string PartitionId => partitionId;
        public Vector2 StartMeters => startMeters;
        public Vector2 EndMeters => endMeters;
        public float ThicknessMeters => thicknessMeters;
        public float HeightMeters => heightMeters;
        public IReadOnlyList<GeneratedOpening> Openings => openings;
        public float LengthMeters => Vector2.Distance(startMeters, endMeters);

        public GeneratedPartition(
            string partitionId,
            Vector2 startMeters,
            Vector2 endMeters,
            float thicknessMeters,
            float heightMeters,
            IEnumerable<GeneratedOpening> openings)
        {
            this.partitionId = partitionId;
            this.startMeters = startMeters;
            this.endMeters = endMeters;
            this.thicknessMeters = thicknessMeters;
            this.heightMeters = heightMeters;
            this.openings = new List<GeneratedOpening>(
                openings ?? Array.Empty<GeneratedOpening>());
        }
    }

    [Serializable]
    public sealed class GeneratedAssetClearance
    {
        [SerializeField] private string clearanceId;
        [SerializeField] private ProceduralClearanceKind kind;
        [SerializeField] private PlanRect boundsMeters;
        [SerializeField] private bool required;

        public string ClearanceId => clearanceId;
        public ProceduralClearanceKind Kind => kind;
        public PlanRect BoundsMeters => boundsMeters;
        public bool Required => required;

        public GeneratedAssetClearance(
            string clearanceId,
            ProceduralClearanceKind kind,
            PlanRect boundsMeters,
            bool required)
        {
            this.clearanceId = clearanceId;
            this.kind = kind;
            this.boundsMeters = boundsMeters;
            this.required = required;
        }
    }

    [Serializable]
    public sealed class GeneratedAssetPlacement
    {
        [SerializeField] private string placementId;
        [SerializeField] private string requestId;
        [SerializeField] private string assetId;
        [SerializeField] private ProceduralAssetCategory category;
        [SerializeField] private FunctionalZoneType hostZone;
        [SerializeField] private ProceduralMountingMode mountingMode;
        [SerializeField] private Vector3 localPositionMeters;
        [SerializeField] private float yawDegrees;
        [SerializeField] private PlanRect physicalBoundsMeters;
        [SerializeField] private List<GeneratedAssetClearance> clearances = new();
        [SerializeField] private string hostWallId;
        [SerializeField] private string parentPlacementId;
        [SerializeField] private string hostSocketId;
        [SerializeField] private GameObject prefab;

        public string PlacementId => placementId;
        public string RequestId => requestId;
        public string AssetId => assetId;
        public ProceduralAssetCategory Category => category;
        public FunctionalZoneType HostZone => hostZone;
        public ProceduralMountingMode MountingMode => mountingMode;
        public Vector3 LocalPositionMeters => localPositionMeters;
        public float YawDegrees => yawDegrees;
        public PlanRect PhysicalBoundsMeters => physicalBoundsMeters;
        public IReadOnlyList<GeneratedAssetClearance> Clearances => clearances;
        public string HostWallId => hostWallId;
        public string ParentPlacementId => parentPlacementId;
        public string HostSocketId => hostSocketId;
        public GameObject Prefab => prefab;

        public GeneratedAssetPlacement(
            string placementId,
            string requestId,
            ProceduralAssetComponent asset,
            FunctionalZoneType hostZone,
            ProceduralMountingMode mountingMode,
            Vector3 localPositionMeters,
            float yawDegrees,
            PlanRect physicalBoundsMeters,
            IEnumerable<GeneratedAssetClearance> clearances,
            string hostWallId = null,
            string parentPlacementId = null,
            string hostSocketId = null)
        {
            this.placementId = placementId;
            this.requestId = requestId;
            assetId = asset.StableAssetId;
            category = asset.PrimaryCategory;
            this.hostZone = hostZone;
            this.mountingMode = mountingMode;
            this.localPositionMeters = localPositionMeters;
            this.yawDegrees = yawDegrees;
            this.physicalBoundsMeters = physicalBoundsMeters;
            this.clearances = new List<GeneratedAssetClearance>(
                clearances ?? Array.Empty<GeneratedAssetClearance>());
            this.hostWallId = hostWallId;
            this.parentPlacementId = parentPlacementId;
            this.hostSocketId = hostSocketId;
            prefab = asset.gameObject;
        }
    }

    [Serializable]
    public sealed class ProceduralGenerationResult
    {
        public const string CurrentGeneratorVersion =
            "procedural-commercial-graybox-v1";

        [SerializeField] private string generatorVersion = CurrentGeneratorVersion;
        [SerializeField] private bool success;
        [SerializeField] private int seed;
        [SerializeField] private CommercialBuildingArchetype archetype;
        [SerializeField] private int storyCount;
        [SerializeField] private CommercialArchetypeProfile profile;
        [SerializeField] private GeneratedFootprint footprint;
        [SerializeField] private List<GeneratedWallSegment> walls = new();
        [SerializeField] private List<GeneratedOpening> openings = new();
        [SerializeField] private List<GeneratedCommercialBay> bays = new();
        [SerializeField] private List<GeneratedCommercialUnit> units = new();
        [SerializeField] private List<GeneratedFunctionalZone> zones = new();
        [SerializeField] private List<GeneratedCirculationPath> circulation = new();
        [SerializeField] private List<GeneratedPartition> partitions = new();
        [SerializeField] private List<GeneratedAssetPlacement> placements = new();
        [SerializeField] private List<ProceduralDiagnostic> diagnostics = new();
        [SerializeField] private float softScore;

        public string GeneratorVersion => generatorVersion;
        public bool Success => success;
        public int Seed => seed;
        public CommercialBuildingArchetype Archetype => archetype;
        public int StoryCount => storyCount;
        public CommercialArchetypeProfile Profile => profile;
        public GeneratedFootprint Footprint => footprint;
        public IReadOnlyList<GeneratedWallSegment> Walls => walls;
        public IReadOnlyList<GeneratedOpening> Openings => openings;
        public IReadOnlyList<GeneratedCommercialBay> Bays => bays;
        public IReadOnlyList<GeneratedCommercialUnit> Units => units;
        public IReadOnlyList<GeneratedFunctionalZone> Zones => zones;
        public IReadOnlyList<GeneratedCirculationPath> Circulation => circulation;
        public IReadOnlyList<GeneratedPartition> Partitions => partitions;
        public IReadOnlyList<GeneratedAssetPlacement> Placements => placements;
        public IReadOnlyList<ProceduralDiagnostic> Diagnostics => diagnostics;
        public float SoftScore => softScore;

        internal ProceduralGenerationResult(
            int seed,
            CommercialBuildingArchetype archetype,
            int storyCount,
            CommercialArchetypeProfile profile)
        {
            this.seed = seed;
            this.archetype = archetype;
            this.storyCount = storyCount;
            this.profile = profile;
        }

        internal void SetFootprint(GeneratedFootprint value)
        {
            footprint = value;
        }

        internal void MarkSuccess(float score)
        {
            success = true;
            softScore = score;
        }

        internal void Reject(string code, string message)
        {
            success = false;
            diagnostics.Add(new ProceduralDiagnostic(
                code,
                ProceduralDiagnosticSeverity.Error,
                message));
        }

        internal void AddDiagnostic(
            string code,
            ProceduralDiagnosticSeverity severity,
            string message)
        {
            diagnostics.Add(new ProceduralDiagnostic(code, severity, message));
        }

        internal List<GeneratedWallSegment> MutableWalls => walls;
        internal List<GeneratedOpening> MutableOpenings => openings;
        internal List<GeneratedCommercialBay> MutableBays => bays;
        internal List<GeneratedCommercialUnit> MutableUnits => units;
        internal List<GeneratedFunctionalZone> MutableZones => zones;
        internal List<GeneratedCirculationPath> MutableCirculation => circulation;
        internal List<GeneratedPartition> MutablePartitions => partitions;
        internal List<GeneratedAssetPlacement> MutablePlacements => placements;

        public bool HasError(string code)
        {
            return diagnostics.Any(item =>
                item.Severity == ProceduralDiagnosticSeverity.Error &&
                string.Equals(item.Code, code, StringComparison.Ordinal));
        }

        public string CanonicalSignature()
        {
            StringBuilder builder = new();
            Append(builder, generatorVersion);
            Append(builder, seed);
            Append(builder, archetype);
            Append(builder, storyCount);
            Append(builder, footprint?.Kind.ToString());
            if (footprint != null)
            {
                foreach (PlanRect rectangle in footprint.Rectangles)
                {
                    Append(builder, rectangle);
                }
            }

            foreach (GeneratedWallSegment wall in walls.OrderBy(item => item.WallId))
            {
                Append(builder, wall.WallId);
                Append(builder, wall.StartMeters);
                Append(builder, wall.EndMeters);
                Append(builder, wall.FrontageRole);
            }

            foreach (GeneratedOpening opening in openings.OrderBy(item => item.OpeningId))
            {
                Append(builder, opening.OpeningId);
                Append(builder, opening.WallId);
                Append(builder, opening.OffsetMeters);
                Append(builder, opening.WidthMeters);
                Append(builder, opening.SillMeters);
            }

            foreach (GeneratedFunctionalZone zone in zones.OrderBy(item => item.ZoneId))
            {
                Append(builder, zone.ZoneId);
                Append(builder, zone.ZoneType);
                Append(builder, zone.BoundsMeters);
            }

            foreach (GeneratedAssetPlacement placement in
                     placements.OrderBy(item => item.PlacementId))
            {
                Append(builder, placement.PlacementId);
                Append(builder, placement.AssetId);
                Append(builder, placement.LocalPositionMeters);
                Append(builder, placement.YawDegrees);
                Append(builder, placement.ParentPlacementId);
                Append(builder, placement.HostSocketId);
            }

            uint hash = 2166136261;
            string canonical = builder.ToString();
            for (int index = 0; index < canonical.Length; index++)
            {
                hash ^= canonical[index];
                hash *= 16777619;
            }

            return hash.ToString("x8", CultureInfo.InvariantCulture);
        }

        private static void Append(StringBuilder builder, object value)
        {
            switch (value)
            {
                case null:
                    builder.Append("null");
                    break;
                case float number:
                    builder.Append(number.ToString("R", CultureInfo.InvariantCulture));
                    break;
                case Vector2 vector2:
                    Append(builder, vector2.x);
                    builder.Append(',');
                    Append(builder, vector2.y);
                    break;
                case Vector3 vector3:
                    Append(builder, vector3.x);
                    builder.Append(',');
                    Append(builder, vector3.y);
                    builder.Append(',');
                    Append(builder, vector3.z);
                    break;
                case PlanRect rectangle:
                    Append(builder, rectangle.MinX);
                    builder.Append(',');
                    Append(builder, rectangle.MinZ);
                    builder.Append(',');
                    Append(builder, rectangle.Width);
                    builder.Append(',');
                    Append(builder, rectangle.Depth);
                    break;
                default:
                    builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                    break;
            }

            builder.Append('|');
        }
    }

    internal struct ProceduralDeterministicRandom
    {
        private uint state;

        public ProceduralDeterministicRandom(int seed)
        {
            state = unchecked((uint)seed);
            if (state == 0)
            {
                state = 0x6d2b79f5;
            }
        }

        public uint NextUInt()
        {
            uint value = state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            state = value;
            return value;
        }

        public int NextInt(int minimumInclusive, int maximumExclusive)
        {
            if (maximumExclusive <= minimumInclusive)
            {
                return minimumInclusive;
            }

            return minimumInclusive +
                   (int)(NextUInt() % (uint)(maximumExclusive - minimumInclusive));
        }

        public float NextFloat()
        {
            return (NextUInt() & 0x00ffffff) / 16777216f;
        }

        public bool NextBool()
        {
            return (NextUInt() & 1) == 0;
        }
    }
}
