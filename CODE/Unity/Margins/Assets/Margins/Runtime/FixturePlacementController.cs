// Draft implementation — Unity verification pending
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Margins
{
    [Serializable]
    public sealed class InitialFixturePlacementConfiguration
    {
        [SerializeField] private PlaceableFixtureComponent fixture;
        [SerializeField] private GridPosition gridPosition;
        [SerializeField] private int quarterTurns;

        public PlaceableFixtureComponent Fixture => fixture;
        public GridPosition GridPosition => gridPosition;
        public int QuarterTurns => quarterTurns;
    }

    [Serializable]
    public sealed class LegacyFixtureGridConfiguration
    {
        [SerializeField, Min(1)] private int gridWidthCells;
        [SerializeField, Min(1)] private int gridDepthCells;
        [SerializeField, Min(0.01f)] private float cellSizeMeters =
            FixturePlacementGrid.LegacyPlacementIncrementMeters;
        [SerializeField] private Vector3 originOffsetMeters;
        [SerializeField] private string[] fixtureInstanceIds;

        public int GridWidthCells => gridWidthCells;
        public int GridDepthCells => gridDepthCells;
        public float CellSizeMeters => cellSizeMeters;
        public Vector3 OriginOffsetMeters => originOffsetMeters;
        public IReadOnlyList<string> FixtureInstanceIds =>
            fixtureInstanceIds ?? Array.Empty<string>();

        public bool Matches(FixtureLayout layout)
        {
            return layout != null &&
                   layout.Width == gridWidthCells &&
                   layout.Depth == gridDepthCells;
        }
    }

    public enum FixturePlacementPreviewState
    {
        None,
        Valid,
        Invalid
    }

    public sealed class FixturePlacementController : MonoBehaviour
    {
        [SerializeField] private Transform gridOrigin;
        [SerializeField, Min(1)] private int gridWidthCells = 10;
        [SerializeField, Min(1)] private int gridDepthCells = 10;
        [SerializeField] private PlaceableFixtureComponent[] fixtures;
        [SerializeField] private InitialFixturePlacementConfiguration[] initialPlacements;
        [Header("Save compatibility")]
        [SerializeField] private LegacyFixtureGridConfiguration[] legacyGridConfigurations;

        private readonly Dictionary<string, PlaceableFixtureComponent> fixturesById =
            new(StringComparer.Ordinal);
        private StoreOperatingController operatingController;

        internal FixtureLayout Layout { get; private set; }
        public int PlacedCount => Layout?.Count ?? 0;
        public bool IsInitialized => Layout != null;
        public Transform GridOrigin => gridOrigin;
        public int GridWidthCells => gridWidthCells;
        public int GridDepthCells => gridDepthCells;
        public float CellSize => FixturePlacementGrid.PlacementIncrementMeters;

        public bool HasConfiguredFixture(string fixtureInstanceId)
        {
            return FirstStoreIdentifier.IsValid(fixtureInstanceId) &&
                   fixturesById.ContainsKey(fixtureInstanceId);
        }

        public bool IsConfiguredFixture(PlaceableFixtureComponent fixture)
        {
            return fixture != null &&
                   fixturesById.TryGetValue(
                       fixture.StableFixtureInstanceId,
                       out PlaceableFixtureComponent configured) &&
                   configured == fixture;
        }

        public bool IsFixtureModificationRestricted(
            PlaceableFixtureComponent fixture)
        {
            return fixture != null &&
                   operatingController != null &&
                   operatingController.IsFixtureModificationRestricted(
                       fixture.StableFixtureInstanceId);
        }

        public bool IsFixtureRemovalRestricted(
            PlaceableFixtureComponent fixture)
        {
            return fixture != null &&
                   operatingController != null &&
                   operatingController.IsFixtureRemovalRestricted(
                       fixture.StableFixtureInstanceId);
        }

        public bool IsPlaced(string fixtureInstanceId)
        {
            return Layout != null &&
                   Layout.TryGetPlacement(fixtureInstanceId, out _);
        }

        public bool TryGetPlacement(
            string fixtureInstanceId,
            out FixturePlacementSnapshot placement)
        {
            if (Layout != null && Layout.TryGetPlacement(fixtureInstanceId, out placement))
            {
                return true;
            }

            placement = null;
            return false;
        }

        private void Start()
        {
            if (!TryInitialize(out string error))
            {
                Debug.LogError($"Fixture placement initialization failed: {error}", this);
            }
        }

        public bool TryValidateConfiguration(out string error)
        {
            error = null;
            if (gridOrigin == null)
            {
                error = "Fixture placement requires an explicit grid origin.";
                return false;
            }

            if (gridWidthCells <= 0 || gridDepthCells <= 0)
            {
                error = "Fixture grid dimensions must be positive.";
                return false;
            }

            if (fixtures == null || fixtures.Length == 0)
            {
                error = "Fixture placement requires at least one explicit fixture reference.";
                return false;
            }

            HashSet<string> identifiers = new(StringComparer.Ordinal);
            foreach (PlaceableFixtureComponent fixture in fixtures)
            {
                if (fixture == null || !fixture.TryValidateConfiguration(out error))
                {
                    error ??= "Fixture placement contains a missing fixture reference.";
                    return false;
                }

                if (!identifiers.Add(fixture.StableFixtureInstanceId))
                {
                    error =
                        $"Duplicate fixture instance id '{fixture.StableFixtureInstanceId}'.";
                    return false;
                }
            }

            if (initialPlacements != null)
            {
                HashSet<string> initialIds = new(StringComparer.Ordinal);
                foreach (InitialFixturePlacementConfiguration placement in initialPlacements)
                {
                    if (placement?.Fixture == null ||
                        !identifiers.Contains(placement.Fixture.StableFixtureInstanceId) ||
                        !initialIds.Add(placement.Fixture.StableFixtureInstanceId))
                    {
                        error = "Initial fixture placements contain a missing, unconfigured, or duplicate fixture.";
                        return false;
                    }
                }
            }

            HashSet<string> legacyDimensions = new(StringComparer.Ordinal);
            if (legacyGridConfigurations != null)
            {
                foreach (LegacyFixtureGridConfiguration legacy in legacyGridConfigurations)
                {
                    if (legacy == null ||
                        legacy.GridWidthCells <= 0 ||
                        legacy.GridDepthCells <= 0 ||
                        float.IsNaN(legacy.CellSizeMeters) ||
                        float.IsInfinity(legacy.CellSizeMeters) ||
                        legacy.CellSizeMeters <= 0f ||
                        float.IsNaN(legacy.OriginOffsetMeters.x) ||
                        float.IsInfinity(legacy.OriginOffsetMeters.x) ||
                        float.IsNaN(legacy.OriginOffsetMeters.y) ||
                        float.IsInfinity(legacy.OriginOffsetMeters.y) ||
                        float.IsNaN(legacy.OriginOffsetMeters.z) ||
                        float.IsInfinity(legacy.OriginOffsetMeters.z))
                    {
                        error = "Fixture placement contains an invalid legacy grid configuration.";
                        return false;
                    }

                    if (legacy.GridWidthCells == gridWidthCells &&
                        legacy.GridDepthCells == gridDepthCells)
                    {
                        error = "A legacy fixture grid cannot share the current grid dimensions.";
                        return false;
                    }

                    string dimensions =
                        $"{legacy.GridWidthCells}x{legacy.GridDepthCells}";
                    if (!legacyDimensions.Add(dimensions))
                    {
                        error = $"Duplicate legacy fixture grid dimensions '{dimensions}'.";
                        return false;
                    }

                    HashSet<string> legacyIds = new(StringComparer.Ordinal);
                    foreach (string fixtureInstanceId in legacy.FixtureInstanceIds)
                    {
                        if (!FirstStoreIdentifier.IsValid(fixtureInstanceId) ||
                            !identifiers.Contains(fixtureInstanceId) ||
                            !legacyIds.Add(fixtureInstanceId))
                        {
                            error =
                                $"Legacy fixture grid '{dimensions}' contains an invalid, unconfigured, or duplicate fixture id.";
                            return false;
                        }
                    }
                }
            }

            error = null;
            return true;
        }

        public bool TryInitialize(out string error)
        {
            if (Layout != null)
            {
                error = null;
                return true;
            }

            if (!TryValidateConfiguration(out error))
            {
                return false;
            }

            fixturesById.Clear();
            foreach (PlaceableFixtureComponent fixture in fixtures)
            {
                fixturesById.Add(fixture.StableFixtureInstanceId, fixture);
            }

            Layout = new FixtureLayout(gridWidthCells, gridDepthCells);
            if (initialPlacements != null)
            {
                foreach (InitialFixturePlacementConfiguration placement in initialPlacements)
                {
                    FixturePlacementResult result = Layout.TryPlace(
                        placement.Fixture.StableFixtureInstanceId,
                        placement.GridPosition,
                        placement.Fixture.Footprint,
                        placement.QuarterTurns);
                    if (!result.IsSuccess)
                    {
                        Layout = null;
                        error =
                            $"Initial fixture '{placement.Fixture.StableFixtureInstanceId}' placement failed ({result.Failure}).";
                        return false;
                    }

                    ApplyResult(placement.Fixture, result);
                }
            }
            error = null;
            return true;
        }

        public FixturePlacementResult TryPlace(
            PlaceableFixtureComponent fixture,
            GridPosition gridPosition,
            int quarterTurns)
        {
            if (!TryResolveFixture(fixture, out FixturePlacementResult rejection))
            {
                return rejection;
            }

            if (IsFixtureModificationRestricted(fixture))
            {
                return FixturePlacementResult.Reject(
                    FixturePlacementFailure.OperatingStateRestricted,
                    fixture.StableFixtureInstanceId);
            }

            FixturePlacementResult result = Layout.TryPlace(
                fixture.StableFixtureInstanceId,
                gridPosition,
                fixture.Footprint,
                quarterTurns);
            ApplyResult(fixture, result);
            return result;
        }

        public FixturePlacementResult TryMove(
            PlaceableFixtureComponent fixture,
            GridPosition gridPosition,
            int quarterTurns)
        {
            if (!TryResolveFixture(fixture, out FixturePlacementResult rejection))
            {
                return rejection;
            }

            if (IsFixtureModificationRestricted(fixture))
            {
                return RejectOperatingState(fixture);
            }

            FixturePlacementResult result = Layout.TryMove(
                fixture.StableFixtureInstanceId,
                gridPosition,
                quarterTurns);
            ApplyResult(fixture, result);
            return result;
        }

        public FixturePlacementResult TryRemove(PlaceableFixtureComponent fixture)
        {
            if (!TryResolveFixture(fixture, out FixturePlacementResult rejection))
            {
                return rejection;
            }

            if (IsFixtureRemovalRestricted(fixture))
            {
                return FixturePlacementResult.Reject(
                    FixturePlacementFailure.OperatingStateRestricted,
                    fixture.StableFixtureInstanceId);
            }

            FixturePlacementResult result =
                Layout.TryRemove(fixture.StableFixtureInstanceId);
            if (result.IsSuccess)
            {
                fixture.ClearPlacement();
            }
            return result;
        }

        public FixturePlacementResult PreviewPlace(
            PlaceableFixtureComponent fixture,
            GridPosition gridPosition,
            int quarterTurns)
        {
            if (!TryResolveFixture(fixture, out FixturePlacementResult rejection))
            {
                return rejection;
            }

            if (IsFixtureModificationRestricted(fixture))
            {
                return RejectOperatingState(fixture);
            }

            return Layout.PreviewPlace(
                fixture.StableFixtureInstanceId,
                gridPosition,
                fixture.Footprint,
                quarterTurns);
        }

        public FixturePlacementResult PreviewMove(
            PlaceableFixtureComponent fixture,
            GridPosition gridPosition,
            int quarterTurns)
        {
            if (!TryResolveFixture(fixture, out FixturePlacementResult rejection))
            {
                return rejection;
            }

            if (IsFixtureModificationRestricted(fixture))
            {
                return RejectOperatingState(fixture);
            }

            return Layout.PreviewMove(
                fixture.StableFixtureInstanceId,
                gridPosition,
                quarterTurns);
        }

        public bool CanApplyRestoredLayout(FixtureLayout restored, out string error)
        {
            error = null;
            if (Layout == null)
            {
                error = "Fixture placement controller is not initialized.";
                return false;
            }

            if (restored == null || !TryValidateConfiguration(out error))
            {
                error ??= "Restored fixture layout is missing.";
                return false;
            }

            return TryCreateCompatibleRestoredLayout(restored, out _, out error);
        }

        public bool TryApplyRestoredLayout(FixtureLayout restored, out string error)
        {
            error = null;
            if (Layout == null || !TryValidateConfiguration(out error))
            {
                error ??= "Fixture placement controller is not initialized.";
                return false;
            }

            if (!TryCreateCompatibleRestoredLayout(
                    restored,
                    out FixtureLayout compatible,
                    out error))
            {
                return false;
            }

            foreach (PlaceableFixtureComponent fixture in fixturesById.Values)
            {
                fixture.ClearPlacement();
            }

            Layout = compatible;
            foreach (FixturePlacementSnapshot placement in Layout.CreateSnapshot())
            {
                fixturesById[placement.fixtureInstanceId].ApplyPlacement(
                    placement,
                    gridOrigin);
            }
            return true;
        }

        private bool TryCreateCompatibleRestoredLayout(
            FixtureLayout restored,
            out FixtureLayout compatible,
            out string error)
        {
            compatible = null;
            if (restored == null)
            {
                error = "Restored fixture layout is missing.";
                return false;
            }

            bool currentDimensions =
                restored.Width == gridWidthCells &&
                restored.Depth == gridDepthCells;
            LegacyFixtureGridConfiguration legacy = null;
            if (!currentDimensions && legacyGridConfigurations != null)
            {
                foreach (LegacyFixtureGridConfiguration candidate in legacyGridConfigurations)
                {
                    if (candidate != null && candidate.Matches(restored))
                    {
                        legacy = candidate;
                        break;
                    }
                }
            }

            if (!currentDimensions && legacy == null)
            {
                error = "Restored fixture grid dimensions do not match the current or supported legacy property grid.";
                return false;
            }

            foreach (FixturePlacementSnapshot placement in restored.CreateSnapshot())
            {
                if (!fixturesById.ContainsKey(placement.fixtureInstanceId))
                {
                    error =
                        $"Restored fixture '{placement.fixtureInstanceId}' has no inspector reference.";
                    return false;
                }
            }

            if (currentDimensions)
            {
                compatible = restored;
                error = null;
                return true;
            }

            FixtureLayout migrated = new(gridWidthCells, gridDepthCells);
            foreach (FixturePlacementSnapshot placement in restored.CreateSnapshot())
            {
                PlaceableFixtureComponent fixture =
                    fixturesById[placement.fixtureInstanceId];
                GridPosition migratedPosition =
                    FixturePlacementGrid.MigrateLegacyPosition(
                        placement.gridPosition,
                        placement.unrotatedFootprint,
                        placement.quarterTurns,
                        legacy.CellSizeMeters,
                        legacy.OriginOffsetMeters,
                        fixture.Footprint);
                FixturePlacementResult result = TryPlaceNearest(
                    migrated,
                    placement.fixtureInstanceId,
                    migratedPosition,
                    fixture.Footprint,
                    placement.quarterTurns);
                if (!result.IsSuccess)
                {
                    error =
                        $"Legacy fixture '{placement.fixtureInstanceId}' could not migrate to the owned-property grid ({result.Failure}).";
                    return false;
                }
            }

            HashSet<string> legacyIds = new(
                legacy.FixtureInstanceIds,
                StringComparer.Ordinal);
            if (initialPlacements != null)
            {
                foreach (InitialFixturePlacementConfiguration initial in initialPlacements)
                {
                    if (initial?.Fixture == null ||
                        legacyIds.Contains(initial.Fixture.StableFixtureInstanceId) ||
                        migrated.TryGetPlacement(
                            initial.Fixture.StableFixtureInstanceId,
                            out _))
                    {
                        continue;
                    }

                    FixturePlacementResult result = migrated.TryPlace(
                        initial.Fixture.StableFixtureInstanceId,
                        initial.GridPosition,
                        initial.Fixture.Footprint,
                        initial.QuarterTurns);
                    if (!result.IsSuccess)
                    {
                        error =
                            $"New fixture '{initial.Fixture.StableFixtureInstanceId}' could not be added while migrating the legacy layout ({result.Failure}).";
                        return false;
                    }
                }
            }

            compatible = migrated;
            error = null;
            return true;
        }

        private static FixturePlacementResult TryPlaceNearest(
            FixtureLayout layout,
            string fixtureInstanceId,
            GridPosition preferredPosition,
            GridFootprint footprint,
            int quarterTurns)
        {
            FixturePlacementResult preferred = layout.TryPlace(
                fixtureInstanceId,
                preferredPosition,
                footprint,
                quarterTurns);
            if (preferred.IsSuccess ||
                (preferred.Failure != FixturePlacementFailure.Occupied &&
                 preferred.Failure != FixturePlacementFailure.OutOfBounds))
            {
                return preferred;
            }

            int maximumRadius = layout.Width + layout.Depth;
            for (int radius = 1; radius <= maximumRadius; radius++)
            {
                for (int xOffset = -radius; xOffset <= radius; xOffset++)
                {
                    int zMagnitude = radius - Math.Abs(xOffset);
                    FixturePlacementResult result = layout.TryPlace(
                        fixtureInstanceId,
                        new GridPosition(
                            preferredPosition.x + xOffset,
                            preferredPosition.z - zMagnitude),
                        footprint,
                        quarterTurns);
                    if (result.IsSuccess)
                    {
                        return result;
                    }

                    if (zMagnitude == 0)
                    {
                        continue;
                    }

                    result = layout.TryPlace(
                        fixtureInstanceId,
                        new GridPosition(
                            preferredPosition.x + xOffset,
                            preferredPosition.z + zMagnitude),
                        footprint,
                        quarterTurns);
                    if (result.IsSuccess)
                    {
                        return result;
                    }
                }
            }

            return preferred;
        }

        public bool TryBindOperatingController(
            StoreOperatingController controller,
            out string error)
        {
            if (controller == null ||
                (operatingController != null && operatingController != controller))
            {
                error =
                    "Fixture placement cannot bind a missing or second operating controller.";
                return false;
            }

            operatingController = controller;
            error = null;
            return true;
        }

        private bool TryResolveFixture(
            PlaceableFixtureComponent fixture,
            out FixturePlacementResult rejection)
        {
            if (Layout == null)
            {
                rejection = FixturePlacementResult.Reject(
                    FixturePlacementFailure.MissingFixture,
                    fixture?.StableFixtureInstanceId);
                return false;
            }

            if (fixture == null ||
                !fixturesById.TryGetValue(
                    fixture.StableFixtureInstanceId,
                    out PlaceableFixtureComponent configured) ||
                configured != fixture)
            {
                rejection = FixturePlacementResult.Reject(
                    FixturePlacementFailure.MissingFixture,
                    fixture?.StableFixtureInstanceId);
                return false;
            }

            rejection = null;
            return true;
        }

        private void ApplyResult(
            PlaceableFixtureComponent fixture,
            FixturePlacementResult result)
        {
            if (result.IsSuccess &&
                Layout.TryGetPlacement(
                    fixture.StableFixtureInstanceId,
                    out FixturePlacementSnapshot placement))
            {
                fixture.ApplyPlacement(placement, gridOrigin);
            }
            else
            {
                fixture.SetPreviewState(FixturePlacementPreviewState.Invalid);
            }
        }

        private static FixturePlacementResult RejectOperatingState(
            PlaceableFixtureComponent fixture)
        {
            fixture.SetPreviewState(FixturePlacementPreviewState.Invalid);
            return FixturePlacementResult.Reject(
                FixturePlacementFailure.OperatingStateRestricted,
                fixture.StableFixtureInstanceId);
        }
    }
}
