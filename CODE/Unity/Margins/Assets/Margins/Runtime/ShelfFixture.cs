using System;
using System.Collections.Generic;
using UnityEngine;

namespace Margins
{
    public enum PlacementFailure
    {
        None,
        MissingProductDefinition,
        InvalidSnapPoint,
        Incompatible,
        OutOfRange,
        Occupied
    }

    [Serializable]
    public sealed class ShelfSnapPointDefinition
    {
        [SerializeField] private string stableSnapPointId;
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private Vector3 localEulerAngles;
        [SerializeField] private string[] acceptedCompatibilityTags;

        public string StableSnapPointId => stableSnapPointId;
        public Vector3 LocalPosition => localPosition;
        public Vector3 LocalEulerAngles => localEulerAngles;
        public IReadOnlyList<string> AcceptedCompatibilityTags => acceptedCompatibilityTags;

        public bool Accepts(string compatibilityTag)
        {
            if (string.IsNullOrWhiteSpace(compatibilityTag) || acceptedCompatibilityTags == null)
            {
                return false;
            }

            foreach (string acceptedTag in acceptedCompatibilityTags)
            {
                if (string.Equals(acceptedTag, compatibilityTag, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class ShelfFixture : MonoBehaviour
    {
        private const float DistanceTieTolerance = 0.000001f;

        [SerializeField] private string stableFixtureId;
        [SerializeField, Min(0.01f)] private float snapSearchRadius = 0.75f;
        [SerializeField] private List<ShelfSnapPointDefinition> snapPoints = new();

        private readonly Dictionary<string, ProductItem> occupancy = new(StringComparer.Ordinal);

        public string StableFixtureId => stableFixtureId;
        public float SnapSearchRadius => snapSearchRadius;
        public IReadOnlyList<ShelfSnapPointDefinition> SnapPoints => snapPoints;
        public bool HasOccupiedSnapPoints => occupancy.Count > 0;

        public bool TryFindNearestAvailable(
            ProductItem product,
            Vector3 worldPosition,
            out ShelfSnapPointDefinition selected,
            out PlacementFailure failure)
        {
            selected = null;
            failure = PlacementFailure.OutOfRange;

            if (product == null || product.Definition == null)
            {
                failure = PlacementFailure.MissingProductDefinition;
                return false;
            }

            bool foundCompatible = false;
            bool foundOccupiedInRange = false;
            float bestDistanceSquared = float.PositiveInfinity;
            float radiusSquared = snapSearchRadius * snapSearchRadius;

            foreach (ShelfSnapPointDefinition snapPoint in snapPoints)
            {
                if (snapPoint == null || !snapPoint.Accepts(product.Definition.SnapCompatibilityTag))
                {
                    continue;
                }

                foundCompatible = true;
                float distanceSquared = (GetWorldPosition(snapPoint) - worldPosition).sqrMagnitude;
                if (distanceSquared > radiusSquared)
                {
                    continue;
                }

                if (IsOccupied(snapPoint.StableSnapPointId))
                {
                    foundOccupiedInRange = true;
                    continue;
                }

                bool isCloser = distanceSquared < bestDistanceSquared - DistanceTieTolerance;
                bool isTieWithEarlierIdentifier =
                    Mathf.Abs(distanceSquared - bestDistanceSquared) <= DistanceTieTolerance &&
                    selected != null &&
                    string.CompareOrdinal(snapPoint.StableSnapPointId, selected.StableSnapPointId) < 0;

                if (selected == null || isCloser || isTieWithEarlierIdentifier)
                {
                    selected = snapPoint;
                    bestDistanceSquared = distanceSquared;
                }
            }

            if (selected != null)
            {
                failure = PlacementFailure.None;
                return true;
            }

            if (foundOccupiedInRange)
            {
                failure = PlacementFailure.Occupied;
            }
            else if (!foundCompatible)
            {
                failure = PlacementFailure.Incompatible;
            }

            return false;
        }

        public bool TryPlaceNearest(
            ProductItem product,
            Vector3 worldPosition,
            int quarterTurns,
            out string snapPointId,
            out PlacementFailure failure)
        {
            snapPointId = null;
            if (!TryFindNearestAvailable(product, worldPosition, out ShelfSnapPointDefinition selected, out failure))
            {
                return false;
            }

            snapPointId = selected.StableSnapPointId;
            return TryPlaceAt(product, snapPointId, quarterTurns, out failure);
        }

        public bool TryPlaceAt(
            ProductItem product,
            string snapPointId,
            int quarterTurns,
            out PlacementFailure failure)
        {
            if (product == null || product.Definition == null)
            {
                failure = PlacementFailure.MissingProductDefinition;
                return false;
            }

            if (!TryGetSnapPoint(snapPointId, out ShelfSnapPointDefinition snapPoint))
            {
                failure = PlacementFailure.InvalidSnapPoint;
                return false;
            }

            if (!snapPoint.Accepts(product.Definition.SnapCompatibilityTag))
            {
                failure = PlacementFailure.Incompatible;
                return false;
            }

            if (IsOccupied(snapPointId))
            {
                failure = PlacementFailure.Occupied;
                return false;
            }

            int normalizedQuarterTurns = ((quarterTurns % 4) + 4) % 4;
            occupancy.Add(snapPointId, product);
            product.ApplySnappedPlacement(
                this,
                snapPointId,
                normalizedQuarterTurns,
                GetWorldPosition(snapPoint),
                GetWorldRotation(snapPoint) * Quaternion.Euler(0f, normalizedQuarterTurns * 90f, 0f));

            failure = PlacementFailure.None;
            return true;
        }

        public bool TryGetSnapPoint(string snapPointId, out ShelfSnapPointDefinition snapPoint)
        {
            foreach (ShelfSnapPointDefinition candidate in snapPoints)
            {
                if (candidate != null && string.Equals(candidate.StableSnapPointId, snapPointId, StringComparison.Ordinal))
                {
                    snapPoint = candidate;
                    return true;
                }
            }

            snapPoint = null;
            return false;
        }

        public bool IsOccupied(string snapPointId)
        {
            return !string.IsNullOrEmpty(snapPointId) && occupancy.ContainsKey(snapPointId);
        }

        public ProductItem GetOccupant(string snapPointId)
        {
            return occupancy.TryGetValue(snapPointId, out ProductItem product) ? product : null;
        }

        public void ReleaseProduct(ProductItem product)
        {
            string occupiedSnapPointId = null;
            foreach (KeyValuePair<string, ProductItem> pair in occupancy)
            {
                if (pair.Value == product)
                {
                    occupiedSnapPointId = pair.Key;
                    break;
                }
            }

            if (occupiedSnapPointId != null)
            {
                occupancy.Remove(occupiedSnapPointId);
            }
        }

        public void ClearRuntimeOccupancy()
        {
            occupancy.Clear();
        }

        public Vector3 GetWorldPosition(ShelfSnapPointDefinition snapPoint)
        {
            return transform.TransformPoint(snapPoint.LocalPosition);
        }

        public Quaternion GetWorldRotation(ShelfSnapPointDefinition snapPoint)
        {
            return transform.rotation * Quaternion.Euler(snapPoint.LocalEulerAngles);
        }
    }
}
