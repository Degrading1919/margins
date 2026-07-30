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

        private bool isActive;
        private bool hasInitializedState;

        public string StableTaskId => stableTaskId;
        public string DisplayName => string.IsNullOrWhiteSpace(name)
            ? "Cleaning task"
            : name;
        public int RequiredProgressUnits => requiredProgressUnits;
        public int CompletedProgressUnits { get; private set; }
        public bool IsActive
        {
            get
            {
                EnsureRuntimeState();
                return isActive;
            }
        }
        public bool NeedsCleaning
        {
            get
            {
                EnsureRuntimeState();
                return isActive && !IsComplete;
            }
        }
        public bool IsComplete =>
            GetIsComplete();

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

            if (IsComplete)
            {
                return CleaningProgressResult.AlreadyComplete;
            }

            if (progressUnits <= 0)
            {
                return CleaningProgressResult.InvalidAmount;
            }

            int remaining = requiredProgressUnits - CompletedProgressUnits;
            CompletedProgressUnits += Mathf.Min(remaining, progressUnits);
            return IsComplete
                ? CleaningProgressResult.Completed
                : CleaningProgressResult.Progressed;
        }

        public bool TryCreateMess()
        {
            EnsureRuntimeState();
            if (!TryValidateConfiguration(out _) || NeedsCleaning)
            {
                return false;
            }

            isActive = true;
            CompletedProgressUnits = 0;
            return true;
        }

        public CleaningTaskSnapshot CreateSnapshot()
        {
            EnsureRuntimeState();
            return new CleaningTaskSnapshot(
                stableTaskId,
                requiredProgressUnits,
                CompletedProgressUnits,
                isActive);
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

            CompletedProgressUnits = snapshot.completedProgressUnits;
            isActive = snapshot.isActive;
            hasInitializedState = true;
            return true;
        }

        private bool GetIsComplete()
        {
            EnsureRuntimeState();
            return !isActive ||
                   (requiredProgressUnits > 0 &&
                    CompletedProgressUnits >= requiredProgressUnits);
        }

        private void EnsureRuntimeState()
        {
            if (hasInitializedState)
            {
                return;
            }

            isActive = startsDirty;
            CompletedProgressUnits = 0;
            hasInitializedState = true;
        }
    }
}
