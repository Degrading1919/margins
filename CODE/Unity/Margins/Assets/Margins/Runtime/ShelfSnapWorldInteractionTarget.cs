using UnityEngine;

namespace Margins
{
    public sealed class ShelfSnapWorldInteractionTarget : MonoBehaviour, IFirstStoreWorldInteractionTarget
    {
        [SerializeField] private string stableTargetId;
        [SerializeField] private StockingController stocking;
        [SerializeField] private ShelfFixture shelfFixture;
        [SerializeField] private string snapPointId;

        public string StableTargetId => stableTargetId;
        public ShelfFixture ShelfFixture => shelfFixture;
        public string SnapPointId => snapPointId;
        public FirstStoreWorldInteractionPriority Priority => FirstStoreWorldInteractionPriority.HeldPlacement;
        public bool IsAvailable => HasValidReference() && stocking.HeldPhysicalUnit != null;
        public FirstStoreWorldInteractionPrompt Prompt
        {
            get
            {
                ProductItem held = stocking != null ? stocking.HeldPhysicalUnit : null;
                if (held == null || held.Definition == null)
                {
                    return new FirstStoreWorldInteractionPrompt(
                        "E",
                        "Stock product",
                        "hold a product first");
                }

                string productName = string.IsNullOrWhiteSpace(held.Definition.DisplayName)
                    ? "product"
                    : held.Definition.DisplayName;
                bool isValid = stocking.CanStockHeldUnit(
                    shelfFixture,
                    snapPointId,
                    out string reason);
                held.SetPlacementPreview(isValid);
                string state = isValid
                    ? "targeted shelf position; mouse wheel rotates"
                    : reason;
                return new FirstStoreWorldInteractionPrompt(
                    "E",
                    $"Stock {productName}",
                    state);
            }
        }

        public bool TryPrimary(out string error)
        {
            if (!HasValidReference())
            {
                error = "This shelf position is unavailable.";
                return false;
            }

            ProductItem held = stocking.HeldPhysicalUnit;
            if (held == null)
            {
                error = "Hold a product before stocking it.";
                return false;
            }

            if (!stocking.TryStockHeldUnit(
                    shelfFixture,
                    snapPointId,
                    held.QuarterTurns,
                    out error))
            {
                return false;
            }

            error = null;
            return true;
        }

        public bool TryCancel(out string error)
        {
            error = "Keep holding the product or target a valid shelf position.";
            return false;
        }

        private bool HasValidReference()
        {
            return FirstStoreIdentifier.IsValid(stableTargetId) &&
                   stocking != null &&
                   shelfFixture != null &&
                   FirstStoreIdentifier.IsValid(snapPointId) &&
                   shelfFixture.TryGetSnapPoint(snapPointId, out _);
        }
    }
}
