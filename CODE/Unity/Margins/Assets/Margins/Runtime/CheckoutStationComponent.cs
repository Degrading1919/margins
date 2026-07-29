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
        [SerializeField, Min(1)] private int maximumCompletedTransactions = 32;
        [SerializeField] private CheckoutPriceConfiguration[] prices;

        public FirstStoreInventoryComponent InventoryComponent => inventoryComponent;
        public PhysicalProductUnitRegistry PhysicalUnits => physicalUnits;
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
                    prices == null)
                {
                    return false;
                }

                foreach (CheckoutPriceConfiguration price in prices)
                {
                    if (price?.ProductDefinition != null &&
                        inventoryComponent.Inventory.GetQuantity(
                            price.ShelfLocationId,
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
                physicalUnits == null ||
                !physicalUnits.TryValidateConfiguration(out error))
            {
                error ??=
                    "Checkout requires initialized inventory and physical-unit references.";
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
            if (!physicalUnits.CanConsumeShelvedUnits(
                    shelfMappings,
                    lines,
                    out _))
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

            if (!physicalUnits.TryConsumeShelvedUnits(
                    shelfMappings,
                    lines,
                    out string physicalError))
            {
                throw new InvalidOperationException(
                    $"Validated physical checkout reconciliation failed: {physicalError}");
            }

            return true;
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
            CheckoutPriceConfiguration price = FindPrice(productId);
            shelfLocationId = price?.ShelfLocationId;
            return price != null;
        }

        private SortedDictionary<string, string> CreateShelfMappings()
        {
            SortedDictionary<string, string> mappings = new(StringComparer.Ordinal);
            foreach (CheckoutPriceConfiguration price in prices)
            {
                mappings.Add(
                    price.ProductDefinition.StableProductId,
                    price.ShelfLocationId);
            }
            return mappings;
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
