using System;
using System.Collections.Generic;
using UnityEngine;

namespace Margins
{
    public enum StagedCheckoutPrimaryAction
    {
        None,
        Begin,
        Scan,
        Complete,
        Replay
    }

    [Serializable]
    public sealed class StagedCheckoutProductConfiguration
    {
        [SerializeField] private ProductDefinition productDefinition;
        [SerializeField, Min(1)] private int quantityUnits = 1;

        public ProductDefinition ProductDefinition => productDefinition;
        public int QuantityUnits => quantityUnits;
    }

    [Serializable]
    public sealed class StagedCheckoutBasketConfiguration
    {
        [SerializeField] private string stableTransactionId;
        [SerializeField] private StagedCheckoutProductConfiguration[] products;

        public string StableTransactionId => stableTransactionId;
        public IReadOnlyList<StagedCheckoutProductConfiguration> Products => products;
    }

    /// <summary>
    /// Transient presentation sequencing for a deliberately configured checkout proof.
    /// CheckoutStationComponent remains the authority for inventory and its completed ledger.
    /// </summary>
    public sealed class StagedCheckoutInteractionComponent : MonoBehaviour
    {
        [SerializeField] private CheckoutStationComponent checkout;
        [SerializeField] private StagedCheckoutBasketConfiguration[] baskets;

        private readonly List<int> scanHistory = new();
        private int[] scannedQuantities;
        private int currentBasketIndex;
        private bool sessionStarted;
        private string firstBlocker;

        public CheckoutStationComponent Checkout => checkout;
        public ProductDefinition ActiveProduct =>
            TryGetActiveLine(out StagedCheckoutProductConfiguration line, out _)
                ? line.ProductDefinition
                : null;
        public string ActiveProductDisplayName => ActiveProduct?.DisplayName;
        public int ActiveLineQuantity =>
            TryGetActiveLine(out StagedCheckoutProductConfiguration line, out _)
                ? line.QuantityUnits
                : 0;
        public int ActiveLineScannedQuantity =>
            TryGetActiveLine(out _, out int lineIndex) && scannedQuantities != null
                ? scannedQuantities[lineIndex]
                : 0;
        public long SubtotalCents
        {
            get
            {
                if (TryGetCompletedSummary(CurrentBasket, out CheckoutTransactionSummary summary))
                {
                    return summary.subtotalCents;
                }

                return checkout?.ActiveSubtotalCents ?? 0;
            }
        }
        public string FirstBlocker => firstBlocker;
        public int CurrentBasketNumber => baskets == null || baskets.Length == 0
            ? 0
            : Math.Min(currentBasketIndex + 1, baskets.Length);
        public int BasketCount => baskets?.Length ?? 0;
        public int ActiveLineNumber => FindNextIncompleteLineIndex() + 1;
        public int ActiveLineCount => CurrentBasket?.Products?.Count ?? 0;
        public bool AllBasketsComplete =>
            baskets != null && baskets.Length > 0 && currentBasketIndex >= baskets.Length;
        public bool IsAwaitingContinue =>
            !AllBasketsComplete && TryGetCompletedSummary(CurrentBasket, out _);
        public StagedCheckoutPrimaryAction NextAction => GetNextAction();

        private StagedCheckoutBasketConfiguration CurrentBasket =>
            baskets != null && currentBasketIndex >= 0 && currentBasketIndex < baskets.Length
                ? baskets[currentBasketIndex]
                : null;

        public bool TryValidateConfiguration(out string error)
        {
            error = null;
            if (checkout == null)
            {
                error = "Staged checkout requires a configured checkout station.";
                return false;
            }

            if (!checkout.TryValidateConfiguration(out error))
            {
                return false;
            }

            if (baskets == null || baskets.Length == 0)
            {
                error = "Staged checkout requires at least one configured basket.";
                return false;
            }

            HashSet<string> transactionIds = new(StringComparer.Ordinal);
            foreach (StagedCheckoutBasketConfiguration basket in baskets)
            {
                if (basket == null ||
                    !FirstStoreIdentifier.IsValid(basket.StableTransactionId) ||
                    !transactionIds.Add(basket.StableTransactionId) ||
                    basket.Products == null ||
                    basket.Products.Count == 0)
                {
                    error = "Each staged basket requires a unique valid transaction id and products.";
                    return false;
                }

                foreach (StagedCheckoutProductConfiguration line in basket.Products)
                {
                    if (line == null || line.ProductDefinition == null ||
                        line.QuantityUnits <= 0 ||
                        !checkout.TryGetShelfLocation(
                            line.ProductDefinition.StableProductId,
                            out _))
                    {
                        error = "Each staged basket product requires a configured checkout shelf mapping.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        public bool TryPrimary(
            out CheckoutTransactionSummary summary,
            out CheckoutFailure failure,
            out string error)
        {
            summary = null;
            failure = CheckoutFailure.None;
            if (!TryPrepare(out error))
            {
                failure = CheckoutFailure.InvalidSession;
                return false;
            }

            if (AllBasketsComplete)
            {
                return Reject("All staged baskets are complete.", out error, out failure);
            }

            StagedCheckoutBasketConfiguration basket = CurrentBasket;
            if (TryGetCompletedSummary(basket, out summary))
            {
                firstBlocker = null;
                failure = CheckoutFailure.AlreadyCompleted;
                return true;
            }

            if (!sessionStarted)
            {
                if (!checkout.TryBeginSession(basket.StableTransactionId, out error))
                {
                    firstBlocker = error;
                    failure = CheckoutFailure.InvalidSession;
                    return false;
                }

                sessionStarted = true;
                firstBlocker = null;
                return true;
            }

            if (AllRequiredUnitsScanned())
            {
                if (!checkout.TryComplete(out summary, out failure))
                {
                    firstBlocker = FormatCheckoutFailure(failure);
                    error = firstBlocker;
                    return false;
                }

                firstBlocker = null;
                return true;
            }

            if (!TryGetActiveLine(out StagedCheckoutProductConfiguration line, out int lineIndex))
            {
                return Reject("No staged product is available to scan.", out error, out failure);
            }

            if (!checkout.TryScan(line.ProductDefinition, 1, out failure))
            {
                firstBlocker = FormatCheckoutFailure(failure);
                error = firstBlocker;
                return false;
            }

            scannedQuantities[lineIndex]++;
            scanHistory.Add(lineIndex);
            firstBlocker = null;
            error = null;
            return true;
        }

        public bool TryCorrect(out CheckoutFailure failure, out string error)
        {
            failure = CheckoutFailure.None;
            if (!TryPrepare(out error))
            {
                failure = CheckoutFailure.InvalidSession;
                return false;
            }

            if (AllBasketsComplete || TryGetCompletedSummary(CurrentBasket, out _))
            {
                return Reject("The completed basket cannot be corrected.", out error, out failure);
            }

            if (!sessionStarted || scanHistory.Count == 0)
            {
                return Reject("No scanned product can be corrected.", out error, out failure);
            }

            int historyIndex = scanHistory.Count - 1;
            int lineIndex = scanHistory[historyIndex];
            StagedCheckoutProductConfiguration line = CurrentBasket.Products[lineIndex];
            if (!checkout.TryCorrect(line.ProductDefinition, 1, out failure))
            {
                firstBlocker = FormatCheckoutFailure(failure);
                error = firstBlocker;
                return false;
            }

            scanHistory.RemoveAt(historyIndex);
            scannedQuantities[lineIndex]--;
            firstBlocker = null;
            error = null;
            return true;
        }

        public bool TryContinue(out string error)
        {
            if (!TryPrepare(out error))
            {
                return false;
            }

            if (AllBasketsComplete)
            {
                error = "All staged baskets are complete.";
                firstBlocker = error;
                return false;
            }

            if (!TryGetCompletedSummary(CurrentBasket, out _))
            {
                error = "Complete the current staged basket before continuing.";
                firstBlocker = error;
                return false;
            }

            currentBasketIndex++;
            sessionStarted = false;
            scanHistory.Clear();
            scannedQuantities = null;
            firstBlocker = null;
            error = null;
            return true;
        }

        public void ResetTransientStateAfterRestore()
        {
            currentBasketIndex = 0;
            sessionStarted = false;
            scanHistory.Clear();
            scannedQuantities = null;
            firstBlocker = null;

            while (baskets != null &&
                   currentBasketIndex < baskets.Length &&
                   TryGetCompletedSummary(baskets[currentBasketIndex], out _))
            {
                currentBasketIndex++;
            }
        }

        private bool TryPrepare(out string error)
        {
            if (!TryValidateConfiguration(out error))
            {
                firstBlocker = error;
                return false;
            }

            if (!AllBasketsComplete && scannedQuantities == null)
            {
                scannedQuantities = new int[CurrentBasket.Products.Count];
            }

            return true;
        }

        private bool TryGetActiveLine(
            out StagedCheckoutProductConfiguration line,
            out int lineIndex)
        {
            lineIndex = FindNextIncompleteLineIndex();
            if (lineIndex >= 0 && CurrentBasket != null)
            {
                line = CurrentBasket.Products[lineIndex];
                return true;
            }

            line = null;
            return false;
        }

        private int FindNextIncompleteLineIndex()
        {
            if (CurrentBasket?.Products == null || scannedQuantities == null)
            {
                return -1;
            }

            for (int index = 0; index < CurrentBasket.Products.Count; index++)
            {
                if (scannedQuantities[index] < CurrentBasket.Products[index].QuantityUnits)
                {
                    return index;
                }
            }

            return -1;
        }

        private bool AllRequiredUnitsScanned()
        {
            return FindNextIncompleteLineIndex() < 0 && scannedQuantities != null;
        }

        private StagedCheckoutPrimaryAction GetNextAction()
        {
            if (baskets == null || baskets.Length == 0 || AllBasketsComplete)
            {
                return StagedCheckoutPrimaryAction.None;
            }

            if (!TryValidateConfiguration(out _))
            {
                return StagedCheckoutPrimaryAction.None;
            }

            if (TryGetCompletedSummary(CurrentBasket, out _))
            {
                return StagedCheckoutPrimaryAction.Replay;
            }

            if (!sessionStarted)
            {
                return StagedCheckoutPrimaryAction.Begin;
            }

            return AllRequiredUnitsScanned()
                ? StagedCheckoutPrimaryAction.Complete
                : StagedCheckoutPrimaryAction.Scan;
        }

        private bool TryGetCompletedSummary(
            StagedCheckoutBasketConfiguration basket,
            out CheckoutTransactionSummary summary)
        {
            summary = null;
            if (checkout == null || basket == null)
            {
                return false;
            }

            foreach (CheckoutTransactionSummary candidate in checkout.CompletedTransactions)
            {
                if (string.Equals(
                        candidate.transactionId,
                        basket.StableTransactionId,
                        StringComparison.Ordinal))
                {
                    summary = candidate;
                    return true;
                }
            }

            return false;
        }

        private bool Reject(
            string blocker,
            out string error,
            out CheckoutFailure failure)
        {
            firstBlocker = blocker;
            error = blocker;
            failure = CheckoutFailure.InvalidSession;
            return false;
        }

        private static string FormatCheckoutFailure(CheckoutFailure failure)
        {
            return failure switch
            {
                CheckoutFailure.InsufficientStock => "The staged product is not available on its shelf.",
                CheckoutFailure.AlreadyCompleted => "The staged basket is already complete.",
                CheckoutFailure.CorrectionExceedsScanned => "That correction exceeds the scanned quantity.",
                _ => "Checkout cannot perform that staged action right now."
            };
        }
    }
}
