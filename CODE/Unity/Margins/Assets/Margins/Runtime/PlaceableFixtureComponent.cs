using UnityEngine;

namespace Margins
{
    public sealed class PlaceableFixtureComponent : MonoBehaviour
    {
        [SerializeField] private string stableFixtureInstanceId;
        [SerializeField, Min(1)] private int footprintWidthCells = 1;
        [SerializeField, Min(1)] private int footprintDepthCells = 1;
        [SerializeField] private Renderer previewRenderer;
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Material validMaterial;
        [SerializeField] private Material invalidMaterial;

        public string StableFixtureInstanceId => stableFixtureInstanceId;
        public GridFootprint Footprint =>
            new(footprintWidthCells, footprintDepthCells);
        public FixturePlacementPreviewState PreviewState { get; private set; }

        public bool TryValidateConfiguration(out string error)
        {
            if (!FirstStoreIdentifier.IsValid(stableFixtureInstanceId))
            {
                error = $"Fixture '{name}' requires a valid stable instance id.";
                return false;
            }

            if (footprintWidthCells <= 0 || footprintDepthCells <= 0)
            {
                error = $"Fixture '{stableFixtureInstanceId}' requires a positive footprint.";
                return false;
            }

            if (previewRenderer != null &&
                (defaultMaterial == null || validMaterial == null || invalidMaterial == null))
            {
                error =
                    $"Fixture '{stableFixtureInstanceId}' preview renderer requires all three materials.";
                return false;
            }

            error = null;
            return true;
        }

        public void SetPreviewState(FixturePlacementPreviewState state)
        {
            PreviewState = state;
            if (previewRenderer == null)
            {
                return;
            }

            Material material = state switch
            {
                FixturePlacementPreviewState.Valid => validMaterial,
                FixturePlacementPreviewState.Invalid => invalidMaterial,
                _ => defaultMaterial
            };
            if (material != null)
            {
                previewRenderer.sharedMaterial = material;
            }
        }

        public void ApplyPlacement(
            FixturePlacementSnapshot placement,
            Transform gridOrigin,
            float cellSize)
        {
            gameObject.SetActive(true);
            GridFootprint rotated = placement.RotatedFootprint;
            Vector3 localCenter = new(
                (placement.gridPosition.x + rotated.width * 0.5f) * cellSize,
                0f,
                (placement.gridPosition.z + rotated.depth * 0.5f) * cellSize);
            transform.SetPositionAndRotation(
                gridOrigin.TransformPoint(localCenter),
                gridOrigin.rotation *
                Quaternion.Euler(0f, placement.quarterTurns * 90f, 0f));
            SetPreviewState(FixturePlacementPreviewState.None);
        }

        public void ApplyPreview(
            GridPosition gridPosition,
            int quarterTurns,
            Transform gridOrigin,
            float cellSize,
            bool isValid)
        {
            if (gridOrigin == null || cellSize <= 0f)
            {
                SetPreviewState(FixturePlacementPreviewState.Invalid);
                return;
            }

            gameObject.SetActive(true);
            int normalizedTurns = GridFootprint.NormalizeQuarterTurns(quarterTurns);
            GridFootprint rotated = Footprint.Rotate(normalizedTurns);
            Vector3 localCenter = new(
                (gridPosition.x + rotated.width * 0.5f) * cellSize,
                0f,
                (gridPosition.z + rotated.depth * 0.5f) * cellSize);
            transform.SetPositionAndRotation(
                gridOrigin.TransformPoint(localCenter),
                gridOrigin.rotation * Quaternion.Euler(0f, normalizedTurns * 90f, 0f));
            SetPreviewState(isValid
                ? FixturePlacementPreviewState.Valid
                : FixturePlacementPreviewState.Invalid);
        }

        public void ClearPlacement()
        {
            SetPreviewState(FixturePlacementPreviewState.None);
            gameObject.SetActive(false);
        }
    }
}
