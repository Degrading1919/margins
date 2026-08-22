using System;
using System.Collections.Generic;
using UnityEngine;

namespace Margins
{
    [CreateAssetMenu(
        fileName = "ProceduralAssetRegistry",
        menuName = "Margins/Procedural/Asset Registry")]
    public sealed class ProceduralAssetRegistry : ScriptableObject
    {
        [SerializeField] private GameObject[] assetPrefabs = Array.Empty<GameObject>();

        public IReadOnlyList<GameObject> AssetPrefabs =>
            assetPrefabs ?? Array.Empty<GameObject>();

        public void Configure(GameObject[] prefabs)
        {
            assetPrefabs = prefabs ?? Array.Empty<GameObject>();
        }

        public List<ProceduralAssetComponent> FindCandidates(
            ProceduralAssetCategory category,
            IReadOnlyList<string> requiredCapabilities,
            ProceduralEnvironment requiredEnvironment)
        {
            List<ProceduralAssetComponent> candidates = new();
            foreach (GameObject prefab in AssetPrefabs)
            {
                ProceduralAssetComponent asset =
                    prefab == null ? null : prefab.GetComponent<ProceduralAssetComponent>();
                if (asset == null || asset.PrimaryCategory != category ||
                    !asset.SupportsAllCapabilities(requiredCapabilities) ||
                    !EnvironmentMatches(asset.Environment, requiredEnvironment))
                {
                    continue;
                }

                candidates.Add(asset);
            }

            candidates.Sort((left, right) => string.CompareOrdinal(
                left.StableAssetId,
                right.StableAssetId));
            return candidates;
        }

        public bool TryValidate(bool requireAllPrimaryCategories, out string error)
        {
            error = null;
            if (assetPrefabs == null || assetPrefabs.Length == 0)
            {
                error = "The procedural asset registry is empty.";
                return false;
            }

            HashSet<string> assetIds = new(StringComparer.Ordinal);
            HashSet<ProceduralAssetCategory> categories = new();
            foreach (GameObject prefab in assetPrefabs)
            {
                ProceduralAssetComponent asset =
                    prefab == null ? null : prefab.GetComponent<ProceduralAssetComponent>();
                if (asset == null || asset.gameObject != prefab ||
                    !asset.TryValidateConfiguration(out error))
                {
                    error = $"Registry entry is not a valid procedural prefab root. {error}";
                    return false;
                }

                if (!assetIds.Add(asset.StableAssetId))
                {
                    error = $"Duplicate procedural asset id '{asset.StableAssetId}'.";
                    return false;
                }

                categories.Add(asset.PrimaryCategory);
            }

            if (requireAllPrimaryCategories)
            {
                foreach (ProceduralAssetCategory category in
                         Enum.GetValues(typeof(ProceduralAssetCategory)))
                {
                    if (!categories.Contains(category))
                    {
                        error = $"The registry has no representative asset for '{category}'.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        private static bool EnvironmentMatches(
            ProceduralEnvironment candidate,
            ProceduralEnvironment required)
        {
            return candidate == ProceduralEnvironment.IndoorOrOutdoor ||
                   required == ProceduralEnvironment.IndoorOrOutdoor ||
                   candidate == required;
        }
    }
}
