using UnityEngine;

namespace Margins
{
    public sealed class StagedCheckoutWorldInteractionTarget :
        MonoBehaviour,
        IFirstStoreWorldInteractionTarget
    {
        [SerializeField] private string stableTargetId;
        [SerializeField] private StagedCheckoutInteractionComponent stagedCheckout;
        [SerializeField] private StoreOperatingController operatingController;

        private bool replayAcknowledged;

        public string StableTargetId => stableTargetId;
        public FirstStoreWorldInteractionPriority Priority =>
            FirstStoreWorldInteractionPriority.Checkout;
        public bool IsAvailable =>
            FirstStoreIdentifier.IsValid(stableTargetId) &&
            stagedCheckout != null &&
            operatingController != null;

        public FirstStoreWorldInteractionPrompt Prompt
        {
            get
            {
                if (!IsAvailable || !stagedCheckout.TryValidateConfiguration(out _))
                {
                    return new FirstStoreWorldInteractionPrompt(
                        "E",
                        "Use checkout",
                        "checkout unavailable");
                }

                if (!CanUseCheckout(out string operatingBlocker))
                {
                    return new FirstStoreWorldInteractionPrompt(
                        "E",
                        "Use checkout",
                        operatingBlocker);
                }

                string blocker = stagedCheckout.FirstBlocker;
                string basket = stagedCheckout.BasketCount > 0
                    ? $"basket {stagedCheckout.CurrentBasketNumber}/{stagedCheckout.BasketCount}"
                    : "staged basket";
                string subtotal = FormatCents(stagedCheckout.SubtotalCents);

                if (stagedCheckout.AllBasketsComplete)
                {
                    return new FirstStoreWorldInteractionPrompt(
                        "E",
                        "Checkout complete",
                        "all staged baskets completed");
                }

                switch (stagedCheckout.NextAction)
                {
                    case StagedCheckoutPrimaryAction.Begin:
                        return new FirstStoreWorldInteractionPrompt(
                            "E",
                            "Begin staged checkout",
                            blocker ?? basket);

                    case StagedCheckoutPrimaryAction.Scan:
                        string productName =
                            string.IsNullOrWhiteSpace(stagedCheckout.ActiveProductDisplayName)
                                ? "product"
                                : stagedCheckout.ActiveProductDisplayName;
                        string line =
                            $"{stagedCheckout.ActiveLineScannedQuantity}/{stagedCheckout.ActiveLineQuantity}; " +
                            $"subtotal {subtotal}; Q corrects recent scan";
                        return new FirstStoreWorldInteractionPrompt(
                            "E",
                            $"Scan {productName}",
                            blocker ?? line);

                    case StagedCheckoutPrimaryAction.Complete:
                        return new FirstStoreWorldInteractionPrompt(
                            "E",
                            "Complete transaction",
                            blocker ?? $"subtotal {subtotal}");

                    case StagedCheckoutPrimaryAction.Replay when !replayAcknowledged:
                        return new FirstStoreWorldInteractionPrompt(
                            "E",
                            "Verify completed transaction",
                            $"already completed; subtotal {subtotal}");

                    case StagedCheckoutPrimaryAction.Replay:
                        return new FirstStoreWorldInteractionPrompt(
                            "E",
                            "Continue staged checkout",
                            $"{basket} completed");

                    default:
                        return new FirstStoreWorldInteractionPrompt(
                            "E",
                            "Use checkout",
                            blocker ?? "no staged action available");
                }
            }
        }

        public bool TryPrimary(out string error)
        {
            if (!IsAvailable)
            {
                error = "Checkout is unavailable.";
                return false;
            }

            if (!CanUseCheckout(out error))
            {
                return false;
            }

            StagedCheckoutPrimaryAction actionBefore = stagedCheckout.NextAction;
            if (actionBefore == StagedCheckoutPrimaryAction.Replay &&
                replayAcknowledged)
            {
                bool continued = stagedCheckout.TryContinue(out error);
                if (continued)
                {
                    replayAcknowledged = false;
                }
                return continued;
            }

            bool success = stagedCheckout.TryPrimary(
                out _,
                out _,
                out error);
            if (success && actionBefore == StagedCheckoutPrimaryAction.Replay)
            {
                replayAcknowledged = true;
            }
            return success;
        }

        public bool TryCancel(out string error)
        {
            if (!IsAvailable || stagedCheckout.AllBasketsComplete)
            {
                error = "No checkout scan can be corrected.";
                return false;
            }

            return stagedCheckout.TryCorrect(out _, out error);
        }

        private bool CanUseCheckout(out string blocker)
        {
            if (operatingController == null || !operatingController.IsInitialized)
            {
                blocker = "store controls are not ready";
                return false;
            }

            if (operatingController.State == StoreOperatingState.Open)
            {
                blocker = null;
                return true;
            }

            if (operatingController.State == StoreOperatingState.Closing &&
                stagedCheckout.Checkout != null &&
                stagedCheckout.Checkout.HasActiveIncompleteSession)
            {
                blocker = null;
                return true;
            }

            blocker = operatingController.State == StoreOperatingState.Closing
                ? "checkout is closed to new staged baskets"
                : "open the store before using checkout";
            return false;
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
