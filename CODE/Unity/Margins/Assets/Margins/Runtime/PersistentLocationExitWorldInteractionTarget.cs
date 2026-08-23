using UnityEngine;

namespace Margins
{
    public sealed class PersistentLocationExitWorldInteractionTarget :
        MonoBehaviour,
        IFirstStoreWorldInteractionTarget
    {
        [SerializeField] private string stableTargetId;
        [SerializeField] private PersistentPortfolioLocationSceneAdapter adapter;

        public string StableTargetId => stableTargetId;
        public FirstStoreWorldInteractionPriority Priority =>
            FirstStoreWorldInteractionPriority.Operating;
        public bool IsAvailable =>
            FirstStoreIdentifier.IsValid(stableTargetId) &&
            adapter != null && adapter.HasActiveGeneratedLocation;
        public FirstStoreWorldInteractionPrompt Prompt => new(
            "E",
            "Leave location",
            "return to company management");

        public void Configure(
            string targetId,
            PersistentPortfolioLocationSceneAdapter sceneAdapter)
        {
            stableTargetId = targetId;
            adapter = sceneAdapter;
        }

        public bool TryPrimary(out string error)
        {
            if (!IsAvailable)
            {
                error = "No generated business location is active.";
                return false;
            }
            return adapter.TryLeaveToManagement(out error);
        }

        public bool TryCancel(out string error)
        {
            error = "Location travel has no secondary action.";
            return false;
        }
    }
}
