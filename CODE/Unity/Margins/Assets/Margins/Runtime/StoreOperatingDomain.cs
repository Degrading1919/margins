// Draft implementation — Unity verification pending
using System;

namespace Margins
{
    public enum StoreOperatingState
    {
        Closed,
        Preparing,
        Open,
        Closing,
        ClosedWithResultPending
    }

    public enum StoreOperatingFailure
    {
        None,
        InvalidSessionIdentifier,
        InvalidTransition,
        MissingTotals,
        InvalidTotals
    }

    [Serializable]
    public sealed class StoreSessionTotals : IEquatable<StoreSessionTotals>
    {
        public long grossSalesCents;
        public long costOfGoodsSoldCents;
        public long includedOperatingExpensesCents;
        public long contributionAfterCostOfGoodsCents;
        public int unitsSold;
        public int transactionCount;

        public StoreSessionTotals(
            long grossSalesCents,
            long costOfGoodsSoldCents,
            long includedOperatingExpensesCents,
            long contributionAfterCostOfGoodsCents,
            int unitsSold,
            int transactionCount)
        {
            this.grossSalesCents = grossSalesCents;
            this.costOfGoodsSoldCents = costOfGoodsSoldCents;
            this.includedOperatingExpensesCents = includedOperatingExpensesCents;
            this.contributionAfterCostOfGoodsCents =
                contributionAfterCostOfGoodsCents;
            this.unitsSold = unitsSold;
            this.transactionCount = transactionCount;
        }

        public bool IsValid
        {
            get
            {
                if (grossSalesCents < 0 ||
                    costOfGoodsSoldCents < 0 ||
                    includedOperatingExpensesCents < 0 ||
                    unitsSold < 0 ||
                    transactionCount < 0)
                {
                    return false;
                }

                try
                {
                    return contributionAfterCostOfGoodsCents ==
                           checked(
                               grossSalesCents -
                               costOfGoodsSoldCents -
                               includedOperatingExpensesCents);
                }
                catch (OverflowException)
                {
                    return false;
                }
            }
        }

        public bool Equals(StoreSessionTotals other)
        {
            return other != null &&
                   grossSalesCents == other.grossSalesCents &&
                   costOfGoodsSoldCents == other.costOfGoodsSoldCents &&
                   includedOperatingExpensesCents ==
                   other.includedOperatingExpensesCents &&
                   contributionAfterCostOfGoodsCents ==
                   other.contributionAfterCostOfGoodsCents &&
                   unitsSold == other.unitsSold &&
                   transactionCount == other.transactionCount;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as StoreSessionTotals);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(grossSalesCents);
            hash.Add(costOfGoodsSoldCents);
            hash.Add(includedOperatingExpensesCents);
            hash.Add(contributionAfterCostOfGoodsCents);
            hash.Add(unitsSold);
            hash.Add(transactionCount);
            return hash.ToHashCode();
        }

        public static bool TryCreateFromLedger(
            CompletedTransactionLedger ledger,
            long includedOperatingExpensesCents,
            out StoreSessionTotals totals,
            out string error)
        {
            totals = null;
            if (ledger == null)
            {
                error = "Completed transaction ledger is required for result totals.";
                return false;
            }

            if (includedOperatingExpensesCents < 0)
            {
                error = "Included operating expenses cannot be negative.";
                return false;
            }

            long costOfGoodsSoldCents = 0;
            try
            {
                foreach (CheckoutTransactionSummary transaction in ledger.Transactions)
                {
                    foreach (CheckoutLineSnapshot line in transaction.lines)
                    {
                        costOfGoodsSoldCents = checked(
                            costOfGoodsSoldCents +
                            line.LineCostCents);
                    }
                }

                long contributionAfterCostOfGoodsCents = checked(
                    ledger.GrossSalesCents -
                    costOfGoodsSoldCents -
                    includedOperatingExpensesCents);
                totals = new StoreSessionTotals(
                    ledger.GrossSalesCents,
                    costOfGoodsSoldCents,
                    includedOperatingExpensesCents,
                    contributionAfterCostOfGoodsCents,
                    ledger.UnitsSold,
                    ledger.TransactionCount);
                error = null;
                return true;
            }
            catch (OverflowException)
            {
                error = "Result totals overflow integer-cent storage.";
                return false;
            }
        }

        public static bool TryValidateAgainstLedger(
            StoreSessionTotals totals,
            CompletedTransactionLedger ledger,
            out string error)
        {
            if (totals == null || !totals.IsValid)
            {
                error = "Store result totals are invalid.";
                return false;
            }

            if (!TryCreateFromLedger(
                    ledger,
                    totals.includedOperatingExpensesCents,
                    out StoreSessionTotals expected,
                    out error))
            {
                return false;
            }

            if (!totals.Equals(expected))
            {
                error =
                    "Store result totals contradict the completed transaction ledger or historical product costs.";
                return false;
            }

            error = null;
            return true;
        }

        internal static StoreSessionTotals Clone(StoreSessionTotals source)
        {
            return source == null
                ? null
                : new StoreSessionTotals(
                    source.grossSalesCents,
                    source.costOfGoodsSoldCents,
                    source.includedOperatingExpensesCents,
                    source.contributionAfterCostOfGoodsCents,
                    source.unitsSold,
                    source.transactionCount);
        }
    }

    [Serializable]
    public sealed class StoreOperatingSnapshot : IEquatable<StoreOperatingSnapshot>
    {
        public string sessionId;
        public StoreOperatingState state;
        public bool hasResult;
        public StoreSessionTotals totals;

        public StoreOperatingSnapshot(
            string sessionId,
            StoreOperatingState state,
            bool hasResult,
            StoreSessionTotals totals)
        {
            this.sessionId = sessionId;
            this.state = state;
            this.hasResult = hasResult;
            this.totals = totals;
        }

        public bool Equals(StoreOperatingSnapshot other)
        {
            return other != null &&
                   string.Equals(sessionId, other.sessionId, StringComparison.Ordinal) &&
                   state == other.state &&
                   hasResult == other.hasResult &&
                   FirstStoreEquality.AreEqual(totals, other.totals);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as StoreOperatingSnapshot);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(sessionId, state, hasResult, totals);
        }
    }

    public sealed class StoreOperatingSession
    {
        private StoreSessionTotals totals;

        public string SessionId { get; }
        public StoreOperatingState State { get; private set; }
        public StoreSessionTotals Totals => StoreSessionTotals.Clone(totals);
        public bool HasResult => totals != null;

        private StoreOperatingSession(
            string sessionId,
            StoreOperatingState state,
            StoreSessionTotals totals)
        {
            SessionId = sessionId;
            State = state;
            this.totals = StoreSessionTotals.Clone(totals);
        }

        public static bool TryCreate(
            string sessionId,
            out StoreOperatingSession session,
            out StoreOperatingFailure failure)
        {
            if (!FirstStoreIdentifier.IsValid(sessionId))
            {
                session = null;
                failure = StoreOperatingFailure.InvalidSessionIdentifier;
                return false;
            }

            session = new StoreOperatingSession(
                sessionId,
                StoreOperatingState.Closed,
                null);
            failure = StoreOperatingFailure.None;
            return true;
        }

        public bool TryTransition(
            StoreOperatingState nextState,
            out StoreOperatingFailure failure)
        {
            if (State == StoreOperatingState.Closing &&
                nextState == StoreOperatingState.ClosedWithResultPending)
            {
                failure = StoreOperatingFailure.MissingTotals;
                return false;
            }

            if (!IsValidTransition(State, nextState))
            {
                failure = StoreOperatingFailure.InvalidTransition;
                return false;
            }

            if (State == StoreOperatingState.Closed &&
                nextState == StoreOperatingState.Preparing)
            {
                totals = null;
            }

            State = nextState;
            failure = StoreOperatingFailure.None;
            return true;
        }

        public bool TryFinalizeClosing(
            CompletedTransactionLedger ledger,
            long includedOperatingExpensesCents,
            out StoreOperatingFailure failure)
        {
            if (State != StoreOperatingState.Closing)
            {
                failure = StoreOperatingFailure.InvalidTransition;
                return false;
            }

            if (!StoreSessionTotals.TryCreateFromLedger(
                    ledger,
                    includedOperatingExpensesCents,
                    out StoreSessionTotals derivedTotals,
                    out _))
            {
                failure = StoreOperatingFailure.InvalidTotals;
                return false;
            }

            totals = derivedTotals;
            State = StoreOperatingState.ClosedWithResultPending;
            failure = StoreOperatingFailure.None;
            return true;
        }

        public StoreOperatingSnapshot CreateSnapshot()
        {
            StoreSessionTotals totalsCopy = StoreSessionTotals.Clone(totals);
            return new StoreOperatingSnapshot(
                SessionId,
                State,
                totalsCopy != null,
                totalsCopy);
        }

        public static bool TryRestore(
            StoreOperatingSnapshot snapshot,
            CompletedTransactionLedger ledger,
            out StoreOperatingSession session,
            out string error)
        {
            session = null;
            if (snapshot == null ||
                !FirstStoreIdentifier.IsValid(snapshot.sessionId) ||
                !Enum.IsDefined(typeof(StoreOperatingState), snapshot.state))
            {
                error = "Store operating snapshot is invalid.";
                return false;
            }

            StoreSessionTotals restoredTotals = snapshot.totals;
            if (!snapshot.hasResult &&
                restoredTotals != null &&
                !IsSerializedNoResultPlaceholder(restoredTotals))
            {
                error = "Store operating result flag and totals disagree.";
                return false;
            }

            if (snapshot.hasResult && restoredTotals == null)
            {
                error = "Store operating result flag and totals disagree.";
                return false;
            }

            if (!snapshot.hasResult)
            {
                restoredTotals = null;
            }

            if (restoredTotals != null &&
                !StoreSessionTotals.TryValidateAgainstLedger(
                    restoredTotals,
                    ledger,
                    out error))
            {
                return false;
            }

            if (snapshot.state == StoreOperatingState.ClosedWithResultPending &&
                restoredTotals == null)
            {
                error = "Closed-with-result-pending state requires totals.";
                return false;
            }

            if ((snapshot.state == StoreOperatingState.Preparing ||
                 snapshot.state == StoreOperatingState.Open ||
                 snapshot.state == StoreOperatingState.Closing) &&
                restoredTotals != null)
            {
                error = $"Store state '{snapshot.state}' cannot retain prior result totals.";
                return false;
            }

            session = new StoreOperatingSession(
                snapshot.sessionId,
                snapshot.state,
                restoredTotals);
            error = null;
            return true;
        }

        private static bool IsSerializedNoResultPlaceholder(
            StoreSessionTotals totals)
        {
            return totals.grossSalesCents == 0 &&
                   totals.costOfGoodsSoldCents == 0 &&
                   totals.includedOperatingExpensesCents == 0 &&
                   totals.contributionAfterCostOfGoodsCents == 0 &&
                   totals.unitsSold == 0 &&
                   totals.transactionCount == 0;
        }

        private static bool IsValidTransition(
            StoreOperatingState current,
            StoreOperatingState next)
        {
            return (current == StoreOperatingState.Closed &&
                    next == StoreOperatingState.Preparing) ||
                   (current == StoreOperatingState.Preparing &&
                    (next == StoreOperatingState.Open ||
                     next == StoreOperatingState.Closed)) ||
                   (current == StoreOperatingState.Open &&
                    next == StoreOperatingState.Closing) ||
                   (current == StoreOperatingState.Closing &&
                    next == StoreOperatingState.ClosedWithResultPending) ||
                   (current == StoreOperatingState.ClosedWithResultPending &&
                    next == StoreOperatingState.Closed);
        }
    }
}
