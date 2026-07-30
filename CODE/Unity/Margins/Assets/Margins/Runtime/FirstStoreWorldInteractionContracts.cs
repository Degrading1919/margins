using System;
using System.Collections.Generic;

namespace Margins
{
    public readonly struct FirstStoreInteractionFeedback
    {
        public FirstStoreInteractionFeedback(
            bool succeeded,
            string targetId,
            string action,
            string message)
        {
            Succeeded = succeeded;
            TargetId = targetId ?? string.Empty;
            Action = action ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public bool Succeeded { get; }
        public string TargetId { get; }
        public string Action { get; }
        public string Message { get; }
    }

    public enum FirstStoreWorldInteractionPriority
    {
        HeldPlacement = 0,
        Checkout = 1,
        Delivery = 2,
        LooseProduct = 3,
        Fixture = 4,
        Cleaning = 5,
        Operating = 6
    }

    public sealed class FirstStoreWorldInteractionPrompt
    {
        public FirstStoreWorldInteractionPrompt(
            string input,
            string action,
            string stateOrBlocker = null)
        {
            Input = input ?? string.Empty;
            Action = action ?? string.Empty;
            StateOrBlocker = stateOrBlocker ?? string.Empty;
        }

        public string Input { get; }
        public string Action { get; }
        public string StateOrBlocker { get; }

        public string FormattedText => string.IsNullOrWhiteSpace(StateOrBlocker)
            ? $"[{Input}] {Action}"
            : $"[{Input}] {Action} — {StateOrBlocker}";
    }

    public interface IFirstStoreWorldInteractionTarget
    {
        string StableTargetId { get; }
        FirstStoreWorldInteractionPriority Priority { get; }
        bool IsAvailable { get; }
        FirstStoreWorldInteractionPrompt Prompt { get; }

        bool TryPrimary(out string error);
        bool TryCancel(out string error);
    }

    public readonly struct FirstStoreWorldInteractionCandidate
    {
        public FirstStoreWorldInteractionCandidate(
            IFirstStoreWorldInteractionTarget target,
            float distance)
        {
            Target = target;
            Distance = distance;
        }

        public IFirstStoreWorldInteractionTarget Target { get; }
        public float Distance { get; }

        public bool IsValid =>
            Target != null &&
            Target.IsAvailable &&
            !string.IsNullOrWhiteSpace(Target.StableTargetId) &&
            Target.Priority >= FirstStoreWorldInteractionPriority.HeldPlacement &&
            Target.Priority <= FirstStoreWorldInteractionPriority.Operating &&
            !float.IsNaN(Distance) &&
            !float.IsInfinity(Distance) &&
            Distance >= 0f;
    }

    public static class FirstStoreWorldInteractionTargetResolver
    {
        public static IFirstStoreWorldInteractionTarget Resolve(
            IEnumerable<FirstStoreWorldInteractionCandidate> candidates)
        {
            if (candidates == null)
            {
                return null;
            }

            FirstStoreWorldInteractionCandidate? selected = null;
            foreach (FirstStoreWorldInteractionCandidate candidate in candidates)
            {
                if (!candidate.IsValid ||
                    (selected.HasValue && !IsPreferred(candidate, selected.Value)))
                {
                    continue;
                }

                selected = candidate;
            }

            return selected.HasValue ? selected.Value.Target : null;
        }

        private static bool IsPreferred(
            FirstStoreWorldInteractionCandidate candidate,
            FirstStoreWorldInteractionCandidate selected)
        {
            int priorityComparison = candidate.Target.Priority.CompareTo(selected.Target.Priority);
            if (priorityComparison != 0)
            {
                return priorityComparison < 0;
            }

            int distanceComparison = candidate.Distance.CompareTo(selected.Distance);
            if (distanceComparison != 0)
            {
                return distanceComparison < 0;
            }

            return StringComparer.Ordinal.Compare(
                       candidate.Target.StableTargetId,
                       selected.Target.StableTargetId) < 0;
        }
    }
}
