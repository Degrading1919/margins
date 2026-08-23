using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

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
        [SerializeField] private DeliveryBoxComponent deliveryBox;
        [SerializeField] private CleaningTaskComponent cleaningTask;
        [SerializeField] private CleaningWorldInteractionTarget cleaningTarget;
        [SerializeField] private CarryableToolComponent cleaningTool;
        [SerializeField] private StoreOperatingWorldInteractionTarget
            operatingControl;
        [SerializeField] private FirstPersonController player;

        private readonly Dictionary<string, int> localInventoryBaseline =
            new(StringComparer.Ordinal);
        private readonly List<PortfolioProductInventorySnapshot>
            portfolioInventoryBaseline = new();

        private FirstStoreSnapshot firstStoreSnapshot;
        private string activeLocationId;
        private FirstStorePlayerTransformSnapshot firstStorePlayerTransform;
        private bool hasFirstStorePlayerPose;
        private bool customerFlowWasEnabled;
        private bool employeeWorkWasEnabled;
        private int firstStoreOperatingExpensesCents;
        private long firstStorePayrollCents;
        private bool capturedFirstStoreAdapters;
        private string lastSynchronizationError;
        private StoreCustomerFlowLocationBindings firstStoreCustomerBindings;
        private InStoreEmployeeLocationBindings firstStoreEmployeeBindings;
        private readonly List<TransformState> firstStoreTransformStates = new();
        private GeneratedDetailedLocationBindings activeBindings;
        private NavMeshSurface activeNavigationSurface;

        private sealed class TransformState
        {
            public Transform Target;
            public Transform Parent;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 LocalScale;
            public bool ActiveSelf;
        }

        public string ActiveLocationId => activeLocationId;
        public bool HasActiveGeneratedLocation =>
            !string.IsNullOrWhiteSpace(activeLocationId) &&
            !string.Equals(
                activeLocationId,
                PortfolioProgressionRules.FirstLocationId,
                StringComparison.Ordinal);
        public string LastSynchronizationError => lastSynchronizationError;
        public GeneratedDetailedLocationBindings ActiveBindings =>
            activeBindings;
        public bool HasActiveGeneratedNavigation =>
            activeNavigationSurface != null &&
            activeNavigationSurface.navMeshData != null;

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
            DeliveryBoxComponent detailedDeliveryBox,
            CleaningTaskComponent detailedCleaningTask,
            CleaningWorldInteractionTarget detailedCleaningTarget,
            CarryableToolComponent detailedCleaningTool,
            StoreOperatingWorldInteractionTarget detailedOperatingControl,
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
            deliveryBox = detailedDeliveryBox;
            cleaningTask = detailedCleaningTask;
            cleaningTarget = detailedCleaningTarget;
            cleaningTool = detailedCleaningTool;
            operatingControl = detailedOperatingControl;
            player = firstPerson;
        }

        public bool TryValidateConfiguration(out string error)
        {
            error = null;
            if (portfolio == null || !portfolio.IsInitialized ||
                locations == null || storePersistence == null ||
                detailedStore == null || detailedInventory == null ||
                physicalUnits == null || stocking == null ||
                merchandising == null || stagedCheckout == null ||
                customerFlow == null || employeeWork == null ||
                deliveryBox == null || cleaningTask == null ||
                cleaningTarget == null || cleaningTool == null ||
                operatingControl == null || player == null)
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

            if (detailedStore.CustomerFlow != customerFlow ||
                detailedStore.EmployeeWork != employeeWork ||
                detailedStore.CleaningTask != cleaningTask ||
                employeeWork.CustomerFlow != customerFlow ||
                deliveryBox.InventoryComponent != detailedInventory ||
                deliveryBox.PhysicalUnits != physicalUnits ||
                !customerFlow.TryValidateConfiguration(out error) ||
                !employeeWork.TryValidateConfiguration(out error) ||
                !deliveryBox.TryValidateConfiguration(out error) ||
                !cleaningTool.TryValidateConfiguration(out error))
            {
                error ??=
                    "Generated detailed operation must reuse the configured customer, employee, delivery, cleaning, and tool authorities.";
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

            if (!TryActivateGeneratedDetailedRig(locationId, out error))
            {
                return RollBackToFirstStore(error, out error);
            }

            CreateGeneratedInteractionViews();
            TeleportPlayerToGeneratedEntrance();
            player.SetGameplayMode(true);
            error = null;
            return true;
        }

        public bool TryCapturePersistenceState(
            out FirstStoreSnapshot parkedFirstStore,
            out PersistentGeneratedLocationDiskSnapshot generatedLocation,
            out string error)
        {
            parkedFirstStore = null;
            generatedLocation = null;
            if (!HasActiveGeneratedLocation)
            {
                error =
                    "Generated-location persistence capture requires an active generated location.";
                return false;
            }

            if (cleaningTool.IsCarried)
            {
                error = "Set down the cleaning tool before saving.";
                return false;
            }
            if (storePersistence.TryGetDiskSaveBlocker(out error) ||
                !portfolio.TrySynchronizeDetailedProcurement(out error) ||
                !TrySynchronizeActiveLocation(out error) ||
                !storePersistence.TryCapture(
                    out FirstStoreSnapshot activeDetailedStore,
                    out error))
            {
                return false;
            }
            if (firstStoreSnapshot == null || !capturedFirstStoreAdapters ||
                !storePersistence.TryValidateSnapshot(
                    firstStoreSnapshot,
                    out error))
            {
                error ??=
                    "The parked first-store state is unavailable for generated-location persistence.";
                return false;
            }

            parkedFirstStore = CloneSnapshot(firstStoreSnapshot);
            generatedLocation = new PersistentGeneratedLocationDiskSnapshot
            {
                locationId = activeLocationId,
                detailedStore = CloneSnapshot(activeDetailedStore),
                firstStoreReturnTransform = firstStorePlayerTransform,
                customerFlowEnabled = customerFlowWasEnabled,
                employeeWorkEnabled = employeeWorkWasEnabled
            };
            error = null;
            return true;
        }

        public bool TryResetForPersistenceRestore(out string error)
        {
            error = null;
            if (!HasActiveGeneratedLocation)
            {
                activeLocationId = null;
                return true;
            }

            if (!locations.TryLeaveActiveLocation(out error))
            {
                return false;
            }

            TeardownGeneratedDetailedRig();
            activeLocationId = null;
            localInventoryBaseline.Clear();
            portfolioInventoryBaseline.Clear();
            if (!TryRestoreFirstStoreRig(out error))
            {
                return false;
            }

            player.SetGameplayMode(false);
            return true;
        }

        public bool TryRestorePersistenceState(
            PersistentGeneratedLocationDiskSnapshot savedLocation,
            out string error)
        {
            if (!TryValidateConfiguration(out error) ||
                savedLocation == null ||
                !FirstStoreIdentifier.IsValid(savedLocation.locationId) ||
                string.Equals(
                    savedLocation.locationId,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal) ||
                savedLocation.detailedStore == null ||
                !storePersistence.TryValidateSnapshot(
                    savedLocation.detailedStore,
                    out error) ||
                !player.TryPreflightApplyTransformSnapshot(
                    savedLocation.firstStoreReturnTransform,
                    out error))
            {
                error ??=
                    "A valid generated detailed-location persistence snapshot is required.";
                return false;
            }
            if (HasActiveGeneratedLocation)
            {
                error =
                    "Reset the current generated location before restoring another detailed location.";
                return false;
            }

            PortfolioProgressionSnapshot company =
                portfolio.Progression.CreateSnapshot();
            if (!string.Equals(
                    company.company.activeDetailedLocationId,
                    savedLocation.locationId,
                    StringComparison.Ordinal))
            {
                error =
                    "The saved generated location does not match the portfolio's active detailed location.";
                return false;
            }
            PortfolioLocationSnapshot location = company.locations
                .FirstOrDefault(value => string.Equals(
                    value.locationId,
                    savedLocation.locationId,
                    StringComparison.Ordinal));
            if (location == null)
            {
                error =
                    "The saved generated location is not an occupied portfolio business.";
                return false;
            }

            if (!TryCaptureFirstStoreRigState(out error) ||
                !locations.TryMaterializeLocation(
                    savedLocation.locationId,
                    out error))
            {
                return RollBackToFirstStore(error, out error);
            }

            firstStorePlayerTransform =
                savedLocation.firstStoreReturnTransform;
            hasFirstStorePlayerPose = true;
            customerFlowWasEnabled = savedLocation.customerFlowEnabled;
            employeeWorkWasEnabled = savedLocation.employeeWorkEnabled;
            activeLocationId = savedLocation.locationId;
            customerFlow.enabled = false;
            employeeWork.enabled = false;

            bool alreadyChargedToday = location.hasLastReport &&
                                       location.lastReport != null &&
                                       location.lastReport.day ==
                                       portfolio.Progression.CurrentDay;
            if (!merchandising.TryBindDetailedLocation(
                    savedLocation.locationId,
                    out error) ||
                !portfolio.TryConfigureDetailedOperatingCosts(
                    savedLocation.locationId,
                    detailedStore,
                    alreadyChargedToday,
                    out error) ||
                !TryActivateGeneratedDetailedRig(
                    savedLocation.locationId,
                    false,
                    out error) ||
                !storePersistence.TryRestore(
                    savedLocation.detailedStore,
                    out error))
            {
                return RollBackToFirstStore(error, out error);
            }

            portfolioInventoryBaseline.Clear();
            portfolioInventoryBaseline.AddRange(
                location.productInventory
                    .Select(PortfolioOperationsRules.Clone));
            if (!TryCreateLocalProductInventory(
                    out List<PortfolioProductInventorySnapshot> local,
                    out error))
            {
                return RollBackToFirstStore(error, out error);
            }
            localInventoryBaseline.Clear();
            foreach (PortfolioProductInventorySnapshot product in local)
            {
                localInventoryBaseline.Add(
                    product.productId,
                    product.quantityUnits);
            }

            if (!portfolio.TryEstablishDetailedLocationBaseline(
                    savedLocation.locationId,
                    detailedStore,
                    portfolioInventoryBaseline,
                    CreateCurrentDetailedMetrics(),
                    out error) ||
                (!alreadyChargedToday &&
                 !portfolio.TryConfigureDetailedOperatingCosts(
                     savedLocation.locationId,
                     detailedStore,
                     true,
                     out error)) ||
                !TryActivateGeneratedDetailedRig(
                    savedLocation.locationId,
                    true,
                    out error))
            {
                return RollBackToFirstStore(error, out error);
            }

            CreateGeneratedInteractionViews();
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

        internal bool TryStageDetailedDeliveryOverflow(
            string locationId,
            IReadOnlyDictionary<string, int> overflowByProduct,
            out string error)
        {
            error = null;
            if (!HasActiveGeneratedLocation ||
                !string.Equals(
                    activeLocationId,
                    locationId,
                    StringComparison.Ordinal) ||
                overflowByProduct == null)
            {
                error =
                    "Detailed delivery overflow requires the matching active generated location.";
                return false;
            }

            List<PortfolioProductInventorySnapshot> candidate =
                portfolioInventoryBaseline
                    .Select(PortfolioOperationsRules.Clone)
                    .ToList();
            try
            {
                foreach (KeyValuePair<string, int> overflow in
                         overflowByProduct)
                {
                    if (overflow.Value < 0)
                    {
                        error =
                            $"Detailed delivery overflow for '{overflow.Key}' is negative.";
                        return false;
                    }
                    if (overflow.Value == 0)
                    {
                        continue;
                    }

                    PortfolioProductInventorySnapshot product = candidate
                        .FirstOrDefault(value => string.Equals(
                            value.productId,
                            overflow.Key,
                            StringComparison.Ordinal));
                    if (product == null)
                    {
                        error =
                            $"Detailed delivery overflow product '{overflow.Key}' is outside the active merchandise catalog.";
                        return false;
                    }
                    product.quantityUnits = checked(
                        product.quantityUnits + overflow.Value);
                }

                PortfolioLocationSnapshot location = portfolio.Progression
                    .Locations.FirstOrDefault(value => string.Equals(
                        value.locationId,
                        locationId,
                        StringComparison.Ordinal));
                if (location == null ||
                    !TryCreateLocalProductInventory(
                        out List<PortfolioProductInventorySnapshot> local,
                        out error))
                {
                    error ??=
                        "The active detailed inventory is unavailable for delivery overflow.";
                    return false;
                }
                int reconciledUnits = checked(
                    candidate.Sum(value => value.quantityUnits) +
                    local.Sum(value => value.quantityUnits) -
                    localInventoryBaseline.Values.Sum());
                if (reconciledUnits > location.inventoryCapacityUnits)
                {
                    error =
                        "Detailed delivery overflow exceeds the persistent location's inventory capacity.";
                    return false;
                }
            }
            catch (OverflowException)
            {
                error = "Detailed delivery overflow exceeded integer storage.";
                return false;
            }

            portfolioInventoryBaseline.Clear();
            portfolioInventoryBaseline.AddRange(candidate);
            return true;
        }

        internal bool TryCaptureDetailedDeliveryInventoryBaseline(
            string locationId,
            out List<PortfolioProductInventorySnapshot> snapshot,
            out string error)
        {
            snapshot = null;
            if (!HasActiveGeneratedLocation ||
                !string.Equals(
                    activeLocationId,
                    locationId,
                    StringComparison.Ordinal))
            {
                error =
                    "Detailed delivery inventory capture requires the matching active generated location.";
                return false;
            }

            snapshot = portfolioInventoryBaseline
                .Select(PortfolioOperationsRules.Clone)
                .ToList();
            error = null;
            return true;
        }

        internal bool TryRestoreDetailedDeliveryInventoryBaseline(
            string locationId,
            IReadOnlyList<PortfolioProductInventorySnapshot> snapshot,
            out string error)
        {
            if (!HasActiveGeneratedLocation ||
                !string.Equals(
                    activeLocationId,
                    locationId,
                    StringComparison.Ordinal) ||
                snapshot == null)
            {
                error =
                    "Detailed delivery inventory rollback requires the matching active generated location and captured baseline.";
                return false;
            }

            List<PortfolioProductInventorySnapshot> candidate = snapshot
                .Select(PortfolioOperationsRules.Clone)
                .ToList();
            HashSet<string> productIds = new(StringComparer.Ordinal);
            if (candidate.Any(value =>
                    value == null ||
                    !StableIdentifier.IsValid(value.productId) ||
                    value.quantityUnits < 0 ||
                    value.unitCostCents < 0 ||
                    !productIds.Add(value.productId)) ||
                !productIds.SetEquals(portfolioInventoryBaseline.Select(value =>
                    value.productId)))
            {
                error =
                    "Captured detailed delivery inventory baseline is invalid or no longer matches the active merchandise catalog.";
                return false;
            }

            portfolioInventoryBaseline.Clear();
            portfolioInventoryBaseline.AddRange(candidate);
            error = null;
            return true;
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
            if (employeeWork.IsHandlingInventory)
            {
                error =
                    "Wait for the current employee inventory move to finish before changing locations.";
                return false;
            }
            if (deliveryBox.IsCarried)
            {
                error = "Set down the delivery container before changing locations.";
                return false;
            }
            if (cleaningTool.IsCarried)
            {
                error = "Set down the cleaning tool before changing locations.";
                return false;
            }
            if (!portfolio.TrySynchronizeDetailedShift(out error))
            {
                return false;
            }
            if (storePersistence.TryGetDiskSaveBlocker(out string blocker))
            {
                error = blocker;
                return false;
            }
            if (!TryCaptureFirstStoreRigState(out error))
            {
                return false;
            }

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

        private bool TryCaptureFirstStoreRigState(out string error)
        {
            if (!storePersistence.TryCapture(out firstStoreSnapshot, out error))
            {
                return false;
            }

            firstStorePlayerTransform = player.CaptureTransformSnapshot();
            hasFirstStorePlayerPose = true;
            firstStoreOperatingExpensesCents =
                detailedStore.IncludedOperatingExpensesCents;
            firstStorePayrollCents = detailedStore.LivePayrollCents;
            customerFlowWasEnabled = customerFlow != null && customerFlow.enabled;
            employeeWorkWasEnabled = employeeWork != null && employeeWork.enabled;
            firstStoreCustomerBindings = customerFlow.CaptureLocationBindings();
            firstStoreEmployeeBindings = employeeWork.CaptureLocationBindings();
            if (!TryCaptureFirstStoreTransformStates(out error))
            {
                return false;
            }
            capturedFirstStoreAdapters = true;
            error = null;
            return true;
        }

        private bool TryLeaveGeneratedLocation(
            bool restoreFirstStore,
            bool enterFirstStore,
            bool openManagement,
            out string error)
        {
            if (customerFlow.TryGetDetailedLocationChangeBlocker(
                    out string customerBlocker))
            {
                error = customerBlocker;
                return false;
            }
            if (employeeWork.IsHandlingInventory || deliveryBox.IsCarried)
            {
                error =
                    "Finish the current delivery or stocking move before leaving this location.";
                return false;
            }
            if (cleaningTool.IsCarried)
            {
                error = "Set down the cleaning tool before leaving this location.";
                return false;
            }
            if (!portfolio.TrySynchronizeDetailedProcurement(out error))
            {
                return false;
            }
            PurchaseOrderSnapshot receivingOrder = portfolio.Progression
                .PurchaseOrders.FirstOrDefault(value =>
                    string.Equals(
                        value.locationId,
                        activeLocationId,
                        StringComparison.Ordinal) &&
                    (value.status == PurchaseOrderStatus.Delivered ||
                     value.status == PurchaseOrderStatus.PartiallyReceived));
            if (receivingOrder != null)
            {
                error =
                    $"Receive every unit in {receivingOrder.orderId} before leaving this detailed location.";
                return false;
            }
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

            TeardownGeneratedDetailedRig();
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
                if (!player.TryApplyTransformSnapshot(
                        firstStorePlayerTransform,
                        out error))
                {
                    return false;
                }
            }
            player.SetGameplayMode(!openManagement);
            error = null;
            return true;
        }

        private bool TryRestoreFirstStoreRig(out string error)
        {
            error = null;
            if (firstStoreCustomerBindings == null ||
                firstStoreEmployeeBindings == null ||
                !customerFlow.TryBindDetailedLocation(
                    firstStoreCustomerBindings,
                    out error) ||
                !employeeWork.TryBindDetailedLocation(
                    PortfolioProgressionRules.FirstLocationId,
                    firstStoreEmployeeBindings,
                    out error) ||
                !merchandising.TryBindDetailedLocation(
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
            employeeWork.PlaceAvatarsAtBoundWorkplaces();
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
            TeardownGeneratedDetailedRig();
            activeLocationId = null;
            localInventoryBaseline.Clear();
            portfolioInventoryBaseline.Clear();
            if (capturedFirstStoreAdapters && firstStoreSnapshot != null &&
                TryRestoreFirstStoreRig(out rollbackError))
            {
                if (hasFirstStorePlayerPose)
                {
                    player.TryApplyTransformSnapshot(
                        firstStorePlayerTransform,
                        out _);
                }
                player.SetGameplayMode(false);
                error = failure;
                return false;
            }

            error =
                $"{failure} First-store rollback also failed: {rollbackError}";
            return false;
        }

        private bool TryCaptureFirstStoreTransformStates(out string error)
        {
            firstStoreTransformStates.Clear();
            PlaceableFixtureComponent checkoutFixture =
                firstStoreCustomerBindings?.CheckoutCustomerPoint?
                    .GetComponentInParent<PlaceableFixtureComponent>();
            if (checkoutFixture == null)
            {
                error =
                    "The reusable customer checkout is not attached to an authored fixture.";
                return false;
            }

            List<Transform> targets = new()
            {
                checkoutFixture.transform,
                deliveryBox.transform,
                cleaningTarget.transform,
                cleaningTool.transform,
                operatingControl.transform
            };
            targets.AddRange(stocking.AuthoredProductMappings
                .Where(value => value?.ShelfFixture != null)
                .Select(value => value.ShelfFixture.transform)
                .Distinct());
            targets.AddRange(physicalUnits.ProductConfigurations
                .Where(value => value?.LooseSpawnPoint != null)
                .Select(value => value.LooseSpawnPoint));

            foreach (Transform target in targets.Distinct())
            {
                firstStoreTransformStates.Add(new TransformState
                {
                    Target = target,
                    Parent = target.parent,
                    Position = target.position,
                    Rotation = target.rotation,
                    LocalScale = target.localScale,
                    ActiveSelf = target.gameObject.activeSelf
                });
            }

            error = null;
            return true;
        }

        private bool TryActivateGeneratedDetailedRig(
            string locationId,
            out string error)
        {
            return TryActivateGeneratedDetailedRig(
                locationId,
                false,
                out error);
        }

        private bool TryActivateGeneratedDetailedRig(
            string locationId,
            bool allowRestoredCustomers,
            out string error)
        {
            error = null;
            RestoreFirstStoreTransformStates();
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
            if (!GeneratedDetailedLocationBindings.TryCreate(
                    locations.ActiveBuilding,
                    shelfSlots.Count,
                    out activeBindings,
                    out error))
            {
                return false;
            }

            PlaceableFixtureComponent checkoutFixture =
                firstStoreCustomerBindings.CheckoutCustomerPoint
                    .GetComponentInParent<PlaceableFixtureComponent>();
            if (checkoutFixture == null ||
                !TryMoveFixture(
                    checkoutFixture,
                    activeBindings.CheckoutFixture,
                    out error))
            {
                error ??=
                    "The authored checkout could not bind to the generated payment asset.";
                return false;
            }

            for (int index = 0; index < shelfSlots.Count; index++)
            {
                if (!TryMoveFixture(
                        shelfSlots[index].ShelfFixture
                            .GetComponent<PlaceableFixtureComponent>(),
                        activeBindings.DisplayFixtures[index],
                        out error))
                {
                    error ??=
                        $"Authored shelf '{shelfSlots[index].ShelfFixture.StableFixtureId}' could not bind to generated display metadata.";
                    return false;
                }
            }

            deliveryBox.transform.SetPositionAndRotation(
                activeBindings.DeliveryDropPoint.position,
                locations.ActiveBuilding.transform.rotation);
            int looseSpawnIndex = 0;
            foreach (PhysicalProductUnitConfiguration configuration in
                     physicalUnits.ProductConfigurations
                         .Where(value => value?.LooseSpawnPoint != null)
                         .OrderBy(
                             value => value.ProductDefinition.StableProductId,
                             StringComparer.Ordinal))
            {
                configuration.LooseSpawnPoint.SetPositionAndRotation(
                    activeBindings.DeliveryWorkPoint.position +
                    locations.ActiveBuilding.transform.right *
                    (looseSpawnIndex * 0.35f - 0.175f),
                    locations.ActiveBuilding.transform.rotation);
                looseSpawnIndex++;
            }
            Physics.SyncTransforms();
            if (!TryBuildGeneratedNavigation(out error))
            {
                return false;
            }

            cleaningTarget.transform.SetPositionAndRotation(
                activeBindings.CleaningPoint.position,
                locations.ActiveBuilding.transform.rotation);
            cleaningTool.transform.SetPositionAndRotation(
                activeBindings.ToolRestPoint.position,
                locations.ActiveBuilding.transform.rotation);
            operatingControl.transform.SetPositionAndRotation(
                activeBindings.OperatingControlPoint.position,
                locations.ActiveBuilding.transform.rotation);

            StoreCustomerFlowLocationBindings customerBindings = new(
                activeBindings.EntrancePoint,
                activeBindings.ExitPoint,
                activeBindings.CheckoutCustomerPoint,
                activeBindings.CheckoutItemPoints,
                activeBindings.BrowsePoints,
                activeBindings.QueuePoints);
            InStoreEmployeeLocationBindings employeeBindings = new(
                activeBindings.CashierWorkPoint,
                activeBindings.DeliveryWorkPoint,
                activeBindings.DeliveryDropPoint,
                activeBindings.BrowsePoints[0],
                activeBindings.ManagerWorkPoint);
            bool customerBound = allowRestoredCustomers
                ? customerFlow.TryBindDetailedLocationAfterRestore(
                    customerBindings,
                    out error)
                : customerFlow.TryBindDetailedLocation(
                    customerBindings,
                    out error);
            if (!customerBound ||
                !employeeWork.TryBindDetailedLocation(
                    locationId,
                    employeeBindings,
                    out error))
            {
                return false;
            }

            employeeWork.PlaceAvatarsAtBoundWorkplaces();
            if (!TryValidateActiveNavigation(out error))
            {
                return false;
            }

            customerFlow.enabled = customerFlowWasEnabled;
            employeeWork.enabled = employeeWorkWasEnabled;
            error = null;
            return true;
        }

        private bool TryMoveFixture(
            PlaceableFixtureComponent fixture,
            GeneratedDetailedLocationBindings.FixtureBinding destination,
            out string error)
        {
            if (fixture == null || destination?.Instance == null ||
                destination.Metadata == null)
            {
                error = "A valid authored fixture and generated destination are required.";
                return false;
            }

            TransformState original = firstStoreTransformStates.FirstOrDefault(
                value => value.Target == fixture.transform);
            if (original == null)
            {
                error =
                    $"Fixture '{fixture.StableFixtureInstanceId}' has no parked first-store transform.";
                return false;
            }

            float sourceWidth = fixture.Footprint.width *
                                FixturePlacementGrid.PlacementIncrementMeters;
            float sourceDepth = fixture.Footprint.depth *
                                FixturePlacementGrid.PlacementIncrementMeters;
            Vector3 targetSize = destination.Metadata.PhysicalSizeMeters;
            fixture.transform.SetPositionAndRotation(
                destination.Instance.position,
                destination.Instance.rotation);
            fixture.transform.localScale = new Vector3(
                original.LocalScale.x * targetSize.x / sourceWidth,
                original.LocalScale.y,
                original.LocalScale.z * targetSize.z / sourceDepth);
            fixture.gameObject.SetActive(true);

            if (destination.Metadata.PrimaryCollider != null)
            {
                destination.Metadata.PrimaryCollider.enabled = false;
            }
            if (destination.Metadata.VisualRoot != null)
            {
                destination.Metadata.VisualRoot.gameObject.SetActive(false);
            }

            error = null;
            return true;
        }

        private bool TryBuildGeneratedNavigation(out string error)
        {
            ProceduralCommercialBuilding building = locations.ActiveBuilding;
            if (building == null || activeBindings?.SelectedUnit == null)
            {
                error = "Generated navigation requires an active selected unit.";
                return false;
            }

            DisableRenderOnlyColliders(
                building.transform.Find(
                    "Generated Procedural Layout/Opening Placeholders"));
            DisableRenderOnlyColliders(
                building.transform.Find(
                    "Generated Procedural Layout/Debug Overlays"));
            Physics.SyncTransforms();

            activeNavigationSurface =
                building.GetComponent<NavMeshSurface>() ??
                building.gameObject.AddComponent<NavMeshSurface>();
            PlanRect unit = activeBindings.SelectedUnit.BoundsMeters;
            activeNavigationSurface.collectObjects = CollectObjects.Volume;
            activeNavigationSurface.useGeometry =
                NavMeshCollectGeometry.PhysicsColliders;
            activeNavigationSurface.layerMask = ~0;
            activeNavigationSurface.center = new Vector3(
                unit.Center.x,
                1.5f,
                unit.Center.y);
            activeNavigationSurface.size = new Vector3(
                unit.Width,
                4f,
                unit.Depth);

            try
            {
                if (activeNavigationSurface.navMeshData != null)
                {
                    activeNavigationSurface.RemoveData();
                }
                activeNavigationSurface.BuildNavMesh();
            }
            catch (Exception exception)
            {
                error =
                    $"Generated navigation build failed: {exception.Message}";
                return false;
            }

            if (activeNavigationSurface.navMeshData == null)
            {
                error = "Generated navigation did not produce NavMesh data.";
                return false;
            }

            error = null;
            return true;
        }

        private static void DisableRenderOnlyColliders(Transform root)
        {
            if (root == null)
            {
                return;
            }
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(
                         true))
            {
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
        }

        public bool TryValidateActiveNavigation(out string error)
        {
            if (activeBindings == null || activeNavigationSurface == null ||
                activeNavigationSurface.navMeshData == null ||
                !NavMesh.SamplePosition(
                    activeBindings.EntrancePoint.position,
                    out NavMeshHit origin,
                    1.25f,
                    NavMesh.AllAreas))
            {
                error =
                    "The active generated location has no traversable entrance NavMesh.";
                return false;
            }

            NavMeshPath path = new();
            foreach (Transform target in activeBindings.RequiredNavigationPoints)
            {
                Vector3 local = locations.ActiveBuilding.transform
                    .InverseTransformPoint(target.position);
                if (!activeBindings.SelectedUnit.BoundsMeters.Contains(
                        new Vector2(local.x, local.z),
                        0.05f))
                {
                    error =
                        $"Generated detailed anchor '{target.name}' lies outside selected unit bounds at ({local.x:0.00}, {local.z:0.00}).";
                    return false;
                }
                if (!NavMesh.SamplePosition(
                        target.position,
                        out NavMeshHit destination,
                        1.25f,
                        NavMesh.AllAreas))
                {
                    error =
                        $"Generated detailed anchor '{target.name}' has no NavMesh within 1.25 m at ({local.x:0.00}, {local.z:0.00}).";
                    return false;
                }
                if (!NavMesh.CalculatePath(
                        origin.position,
                        destination.position,
                        NavMesh.AllAreas,
                        path) ||
                    path.status != NavMeshPathStatus.PathComplete)
                {
                    string corners = string.Join(
                        "; ",
                        path.corners.Select(value =>
                        {
                            Vector3 corner = locations.ActiveBuilding.transform
                                .InverseTransformPoint(value);
                            return $"({corner.x:0.00},{corner.z:0.00})";
                        }));
                    Vector3 originLocal = locations.ActiveBuilding.transform
                        .InverseTransformPoint(origin.position);
                    Vector3 destinationLocal = locations.ActiveBuilding.transform
                        .InverseTransformPoint(destination.position);
                    error =
                        $"Generated detailed anchor '{target.name}' has no complete path from entrance ({originLocal.x:0.00}, {originLocal.z:0.00}) to ({destinationLocal.x:0.00}, {destinationLocal.z:0.00}) (status {path.status}; corners {corners}).";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private void TeardownGeneratedDetailedRig()
        {
            if (customerFlow != null)
            {
                customerFlow.enabled = false;
                customerFlow.ResetTransientStateForRestore();
            }
            if (employeeWork != null)
            {
                employeeWork.enabled = false;
            }
            if (activeNavigationSurface != null)
            {
                activeNavigationSurface.RemoveData();
                activeNavigationSurface = null;
            }
            RestoreFirstStoreTransformStates();
            activeBindings = null;
        }

        private void RestoreFirstStoreTransformStates()
        {
            foreach (TransformState state in firstStoreTransformStates)
            {
                if (state?.Target == null)
                {
                    continue;
                }
                state.Target.SetParent(state.Parent, true);
                state.Target.SetPositionAndRotation(
                    state.Position,
                    state.Rotation);
                state.Target.localScale = state.LocalScale;
                state.Target.gameObject.SetActive(state.ActiveSelf);
            }
            Physics.SyncTransforms();
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
                bool standardsNeedAttention = location.detailedReconciliation?
                    .initialized == true &&
                    location.detailedReconciliation.metrics != null &&
                    !location.detailedReconciliation.metrics
                        .standardsTaskComplete;
                snapshot.cleaningTask.completedProgressUnits =
                    standardsNeedAttention
                        ? 0
                        : snapshot.cleaningTask.requiredProgressUnits;
                snapshot.cleaningTask.isActive = standardsNeedAttention;
            }
            snapshot.customerFlow = StoreCustomerFlowSnapshot.Empty();
            snapshot.customerFlow.secondsUntilNextArrival = 0.75f;
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
            Collider exitCollider = exit.GetComponent<Collider>();
            if (exitCollider != null)
            {
                exitCollider.isTrigger = true;
            }
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

        private static FirstStoreSnapshot CloneSnapshot(
            FirstStoreSnapshot source)
        {
            return source == null
                ? null
                : JsonUtility.FromJson<FirstStoreSnapshot>(
                    JsonUtility.ToJson(source));
        }
    }
}
