using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Margins
{
    /// <summary>
    /// Compact UI Toolkit editor opened from a physical shelf tag.
    /// </summary>
    public sealed class ShelfMerchandisingEditorController : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private StyleSheet styleSheet;
        [SerializeField] private FirstStoreMerchandisingComponent merchandising;
        [SerializeField] private FirstPersonController firstPersonController;

        private readonly List<ProductDefinition> productChoices = new();
        private VisualElement root;
        private Label shelfName;
        private DropdownField productField;
        private TextField priceField;
        private TextField customLabelField;
        private Label referencePrice;
        private Label errorLabel;
        private Button applyButton;
        private Button cancelButton;
        private string activeShelfFixtureId;
        private bool configured;
        private bool registeredModal;

        public bool IsOpen =>
            root != null && root.style.display == DisplayStyle.Flex;
        public string ActiveShelfFixtureId => activeShelfFixtureId;

        private void OnEnable()
        {
            TryConfigure(out _);
        }

        private void OnDisable()
        {
            Close();
            if (!configured)
            {
                return;
            }
            applyButton.clicked -= HandleApply;
            cancelButton.clicked -= HandleCancel;
            productField.UnregisterValueChangedCallback(HandleProductChanged);
            configured = false;
        }

        private void Update()
        {
            if (IsOpen && Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public bool TryOpen(string shelfFixtureId, out string error)
        {
            if (!TryConfigure(out error) || merchandising == null ||
                firstPersonController == null ||
                !merchandising.TryGetShelfAssignment(
                    shelfFixtureId,
                    out string assignedProductId,
                    out string customLabel))
            {
                error ??= "The shelf editor is unavailable for that shelf.";
                return false;
            }

            activeShelfFixtureId = shelfFixtureId;
            productChoices.Clear();
            productChoices.AddRange(merchandising.ProductCatalog);
            List<string> choices = new() { "Unassigned" };
            foreach (ProductDefinition product in productChoices)
            {
                choices.Add(string.IsNullOrWhiteSpace(product.DisplayName)
                    ? product.StableProductId
                    : product.DisplayName);
            }
            productField.choices = choices;
            int selectedIndex = 0;
            for (int index = 0; index < productChoices.Count; index++)
            {
                if (string.Equals(
                        productChoices[index].StableProductId,
                        assignedProductId,
                        StringComparison.Ordinal))
                {
                    selectedIndex = index + 1;
                    break;
                }
            }
            productField.index = selectedIndex;
            productField.SetValueWithoutNotify(choices[selectedIndex]);
            customLabelField.SetValueWithoutNotify(customLabel ?? string.Empty);
            shelfName.text = FriendlyShelfName(shelfFixtureId);
            RefreshPriceForSelectedProduct();
            SetError(null);
            root.style.display = DisplayStyle.Flex;
            if (!registeredModal)
            {
                registeredModal = true;
                GamePauseMenuController.RegisterExternalModal();
            }
            Time.timeScale = 0f;
            firstPersonController.SetGameplayMode(false);
            productField.Focus();
            error = null;
            return true;
        }

        public bool TryApplyCurrentDraft(out string error)
        {
            error = null;
            if (!IsOpen || string.IsNullOrWhiteSpace(activeShelfFixtureId))
            {
                error = "No shelf is open in the merchandise editor.";
                SetError(error);
                return false;
            }

            string productId = SelectedProductId();
            int priceCents = 0;
            if (productId != null &&
                !TryParsePriceCents(priceField.value, out priceCents, out error))
            {
                SetError(error);
                return false;
            }

            if (!merchandising.TryUpdateShelfOffer(
                    activeShelfFixtureId,
                    productId,
                    priceCents,
                    customLabelField.value,
                    out error))
            {
                SetError(error);
                return false;
            }

            Close();
            return true;
        }

        public void Close()
        {
            if (root != null)
            {
                root.style.display = DisplayStyle.None;
            }
            activeShelfFixtureId = null;
            if (registeredModal)
            {
                registeredModal = false;
                GamePauseMenuController.UnregisterExternalModal();
            }
            if (!GamePauseMenuController.IsAnyMenuOpen)
            {
                Time.timeScale = 1f;
                firstPersonController?.SetGameplayMode(true);
            }
        }

        public static bool TryParsePriceCents(
            string value,
            out int cents,
            out string error)
        {
            cents = 0;
            string normalized = (value ?? string.Empty)
                .Trim()
                .Replace("$", string.Empty)
                .Replace(",", string.Empty);
            if (!decimal.TryParse(
                    normalized,
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out decimal dollars) ||
                dollars <= 0m)
            {
                error = "Enter a positive price such as $1.49.";
                return false;
            }

            decimal exactCents = dollars * 100m;
            if (decimal.Truncate(exactCents) != exactCents ||
                exactCents > MerchandisingRules.MaximumSalePriceCents)
            {
                error = "Use no more than two decimal places and stay within the supported price range.";
                return false;
            }
            cents = decimal.ToInt32(exactCents);
            error = null;
            return true;
        }

        private bool TryConfigure(out string error)
        {
            if (configured)
            {
                error = null;
                return true;
            }
            if (document == null || merchandising == null ||
                firstPersonController == null || document.rootVisualElement == null)
            {
                error =
                    "Shelf editor requires its UI document, merchandising authority, and player reference.";
                return false;
            }

            root = document.rootVisualElement.Q<VisualElement>(
                "merchandising-editor-root");
            shelfName = document.rootVisualElement.Q<Label>("shelf-name");
            productField = document.rootVisualElement.Q<DropdownField>("product-field");
            priceField = document.rootVisualElement.Q<TextField>("price-field");
            customLabelField = document.rootVisualElement.Q<TextField>(
                "custom-label-field");
            referencePrice = document.rootVisualElement.Q<Label>("reference-price");
            errorLabel = document.rootVisualElement.Q<Label>("editor-error");
            applyButton = document.rootVisualElement.Q<Button>("apply-button");
            cancelButton = document.rootVisualElement.Q<Button>("cancel-button");
            if (root == null || shelfName == null || productField == null ||
                priceField == null || customLabelField == null ||
                referencePrice == null || errorLabel == null ||
                applyButton == null || cancelButton == null)
            {
                error = "Shelf editor UI assets are missing required named controls.";
                return false;
            }

            if (styleSheet != null &&
                !document.rootVisualElement.styleSheets.Contains(styleSheet))
            {
                document.rootVisualElement.styleSheets.Add(styleSheet);
            }
            root.style.display = DisplayStyle.None;
            applyButton.clicked += HandleApply;
            cancelButton.clicked += HandleCancel;
            productField.RegisterValueChangedCallback(HandleProductChanged);
            configured = true;
            error = null;
            return true;
        }

        private void HandleApply()
        {
            TryApplyCurrentDraft(out _);
        }

        private void HandleCancel()
        {
            Close();
        }

        private void HandleProductChanged(ChangeEvent<string> _)
        {
            RefreshPriceForSelectedProduct();
        }

        private void RefreshPriceForSelectedProduct()
        {
            string productId = SelectedProductId();
            if (productId == null)
            {
                priceField.SetEnabled(false);
                priceField.SetValueWithoutNotify(string.Empty);
                referencePrice.text = "No product assigned";
                return;
            }

            priceField.SetEnabled(true);
            if (merchandising.TryGetProductPrice(
                    productId,
                    out int salePrice,
                    out int reference))
            {
                priceField.SetValueWithoutNotify(FormatCents(salePrice));
                referencePrice.text =
                    $"Reference price {FormatCents(reference)} • procurement cost is unchanged";
            }
        }

        private string SelectedProductId()
        {
            int index = productField?.index ?? 0;
            return index <= 0 || index > productChoices.Count
                ? null
                : productChoices[index - 1].StableProductId;
        }

        private void SetError(string error)
        {
            if (errorLabel == null)
            {
                return;
            }
            errorLabel.text = error ?? string.Empty;
            errorLabel.style.display = string.IsNullOrWhiteSpace(error)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        private static string FriendlyShelfName(string shelfFixtureId)
        {
            return shelfFixtureId.Replace("fixture-", string.Empty)
                .Replace('-', ' ')
                .ToUpperInvariant();
        }

        private static string FormatCents(int cents)
        {
            return $"${cents / 100}.{cents % 100:00}";
        }
    }
}
