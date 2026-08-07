using System;
using UnityEngine;

namespace Margins
{
    /// <summary>
    /// A transient first-person placement session. FixturePlacementController remains
    /// the sole owner of accepted layout state.
    /// </summary>
    public sealed class FirstStoreFixturePlacementModeController :
        MonoBehaviour,
        IFirstStoreWorldInteractionTarget
    {
        [SerializeField] private string stableTargetId;
        [SerializeField] private FixturePlacementController fixturePlacement;
        [SerializeField] private Collider placementFloor;
        [SerializeField] private OwnedPropertyPlacementArea propertyArea;
        [SerializeField, Min(0.1f)] private float maximumRayDistance = 12f;

        private PlaceableFixtureComponent activeFixture;
        private FixturePlacementSnapshot priorPlacement;
        private bool isMove;
        private bool hasPreview;
        private GridPosition previewPosition;
        private int previewQuarterTurns;
        private FixturePlacementResult previewResult;

        public event Action<bool, string> BuildModeChanged;

        public bool IsBuildModeActive { get; private set; }
        public bool IsActive => activeFixture != null;
        public string StableTargetId => stableTargetId;
        public FirstStoreWorldInteractionPriority Priority => FirstStoreWorldInteractionPriority.Fixture;
        public bool IsAvailable =>
            IsActive &&
            FirstStoreIdentifier.IsValid(stableTargetId) &&
            fixturePlacement != null;
        public FirstStoreWorldInteractionPrompt Prompt
        {
            get
            {
                string previewState = HasPreview
                    ? PreviewReason ?? "valid grid position"
                    : "aim at the placement floor";
                return new FirstStoreWorldInteractionPrompt(
                    "Q",
                    HasPreview && PreviewResult != null && PreviewResult.IsSuccess
                        ? "Place fixture"
                        : "Cancel fixture move",
                    $"{previewState}; mouse wheel rotates");
            }
        }
        public PlaceableFixtureComponent ActiveFixture => activeFixture;
        public bool HasPreview => hasPreview;
        public GridPosition PreviewPosition => previewPosition;
        public int PreviewQuarterTurns => previewQuarterTurns;
        public FixturePlacementResult PreviewResult => previewResult;
        public string PreviewReason => FormatResult(previewResult);

        public bool IsPlaced(PlaceableFixtureComponent fixture)
        {
            return fixture != null &&
                   fixturePlacement != null &&
                   fixturePlacement.IsPlaced(fixture.StableFixtureInstanceId);
        }

        public bool TryValidateConfiguration(out string error)
        {
            if (fixturePlacement == null || propertyArea == null ||
                propertyArea.PlacementSurface == null)
            {
                error =
                    "Fixture placement mode requires explicit placement and owned-property references.";
                return false;
            }

            if (!propertyArea.TryValidateConfiguration(out error))
            {
                return false;
            }

            if (maximumRayDistance <= 0f)
            {
                error = "Fixture placement ray distance must be positive.";
                return false;
            }

            error = null;
            return true;
        }

        private void Update()
        {
            RefreshOwnedPropertyPresence();
        }

        public bool RefreshOwnedPropertyPresence()
        {
            if (!IsBuildModeActive ||
                (propertyArea != null && propertyArea.ContainsPlayer()))
            {
                return IsBuildModeActive;
            }

            TrySetBuildMode(false, out _);
            return false;
        }

        private void OnDisable()
        {
            if (IsBuildModeActive)
            {
                TrySetBuildMode(false, out _);
            }
        }

        public bool TryToggleBuildMode(out string error)
        {
            return TrySetBuildMode(!IsBuildModeActive, out error);
        }

        public bool TrySetBuildMode(bool enabled, out string error)
        {
            if (enabled == IsBuildModeActive)
            {
                error = null;
                return true;
            }

            if (enabled)
            {
                if (!TryValidateConfiguration(out error))
                {
                    return false;
                }

                if (!propertyArea.ContainsPlayer())
                {
                    error = "Return to the owned property before entering Build Mode.";
                    return false;
                }

                IsBuildModeActive = true;
                error = null;
                BuildModeChanged?.Invoke(true, null);
                return true;
            }

            if (IsActive)
            {
                TryCancel(out _);
            }

            IsBuildModeActive = false;
            error = null;
            BuildModeChanged?.Invoke(false, "Build Mode exited.");
            return true;
        }

        public bool TryBegin(PlaceableFixtureComponent fixture, out string error)
        {
            if (!TryValidateConfiguration(out error) ||
                !fixturePlacement.TryInitialize(out error))
            {
                return false;
            }

            if (fixture == null || !fixturePlacement.IsConfiguredFixture(fixture))
            {
                error = "This fixture is not configured for placement.";
                return false;
            }

            if (!IsBuildModeActive)
            {
                error = "Enter Build Mode before moving fixtures.";
                return false;
            }

            if (fixturePlacement.IsFixtureModificationRestricted(fixture))
            {
                error = FormatResult(FixturePlacementResult.Reject(
                    FixturePlacementFailure.OperatingStateRestricted,
                    fixture.StableFixtureInstanceId));
                return false;
            }

            if (IsActive)
            {
                error = activeFixture == fixture
                    ? "Fixture placement is already active."
                    : "Finish or cancel the current fixture placement first.";
                return false;
            }

            activeFixture = fixture;
            isMove = fixturePlacement.TryGetPlacement(
                fixture.StableFixtureInstanceId,
                out priorPlacement);
            previewQuarterTurns = isMove ? priorPlacement.quarterTurns : 0;
            hasPreview = false;
            previewResult = null;

            if (isMove)
            {
                if (TryPreviewAtGridPosition(priorPlacement.gridPosition, out error))
                {
                    return true;
                }

                activeFixture.ApplyPlacement(
                    priorPlacement,
                    fixturePlacement.GridOrigin,
                    fixturePlacement.CellSize);
                ClearSession();
                return false;
            }

            error = null;
            return true;
        }

        public bool TryPreviewFromRay(Ray ray, out string error)
        {
            if (!IsActive)
            {
                error = "Enter fixture placement before selecting a grid cell.";
                return false;
            }

            Collider surface = propertyArea?.PlacementSurface ?? placementFloor;
            if (surface == null ||
                !surface.Raycast(ray, out RaycastHit hit, maximumRayDistance))
            {
                error = "Aim at the placement floor to select a grid cell.";
                InvalidateCurrentPreview();
                return false;
            }

            return TryPreviewAtWorldPoint(hit.point, out error);
        }

        public bool TryRefreshPreview(Ray ray, out string error)
        {
            return TryPreviewFromRay(ray, out error);
        }

        public bool TryPreviewAtWorldPoint(Vector3 worldPoint, out string error)
        {
            if (!TryGetGridPosition(worldPoint, out GridPosition gridPosition, out error))
            {
                InvalidateCurrentPreview();
                return false;
            }

            return TryPreviewAtGridPosition(gridPosition, out error);
        }

        public bool TryGetGridPosition(
            Vector3 worldPoint,
            out GridPosition gridPosition,
            out string error)
        {
            gridPosition = default;
            if (!TryValidateConfiguration(out error) || fixturePlacement.GridOrigin == null)
            {
                return false;
            }

            Vector3 localPoint = fixturePlacement.GridOrigin.InverseTransformPoint(worldPoint);
            gridPosition = new GridPosition(
                Mathf.FloorToInt(localPoint.x / fixturePlacement.CellSize),
                Mathf.FloorToInt(localPoint.z / fixturePlacement.CellSize));
            error = null;
            return true;
        }

        public bool TryRotate(int direction, out string error)
        {
            if (!IsActive || direction == 0)
            {
                error = "No active fixture preview can be rotated.";
                return false;
            }

            previewQuarterTurns = GridFootprint.NormalizeQuarterTurns(
                previewQuarterTurns + (direction > 0 ? 1 : -1));
            if (!hasPreview)
            {
                error = null;
                return true;
            }

            return TryPreviewAtGridPosition(previewPosition, out error);
        }

        public bool AdjustQuarterTurns(int direction, out string error)
        {
            return TryRotate(direction, out error);
        }

        public bool TryConfirm(out string error)
        {
            if (!IsActive || !hasPreview)
            {
                error = "Select a valid placement-floor cell before confirming.";
                return false;
            }

            if (previewResult == null || !previewResult.IsSuccess)
            {
                error = FormatResult(previewResult) ??
                        "Select a valid placement-floor cell before confirming.";
                return false;
            }

            if (!propertyArea.TryValidateFixturePlacement(
                    activeFixture,
                    fixturePlacement.GridOrigin,
                    fixturePlacement.CellSize,
                    previewQuarterTurns,
                    out FixturePlacementFailure physicalFailure,
                    out error))
            {
                previewResult = FixturePlacementResult.Reject(
                    physicalFailure,
                    activeFixture.StableFixtureInstanceId);
                activeFixture.SetPreviewState(FixturePlacementPreviewState.Invalid);
                error ??= FormatResult(previewResult);
                return false;
            }

            FixturePlacementResult result = isMove
                ? fixturePlacement.TryMove(activeFixture, previewPosition, previewQuarterTurns)
                : fixturePlacement.TryPlace(activeFixture, previewPosition, previewQuarterTurns);
            previewResult = result;
            if (!result.IsSuccess)
            {
                activeFixture.ApplyPreview(
                    previewPosition,
                    previewQuarterTurns,
                    fixturePlacement.GridOrigin,
                    fixturePlacement.CellSize,
                    false);
                error = FormatResult(result);
                return false;
            }

            ClearSession();
            error = null;
            return true;
        }

        public bool TryCancel(out string error)
        {
            if (!IsActive)
            {
                error = "No fixture placement is active.";
                return false;
            }

            if (isMove && priorPlacement != null)
            {
                activeFixture.ApplyPlacement(
                    priorPlacement,
                    fixturePlacement.GridOrigin,
                    fixturePlacement.CellSize);
            }
            else
            {
                activeFixture.ClearPlacement();
            }

            ClearSession();
            error = null;
            return true;
        }

        public void ResetTransientStateAfterRestore()
        {
            if (activeFixture != null && fixturePlacement != null)
            {
                if (fixturePlacement.TryGetPlacement(
                        activeFixture.StableFixtureInstanceId,
                        out FixturePlacementSnapshot restoredPlacement))
                {
                    activeFixture.ApplyPlacement(
                        restoredPlacement,
                        fixturePlacement.GridOrigin,
                        fixturePlacement.CellSize);
                }
                else
                {
                    activeFixture.ClearPlacement();
                }
            }

            ClearSession();
            if (IsBuildModeActive)
            {
                IsBuildModeActive = false;
                BuildModeChanged?.Invoke(false, "Build Mode exited after loading.");
            }
        }

        public bool TryRemove(PlaceableFixtureComponent fixture, out string error)
        {
            if (!TryValidateConfiguration(out error) || fixture == null)
            {
                error ??= "This fixture is unavailable for removal.";
                return false;
            }

            if (IsActive)
            {
                error = "Finish or cancel fixture placement before removing a fixture.";
                return false;
            }

            FixturePlacementResult result = fixturePlacement.TryRemove(fixture);
            if (!result.IsSuccess)
            {
                error = FormatResult(result);
                return false;
            }

            error = null;
            return true;
        }

        public bool TryRemoveFixture(PlaceableFixtureComponent fixture, out string error)
        {
            return TryRemove(fixture, out error);
        }

        public bool TryGetEntryBlocker(
            PlaceableFixtureComponent fixture,
            out string blocker)
        {
            if (fixture == null || fixturePlacement == null ||
                !fixturePlacement.IsConfiguredFixture(fixture))
            {
                blocker = "fixture unavailable";
                return true;
            }

            if (fixturePlacement.IsFixtureModificationRestricted(fixture))
            {
                blocker = "fixture changes are unavailable while this fixture is in active use";
                return true;
            }

            blocker = null;
            return false;
        }

        public bool TryPrimary(out string error)
        {
            error = "Use Q to place or cancel the selected fixture.";
            return false;
        }

        private bool TryPreviewAtGridPosition(GridPosition gridPosition, out string error)
        {
            if (!IsActive)
            {
                error = "Enter fixture placement before selecting a grid cell.";
                return false;
            }

            previewPosition = gridPosition;
            hasPreview = true;
            previewResult = isMove
                ? fixturePlacement.PreviewMove(activeFixture, gridPosition, previewQuarterTurns)
                : fixturePlacement.PreviewPlace(activeFixture, gridPosition, previewQuarterTurns);
            activeFixture.ApplyPreview(
                gridPosition,
                previewQuarterTurns,
                fixturePlacement.GridOrigin,
                fixturePlacement.CellSize,
                previewResult.IsSuccess);

            if (previewResult.IsSuccess &&
                !propertyArea.TryValidateFixturePlacement(
                    activeFixture,
                    fixturePlacement.GridOrigin,
                    fixturePlacement.CellSize,
                    previewQuarterTurns,
                    out FixturePlacementFailure physicalFailure,
                    out string physicalError))
            {
                previewResult = FixturePlacementResult.Reject(
                    physicalFailure,
                    activeFixture.StableFixtureInstanceId);
                activeFixture.SetPreviewState(FixturePlacementPreviewState.Invalid);
                error = physicalError ?? FormatResult(previewResult);
                return false;
            }

            error = previewResult.IsSuccess ? null : FormatResult(previewResult);
            return previewResult.IsSuccess;
        }

        private void ClearSession()
        {
            activeFixture = null;
            priorPlacement = null;
            isMove = false;
            hasPreview = false;
            previewResult = null;
        }

        private void InvalidateCurrentPreview()
        {
            hasPreview = false;
            previewResult = null;
            activeFixture?.SetPreviewState(FixturePlacementPreviewState.Invalid);
        }

        private static string FormatResult(FixturePlacementResult result)
        {
            if (result == null || result.IsSuccess)
            {
                return null;
            }

            return result.Failure switch
            {
                FixturePlacementFailure.OutOfBounds => "That fixture footprint extends outside the placement grid.",
                FixturePlacementFailure.Occupied => "That grid space is occupied by another fixture.",
                FixturePlacementFailure.InvalidSupport =>
                    "The whole fixture must remain on supported owned property.",
                FixturePlacementFailure.StructuralCollision =>
                    "That fixture collides with the building or another structural obstacle.",
                FixturePlacementFailure.OperatingStateRestricted =>
                    "Fixture changes are unavailable while this fixture is in active use or required for current operations.",
                FixturePlacementFailure.MissingFixture => "This fixture is unavailable for placement.",
                _ => "That fixture placement is unavailable."
            };
        }
    }
}
