using System;
using System.Collections.Generic;
using UnityEngine;

namespace Margins
{
    [CreateAssetMenu(
        fileName = "ProceduralBusinessRecipe",
        menuName = "Margins/Procedural/Business Recipe")]
    public sealed class ProceduralBusinessRecipe : ScriptableObject
    {
        [SerializeField] private string stableRecipeId;
        [SerializeField, Min(1f)] private float minimumFrontageFeet = 20f;
        [SerializeField, Min(1f)] private float minimumDepthFeet = 20f;
        [SerializeField, Min(1f)] private float minimumUsableAreaSquareFeet = 400f;
        [SerializeField] private bool requiresServiceEntrance;
        [SerializeField] private ProceduralZoneRequest[] zoneRequests =
            Array.Empty<ProceduralZoneRequest>();
        [SerializeField] private ProceduralAssetRequest[] assetRequests =
            Array.Empty<ProceduralAssetRequest>();

        public string StableRecipeId => stableRecipeId;
        public float MinimumFrontageFeet => minimumFrontageFeet;
        public float MinimumDepthFeet => minimumDepthFeet;
        public float MinimumUsableAreaSquareFeet => minimumUsableAreaSquareFeet;
        public bool RequiresServiceEntrance => requiresServiceEntrance;
        public IReadOnlyList<ProceduralZoneRequest> ZoneRequests =>
            zoneRequests ?? Array.Empty<ProceduralZoneRequest>();
        public IReadOnlyList<ProceduralAssetRequest> AssetRequests =>
            assetRequests ?? Array.Empty<ProceduralAssetRequest>();

        public void Configure(
            string recipeId,
            float minimumFrontage,
            float minimumDepth,
            float minimumArea,
            bool serviceEntrance,
            ProceduralZoneRequest[] zones,
            ProceduralAssetRequest[] requests)
        {
            stableRecipeId = recipeId;
            minimumFrontageFeet = minimumFrontage;
            minimumDepthFeet = minimumDepth;
            minimumUsableAreaSquareFeet = minimumArea;
            requiresServiceEntrance = serviceEntrance;
            zoneRequests = zones ?? Array.Empty<ProceduralZoneRequest>();
            assetRequests = requests ?? Array.Empty<ProceduralAssetRequest>();
        }

        public bool RequiresZone(FunctionalZoneType zoneType)
        {
            foreach (ProceduralZoneRequest request in ZoneRequests)
            {
                if (request != null && request.Required && request.ZoneType == zoneType)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryValidate(out string error)
        {
            error = null;
            if (!StableIdentifier.IsValid(stableRecipeId) ||
                !CommercialGenerationDimensions.IsPositiveFinite(minimumFrontageFeet) ||
                !CommercialGenerationDimensions.IsPositiveFinite(minimumDepthFeet) ||
                !CommercialGenerationDimensions.IsPositiveFinite(
                    minimumUsableAreaSquareFeet))
            {
                error = "A business recipe requires a stable id and positive spatial minima.";
                return false;
            }

            HashSet<FunctionalZoneType> zones = new();
            foreach (ProceduralZoneRequest request in ZoneRequests)
            {
                if (request == null || !request.TryValidate(out error) ||
                    !zones.Add(request.ZoneType))
                {
                    error = $"Recipe '{stableRecipeId}' has invalid or duplicate zone data. {error}";
                    return false;
                }
            }

            if (!RequiresZone(FunctionalZoneType.Public) ||
                !RequiresZone(FunctionalZoneType.Circulation))
            {
                error = $"Recipe '{stableRecipeId}' requires Public and Circulation zones.";
                return false;
            }

            HashSet<string> requestIds = new(StringComparer.Ordinal);
            foreach (ProceduralAssetRequest request in AssetRequests)
            {
                if (request == null || !request.TryValidate(out error) ||
                    !requestIds.Add(request.RequestId))
                {
                    error = $"Recipe '{stableRecipeId}' has invalid or duplicate asset requests. {error}";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
