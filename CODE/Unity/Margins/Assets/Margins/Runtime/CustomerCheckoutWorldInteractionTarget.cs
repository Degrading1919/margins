using UnityEngine;

namespace Margins
{
    public sealed class CustomerCheckoutWorldInteractionTarget :
        MonoBehaviour,
        IFirstStoreWorldInteractionTarget
    {
        [SerializeField] private string stableTargetId;
        [SerializeField] private StoreCustomerFlowController customerFlow;
        [SerializeField] private StoreOperatingController operatingController;
        [SerializeField] private FixturePlacementController fixturePlacement;
        [SerializeField] private PlaceableFixtureComponent requiredFixture;

        public string StableTargetId => stableTargetId;
        public FirstStoreWorldInteractionPriority Priority =>
            FirstStoreWorldInteractionPriority.Checkout;
        public bool IsAvailable =>
            isActiveAndEnabled &&
            FirstStoreIdentifier.IsValid(stableTargetId) &&
            customerFlow != null && operatingController != null &&
            (fixturePlacement == null || requiredFixture == null ||
             fixturePlacement.IsPlaced(requiredFixture.StableFixtureInstanceId));

        public FirstStoreWorldInteractionPrompt Prompt
        {
            get
            {
                if (!IsAvailable)
                {
                    return new FirstStoreWorldInteractionPrompt(
                        "E",
                        "Use checkout",
                        "checkout unavailable");
                }

                if (operatingController.State != StoreOperatingState.Open &&
                    operatingController.State != StoreOperatingState.Closing)
                {
                    return new FirstStoreWorldInteractionPrompt(
                        "E",
                        "Use checkout",
                        "open the store before serving customers");
                }

                if (customerFlow.HasActiveCheckout)
                {
                    return customerFlow.ActiveCheckoutScannedCount ==
                           customerFlow.ActiveCheckoutItemCount
                        ? new FirstStoreWorldInteractionPrompt(
                            "E",
                            "Take payment",
                            $"total {FormatCents(customerFlow.ActiveCheckoutSubtotalCents)}")
                        : new FirstStoreWorldInteractionPrompt(
                            "E",
                            "Use customer items",
                            customerFlow.CheckoutBlocker);
                }

                return customerFlow.CanStartCheckout
                    ? new FirstStoreWorldInteractionPrompt(
                        "E",
                        "Start checkout",
                        customerFlow.CheckoutBlocker)
                    : new FirstStoreWorldInteractionPrompt(
                        "E",
                        "Use checkout",
                        customerFlow.CheckoutBlocker);
            }
        }

        public bool TryPrimary(out string error)
        {
            if (!IsAvailable)
            {
                error = "Customer checkout is unavailable.";
                return false;
            }
            return customerFlow.TryUseRegister(out error);
        }

        public bool TryCancel(out string error)
        {
            if (!IsAvailable)
            {
                error = "Customer checkout is unavailable.";
                return false;
            }
            return customerFlow.TryCorrectLastScan(out error);
        }

        private static string FormatCents(long cents)
        {
            bool negative = cents < 0;
            ulong absolute = negative
                ? (ulong)(-(cents + 1)) + 1UL
                : (ulong)cents;
            return negative
                ? $"-${absolute / 100}.{absolute % 100:00}"
                : $"${absolute / 100}.{absolute % 100:00}";
        }
    }
}
