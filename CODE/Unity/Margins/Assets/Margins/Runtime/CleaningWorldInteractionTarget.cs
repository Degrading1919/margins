using UnityEngine;

namespace Margins
{
    public sealed class CleaningWorldInteractionTarget : MonoBehaviour, IFirstStoreWorldInteractionTarget
    {
        [SerializeField] private string stableTargetId;
        [SerializeField] private CleaningTaskComponent cleaningTask;
        [SerializeField] private PlayerCarryableToolController toolCarrier;
        [SerializeField] private string requiredToolCapabilityId = "clean-floor";

        private Renderer[] visualRenderers;
        private Collider[] interactionColliders;

        public string StableTargetId => stableTargetId;
        public FirstStoreWorldInteractionPriority Priority => FirstStoreWorldInteractionPriority.Cleaning;
        public bool IsAvailable =>
            FirstStoreIdentifier.IsValid(stableTargetId) &&
            FirstStoreIdentifier.IsValid(requiredToolCapabilityId) &&
            cleaningTask != null &&
            toolCarrier != null &&
            cleaningTask.TryValidateConfiguration(out _) &&
            cleaningTask.NeedsCleaning;
        public FirstStoreWorldInteractionPrompt Prompt
        {
            get
            {
                string taskName = cleaningTask == null ? "Cleaning task" : cleaningTask.DisplayName;
                string state = cleaningTask != null && cleaningTask.NeedsCleaning &&
                               toolCarrier != null &&
                               !toolCarrier.HasCapability(requiredToolCapabilityId)
                    ? "requires a compatible cleaning tool"
                    : cleaningTask != null && !cleaningTask.IsActive
                    ? "store is clean"
                    : cleaningTask != null && cleaningTask.IsComplete
                    ? "cleaned"
                    : cleaningTask == null
                        ? "unavailable"
                        : $"{cleaningTask.CompletedProgressUnits}/{cleaningTask.RequiredProgressUnits}";
                return new FirstStoreWorldInteractionPrompt("E", $"Clean {taskName}", state);
            }
        }

        private void Awake()
        {
            visualRenderers = GetComponentsInChildren<Renderer>(true);
            interactionColliders = GetComponentsInChildren<Collider>(true);
        }

        private void LateUpdate()
        {
            bool visible = cleaningTask != null && cleaningTask.NeedsCleaning;
            if (visualRenderers != null)
            {
                foreach (Renderer visual in visualRenderers)
                {
                    if (visual != null)
                    {
                        visual.enabled = visible;
                    }
                }
            }
            if (interactionColliders != null)
            {
                foreach (Collider interactionCollider in interactionColliders)
                {
                    if (interactionCollider != null)
                    {
                        interactionCollider.enabled = visible;
                    }
                }
            }
        }

        public bool TryPrimary(out string error)
        {
            if (!IsAvailable)
            {
                error = "There is no dirt or spill to clean here.";
                return false;
            }

            if (!toolCarrier.HasCapability(requiredToolCapabilityId))
            {
                error = "Pick up the compatible cleaning tool before cleaning this spill.";
                return false;
            }

            CleaningProgressResult result = cleaningTask.TryApplyProgress(1);
            if (result == CleaningProgressResult.InvalidConfiguration ||
                result == CleaningProgressResult.InvalidAmount)
            {
                error = "This cleaning task cannot be progressed right now.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryCancel(out string error)
        {
            error = "Cleaning progress cannot be cancelled.";
            return false;
        }
    }
}
