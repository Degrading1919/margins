// Draft implementation — Unity verification pending
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
        [SerializeField] private string[] requiredFixtureInstanceIds;

        public StoreOperatingSession Session { get; private set; }
        public StoreOperatingState State =>
            Session?.State ?? StoreOperatingState.Closed;
        public bool IsInitialized => Session != null;
        public FixturePlacementController FixturePlacement => fixturePlacement;
        public CheckoutStationComponent Checkout => checkout;
        public CleaningTaskComponent CleaningTask => cleaningTask;

        private void Start()
        {
            if (!TryInitialize(out string error))
            {
                Debug.LogError($"Store operating initialization failed: {error}", this);
            }
        }

        public bool TryValidateConfiguration(out string error)
        {
            if (!FirstStoreIdentifier.IsValid(stableSessionId))
            {
                error = "Store operating controller requires a valid session id.";
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

            if (stocking.InventoryComponent != checkout.InventoryComponent)
            {
                error =
                    "Stocking and checkout must reference the same inventory component.";
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

            if (!fixturePlacement.TryInitialize(out error))
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
            error = null;
            return true;
        }

        public bool TryBeginPreparation(out string error)
        {
            return TryTransition(
                StoreOperatingState.Preparing,
                null,
                out error);
        }

        public bool TryAbortPreparation(out string error)
        {
            return TryTransition(
                StoreOperatingState.Closed,
                null,
                out error);
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

            return TryTransition(StoreOperatingState.Open, null, out error);
        }

        public bool TryBeginClosing(out string error)
        {
            return TryTransition(
                StoreOperatingState.Closing,
                null,
                out error);
        }

        public bool TryFinishClosing(
            StoreSessionTotals totals,
            out string error)
        {
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

            return TryTransition(
                StoreOperatingState.ClosedWithResultPending,
                totals,
                out error);
        }

        public bool TryAcknowledgeResult(out string error)
        {
            return TryTransition(
                StoreOperatingState.Closed,
                null,
                out error);
        }

        public bool CanApplySnapshot(
            StoreOperatingSnapshot snapshot,
            out string error)
        {
            if (snapshot == null ||
                !string.Equals(
                    snapshot.sessionId,
                    stableSessionId,
                    System.StringComparison.Ordinal))
            {
                error =
                    $"Store operating snapshot does not match session '{stableSessionId}'.";
                return false;
            }

            return StoreOperatingSession.TryRestore(
                snapshot,
                out _,
                out error);
        }

        public bool TryApplySnapshot(
            StoreOperatingSnapshot snapshot,
            out string error)
        {
            if (!CanApplySnapshot(snapshot, out error))
            {
                return false;
            }

            return StoreOperatingSession.TryRestore(
                snapshot,
                out StoreOperatingSession restored,
                out error) &&
                   AssignRestored(restored);
        }

        private bool TryTransition(
            StoreOperatingState next,
            StoreSessionTotals totals,
            out string error)
        {
            if (Session == null)
            {
                error = "Store operating controller is not initialized.";
                return false;
            }

            if (!Session.TryTransition(next, totals, out StoreOperatingFailure failure))
            {
                error =
                    $"Store transition '{Session.State}' to '{next}' rejected ({failure}).";
                return false;
            }

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
