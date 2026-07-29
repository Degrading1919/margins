// Draft implementation — Unity verification pending
using System;
using System.Collections.Generic;

namespace Margins
{
    [Serializable]
    public sealed class CheckoutLineSnapshot : IEquatable<CheckoutLineSnapshot>
    {
        public string productId;
        public int unitPriceCents;
        public int unitCostCents;
        public int quantityUnits;

        public CheckoutLineSnapshot(
            string productId,
            int unitPriceCents,
            int unitCostCents,
            int quantityUnits)
        {
            this.productId = productId;
            this.unitPriceCents = unitPriceCents;
            this.unitCostCents = unitCostCents;
            this.quantityUnits = quantityUnits;
        }

        public long LineTotalCents => (long)unitPriceCents * quantityUnits;
        public long LineCostCents => (long)unitCostCents * quantityUnits;

        public bool Equals(CheckoutLineSnapshot other)
        {
            return other != null &&
                   string.Equals(productId, other.productId, StringComparison.Ordinal) &&
                   unitPriceCents == other.unitPriceCents &&
                   unitCostCents == other.unitCostCents &&
                   quantityUnits == other.quantityUnits;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CheckoutLineSnapshot);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                productId,
                unitPriceCents,
                unitCostCents,
                quantityUnits);
        }
    }

    [Serializable]
    public sealed class CheckoutTransactionSummary : IEquatable<CheckoutTransactionSummary>
    {
        public string transactionId;
        public List<CheckoutLineSnapshot> lines = new();
        public long subtotalCents;
        public int unitsSold;
        public bool isCompleted;

        public CheckoutTransactionSummary(string transactionId)
        {
            this.transactionId = transactionId;
        }

        public bool Equals(CheckoutTransactionSummary other)
        {
            if (other == null ||
                !string.Equals(transactionId, other.transactionId, StringComparison.Ordinal) ||
                subtotalCents != other.subtotalCents ||
                unitsSold != other.unitsSold ||
                isCompleted != other.isCompleted ||
                lines == null ||
                other.lines == null ||
                lines.Count != other.lines.Count)
            {
                return false;
            }

            for (int index = 0; index < lines.Count; index++)
            {
                if (!lines[index].Equals(other.lines[index]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CheckoutTransactionSummary);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(transactionId);
            hash.Add(subtotalCents);
            hash.Add(unitsSold);
            hash.Add(isCompleted);
            if (lines != null)
            {
                foreach (CheckoutLineSnapshot line in lines)
                {
                    hash.Add(line);
                }
            }
            return hash.ToHashCode();
        }
    }

    internal static class CheckoutSnapshotCopies
    {
        public static List<CheckoutLineSnapshot> CloneLines(
            IReadOnlyList<CheckoutLineSnapshot> source)
        {
            List<CheckoutLineSnapshot> clones = new();
            if (source == null)
            {
                return clones;
            }

            foreach (CheckoutLineSnapshot line in source)
            {
                clones.Add(
                    new CheckoutLineSnapshot(
                        line.productId,
                        line.unitPriceCents,
                        line.unitCostCents,
                        line.quantityUnits));
            }

            return clones;
        }

        public static CheckoutTransactionSummary CloneSummary(
            CheckoutTransactionSummary source)
        {
            if (source == null)
            {
                return null;
            }

            return new CheckoutTransactionSummary(source.transactionId)
            {
                subtotalCents = source.subtotalCents,
                unitsSold = source.unitsSold,
                isCompleted = source.isCompleted,
                lines = CloneLines(source.lines)
            };
        }
    }

    public enum CompletedTransactionLedgerFailure
    {
        None,
        InvalidSummary,
        DuplicateTransactionId,
        CapacityExceeded,
        ArithmeticOverflow
    }

    [Serializable]
    public sealed class CompletedTransactionLedgerSnapshot :
        IEquatable<CompletedTransactionLedgerSnapshot>
    {
        public int maximumTransactionCount;
        public List<CheckoutTransactionSummary> transactions = new();

        public CompletedTransactionLedgerSnapshot(int maximumTransactionCount)
        {
            this.maximumTransactionCount = maximumTransactionCount;
        }

        public bool Equals(CompletedTransactionLedgerSnapshot other)
        {
            if (other == null ||
                maximumTransactionCount != other.maximumTransactionCount ||
                transactions == null ||
                other.transactions == null ||
                transactions.Count != other.transactions.Count)
            {
                return false;
            }

            for (int index = 0; index < transactions.Count; index++)
            {
                if (!transactions[index].Equals(other.transactions[index]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CompletedTransactionLedgerSnapshot);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(maximumTransactionCount);
            if (transactions != null)
            {
                foreach (CheckoutTransactionSummary transaction in transactions)
                {
                    hash.Add(transaction);
                }
            }
            return hash.ToHashCode();
        }
    }

    public sealed class CompletedTransactionLedger
    {
        private readonly SortedDictionary<string, CheckoutTransactionSummary> transactions =
            new(StringComparer.Ordinal);
        private long grossSalesCents;
        private int unitsSold;

        public int MaximumTransactionCount { get; }
        public int TransactionCount => transactions.Count;
        public long GrossSalesCents => grossSalesCents;
        public int UnitsSold => unitsSold;

        public IReadOnlyList<CheckoutTransactionSummary> Transactions
        {
            get
            {
                List<CheckoutTransactionSummary> copies = new();
                foreach (CheckoutTransactionSummary transaction in transactions.Values)
                {
                    copies.Add(CheckoutSnapshotCopies.CloneSummary(transaction));
                }
                return copies;
            }
        }

        public CompletedTransactionLedger(int maximumTransactionCount)
        {
            if (maximumTransactionCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumTransactionCount));
            }

            MaximumTransactionCount = maximumTransactionCount;
        }

        public bool ContainsTransaction(string transactionId)
        {
            return FirstStoreIdentifier.IsValid(transactionId) &&
                   transactions.ContainsKey(transactionId);
        }

        public bool TryGetTransaction(
            string transactionId,
            out CheckoutTransactionSummary summary)
        {
            if (transactions.TryGetValue(
                    transactionId,
                    out CheckoutTransactionSummary stored))
            {
                summary = CheckoutSnapshotCopies.CloneSummary(stored);
                return true;
            }

            summary = null;
            return false;
        }

        public bool CanAdd(
            CheckoutTransactionSummary summary,
            out CompletedTransactionLedgerFailure failure)
        {
            return TryValidateAdd(summary, out _, out _, out failure);
        }

        public bool TryAdd(
            CheckoutTransactionSummary summary,
            out CompletedTransactionLedgerFailure failure)
        {
            if (!TryValidateAdd(
                    summary,
                    out long nextGrossSales,
                    out int nextUnitsSold,
                    out failure))
            {
                return false;
            }

            transactions.Add(
                summary.transactionId,
                CheckoutSnapshotCopies.CloneSummary(summary));
            grossSalesCents = nextGrossSales;
            unitsSold = nextUnitsSold;
            return true;
        }

        public CompletedTransactionLedgerSnapshot CreateSnapshot()
        {
            CompletedTransactionLedgerSnapshot snapshot = new(MaximumTransactionCount);
            foreach (CheckoutTransactionSummary transaction in transactions.Values)
            {
                snapshot.transactions.Add(
                    CheckoutSnapshotCopies.CloneSummary(transaction));
            }
            return snapshot;
        }

        public static bool TryRestore(
            CompletedTransactionLedgerSnapshot snapshot,
            out CompletedTransactionLedger ledger,
            out string error)
        {
            ledger = null;
            if (snapshot == null ||
                snapshot.maximumTransactionCount <= 0 ||
                snapshot.transactions == null)
            {
                error = "Completed transaction ledger snapshot is invalid.";
                return false;
            }

            CompletedTransactionLedger candidate =
                new(snapshot.maximumTransactionCount);
            string previousTransactionId = null;
            foreach (CheckoutTransactionSummary transaction in snapshot.transactions)
            {
                if (transaction == null)
                {
                    error = "Completed transaction ledger contains a null transaction.";
                    return false;
                }

                if (previousTransactionId != null &&
                    string.CompareOrdinal(
                        previousTransactionId,
                        transaction.transactionId) >= 0)
                {
                    error =
                        "Completed transaction ledger is not in deterministic transaction-id order.";
                    return false;
                }

                if (!candidate.TryAdd(
                        transaction,
                        out CompletedTransactionLedgerFailure failure))
                {
                    error =
                        $"Completed transaction '{transaction.transactionId}' restore failed ({failure}).";
                    return false;
                }

                previousTransactionId = transaction.transactionId;
            }

            ledger = candidate;
            error = null;
            return true;
        }

        private bool TryValidateAdd(
            CheckoutTransactionSummary summary,
            out long nextGrossSales,
            out int nextUnitsSold,
            out CompletedTransactionLedgerFailure failure)
        {
            nextGrossSales = grossSalesCents;
            nextUnitsSold = unitsSold;
            if (!CheckoutSession.TryValidateSummary(summary, out _))
            {
                failure = CompletedTransactionLedgerFailure.InvalidSummary;
                return false;
            }

            if (transactions.ContainsKey(summary.transactionId))
            {
                failure = CompletedTransactionLedgerFailure.DuplicateTransactionId;
                return false;
            }

            if (transactions.Count >= MaximumTransactionCount)
            {
                failure = CompletedTransactionLedgerFailure.CapacityExceeded;
                return false;
            }

            try
            {
                nextGrossSales = checked(grossSalesCents + summary.subtotalCents);
                nextUnitsSold = checked(unitsSold + summary.unitsSold);
            }
            catch (OverflowException)
            {
                failure = CompletedTransactionLedgerFailure.ArithmeticOverflow;
                return false;
            }

            failure = CompletedTransactionLedgerFailure.None;
            return true;
        }
    }

    public enum CheckoutFailure
    {
        None,
        AlreadyCompleted,
        InvalidSession,
        InvalidProduct,
        MissingShelfMapping,
        InvalidPrice,
        InvalidQuantity,
        PriceMismatch,
        InsufficientStock,
        MissingLine,
        CorrectionExceedsScanned,
        EmptySession,
        ArithmeticOverflow,
        InventoryChanged,
        DuplicateTransactionId,
        LedgerCapacityExceeded,
        LedgerRejected
    }

    public sealed class CheckoutSession
    {
        private readonly FirstStoreInventory inventory;
        private readonly SortedDictionary<string, string> shelfLocationIdsByProduct;
        private readonly List<CheckoutLineSnapshot> lines = new();
        private CheckoutTransactionSummary completedSummary;

        public string TransactionId { get; }
        public bool IsCompleted => completedSummary != null;
        public IReadOnlyList<CheckoutLineSnapshot> Lines =>
            CheckoutSnapshotCopies.CloneLines(lines);

        public long SubtotalCents
        {
            get
            {
                long subtotal = 0;
                foreach (CheckoutLineSnapshot line in lines)
                {
                    subtotal = checked(subtotal + line.LineTotalCents);
                }
                return subtotal;
            }
        }

        private CheckoutSession(
            FirstStoreInventory inventory,
            IReadOnlyDictionary<string, string> shelfLocationIdsByProduct,
            string transactionId)
        {
            this.inventory = inventory;
            this.shelfLocationIdsByProduct = new SortedDictionary<string, string>(
                StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> mapping in shelfLocationIdsByProduct)
            {
                this.shelfLocationIdsByProduct.Add(mapping.Key, mapping.Value);
            }
            TransactionId = transactionId;
        }

        public static bool TryCreate(
            FirstStoreInventory inventory,
            IReadOnlyDictionary<string, string> shelfLocationIdsByProduct,
            string transactionId,
            out CheckoutSession session,
            out string error)
        {
            session = null;
            if (inventory == null ||
                !FirstStoreIdentifier.IsValid(transactionId) ||
                shelfLocationIdsByProduct == null ||
                shelfLocationIdsByProduct.Count == 0)
            {
                error = "Checkout session configuration is invalid.";
                return false;
            }

            foreach (KeyValuePair<string, string> mapping in shelfLocationIdsByProduct)
            {
                if (!FirstStoreIdentifier.IsValid(mapping.Key) ||
                    !inventory.IsKnownProduct(mapping.Key) ||
                    !FirstStoreIdentifier.IsValid(mapping.Value) ||
                    !inventory.TryGetLocationKind(
                        mapping.Value,
                        out InventoryLocationKind kind) ||
                    kind != InventoryLocationKind.Shelf)
                {
                    error = "Checkout shelf mapping is invalid.";
                    return false;
                }
            }

            session = new CheckoutSession(
                inventory,
                shelfLocationIdsByProduct,
                transactionId);
            error = null;
            return true;
        }

        public bool TryScan(
            string productId,
            int unitPriceCents,
            int unitCostCents,
            int quantityUnits,
            out CheckoutFailure failure)
        {
            if (IsCompleted)
            {
                failure = CheckoutFailure.AlreadyCompleted;
                return false;
            }

            if (!FirstStoreIdentifier.IsValid(productId) ||
                !inventory.IsKnownProduct(productId))
            {
                failure = CheckoutFailure.InvalidProduct;
                return false;
            }

            if (!shelfLocationIdsByProduct.TryGetValue(
                    productId,
                    out string shelfLocationId))
            {
                failure = CheckoutFailure.MissingShelfMapping;
                return false;
            }

            if (unitPriceCents <= 0)
            {
                failure = CheckoutFailure.InvalidPrice;
                return false;
            }

            if (unitCostCents < 0)
            {
                failure = CheckoutFailure.InvalidPrice;
                return false;
            }

            if (quantityUnits <= 0)
            {
                failure = CheckoutFailure.InvalidQuantity;
                return false;
            }

            CheckoutLineSnapshot line = FindLine(productId);
            if (line != null &&
                (line.unitPriceCents != unitPriceCents ||
                 line.unitCostCents != unitCostCents))
            {
                failure = CheckoutFailure.PriceMismatch;
                return false;
            }

            int existingQuantity = line?.quantityUnits ?? 0;
            if (quantityUnits > int.MaxValue - existingQuantity)
            {
                failure = CheckoutFailure.ArithmeticOverflow;
                return false;
            }

            int newQuantity = existingQuantity + quantityUnits;
            if (inventory.GetQuantity(shelfLocationId, productId) < newQuantity)
            {
                failure = CheckoutFailure.InsufficientStock;
                return false;
            }

            int currentUnits = 0;
            foreach (CheckoutLineSnapshot existingLine in lines)
            {
                currentUnits = checked(currentUnits + existingLine.quantityUnits);
            }

            if (quantityUnits > int.MaxValue - currentUnits)
            {
                failure = CheckoutFailure.ArithmeticOverflow;
                return false;
            }

            long addedTotal = (long)unitPriceCents * quantityUnits;
            long currentSubtotal;
            try
            {
                currentSubtotal = SubtotalCents;
            }
            catch (OverflowException)
            {
                failure = CheckoutFailure.ArithmeticOverflow;
                return false;
            }

            if (addedTotal > long.MaxValue - currentSubtotal)
            {
                failure = CheckoutFailure.ArithmeticOverflow;
                return false;
            }

            if (line == null)
            {
                lines.Add(
                    new CheckoutLineSnapshot(
                        productId,
                        unitPriceCents,
                        unitCostCents,
                        quantityUnits));
                lines.Sort((left, right) => string.CompareOrdinal(left.productId, right.productId));
            }
            else
            {
                line.quantityUnits = newQuantity;
            }

            failure = CheckoutFailure.None;
            return true;
        }

        public bool TryRemove(
            string productId,
            int quantityUnits,
            out CheckoutFailure failure)
        {
            if (IsCompleted)
            {
                failure = CheckoutFailure.AlreadyCompleted;
                return false;
            }

            if (quantityUnits <= 0)
            {
                failure = CheckoutFailure.InvalidQuantity;
                return false;
            }

            CheckoutLineSnapshot line = FindLine(productId);
            if (line == null)
            {
                failure = CheckoutFailure.MissingLine;
                return false;
            }

            if (quantityUnits > line.quantityUnits)
            {
                failure = CheckoutFailure.CorrectionExceedsScanned;
                return false;
            }

            line.quantityUnits -= quantityUnits;
            if (line.quantityUnits == 0)
            {
                lines.Remove(line);
            }

            failure = CheckoutFailure.None;
            return true;
        }

        public bool TryComplete(
            CompletedTransactionLedger ledger,
            out CheckoutTransactionSummary summary,
            out CheckoutFailure failure)
        {
            if (completedSummary != null)
            {
                summary = CheckoutSnapshotCopies.CloneSummary(completedSummary);
                failure = CheckoutFailure.AlreadyCompleted;
                return true;
            }

            if (lines.Count == 0)
            {
                summary = null;
                failure = CheckoutFailure.EmptySession;
                return false;
            }

            if (ledger == null)
            {
                summary = null;
                failure = CheckoutFailure.LedgerRejected;
                return false;
            }

            Dictionary<string, int> requested = new(StringComparer.Ordinal);
            foreach (CheckoutLineSnapshot line in lines)
            {
                requested.Add(line.productId, line.quantityUnits);
            }

            CheckoutTransactionSummary candidateSummary;
            try
            {
                candidateSummary = BuildSummary(TransactionId, lines);
            }
            catch (OverflowException)
            {
                summary = null;
                failure = CheckoutFailure.ArithmeticOverflow;
                return false;
            }

            if (!ledger.CanAdd(
                    candidateSummary,
                    out CompletedTransactionLedgerFailure ledgerFailure))
            {
                summary = null;
                failure = ledgerFailure switch
                {
                    CompletedTransactionLedgerFailure.DuplicateTransactionId =>
                        CheckoutFailure.DuplicateTransactionId,
                    CompletedTransactionLedgerFailure.CapacityExceeded =>
                        CheckoutFailure.LedgerCapacityExceeded,
                    CompletedTransactionLedgerFailure.ArithmeticOverflow =>
                        CheckoutFailure.ArithmeticOverflow,
                    _ => CheckoutFailure.LedgerRejected
                };
                return false;
            }

            if (!inventory.TryConsumeMappedSale(
                    shelfLocationIdsByProduct,
                    requested,
                    out InventorySaleFailure inventoryFailure))
            {
                summary = null;
                failure = inventoryFailure == InventorySaleFailure.InsufficientQuantity
                    ? CheckoutFailure.InsufficientStock
                    : CheckoutFailure.InventoryChanged;
                return false;
            }

            if (!ledger.TryAdd(candidateSummary, out ledgerFailure))
            {
                throw new InvalidOperationException(
                    $"Validated transaction ledger add failed after inventory consumption ({ledgerFailure}).");
            }

            completedSummary = candidateSummary;
            summary = CheckoutSnapshotCopies.CloneSummary(completedSummary);
            failure = CheckoutFailure.None;
            return true;
        }

        public CheckoutTransactionSummary GetCompletedSummary()
        {
            return CheckoutSnapshotCopies.CloneSummary(completedSummary);
        }

        public static bool TryRestoreCompleted(
            FirstStoreInventory inventory,
            IReadOnlyDictionary<string, string> shelfLocationIdsByProduct,
            CheckoutTransactionSummary summary,
            out CheckoutSession session,
            out string error)
        {
            session = null;
            error = null;
            if (summary == null ||
                !summary.isCompleted ||
                summary.lines == null ||
                !TryValidateSummary(summary, out error))
            {
                error ??= "Completed checkout summary is invalid.";
                return false;
            }

            if (!TryCreate(
                    inventory,
                    shelfLocationIdsByProduct,
                    summary.transactionId,
                    out session,
                    out error))
            {
                return false;
            }

            foreach (CheckoutLineSnapshot line in summary.lines)
            {
                if (!inventory.IsKnownProduct(line.productId) ||
                    !shelfLocationIdsByProduct.ContainsKey(line.productId))
                {
                    session = null;
                    error =
                        $"Completed checkout summary references an unmapped product '{line.productId}'.";
                    return false;
                }

                session.lines.Add(
                    new CheckoutLineSnapshot(
                        line.productId,
                        line.unitPriceCents,
                        line.unitCostCents,
                        line.quantityUnits));
            }
            session.completedSummary = CheckoutSnapshotCopies.CloneSummary(summary);
            return true;
        }

        public static bool TryValidateSummary(
            CheckoutTransactionSummary summary,
            out string error)
        {
            if (summary == null ||
                !summary.isCompleted ||
                !FirstStoreIdentifier.IsValid(summary.transactionId) ||
                summary.lines == null ||
                summary.lines.Count == 0)
            {
                error = "Checkout summary is incomplete.";
                return false;
            }

            HashSet<string> productIds = new(StringComparer.Ordinal);
            long expectedSubtotal = 0;
            int expectedUnits = 0;
            string previousProductId = null;
            foreach (CheckoutLineSnapshot line in summary.lines)
            {
                if (line == null ||
                    !FirstStoreIdentifier.IsValid(line.productId) ||
                    line.unitPriceCents <= 0 ||
                    line.unitCostCents < 0 ||
                    line.quantityUnits <= 0 ||
                    !productIds.Add(line.productId))
                {
                    error = "Checkout summary contains an invalid or duplicate line.";
                    return false;
                }

                if (previousProductId != null &&
                    string.CompareOrdinal(previousProductId, line.productId) >= 0)
                {
                    error = "Checkout summary lines are not in deterministic product order.";
                    return false;
                }

                try
                {
                    expectedSubtotal = checked(expectedSubtotal + line.LineTotalCents);
                    expectedUnits = checked(expectedUnits + line.quantityUnits);
                }
                catch (OverflowException)
                {
                    error = "Checkout summary totals overflow.";
                    return false;
                }

                previousProductId = line.productId;
            }

            if (summary.subtotalCents != expectedSubtotal ||
                summary.unitsSold != expectedUnits)
            {
                error = "Checkout summary totals do not match its lines.";
                return false;
            }

            error = null;
            return true;
        }

        private CheckoutLineSnapshot FindLine(string productId)
        {
            foreach (CheckoutLineSnapshot line in lines)
            {
                if (string.Equals(line.productId, productId, StringComparison.Ordinal))
                {
                    return line;
                }
            }

            return null;
        }

        private static CheckoutTransactionSummary BuildSummary(
            string transactionId,
            IReadOnlyList<CheckoutLineSnapshot> sourceLines)
        {
            CheckoutTransactionSummary summary = new(transactionId)
            {
                isCompleted = true
            };

            foreach (CheckoutLineSnapshot line in sourceLines)
            {
                CheckoutLineSnapshot copy = new(
                    line.productId,
                    line.unitPriceCents,
                    line.unitCostCents,
                    line.quantityUnits);
                summary.lines.Add(copy);
                summary.subtotalCents = checked(summary.subtotalCents + copy.LineTotalCents);
                summary.unitsSold = checked(summary.unitsSold + copy.quantityUnits);
            }

            return summary;
        }

    }
}
