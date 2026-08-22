using UnityEngine;

namespace Margins
{
    public sealed class ProceduralSocketComponent : MonoBehaviour
    {
        [SerializeField] private string socketId;
        [SerializeField] private string compatibilityTag;

        public string SocketId => socketId;
        public string CompatibilityTag => compatibilityTag;

        public void Configure(string id, string compatibility)
        {
            socketId = id;
            compatibilityTag = compatibility;
        }

        public bool TryValidate(out string error)
        {
            if (!StableIdentifier.IsValid(socketId) ||
                !StableIdentifier.IsValid(compatibilityTag))
            {
                error = "Sockets require stable socket and compatibility identifiers.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
