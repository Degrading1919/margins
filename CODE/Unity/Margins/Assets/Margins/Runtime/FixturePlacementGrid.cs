using System;
using UnityEngine;

namespace Margins
{
    /// <summary>
    /// Authoritative dimensional contract for player-placeable fixtures.
    /// Unity remains meter-native: one Unity unit is one meter.
    /// </summary>
    public static class FixturePlacementGrid
    {
        public const float MetersPerUnityUnit = 1f;
        public const float MetersPerInch = 0.0254f;
        public const float PlacementIncrementInches = 6f;
        public const float PlacementIncrementMeters =
            PlacementIncrementInches * MetersPerInch;
        public const float LegacyPlacementIncrementMeters = 0.5f;

        private const float CellCountTolerance = 0.0001f;

        public static int CellsToCoverMeters(float meters)
        {
            if (float.IsNaN(meters) || float.IsInfinity(meters) || meters <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(meters),
                    "Fixture dimensions must be a positive finite meter value.");
            }

            return Mathf.Max(
                1,
                Mathf.CeilToInt(
                    meters / PlacementIncrementMeters - CellCountTolerance));
        }

        public static float CellsToMeters(int cells)
        {
            if (cells < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cells),
                    "Fixture cell counts cannot be negative.");
            }

            return cells * PlacementIncrementMeters;
        }

        public static GridPosition WorldPointToGridPosition(
            Transform gridOrigin,
            Vector3 worldPoint)
        {
            if (gridOrigin == null)
            {
                throw new ArgumentNullException(nameof(gridOrigin));
            }

            Vector3 localPoint = gridOrigin.InverseTransformPoint(worldPoint);
            return new GridPosition(
                Mathf.FloorToInt(localPoint.x / PlacementIncrementMeters),
                Mathf.FloorToInt(localPoint.z / PlacementIncrementMeters));
        }

        public static Vector3 LocalCenter(
            GridPosition gridPosition,
            GridFootprint unrotatedFootprint,
            int quarterTurns)
        {
            GridFootprint rotated = unrotatedFootprint.Rotate(quarterTurns);
            return new Vector3(
                (gridPosition.x + rotated.width * 0.5f) * PlacementIncrementMeters,
                0f,
                (gridPosition.z + rotated.depth * 0.5f) * PlacementIncrementMeters);
        }

        public static GridPosition NearestPositionForLocalCenter(
            Vector3 localCenterMeters,
            GridFootprint unrotatedFootprint,
            int quarterTurns)
        {
            GridFootprint rotated = unrotatedFootprint.Rotate(quarterTurns);
            return new GridPosition(
                RoundToNearestCell(
                    localCenterMeters.x / PlacementIncrementMeters -
                    rotated.width * 0.5f),
                RoundToNearestCell(
                    localCenterMeters.z / PlacementIncrementMeters -
                    rotated.depth * 0.5f));
        }

        public static GridPosition MigrateLegacyPosition(
            GridPosition legacyPosition,
            GridFootprint legacyUnrotatedFootprint,
            int quarterTurns,
            float legacyCellSizeMeters,
            Vector3 legacyOriginOffsetMeters,
            GridFootprint currentUnrotatedFootprint)
        {
            if (float.IsNaN(legacyCellSizeMeters) ||
                float.IsInfinity(legacyCellSizeMeters) ||
                legacyCellSizeMeters <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(legacyCellSizeMeters),
                    "Legacy fixture cell size must be a positive finite meter value.");
            }

            GridFootprint legacyRotated =
                legacyUnrotatedFootprint.Rotate(quarterTurns);
            Vector3 legacyCenter = legacyOriginOffsetMeters + new Vector3(
                (legacyPosition.x + legacyRotated.width * 0.5f) *
                legacyCellSizeMeters,
                0f,
                (legacyPosition.z + legacyRotated.depth * 0.5f) *
                legacyCellSizeMeters);
            return NearestPositionForLocalCenter(
                legacyCenter,
                currentUnrotatedFootprint,
                quarterTurns);
        }

        private static int RoundToNearestCell(float value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
