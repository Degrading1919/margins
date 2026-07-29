using UnityEngine;

namespace Margins
{
    public sealed class DeliveryProductWorldInteractionTarget : MonoBehaviour, IFirstStoreWorldInteractionTarget
    {
        [SerializeField] private string stableTargetId;
        [SerializeField] private DeliveryBoxComponent deliveryBox;
        [SerializeField] private ProductDefinition productDefinition;

        public string StableTargetId => stableTargetId;
        public FirstStoreWorldInteractionPriority Priority => FirstStoreWorldInteractionPriority.Delivery;
        public bool IsAvailable => HasValidReference() && deliveryBox.IsOpen;
        public FirstStoreWorldInteractionPrompt Prompt
        {
            get
            {
                string productName = GetProductName();
                if (!TryGetRemaining(out int remainingUnits))
                {
                    return new FirstStoreWorldInteractionPrompt(
                        "E",
                        $"Take {productName}",
                        "delivery unavailable");
                }

                string state = deliveryBox != null && !deliveryBox.IsOpen
                    ? $"delivery sealed; {remainingUnits} left"
                    : $"{remainingUnits} left";
                return new FirstStoreWorldInteractionPrompt(
                    "E",
                    $"Take {productName}",
                    state);
            }
        }

        public bool TryPrimary(out string error)
        {
            if (!HasValidReference())
            {
                error = "This delivery product is unavailable.";
                return false;
            }

            string productName = GetProductName();
            if (!deliveryBox.IsOpen)
            {
                error = $"Open the delivery before taking {productName}.";
                return false;
            }

            if (!TryGetRemaining(out int remainingUnits))
            {
                error = $"{productName} is unavailable from this delivery.";
                return false;
            }

            if (remainingUnits <= 0)
            {
                error = $"No {productName} remain in this delivery.";
                return false;
            }

            if (!deliveryBox.TryRemoveOneUnit(
                    productDefinition,
                    out _,
                    out _,
                    out _,
                    out _))
            {
                error = $"{productName} could not be removed from this delivery.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryCancel(out string error)
        {
            error = "Delivery removal cannot be cancelled.";
            return false;
        }

        private bool HasValidReference()
        {
            return FirstStoreIdentifier.IsValid(stableTargetId) &&
                   deliveryBox != null &&
                   deliveryBox.IsInitialized &&
                   productDefinition != null &&
                   FirstStoreIdentifier.IsValid(productDefinition.StableProductId);
        }

        private string GetProductName()
        {
            if (productDefinition == null)
            {
                return "product";
            }

            return string.IsNullOrWhiteSpace(productDefinition.DisplayName)
                ? "product"
                : productDefinition.DisplayName;
        }

        private bool TryGetRemaining(out int remainingUnits)
        {
            remainingUnits = 0;
            return deliveryBox != null &&
                   deliveryBox.TryGetConfiguredProductRemaining(
                       productDefinition,
                       out _,
                       out remainingUnits,
                       out _);
        }
    }
}
