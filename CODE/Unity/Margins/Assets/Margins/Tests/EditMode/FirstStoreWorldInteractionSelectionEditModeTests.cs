using NUnit.Framework;

namespace Margins.Tests
{
    public sealed class FirstStoreWorldInteractionSelectionEditModeTests
    {
        [Test]
        public void ResolvePrefersConfiguredPriorityBeforeDistance()
        {
            TestTarget operating = new("operating-control", FirstStoreWorldInteractionPriority.Operating);
            TestTarget heldPlacement = new("held-shelf", FirstStoreWorldInteractionPriority.HeldPlacement);

            IFirstStoreWorldInteractionTarget selected =
                FirstStoreWorldInteractionTargetResolver.Resolve(new[]
                {
                    new FirstStoreWorldInteractionCandidate(operating, 0.1f),
                    new FirstStoreWorldInteractionCandidate(heldPlacement, 2f)
                });

            Assert.That(selected, Is.SameAs(heldPlacement));
        }

        [Test]
        public void ResolvePrefersNearestCandidateWithinSamePriority()
        {
            TestTarget distant = new("delivery-distant", FirstStoreWorldInteractionPriority.Delivery);
            TestTarget nearby = new("delivery-nearby", FirstStoreWorldInteractionPriority.Delivery);

            IFirstStoreWorldInteractionTarget selected =
                FirstStoreWorldInteractionTargetResolver.Resolve(new[]
                {
                    new FirstStoreWorldInteractionCandidate(distant, 2f),
                    new FirstStoreWorldInteractionCandidate(nearby, 1f)
                });

            Assert.That(selected, Is.SameAs(nearby));
        }

        [Test]
        public void ResolveUsesOrdinalStableIdForEqualPriorityAndDistance()
        {
            TestTarget later = new("target-z", FirstStoreWorldInteractionPriority.Cleaning);
            TestTarget earlier = new("target-a", FirstStoreWorldInteractionPriority.Cleaning);

            IFirstStoreWorldInteractionTarget selected =
                FirstStoreWorldInteractionTargetResolver.Resolve(new[]
                {
                    new FirstStoreWorldInteractionCandidate(later, 1f),
                    new FirstStoreWorldInteractionCandidate(earlier, 1f)
                });

            Assert.That(selected, Is.SameAs(earlier));
        }

        [Test]
        public void ResolveIgnoresNullUnavailableAndInvalidCandidates()
        {
            TestTarget unavailable = new(
                "checkout-unavailable",
                FirstStoreWorldInteractionPriority.Checkout,
                isAvailable: false);
            TestTarget blankId = new("", FirstStoreWorldInteractionPriority.Delivery);
            TestTarget invalidPriority = new(
                "invalid-priority",
                (FirstStoreWorldInteractionPriority)(-1));
            TestTarget available = new("loose-valid", FirstStoreWorldInteractionPriority.LooseProduct);

            IFirstStoreWorldInteractionTarget selected =
                FirstStoreWorldInteractionTargetResolver.Resolve(new[]
                {
                    new FirstStoreWorldInteractionCandidate(null, 0f),
                    new FirstStoreWorldInteractionCandidate(unavailable, 0f),
                    new FirstStoreWorldInteractionCandidate(blankId, 0f),
                    new FirstStoreWorldInteractionCandidate(invalidPriority, 0f),
                    new FirstStoreWorldInteractionCandidate(available, 1f),
                    new FirstStoreWorldInteractionCandidate(available, -1f)
                });

            Assert.That(selected, Is.SameAs(available));
        }

        [Test]
        public void PromptFormatsInputActionAndShortStateOrBlocker()
        {
            FirstStoreWorldInteractionPrompt blocked =
                new("E", "Open store", "1 prerequisite missing");
            FirstStoreWorldInteractionPrompt actionOnly =
                new("Q", "Cancel placement");

            Assert.That(
                blocked.FormattedText,
                Is.EqualTo("[E] Open store — 1 prerequisite missing"));
            Assert.That(actionOnly.FormattedText, Is.EqualTo("[Q] Cancel placement"));
        }

        private sealed class TestTarget : IFirstStoreWorldInteractionTarget
        {
            public TestTarget(
                string stableTargetId,
                FirstStoreWorldInteractionPriority priority,
                bool isAvailable = true)
            {
                StableTargetId = stableTargetId;
                Priority = priority;
                IsAvailable = isAvailable;
                Prompt = new FirstStoreWorldInteractionPrompt("E", "Test target");
            }

            public string StableTargetId { get; }
            public FirstStoreWorldInteractionPriority Priority { get; }
            public bool IsAvailable { get; }
            public FirstStoreWorldInteractionPrompt Prompt { get; }

            public bool TryPrimary(out string error)
            {
                error = null;
                return true;
            }

            public bool TryCancel(out string error)
            {
                error = null;
                return true;
            }
        }
    }
}
