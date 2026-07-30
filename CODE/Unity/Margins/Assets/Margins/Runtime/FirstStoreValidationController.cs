using UnityEngine;
using UnityEngine.InputSystem;

namespace Margins
{
    public sealed class FirstStoreValidationController : MonoBehaviour
    {
        [SerializeField] private FirstStoreInventoryComponent inventory;
        [SerializeField] private DeliveryBoxComponent delivery;
        [SerializeField] private StockingController stocking;
        [SerializeField] private CheckoutStationComponent checkout;
        [SerializeField] private CleaningTaskComponent cleaning;
        [SerializeField] private StoreOperatingController store;
        [SerializeField] private FixturePlacementController fixturePlacement;
        [SerializeField] private PlaceableFixtureComponent essentialFixture;
        [SerializeField] private FirstStoreDiskPersistenceController diskPersistence;
        [SerializeField] private ProductDefinition colaProduct;
        [SerializeField] private ProductDefinition chipsProduct;
        [SerializeField] private FirstPersonController firstPersonController;
        [SerializeField] private PortfolioProgressionController portfolioProgression;

        private int transactionOrdinal = 1;
        private string lastCompletedTransactionId;
        private string lastAction = "Validation scene ready.";

        public string LastAction => lastAction;
        public bool IsHudModeActive =>
            firstPersonController != null &&
            !firstPersonController.IsGameplayMode &&
            (portfolioProgression == null ||
             !portfolioProgression.OwnsManagementDesk);

        private void Start()
        {
            string error = null;
            if (firstPersonController == null ||
                diskPersistence == null ||
                portfolioProgression == null ||
                !delivery.TryInitialize(out error) ||
                !store.TryInitialize(out error) ||
                !portfolioProgression.TryValidateConfiguration(out error) ||
                !diskPersistence.TryValidateConfiguration(out error))
            {
                Record(
                    $"Initialization failed: " +
                    $"{error ?? "the first-person controller reference is missing."}",
                    true);
                return;
            }

            Record("Initialized with explicit first-store references.");
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !IsHudModeActive)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                PlaceRequiredFixture();
            }
            if (keyboard.digit2Key.wasPressedThisFrame)
            {
                OpenDelivery();
            }
            if (keyboard.digit3Key.wasPressedThisFrame)
            {
                RemoveDeliveryUnit(colaProduct);
            }
            if (keyboard.digit4Key.wasPressedThisFrame)
            {
                RemoveDeliveryUnit(chipsProduct);
            }
            if (keyboard.digit5Key.wasPressedThisFrame)
            {
                PickUpLooseUnit(colaProduct);
            }
            if (keyboard.digit6Key.wasPressedThisFrame)
            {
                PickUpLooseUnit(chipsProduct);
            }
            if (keyboard.digit7Key.wasPressedThisFrame)
            {
                StockHeldUnit();
            }
            if (keyboard.digit8Key.wasPressedThisFrame)
            {
                CompleteSale(colaProduct);
            }
            if (keyboard.digit9Key.wasPressedThisFrame)
            {
                CompleteSale(chipsProduct);
            }
            if (keyboard.cKey.wasPressedThisFrame)
            {
                CleanOneUnit();
            }
            if (keyboard.oKey.wasPressedThisFrame)
            {
                AdvanceOperatingState();
            }
            if (keyboard.mKey.wasPressedThisFrame)
            {
                MoveRequiredFixture();
            }
            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                RemoveRequiredFixture();
            }
            if (keyboard.dKey.wasPressedThisFrame)
            {
                AttemptDuplicateTransaction();
            }
        }

        private void OnGUI()
        {
            if (!IsHudModeActive)
            {
                return;
            }

            GUI.Box(new Rect(12f, 12f, 650f, 286f), "");
            GUILayout.BeginArea(new Rect(24f, 20f, 626f, 270f));
            GUILayout.Label("MARGINS FIRST-STORE LOCAL VALIDATION (development HUD)");
            GUILayout.Label("Tab returns to gameplay | world E context | Q correct/cancel | wheel rotate | Backspace remove fixture");
            GUILayout.Label("HUD shortcuts: 1 place fixture | 2 open delivery");
            GUILayout.Label("3/4 remove cola/chips | 5/6 pick cola/chips | 7 stock held");
            GUILayout.Label("8/9 sell cola/chips | D duplicate-ID attempt | C clean | O advance store");
            GUILayout.Label("M move fixture | Backspace remove fixture | F5 save to disk | F9 load from disk");
            GUILayout.Space(5f);
            GUILayout.Label($"State: {store.State} | physical units: {stocking.PhysicalUnits.VisibleUnitCount}");
            GUILayout.Label(
                $"Box cola/chips: {Quantity("loc-delivery", colaProduct)}/{Quantity("loc-delivery", chipsProduct)} | " +
                $"Loose: {Quantity("loc-loose", colaProduct)}/{Quantity("loc-loose", chipsProduct)} | " +
                $"Held: {Quantity("loc-held", colaProduct)}/{Quantity("loc-held", chipsProduct)}");
            GUILayout.Label(
                $"Shelves cola/chips: {Quantity("loc-shelf-cola", colaProduct)}/{Quantity("loc-shelf-chips", chipsProduct)}");
            GUILayout.Label(
                $"Ledger: {checkout.CompletedTransactionCount} transactions, {checkout.UnitsSold} units, ${checkout.GrossSalesCents / 100f:0.00} gross");
            if (store.ResultTotals != null)
            {
                StoreSessionTotals totals = store.ResultTotals;
                GUILayout.Label(
                    $"Result: gross ${totals.grossSalesCents / 100f:0.00} | COGS ${totals.costOfGoodsSoldCents / 100f:0.00} | " +
                    $"expenses ${totals.includedOperatingExpensesCents / 100f:0.00} | contribution ${totals.contributionAfterCostOfGoodsCents / 100f:0.00}");
            }
            GUILayout.Label($"Last: {lastAction}");
            GUILayout.Label($"Persistence: {diskPersistence.LastDiagnostic}");
            GUILayout.EndArea();

            GUI.Box(
                new Rect(12f, Screen.height - 158f, Screen.width - 24f, 146f),
                "VALIDATION CONTROLS");
            GUILayout.BeginArea(
                new Rect(24f, Screen.height - 134f, Screen.width - 48f, 116f));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Place Fixture")) PlaceRequiredFixture();
            if (GUILayout.Button("Open Delivery")) OpenDelivery();
            if (GUILayout.Button("Remove Cola")) RemoveDeliveryUnit(colaProduct);
            if (GUILayout.Button("Remove Chips")) RemoveDeliveryUnit(chipsProduct);
            if (GUILayout.Button("Pick Cola")) PickUpLooseUnit(colaProduct);
            if (GUILayout.Button("Pick Chips")) PickUpLooseUnit(chipsProduct);
            if (GUILayout.Button("Stock Held")) StockHeldUnit();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Sell Cola")) CompleteSale(colaProduct);
            if (GUILayout.Button("Sell Chips")) CompleteSale(chipsProduct);
            if (GUILayout.Button("Duplicate ID")) AttemptDuplicateTransaction();
            if (GUILayout.Button("Clean +1")) CleanOneUnit();
            if (GUILayout.Button("Advance State")) AdvanceOperatingState();
            if (GUILayout.Button("Move Fixture")) MoveRequiredFixture();
            if (GUILayout.Button("Remove Fixture")) RemoveRequiredFixture();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save to Disk (F5)")) SaveToDisk();
            if (GUILayout.Button("Load from Disk (F9)")) LoadFromDisk();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void PlaceRequiredFixture()
        {
            FixturePlacementResult result = fixturePlacement.TryPlace(
                essentialFixture,
                new GridPosition(1, 1),
                0);
            Record($"Place required fixture: {result.Failure}.");
        }

        private void OpenDelivery()
        {
            bool success = delivery.TryOpen(
                out DeliveryContainerOpenResult result,
                out string error);
            Record($"Open delivery: {success}, {result}, {error ?? "ok"}.");
        }

        private void StockHeldUnit()
        {
            ProductItem held = stocking.HeldPhysicalUnit;
            int quarterTurns = held?.QuarterTurns ?? 0;
            bool success = stocking.TryStockHeldUnit(quarterTurns, out string error);
            Record($"Stock held unit: {success}, {error ?? "ok"}.");
        }

        private void CleanOneUnit()
        {
            CleaningProgressResult result = cleaning.TryApplyProgress(1);
            Record(
                $"Cleaning progress: {result} " +
                $"({cleaning.CompletedProgressUnits}/{cleaning.RequiredProgressUnits}).");
        }

        private void MoveRequiredFixture()
        {
            FixturePlacementResult result = fixturePlacement.TryMove(
                essentialFixture,
                new GridPosition(4, 3),
                1);
            Record($"Move required fixture: {result.Failure}.");
        }

        private void RemoveRequiredFixture()
        {
            FixturePlacementResult result =
                fixturePlacement.TryRemove(essentialFixture);
            Record($"Remove required fixture: {result.Failure}.");
        }

        private void SaveToDisk()
        {
            bool success = diskPersistence.TrySave();
            Record($"Disk save: {success}, {diskPersistence.LastDiagnostic}");
        }

        private void LoadFromDisk()
        {
            bool success = diskPersistence.TryLoad();
            Record($"Disk load: {success}, {diskPersistence.LastDiagnostic}");
        }

        private void RemoveDeliveryUnit(ProductDefinition product)
        {
            bool success = delivery.TryRemoveOneUnit(
                product,
                out ProductItem item,
                out DeliveryContainerFailure failure,
                out InventoryTransferResult transfer,
                out string error);
            Record(
                $"Remove {product.DisplayName}: {success}, {failure}, " +
                $"{transfer?.Failure.ToString() ?? "no transfer"}, unit {item?.PhysicalUnitId ?? "none"}, {error ?? "ok"}.");
        }

        private void PickUpLooseUnit(ProductDefinition product)
        {
            bool success = stocking.TryPickUpLooseUnit(
                product,
                out ProductItem item,
                out string error);
            Record(
                $"Pick up {product.DisplayName}: {success}, unit {item?.PhysicalUnitId ?? "none"}, {error ?? "ok"}.");
        }

        private void CompleteSale(ProductDefinition product)
        {
            string transactionId = $"manual-transaction-{transactionOrdinal:D3}";
            CheckoutFailure scanFailure = CheckoutFailure.InvalidSession;
            CheckoutFailure completionFailure = CheckoutFailure.InvalidSession;
            CheckoutTransactionSummary summary = null;
            bool began = checkout.TryBeginSession(transactionId, out string error);
            bool scanned = began && checkout.TryScan(product, 1, out scanFailure);
            bool completed = scanned && checkout.TryComplete(
                out summary,
                out completionFailure);
            if (!completed)
            {
                Record(
                    $"Sell {product.DisplayName}: false, " +
                    $"scan {scanFailure}, completion {completionFailure}, {error ?? "rejected"}.");
                return;
            }

            lastCompletedTransactionId = transactionId;
            transactionOrdinal++;
            Record(
                $"Sell {product.DisplayName}: true, {summary.transactionId}, ${summary.subtotalCents / 100f:0.00}.");
        }

        private void AttemptDuplicateTransaction()
        {
            if (lastCompletedTransactionId == null)
            {
                Record("Duplicate transaction attempt: no completed transaction exists.");
                return;
            }

            ProductDefinition product =
                Quantity("loc-shelf-cola", colaProduct) > 0
                    ? colaProduct
                    : chipsProduct;
            CheckoutFailure scanFailure = CheckoutFailure.InvalidSession;
            CheckoutFailure completionFailure = CheckoutFailure.InvalidSession;
            bool began = checkout.TryBeginSession(lastCompletedTransactionId, out string error);
            bool scanned = began && checkout.TryScan(product, 1, out scanFailure);
            bool completed = scanned && checkout.TryComplete(
                out _,
                out completionFailure);
            Record(
                $"Duplicate transaction attempt: began {began}, scanned {scanned} ({scanFailure}), " +
                $"completed {completed} ({completionFailure}), {error ?? "expected rejection"}.");
        }

        private void AdvanceOperatingState()
        {
            bool success;
            string error;
            switch (store.State)
            {
                case StoreOperatingState.Closed:
                    success = store.TryBeginPreparation(out error);
                    break;
                case StoreOperatingState.Preparing:
                    success = store.TryOpenStore(out error);
                    break;
                case StoreOperatingState.Open:
                    success = store.TryBeginClosing(out error);
                    break;
                case StoreOperatingState.Closing:
                    success = store.TryFinishClosing(out error);
                    break;
                case StoreOperatingState.ClosedWithResultPending:
                    success = store.TryAcknowledgeResult(out error);
                    break;
                default:
                    success = false;
                    error = "Unsupported operating state.";
                    break;
            }

            Record($"Advance operating state: {success}, now {store.State}, {error ?? "ok"}.");
        }

        private int Quantity(string locationId, ProductDefinition product)
        {
            return inventory.Inventory.GetQuantity(
                locationId,
                product.StableProductId);
        }

        private void Record(string message, bool isError = false)
        {
            lastAction = message;
            if (isError)
            {
                Debug.LogError(message, this);
            }
            else
            {
                Debug.Log(message, this);
            }
        }
    }
}
