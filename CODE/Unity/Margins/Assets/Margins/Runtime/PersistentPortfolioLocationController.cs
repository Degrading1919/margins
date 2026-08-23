using System;
using System.Linq;
using UnityEngine;

namespace Margins
{
    /// <summary>
    /// Materializes an existing persistent portfolio location through the
    /// approved procedural generator. Portfolio state owns identity and layout;
    /// this component owns only the currently instantiated graybox.
    /// </summary>
    public sealed class PersistentPortfolioLocationController : MonoBehaviour
    {
        private const string GeneratedHostName =
            "Persistent Portfolio Location";

        [SerializeField] private PortfolioProgressionController portfolio;
        [SerializeField] private ProceduralAssetRegistry assetRegistry;
        [SerializeField] private ProceduralBusinessRecipe[] businessRecipes =
            Array.Empty<ProceduralBusinessRecipe>();

        private PortfolioProgression directProgression;
        private GameObject activeHost;
        private ProceduralCommercialBuilding activeBuilding;
        private string activeLocationId;

        public string ActiveLocationId => activeLocationId;
        public ProceduralCommercialBuilding ActiveBuilding => activeBuilding;

        private PortfolioProgression Progression =>
            portfolio?.Progression ?? directProgression;

        public void Configure(
            PortfolioProgressionController progressionController,
            ProceduralAssetRegistry registry,
            params ProceduralBusinessRecipe[] recipes)
        {
            portfolio = progressionController;
            directProgression = null;
            assetRegistry = registry;
            businessRecipes = recipes ??
                              Array.Empty<ProceduralBusinessRecipe>();
        }

        public void ConfigureForDomain(
            PortfolioProgression progression,
            ProceduralAssetRegistry registry,
            params ProceduralBusinessRecipe[] recipes)
        {
            directProgression = progression;
            portfolio = null;
            assetRegistry = registry;
            businessRecipes = recipes ??
                              Array.Empty<ProceduralBusinessRecipe>();
        }

        public bool TryMaterializeLocation(
            string locationId,
            out string error)
        {
            error = null;
            if (Progression == null)
            {
                error = "Persistent portfolio progression is unavailable.";
                return false;
            }

            PortfolioProgressionSnapshot snapshot =
                Progression.CreateSnapshot();
            PortfolioCommercialUnitSnapshot unit = snapshot.company.properties
                .SelectMany(property => property.commercialUnits)
                .FirstOrDefault(value => string.Equals(
                    value.occupyingLocationId,
                    locationId,
                    StringComparison.Ordinal));
            if (unit?.generatedLayout == null)
            {
                error = "The selected portfolio location has no persistent generated layout.";
                return false;
            }

            PersistentGeneratedLayoutSnapshot layout = unit.generatedLayout;
            if (!TryResolveContent(
                    layout.businessRecipeId,
                    out ProceduralAssetRegistry registry,
                    out ProceduralBusinessRecipe recipe,
                    out error))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(activeLocationId) &&
                !string.Equals(
                    activeLocationId,
                    locationId,
                    StringComparison.Ordinal) &&
                !Progression.TryLeaveDetailedLocation(
                    activeLocationId,
                    out error))
            {
                return false;
            }
            if (!string.IsNullOrWhiteSpace(activeLocationId) &&
                !string.Equals(
                    activeLocationId,
                    locationId,
                    StringComparison.Ordinal))
            {
                activeLocationId = null;
            }

            ClearInstantiatedLocation();
            activeHost = new GameObject(
                $"{GeneratedHostName} [{locationId}]");
            activeHost.transform.SetParent(transform, false);
            activeBuilding =
                activeHost.AddComponent<ProceduralCommercialBuilding>();
            activeBuilding.Configure(new ProceduralBuildingRequest(
                layout.seed,
                layout.archetype,
                layout.footprintKind,
                layout.widthFeet,
                layout.depthFeet,
                layout.cornerExposure,
                registry,
                recipe), false);
            if (!activeBuilding.TryGenerate(out error))
            {
                ClearInstantiatedLocation();
                return false;
            }

            ProceduralGenerationResult result = activeBuilding.LastResult;
            if (!string.Equals(
                    result.GeneratorVersion,
                    layout.generatorVersion,
                    StringComparison.Ordinal) ||
                result.Units.Count != 1 ||
                !string.Equals(
                    result.Units[0].UnitId,
                    layout.selectedUnitId,
                    StringComparison.Ordinal))
            {
                error =
                    "Generated version or selected commercial unit differs from persistent location identity.";
                ClearInstantiatedLocation();
                return false;
            }

            string signature = result.CanonicalSignature();
            if (!Progression.TryBindGeneratedLayout(
                    locationId,
                    signature,
                    out error))
            {
                ClearInstantiatedLocation();
                return false;
            }

            ApplyPersistentModifications(unit);
            if (!Progression.TryEnterDetailedLocation(locationId, out error))
            {
                ClearInstantiatedLocation();
                return false;
            }

            activeLocationId = locationId;
            error = null;
            return true;
        }

        public bool TryRecordTransformOverride(
            string modificationId,
            string targetStableId,
            Vector3 localPosition,
            float localYawDegrees,
            out string error)
        {
            if (string.IsNullOrWhiteSpace(activeLocationId) ||
                FindGeneratedTarget(targetStableId) == null)
            {
                error = "A generated target in the active persistent location is required.";
                return false;
            }

            PortfolioLocationModificationSnapshot modification = new()
            {
                modificationId = modificationId,
                modificationTypeId = "transform-override",
                targetStableId = targetStableId,
                localPositionX = localPosition.x,
                localPositionY = localPosition.y,
                localPositionZ = localPosition.z,
                localYawDegrees = localYawDegrees
            };
            if (!Progression.TryRecordLocationModification(
                    activeLocationId,
                    modification,
                    out error))
            {
                return false;
            }

            ApplyModification(modification);
            error = null;
            return true;
        }

        public bool TryLeaveActiveLocation(out string error)
        {
            if (string.IsNullOrWhiteSpace(activeLocationId))
            {
                error = null;
                return true;
            }
            if (!Progression.TryLeaveDetailedLocation(
                    activeLocationId,
                    out error))
            {
                return false;
            }

            ClearInstantiatedLocation();
            activeLocationId = null;
            error = null;
            return true;
        }

        private bool TryResolveContent(
            string businessRecipeId,
            out ProceduralAssetRegistry registry,
            out ProceduralBusinessRecipe recipe,
            out string error)
        {
            error = null;
            registry = assetRegistry != null
                ? assetRegistry
                : Resources.Load<ProceduralAssetRegistry>(
                    "ProceduralAssetRegistry");
            recipe = null;
            if (!StableIdentifier.IsValid(businessRecipeId))
            {
                error = "Persistent location has an invalid business recipe id.";
                return false;
            }

            ProceduralBusinessRecipe[] configured = businessRecipes?
                .Where(value => value != null)
                .ToArray() ?? Array.Empty<ProceduralBusinessRecipe>();
            ProceduralBusinessRecipe[] candidates = configured.Length > 0
                ? configured
                : Resources.LoadAll<ProceduralBusinessRecipe>("Recipes");
            recipe = candidates.FirstOrDefault(value => string.Equals(
                value.StableRecipeId,
                businessRecipeId,
                StringComparison.Ordinal));
            if (registry == null || recipe == null ||
                !registry.TryValidate(true, out error) ||
                !recipe.TryValidate(out error))
            {
                error ??=
                    $"Persistent location materialization could not resolve recipe '{businessRecipeId}' from the configured content registry.";
                return false;
            }

            error = null;
            return true;
        }

        private void ApplyPersistentModifications(
            PortfolioCommercialUnitSnapshot unit)
        {
            foreach (PortfolioLocationModificationSnapshot modification in
                     unit.generatedLayout.modifications
                         .OrderBy(value =>
                             value.modificationId,
                             StringComparer.Ordinal))
            {
                ApplyModification(modification);
            }
        }

        private void ApplyModification(
            PortfolioLocationModificationSnapshot modification)
        {
            if (!string.Equals(
                    modification.modificationTypeId,
                    "transform-override",
                    StringComparison.Ordinal))
            {
                return;
            }

            Transform target = FindGeneratedTarget(
                modification.targetStableId);
            if (target == null)
            {
                Debug.LogWarning(
                    $"Persistent modification '{modification.modificationId}' targets missing generated id '{modification.targetStableId}'.",
                    this);
                return;
            }
            target.localPosition = new Vector3(
                modification.localPositionX,
                modification.localPositionY,
                modification.localPositionZ);
            target.localRotation = Quaternion.Euler(
                0f,
                modification.localYawDegrees,
                0f);
        }

        private Transform FindGeneratedTarget(string stableId)
        {
            return activeHost == null
                ? null
                : activeHost.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(value => string.Equals(
                        value.name,
                        stableId,
                        StringComparison.Ordinal));
        }

        private void ClearInstantiatedLocation()
        {
            if (activeHost != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(activeHost);
                }
                else
                {
                    DestroyImmediate(activeHost);
                }
            }
            activeHost = null;
            activeBuilding = null;
        }

        private void OnDestroy()
        {
            if (!string.IsNullOrWhiteSpace(activeLocationId) &&
                Progression != null)
            {
                Progression.TryLeaveDetailedLocation(activeLocationId, out _);
            }
        }
    }
}
