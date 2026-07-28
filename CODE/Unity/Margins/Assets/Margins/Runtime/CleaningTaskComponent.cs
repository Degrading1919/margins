// Draft implementation — Unity verification pending
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

        public string StableTaskId => stableTaskId;
        public int RequiredProgressUnits => requiredProgressUnits;
        public int CompletedProgressUnits { get; private set; }
        public bool IsComplete =>
            requiredProgressUnits > 0 &&
            CompletedProgressUnits >= requiredProgressUnits;

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

        public CleaningTaskSnapshot CreateSnapshot()
        {
            return new CleaningTaskSnapshot(
                stableTaskId,
                requiredProgressUnits,
                CompletedProgressUnits);
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
            return true;
        }
    }
}
