using UnityEngine;

namespace Margins
{
    public enum ProductFootprint
    {
        Small
    }

    [CreateAssetMenu(fileName = "ProductDefinition", menuName = "Margins/Product Definition")]
    public sealed class ProductDefinition : ScriptableObject
    {
        [SerializeField] private string stableProductId;
        [SerializeField] private string displayName;
        [SerializeField] private GameObject visualPrefab;
        [SerializeField] private ProductFootprint shelfFootprint;
        [SerializeField] private string snapCompatibilityTag;

        public string StableProductId => stableProductId;
        public string DisplayName => displayName;
        public GameObject VisualPrefab => visualPrefab;
        public ProductFootprint ShelfFootprint => shelfFootprint;
        public string SnapCompatibilityTag => snapCompatibilityTag;
    }
}
