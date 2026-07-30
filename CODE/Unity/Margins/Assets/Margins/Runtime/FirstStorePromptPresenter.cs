using System;
using UnityEngine;

namespace Margins
{
    public enum FirstStoreObjectiveKind
    {
        ClockIn,
        PlaceCheckout,
        OpenDelivery,
        TakeCola,
        TakeChips,
        StockHeldProduct,
        OpenStore,
        CompleteCheckout,
        CleanSpill,
        BeginClosing,
        FinalizeClosing,
        ReviewResult,
        Complete
    }

    /// <summary>
    /// Derived first-shift guidance. It owns no gameplay state and is absent from saves.
    /// </summary>
    public sealed class FirstStorePromptPresenter : MonoBehaviour
    {
        private static readonly Color Ink = new(0.93f, 0.95f, 0.94f, 1f);
        private static readonly Color MutedInk = new(0.67f, 0.72f, 0.71f, 1f);
        private static readonly Color Night = new(0.035f, 0.055f, 0.075f, 0.94f);
        private static readonly Color NightSoft = new(0.055f, 0.085f, 0.105f, 0.9f);
        private static readonly Color Teal = new(0.12f, 0.78f, 0.68f, 1f);
        private static readonly Color Amber = new(1f, 0.58f, 0.2f, 1f);
        private static readonly Color Error = new(0.95f, 0.28f, 0.22f, 1f);

        [SerializeField] private string storeDisplayName = "MILE 7 MARKET";
        [SerializeField] private FirstStoreInteractionController interaction;
        [SerializeField] private FixturePlacementController fixturePlacement;
        [SerializeField] private PlaceableFixtureComponent[] requiredFixtures;
        [SerializeField] private DeliveryBoxComponent delivery;
        [SerializeField] private StockingController stocking;
        [SerializeField] private CheckoutStationComponent checkout;
        [SerializeField] private StagedCheckoutInteractionComponent stagedCheckout;
        [SerializeField] private CleaningTaskComponent cleaning;
        [SerializeField] private StoreOperatingController store;
        [SerializeField] private PortfolioProgressionController portfolio;
        [SerializeField] private ProductDefinition colaProduct;
        [SerializeField] private ProductDefinition chipsProduct;
        [Header("Objective world targets")]
        [SerializeField] private Transform storeControlTarget;
        [SerializeField] private Transform fixtureHandleTarget;
        [SerializeField] private Transform deliveryTarget;
        [SerializeField] private Transform colaDeliveryTarget;
        [SerializeField] private Transform chipsDeliveryTarget;
        [SerializeField] private Transform colaShelfTarget;
        [SerializeField] private Transform chipsShelfTarget;
        [SerializeField] private Transform checkoutTarget;
        [SerializeField] private Transform cleaningTarget;

        private GUIStyle titleStyle;
        private GUIStyle eyebrowStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle objectiveStyle;
        private GUIStyle keyStyle;
        private GUIStyle centerStyle;
        private GUIStyle resultValueStyle;
        private float shownAt;
        private float feedbackUntil;
        private string feedbackText;
        private bool feedbackSucceeded;

        public string CurrentPromptText =>
            interaction != null && interaction.IsWorldInteractionEnabled
                ? interaction.CurrentPromptText
                : string.Empty;

        public FirstStoreObjectiveKind CurrentObjectiveKind => DeriveObjectiveKind();
        public string CurrentObjectiveText => DescribeObjective(CurrentObjectiveKind);
        public int CurrentObjectiveStep => GetObjectiveStep(CurrentObjectiveKind);
        public int ObjectiveStepCount => 8;
        public Transform CurrentObjectiveTarget => ResolveObjectiveTarget(CurrentObjectiveKind);

        private void OnEnable()
        {
            shownAt = Time.unscaledTime;
            if (interaction != null)
            {
                interaction.InteractionResolved += HandleInteractionResolved;
            }
        }

        private void OnDisable()
        {
            if (interaction != null)
            {
                interaction.InteractionResolved -= HandleInteractionResolved;
            }
        }

        public bool TryValidateConfiguration(out string error)
        {
            if (interaction == null || fixturePlacement == null || delivery == null ||
                stocking == null || checkout == null || stagedCheckout == null ||
                cleaning == null || store == null || colaProduct == null ||
                chipsProduct == null || requiredFixtures == null ||
                requiredFixtures.Length == 0)
            {
                error = "First-store guidance requires explicit loop and product references.";
                return false;
            }

            for (int index = 0; index < requiredFixtures.Length; index++)
            {
                if (requiredFixtures[index] == null)
                {
                    error = "First-store guidance has a missing required fixture.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public string DeriveCurrentObjective()
        {
            return TryValidateConfiguration(out _)
                ? DescribeObjective(DeriveObjectiveKind())
                : "Resolve first-store configuration";
        }

        private FirstStoreObjectiveKind DeriveObjectiveKind()
        {
            if (!TryValidateConfiguration(out _))
            {
                return FirstStoreObjectiveKind.ClockIn;
            }

            if (store.IsContinuousOperation)
            {
                return FirstStoreObjectiveKind.Complete;
            }

            bool saleComplete = checkout.CompletedTransactionCount > 0;
            if (store.State == StoreOperatingState.ClosedWithResultPending)
            {
                return FirstStoreObjectiveKind.ReviewResult;
            }

            if (store.State == StoreOperatingState.Closing)
            {
                return FirstStoreObjectiveKind.FinalizeClosing;
            }

            if (store.State == StoreOperatingState.Closed &&
                saleComplete && cleaning.IsComplete)
            {
                return FirstStoreObjectiveKind.Complete;
            }

            if (store.State == StoreOperatingState.Closed)
            {
                return FirstStoreObjectiveKind.ClockIn;
            }

            for (int index = 0; index < requiredFixtures.Length; index++)
            {
                if (!fixturePlacement.IsPlaced(
                        requiredFixtures[index].StableFixtureInstanceId))
                {
                    return FirstStoreObjectiveKind.PlaceCheckout;
                }
            }

            if (!delivery.IsOpen)
            {
                return FirstStoreObjectiveKind.OpenDelivery;
            }

            if (stocking.HeldPhysicalUnit != null)
            {
                return FirstStoreObjectiveKind.StockHeldProduct;
            }

            if (!saleComplete && !HasStocked(colaProduct))
            {
                return FirstStoreObjectiveKind.TakeCola;
            }

            if (!saleComplete && !HasStocked(chipsProduct))
            {
                return FirstStoreObjectiveKind.TakeChips;
            }

            if (store.State == StoreOperatingState.Preparing)
            {
                return FirstStoreObjectiveKind.OpenStore;
            }

            if (!stagedCheckout.AllBasketsComplete)
            {
                return FirstStoreObjectiveKind.CompleteCheckout;
            }

            if (!cleaning.IsComplete)
            {
                return FirstStoreObjectiveKind.CleanSpill;
            }

            if (store.State == StoreOperatingState.Open)
            {
                return FirstStoreObjectiveKind.BeginClosing;
            }

            return FirstStoreObjectiveKind.Complete;
        }

        private string DescribeObjective(FirstStoreObjectiveKind objective)
        {
            return objective switch
            {
                FirstStoreObjectiveKind.ClockIn =>
                    "Enter the store and clock in at the front panel",
                FirstStoreObjectiveKind.PlaceCheckout =>
                    "Place the checkout counter on the sales floor",
                FirstStoreObjectiveKind.OpenDelivery =>
                    "Open the starter delivery in Receiving",
                FirstStoreObjectiveKind.TakeCola =>
                    "Take a cola case item from the delivery",
                FirstStoreObjectiveKind.TakeChips =>
                    "Take a chips case item from the delivery",
                FirstStoreObjectiveKind.StockHeldProduct =>
                    $"Stock {HeldProductName()} on the highlighted shelf",
                FirstStoreObjectiveKind.OpenStore =>
                    "Switch the front panel to OPEN",
                FirstStoreObjectiveKind.CompleteCheckout =>
                    "Serve the waiting basket at Checkout 01",
                FirstStoreObjectiveKind.CleanSpill =>
                    "Clean the spill before closing",
                FirstStoreObjectiveKind.BeginClosing =>
                    "Return to the front panel and begin closing",
                FirstStoreObjectiveKind.FinalizeClosing =>
                    "Finish closing and post the shift result",
                FirstStoreObjectiveKind.ReviewResult =>
                    "Review the shift result at the front panel",
                _ => "First shift complete — explore or press F5 to save"
            };
        }

        private int GetObjectiveStep(FirstStoreObjectiveKind objective)
        {
            return objective switch
            {
                FirstStoreObjectiveKind.ClockIn => 1,
                FirstStoreObjectiveKind.PlaceCheckout => 2,
                FirstStoreObjectiveKind.OpenDelivery => 3,
                FirstStoreObjectiveKind.TakeCola or
                FirstStoreObjectiveKind.TakeChips or
                FirstStoreObjectiveKind.StockHeldProduct => 4,
                FirstStoreObjectiveKind.OpenStore => 5,
                FirstStoreObjectiveKind.CompleteCheckout => 6,
                FirstStoreObjectiveKind.CleanSpill => 7,
                _ => 8
            };
        }

        private Transform ResolveObjectiveTarget(FirstStoreObjectiveKind objective)
        {
            return objective switch
            {
                FirstStoreObjectiveKind.ClockIn or
                FirstStoreObjectiveKind.OpenStore or
                FirstStoreObjectiveKind.BeginClosing or
                FirstStoreObjectiveKind.FinalizeClosing or
                FirstStoreObjectiveKind.ReviewResult => storeControlTarget,
                FirstStoreObjectiveKind.PlaceCheckout => fixtureHandleTarget,
                FirstStoreObjectiveKind.OpenDelivery => deliveryTarget,
                FirstStoreObjectiveKind.TakeCola => colaDeliveryTarget,
                FirstStoreObjectiveKind.TakeChips => chipsDeliveryTarget,
                FirstStoreObjectiveKind.StockHeldProduct =>
                    stocking?.HeldPhysicalUnit?.Definition == chipsProduct
                        ? chipsShelfTarget
                        : colaShelfTarget,
                FirstStoreObjectiveKind.CompleteCheckout => checkoutTarget,
                FirstStoreObjectiveKind.CleanSpill => cleaningTarget,
                _ => null
            };
        }

        private bool HasStocked(ProductDefinition product)
        {
            if (product == null)
            {
                return false;
            }

            if (checkout.CompletedTransactionCount > 0)
            {
                return true;
            }

            return checkout.TryGetShelfLocation(
                       product.StableProductId,
                       out string locationId) &&
                   delivery.InventoryComponent != null &&
                   delivery.InventoryComponent.IsInitialized &&
                   delivery.InventoryComponent.Inventory.GetQuantity(
                       locationId,
                       product.StableProductId) > 0;
        }

        private string HeldProductName()
        {
            ProductDefinition definition = stocking?.HeldPhysicalUnit?.Definition;
            return definition == null || string.IsNullOrWhiteSpace(definition.DisplayName)
                ? "the product"
                : definition.DisplayName;
        }

        private void HandleInteractionResolved(FirstStoreInteractionFeedback feedback)
        {
            feedbackSucceeded = feedback.Succeeded;
            feedbackText = feedback.Message;
            feedbackUntil = Time.unscaledTime + (feedback.Succeeded ? 1.35f : 3.25f);
        }

        private void OnGUI()
        {
            if (interaction == null || !interaction.IsWorldInteractionEnabled)
            {
                return;
            }

            EnsureStyles();
            float scale = Mathf.Max(
                0.62f,
                Mathf.Min(Screen.width / 1920f, Screen.height / 1080f));
            Matrix4x4 priorMatrix = GUI.matrix;
            Color priorColor = GUI.color;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            DrawStoreBadge();
            if (store != null && store.IsContinuousOperation)
            {
                DrawContinuousStatus(width);
                DrawContinuousAction(width, height);
            }
            else
            {
                DrawChecklist(width);
                DrawObjective(width, height);
            }
            DrawCrosshair(width, height);
            DrawHeldItem(width, height);
            DrawFeedback(width, height);
            DrawIntroControls(height);
            if (store != null && !store.IsContinuousOperation &&
                store.State == StoreOperatingState.ClosedWithResultPending)
            {
                DrawResult(width, height);
            }

            GUI.matrix = priorMatrix;
            GUI.color = priorColor;
        }

        private void DrawStoreBadge()
        {
            Rect panel = new(32f, 30f, 410f, 102f);
            DrawPanel(panel, Night);
            DrawPanel(new Rect(panel.x, panel.y, 7f, panel.height), Teal);
            GUI.Label(new Rect(54f, 43f, 365f, 30f), storeDisplayName, titleStyle);
            GUI.Label(
                new Rect(54f, 80f, 365f, 24f),
                store != null && store.IsContinuousOperation
                    ? $"LIVE BUSINESS  /  {FriendlyStoreState()}"
                    : $"FIRST SHIFT  /  {FriendlyStoreState()}",
                eyebrowStyle);
        }

        private void DrawContinuousStatus(float width)
        {
            StoreSessionTotals totals = store?.CurrentTotals;
            PortfolioProgressionSnapshot company =
                portfolio?.Progression?.CreateSnapshot();
            long cash = company?.cashCents ?? 0;
            Rect panel = new(width - 442f, 30f, 410f, 196f);
            DrawPanel(panel, Night);
            DrawPanel(new Rect(panel.x, panel.y, 7f, panel.height), Teal);
            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 16f, 350f, 24f),
                $"DAY {company?.currentDay ?? 1}  /  LIVE LEDGER",
                eyebrowStyle);
            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 49f, 350f, 32f),
                $"Cash  {FormatCents(cash)}",
                titleStyle);
            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 90f, 360f, 25f),
                $"Sales {FormatCents(totals?.grossSalesCents ?? 0)}   •   " +
                $"COGS {FormatCents(totals?.costOfGoodsSoldCents ?? 0)}",
                bodyStyle);
            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 123f, 360f, 25f),
                $"Rent/overhead {FormatCents(totals?.includedOperatingExpensesCents ?? 0)}   •   " +
                $"Stock {CountDetailedInventory()} units",
                smallStyle);
            string condition = cleaning != null && cleaning.NeedsCleaning
                ? "Store condition: spill needs attention"
                : "Store condition: clean";
            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 157f, 360f, 24f),
                condition,
                cleaning != null && cleaning.NeedsCleaning ? bodyStyle : smallStyle);
        }

        private void DrawContinuousAction(float width, float height)
        {
            Rect panel = new((width - 920f) * 0.5f, height - 142f, 920f, 104f);
            DrawPanel(panel, Night);
            DrawPanel(new Rect(panel.x, panel.y, 8f, panel.height), Teal);
            FirstStoreWorldInteractionPrompt prompt = interaction.CurrentPrompt;
            string headline = prompt == null
                ? "Operate freely — receive, stock, serve, clean when needed, or manage the company"
                : prompt.Action;
            GUI.Label(
                new Rect(panel.x + 28f, panel.y + 17f, 850f, 32f),
                headline,
                objectiveStyle);
            if (prompt == null)
            {
                GUI.Label(
                    new Rect(panel.x + 28f, panel.y + 59f, 850f, 24f),
                    "Tab company management   •   Escape game menu   •   F5 quick save   •   F9 quick load",
                    smallStyle);
                return;
            }

            DrawPanel(new Rect(panel.x + 28f, panel.y + 57f, 44f, 28f), Teal);
            GUI.Label(new Rect(panel.x + 28f, panel.y + 58f, 44f, 25f), prompt.Input, keyStyle);
            GUI.Label(
                new Rect(panel.x + 86f, panel.y + 58f, 780f, 25f),
                string.IsNullOrWhiteSpace(prompt.StateOrBlocker)
                    ? "Ready"
                    : prompt.StateOrBlocker,
                smallStyle);
        }

        private int CountDetailedInventory()
        {
            FirstStoreInventory inventory = delivery?.InventoryComponent?.Inventory;
            if (inventory == null || colaProduct == null || chipsProduct == null)
            {
                return 0;
            }

            return inventory.GetTotalQuantity(colaProduct.StableProductId) +
                   inventory.GetTotalQuantity(chipsProduct.StableProductId);
        }

        private void DrawChecklist(float width)
        {
            Rect panel = new(width - 402f, 30f, 370f, 278f);
            DrawPanel(panel, Night);
            GUI.Label(new Rect(panel.x + 22f, panel.y + 17f, 320f, 28f), "SHIFT PLAN", eyebrowStyle);

            bool saleComplete = checkout != null && checkout.CompletedTransactionCount > 0;
            bool opened = saleComplete ||
                          store.State == StoreOperatingState.Open ||
                          store.State == StoreOperatingState.Closing ||
                          store.State == StoreOperatingState.ClosedWithResultPending;
            bool closed = store.State == StoreOperatingState.ClosedWithResultPending ||
                          (store.State == StoreOperatingState.Closed && saleComplete &&
                           cleaning != null && cleaning.IsComplete);
            bool fixturePlaced = requiredFixtures != null && requiredFixtures.Length > 0 &&
                                 requiredFixtures[0] != null && fixturePlacement != null &&
                                 fixturePlacement.IsPlaced(
                                     requiredFixtures[0].StableFixtureInstanceId);
            bool clockedIn = store.State != StoreOperatingState.Closed || fixturePlaced ||
                             (delivery != null && delivery.IsOpen) || saleComplete;
            bool stocked = HasStocked(colaProduct) && HasStocked(chipsProduct);

            DrawChecklistLine(panel, 0, clockedIn, "Clock in");
            DrawChecklistLine(panel, 1, fixturePlaced, "Place checkout");
            DrawChecklistLine(panel, 2, delivery != null && delivery.IsOpen, "Open delivery");
            DrawChecklistLine(panel, 3, stocked, "Stock cola + chips");
            DrawChecklistLine(panel, 4, opened, "Open the store");
            DrawChecklistLine(panel, 5, saleComplete, "Complete one sale");
            DrawChecklistLine(panel, 6, cleaning != null && cleaning.IsComplete, "Clean the spill");
            DrawChecklistLine(panel, 7, closed, "Close and review");
        }

        private void DrawChecklistLine(Rect panel, int index, bool complete, string label)
        {
            float y = panel.y + 53f + index * 26f;
            Color marker = complete ? Teal : new Color(0.22f, 0.28f, 0.3f, 1f);
            DrawPanel(new Rect(panel.x + 22f, y + 5f, 13f, 13f), marker);
            GUIStyle style = new(bodyStyle) { normal = { textColor = complete ? MutedInk : Ink } };
            GUI.Label(new Rect(panel.x + 48f, y, 285f, 24f), label, style);
            if (complete)
            {
                GUI.Label(new Rect(panel.x + 295f, y, 46f, 24f), "DONE", eyebrowStyle);
            }
        }

        private void DrawObjective(float width, float height)
        {
            Rect panel = new((width - 980f) * 0.5f, height - 156f, 980f, 118f);
            DrawPanel(panel, Night);
            DrawPanel(new Rect(panel.x, panel.y, 8f, panel.height), Amber);
            GUI.Label(
                new Rect(panel.x + 30f, panel.y + 13f, 620f, 24f),
                $"TASK {CurrentObjectiveStep} OF {ObjectiveStepCount}",
                eyebrowStyle);
            GUI.Label(
                new Rect(panel.x + 30f, panel.y + 38f, 900f, 34f),
                CurrentObjectiveText,
                objectiveStyle);

            FirstStoreWorldInteractionPrompt prompt = interaction.CurrentPrompt;
            if (prompt == null)
            {
                GUI.Label(
                    new Rect(panel.x + 30f, panel.y + 82f, 900f, 23f),
                    "Follow the amber marker and center it in view",
                    smallStyle);
                return;
            }

            DrawPanel(new Rect(panel.x + 30f, panel.y + 79f, 44f, 28f), Teal);
            GUI.Label(new Rect(panel.x + 30f, panel.y + 80f, 44f, 25f), prompt.Input, keyStyle);
            GUI.Label(
                new Rect(panel.x + 88f, panel.y + 79f, 430f, 27f),
                prompt.Action,
                bodyStyle);
            if (!string.IsNullOrWhiteSpace(prompt.StateOrBlocker))
            {
                GUI.Label(
                    new Rect(panel.x + 520f, panel.y + 80f, 420f, 25f),
                    prompt.StateOrBlocker,
                    smallStyle);
            }
        }

        private void DrawCrosshair(float width, float height)
        {
            float x = width * 0.5f;
            float y = height * 0.5f;
            Color color = interaction.CurrentPrompt == null ?
                new Color(0.8f, 0.84f, 0.82f, 0.62f) : Teal;
            DrawPanel(new Rect(x - 8f, y - 1f, 16f, 2f), color);
            DrawPanel(new Rect(x - 1f, y - 8f, 2f, 16f), color);
            if (interaction.CurrentPrompt != null)
            {
                DrawPanel(new Rect(x - 2f, y - 2f, 4f, 4f), Ink);
            }
        }

        private void DrawHeldItem(float width, float height)
        {
            ProductItem held = stocking?.HeldPhysicalUnit;
            if (held == null)
            {
                return;
            }

            Rect panel = new(width - 402f, height - 148f, 370f, 76f);
            DrawPanel(panel, NightSoft);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 12f, 320f, 20f), "IN HAND", eyebrowStyle);
            GUI.Label(
                new Rect(panel.x + 20f, panel.y + 35f, 320f, 28f),
                HeldProductName(),
                bodyStyle);
        }

        private void DrawFeedback(float width, float height)
        {
            if (Time.unscaledTime > feedbackUntil || string.IsNullOrWhiteSpace(feedbackText))
            {
                return;
            }

            Rect panel = new((width - 680f) * 0.5f, height * 0.5f + 58f, 680f, 58f);
            DrawPanel(panel, Night);
            DrawPanel(
                new Rect(panel.x, panel.y, 7f, panel.height),
                feedbackSucceeded ? Teal : Error);
            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 14f, panel.width - 48f, 30f),
                feedbackText,
                centerStyle);
        }

        private void DrawIntroControls(float height)
        {
            float age = Time.unscaledTime - shownAt;
            if (age > 18f)
            {
                return;
            }

            float alpha = age < 13f ? 1f : Mathf.InverseLerp(18f, 13f, age);
            Color panelColor = Night;
            panelColor.a *= alpha;
            Rect panel = new(32f, height - 126f, 550f, 82f);
            DrawPanel(panel, panelColor);
            Color prior = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(
                new Rect(panel.x + 20f, panel.y + 13f, 510f, 22f),
                "WASD MOVE   •   SHIFT BRISK WALK   •   MOUSE LOOK",
                eyebrowStyle);
            GUI.Label(
                new Rect(panel.x + 20f, panel.y + 43f, 510f, 24f),
                "E interact   •   Q back/correct   •   Wheel rotate   •   B camera motion",
                smallStyle);
            GUI.color = prior;
        }

        private void DrawResult(float width, float height)
        {
            StoreSessionTotals totals = store.ResultTotals;
            if (totals == null)
            {
                return;
            }

            Rect shade = new(0f, 0f, width, height);
            DrawPanel(shade, new Color(0.01f, 0.018f, 0.025f, 0.68f));
            Rect panel = new((width - 820f) * 0.5f, (height - 500f) * 0.5f, 820f, 500f);
            DrawPanel(panel, new Color(0.035f, 0.055f, 0.075f, 0.99f));
            DrawPanel(new Rect(panel.x, panel.y, panel.width, 9f), Teal);
            GUI.Label(
                new Rect(panel.x + 45f, panel.y + 38f, panel.width - 90f, 32f),
                "FIRST SHIFT COMPLETE",
                titleStyle);
            GUI.Label(
                new Rect(panel.x + 45f, panel.y + 79f, panel.width - 90f, 26f),
                "You turned an empty lease into an operating store.",
                bodyStyle);

            DrawResultMetric(panel, 0, "GROSS SALES", FormatCents(totals.grossSalesCents));
            DrawResultMetric(panel, 1, "INVENTORY COST", FormatCents(totals.costOfGoodsSoldCents));
            DrawResultMetric(panel, 2, "SHIFT EXPENSES", FormatCents(totals.includedOperatingExpensesCents));
            DrawResultMetric(
                panel,
                3,
                "CONTRIBUTION",
                FormatCents(totals.contributionAfterCostOfGoodsCents));
            GUI.Label(
                new Rect(panel.x + 45f, panel.y + 366f, panel.width - 90f, 26f),
                $"{totals.unitsSold} items sold  •  {totals.transactionCount} completed sale",
                centerStyle);
            GUI.Label(
                new Rect(panel.x + 45f, panel.y + 424f, panel.width - 90f, 34f),
                "Return to the front panel and press E to continue",
                objectiveStyle);
        }

        private void DrawResultMetric(Rect panel, int index, string label, string value)
        {
            float columnWidth = (panel.width - 110f) * 0.5f;
            int column = index % 2;
            int row = index / 2;
            Rect metric = new(
                panel.x + 45f + column * (columnWidth + 20f),
                panel.y + 132f + row * 105f,
                columnWidth,
                85f);
            DrawPanel(metric, NightSoft);
            GUI.Label(new Rect(metric.x + 20f, metric.y + 13f, metric.width - 40f, 20f), label, eyebrowStyle);
            GUI.Label(new Rect(metric.x + 20f, metric.y + 36f, metric.width - 40f, 38f), value, resultValueStyle);
        }

        private string FriendlyStoreState()
        {
            if (store == null)
            {
                return "LOADING";
            }

            return store.State switch
            {
                StoreOperatingState.Closed => "CLOSED",
                StoreOperatingState.Preparing => "SETTING UP",
                StoreOperatingState.Open => "OPEN",
                StoreOperatingState.Closing => "CLOSING",
                StoreOperatingState.ClosedWithResultPending => "SHIFT COMPLETE",
                _ => "CLOSED"
            };
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = CreateStyle(25, FontStyle.Bold, Ink);
            eyebrowStyle = CreateStyle(13, FontStyle.Bold, Teal);
            bodyStyle = CreateStyle(18, FontStyle.Normal, Ink);
            smallStyle = CreateStyle(15, FontStyle.Normal, MutedInk);
            objectiveStyle = CreateStyle(22, FontStyle.Bold, Ink);
            keyStyle = CreateStyle(17, FontStyle.Bold, Color.black, TextAnchor.MiddleCenter);
            centerStyle = CreateStyle(17, FontStyle.Normal, Ink, TextAnchor.MiddleCenter);
            resultValueStyle = CreateStyle(29, FontStyle.Bold, Ink);
        }

        private static GUIStyle CreateStyle(
            int size,
            FontStyle fontStyle,
            Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = fontStyle,
                alignment = alignment,
                wordWrap = false,
                clipping = TextClipping.Clip,
                normal = { textColor = color }
            };
        }

        private static void DrawPanel(Rect rect, Color color)
        {
            Color prior = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = prior;
        }

        private static string FormatCents(long cents)
        {
            bool negative = cents < 0;
            ulong absolute = negative
                ? (ulong)(-(cents + 1)) + 1UL
                : (ulong)cents;
            return negative
                ? $"-${absolute / 100}.{absolute % 100:00}"
                : $"${absolute / 100}.{absolute % 100:00}";
        }
    }
}
