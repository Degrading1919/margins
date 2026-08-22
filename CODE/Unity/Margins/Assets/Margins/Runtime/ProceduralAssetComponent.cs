using System;
using System.Collections.Generic;
using UnityEngine;

namespace Margins
{
    public sealed class ProceduralAssetComponent : MonoBehaviour
    {
        [SerializeField] private string stableAssetId;
        [SerializeField] private ProceduralAssetCategory primaryCategory;
        [SerializeField] private string[] capabilityTags = Array.Empty<string>();
        [SerializeField] private ProceduralMountingMode mountingModes =
            ProceduralMountingMode.Floor;
        [SerializeField] private ProceduralAccessMode accessMode;
        [SerializeField] private ProceduralInteractionSide interactionSides;
        [SerializeField] private ProceduralWallRelationship wallRelationship;
        [SerializeField] private ProceduralEnvironment environment =
            ProceduralEnvironment.IndoorOnly;
        [SerializeField] private ProceduralAssemblyBehavior assemblyBehavior;
        [SerializeField] private ProceduralResizeBehavior resizeBehavior;
        [SerializeField] private ProceduralPivotConvention pivotConvention;
        [SerializeField] private Vector3 physicalSizeMeters = Vector3.one;
        [SerializeField] private float preferredMountHeightMeters;
        [SerializeField] private string requiredSocketCompatibility;
        [SerializeField] private ProceduralClearanceDefinition[] clearances =
            Array.Empty<ProceduralClearanceDefinition>();
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Collider primaryCollider;
        [SerializeField] private Color debugColor = Color.gray;

        public string StableAssetId => stableAssetId;
        public ProceduralAssetCategory PrimaryCategory => primaryCategory;
        public IReadOnlyList<string> CapabilityTags =>
            capabilityTags ?? Array.Empty<string>();
        public ProceduralMountingMode MountingModes => mountingModes;
        public ProceduralAccessMode AccessMode => accessMode;
        public ProceduralInteractionSide InteractionSides => interactionSides;
        public ProceduralWallRelationship WallRelationship => wallRelationship;
        public ProceduralEnvironment Environment => environment;
        public ProceduralAssemblyBehavior AssemblyBehavior => assemblyBehavior;
        public ProceduralResizeBehavior ResizeBehavior => resizeBehavior;
        public ProceduralPivotConvention PivotConvention => pivotConvention;
        public Vector3 PhysicalSizeMeters => physicalSizeMeters;
        public float PreferredMountHeightMeters => preferredMountHeightMeters;
        public string RequiredSocketCompatibility => requiredSocketCompatibility;
        public IReadOnlyList<ProceduralClearanceDefinition> Clearances =>
            clearances ?? Array.Empty<ProceduralClearanceDefinition>();
        public Transform VisualRoot => visualRoot;
        public Collider PrimaryCollider => primaryCollider;
        public Color DebugColor => debugColor;

        public IReadOnlyList<ProceduralSocketComponent> Sockets =>
            GetComponentsInChildren<ProceduralSocketComponent>(true);
        public IReadOnlyList<ProceduralAnchorComponent> Anchors =>
            GetComponentsInChildren<ProceduralAnchorComponent>(true);

        public bool HasCapability(string capabilityId)
        {
            if (!StableIdentifier.IsValid(capabilityId) || capabilityTags == null)
            {
                return false;
            }

            foreach (string capability in capabilityTags)
            {
                if (string.Equals(capability, capabilityId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool SupportsAllCapabilities(IReadOnlyList<string> requiredCapabilities)
        {
            if (requiredCapabilities == null)
            {
                return true;
            }

            foreach (string capability in requiredCapabilities)
            {
                if (!HasCapability(capability))
                {
                    return false;
                }
            }

            return true;
        }

        public void Configure(
            string assetId,
            ProceduralAssetCategory category,
            string[] capabilities,
            ProceduralMountingMode allowedMountingModes,
            ProceduralAccessMode allowedAccess,
            ProceduralInteractionSide sides,
            ProceduralWallRelationship wallPreference,
            ProceduralEnvironment allowedEnvironment,
            ProceduralAssemblyBehavior assembly,
            ProceduralResizeBehavior resize,
            ProceduralPivotConvention pivot,
            Vector3 sizeMeters,
            float mountHeightMeters,
            string socketCompatibility,
            ProceduralClearanceDefinition[] clearanceDefinitions,
            Transform replaceableVisualRoot,
            Collider rootCollider,
            Color color)
        {
            stableAssetId = assetId;
            primaryCategory = category;
            capabilityTags = capabilities ?? Array.Empty<string>();
            mountingModes = allowedMountingModes;
            accessMode = allowedAccess;
            interactionSides = sides;
            wallRelationship = wallPreference;
            environment = allowedEnvironment;
            assemblyBehavior = assembly;
            resizeBehavior = resize;
            pivotConvention = pivot;
            physicalSizeMeters = sizeMeters;
            preferredMountHeightMeters = mountHeightMeters;
            requiredSocketCompatibility = socketCompatibility;
            clearances = clearanceDefinitions ??
                         Array.Empty<ProceduralClearanceDefinition>();
            visualRoot = replaceableVisualRoot;
            primaryCollider = rootCollider;
            debugColor = color;
        }

        public bool TryValidateConfiguration(out string error)
        {
            error = null;
            if (!StableIdentifier.IsValid(stableAssetId))
            {
                error = $"Procedural asset '{name}' requires a stable lowercase id.";
                return false;
            }

            if (!Enum.IsDefined(typeof(ProceduralAssetCategory), primaryCategory) ||
                mountingModes == ProceduralMountingMode.None)
            {
                error = $"Procedural asset '{stableAssetId}' requires one category and a mounting mode.";
                return false;
            }

            if (!CommercialGenerationDimensions.IsPositiveFinite(physicalSizeMeters.x) ||
                !CommercialGenerationDimensions.IsPositiveFinite(physicalSizeMeters.y) ||
                !CommercialGenerationDimensions.IsPositiveFinite(physicalSizeMeters.z))
            {
                error = $"Procedural asset '{stableAssetId}' requires positive finite dimensions.";
                return false;
            }

            if (visualRoot == null || visualRoot.parent != transform ||
                !string.Equals(visualRoot.name, "Visual", StringComparison.Ordinal) ||
                primaryCollider == null || primaryCollider.transform != transform)
            {
                error = $"Procedural asset '{stableAssetId}' must own a root collider and a direct replaceable Visual child.";
                return false;
            }

            HashSet<string> capabilities = new(StringComparer.Ordinal);
            foreach (string capability in CapabilityTags)
            {
                if (!StableIdentifier.IsValid(capability) || !capabilities.Add(capability))
                {
                    error = $"Procedural asset '{stableAssetId}' has an invalid or duplicate capability.";
                    return false;
                }
            }

            HashSet<string> clearanceIds = new(StringComparer.Ordinal);
            foreach (ProceduralClearanceDefinition clearance in Clearances)
            {
                if (clearance == null || !clearance.TryValidate(out error) ||
                    !clearanceIds.Add(clearance.ClearanceId))
                {
                    error = $"Procedural asset '{stableAssetId}' has invalid clearance data. {error}";
                    return false;
                }
            }

            HashSet<string> socketIds = new(StringComparer.Ordinal);
            foreach (ProceduralSocketComponent socket in Sockets)
            {
                if (socket == null || !socket.TryValidate(out error) ||
                    !socketIds.Add(socket.SocketId))
                {
                    error = $"Procedural asset '{stableAssetId}' has invalid socket data. {error}";
                    return false;
                }
            }

            HashSet<string> anchorIds = new(StringComparer.Ordinal);
            foreach (ProceduralAnchorComponent anchor in Anchors)
            {
                if (anchor == null || !anchor.TryValidate(out error) ||
                    !anchorIds.Add(anchor.AnchorId))
                {
                    error = $"Procedural asset '{stableAssetId}' has invalid anchor data. {error}";
                    return false;
                }
            }

            if (resizeBehavior == ProceduralResizeBehavior.Repeatable &&
                anchorIds.Count < 2)
            {
                error = $"Repeatable asset '{stableAssetId}' requires extension anchors.";
                return false;
            }

            if ((mountingModes & ProceduralMountingMode.Socket) != 0 &&
                !StableIdentifier.IsValid(requiredSocketCompatibility))
            {
                error = $"Socket-mounted asset '{stableAssetId}' requires a compatibility id.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
