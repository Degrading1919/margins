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
        public StoreCustomerFlowController CustomerFlow => customerFlow;
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

        public bool IsDedicatedCheckoutActive =>
            IsAvailable && customerFlow.HasActiveCheckout &&
            customerFlow.Checkout.HasActiveIncompleteSession;

        public FirstStoreWorldInteractionPrompt DedicatedPrompt
        {
            get
            {
                if (!IsDedicatedCheckoutActive)
                {
                    return new FirstStoreWorldInteractionPrompt(
                        "E",
                        "Checkout ended",
                        "returning to the store");
                }

                if (customerFlow.ActiveCheckoutScannedCount <
                    customerFlow.ActiveCheckoutItemCount)
                {
                    return new FirstStoreWorldInteractionPrompt(
                        "E",
                        $"Scan {customerFlow.ActiveCheckoutNextProductName ?? "item"}",
                        $"{customerFlow.ActiveCheckoutScannedCount}/" +
                        $"{customerFlow.ActiveCheckoutItemCount} scanned  •  " +
                        $"{FormatCents(customerFlow.ActiveCheckoutSubtotalCents)}  •  " +
                        (customerFlow.ActiveCheckoutScannedCount > 0
                            ? "Q undo last scan"
                            : "Q cancel checkout"));
                }

                return new FirstStoreWorldInteractionPrompt(
                    "E",
                    $"Take payment {FormatCents(customerFlow.ActiveCheckoutSubtotalCents)}",
                    "Q undoes the last scan");
            }
        }

        public bool TryEnterDedicatedMode(out string error)
        {
            if (!IsAvailable)
            {
                error = "Customer checkout is unavailable.";
                return false;
            }
            if (!customerFlow.TryClearStaleCheckout(out error))
            {
                return false;
            }
            if (customerFlow.HasActiveCheckout)
            {
                error = null;
                return true;
            }
            return customerFlow.TryStartCheckout(out error);
        }

        public bool TryDedicatedPrimary(out string error)
        {
            if (!IsDedicatedCheckoutActive)
            {
                error = "The customer checkout has ended.";
                return false;
            }
            return customerFlow.ActiveCheckoutScannedCount <
                   customerFlow.ActiveCheckoutItemCount
                ? customerFlow.TryScanNextCustomerItem(out error)
                : customerFlow.TryCompleteCheckout(out error);
        }

        public bool TryDedicatedCancel(out string error)
        {
            if (!IsDedicatedCheckoutActive)
            {
                error = "The customer checkout has ended.";
                return false;
            }
            return customerFlow.ActiveCheckoutScannedCount > 0
                ? customerFlow.TryCorrectLastScan(out error)
                : customerFlow.TryCancelActiveCheckout(out error);
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
