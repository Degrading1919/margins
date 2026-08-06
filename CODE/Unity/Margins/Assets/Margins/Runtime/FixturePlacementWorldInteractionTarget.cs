using UnityEngine;

namespace Margins
{
    public interface IFirstStoreRemovableWorldInteractionTarget
    {
        bool TryRemove(out string error);
    }

    /// <summary>
    /// Explicit fixture or handle target. A separate active handle may reference an
    /// inactive removed fixture, so re-placement never depends on object discovery.
    /// </summary>
    public sealed class FixturePlacementWorldInteractionTarget :
        MonoBehaviour,
        IFirstStoreWorldInteractionTarget,
        IFirstStoreRemovableWorldInteractionTarget
    {
        [SerializeField] private string stableTargetId;
        [SerializeField] private FirstStoreFixturePlacementModeController placementMode;
        [SerializeField] private PlaceableFixtureComponent fixture;
        [SerializeField] private bool allowsUnplacedFixture = true;

        public string StableTargetId => stableTargetId;
        public FirstStoreWorldInteractionPriority Priority => FirstStoreWorldInteractionPriority.Fixture;
        public bool IsAvailable =>
            FirstStoreIdentifier.IsValid(stableTargetId) &&
            placementMode != null &&
            fixture != null &&
            (allowsUnplacedFixture || placementMode.ActiveFixture == fixture || fixture.gameObject.activeInHierarchy);

        public FirstStoreWorldInteractionPrompt Prompt
        {
            get
            {
                if (!IsAvailable)
                {
                    return new FirstStoreWorldInteractionPrompt(
                        "E",
                        "Place fixture",
                        "fixture unavailable");
                }

                if (placementMode.IsActive && placementMode.ActiveFixture == fixture)
                {
                    string previewState = placementMode.HasPreview
                        ? placementMode.PreviewReason ?? "valid grid position"
                        : "aim at the placement floor";
                    return new FirstStoreWorldInteractionPrompt(
                        "E",
                        "Confirm fixture",
                        $"{previewState}; mouse wheel rotates; Q cancels");
                }

                string action = placementMode.IsPlaced(fixture)
                    ? "Move fixture"
                    : "Place fixture";
                string entryState = placementMode.TryGetEntryBlocker(
                    fixture,
                    out string blocker)
                        ? blocker
                        : placementMode.IsPlaced(fixture)
                            ? "Backspace removes; mouse wheel rotates after selection; Q cancels"
                            : "mouse wheel rotates after selection; Q cancels";
                return new FirstStoreWorldInteractionPrompt(
                    "E",
                    action,
                    entryState);
            }
        }

        public bool TryPrimary(out string error)
        {
            if (!IsAvailable)
            {
                error = "Fixture placement is unavailable.";
                return false;
            }

            return placementMode.IsActive && placementMode.ActiveFixture == fixture
                ? placementMode.TryConfirm(out error)
                : placementMode.TryBegin(fixture, out error);
        }

        public bool TryCancel(out string error)
        {
            if (!IsAvailable || !placementMode.IsActive || placementMode.ActiveFixture != fixture)
            {
                error = "No placement preview for this fixture is active.";
                return false;
            }

            return placementMode.TryCancel(out error);
        }

        public bool TryRemove(out string error)
        {
            if (!IsAvailable)
            {
                error = "Fixture removal is unavailable.";
                return false;
            }

            return placementMode.TryRemove(fixture, out error);
        }
    }
}
