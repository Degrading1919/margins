using UnityEngine;

namespace Margins
{
    public sealed class ProceduralAnchorComponent : MonoBehaviour
    {
        [SerializeField] private string anchorId;
        [SerializeField] private string compatibilityTag;

        public string AnchorId => anchorId;
        public string CompatibilityTag => compatibilityTag;

        public void Configure(string id, string compatibility)
        {
            anchorId = id;
            compatibilityTag = compatibility;
        }

        public bool TryValidate(out string error)
        {
            if (!StableIdentifier.IsValid(anchorId) ||
                !StableIdentifier.IsValid(compatibilityTag))
            {
                error = "Extension anchors require stable anchor and compatibility identifiers.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
