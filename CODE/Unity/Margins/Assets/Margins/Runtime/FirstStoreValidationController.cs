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
        [SerializeField] private FirstStorePersistenceMapperComponent persistence;
        [SerializeField] private ProductDefinition colaProduct;
        [SerializeField] private ProductDefinition chipsProduct;

        private FirstStoreSnapshot capturedSnapshot;
        private int transactionOrdinal = 1;
        private string lastCompletedTransactionId;
        private string lastAction = "Validation scene ready.";

        public string LastAction => lastAction;

        private void Start()
        {
            if (!delivery.TryInitialize(out string error) ||
                !store.TryInitialize(out error) ||
                !persistence.TryValidateConfiguration(out error))
            {
                Record($"Initialization failed: {error}", true);
                return;
            }

            Record("Initialized with explicit first-store references.");
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                FixturePlacementResult result = fixturePlacement.TryPlace(
                    essentialFixture,
                    new GridPosition(1, 1),
                    0);
                Record($"Place required fixture: {result.Failure}.");
            }
            if (keyboard.digit2Key.wasPressedThisFrame)
            {
                bool success = delivery.TryOpen(out DeliveryContainerOpenResult result, out string error);
                Record($"Open delivery: {success}, {result}, {error ?? "ok"}.");
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
                bool success = stocking.TryStockHeldUnit(0, out string error);
                Record($"Stock held unit: {success}, {error ?? "ok"}.");
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
                CleaningProgressResult result = cleaning.TryApplyProgress(1);
                Record($"Cleaning progress: {result} ({cleaning.CompletedProgressUnits}/{cleaning.RequiredProgressUnits}).");
            }
            if (keyboard.oKey.wasPressedThisFrame)
            {
                AdvanceOperatingState();
            }
            if (keyboard.mKey.wasPressedThisFrame)
            {
                FixturePlacementResult result = fixturePlacement.TryMove(
                    essentialFixture,
                    new GridPosition(4, 3),
                    1);
                Record($"Move required fixture: {result.Failure}.");
            }
            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                FixturePlacementResult result =
                    fixturePlacement.TryRemove(essentialFixture);
                Record($"Remove required fixture: {result.Failure}.");
            }
            if (keyboard.dKey.wasPressedThisFrame)
            {
                AttemptDuplicateTransaction();
            }
            if (keyboard.f5Key.wasPressedThisFrame)
            {
                bool success = persistence.TryCapture(
                    out capturedSnapshot,
                    out string error);
                Record($"Capture temporary in-memory snapshot: {success}, {error ?? "ok"}.");
            }
            if (keyboard.f9Key.wasPressedThisFrame)
            {
                string error = null;
                bool success = capturedSnapshot != null &&
                               persistence.TryRestore(capturedSnapshot, out error);
                Record(
                    capturedSnapshot == null
                        ? "Restore temporary snapshot: false, no capture exists."
                        : $"Restore temporary snapshot: {success}, {error ?? "ok"}.");
            }
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(12f, 12f, 650f, 286f), "");
            GUILayout.BeginArea(new Rect(24f, 20f, 626f, 270f));
            GUILayout.Label("MARGINS FIRST-STORE LOCAL VALIDATION (development HUD)");
            GUILayout.Label("WASD/mouse: movement/look | 1 place fixture | 2 open delivery");
            GUILayout.Label("3/4 remove cola/chips | 5/6 pick cola/chips | 7 stock held");
            GUILayout.Label("8/9 sell cola/chips | D duplicate-ID attempt | C clean | O advance store");
            GUILayout.Label("M move fixture | Backspace remove fixture | F5 capture | F9 restore");
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
            GUILayout.Label("Disk save/exit/reload remains blocked; F5/F9 is the temporary in-memory snapshot only.");
            GUILayout.EndArea();
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
