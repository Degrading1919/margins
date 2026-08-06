using UnityEngine;

namespace Margins
{
    /// <summary>
    /// Makes the visible fixture the stocking target. The controller selects the
    /// first compatible free authored snap point, so the player never has to hunt
    /// for a small invisible slot collider.
    /// </summary>
    public sealed class ShelfFixtureWorldInteractionTarget :
        MonoBehaviour,
        IFirstStoreWorldInteractionTarget
    {
        [SerializeField] private string stableTargetId;
        [SerializeField] private StockingController stocking;
        [SerializeField] private ShelfFixture shelfFixture;

        public string StableTargetId => stableTargetId;
        public FirstStoreWorldInteractionPriority Priority =>
            FirstStoreWorldInteractionPriority.HeldPlacement;
        public bool IsAvailable =>
            FirstStoreIdentifier.IsValid(stableTargetId) &&
            stocking != null &&
            shelfFixture != null &&
            stocking.HeldPhysicalUnit != null;

        public FirstStoreWorldInteractionPrompt Prompt
        {
            get
            {
                ProductDefinition product = null;
                string reason = "No compatible shelf position is available.";
                bool canStock = stocking != null &&
                                stocking.TryGetAvailableShelfPosition(
                                    shelfFixture,
                                    out product,
                                    out _,
                                    out reason);
                stocking?.HeldPhysicalUnit?.SetPlacementPreview(canStock);
                string productName = string.IsNullOrWhiteSpace(product?.DisplayName)
                    ? "product"
                    : product.DisplayName;
                return new FirstStoreWorldInteractionPrompt(
                    "E",
                    $"Stock {productName}",
                    canStock ? "next open shelf position" : reason);
            }
        }

        public bool TryPrimary(out string error)
        {
            if (!IsAvailable)
            {
                error = "Hold a product and aim at its shelf.";
                return false;
            }

            return stocking.TryStockHeldUnit(
                shelfFixture,
                stocking.HeldPhysicalUnit.QuarterTurns,
                out error);
        }

        public bool TryCancel(out string error)
        {
            error = "Aim away from the shelf to keep carrying the product.";
            return false;
        }
    }
}
