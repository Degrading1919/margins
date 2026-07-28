// Draft implementation — Unity verification pending
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Margins
{
    [Serializable]
    public sealed class CheckoutPriceConfiguration
    {
        [SerializeField] private ProductDefinition productDefinition;
        [SerializeField, Min(1)] private int unitPriceCents = 100;

        public ProductDefinition ProductDefinition => productDefinition;
        public int UnitPriceCents => unitPriceCents;
    }

    public sealed class CheckoutStationComponent : MonoBehaviour
    {
        [SerializeField] private FirstStoreInventoryComponent inventoryComponent;
        [SerializeField] private string shelfLocationId;
        [SerializeField] private CheckoutPriceConfiguration[] prices;

        public FirstStoreInventoryComponent InventoryComponent => inventoryComponent;
        public CheckoutSession ActiveSession { get; private set; }
        public bool HasActiveIncompleteSession =>
            ActiveSession != null && !ActiveSession.IsCompleted;
        public CheckoutTransactionSummary CompletedSummary =>
            ActiveSession?.GetCompletedSummary();

        public bool HasSellableStock
        {
            get
            {
                if (inventoryComponent == null ||
                    !inventoryComponent.IsInitialized ||
                    prices == null)
                {
                    return false;
                }

                foreach (CheckoutPriceConfiguration price in prices)
                {
                    if (price?.ProductDefinition != null &&
                        inventoryComponent.Inventory.GetQuantity(
                            shelfLocationId,
                            price.ProductDefinition.StableProductId) > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool TryValidateConfiguration(out string error)
        {
            if (inventoryComponent == null || !inventoryComponent.IsInitialized)
            {
                error = "Checkout requires an initialized inventory component.";
                return false;
            }

            if (!FirstStoreIdentifier.IsValid(shelfLocationId) ||
                !inventoryComponent.Inventory.TryGetLocationKind(
                    shelfLocationId,
                    out InventoryLocationKind kind) ||
                kind != InventoryLocationKind.Shelf)
            {
                error = "Checkout requires a valid shelf inventory location.";
                return false;
            }

            if (prices == null || prices.Length == 0)
            {
                error = "Checkout requires at least one explicit product price.";
                return false;
            }

            HashSet<string> productIds = new(StringComparer.Ordinal);
            foreach (CheckoutPriceConfiguration price in prices)
            {
                if (price == null ||
                    price.ProductDefinition == null ||
                    price.UnitPriceCents <= 0 ||
                    !inventoryComponent.Inventory.IsKnownProduct(
                        price.ProductDefinition.StableProductId))
                {
                    error = "Checkout contains an invalid product or integer-cent price.";
                    return false;
                }

                if (!productIds.Add(price.ProductDefinition.StableProductId))
                {
                    error =
                        $"Checkout contains duplicate product '{price.ProductDefinition.StableProductId}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public bool TryInitializeDependencies(out string error)
        {
            if (inventoryComponent == null)
            {
                error = "Checkout requires an inventory component.";
                return false;
            }

            if (!inventoryComponent.TryInitialize(out error))
            {
                error = $"Checkout could not initialize inventory: {error}";
                return false;
            }

            return TryValidateConfiguration(out error);
        }

        public bool TryBeginSession(string transactionId, out string error)
        {
            if (!TryInitializeDependencies(out error))
            {
                return false;
            }

            if (HasActiveIncompleteSession)
            {
                error = "Checkout already has an active incomplete session.";
                return false;
            }

            if (!CheckoutSession.TryCreate(
                    inventoryComponent.Inventory,
                    shelfLocationId,
                    transactionId,
                    out CheckoutSession session,
                    out error))
            {
                return false;
            }

            ActiveSession = session;
            return true;
        }

        public bool TryScan(
            ProductDefinition productDefinition,
            int quantityUnits,
            out CheckoutFailure failure)
        {
            if (ActiveSession == null)
            {
                failure = CheckoutFailure.InvalidSession;
                return false;
            }

            CheckoutPriceConfiguration price = FindPrice(productDefinition);
            if (price == null)
            {
                failure = CheckoutFailure.InvalidProduct;
                return false;
            }

            return ActiveSession.TryScan(
                price.ProductDefinition.StableProductId,
                price.UnitPriceCents,
                quantityUnits,
                out failure);
        }

        public bool TryCorrect(
            ProductDefinition productDefinition,
            int quantityUnits,
            out CheckoutFailure failure)
        {
            if (ActiveSession == null || productDefinition == null)
            {
                failure = CheckoutFailure.InvalidSession;
                return false;
            }

            return ActiveSession.TryRemove(
                productDefinition.StableProductId,
                quantityUnits,
                out failure);
        }

        public bool TryComplete(
            out CheckoutTransactionSummary summary,
            out CheckoutFailure failure)
        {
            if (ActiveSession == null)
            {
                summary = null;
                failure = CheckoutFailure.InvalidSession;
                return false;
            }

            return ActiveSession.TryComplete(out summary, out failure);
        }

        public bool CanApplySummary(
            CheckoutTransactionSummary summary,
            out string error)
        {
            if (summary == null)
            {
                error = null;
                return true;
            }

            if (!TryValidateConfiguration(out error) ||
                !CheckoutSession.TryValidateSummary(summary, out error))
            {
                return false;
            }

            foreach (CheckoutLineSnapshot line in summary.lines)
            {
                if (!inventoryComponent.Inventory.IsKnownProduct(line.productId))
                {
                    error =
                        $"Checkout summary references unconfigured product '{line.productId}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public bool TryApplySummary(
            CheckoutTransactionSummary summary,
            out string error)
        {
            if (!CanApplySummary(summary, out error))
            {
                return false;
            }

            if (summary == null)
            {
                ActiveSession = null;
                return true;
            }

            if (!CheckoutSession.TryRestoreCompleted(
                    inventoryComponent.Inventory,
                    shelfLocationId,
                    summary,
                    out CheckoutSession session,
                    out error))
            {
                return false;
            }

            ActiveSession = session;
            return true;
        }

        private CheckoutPriceConfiguration FindPrice(
            ProductDefinition productDefinition)
        {
            if (productDefinition == null || prices == null)
            {
                return null;
            }

            foreach (CheckoutPriceConfiguration price in prices)
            {
                if (price?.ProductDefinition != null &&
                    string.Equals(
                        price.ProductDefinition.StableProductId,
                        productDefinition.StableProductId,
                        StringComparison.Ordinal))
                {
                    return price;
                }
            }

            return null;
        }
    }
}
