using UnityEngine;

namespace Margins
{
    public sealed class CleaningWorldInteractionTarget : MonoBehaviour, IFirstStoreWorldInteractionTarget
    {
        [SerializeField] private string stableTargetId;
        [SerializeField] private CleaningTaskComponent cleaningTask;

        public string StableTargetId => stableTargetId;
        public FirstStoreWorldInteractionPriority Priority => FirstStoreWorldInteractionPriority.Cleaning;
        public bool IsAvailable =>
            FirstStoreIdentifier.IsValid(stableTargetId) &&
            cleaningTask != null &&
            cleaningTask.TryValidateConfiguration(out _);
        public FirstStoreWorldInteractionPrompt Prompt
        {
            get
            {
                string taskName = cleaningTask == null ? "Cleaning task" : cleaningTask.DisplayName;
                string state = cleaningTask != null && cleaningTask.IsComplete
                    ? "already complete"
                    : cleaningTask == null
                        ? "unavailable"
                        : $"{cleaningTask.CompletedProgressUnits}/{cleaningTask.RequiredProgressUnits}";
                return new FirstStoreWorldInteractionPrompt("E", $"Clean {taskName}", state);
            }
        }

        public bool TryPrimary(out string error)
        {
            if (!IsAvailable)
            {
                error = "This cleaning task is unavailable.";
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
