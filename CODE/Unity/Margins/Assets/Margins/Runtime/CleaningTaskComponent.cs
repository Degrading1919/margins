using UnityEngine;

namespace Margins
{
    public enum CleaningProgressResult
    {
        Progressed,
        Completed,
        AlreadyComplete,
        InvalidAmount,
        InvalidConfiguration
    }

    public sealed class CleaningTaskComponent : MonoBehaviour
    {
        [SerializeField] private string stableTaskId;
        [SerializeField, Min(1)] private int requiredProgressUnits = 4;
        [SerializeField] private bool startsDirty = true;

        private BusinessTaskProgress progress;

        public string StableTaskId => stableTaskId;
        public string DisplayName => string.IsNullOrWhiteSpace(name)
            ? "Cleaning task"
            : name;
        public int RequiredProgressUnits => requiredProgressUnits;
        public int CompletedProgressUnits
        {
            get
            {
                EnsureRuntimeState();
                return progress.CompletedWorkUnits;
            }
        }
        public bool IsActive
        {
            get
            {
                EnsureRuntimeState();
                return progress.IsActive;
            }
        }
        public bool NeedsCleaning
        {
            get
            {
                EnsureRuntimeState();
                return progress.IsActive && !progress.IsComplete;
            }
        }
        public bool IsComplete
        {
            get
            {
                EnsureRuntimeState();
                return progress.IsComplete;
            }
        }

        private void Awake()
        {
            EnsureRuntimeState();
        }

        public bool TryValidateConfiguration(out string error)
        {
            if (!FirstStoreIdentifier.IsValid(stableTaskId) ||
                requiredProgressUnits <= 0)
            {
                error =
                    $"Cleaning task '{name}' requires a valid id and positive work total.";
                return false;
            }

            error = null;
            return true;
        }

        public CleaningProgressResult TryApplyProgress(int progressUnits)
        {
            EnsureRuntimeState();
            if (!TryValidateConfiguration(out _))
            {
                return CleaningProgressResult.InvalidConfiguration;
            }

            BusinessTaskProgressResult result =
                progress.TryApplyWork(progressUnits);
            return result switch
            {
                BusinessTaskProgressResult.Progressed =>
                    CleaningProgressResult.Progressed,
                BusinessTaskProgressResult.Completed =>
                    CleaningProgressResult.Completed,
                BusinessTaskProgressResult.AlreadyComplete =>
                    CleaningProgressResult.AlreadyComplete,
                _ => CleaningProgressResult.InvalidAmount
            };
        }

        public bool TryCreateMess()
        {
            EnsureRuntimeState();
            if (!TryValidateConfiguration(out _) || NeedsCleaning)
            {
                return false;
            }

            return progress.TryActivate();
        }

        public CleaningTaskSnapshot CreateSnapshot()
        {
            EnsureRuntimeState();
            return new CleaningTaskSnapshot(
                stableTaskId,
                requiredProgressUnits,
                CompletedProgressUnits,
                IsActive);
        }

        public bool CanApplySnapshot(
            CleaningTaskSnapshot snapshot,
            out string error)
        {
            if (snapshot == null ||
                !snapshot.IsValid ||
                !string.Equals(
                    snapshot.taskId,
                    stableTaskId,
                    System.StringComparison.Ordinal) ||
                snapshot.requiredProgressUnits != requiredProgressUnits)
            {
                error =
                    $"Cleaning snapshot does not match configured task '{stableTaskId}'.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryApplySnapshot(
            CleaningTaskSnapshot snapshot,
            out string error)
        {
            if (!CanApplySnapshot(snapshot, out error))
            {
                return false;
            }

            BusinessTaskProgress restored = new(
                requiredProgressUnits,
                snapshot.isActive);
            if (!restored.TryRestore(
                    snapshot.completedProgressUnits,
                    snapshot.isActive))
            {
                error = "Cleaning task progress could not be restored.";
                return false;
            }

            progress = restored;
            return true;
        }

        private void EnsureRuntimeState()
        {
            if (progress != null)
            {
                return;
            }

            progress = new BusinessTaskProgress(
                Mathf.Max(1, requiredProgressUnits),
                startsDirty);
        }
    }
}
