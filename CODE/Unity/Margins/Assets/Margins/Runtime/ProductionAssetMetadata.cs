using UnityEngine;

namespace Margins
{
    [DisallowMultipleComponent]
    public sealed class ProductionAssetMetadata : MonoBehaviour
    {
        [SerializeField] private string assetId;
        [SerializeField] private Vector3 expectedDimensionsMeters = Vector3.one;
        [SerializeField, Min(0.0001f)] private float dimensionToleranceMeters = 0.01f;
        [SerializeField] private Transform visualRoot;

        public string AssetId => assetId;
        public Vector3 ExpectedDimensionsMeters => expectedDimensionsMeters;
        public float DimensionToleranceMeters => dimensionToleranceMeters;
        public Transform VisualRoot => visualRoot;

        public void Configure(
            string id,
            Vector3 dimensionsMeters,
            float toleranceMeters,
            Transform normalizedVisualRoot)
        {
            assetId = id;
            expectedDimensionsMeters = dimensionsMeters;
            dimensionToleranceMeters = toleranceMeters;
            visualRoot = normalizedVisualRoot;
        }
    }
}
