using UnityEngine;

namespace Margins
{
    public sealed class DeliveryOpenWorldInteractionTarget : MonoBehaviour, IFirstStoreWorldInteractionTarget
    {
        [SerializeField] private string stableTargetId;
        [SerializeField] private DeliveryBoxComponent deliveryBox;

        public string StableTargetId => stableTargetId;
        public FirstStoreWorldInteractionPriority Priority => FirstStoreWorldInteractionPriority.Delivery;
        public bool IsAvailable =>
            FirstStoreIdentifier.IsValid(stableTargetId) &&
            deliveryBox != null &&
            deliveryBox.IsInitialized &&
            deliveryBox.IsSealed;
        public FirstStoreWorldInteractionPrompt Prompt => new(
            "E",
            "Open delivery",
            deliveryBox != null && deliveryBox.IsOpen ? "already open" : null);

        public bool TryPrimary(out string error)
        {
            if (!HasValidReference())
            {
                error = "Delivery is unavailable.";
                return false;
            }

            if (!deliveryBox.TryOpen(out _, out _))
            {
                error = "Delivery cannot be opened right now.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryCancel(out string error)
        {
            error = "Delivery opening cannot be cancelled.";
            return false;
        }

        private bool HasValidReference()
        {
            return FirstStoreIdentifier.IsValid(stableTargetId) &&
                   deliveryBox != null &&
                   deliveryBox.IsInitialized;
        }
    }
}
