using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
    /// Presentation-only guidance. The default HUD stays quiet and reveals
    /// controls, blockers, held state, and business depth only in context.
    /// </summary>
    public sealed class FirstStorePromptPresenter : MonoBehaviour
    {
        private static readonly Color Ink =
            new(0.94f, 0.96f, 0.95f, 1f);
        private static readonly Color MutedInk =
            new(0.64f, 0.7f, 0.69f, 1f);
        private static readonly Color Night =
            new(0.025f, 0.04f, 0.052f, 0.94f);
        private static readonly Color NightSoft =
            new(0.055f, 0.08f, 0.092f, 0.92f);
        private static readonly Color Teal =
            new(0.12f, 0.78f, 0.68f, 1f);
        private static readonly Color Amber =
            new(1f, 0.58f, 0.2f, 1f);
        private static readonly Color Error =
            new(0.95f, 0.3f, 0.23f, 1f);

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
        [SerializeField] private FirstStoreDiskPersistenceController persistence;
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
        private GUIStyle promptStyle;
        private GUIStyle keyStyle;
        private GUIStyle centerStyle;
        private GUIStyle resultValueStyle;
        private float shownAt;
        private float feedbackUntil;
        private string feedbackText;
        private bool feedbackSucceeded;
        private bool helpVisible;
        private FirstStoreObjectiveKind priorObjective;
        private float objectiveToastUntil;

        public string CurrentPromptText =>
            interaction != null && interaction.IsWorldInteractionEnabled
                ? interaction.CurrentPromptText
                : string.Empty;

        public FirstStoreObjectiveKind CurrentObjectiveKind => DeriveObjectiveKind();
        public string CurrentObjectiveText => DescribeObjective(CurrentObjectiveKind);
        public int CurrentObjectiveStep => GetObjectiveStep(CurrentObjectiveKind);
        public int ObjectiveStepCount => 8;
        public Transform CurrentObjectiveTarget => ResolveObjectiveTarget(CurrentObjectiveKind);
        public bool HelpVisible => helpVisible;

        private void OnEnable()
        {
            shownAt = Time.unscaledTime;
            priorObjective = DeriveObjectiveKind();
            objectiveToastUntil = Time.unscaledTime + 6f;
            if (interaction != null)
            {
                interaction.InteractionResolved += HandleInteractionResolved;
            }
            if (persistence != null)
            {
                persistence.OperationCompleted += HandlePersistenceCompleted;
            }
        }

        private void OnDisable()
        {
            if (interaction != null)
            {
                interaction.InteractionResolved -= HandleInteractionResolved;
            }
            if (persistence != null)
            {
                persistence.OperationCompleted -= HandlePersistenceCompleted;
            }
        }

        private void Update()
        {
            FirstStoreObjectiveKind objective = DeriveObjectiveKind();
            if (objective != priorObjective)
            {
                priorObjective = objective;
                objectiveToastUntil = Time.unscaledTime + 5.5f;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null &&
                keyboard.hKey.wasPressedThisFrame &&
                !GamePauseMenuController.IsAnyMenuOpen)
            {
                helpVisible = !helpVisible;
                if (helpVisible)
                {
                    objectiveToastUntil = Time.unscaledTime + 8f;
                }
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
                : "Store setup needs attention";
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
                    "Use the front control to begin",
                FirstStoreObjectiveKind.PlaceCheckout =>
                    "Place the checkout counter",
                FirstStoreObjectiveKind.OpenDelivery =>
                    "Open the delivery in Receiving",
                FirstStoreObjectiveKind.TakeCola =>
                    "Take cola from the delivery",
                FirstStoreObjectiveKind.TakeChips =>
                    "Take chips from the delivery",
                FirstStoreObjectiveKind.StockHeldProduct =>
                    $"Stock {HeldProductName()} on its shelf",
                FirstStoreObjectiveKind.OpenStore =>
                    "Open for business",
                FirstStoreObjectiveKind.CompleteCheckout =>
                    "Serve the waiting customer",
                FirstStoreObjectiveKind.CleanSpill =>
                    "Clean the spill before closing",
                FirstStoreObjectiveKind.BeginClosing =>
                    "Begin closing at the front control",
                FirstStoreObjectiveKind.FinalizeClosing =>
                    "Finish closing once the floor is clear",
                FirstStoreObjectiveKind.ReviewResult =>
                    "Review the shift result",
                _ => "The business is yours to run"
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
                ? "product"
                : definition.DisplayName;
        }

        private void HandleInteractionResolved(FirstStoreInteractionFeedback feedback)
        {
            feedbackSucceeded = feedback.Succeeded;
            feedbackText = feedback.Succeeded
                ? FriendlySuccess(feedback.Action)
                : FriendlyFailure(feedback.Message);
            feedbackUntil = Time.unscaledTime +
                            (feedback.Succeeded ? 0.9f : 3.4f);
        }

        private void HandlePersistenceCompleted(bool succeeded, string diagnostic)
        {
            feedbackSucceeded = succeeded;
            feedbackText = succeeded
                ? diagnostic.Contains("Press F9", StringComparison.OrdinalIgnoreCase)
                    ? "Press F9 again to reload"
                    : diagnostic.Contains("Loaded", StringComparison.OrdinalIgnoreCase)
                        ? "Company loaded"
                        : diagnostic.Contains("No saved", StringComparison.OrdinalIgnoreCase)
                            ? "No saved company yet"
                            : "Company saved"
                : FriendlyFailure(diagnostic);
            feedbackUntil = Time.unscaledTime + (succeeded ? 1.8f : 3.8f);
        }

        private void OnGUI()
        {
            if (interaction == null || !interaction.IsWorldInteractionEnabled)
            {
                return;
            }

            EnsureStyles();
            float scale = Mathf.Max(
                0.68f,
                Mathf.Min(Screen.width / 1920f, Screen.height / 1080f) *
                GamePauseMenuController.UserInterfaceScale);
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            if (store != null && store.IsContinuousOperation)
            {
                DrawCompanyGlance(width);
            }
            else
            {
                DrawObjectiveToast(width);
            }
            DrawCrosshair(width, height);
            DrawContextPrompt(width, height);
            DrawHeldItem(width, height);
            DrawFeedback(width, height);
            DrawOperationalCue(height);
            if (helpVisible)
            {
                DrawHelp(height);
            }
            if (store != null && !store.IsContinuousOperation &&
                store.State == StoreOperatingState.ClosedWithResultPending)
            {
                DrawResult(width, height);
            }

            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private void DrawCompanyGlance(float width)
        {
            PortfolioProgressionSnapshot company =
                portfolio?.Progression?.CreateSnapshot();
            if (company == null)
            {
                return;
            }

            Rect panel = new(width - 340f, 28f, 308f, 58f);
            DrawPanel(panel, Night);
            DrawPanel(new Rect(panel.x, panel.y, 5f, panel.height), Teal);
            GUI.Label(
                new Rect(panel.x + 20f, panel.y + 8f, 98f, 20f),
                $"DAY {company.currentDay}",
                eyebrowStyle);
            GUI.Label(
                new Rect(panel.x + 20f, panel.y + 27f, 265f, 24f),
                FormatCents(company.cashCents),
                titleStyle);
        }

        private void DrawObjectiveToast(float width)
        {
            if (!helpVisible &&
                (Time.unscaledTime > objectiveToastUntil ||
                 CurrentObjectiveKind == FirstStoreObjectiveKind.Complete))
            {
                return;
            }

            Rect panel = new((width - 660f) * 0.5f, 30f, 660f, 72f);
            DrawPanel(panel, Night);
            DrawPanel(new Rect(panel.x, panel.y, 6f, panel.height), Amber);
            GUI.Label(
                new Rect(panel.x + 22f, panel.y + 9f, 600f, 20f),
                "NEXT",
                eyebrowStyle);
            GUI.Label(
                new Rect(panel.x + 22f, panel.y + 30f, 600f, 30f),
                CurrentObjectiveText,
                bodyStyle);
        }

        private void DrawContextPrompt(float width, float height)
        {
            FirstStoreWorldInteractionPrompt prompt = interaction.CurrentPrompt;
            if (prompt == null)
            {
                return;
            }

            string state = FriendlyPromptState(prompt.StateOrBlocker);
            bool hasState = !string.IsNullOrWhiteSpace(state);
            float panelHeight = hasState ? 76f : 54f;
            Rect panel = new(
                (width - 660f) * 0.5f,
                height * 0.68f,
                660f,
                panelHeight);
            DrawPanel(panel, Night);
            Rect key = new(panel.x + 14f, panel.y + 11f, 42f, 32f);
            DrawPanel(key, Teal);
            GUI.Label(key, prompt.Input, keyStyle);
            GUI.Label(
                new Rect(panel.x + 72f, panel.y + 8f, panel.width - 90f, 38f),
                FriendlyAction(prompt.Action),
                promptStyle);
            if (hasState)
            {
                GUI.Label(
                    new Rect(panel.x + 72f, panel.y + 43f, panel.width - 90f, 25f),
                    state,
                    smallStyle);
            }
        }

        private void DrawCrosshair(float width, float height)
        {
            float x = width * 0.5f;
            float y = height * 0.5f;
            bool focused = interaction.CurrentPrompt != null;
            Color color = focused
                ? Teal
                : new Color(0.88f, 0.91f, 0.89f, 0.58f);
            float radius = focused ? 8f : 4f;
            DrawPanel(new Rect(x - radius, y - 1f, radius * 0.65f, 2f), color);
            DrawPanel(new Rect(x + radius * 0.35f, y - 1f, radius * 0.65f, 2f), color);
            DrawPanel(new Rect(x - 1f, y - radius, 2f, radius * 0.65f), color);
            DrawPanel(new Rect(x - 1f, y + radius * 0.35f, 2f, radius * 0.65f), color);
        }

        private void DrawHeldItem(float width, float height)
        {
            ProductItem held = stocking?.HeldPhysicalUnit;
            if (held == null)
            {
                return;
            }

            Rect panel = new(width - 350f, height - 94f, 318f, 62f);
            DrawPanel(panel, NightSoft);
            DrawPanel(new Rect(panel.x, panel.y, 5f, panel.height), Amber);
            GUI.Label(
                new Rect(panel.x + 18f, panel.y + 8f, panel.width - 36f, 24f),
                HeldProductName(),
                bodyStyle);
            GUI.Label(
                new Rect(panel.x + 18f, panel.y + 34f, panel.width - 36f, 20f),
                "Wheel  Rotate    •    Q  Put down",
                smallStyle);
        }

        private void DrawFeedback(float width, float height)
        {
            if (Time.unscaledTime > feedbackUntil ||
                string.IsNullOrWhiteSpace(feedbackText))
            {
                return;
            }

            float feedbackWidth = feedbackSucceeded ? 360f : 620f;
            Rect panel = new(
                (width - feedbackWidth) * 0.5f,
                height * 0.54f,
                feedbackWidth,
                52f);
            DrawPanel(panel, Night);
            DrawPanel(
                new Rect(panel.x, panel.y, 6f, panel.height),
                feedbackSucceeded ? Teal : Error);
            GUI.Label(
                new Rect(panel.x + 18f, panel.y + 9f, panel.width - 36f, 34f),
                feedbackText,
                centerStyle);
        }

        private void DrawOperationalCue(float height)
        {
            string cue = null;
            Color cueColor = Teal;
            if (cleaning != null && cleaning.NeedsCleaning)
            {
                cue = "Cleanup needed";
                cueColor = Amber;
            }
            else if (stagedCheckout != null &&
                     !stagedCheckout.AllBasketsComplete &&
                     stagedCheckout.NextAction == StagedCheckoutPrimaryAction.Begin)
            {
                cue = "Customer waiting";
            }

            if (cue != null)
            {
                Rect panel = new(30f, height - 72f, 220f, 40f);
                DrawPanel(panel, NightSoft);
                DrawPanel(
                    new Rect(panel.x + 13f, panel.y + 15f, 10f, 10f),
                    cueColor);
                GUI.Label(
                    new Rect(panel.x + 36f, panel.y + 8f, 170f, 24f),
                    cue,
                    smallStyle);
                return;
            }

            if (Time.unscaledTime - shownAt < 12f)
            {
                GUI.Label(
                    new Rect(30f, height - 58f, 220f, 24f),
                    "H  Help",
                    smallStyle);
            }
        }

        private void DrawHelp(float height)
        {
            Rect panel = new(30f, height - 294f, 430f, 252f);
            DrawPanel(panel, Night);
            DrawPanel(new Rect(panel.x, panel.y, 6f, panel.height), Teal);
            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 18f, 370f, 28f),
                "Controls",
                titleStyle);
            DrawHelpLine(panel, 0, "MOVE", "WASD   •   Shift brisk walk");
            DrawHelpLine(panel, 1, "USE", "E interact   •   Q back / put down");
            DrawHelpLine(panel, 2, "HANDLE", "Mouse wheel rotates held objects");
            DrawHelpLine(panel, 3, "COMPANY", "Tab management   •   Esc menu");
            DrawHelpLine(panel, 4, "SAVE", "F5 save   •   F9 twice to reload");
            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 220f, 370f, 20f),
                "Press H to close",
                smallStyle);
        }

        private void DrawHelpLine(
            Rect panel,
            int index,
            string label,
            string value)
        {
            float y = panel.y + 58f + index * 31f;
            GUI.Label(
                new Rect(panel.x + 24f, y, 90f, 22f),
                label,
                eyebrowStyle);
            GUI.Label(
                new Rect(panel.x + 112f, y, 285f, 22f),
                value,
                smallStyle);
        }

        private void DrawResult(float width, float height)
        {
            StoreSessionTotals totals = store.ResultTotals;
            if (totals == null)
            {
                return;
            }

            DrawPanel(
                new Rect(0f, 0f, width, height),
                new Color(0.008f, 0.014f, 0.02f, 0.78f));
            Rect panel = new(
                (width - 820f) * 0.5f,
                (height - 490f) * 0.5f,
                820f,
                490f);
            DrawPanel(panel, Night);
            DrawPanel(new Rect(panel.x, panel.y, panel.width, 8f), Teal);
            GUI.Label(
                new Rect(panel.x + 42f, panel.y + 35f, panel.width - 84f, 38f),
                "Shift complete",
                titleStyle);
            GUI.Label(
                new Rect(panel.x + 42f, panel.y + 77f, panel.width - 84f, 26f),
                "Here is what the floor produced.",
                bodyStyle);

            DrawResultMetric(panel, 0, "SALES", FormatCents(totals.grossSalesCents));
            DrawResultMetric(panel, 1, "INVENTORY", FormatCents(totals.costOfGoodsSoldCents));
            DrawResultMetric(
                panel,
                2,
                "OVERHEAD",
                FormatCents(totals.includedOperatingExpensesCents));
            DrawResultMetric(
                panel,
                3,
                "CONTRIBUTION",
                FormatCents(totals.contributionAfterCostOfGoodsCents));
            GUI.Label(
                new Rect(panel.x + 42f, panel.y + 365f, panel.width - 84f, 28f),
                $"{totals.unitsSold} items  •  {totals.transactionCount} completed sales",
                centerStyle);
            GUI.Label(
                new Rect(panel.x + 42f, panel.y + 420f, panel.width - 84f, 30f),
                "Use the front control to continue",
                promptStyle);
        }

        private void DrawResultMetric(
            Rect panel,
            int index,
            string label,
            string value)
        {
            float columnWidth = (panel.width - 104f) * 0.5f;
            int column = index % 2;
            int row = index / 2;
            Rect metric = new(
                panel.x + 42f + column * (columnWidth + 20f),
                panel.y + 128f + row * 102f,
                columnWidth,
                82f);
            DrawPanel(metric, NightSoft);
            GUI.Label(
                new Rect(metric.x + 18f, metric.y + 12f, metric.width - 36f, 20f),
                label,
                eyebrowStyle);
            GUI.Label(
                new Rect(metric.x + 18f, metric.y + 34f, metric.width - 36f, 38f),
                value,
                resultValueStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = CreateStyle(24, FontStyle.Bold, Ink);
            eyebrowStyle = CreateStyle(12, FontStyle.Bold, Teal);
            bodyStyle = CreateStyle(18, FontStyle.Normal, Ink);
            smallStyle = CreateStyle(14, FontStyle.Normal, MutedInk);
            promptStyle = CreateStyle(20, FontStyle.Bold, Ink);
            keyStyle = CreateStyle(
                17,
                FontStyle.Bold,
                Color.black,
                TextAnchor.MiddleCenter);
            centerStyle = CreateStyle(
                16,
                FontStyle.Normal,
                Ink,
                TextAnchor.MiddleCenter);
            resultValueStyle = CreateStyle(28, FontStyle.Bold, Ink);
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
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static string FriendlyAction(string action)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                return "Use";
            }
            return action
                .Replace("staged", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();
        }

        private static string FriendlyPromptState(string state)
        {
            if (string.IsNullOrWhiteSpace(state) ||
                string.Equals(state, "ready", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string friendly = state
                .Replace("staged basket", "customer", StringComparison.OrdinalIgnoreCase)
                .Replace("staged baskets", "customers", StringComparison.OrdinalIgnoreCase)
                .Replace("Q corrects recent scan", "Q corrects", StringComparison.OrdinalIgnoreCase)
                .Replace("aim at the visible product", "use the item on the counter", StringComparison.OrdinalIgnoreCase)
                .Replace("; ", "  •  ", StringComparison.Ordinal);
            return friendly.Length <= 92
                ? friendly
                : $"{friendly.Substring(0, 89)}…";
        }

        private static string FriendlySuccess(string action)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                return "Done";
            }

            if (action.Contains("Rotate", StringComparison.OrdinalIgnoreCase))
            {
                return "Rotated";
            }
            if (action.Contains("Place", StringComparison.OrdinalIgnoreCase) ||
                action.Contains("Stock", StringComparison.OrdinalIgnoreCase))
            {
                return "Placed";
            }
            if (action.Contains("Open", StringComparison.OrdinalIgnoreCase))
            {
                return "Opened";
            }
            if (action.Contains("payment", StringComparison.OrdinalIgnoreCase) ||
                action.Contains("checkout", StringComparison.OrdinalIgnoreCase))
            {
                return "Sale complete";
            }
            if (action.Contains("Clean", StringComparison.OrdinalIgnoreCase))
            {
                return "Cleaned";
            }
            if (action.Contains("Put down", StringComparison.OrdinalIgnoreCase))
            {
                return "Put down";
            }
            return action;
        }

        private static string FriendlyFailure(string diagnostic)
        {
            string value = diagnostic ?? string.Empty;
            if (value.Contains("held", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("holding", StringComparison.OrdinalIgnoreCase))
            {
                return "Put down what you are holding first.";
            }
            if (value.Contains("checkout", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("transaction", StringComparison.OrdinalIgnoreCase))
            {
                return "Finish or cancel the current checkout first.";
            }
            if (value.Contains("occupied", StringComparison.OrdinalIgnoreCase))
            {
                return "That space is occupied.";
            }
            if (value.Contains("outside", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("bounds", StringComparison.OrdinalIgnoreCase))
            {
                return "Keep the whole fixture inside the floor area.";
            }
            if (value.Contains("sealed", StringComparison.OrdinalIgnoreCase))
            {
                return "Open the delivery first.";
            }
            if (value.Contains("no accepted", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("No saved", StringComparison.OrdinalIgnoreCase))
            {
                return "No saved company is available yet.";
            }
            if (value.Contains("malformed", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("unsupported", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("reconcile", StringComparison.OrdinalIgnoreCase))
            {
                return "That save could not be opened. Nothing was changed.";
            }
            if (value.Contains("team member", StringComparison.OrdinalIgnoreCase))
            {
                return "A team member is moving stock. Try again in a moment.";
            }
            if (string.IsNullOrWhiteSpace(value))
            {
                return "That action is not available.";
            }
            return value.Length <= 104
                ? value
                : "That action could not be completed. Try a different position.";
        }

        private static string FormatCents(long cents)
        {
            bool negative = cents < 0;
            ulong absolute = negative
                ? (ulong)(-(cents + 1)) + 1UL
                : (ulong)cents;
            string dollars = (absolute / 100).ToString(
                "N0",
                System.Globalization.CultureInfo.InvariantCulture);
            return negative
                ? $"-${dollars}.{absolute % 100:00}"
                : $"${dollars}.{absolute % 100:00}";
        }
    }
}
