using System;
using System.Collections.Generic;
using UnityEngine;

namespace Margins
{
    public sealed class StoreOperatingController : MonoBehaviour
    {
        [SerializeField] private string stableSessionId;
        [SerializeField] private FixturePlacementController fixturePlacement;
        [SerializeField] private StockingController stocking;
        [SerializeField] private CheckoutStationComponent checkout;
        [SerializeField] private CleaningTaskComponent cleaningTask;
        [SerializeField] private StoreCustomerFlowController customerFlow;
        [SerializeField] private string[] requiredFixtureInstanceIds;
        [SerializeField, Min(0)] private int includedOperatingExpensesCents;
        [SerializeField] private bool continuousOperation;

        private long livePayrollCents;

        public event Action<StoreSessionTotals> ReportPosted;

        internal StoreOperatingSession Session { get; private set; }
        public StoreOperatingState State =>
            Session?.State ?? StoreOperatingState.Closed;
        public string StableSessionId => stableSessionId;
        public StoreSessionTotals ResultTotals => Session?.Totals;
        public bool IsContinuousOperation => continuousOperation;
        public bool IsInitialized => Session != null;
        public FixturePlacementController FixturePlacement => fixturePlacement;
        public StockingController Stocking => stocking;
        public CheckoutStationComponent Checkout => checkout;
        public CleaningTaskComponent CleaningTask => cleaningTask;
        public StoreCustomerFlowController CustomerFlow => customerFlow;
        public int IncludedOperatingExpensesCents =>
            includedOperatingExpensesCents;
        public long LivePayrollCents => livePayrollCents;

        public StoreSessionTotals CurrentTotals
        {
            get
            {
                if (Session?.Totals != null)
                {
                    return Session.Totals;
                }

                return checkout?.TransactionLedger != null &&
                       StoreSessionTotals.TryCreateFromLedger(
                           checkout.TransactionLedger,
                           checked(includedOperatingExpensesCents + livePayrollCents),
                           out StoreSessionTotals live,
                           out _)
                    ? live
                    : null;
            }
        }

        public bool TrySetLivePayrollCents(long payrollCents, out string error)
        {
            if (payrollCents < 0)
            {
                error = "Live payroll cannot be negative.";
                return false;
            }

            try
            {
                _ = checked(includedOperatingExpensesCents + payrollCents);
            }
            catch (OverflowException)
            {
                error = "Live operating expenses exceed supported cent storage.";
                return false;
            }

            livePayrollCents = payrollCents;
            error = null;
            return true;
        }

        private void Start()
        {
            if (!TryInitialize(out string error))
            {
                Debug.LogError($"Store operating initialization failed: {error}", this);
            }
        }

        private void Update()
        {
            if (State == StoreOperatingState.Closing &&
                !TryGetFirstFinalCloseBlocker(out _))
            {
                TryFinishClosing(out _);
            }
            else if (State == StoreOperatingState.ClosedWithResultPending)
            {
                TryPostPendingReport(out _);
            }
        }

        public bool TryValidateConfiguration(out string error)
        {
            if (!FirstStoreIdentifier.IsValid(stableSessionId) ||
                includedOperatingExpensesCents < 0)
            {
                error =
                    "Store operating controller requires a valid session id and nonnegative expenses.";
                return false;
            }

            if (fixturePlacement == null ||
                stocking == null ||
                checkout == null ||
                cleaningTask == null)
            {
                error =
                    "Store operating controller requires explicit fixture, stocking, checkout, and cleaning references.";
                return false;
            }

            if (!fixturePlacement.IsInitialized)
            {
                error = "Fixture placement controller is not initialized.";
                return false;
            }

            if (stocking.InventoryComponent != checkout.InventoryComponent ||
                stocking.PhysicalUnits == null ||
                stocking.PhysicalUnits != checkout.PhysicalUnits)
            {
                error =
                    "Stocking and checkout must share inventory and physical-unit configuration.";
                return false;
            }

            if (requiredFixtureInstanceIds == null ||
                requiredFixtureInstanceIds.Length == 0)
            {
                error =
                    "Store operating controller requires at least one required fixture id.";
                return false;
            }

            HashSet<string> fixtureIds = new(StringComparer.Ordinal);
            foreach (string fixtureId in requiredFixtureInstanceIds)
            {
                if (!FirstStoreIdentifier.IsValid(fixtureId) ||
                    !fixtureIds.Add(fixtureId) ||
                    !fixturePlacement.HasConfiguredFixture(fixtureId))
                {
                    error =
                        $"Required fixture id '{fixtureId}' is invalid, duplicated, or unconfigured.";
                    return false;
                }
            }

            if (!stocking.TryValidateConfiguration(out error) ||
                !checkout.TryValidateConfiguration(out error) ||
                !cleaningTask.TryValidateConfiguration(out error))
            {
                return false;
            }

            foreach (string productId in checkout.ConfiguredProductIds)
            {
                if (!checkout.TryGetShelfLocation(
                        productId,
                        out string checkoutShelf) ||
                    !stocking.TryGetShelfLocation(
                        productId,
                        out string stockingShelf) ||
                    !string.Equals(
                        checkoutShelf,
                        stockingShelf,
                        StringComparison.Ordinal))
                {
                    error =
                        $"Checkout and stocking shelf mappings disagree for '{productId}'.";
                    return false;
                }
            }

            if (customerFlow != null &&
                (customerFlow.StoreOperating != this ||
                 customerFlow.Checkout != checkout ||
                 customerFlow.PhysicalUnits != stocking.PhysicalUnits))
            {
                error =
                    "Store customer flow does not use this operating controller's checkout and physical units.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryInitialize(out string error)
        {
            if (Session != null)
            {
                error = null;
                return true;
            }

            if (fixturePlacement == null ||
                stocking == null ||
                checkout == null)
            {
                error =
                    "Store operating initialization requires fixture, stocking, and checkout references.";
                return false;
            }

            if (!fixturePlacement.TryBindOperatingController(this, out error) ||
                !fixturePlacement.TryInitialize(out error))
            {
                error = $"Store operating could not initialize fixture placement: {error}";
                return false;
            }

            if (!stocking.TryInitializeDependencies(out error))
            {
                error = $"Store operating could not initialize stocking: {error}";
                return false;
            }

            if (!checkout.TryInitializeDependencies(out error))
            {
                error = $"Store operating could not initialize checkout: {error}";
                return false;
            }

            if (!TryValidateConfiguration(out error))
            {
                return false;
            }

            if (!StoreOperatingSession.TryCreate(
                    stableSessionId,
                    out StoreOperatingSession session,
                    out StoreOperatingFailure failure))
            {
                error = $"Store operating domain rejected initialization ({failure}).";
                return false;
            }

            Session = session;
            if (continuousOperation)
            {
                if (!Session.TryTransition(
                        StoreOperatingState.Preparing,
                        out failure) ||
                    !Session.TryTransition(
                        StoreOperatingState.Open,
                        out failure))
                {
                    Session = null;
                    error = $"Continuous store startup was rejected ({failure}).";
                    return false;
                }
            }
            error = null;
            return true;
        }

        public bool TryBeginPreparation(out string error)
        {
            return TryTransition(StoreOperatingState.Preparing, out error);
        }

        public bool TryAbortPreparation(out string error)
        {
            return TryTransition(StoreOperatingState.Closed, out error);
        }

        public bool TryOpenStore(out string error)
        {
            if (Session == null)
            {
                error = "Store operating controller is not initialized.";
                return false;
            }

            foreach (string fixtureId in requiredFixtureInstanceIds)
            {
                if (!fixturePlacement.IsPlaced(fixtureId))
                {
                    error =
                        $"Required fixture '{fixtureId}' must be placed before opening.";
                    return false;
                }
            }

            if (!checkout.HasSellableStock)
            {
                error = "At least one configured checkout product must be shelved.";
                return false;
            }

            if (stocking.HasHeldUnit)
            {
                error = "Return or stock the held unit before opening.";
                return false;
            }

            if (State == StoreOperatingState.Closed)
            {
                if (!TryTransition(StoreOperatingState.Preparing, out error))
                {
                    return false;
                }
            }

            if (State != StoreOperatingState.Preparing)
            {
                error = "The store can only open while closed.";
                return false;
            }

            return TryTransition(StoreOperatingState.Open, out error);
        }

        public bool TryGetFirstOpenBlocker(out string blocker)
        {
            if (Session == null)
            {
                blocker = "Store controls are not ready.";
                return true;
            }

            foreach (string fixtureId in requiredFixtureInstanceIds)
            {
                if (!fixturePlacement.IsPlaced(fixtureId))
                {
                    blocker = "Place the required fixture before opening.";
                    return true;
                }
            }

            if (!checkout.HasSellableStock)
            {
                blocker = "Stock a checkout product before opening.";
                return true;
            }

            if (stocking.HasHeldUnit)
            {
                blocker = "Return or stock the held product before opening.";
                return true;
            }

            blocker = null;
            return false;
        }

        public bool TryBeginClosing(out string error)
        {
            return TryTransition(StoreOperatingState.Closing, out error);
        }

        public bool TryFinishClosing(out string error)
        {
            if (Session == null)
            {
                error = "Store operating controller is not initialized.";
                return false;
            }

            if (customerFlow != null && customerFlow.HasCustomersInStore)
            {
                error = "Wait for all customers to complete checkout or leave before closing.";
                return false;
            }

            if (checkout.HasActiveIncompleteSession)
            {
                error = "Complete or clear the active checkout session before closing.";
                return false;
            }

            if (stocking.HasHeldUnit)
            {
                error = "Return or stock the held unit before closing.";
                return false;
            }

            if (!cleaningTask.IsComplete)
            {
                error = "Complete the required cleaning task before closing.";
                return false;
            }

            if (!Session.TryFinalizeClosing(
                    checkout.TransactionLedger,
                    includedOperatingExpensesCents,
                    out StoreOperatingFailure failure))
            {
                error = $"Store closing totals were rejected ({failure}).";
                return false;
            }

            return TryPostPendingReport(out error);
        }

        public bool TryGetFirstFinalCloseBlocker(out string blocker)
        {
            if (Session == null)
            {
                blocker = "Store controls are not ready.";
                return true;
            }

            if (State != StoreOperatingState.Closing)
            {
                blocker = "Begin closing before finalizing the store.";
                return true;
            }

            if (checkout.HasActiveIncompleteSession)
            {
                blocker = "Complete or clear the active checkout before closing.";
                return true;
            }

            if (customerFlow != null && customerFlow.HasCustomersInStore)
            {
                blocker = "Wait for all customers to complete checkout or leave.";
                return true;
            }

            if (stocking.HasHeldUnit)
            {
                blocker = "Return or stock the held product before closing.";
                return true;
            }

            if (!cleaningTask.IsComplete)
            {
                blocker = "Complete the required cleaning task before closing.";
                return true;
            }

            blocker = null;
            return false;
        }

        public bool TryGetResultCausalNote(out string note)
        {
            StoreSessionTotals totals = ResultTotals;
            if ((State != StoreOperatingState.ClosedWithResultPending &&
                 State != StoreOperatingState.Closed) || totals == null)
            {
                note = null;
                return false;
            }

            note = totals.transactionCount == 0
                ? "No completed sales were included in this result."
                : $"{totals.transactionCount} completed sale(s) and {totals.unitsSold} unit(s) are included.";
            return true;
        }

        public bool TryAcknowledgeResult(out string error)
        {
            return TryPostPendingReport(out error);
        }

        public bool IsFixtureModificationRestricted(string fixtureInstanceId)
        {
            if (continuousOperation)
            {
                return false;
            }

            return State == StoreOperatingState.Open ||
                   State == StoreOperatingState.Closing;
        }

        public bool CanApplySnapshot(
            StoreOperatingSnapshot snapshot,
            CompletedTransactionLedger transactionLedger,
            out string error)
        {
            if (snapshot == null ||
                !string.Equals(
                    snapshot.sessionId,
                    stableSessionId,
                    StringComparison.Ordinal))
            {
                error =
                    $"Store operating snapshot does not match session '{stableSessionId}'.";
                return false;
            }

            return StoreOperatingSession.TryRestore(
                snapshot,
                transactionLedger,
                out _,
                out error);
        }

        public bool TryApplySnapshot(
            StoreOperatingSnapshot snapshot,
            CompletedTransactionLedger transactionLedger,
            out string error)
        {
            if (!CanApplySnapshot(snapshot, transactionLedger, out error))
            {
                return false;
            }

            return StoreOperatingSession.TryRestore(
                       snapshot,
                       transactionLedger,
                       out StoreOperatingSession restored,
                       out error) &&
                   AssignRestored(restored);
        }

        private bool TryTransition(
            StoreOperatingState next,
            out string error)
        {
            if (Session == null)
            {
                error = "Store operating controller is not initialized.";
                return false;
            }

            if (!Session.TryTransition(next, out StoreOperatingFailure failure))
            {
                error =
                    $"Store transition '{Session.State}' to '{next}' rejected ({failure}).";
                return false;
            }

            error = null;
            return true;
        }

        private bool TryPostPendingReport(out string error)
        {
            if (Session == null ||
                State != StoreOperatingState.ClosedWithResultPending ||
                ResultTotals == null)
            {
                error = "No completed store report is waiting to post.";
                return false;
            }

            StoreSessionTotals report = ResultTotals;
            if (!TryTransition(StoreOperatingState.Closed, out error))
            {
                return false;
            }

            ReportPosted?.Invoke(report);
            error = null;
            return true;
        }

        private bool AssignRestored(StoreOperatingSession restored)
        {
            Session = restored;
            return true;
        }
    }
}
