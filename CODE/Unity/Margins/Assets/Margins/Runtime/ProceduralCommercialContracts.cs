using System;
using System.Collections.Generic;
using UnityEngine;

namespace Margins
{
    /// <summary>
    /// Authoritative meter-native conversions used by commercial generation.
    /// Human-facing dimensions remain expressed in feet and inches at authoring seams.
    /// </summary>
    public static class CommercialGenerationDimensions
    {
        public const float MetersPerUnityUnit = FixturePlacementGrid.MetersPerUnityUnit;
        public const float MetersPerInch = FixturePlacementGrid.MetersPerInch;
        public const float InchesPerFoot = 12f;
        public const float StructuralIncrementMeters = InchesPerFoot * MetersPerInch;
        public const float OpeningIncrementMeters = FixturePlacementGrid.PlacementIncrementMeters;
        public const float FixturePlacementIncrementMeters =
            FixturePlacementGrid.PlacementIncrementMeters;
        public const float ExteriorWallThicknessMeters = 8f * MetersPerInch;
        public const float InteriorPartitionThicknessMeters = 4.5f * MetersPerInch;
        public const float FloorSlabThicknessMeters = 6f * MetersPerInch;
        public const float StandardDoorWidthMeters = 3f * StructuralIncrementMeters;
        public const float StandardDoorHeightMeters = 7f * StructuralIncrementMeters;
        public const float MinimumOpeningCornerDistanceMeters =
            2f * StructuralIncrementMeters;
        public const float MinimumOpeningSeparationMeters = StructuralIncrementMeters;
        public const float PrimaryRouteWidthMeters = 4f * StructuralIncrementMeters;
        public const float MinimumAisleWidthMeters = 3f * StructuralIncrementMeters;
        public const float EntranceArrivalDepthMeters = 6f * StructuralIncrementMeters;
        public const float EntranceLandingSizeMeters = 4f * StructuralIncrementMeters;

        private const float AlignmentToleranceMeters = 0.0001f;

        public static float Feet(float feet)
        {
            return feet * StructuralIncrementMeters;
        }

        public static float Inches(float inches)
        {
            return inches * MetersPerInch;
        }

        public static float SnapStructural(float meters)
        {
            return Snap(meters, StructuralIncrementMeters);
        }

        public static float SnapOpening(float meters)
        {
            return Snap(meters, OpeningIncrementMeters);
        }

        public static bool IsStructuralAligned(float meters)
        {
            return IsAligned(meters, StructuralIncrementMeters);
        }

        public static bool IsOpeningAligned(float meters)
        {
            return IsAligned(meters, OpeningIncrementMeters);
        }

        public static float WallFaceOffset(float wallThicknessMeters)
        {
            if (!IsPositiveFinite(wallThicknessMeters))
            {
                throw new ArgumentOutOfRangeException(nameof(wallThicknessMeters));
            }

            return wallThicknessMeters * 0.5f;
        }

        public static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float Snap(float value, float increment)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return (float)Math.Round(
                       value / increment,
                       MidpointRounding.AwayFromZero) *
                   increment;
        }

        private static bool IsAligned(float value, float increment)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return false;
            }

            return Mathf.Abs(value - Snap(value, increment)) <=
                   AlignmentToleranceMeters;
        }
    }

    public enum ProceduralAssetCategory
    {
        Display,
        Transaction,
        WorkSurface,
        ProcessStation,
        UseStation,
        Storage,
        Service,
        Amenity,
        Decor
    }

    [Flags]
    public enum ProceduralMountingMode
    {
        None = 0,
        Floor = 1 << 0,
        Wall = 1 << 1,
        Ceiling = 1 << 2,
        Socket = 1 << 3,
        ExteriorPad = 1 << 4
    }

    public enum ProceduralAccessMode
    {
        Customer,
        Staff,
        CustomerAndStaff
    }

    [Flags]
    public enum ProceduralInteractionSide
    {
        None = 0,
        Front = 1 << 0,
        Back = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3
    }

    public enum ProceduralWallRelationship
    {
        Required,
        StronglyPreferred,
        Preferred,
        Neutral,
        Avoided
    }

    public enum ProceduralEnvironment
    {
        IndoorOnly,
        OutdoorOnly,
        IndoorOrOutdoor
    }

    public enum ProceduralAssemblyBehavior
    {
        Independent,
        LinearRun,
        BankOrRepeatedRow,
        SocketChild
    }

    public enum ProceduralResizeBehavior
    {
        Fixed,
        Repeatable,
        StretchSafe
    }

    public enum ProceduralPivotConvention
    {
        FloorBottomCenter,
        WallOpeningBottomCenter,
        WallMountPlaneCenter,
        CeilingAttachmentCenter,
        CornerIntersection
    }

    public enum ProceduralClearanceKind
    {
        CustomerInteraction,
        StaffInteraction,
        Occupancy,
        Queue,
        Maintenance,
        NoBlock
    }

    public enum FunctionalZoneType
    {
        Public,
        Transaction,
        Work,
        Storage,
        Service,
        Staff,
        Circulation
    }

    public enum CommercialBuildingArchetype
    {
        StandaloneSmallCommercial,
        StripCenterInlineRetail,
        OlderMainStreetMixedUse
    }

    public enum OrthogonalFootprintKind
    {
        Rectangle,
        LShape,
        SteppedRectangle
    }

    public enum FrontageRole
    {
        PrimaryPublic,
        SecondaryPublic,
        RearService,
        SharedInternal
    }

    public enum ProceduralOpeningKind
    {
        PrimaryEntrance,
        ServiceDoor,
        StorefrontWindow,
        UpperFloorWindow,
        InteriorDoor
    }

    public enum ProceduralDiagnosticSeverity
    {
        Information,
        Warning,
        Error
    }

    [Serializable]
    public struct PlanRect : IEquatable<PlanRect>
    {
        [SerializeField] private float minX;
        [SerializeField] private float minZ;
        [SerializeField] private float width;
        [SerializeField] private float depth;

        public PlanRect(float minX, float minZ, float width, float depth)
        {
            this.minX = minX;
            this.minZ = minZ;
            this.width = width;
            this.depth = depth;
        }

        public float MinX => minX;
        public float MinZ => minZ;
        public float Width => width;
        public float Depth => depth;
        public float MaxX => minX + width;
        public float MaxZ => minZ + depth;
        public float Area => width * depth;
        public Vector2 Center => new(minX + width * 0.5f, minZ + depth * 0.5f);
        public bool IsValid =>
            CommercialGenerationDimensions.IsPositiveFinite(width) &&
            CommercialGenerationDimensions.IsPositiveFinite(depth) &&
            !float.IsNaN(minX) && !float.IsInfinity(minX) &&
            !float.IsNaN(minZ) && !float.IsInfinity(minZ);

        public bool Contains(Vector2 point, float tolerance = 0.0001f)
        {
            return point.x >= MinX - tolerance && point.x <= MaxX + tolerance &&
                   point.y >= MinZ - tolerance && point.y <= MaxZ + tolerance;
        }

        public bool Contains(PlanRect other, float tolerance = 0.0001f)
        {
            return other.MinX >= MinX - tolerance &&
                   other.MaxX <= MaxX + tolerance &&
                   other.MinZ >= MinZ - tolerance &&
                   other.MaxZ <= MaxZ + tolerance;
        }

        public bool Overlaps(PlanRect other, float tolerance = 0.0001f)
        {
            return MinX < other.MaxX - tolerance &&
                   MaxX > other.MinX + tolerance &&
                   MinZ < other.MaxZ - tolerance &&
                   MaxZ > other.MinZ + tolerance;
        }

        public PlanRect Expanded(float amount)
        {
            return new PlanRect(
                minX - amount,
                minZ - amount,
                width + amount * 2f,
                depth + amount * 2f);
        }

        public bool Equals(PlanRect other)
        {
            return minX.Equals(other.minX) && minZ.Equals(other.minZ) &&
                   width.Equals(other.width) && depth.Equals(other.depth);
        }

        public override bool Equals(object obj)
        {
            return obj is PlanRect other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(minX, minZ, width, depth);
        }
    }

    [Serializable]
    public sealed class ProceduralClearanceDefinition
    {
        [SerializeField] private string clearanceId;
        [SerializeField] private ProceduralClearanceKind kind;
        [SerializeField] private Vector3 localCenterMeters;
        [SerializeField] private Vector3 sizeMeters;
        [SerializeField] private bool required = true;

        public string ClearanceId => clearanceId;
        public ProceduralClearanceKind Kind => kind;
        public Vector3 LocalCenterMeters => localCenterMeters;
        public Vector3 SizeMeters => sizeMeters;
        public bool Required => required;

        public ProceduralClearanceDefinition(
            string clearanceId,
            ProceduralClearanceKind kind,
            Vector3 localCenterMeters,
            Vector3 sizeMeters,
            bool required = true)
        {
            this.clearanceId = clearanceId;
            this.kind = kind;
            this.localCenterMeters = localCenterMeters;
            this.sizeMeters = sizeMeters;
            this.required = required;
        }

        public bool TryValidate(out string error)
        {
            if (!StableIdentifier.IsValid(clearanceId) ||
                !CommercialGenerationDimensions.IsPositiveFinite(sizeMeters.x) ||
                !CommercialGenerationDimensions.IsPositiveFinite(sizeMeters.y) ||
                !CommercialGenerationDimensions.IsPositiveFinite(sizeMeters.z))
            {
                error = "Clearance definitions require a stable id and positive finite size.";
                return false;
            }

            error = null;
            return true;
        }
    }


    [Serializable]
    public sealed class ProceduralZoneRequest
    {
        [SerializeField] private FunctionalZoneType zoneType;
        [SerializeField] private bool required;
        [SerializeField] private bool enclosed;
        [SerializeField, Min(0f)] private float minimumAreaSquareFeet;
        [SerializeField, Min(0f)] private float preferredAreaSquareFeet;

        public FunctionalZoneType ZoneType => zoneType;
        public bool Required => required;
        public bool Enclosed => enclosed;
        public float MinimumAreaSquareFeet => minimumAreaSquareFeet;
        public float PreferredAreaSquareFeet => preferredAreaSquareFeet;

        public ProceduralZoneRequest(
            FunctionalZoneType zoneType,
            bool required,
            bool enclosed,
            float minimumAreaSquareFeet,
            float preferredAreaSquareFeet)
        {
            this.zoneType = zoneType;
            this.required = required;
            this.enclosed = enclosed;
            this.minimumAreaSquareFeet = minimumAreaSquareFeet;
            this.preferredAreaSquareFeet = preferredAreaSquareFeet;
        }

        public bool TryValidate(out string error)
        {
            if (minimumAreaSquareFeet < 0f ||
                preferredAreaSquareFeet < minimumAreaSquareFeet ||
                float.IsNaN(preferredAreaSquareFeet) ||
                float.IsInfinity(preferredAreaSquareFeet))
            {
                error = $"Zone '{zoneType}' has invalid Minimum/Preferred area values.";
                return false;
            }

            error = null;
            return true;
        }
    }

    [Serializable]
    public sealed class ProceduralAssetRequest
    {
        [SerializeField] private string requestId;
        [SerializeField] private ProceduralAssetCategory primaryCategory;
        [SerializeField] private string[] requiredCapabilities = Array.Empty<string>();
        [SerializeField, Min(0)] private int minimum;
        [SerializeField, Min(0)] private int preferred;
        [SerializeField, Min(0)] private int maximum;
        [SerializeField] private bool required;
        [SerializeField] private FunctionalZoneType preferredHostZone;
        [SerializeField] private int priority;
        [SerializeField] private ProceduralEnvironment environment =
            ProceduralEnvironment.IndoorOnly;

        public string RequestId => requestId;
        public ProceduralAssetCategory PrimaryCategory => primaryCategory;
        public IReadOnlyList<string> RequiredCapabilities =>
            requiredCapabilities ?? Array.Empty<string>();
        public int Minimum => minimum;
        public int Preferred => preferred;
        public int Maximum => maximum;
        public bool Required => required;
        public FunctionalZoneType PreferredHostZone => preferredHostZone;
        public int Priority => priority;
        public ProceduralEnvironment Environment => environment;

        public ProceduralAssetRequest(
            string requestId,
            ProceduralAssetCategory primaryCategory,
            string[] requiredCapabilities,
            int minimum,
            int preferred,
            int maximum,
            bool required,
            FunctionalZoneType preferredHostZone,
            int priority,
            ProceduralEnvironment environment = ProceduralEnvironment.IndoorOnly)
        {
            this.requestId = requestId;
            this.primaryCategory = primaryCategory;
            this.requiredCapabilities = requiredCapabilities ?? Array.Empty<string>();
            this.minimum = minimum;
            this.preferred = preferred;
            this.maximum = maximum;
            this.required = required;
            this.preferredHostZone = preferredHostZone;
            this.priority = priority;
            this.environment = environment;
        }

        public bool TryValidate(out string error)
        {
            if (!StableIdentifier.IsValid(requestId) || minimum < 0 ||
                preferred < minimum || maximum < preferred ||
                (required && minimum == 0))
            {
                error = $"Asset request '{requestId}' has invalid Required or Minimum/Preferred/Maximum values.";
                return false;
            }

            HashSet<string> capabilities = new(StringComparer.Ordinal);
            foreach (string capability in RequiredCapabilities)
            {
                if (!StableIdentifier.IsValid(capability) || !capabilities.Add(capability))
                {
                    error = $"Asset request '{requestId}' has an invalid or duplicate capability.";
                    return false;
                }
            }

            if (capabilities.Count == 0)
            {
                error = $"Asset request '{requestId}' requires at least one capability tag.";
                return false;
            }

            error = null;
            return true;
        }
    }


    [Serializable]
    public struct CommercialArchetypeProfile
    {
        public CommercialBuildingArchetype archetype;
        public float structuralHeightMeters;
        public float finishedCeilingHeightMeters;
        public float parapetHeightMeters;
        public float storefrontSillMeters;
        public float storefrontTopMeters;
        public int minimumStories;
        public int maximumStories;

        public static CommercialArchetypeProfile For(
            CommercialBuildingArchetype archetype)
        {
            switch (archetype)
            {
                case CommercialBuildingArchetype.StandaloneSmallCommercial:
                    return new CommercialArchetypeProfile
                    {
                        archetype = archetype,
                        structuralHeightMeters = CommercialGenerationDimensions.Feet(12f),
                        finishedCeilingHeightMeters = CommercialGenerationDimensions.Feet(10f),
                        parapetHeightMeters = CommercialGenerationDimensions.Feet(14f),
                        storefrontSillMeters = CommercialGenerationDimensions.Feet(1.5f),
                        storefrontTopMeters = CommercialGenerationDimensions.Feet(9f),
                        minimumStories = 1,
                        maximumStories = 1
                    };
                case CommercialBuildingArchetype.StripCenterInlineRetail:
                    return new CommercialArchetypeProfile
                    {
                        archetype = archetype,
                        structuralHeightMeters = CommercialGenerationDimensions.Feet(14f),
                        finishedCeilingHeightMeters = CommercialGenerationDimensions.Feet(12f),
                        parapetHeightMeters = CommercialGenerationDimensions.Feet(16f),
                        storefrontSillMeters = CommercialGenerationDimensions.Feet(1.5f),
                        storefrontTopMeters = CommercialGenerationDimensions.Feet(10f),
                        minimumStories = 1,
                        maximumStories = 1
                    };
                case CommercialBuildingArchetype.OlderMainStreetMixedUse:
                    return new CommercialArchetypeProfile
                    {
                        archetype = archetype,
                        structuralHeightMeters = CommercialGenerationDimensions.Feet(14f),
                        finishedCeilingHeightMeters = CommercialGenerationDimensions.Feet(12f),
                        parapetHeightMeters = CommercialGenerationDimensions.Feet(14f),
                        storefrontSillMeters = CommercialGenerationDimensions.Feet(1.5f),
                        storefrontTopMeters = CommercialGenerationDimensions.Feet(10.5f),
                        minimumStories = 2,
                        maximumStories = 4
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(archetype));
            }
        }
    }

    [Serializable]
    public sealed class ProceduralBuildingRequest
    {
        [SerializeField] private int seed = 1;
        [SerializeField] private CommercialBuildingArchetype archetype;
        [SerializeField] private OrthogonalFootprintKind footprintKind;
        [SerializeField, Min(12)] private int widthFeet = 48;
        [SerializeField, Min(12)] private int depthFeet = 50;
        [SerializeField] private bool cornerExposure;
        [SerializeField] private ProceduralAssetRegistry assetRegistry;
        [SerializeField] private ProceduralBusinessRecipe businessRecipe;

        public int Seed => seed;
        public CommercialBuildingArchetype Archetype => archetype;
        public OrthogonalFootprintKind FootprintKind => footprintKind;
        public int WidthFeet => widthFeet;
        public int DepthFeet => depthFeet;
        public bool CornerExposure => cornerExposure;
        public ProceduralAssetRegistry AssetRegistry => assetRegistry;
        public ProceduralBusinessRecipe BusinessRecipe => businessRecipe;

        public ProceduralBuildingRequest(
            int seed,
            CommercialBuildingArchetype archetype,
            OrthogonalFootprintKind footprintKind,
            int widthFeet,
            int depthFeet,
            bool cornerExposure,
            ProceduralAssetRegistry assetRegistry,
            ProceduralBusinessRecipe businessRecipe)
        {
            this.seed = seed;
            this.archetype = archetype;
            this.footprintKind = footprintKind;
            this.widthFeet = widthFeet;
            this.depthFeet = depthFeet;
            this.cornerExposure = cornerExposure;
            this.assetRegistry = assetRegistry;
            this.businessRecipe = businessRecipe;
        }

        public bool TryValidate(out string error)
        {
            error = null;
            if (widthFeet < 12 || depthFeet < 12)
            {
                error = "Procedural building envelopes must be at least 12 ft per side.";
                return false;
            }

            if (assetRegistry == null ||
                !assetRegistry.TryValidate(true, out error))
            {
                error = $"Procedural building requires a valid asset registry. {error}";
                return false;
            }

            if (businessRecipe == null || !businessRecipe.TryValidate(out error))
            {
                error = $"Procedural building requires a valid business recipe. {error}";
                return false;
            }

            error = null;
            return true;
        }
    }
}
