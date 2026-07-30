using UnityEngine;

namespace Margins
{
    /// <summary>
    /// The customer's visible product owns the scan action. The register only
    /// starts service and takes payment after every physical item was processed.
    /// </summary>
    public sealed class CheckoutProductWorldInteractionTarget :
        MonoBehaviour,
        IFirstStoreWorldInteractionTarget
    {
        [SerializeField] private string stableTargetId;
        [SerializeField] private ProductDefinition productDefinition;
        [SerializeField] private StagedCheckoutInteractionComponent stagedCheckout;
        [SerializeField] private StoreOperatingController operatingController;

        public string StableTargetId => stableTargetId;
        public FirstStoreWorldInteractionPriority Priority =>
            FirstStoreWorldInteractionPriority.Checkout;
        public bool IsAvailable =>
            FirstStoreIdentifier.IsValid(stableTargetId) &&
            productDefinition != null &&
            stagedCheckout != null &&
            stagedCheckout.NextAction == StagedCheckoutPrimaryAction.Scan &&
            stagedCheckout.ActiveProduct == productDefinition &&
            operatingController != null &&
            (operatingController.State == StoreOperatingState.Open ||
             operatingController.State == StoreOperatingState.Closing);

        public FirstStoreWorldInteractionPrompt Prompt =>
            new(
                "E",
                $"Scan {ProductName}",
                $"item {stagedCheckout.ActiveLineScannedQuantity + 1}/" +
                $"{stagedCheckout.ActiveLineQuantity}");

        public bool TryPrimary(out string error)
        {
            if (!IsAvailable)
            {
                error = "This customer item is not ready to scan.";
                return false;
            }

            return stagedCheckout.TryScanVisibleProduct(
                productDefinition,
                out _,
                out error);
        }

        public bool TryCancel(out string error)
        {
            if (stagedCheckout == null)
            {
                error = "No checkout is available.";
                return false;
            }

            return stagedCheckout.TryCorrect(out _, out error);
        }

        private string ProductName =>
            string.IsNullOrWhiteSpace(productDefinition?.DisplayName)
                ? "item"
                : productDefinition.DisplayName;
    }
}
