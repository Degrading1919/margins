// Draft implementation — Unity verification pending
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Margins
{
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
        [SerializeField, Min(0.01f)] private float cellSize = 0.5f;
        [SerializeField] private PlaceableFixtureComponent[] fixtures;

        private readonly Dictionary<string, PlaceableFixtureComponent> fixturesById =
            new(StringComparer.Ordinal);
        private StoreOperatingController operatingController;

        internal FixtureLayout Layout { get; private set; }
        public int PlacedCount => Layout?.Count ?? 0;
        public bool IsInitialized => Layout != null;

        public bool HasConfiguredFixture(string fixtureInstanceId)
        {
            return FirstStoreIdentifier.IsValid(fixtureInstanceId) &&
                   fixturesById.ContainsKey(fixtureInstanceId);
        }

        public bool IsPlaced(string fixtureInstanceId)
        {
            return Layout != null &&
                   Layout.TryGetPlacement(fixtureInstanceId, out _);
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

            if (gridWidthCells <= 0 || gridDepthCells <= 0 || cellSize <= 0f)
            {
                error = "Fixture grid dimensions and cell size must be positive.";
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

            if (operatingController != null &&
                operatingController.IsFixtureModificationRestricted(
                    fixture.StableFixtureInstanceId))
            {
                fixture.SetPreviewState(FixturePlacementPreviewState.Invalid);
                return FixturePlacementResult.Reject(
                    FixturePlacementFailure.OperatingStateRestricted,
                    fixture.StableFixtureInstanceId);
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

            if (operatingController != null &&
                operatingController.IsFixtureModificationRestricted(
                    fixture.StableFixtureInstanceId))
            {
                fixture.SetPreviewState(FixturePlacementPreviewState.Invalid);
                return FixturePlacementResult.Reject(
                    FixturePlacementFailure.OperatingStateRestricted,
                    fixture.StableFixtureInstanceId);
            }

            FixturePlacementResult result =
                Layout.TryRemove(fixture.StableFixtureInstanceId);
            fixture.SetPreviewState(
                result.IsSuccess
                    ? FixturePlacementPreviewState.None
                    : FixturePlacementPreviewState.Invalid);
            return result;
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

            if (restored.Width != gridWidthCells ||
                restored.Depth != gridDepthCells)
            {
                error = "Restored fixture grid dimensions do not match the inspector.";
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

            error = null;
            return true;
        }

        public bool TryApplyRestoredLayout(FixtureLayout restored, out string error)
        {
            if (!CanApplyRestoredLayout(restored, out error))
            {
                return false;
            }

            Layout = restored;
            foreach (FixturePlacementSnapshot placement in Layout.CreateSnapshot())
            {
                fixturesById[placement.fixtureInstanceId].ApplyPlacement(
                    placement,
                    gridOrigin,
                    cellSize);
            }
            return true;
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
                fixture.ApplyPlacement(placement, gridOrigin, cellSize);
            }
            else
            {
                fixture.SetPreviewState(FixturePlacementPreviewState.Invalid);
            }
        }
    }
}
