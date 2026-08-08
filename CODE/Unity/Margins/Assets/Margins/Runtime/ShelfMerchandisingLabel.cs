using System;
using UnityEngine;

namespace Margins
{
    /// <summary>
    /// World-space retail shelf tag and interaction target. Presentation is a
    /// projection of the persistent merchandising authority.
    /// </summary>
    public sealed class ShelfMerchandisingLabel :
        MonoBehaviour,
        IFirstStoreWorldInteractionTarget
    {
        [SerializeField] private string stableTargetId;
        [SerializeField] private ShelfFixture shelfFixture;
        [SerializeField] private FirstStoreMerchandisingComponent merchandising;
        [SerializeField] private ShelfMerchandisingEditorController editor;
        [SerializeField] private TextMesh labelText;

        private string lastPresentation;
        private float nextRefreshAt;

        public string StableTargetId => stableTargetId;
        public FirstStoreWorldInteractionPriority Priority =>
            FirstStoreWorldInteractionPriority.Merchandising;
        public bool IsAvailable =>
            FirstStoreIdentifier.IsValid(stableTargetId) &&
            shelfFixture != null &&
            merchandising != null &&
            editor != null &&
            labelText != null;
        public string DisplayedText
        {
            get
            {
                RefreshNow();
                return labelText?.text ?? string.Empty;
            }
        }

        public FirstStoreWorldInteractionPrompt Prompt
        {
            get
            {
                string product = "Unassigned shelf";
                string state = "choose product and price";
                if (merchandising != null && shelfFixture != null &&
                    merchandising.TryGetOfferForShelf(
                        shelfFixture.StableFixtureId,
                        out MerchandiseOffer offer))
                {
                    product = ProductName(offer.ProductId);
                    state = FormatCents(offer.SalePriceCents);
                }
                return new FirstStoreWorldInteractionPrompt(
                    "E",
                    $"Edit {product}",
                    state);
            }
        }

        private void OnEnable()
        {
            if (merchandising != null)
            {
                merchandising.Changed += RefreshNow;
            }
            RefreshNow();
        }

        private void OnDisable()
        {
            if (merchandising != null)
            {
                merchandising.Changed -= RefreshNow;
            }
        }

        private void Start()
        {
            // OnEnable can run before the portfolio controller finishes its
            // own initialization. Start projects the settled authority without
            // leaving the tag stale for the polling interval.
            RefreshNow();
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextRefreshAt)
            {
                RefreshNow();
            }
        }

        public bool TryPrimary(out string error)
        {
            if (!IsAvailable)
            {
                error = "This shelf label is not configured.";
                return false;
            }
            return editor.TryOpen(shelfFixture.StableFixtureId, out error);
        }

        public bool TryCancel(out string error)
        {
            error = "Use the shelf editor's Cancel button or Escape.";
            return false;
        }

        public void RefreshNow()
        {
            nextRefreshAt = Time.unscaledTime + 0.25f;
            if (labelText == null || merchandising == null || shelfFixture == null)
            {
                return;
            }

            string presentation;
            if (!merchandising.TryGetShelfAssignment(
                    shelfFixture.StableFixtureId,
                    out string assignedProductId,
                    out string customLabel) ||
                string.IsNullOrWhiteSpace(assignedProductId) ||
                !merchandising.TryGetProductPrice(
                    assignedProductId,
                    out int salePriceCents,
                    out _))
            {
                presentation = "UNASSIGNED\nSET PRODUCT + PRICE";
            }
            else
            {
                string productName = ProductName(assignedProductId).ToUpperInvariant();
                presentation = string.IsNullOrWhiteSpace(customLabel)
                    ? $"{productName}\n{FormatCents(salePriceCents)}"
                    : $"{customLabel.ToUpperInvariant()}\n{productName}  {FormatCents(salePriceCents)}";
            }

            if (!string.Equals(
                    presentation,
                    lastPresentation,
                    StringComparison.Ordinal))
            {
                lastPresentation = presentation;
                labelText.text = presentation;
            }
        }

        private string ProductName(string productId)
        {
            foreach (ProductDefinition product in merchandising.ProductCatalog)
            {
                if (string.Equals(
                        product.StableProductId,
                        productId,
                        StringComparison.Ordinal))
                {
                    return string.IsNullOrWhiteSpace(product.DisplayName)
                        ? productId
                        : product.DisplayName;
                }
            }
            return productId;
        }

        private static string FormatCents(long cents)
        {
            return $"${cents / 100}.{Math.Abs(cents % 100):00}";
        }
    }
}
