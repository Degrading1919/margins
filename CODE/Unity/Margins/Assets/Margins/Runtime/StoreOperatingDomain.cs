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
        public int unitsSold;
        public int transactionCount;
        public long includedOperatingExpensesCents;

        public StoreSessionTotals(
            long grossSalesCents,
            int unitsSold,
            int transactionCount,
            long includedOperatingExpensesCents)
        {
            this.grossSalesCents = grossSalesCents;
            this.unitsSold = unitsSold;
            this.transactionCount = transactionCount;
            this.includedOperatingExpensesCents = includedOperatingExpensesCents;
        }

        public long ContributionBeforeCostOfGoodsCents =>
            grossSalesCents - includedOperatingExpensesCents;

        public bool IsValid =>
            grossSalesCents >= 0 &&
            unitsSold >= 0 &&
            transactionCount >= 0 &&
            includedOperatingExpensesCents >= 0;

        public bool Equals(StoreSessionTotals other)
        {
            return other != null &&
                   grossSalesCents == other.grossSalesCents &&
                   unitsSold == other.unitsSold &&
                   transactionCount == other.transactionCount &&
                   includedOperatingExpensesCents ==
                   other.includedOperatingExpensesCents;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as StoreSessionTotals);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                grossSalesCents,
                unitsSold,
                transactionCount,
                includedOperatingExpensesCents);
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
        public string SessionId { get; }
        public StoreOperatingState State { get; private set; }
        public StoreSessionTotals Totals { get; private set; }
        public bool HasResult => Totals != null;

        private StoreOperatingSession(
            string sessionId,
            StoreOperatingState state,
            StoreSessionTotals totals)
        {
            SessionId = sessionId;
            State = state;
            Totals = totals;
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
            StoreSessionTotals closingTotals,
            out StoreOperatingFailure failure)
        {
            if (!IsValidTransition(State, nextState))
            {
                failure = StoreOperatingFailure.InvalidTransition;
                return false;
            }

            if (State == StoreOperatingState.Closing &&
                nextState == StoreOperatingState.ClosedWithResultPending)
            {
                if (closingTotals == null)
                {
                    failure = StoreOperatingFailure.MissingTotals;
                    return false;
                }

                if (!closingTotals.IsValid)
                {
                    failure = StoreOperatingFailure.InvalidTotals;
                    return false;
                }

                Totals = new StoreSessionTotals(
                    closingTotals.grossSalesCents,
                    closingTotals.unitsSold,
                    closingTotals.transactionCount,
                    closingTotals.includedOperatingExpensesCents);
            }
            else if (State == StoreOperatingState.Closed &&
                     nextState == StoreOperatingState.Preparing)
            {
                Totals = null;
            }

            State = nextState;
            failure = StoreOperatingFailure.None;
            return true;
        }

        public StoreOperatingSnapshot CreateSnapshot()
        {
            StoreSessionTotals totalsCopy = Totals == null
                ? null
                : new StoreSessionTotals(
                    Totals.grossSalesCents,
                    Totals.unitsSold,
                    Totals.transactionCount,
                    Totals.includedOperatingExpensesCents);
            return new StoreOperatingSnapshot(
                SessionId,
                State,
                totalsCopy != null,
                totalsCopy);
        }

        public static bool TryRestore(
            StoreOperatingSnapshot snapshot,
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

            if (snapshot.hasResult != (snapshot.totals != null))
            {
                error = "Store operating result flag and totals disagree.";
                return false;
            }

            if (snapshot.totals != null && !snapshot.totals.IsValid)
            {
                error = "Store operating totals are invalid.";
                return false;
            }

            if (snapshot.state == StoreOperatingState.ClosedWithResultPending &&
                snapshot.totals == null)
            {
                error = "Closed-with-result-pending state requires totals.";
                return false;
            }

            if ((snapshot.state == StoreOperatingState.Preparing ||
                 snapshot.state == StoreOperatingState.Open ||
                 snapshot.state == StoreOperatingState.Closing) &&
                snapshot.totals != null)
            {
                error = $"Store state '{snapshot.state}' cannot retain prior result totals.";
                return false;
            }

            StoreSessionTotals totalsCopy = snapshot.totals == null
                ? null
                : new StoreSessionTotals(
                    snapshot.totals.grossSalesCents,
                    snapshot.totals.unitsSold,
                    snapshot.totals.transactionCount,
                    snapshot.totals.includedOperatingExpensesCents);
            session = new StoreOperatingSession(
                snapshot.sessionId,
                snapshot.state,
                totalsCopy);
            error = null;
            return true;
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
