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
                        return CreateOpenPrompt();

                    case StoreOperatingState.Preparing:
                        return CreateOpenPrompt();

                    case StoreOperatingState.Open:
                        return new FirstStoreWorldInteractionPrompt("E", "Begin closing");

                    case StoreOperatingState.Closing:
                        return new FirstStoreWorldInteractionPrompt(
                            "E",
                            "Closing store",
                            "new customer intake stopped; report posts automatically");

                    case StoreOperatingState.ClosedWithResultPending:
                        return new FirstStoreWorldInteractionPrompt(
                            "E",
                            "Posting shift report");

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
                        operatingController.TryOpenStore,
                        "Store cannot open right now.",
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
                    error = operatingController.TryGetFirstFinalCloseBlocker(
                        out string closingBlocker)
                        ? closingBlocker
                        : "The shift report is posting automatically.";
                    return false;

                case StoreOperatingState.ClosedWithResultPending:
                    error = "The shift report is posting automatically.";
                    return false;

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

        private FirstStoreWorldInteractionPrompt CreateOpenPrompt()
        {
            if (operatingController.TryGetFirstOpenBlocker(out string blocker))
            {
                return new FirstStoreWorldInteractionPrompt("E", "Open store", blocker);
            }

            return new FirstStoreWorldInteractionPrompt("E", "Open store", "ready");
        }

        private static bool TryOperatingAction(
            OperatingAction action,
            string genericError,
            out string error)
        {
            if (action(out error))
            {
                error = null;
                return true;
            }

            error = string.IsNullOrWhiteSpace(error) ? genericError : error;
            return false;
        }

    }
}
