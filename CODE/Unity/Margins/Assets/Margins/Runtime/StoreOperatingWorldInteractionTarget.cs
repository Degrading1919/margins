using UnityEngine;

namespace Margins
{
    public sealed class StoreOperatingWorldInteractionTarget : MonoBehaviour, IFirstStoreWorldInteractionTarget
    {
        private delegate bool OperatingAction(out string error);

        [SerializeField] private string stableTargetId;
        [SerializeField] private StoreOperatingController operatingController;

        public string StableTargetId => stableTargetId;
        public FirstStoreWorldInteractionPriority Priority => FirstStoreWorldInteractionPriority.Operating;
        public bool IsAvailable =>
            FirstStoreIdentifier.IsValid(stableTargetId) &&
            operatingController != null &&
            operatingController.IsInitialized &&
            !operatingController.IsContinuousOperation;
        public FirstStoreWorldInteractionPrompt Prompt
        {
            get
            {
                if (operatingController == null || !operatingController.IsInitialized)
                {
                    return new FirstStoreWorldInteractionPrompt(
                        "E",
                        "Use store control",
                        "unavailable");
                }

                switch (operatingController.State)
                {
                    case StoreOperatingState.Closed:
                        return new FirstStoreWorldInteractionPrompt("E", "Begin preparation");

                    case StoreOperatingState.Preparing:
                        return CreatePreparingPrompt();

                    case StoreOperatingState.Open:
                        return new FirstStoreWorldInteractionPrompt("E", "Begin closing");

                    case StoreOperatingState.Closing:
                        return CreateClosingPrompt();

                    case StoreOperatingState.ClosedWithResultPending:
                        return CreateResultPrompt();

                    default:
                        return new FirstStoreWorldInteractionPrompt(
                            "E",
                            "Use store control",
                            "unavailable");
                }
            }
        }

        public bool TryPrimary(out string error)
        {
            if (!IsAvailable)
            {
                error = "Store control is unavailable.";
                return false;
            }

            switch (operatingController.State)
            {
                case StoreOperatingState.Closed:
                    return TryOperatingAction(
                        operatingController.TryBeginPreparation,
                        "Store preparation cannot begin right now.",
                        out error);

                case StoreOperatingState.Preparing:
                    if (operatingController.TryGetFirstOpenBlocker(out error))
                    {
                        return false;
                    }
                    return TryOperatingAction(
                        operatingController.TryOpenStore,
                        "Store opening cannot complete right now.",
                        out error);

                case StoreOperatingState.Open:
                    return TryOperatingAction(
                        operatingController.TryBeginClosing,
                        "Store closing cannot begin right now.",
                        out error);

                case StoreOperatingState.Closing:
                    if (operatingController.TryGetFirstFinalCloseBlocker(out error))
                    {
                        return false;
                    }
                    return TryOperatingAction(
                        operatingController.TryFinishClosing,
                        "Store closing cannot complete right now.",
                        out error);

                case StoreOperatingState.ClosedWithResultPending:
                    return TryOperatingAction(
                        operatingController.TryAcknowledgeResult,
                        "Store result cannot be acknowledged right now.",
                        out error);

                default:
                    error = "Store control is unavailable.";
                    return false;
            }
        }

        public bool TryCancel(out string error)
        {
            error = "This store control has no cancel action.";
            return false;
        }

        private FirstStoreWorldInteractionPrompt CreatePreparingPrompt()
        {
            if (operatingController.TryGetFirstOpenBlocker(out string blocker))
            {
                return new FirstStoreWorldInteractionPrompt("E", "Open store", blocker);
            }

            return new FirstStoreWorldInteractionPrompt("E", "Open store", "ready");
        }

        private FirstStoreWorldInteractionPrompt CreateClosingPrompt()
        {
            if (operatingController.TryGetFirstFinalCloseBlocker(out string blocker))
            {
                return new FirstStoreWorldInteractionPrompt("E", "Finalize closing", blocker);
            }

            return new FirstStoreWorldInteractionPrompt("E", "Finalize closing", "ready");
        }

        private FirstStoreWorldInteractionPrompt CreateResultPrompt()
        {
            StoreSessionTotals totals = operatingController.ResultTotals;
            if (totals == null)
            {
                return new FirstStoreWorldInteractionPrompt(
                    "E",
                    "Acknowledge result",
                    "result unavailable");
            }

            operatingController.TryGetResultCausalNote(out string note);
            string summary =
                $"Gross {FormatCents(totals.grossSalesCents)}; " +
                $"COGS {FormatCents(totals.costOfGoodsSoldCents)}; " +
                $"expenses {FormatCents(totals.includedOperatingExpensesCents)}; " +
                $"contribution {FormatCents(totals.contributionAfterCostOfGoodsCents)}; " +
                $"{totals.unitsSold} units; {totals.transactionCount} transactions";
            if (!string.IsNullOrWhiteSpace(note))
            {
                summary = $"{summary}. {note}";
            }

            return new FirstStoreWorldInteractionPrompt("E", "Acknowledge result", summary);
        }

        private static bool TryOperatingAction(
            OperatingAction action,
            string genericError,
            out string error)
        {
            if (action(out _))
            {
                error = null;
                return true;
            }

            error = genericError;
            return false;
        }

        private static string FormatCents(long cents)
        {
            bool isNegative = cents < 0;
            ulong absoluteCents = isNegative
                ? (ulong)(-(cents + 1)) + 1UL
                : (ulong)cents;
            return isNegative
                ? $"-${absoluteCents / 100}.{absoluteCents % 100:00}"
                : $"${absoluteCents / 100}.{absoluteCents % 100:00}";
        }
    }
}
