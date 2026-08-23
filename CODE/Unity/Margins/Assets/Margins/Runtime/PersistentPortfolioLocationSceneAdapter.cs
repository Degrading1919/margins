using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Margins
{
    /// <summary>
    /// Rebinds the one existing detailed-store rig to a persistent generated
    /// portfolio location. The portfolio remains authoritative for identity and
    /// aggregate state; the reused first-store components remain authoritative
    /// for local inventory, checkout, interaction, and operating totals.
    /// </summary>
    public sealed class PersistentPortfolioLocationSceneAdapter : MonoBehaviour
    {
        [SerializeField] private PortfolioProgressionController portfolio;
        [SerializeField] private PersistentPortfolioLocationController locations;
        [SerializeField] private FirstStorePersistenceMapperComponent storePersistence;
        [SerializeField] private StoreOperatingController detailedStore;
        [SerializeField] private FirstStoreInventoryComponent detailedInventory;
        [SerializeField] private PhysicalProductUnitRegistry physicalUnits;
        [SerializeField] private StockingController stocking;
        [SerializeField] private FirstStoreMerchandisingComponent merchandising;
        [SerializeField] private StagedCheckoutInteractionComponent stagedCheckout;
        [SerializeField] private StoreCustomerFlowController customerFlow;
        [SerializeField] private InStoreEmployeeWorkController employeeWork;
        [SerializeField] private FirstPersonController player;

        private readonly Dictionary<string, int> localInventoryBaseline =
            new(StringComparer.Ordinal);
        private readonly List<PortfolioProductInventorySnapshot>
            portfolioInventoryBaseline = new();

        private FirstStoreSnapshot firstStoreSnapshot;
        private string activeLocationId;
        private Vector3 firstStorePlayerPosition;
        private Quaternion firstStorePlayerRotation;
        private bool hasFirstStorePlayerPose;
        private bool customerFlowWasEnabled;
        private bool employeeWorkWasEnabled;
        private int firstStoreOperatingExpensesCents;
        private long firstStorePayrollCents;
        private bool capturedFirstStoreAdapters;
        private string lastSynchronizationError;

        public string ActiveLocationId => activeLocationId;
        public bool HasActiveGeneratedLocation =>
            !string.IsNullOrWhiteSpace(activeLocationId) &&
            !string.Equals(
                activeLocationId,
                PortfolioProgressionRules.FirstLocationId,
                StringComparison.Ordinal);
        public string LastSynchronizationError => lastSynchronizationError;

        public void Configure(
            PortfolioProgressionController progression,
            PersistentPortfolioLocationController locationMaterializer,
            FirstStorePersistenceMapperComponent persistence,
            StoreOperatingController store,
            FirstStoreInventoryComponent inventory,
            PhysicalProductUnitRegistry unitRegistry,
            StockingController stockingController,
            FirstStoreMerchandisingComponent merchandisingAdapter,
            StagedCheckoutInteractionComponent stagedInteraction,
            StoreCustomerFlowController customerController,
            InStoreEmployeeWorkController employeeController,
            FirstPersonController firstPerson)
        {
            portfolio = progression;
            locations = locationMaterializer;
            storePersistence = persistence;
            detailedStore = store;
            detailedInventory = inventory;
            physicalUnits = unitRegistry;
            stocking = stockingController;
            merchandising = merchandisingAdapter;
            stagedCheckout = stagedInteraction;
            customerFlow = customerController;
            employeeWork = employeeController;
            player = firstPerson;
        }

        public bool TryValidateConfiguration(out string error)
        {
            if (portfolio == null || !portfolio.IsInitialized ||
                locations == null || storePersistence == null ||
                detailedStore == null || detailedInventory == null ||
                physicalUnits == null || stocking == null ||
                merchandising == null || stagedCheckout == null ||
                player == null)
            {
                error =
                    "Persistent location travel requires the existing portfolio, generator, persistence, store, inventory, stocking, checkout, and player adapters.";
                return false;
            }

            if (detailedStore.Checkout == null ||
                detailedStore.Stocking != stocking ||
                stocking.InventoryComponent != detailedInventory ||
                stocking.PhysicalUnits != physicalUnits ||
                detailedStore.Checkout.InventoryComponent != detailedInventory ||
                detailedStore.Checkout.PhysicalUnits != physicalUnits ||
                detailedStore.Checkout.Merchandising != merchandising ||
                stagedCheckout.Checkout != detailedStore.Checkout)
            {
                error =
                    "Persistent location travel must reuse one coherent detailed-store authority path.";
                return false;
            }

            if (!storePersistence.TryValidateConfiguration(out error))
            {
                error = $"The reusable detailed-store rig is invalid: {error}";
                return false;
            }

            error = null;
            return true;
        }

        private void Update()
        {
            if (!HasActiveGeneratedLocation)
            {
                return;
            }

            if (!TrySynchronizeActiveLocation(out string error))
            {
                if (!string.IsNullOrWhiteSpace(error) &&
                    !string.Equals(
                        error,
                        lastSynchronizationError,
                        StringComparison.Ordinal))
                {
                    lastSynchronizationError = error;
                    Debug.LogError(
                        $"Persistent detailed-location reconciliation failed: {error}",
                        this);
                }
                return;
            }

            lastSynchronizationError = null;
        }

        public bool TryEnterLocation(string locationId, out string error)
        {
            if (!TryValidateConfiguration(out error) ||
                !FirstStoreIdentifier.IsValid(locationId))
            {
                error ??= "A valid persistent portfolio location is required.";
                return false;
            }

            if (!portfolio.Progression.Locations.Any(value => string.Equals(
                    value.locationId,
                    locationId,
                    StringComparison.Ordinal)))
            {
                error = "The selected location is not an occupied business in this portfolio.";
                return false;
            }

            if (string.Equals(
                    locationId,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal))
            {
                return TryEnterFirstStore(out error);
            }

            if (string.Equals(
                    activeLocationId,
                    locationId,
                    StringComparison.Ordinal))
            {
                error = null;
                return true;
            }

            if (HasActiveGeneratedLocation)
            {
                if (!TryLeaveGeneratedLocation(
                        restoreFirstStore: false,
                        enterFirstStore: false,
                        openManagement: false,
                        out error))
                {
                    return false;
                }
            }
            else if (!TryCaptureFirstStoreForTravel(out error))
            {
                return false;
            }

            PortfolioLocationSnapshot location = portfolio.Progression.Locations
                .First(value => string.Equals(
                    value.locationId,
                    locationId,
                    StringComparison.Ordinal));
            if (!locations.TryMaterializeLocation(locationId, out error))
            {
                return RollBackToFirstStore(error, out error);
            }

            activeLocationId = locationId;
            if (customerFlow != null)
            {
                customerFlow.enabled = false;
            }
            if (employeeWork != null)
            {
                employeeWork.enabled = false;
            }

            bool alreadyChargedToday = location.hasLastReport &&
                                       location.lastReport != null &&
                                       location.lastReport.day ==
                                       portfolio.Progression.CurrentDay;
            if (!merchandising.TryBindDetailedLocation(locationId, out error) ||
                !portfolio.TryConfigureDetailedOperatingCosts(
                    locationId,
                    detailedStore,
                    alreadyChargedToday,
                    out error) ||
                !TryCreateLocationSnapshot(
                    location,
                    out FirstStoreSnapshot detailedSnapshot,
                    out error) ||
                !storePersistence.TryRestore(detailedSnapshot, out error))
            {
                return RollBackToFirstStore(error, out error);
            }

            stagedCheckout.ResetTransientStateAfterRestore();
            DetailedOperationMetricsSnapshot metrics =
                CreateCurrentDetailedMetrics();
            if (!portfolio.TryEstablishDetailedLocationBaseline(
                    locationId,
                    detailedStore,
                    portfolioInventoryBaseline,
                    metrics,
                    out error))
            {
                return RollBackToFirstStore(error, out error);
            }

            if (!alreadyChargedToday &&
                !portfolio.TryConfigureDetailedOperatingCosts(
                    locationId,
                    detailedStore,
                    true,
                    out error))
            {
                return RollBackToFirstStore(error, out error);
            }

            CreateGeneratedInteractionViews();
            TeleportPlayerToGeneratedEntrance();
            player.SetGameplayMode(true);
            error = null;
            return true;
        }

        public bool TryLeaveToManagement(out string error)
        {
            if (HasActiveGeneratedLocation)
            {
                return TryLeaveGeneratedLocation(
                    restoreFirstStore: true,
                    enterFirstStore: false,
                    openManagement: true,
                    out error);
            }

            string detailedLocation = portfolio?.Progression?.CreateSnapshot()
                .company.activeDetailedLocationId;
            if (string.Equals(
                    detailedLocation,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal))
            {
                if (!portfolio.TrySynchronizeDetailedShift(out error) ||
                    !portfolio.Progression.TryLeaveDetailedLocation(
                        PortfolioProgressionRules.FirstLocationId,
                        out error))
                {
                    return false;
                }
            }

            activeLocationId = null;
            player.SetGameplayMode(false);
            error = null;
            return true;
        }

        public bool TrySynchronizeActiveLocation(out string error)
        {
            if (!HasActiveGeneratedLocation)
            {
                error = null;
                return true;
            }

            if (!TryComposePortfolioInventory(
                    out List<PortfolioProductInventorySnapshot> current,
                    out error))
            {
                return false;
            }

            return portfolio.TrySynchronizeDetailedLocation(
                activeLocationId,
                detailedStore,
                current,
                CreateCurrentDetailedMetrics(),
                out _,
                out error);
        }

        private bool TryEnterFirstStore(out string error)
        {
            if (HasActiveGeneratedLocation)
            {
                return TryLeaveGeneratedLocation(
                    restoreFirstStore: true,
                    enterFirstStore: true,
                    openManagement: false,
                    out error);
            }

            if (!merchandising.TryBindDetailedLocation(
                    PortfolioProgressionRules.FirstLocationId,
                    out error) ||
                !portfolio.Progression.TryEnterDetailedLocation(
                    PortfolioProgressionRules.FirstLocationId,
                    out error) ||
                !TryCreateLocalProductInventory(
                    out List<PortfolioProductInventorySnapshot> inventory,
                    out error) ||
                !portfolio.TryEstablishDetailedLocationBaseline(
                    PortfolioProgressionRules.FirstLocationId,
                    detailedStore,
                    inventory,
                    detailedStore.CustomerFlow?.CreateDetailedOperationMetrics(
                        detailedStore.CleaningTask == null ||
                        detailedStore.CleaningTask.IsComplete),
                    out error))
            {
                return false;
            }

            activeLocationId = PortfolioProgressionRules.FirstLocationId;
            player.SetGameplayMode(true);
            error = null;
            return true;
        }

        private bool TryCaptureFirstStoreForTravel(out string error)
        {
            if (!portfolio.TrySynchronizeDetailedShift(out error))
            {
                return false;
            }
            if (storePersistence.TryGetDiskSaveBlocker(out string blocker))
            {
                error = blocker;
                return false;
            }
            if (!storePersistence.TryCapture(out firstStoreSnapshot, out error))
            {
                return false;
            }

            firstStorePlayerPosition = player.transform.position;
            firstStorePlayerRotation = player.transform.rotation;
            hasFirstStorePlayerPose = true;
            firstStoreOperatingExpensesCents =
                detailedStore.IncludedOperatingExpensesCents;
            firstStorePayrollCents = detailedStore.LivePayrollCents;
            customerFlowWasEnabled = customerFlow != null && customerFlow.enabled;
            employeeWorkWasEnabled = employeeWork != null && employeeWork.enabled;
            capturedFirstStoreAdapters = true;

            string active = portfolio.Progression.CreateSnapshot().company
                .activeDetailedLocationId;
            if (string.Equals(
                    active,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal) &&
                !portfolio.Progression.TryLeaveDetailedLocation(
                    active,
                    out error))
            {
                return false;
            }

            activeLocationId = null;
            return true;
        }

        private bool TryLeaveGeneratedLocation(
            bool restoreFirstStore,
            bool enterFirstStore,
            bool openManagement,
            out string error)
        {
            if (!TrySynchronizeActiveLocation(out error))
            {
                return false;
            }

            if (restoreFirstStore &&
                (!capturedFirstStoreAdapters || firstStoreSnapshot == null ||
                 !storePersistence.TryValidateSnapshot(
                     firstStoreSnapshot,
                     out error)))
            {
                error ??= "The captured first-store rig cannot be restored safely.";
                return false;
            }

            if (!locations.TryLeaveActiveLocation(out error))
            {
                return false;
            }

            activeLocationId = null;
            localInventoryBaseline.Clear();
            portfolioInventoryBaseline.Clear();
            if (restoreFirstStore && !TryRestoreFirstStoreRig(out error))
            {
                return false;
            }

            if (enterFirstStore)
            {
                if (!portfolio.Progression.TryEnterDetailedLocation(
                        PortfolioProgressionRules.FirstLocationId,
                        out error) ||
                    !TryCreateLocalProductInventory(
                        out List<PortfolioProductInventorySnapshot> inventory,
                        out error) ||
                    !portfolio.TryEstablishDetailedLocationBaseline(
                        PortfolioProgressionRules.FirstLocationId,
                        detailedStore,
                        inventory,
                        detailedStore.CustomerFlow?.CreateDetailedOperationMetrics(
                            detailedStore.CleaningTask == null ||
                            detailedStore.CleaningTask.IsComplete),
                        out error))
                {
                    return false;
                }
                activeLocationId = PortfolioProgressionRules.FirstLocationId;
            }

            if (hasFirstStorePlayerPose)
            {
                TeleportPlayer(
                    firstStorePlayerPosition,
                    firstStorePlayerRotation);
            }
            player.SetGameplayMode(!openManagement);
            error = null;
            return true;
        }

        private bool TryRestoreFirstStoreRig(out string error)
        {
            if (!merchandising.TryBindDetailedLocation(
                    PortfolioProgressionRules.FirstLocationId,
                    out error) ||
                !detailedStore.TrySetIncludedOperatingExpensesCents(
                    firstStoreOperatingExpensesCents,
                    out error) ||
                !detailedStore.TrySetLivePayrollCents(
                    firstStorePayrollCents,
                    out error) ||
                !storePersistence.TryRestore(firstStoreSnapshot, out error))
            {
                return false;
            }

            stagedCheckout.ResetTransientStateAfterRestore();
            if (customerFlow != null)
            {
                customerFlow.enabled = customerFlowWasEnabled;
            }
            if (employeeWork != null)
            {
                employeeWork.enabled = employeeWorkWasEnabled;
            }
            return true;
        }

        private bool RollBackToFirstStore(
            string failure,
            out string error)
        {
            string rollbackError = null;
            if (!string.IsNullOrWhiteSpace(locations.ActiveLocationId))
            {
                locations.TryLeaveActiveLocation(out _);
            }
            activeLocationId = null;
            localInventoryBaseline.Clear();
            portfolioInventoryBaseline.Clear();
            if (capturedFirstStoreAdapters && firstStoreSnapshot != null &&
                TryRestoreFirstStoreRig(out rollbackError))
            {
                if (hasFirstStorePlayerPose)
                {
                    TeleportPlayer(
                        firstStorePlayerPosition,
                        firstStorePlayerRotation);
                }
                player.SetGameplayMode(false);
                error = failure;
                return false;
            }

            error =
                $"{failure} First-store rollback also failed: {rollbackError}";
            return false;
        }

        private bool TryCreateLocationSnapshot(
            PortfolioLocationSnapshot location,
            out FirstStoreSnapshot snapshot,
            out string error)
        {
            snapshot = JsonUtility.FromJson<FirstStoreSnapshot>(
                JsonUtility.ToJson(firstStoreSnapshot));
            if (snapshot?.inventory?.locations == null ||
                snapshot.transactionLedger == null ||
                snapshot.storeOperating == null)
            {
                error = "The captured detailed-store template is incomplete.";
                return false;
            }

            portfolioInventoryBaseline.Clear();
            foreach (PortfolioProductInventorySnapshot product in
                     location.productInventory.OrderBy(
                         value => value.productId,
                         StringComparer.Ordinal))
            {
                portfolioInventoryBaseline.Add(
                    PortfolioOperationsRules.Clone(product));
            }

            foreach (InventoryLocationSnapshot inventoryLocation in
                     snapshot.inventory.locations)
            {
                inventoryLocation.quantities.Clear();
            }
            snapshot.deliveryContainers = snapshot.deliveryContainers
                .Select(value => new DeliveryContainerSnapshot(
                    value.containerId,
                    value.inventoryLocationId,
                    false))
                .ToList();
            snapshot.transactionLedger = new CompletedTransactionLedgerSnapshot(
                snapshot.transactionLedger.maximumTransactionCount);
            snapshot.storeOperating = new StoreOperatingSnapshot(
                detailedStore.StableSessionId,
                StoreOperatingState.Open,
                false,
                null);
            if (snapshot.cleaningTask != null)
            {
                snapshot.cleaningTask.completedProgressUnits =
                    snapshot.cleaningTask.requiredProgressUnits;
            }
            snapshot.customerFlow = StoreCustomerFlowSnapshot.Empty();
            snapshot.customerFlow.secondsUntilNextArrival = 3_600f;
            snapshot.physicalProductUnits.Clear();
            localInventoryBaseline.Clear();

            int nextPhysicalOrdinal = 1;
            List<StockingProductConfiguration> shelfSlots = stocking
                .AuthoredProductMappings
                .Where(value => value?.ShelfFixture != null)
                .GroupBy(
                    value => value.ShelfFixture.StableFixtureId,
                    StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(
                    value => value.ShelfFixture.StableFixtureId,
                    StringComparer.Ordinal)
                .ToList();
            foreach (StockingProductConfiguration slot in shelfSlots)
            {
                if (!merchandising.TryGetOfferForShelf(
                        slot.ShelfFixture.StableFixtureId,
                        out MerchandiseOffer offer))
                {
                    continue;
                }

                PortfolioProductInventorySnapshot product =
                    portfolioInventoryBaseline.FirstOrDefault(value =>
                        string.Equals(
                            value.productId,
                            offer.ProductId,
                            StringComparison.Ordinal));
                InventoryLocationSnapshot shelf = snapshot.inventory.locations
                    .FirstOrDefault(value => string.Equals(
                        value.locationId,
                        slot.ShelfLocationId,
                        StringComparison.Ordinal));
                if (product == null || shelf == null ||
                    shelf.kind != InventoryLocationKind.Shelf)
                {
                    error =
                        $"Persistent product '{offer.ProductId}' has no compatible detailed shelf.";
                    return false;
                }

                int physicalCapacity = Math.Min(
                    slot.SnapPointIds.Count,
                    shelf.capacityUnits < 0
                        ? slot.SnapPointIds.Count
                        : shelf.capacityUnits);
                int quantity = Math.Min(
                    product.quantityUnits,
                    physicalCapacity);
                localInventoryBaseline.TryGetValue(
                    product.productId,
                    out int priorQuantity);
                localInventoryBaseline[product.productId] = checked(
                    priorQuantity + quantity);
                if (quantity > 0)
                {
                    shelf.quantities.Add(new InventoryQuantitySnapshot(
                        product.productId,
                        quantity));
                }

                for (int index = 0; index < quantity; index++)
                {
                    string physicalId =
                        $"physical-unit-{nextPhysicalOrdinal:D6}";
                    snapshot.physicalProductUnits.Add(
                        new PhysicalProductUnitSnapshot(
                            physicalId,
                            product.productId,
                            slot.ShelfLocationId,
                            slot.ShelfFixture.StableFixtureId,
                            slot.SnapPointIds[index],
                            0));
                    nextPhysicalOrdinal++;
                }
            }

            foreach (PortfolioProductInventorySnapshot product in
                     portfolioInventoryBaseline)
            {
                localInventoryBaseline.TryAdd(product.productId, 0);
            }
            foreach (InventoryLocationSnapshot inventoryLocation in
                     snapshot.inventory.locations)
            {
                inventoryLocation.quantities.Sort((left, right) =>
                    string.CompareOrdinal(left.productId, right.productId));
            }
            snapshot.nextPhysicalUnitOrdinal = nextPhysicalOrdinal;
            error = null;
            return true;
        }

        private bool TryComposePortfolioInventory(
            out List<PortfolioProductInventorySnapshot> result,
            out string error)
        {
            result = null;
            if (!TryCreateLocalProductInventory(
                    out List<PortfolioProductInventorySnapshot> local,
                    out error))
            {
                return false;
            }

            Dictionary<string, PortfolioProductInventorySnapshot> localById =
                local.ToDictionary(
                    value => value.productId,
                    StringComparer.Ordinal);
            result = new List<PortfolioProductInventorySnapshot>(
                portfolioInventoryBaseline.Count);
            try
            {
                foreach (PortfolioProductInventorySnapshot baseline in
                         portfolioInventoryBaseline)
                {
                    localInventoryBaseline.TryGetValue(
                        baseline.productId,
                        out int stagedQuantity);
                    int currentQuantity = localById.TryGetValue(
                        baseline.productId,
                        out PortfolioProductInventorySnapshot localProduct)
                        ? localProduct.quantityUnits
                        : 0;
                    int reconciled = checked(
                        baseline.quantityUnits +
                        currentQuantity - stagedQuantity);
                    if (reconciled < 0)
                    {
                        error =
                            $"Detailed inventory for '{baseline.productId}' moved below its persistent reserve.";
                        result = null;
                        return false;
                    }
                    result.Add(new PortfolioProductInventorySnapshot
                    {
                        productId = baseline.productId,
                        quantityUnits = reconciled,
                        unitCostCents = baseline.unitCostCents
                    });
                }
            }
            catch (OverflowException)
            {
                error = "Detailed inventory reconciliation overflowed storage.";
                result = null;
                return false;
            }

            error = null;
            return true;
        }

        private bool TryCreateLocalProductInventory(
            out List<PortfolioProductInventorySnapshot> result,
            out string error)
        {
            result = null;
            FirstStoreInventorySnapshot inventory =
                detailedInventory.Inventory?.CreateSnapshot();
            IReadOnlyDictionary<string, int> unitCosts =
                detailedStore.Checkout.ProductUnitCostsCents;
            if (inventory?.locations == null || unitCosts == null)
            {
                error = "The reusable detailed inventory is unavailable.";
                return false;
            }

            Dictionary<string, int> quantities = unitCosts.Keys.ToDictionary(
                value => value,
                _ => 0,
                StringComparer.Ordinal);
            try
            {
                foreach (InventoryLocationSnapshot location in inventory.locations)
                {
                    foreach (InventoryQuantitySnapshot quantity in
                             location.quantities)
                    {
                        if (!quantities.ContainsKey(quantity.productId))
                        {
                            error =
                                $"Detailed inventory product '{quantity.productId}' is outside the checkout catalog.";
                            return false;
                        }
                        quantities[quantity.productId] = checked(
                            quantities[quantity.productId] +
                            quantity.quantityUnits);
                    }
                }
            }
            catch (OverflowException)
            {
                error = "Detailed inventory quantities overflowed storage.";
                return false;
            }

            result = quantities
                .OrderBy(value => value.Key, StringComparer.Ordinal)
                .Select(value => new PortfolioProductInventorySnapshot
                {
                    productId = value.Key,
                    quantityUnits = value.Value,
                    unitCostCents = unitCosts[value.Key]
                })
                .ToList();
            error = null;
            return true;
        }

        private DetailedOperationMetricsSnapshot CreateCurrentDetailedMetrics()
        {
            return customerFlow?.CreateDetailedOperationMetrics(
                       detailedStore.CleaningTask == null ||
                       detailedStore.CleaningTask.IsComplete) ??
                   new DetailedOperationMetricsSnapshot
                   {
                       customerVisits = detailedStore.CurrentTotals?
                           .transactionCount ?? 0,
                       customersServed = detailedStore.CurrentTotals?
                           .transactionCount ?? 0,
                       requestedProductUnits = detailedStore.CurrentTotals?
                           .unitsSold ?? 0,
                       standardsTaskComplete =
                           detailedStore.CleaningTask == null ||
                           detailedStore.CleaningTask.IsComplete
                   };
        }

        private void CreateGeneratedInteractionViews()
        {
            ProceduralCommercialBuilding building = locations.ActiveBuilding;
            if (building?.LastResult == null)
            {
                return;
            }

            GeneratedAssetPlacement checkoutPlacement = building.LastResult
                .Placements.FirstOrDefault(value =>
                    value.Category == ProceduralAssetCategory.Transaction);
            Transform checkoutRoot = checkoutPlacement == null
                ? building.transform
                : building.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(value => string.Equals(
                        value.name,
                        checkoutPlacement.PlacementId,
                        StringComparison.Ordinal)) ?? building.transform;
            StagedCheckoutWorldInteractionTarget checkoutTarget =
                checkoutRoot.gameObject.GetComponent<
                    StagedCheckoutWorldInteractionTarget>() ??
                checkoutRoot.gameObject.AddComponent<
                    StagedCheckoutWorldInteractionTarget>();
            checkoutTarget.Configure(
                $"target-persistent-checkout-{activeLocationId}",
                stagedCheckout,
                detailedStore);

            int productIndex = 0;
            foreach (ProductDefinition product in merchandising.ProductCatalog)
            {
                GameObject prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                prop.name = $"Persistent Checkout Item {product.StableProductId}";
                prop.transform.SetParent(checkoutRoot, false);
                prop.transform.localPosition = new Vector3(
                    -0.22f + productIndex * 0.44f,
                    0.95f,
                    -0.2f);
                prop.transform.localScale = new Vector3(0.22f, 0.28f, 0.18f);
                CheckoutProductWorldInteractionTarget productTarget =
                    prop.AddComponent<CheckoutProductWorldInteractionTarget>();
                productTarget.Configure(
                    $"target-persistent-item-{productIndex + 1:D2}",
                    product,
                    stagedCheckout,
                    detailedStore);
                productIndex++;
            }

            if (!TryGetEntrancePose(
                    building,
                    0.55f,
                    out Vector3 exitPosition,
                    out Quaternion exitRotation))
            {
                return;
            }
            GameObject exit = GameObject.CreatePrimitive(PrimitiveType.Cube);
            exit.name = "Persistent Location Exit";
            exit.transform.SetParent(building.transform, false);
            exit.transform.SetPositionAndRotation(exitPosition, exitRotation);
            exit.transform.localScale = new Vector3(1.1f, 2f, 0.12f);
            PersistentLocationExitWorldInteractionTarget exitTarget =
                exit.AddComponent<PersistentLocationExitWorldInteractionTarget>();
            exitTarget.Configure(
                $"target-persistent-exit-{activeLocationId}",
                this);
        }

        private void TeleportPlayerToGeneratedEntrance()
        {
            if (TryGetEntrancePose(
                    locations.ActiveBuilding,
                    1.8f,
                    out Vector3 position,
                    out Quaternion rotation))
            {
                TeleportPlayer(position, rotation);
            }
        }

        private static bool TryGetEntrancePose(
            ProceduralCommercialBuilding building,
            float inwardDistance,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = default;
            rotation = Quaternion.identity;
            ProceduralGenerationResult result = building?.LastResult;
            GeneratedOpening entrance = result?.Openings.FirstOrDefault(value =>
                value.Kind == ProceduralOpeningKind.PrimaryEntrance);
            GeneratedWallSegment wall = entrance == null
                ? null
                : result.Walls.FirstOrDefault(value => string.Equals(
                    value.WallId,
                    entrance.WallId,
                    StringComparison.Ordinal));
            if (wall == null)
            {
                return false;
            }

            Vector2 local = wall.InteriorFacePointAt(entrance.OffsetMeters) +
                            wall.InwardNormal * inwardDistance;
            position = building.transform.TransformPoint(
                new Vector3(local.x, 1.05f, local.y));
            Vector3 forward = building.transform.TransformDirection(
                new Vector3(wall.InwardNormal.x, 0f, wall.InwardNormal.y));
            rotation = forward.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(forward, Vector3.up)
                : building.transform.rotation;
            return true;
        }

        private void TeleportPlayer(Vector3 position, Quaternion rotation)
        {
            CharacterController character = player.GetComponent<CharacterController>();
            bool wasEnabled = character != null && character.enabled;
            if (wasEnabled)
            {
                character.enabled = false;
            }
            player.transform.SetPositionAndRotation(position, rotation);
            if (wasEnabled)
            {
                character.enabled = true;
            }
        }
    }
}
