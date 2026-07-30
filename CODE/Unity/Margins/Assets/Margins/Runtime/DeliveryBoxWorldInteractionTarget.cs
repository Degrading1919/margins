using UnityEngine;

namespace Margins
{
    /// <summary>
    /// A physical container interaction: pick the box up, move it, set it down,
    /// open it, and only then target its visible contents.
    /// </summary>
    public sealed class DeliveryBoxWorldInteractionTarget :
        MonoBehaviour,
        IFirstStoreWorldInteractionTarget
    {
        [SerializeField] private string stableTargetId;
        [SerializeField] private DeliveryBoxComponent deliveryBox;
        [SerializeField] private StockingController stocking;
        [SerializeField] private Transform carryPoint;
        [SerializeField] private Transform playerBody;
        [SerializeField, Min(0.25f)] private float setDownDistance = 1.25f;
        [SerializeField] private float setDownHeight = 0.48f;

        public string StableTargetId => stableTargetId;
        public FirstStoreWorldInteractionPriority Priority =>
            FirstStoreWorldInteractionPriority.Delivery;
        public bool IsAvailable =>
            FirstStoreIdentifier.IsValid(stableTargetId) &&
            deliveryBox != null &&
            deliveryBox.IsInitialized &&
            stocking != null &&
            carryPoint != null &&
            playerBody != null;

        public FirstStoreWorldInteractionPrompt Prompt
        {
            get
            {
                if (!IsAvailable)
                {
                    return new FirstStoreWorldInteractionPrompt(
                        "E",
                        "Use delivery box",
                        "unavailable");
                }

                if (deliveryBox.IsCarried && deliveryBox.IsSealed)
                {
                    return new FirstStoreWorldInteractionPrompt(
                        "E",
                        "Open carried delivery",
                        "Q sets box down");
                }

                if (deliveryBox.IsCarried)
                {
                    return new FirstStoreWorldInteractionPrompt(
                        "E",
                        "Set delivery down",
                        "open container; contents stay inside");
                }

                if (stocking.HasHeldUnit)
                {
                    return new FirstStoreWorldInteractionPrompt(
                        "E",
                        "Pick up delivery",
                        "hands full");
                }

                return new FirstStoreWorldInteractionPrompt(
                    "E",
                    "Pick up delivery",
                    deliveryBox.IsOpen ? "open container" : "sealed container");
            }
        }

        public bool TryPrimary(out string error)
        {
            if (!IsAvailable)
            {
                error = "The delivery box is unavailable.";
                return false;
            }

            if (!deliveryBox.IsCarried)
            {
                if (stocking.HasHeldUnit)
                {
                    error = "Stock or put down the held product first.";
                    return false;
                }
                return deliveryBox.TryPickUp(carryPoint, out error);
            }

            if (deliveryBox.IsSealed)
            {
                return deliveryBox.TryOpen(out _, out error);
            }

            return TrySetDown(out error);
        }

        public bool TryCancel(out string error)
        {
            if (!IsAvailable || !deliveryBox.IsCarried)
            {
                error = "The delivery box is not being carried.";
                return false;
            }

            return TrySetDown(out error);
        }

        private bool TrySetDown(out string error)
        {
            Vector3 forward = Vector3.ProjectOnPlane(
                playerBody.forward,
                Vector3.up).normalized;
            Vector3 position = playerBody.position + forward * setDownDistance;
            position.y = setDownHeight;
            Quaternion rotation = Quaternion.Euler(0f, playerBody.eulerAngles.y, 0f);
            return deliveryBox.TrySetDown(position, rotation, out error);
        }
    }
}
