using System;
using System.Collections.Generic;
using UnityEngine;

namespace Margins
{
    [Serializable]
    public sealed class CheckoutPriceConfiguration
    {
        [SerializeField] private ProductDefinition productDefinition;
        [SerializeField] private string shelfLocationId;
        [SerializeField, Min(1)] private int unitPriceCents = 100;
        [SerializeField, Min(0)] private int unitCostCents;

        public ProductDefinition ProductDefinition => productDefinition;
        public string ShelfLocationId => shelfLocationId;
        public int UnitPriceCents => unitPriceCents;
        public int UnitCostCents => unitCostCents;
    }

    public sealed class CheckoutStationComponent : MonoBehaviour
    {
        [SerializeField] private FirstStoreInventoryComponent inventoryComponent;
        [SerializeField] private PhysicalProductUnitRegistry physicalUnits;
        [SerializeField] private FirstStoreMerchandisingComponent merchandising;
        [SerializeField, Min(1)] private int maximumCompletedTransactions = 32;
        [SerializeField] private CheckoutPriceConfiguration[] prices;

        public FirstStoreInventoryComponent InventoryComponent => inventoryComponent;
        public PhysicalProductUnitRegistry PhysicalUnits => physicalUnits;
        public FirstStoreMerchandisingComponent Merchandising => merchandising;
        private CheckoutSession ActiveSession { get; set; }
        internal CompletedTransactionLedger TransactionLedger { get; private set; }
        public bool HasActiveIncompleteSession =>
            ActiveSession != null && !ActiveSession.IsCompleted;
        public IReadOnlyList<CheckoutLineSnapshot> ActiveLines =>
            ActiveSession?.Lines ?? Array.Empty<CheckoutLineSnapshot>();
        public long ActiveSubtotalCents => ActiveSession?.SubtotalCents ?? 0;
        public CheckoutTransactionSummary CompletedSummary =>
            ActiveSession?.GetCompletedSummary();
        public long GrossSalesCents => TransactionLedger?.GrossSalesCents ?? 0;
        public int UnitsSold => TransactionLedger?.UnitsSold ?? 0;
        public int CompletedTransactionCount =>
            TransactionLedger?.TransactionCount ?? 0;
        public IReadOnlyList<CheckoutTransactionSummary> CompletedTransactions =>
            TransactionLedger?.Transactions ?? Array.Empty<CheckoutTransactionSummary>();

        public IReadOnlyList<string> ConfiguredProductIds
        {
            get
            {
                List<string> productIds = new();
                if (prices != null)
                {
                    foreach (CheckoutPriceConfiguration price in prices)
                    {
                        if (price?.ProductDefinition != null)
                        {
                            productIds.Add(price.ProductDefinition.StableProductId);
                        }
                    }
                }
                productIds.Sort(StringComparer.Ordinal);
                return productIds;
            }
        }

        public IReadOnlyDictionary<string, int> ProductUnitCostsCents
        {
            get
            {
                SortedDictionary<string, int> costs = new(StringComparer.Ordinal);
                if (prices != null)
                {
                    foreach (CheckoutPriceConfiguration price in prices)
                    {
                        if (price?.ProductDefinition != null)
                        {
                            costs[price.ProductDefinition.StableProductId] =
                                price.UnitCostCents;
                        }
                    }
                }
                return costs;
            }
        }

        public bool HasSellableStock
        {
            get
            {
                if (inventoryComponent == null ||
                    !inventoryComponent.IsInitialized ||
                    merchandising == null || prices == null)
                {
                    return false;
                }

                foreach (CheckoutPriceConfiguration price in prices)
                {
                    if (price?.ProductDefinition != null &&
                        merchandising.TryGetOfferForProduct(
                            price.ProductDefinition.StableProductId,
                            out MerchandiseOffer offer) &&
                        inventoryComponent.Inventory.GetQuantity(
                            offer.InventoryLocationId,
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
            error = null;
            if (inventoryComponent == null || !inventoryComponent.IsInitialized ||
                physicalUnits == null || merchandising == null ||
                !physicalUnits.TryValidateConfiguration(out error))
            {
                error ??=
                    "Checkout requires initialized inventory, physical-unit, and merchandising references.";
                return false;
            }

            if (maximumCompletedTransactions <= 0)
            {
                error = "Checkout transaction-ledger capacity must be positive.";
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
                    price.UnitCostCents < 0 ||
                    !inventoryComponent.Inventory.IsKnownProduct(
                        price.ProductDefinition.StableProductId))
                {
                    error = "Checkout contains an invalid product, price, or product cost.";
                    return false;
                }

                string productId = price.ProductDefinition.StableProductId;
                if (!productIds.Add(productId))
                {
                    error = $"Checkout contains duplicate product mapping '{productId}'.";
                    return false;
                }

                if (!FirstStoreIdentifier.IsValid(price.ShelfLocationId) ||
                    !inventoryComponent.Inventory.TryGetLocationKind(
                        price.ShelfLocationId,
                        out InventoryLocationKind kind) ||
                    kind != InventoryLocationKind.Shelf)
                {
                    error =
                        $"Checkout product '{productId}' requires exactly one valid shelf mapping.";
                    return false;
                }
            }

            if (!merchandising.TryValidateCheckoutConfiguration(this, out error))
            {
                return false;
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

            if (!TryValidateConfiguration(out error))
            {
                return false;
            }

            TransactionLedger ??= new CompletedTransactionLedger(
                maximumCompletedTransactions);
            return true;
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

            if (HasCompletedTransaction(transactionId))
            {
                error =
                    $"Checkout transaction '{transactionId}' is already completed.";
                return false;
            }

            if (!CheckoutSession.TryCreate(
                    inventoryComponent.Inventory,
                    CreateShelfMappings(),
                    transactionId,
                    out CheckoutSession session,
                    out error))
            {
                return false;
            }

            ActiveSession = session;
            return true;
        }

        private bool HasCompletedTransaction(string transactionId)
        {
            if (!FirstStoreIdentifier.IsValid(transactionId) ||
                TransactionLedger == null)
            {
                return false;
            }

            foreach (CheckoutTransactionSummary transaction in
                     TransactionLedger.Transactions)
            {
                if (string.Equals(
                        transaction.transactionId,
                        transactionId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
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
            if (price == null ||
                !merchandising.TryGetOfferForProduct(
                    price.ProductDefinition.StableProductId,
                    out MerchandiseOffer offer))
            {
                failure = CheckoutFailure.InvalidProduct;
                return false;
            }

            return ActiveSession.TryScan(
                price.ProductDefinition.StableProductId,
                offer.SalePriceCents,
                price.UnitCostCents,
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

        public bool TryCancelActiveSession(out string error)
        {
            if (ActiveSession == null)
            {
                error = "Checkout has no active session.";
                return false;
            }

            if (ActiveSession.IsCompleted)
            {
                error = "A completed checkout session cannot be cancelled.";
                return false;
            }

            ActiveSession = null;
            error = null;
            return true;
        }

        public bool TryComplete(
            out CheckoutTransactionSummary summary,
            out CheckoutFailure failure)
        {
            return TryCompleteInternal(
                null,
                useSpecificPhysicalUnits: false,
                out summary,
                out failure);
        }

        public bool TryComplete(
            IReadOnlyList<string> physicalUnitIds,
            out CheckoutTransactionSummary summary,
            out CheckoutFailure failure)
        {
            return TryCompleteInternal(
                physicalUnitIds,
                useSpecificPhysicalUnits: true,
                out summary,
                out failure);
        }

        private bool TryCompleteInternal(
            IReadOnlyList<string> physicalUnitIds,
            bool useSpecificPhysicalUnits,
            out CheckoutTransactionSummary summary,
            out CheckoutFailure failure)
        {
            if (ActiveSession == null || TransactionLedger == null)
            {
                summary = null;
                failure = CheckoutFailure.InvalidSession;
                return false;
            }

            if (ActiveSession.IsCompleted)
            {
                return ActiveSession.TryComplete(
                    TransactionLedger,
                    out summary,
                    out failure);
            }

            IReadOnlyList<CheckoutLineSnapshot> lines = ActiveSession.Lines;
            SortedDictionary<string, string> shelfMappings =
                CreateShelfMappings();
            bool canConsume = useSpecificPhysicalUnits
                ? physicalUnits.CanConsumeSpecificShelvedUnits(
                    shelfMappings,
                    lines,
                    physicalUnitIds,
                    out _)
                : physicalUnits.CanConsumeShelvedUnits(
                    shelfMappings,
                    lines,
                    out _);
            if (!canConsume)
            {
                summary = null;
                failure = CheckoutFailure.InventoryChanged;
                return false;
            }

            if (!ActiveSession.TryComplete(
                    TransactionLedger,
                    out summary,
                    out failure))
            {
                return false;
            }

            string physicalError;
            bool physicallyConsumed = useSpecificPhysicalUnits
                ? physicalUnits.TryConsumeSpecificShelvedUnits(
                    shelfMappings,
                    lines,
                    physicalUnitIds,
                    out physicalError)
                : physicalUnits.TryConsumeShelvedUnits(
                    shelfMappings,
                    lines,
                    out physicalError);
            if (!physicallyConsumed)
            {
                throw new InvalidOperationException(
                    $"Validated physical checkout reconciliation failed: {physicalError}");
            }

            return true;
        }

        public bool TryGetProductDefinition(
            string productId,
            out ProductDefinition productDefinition)
        {
            CheckoutPriceConfiguration price = FindPrice(productId);
            productDefinition = price?.ProductDefinition;
            return productDefinition != null;
        }

        internal bool CanApplyLedger(
            CompletedTransactionLedger restored,
            out string error)
        {
            error = null;
            if (restored == null ||
                restored.MaximumTransactionCount != maximumCompletedTransactions ||
                !TryValidateConfiguration(out error))
            {
                error ??= "Restored checkout ledger is missing or has the wrong capacity.";
                return false;
            }

            foreach (CheckoutTransactionSummary transaction in restored.Transactions)
            {
                foreach (CheckoutLineSnapshot line in transaction.lines)
                {
                    if (FindPrice(line.productId) == null)
                    {
                        error =
                            $"Checkout ledger references unconfigured product '{line.productId}'.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        internal bool TryApplyLedger(
            CompletedTransactionLedger restored,
            out string error)
        {
            if (!CanApplyLedger(restored, out error))
            {
                return false;
            }

            TransactionLedger = restored;
            ActiveSession = null;
            return true;
        }

        public bool TryGetShelfLocation(
            string productId,
            out string shelfLocationId)
        {
            MerchandiseOffer offer = default;
            bool resolved = merchandising != null &&
                            merchandising.TryGetOfferForProduct(
                                productId,
                                out offer);
            shelfLocationId = resolved ? offer.InventoryLocationId : null;
            return resolved;
        }

        private SortedDictionary<string, string> CreateShelfMappings()
        {
            SortedDictionary<string, string> mappings = new(StringComparer.Ordinal);
            foreach (CheckoutPriceConfiguration price in prices)
            {
                string productId = price.ProductDefinition.StableProductId;
                if (merchandising.TryGetOfferForProduct(
                        productId,
                        out MerchandiseOffer offer))
                {
                    mappings.Add(productId, offer.InventoryLocationId);
                }
            }
            return mappings;
        }

        public IReadOnlyList<CheckoutPriceConfiguration> AuthoredPriceMappings =>
            prices ?? Array.Empty<CheckoutPriceConfiguration>();

        public bool TryGetAuthoredProductEconomy(
            string productId,
            out ProductDefinition productDefinition,
            out int referencePriceCents,
            out int unitCostCents,
            out string defaultShelfLocationId)
        {
            CheckoutPriceConfiguration price = FindPrice(productId);
            productDefinition = price?.ProductDefinition;
            referencePriceCents = price?.UnitPriceCents ?? 0;
            unitCostCents = price?.UnitCostCents ?? 0;
            defaultShelfLocationId = price?.ShelfLocationId;
            return price != null;
        }

        public bool TryGetCurrentOffer(
            string productId,
            out MerchandiseOffer offer)
        {
            offer = default;
            return FindPrice(productId) != null &&
                   merchandising != null &&
                   merchandising.TryGetOfferForProduct(productId, out offer);
        }

        private CheckoutPriceConfiguration FindPrice(
            ProductDefinition productDefinition)
        {
            return productDefinition == null
                ? null
                : FindPrice(productDefinition.StableProductId);
        }

        private CheckoutPriceConfiguration FindPrice(string productId)
        {
            if (!FirstStoreIdentifier.IsValid(productId) || prices == null)
            {
                return null;
            }

            foreach (CheckoutPriceConfiguration price in prices)
            {
                if (price?.ProductDefinition != null &&
                    string.Equals(
                        price.ProductDefinition.StableProductId,
                        productId,
                        StringComparison.Ordinal))
                {
                    return price;
                }
            }

            return null;
        }
    }
}
