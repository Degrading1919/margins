using UnityEngine;

namespace Margins
{
    /// <summary>
    /// Development-only derived prompt and objective presentation.
    /// It owns no gameplay state and is intentionally absent from snapshots.
    /// </summary>
    public sealed class FirstStorePromptPresenter : MonoBehaviour
    {
        [SerializeField] private FirstStoreInteractionController interaction;
        [SerializeField] private FixturePlacementController fixturePlacement;
        [SerializeField] private PlaceableFixtureComponent[] requiredFixtures;
        [SerializeField] private DeliveryBoxComponent delivery;
        [SerializeField] private CheckoutStationComponent checkout;
        [SerializeField] private StagedCheckoutInteractionComponent stagedCheckout;
        [SerializeField] private CleaningTaskComponent cleaning;
        [SerializeField] private StoreOperatingController store;

        public string CurrentPromptText =>
            interaction != null && interaction.IsWorldInteractionEnabled
                ? interaction.CurrentPromptText
                : string.Empty;

        public string CurrentObjectiveText => DeriveCurrentObjective();

        public bool TryValidateConfiguration(out string error)
        {
            if (interaction == null || fixturePlacement == null || delivery == null ||
                checkout == null || stagedCheckout == null || cleaning == null ||
                store == null || requiredFixtures == null || requiredFixtures.Length == 0)
            {
                error = "First-store prompt presentation requires explicit loop references.";
                return false;
            }

            for (int index = 0; index < requiredFixtures.Length; index++)
            {
                if (requiredFixtures[index] == null)
                {
                    error = "First-store prompt presentation has a missing required fixture.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public string DeriveCurrentObjective()
        {
            if (!TryValidateConfiguration(out _))
            {
                return "Resolve first-store validation configuration";
            }

            for (int index = 0; index < requiredFixtures.Length; index++)
            {
                if (!fixturePlacement.IsPlaced(
                        requiredFixtures[index].StableFixtureInstanceId))
                {
                    return "Place fixtures";
                }
            }

            if (!delivery.IsOpen)
            {
                return "Open delivery";
            }

            if (checkout.CompletedTransactionCount == 0 && !checkout.HasSellableStock)
            {
                return "Stock a product";
            }

            if (store.State == StoreOperatingState.ClosedWithResultPending)
            {
                return "Review result";
            }

            if ((store.State == StoreOperatingState.Closed ||
                 store.State == StoreOperatingState.Preparing) &&
                (!stagedCheckout.AllBasketsComplete || !cleaning.IsComplete))
            {
                return "Open store";
            }

            if (!stagedCheckout.AllBasketsComplete)
            {
                return "Complete staged checkout";
            }

            if (!cleaning.IsComplete)
            {
                return "Clean";
            }

            if (store.State == StoreOperatingState.Open ||
                store.State == StoreOperatingState.Closing)
            {
                return "Close";
            }

            return "Save and reload";
        }

        private void OnGUI()
        {
            if (interaction == null || !interaction.IsWorldInteractionEnabled)
            {
                return;
            }

            const float width = 720f;
            float left = Mathf.Max(12f, (Screen.width - width) * 0.5f);
            float top = Screen.height - 96f;
            GUI.Box(new Rect(left, top, width, 78f), string.Empty);
            GUI.Label(
                new Rect(left + 14f, top + 10f, width - 28f, 24f),
                $"Objective — {CurrentObjectiveText}");

            string prompt = CurrentPromptText;
            GUI.Label(
                new Rect(left + 14f, top + 40f, width - 28f, 24f),
                string.IsNullOrEmpty(prompt)
                    ? "Center the view on an interaction target"
                    : prompt);
        }
    }
}
