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
        public int quantityUnits;

        public CheckoutLineSnapshot(
            string productId,
            int unitPriceCents,
            int quantityUnits)
        {
            this.productId = productId;
            this.unitPriceCents = unitPriceCents;
            this.quantityUnits = quantityUnits;
        }

        public long LineTotalCents => (long)unitPriceCents * quantityUnits;

        public bool Equals(CheckoutLineSnapshot other)
        {
            return other != null &&
                   string.Equals(productId, other.productId, StringComparison.Ordinal) &&
                   unitPriceCents == other.unitPriceCents &&
                   quantityUnits == other.quantityUnits;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CheckoutLineSnapshot);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(productId, unitPriceCents, quantityUnits);
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

    public enum CheckoutFailure
    {
        None,
        AlreadyCompleted,
        InvalidSession,
        InvalidProduct,
        InvalidPrice,
        InvalidQuantity,
        PriceMismatch,
        InsufficientStock,
        MissingLine,
        CorrectionExceedsScanned,
        EmptySession,
        ArithmeticOverflow,
        InventoryChanged
    }

    public sealed class CheckoutSession
    {
        private readonly FirstStoreInventory inventory;
        private readonly string shelfLocationId;
        private readonly List<CheckoutLineSnapshot> lines = new();
        private CheckoutTransactionSummary completedSummary;

        public string TransactionId { get; }
        public bool IsCompleted => completedSummary != null;
        public IReadOnlyList<CheckoutLineSnapshot> Lines => lines;

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
            string shelfLocationId,
            string transactionId)
        {
            this.inventory = inventory;
            this.shelfLocationId = shelfLocationId;
            TransactionId = transactionId;
        }

        public static bool TryCreate(
            FirstStoreInventory inventory,
            string shelfLocationId,
            string transactionId,
            out CheckoutSession session,
            out string error)
        {
            session = null;
            if (inventory == null ||
                !FirstStoreIdentifier.IsValid(shelfLocationId) ||
                !FirstStoreIdentifier.IsValid(transactionId) ||
                !inventory.TryGetLocationKind(
                    shelfLocationId,
                    out InventoryLocationKind kind) ||
                kind != InventoryLocationKind.Shelf)
            {
                error = "Checkout session configuration is invalid.";
                return false;
            }

            session = new CheckoutSession(inventory, shelfLocationId, transactionId);
            error = null;
            return true;
        }

        public bool TryScan(
            string productId,
            int unitPriceCents,
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

            if (unitPriceCents <= 0)
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
            if (line != null && line.unitPriceCents != unitPriceCents)
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
                lines.Add(new CheckoutLineSnapshot(productId, unitPriceCents, quantityUnits));
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
            out CheckoutTransactionSummary summary,
            out CheckoutFailure failure)
        {
            if (completedSummary != null)
            {
                summary = CloneSummary(completedSummary);
                failure = CheckoutFailure.AlreadyCompleted;
                return true;
            }

            if (lines.Count == 0)
            {
                summary = null;
                failure = CheckoutFailure.EmptySession;
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

            if (!inventory.TryConsumeForSale(
                    shelfLocationId,
                    requested,
                    out InventorySaleFailure inventoryFailure))
            {
                summary = null;
                failure = inventoryFailure == InventorySaleFailure.InsufficientQuantity
                    ? CheckoutFailure.InsufficientStock
                    : CheckoutFailure.InventoryChanged;
                return false;
            }

            completedSummary = candidateSummary;
            summary = CloneSummary(completedSummary);
            failure = CheckoutFailure.None;
            return true;
        }

        public CheckoutTransactionSummary GetCompletedSummary()
        {
            return CloneSummary(completedSummary);
        }

        public static bool TryRestoreCompleted(
            FirstStoreInventory inventory,
            string shelfLocationId,
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
                    shelfLocationId,
                    summary.transactionId,
                    out session,
                    out error))
            {
                return false;
            }

            foreach (CheckoutLineSnapshot line in summary.lines)
            {
                if (!inventory.IsKnownProduct(line.productId))
                {
                    session = null;
                    error =
                        $"Completed checkout summary references unknown product '{line.productId}'.";
                    return false;
                }

                session.lines.Add(
                    new CheckoutLineSnapshot(
                        line.productId,
                        line.unitPriceCents,
                        line.quantityUnits));
            }
            session.completedSummary = CloneSummary(summary);
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
                    line.quantityUnits);
                summary.lines.Add(copy);
                summary.subtotalCents = checked(summary.subtotalCents + copy.LineTotalCents);
                summary.unitsSold = checked(summary.unitsSold + copy.quantityUnits);
            }

            return summary;
        }

        private static CheckoutTransactionSummary CloneSummary(
            CheckoutTransactionSummary source)
        {
            if (source == null)
            {
                return null;
            }

            CheckoutTransactionSummary clone = new(source.transactionId)
            {
                subtotalCents = source.subtotalCents,
                unitsSold = source.unitsSold,
                isCompleted = source.isCompleted
            };

            foreach (CheckoutLineSnapshot line in source.lines)
            {
                clone.lines.Add(
                    new CheckoutLineSnapshot(
                        line.productId,
                        line.unitPriceCents,
                        line.quantityUnits));
            }
            return clone;
        }
    }
}
