using System;
using System.Collections.Generic;
using UnityEngine;

namespace Margins
{
    /// <summary>
    /// Owns only instantiated customer lifecycle, physical shelf reservations,
    /// and queue order. Inventory, checkout completion, and revenue remain with
    /// their existing authoritative systems.
    /// </summary>
    public sealed class StoreCustomerFlowController : MonoBehaviour
    {
        private sealed class RuntimeCustomer
        {
            public string CustomerId;
            public int Ordinal;
            public StoreCustomerState State;
            public readonly List<string> RequestedProductIds = new();
            public readonly List<string> ReservedPhysicalUnitIds = new();
            public readonly List<string> ScannedPhysicalUnitIds = new();
            public GameObject Root;
            public Transform ItemRoot;
            public TextMesh StatusLabel;
            public LocalNavigationAgent Navigation;
            public float PatienceSeconds;
            public float PhaseSeconds;
            public bool WasAbandoned;
        }

        [SerializeField] private StoreOperatingController storeOperating;
        [SerializeField] private CheckoutStationComponent checkout;
        [SerializeField] private PhysicalProductUnitRegistry physicalUnits;
        [SerializeField] private Transform entrancePoint;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private Transform checkoutCustomerPoint;
        [SerializeField] private Transform[] checkoutItemPoints;
        [SerializeField] private Transform[] browsePoints;
        [SerializeField] private Transform[] queuePoints;
        [SerializeField] private Material[] customerMaterials;
        [SerializeField, Min(2)] private int maximumActiveCustomers = 5;
        [SerializeField, Min(0.25f)] private float arrivalIntervalSeconds = 5f;
        [SerializeField, Min(0f)] private float initialArrivalDelaySeconds = 1f;
        [SerializeField, Min(0.1f)] private float movementSpeed = 1.8f;
        [SerializeField, Min(0.1f)] private float shoppingSeconds = 1.5f;
        [SerializeField, Min(1f)] private float queuePatienceSeconds = 35f;
        [SerializeField, Min(1f)] private float checkoutPatienceSeconds = 45f;
        [SerializeField] private bool showDeveloperStatusLabels;

        private readonly List<RuntimeCustomer> customers = new();
        private BusinessStationQueue checkoutQueue;
        private RuntimeCustomer checkoutCustomer;
        private int nextCustomerOrdinal = 1;
        private float secondsUntilNextArrival;
        private bool started;

        public int ActiveCustomerCount => customers.Count;
        public int QueuedCustomerCount => CheckoutQueue.WaitingCount;
        public bool HasCustomersInStore => customers.Count > 0;
        public int LeavingWithoutPurchaseCount
        {
            get
            {
                int count = 0;
                foreach (RuntimeCustomer customer in customers)
                {
                    if (customer.State == StoreCustomerState.Leaving &&
                        customer.WasAbandoned)
                    {
                        count++;
                    }
                }
                return count;
            }
        }
        public bool HasActiveCheckout => checkoutCustomer != null;
        public int ActiveCheckoutItemCount =>
            checkoutCustomer?.ReservedPhysicalUnitIds.Count ?? 0;
        public int ActiveCheckoutScannedCount =>
            checkoutCustomer?.ScannedPhysicalUnitIds.Count ?? 0;
        public IReadOnlyList<string> ActiveCheckoutPhysicalUnitIds =>
            checkoutCustomer == null
                ? Array.Empty<string>()
                : checkoutCustomer.ReservedPhysicalUnitIds.ToArray();
        public long ActiveCheckoutSubtotalCents => checkout?.ActiveSubtotalCents ?? 0;
        public StoreOperatingController StoreOperating => storeOperating;
        public CheckoutStationComponent Checkout => checkout;
        public PhysicalProductUnitRegistry PhysicalUnits => physicalUnits;

        public bool CanStartCheckout
        {
            get
            {
                RuntimeCustomer front = GetFrontQueuedCustomer();
                return front != null && checkoutCustomer == null &&
                       checkout != null && !checkout.HasActiveIncompleteSession &&
                       queuePoints != null && queuePoints.Length > 0 &&
                       IsAtPoint(front, queuePoints[0]);
            }
        }

        public string CheckoutBlocker
        {
            get
            {
                if (checkoutCustomer != null)
                {
                    return ActiveCheckoutScannedCount < ActiveCheckoutItemCount
                        ? $"scan {ActiveCheckoutScannedCount}/{ActiveCheckoutItemCount} actual items"
                        : $"total {FormatCents(ActiveCheckoutSubtotalCents)}";
                }

                RuntimeCustomer front = GetFrontQueuedCustomer();
                if (front == null)
                {
                    return "no customer is waiting";
                }
                return IsAtPoint(front, queuePoints[0])
                    ? $"{QueuedCustomerCount} customer(s) in line"
                    : "customer is approaching the register";
            }
        }

        private void Start()
        {
            EnsureCheckoutQueue();
            started = true;
            secondsUntilNextArrival = initialArrivalDelaySeconds;
            if (!TryValidateConfiguration(out string error))
            {
                Debug.LogError($"Customer flow configuration failed: {error}", this);
            }
        }

        private void Update()
        {
            if (!started || storeOperating == null ||
                !storeOperating.IsInitialized || checkout == null ||
                checkout.TransactionLedger == null)
            {
                return;
            }

            float deltaSeconds = Mathf.Max(0f, Time.deltaTime);
            StoreOperatingState operatingState = storeOperating.State;
            if (operatingState != StoreOperatingState.Open &&
                operatingState != StoreOperatingState.Closing)
            {
                if (customers.Count > 0)
                {
                    ResolveAllForClosedStore();
                }
                return;
            }

            if (operatingState == StoreOperatingState.Open)
            {
                UpdateArrivals(deltaSeconds);
            }

            UpdateCustomerMovementAndState(deltaSeconds);
        }

        public bool TryValidateConfiguration(out string error)
        {
            if (storeOperating == null || checkout == null ||
                physicalUnits == null || entrancePoint == null ||
                exitPoint == null || checkoutCustomerPoint == null)
            {
                error =
                    "Customer flow requires explicit store, checkout, physical-unit, entrance, exit, and checkout references.";
                return false;
            }

            if (storeOperating.Checkout != checkout ||
                checkout.PhysicalUnits != physicalUnits)
            {
                error =
                    "Customer flow must use the operating controller's authoritative checkout and physical units.";
                return false;
            }

            if (browsePoints == null || browsePoints.Length == 0 ||
                ContainsNull(browsePoints) || queuePoints == null ||
                queuePoints.Length < 2 || ContainsNull(queuePoints) ||
                checkoutItemPoints == null || checkoutItemPoints.Length < 2 ||
                ContainsNull(checkoutItemPoints))
            {
                error =
                    "Customer flow requires browse points, at least two visible queue points, and two checkout item points.";
                return false;
            }

            if (maximumActiveCustomers < 2 || arrivalIntervalSeconds <= 0f ||
                movementSpeed <= 0f || shoppingSeconds <= 0f ||
                queuePatienceSeconds <= 0f || checkoutPatienceSeconds <= 0f ||
                checkout.ConfiguredProductIds.Count == 0)
            {
                error = "Customer flow timing, capacity, or product configuration is invalid.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryAdmitCustomerNow(
            out string customerId,
            out string error)
        {
            customerId = null;
            if (!TryValidateConfiguration(out error))
            {
                return false;
            }

            if (!storeOperating.IsInitialized ||
                storeOperating.State != StoreOperatingState.Open)
            {
                error = "New customers enter only while the store is open.";
                return false;
            }

            if (customers.Count >= maximumActiveCustomers)
            {
                error = "The instantiated customer capacity is full.";
                return false;
            }

            int ordinal = nextCustomerOrdinal++;
            customerId = $"store-customer-{ordinal:D6}";
            RuntimeCustomer customer = CreateRuntimeCustomer(
                customerId,
                ordinal,
                StoreCustomerState.Entering,
                entrancePoint.position);
            ChooseRequestedProducts(customer);
            customer.PhaseSeconds = shoppingSeconds;
            customers.Add(customer);
            UpdateCustomerLabel(customer, "SHOPPING");
            error = null;
            return true;
        }

        public bool TryGetCustomerNavigationAgent(
            string customerId,
            out LocalNavigationAgent navigation)
        {
            RuntimeCustomer customer = FindCustomer(customerId);
            navigation = customer?.Navigation;
            return navigation != null;
        }

        public bool IsFixtureModificationRestricted(string fixtureInstanceId)
        {
            if (!FirstStoreIdentifier.IsValid(fixtureInstanceId))
            {
                return false;
            }

            if (checkoutCustomer != null &&
                (IsAttachedToFixture(
                     checkoutCustomerPoint,
                     fixtureInstanceId) ||
                 ContainsAttachedPoint(
                     checkoutItemPoints,
                     fixtureInstanceId)))
            {
                return true;
            }

            return CheckoutQueue.WaitingCount > 0 &&
                   ContainsAttachedPoint(queuePoints, fixtureInstanceId);
        }

        public bool TryUseRegister(out string error)
        {
            if (checkoutCustomer == null)
            {
                return TryStartCheckout(out error);
            }

            if (checkoutCustomer.ScannedPhysicalUnitIds.Count !=
                checkoutCustomer.ReservedPhysicalUnitIds.Count)
            {
                error =
                    $"Scan the customer's actual items ({ActiveCheckoutScannedCount}/{ActiveCheckoutItemCount}).";
                return false;
            }

            return TryCompleteCheckout(out error);
        }

        public bool TryStartCheckout(out string error)
        {
            if (!CanStartCheckout)
            {
                error = CheckoutBlocker;
                return false;
            }

            RuntimeCustomer customer = GetFrontQueuedCustomer();
            string transactionId = $"sale-{customer.CustomerId}";
            if (!checkout.TryBeginSession(transactionId, out error))
            {
                return false;
            }

            if (!CheckoutQueue.TryReserveNext(
                    customer.CustomerId,
                    out BusinessStationQueueFailure queueFailure))
            {
                checkout.TryCancelActiveSession(out _);
                error =
                    $"Checkout station reservation failed ({queueFailure}).";
                return false;
            }

            customer.State = StoreCustomerState.Checkout;
            customer.PatienceSeconds = checkoutPatienceSeconds;
            checkoutCustomer = customer;
            for (int index = 0;
                 index < customer.ReservedPhysicalUnitIds.Count;
                 index++)
            {
                string unitId = customer.ReservedPhysicalUnitIds[index];
                string moveError = null;
                if (!physicalUnits.TryGetUnit(unitId, out ProductItem item, out _))
                {
                    moveError = "the reserved physical unit is missing";
                }
                else if (!item.TryMoveCustomerReservation(
                             checkoutItemPoints[index],
                             out moveError))
                {
                    // The shared failure path below restores the queue state.
                }

                if (moveError != null)
                {
                    checkout.TryCancelActiveSession(out _);
                    checkoutCustomer = null;
                    customer.State = StoreCustomerState.Queueing;
                    if (!CheckoutQueue.TryReturnReservationToFront(
                            customer.CustomerId,
                            out BusinessStationQueueFailure rollbackFailure))
                    {
                        throw new InvalidOperationException(
                            $"Checkout queue rollback failed for '{customer.CustomerId}' ({rollbackFailure}).");
                    }
                    error =
                        $"Customer item '{unitId}' could not reach checkout: {moveError}";
                    return false;
                }

                CustomerCheckoutItemWorldInteractionTarget target =
                    item.GetComponent<CustomerCheckoutItemWorldInteractionTarget>() ??
                    item.gameObject.AddComponent<CustomerCheckoutItemWorldInteractionTarget>();
                target.Initialize(this, unitId);
            }

            UpdateCustomerLabel(customer, "CHECKOUT");
            error = null;
            return true;
        }

        public bool CanScanCustomerItem(string physicalUnitId)
        {
            return checkoutCustomer != null &&
                   checkoutCustomer.State == StoreCustomerState.Checkout &&
                   checkoutCustomer.ReservedPhysicalUnitIds.Contains(physicalUnitId) &&
                   !checkoutCustomer.ScannedPhysicalUnitIds.Contains(physicalUnitId) &&
                   (storeOperating.State == StoreOperatingState.Open ||
                    storeOperating.State == StoreOperatingState.Closing);
        }

        public bool HasReservationAtShelfLocation(string shelfLocationId)
        {
            if (!FirstStoreIdentifier.IsValid(shelfLocationId))
            {
                return false;
            }

            foreach (RuntimeCustomer customer in customers)
            {
                foreach (string unitId in customer.ReservedPhysicalUnitIds)
                {
                    if (physicalUnits.TryGetUnit(
                            unitId,
                            out ProductItem item,
                            out _) &&
                        physicalUnits.IsAtLocation(item, shelfLocationId))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool TryScanCustomerItem(
            string physicalUnitId,
            out string error)
        {
            if (!CanScanCustomerItem(physicalUnitId) ||
                !physicalUnits.TryGetUnit(
                    physicalUnitId,
                    out ProductItem item,
                    out _))
            {
                error = "This physical item does not belong to the active customer checkout.";
                return false;
            }

            if (!checkout.TryScan(item.Definition, 1, out CheckoutFailure failure))
            {
                error = $"Customer item scan was rejected ({failure}).";
                return false;
            }

            checkoutCustomer.ScannedPhysicalUnitIds.Add(physicalUnitId);
            UpdateCustomerLabel(
                checkoutCustomer,
                $"SCAN {ActiveCheckoutScannedCount}/{ActiveCheckoutItemCount}");
            error = null;
            return true;
        }

        public bool TryCorrectLastScan(out string error)
        {
            if (checkoutCustomer == null ||
                checkoutCustomer.ScannedPhysicalUnitIds.Count == 0)
            {
                error = "No customer item scan can be corrected.";
                return false;
            }

            int lastIndex = checkoutCustomer.ScannedPhysicalUnitIds.Count - 1;
            string physicalUnitId =
                checkoutCustomer.ScannedPhysicalUnitIds[lastIndex];
            if (!physicalUnits.TryGetUnit(
                    physicalUnitId,
                    out ProductItem item,
                    out _))
            {
                error = "The last scanned physical customer item is missing.";
                return false;
            }

            if (!checkout.TryCorrect(
                    item.Definition,
                    1,
                    out CheckoutFailure failure))
            {
                error = $"Customer scan correction was rejected ({failure}).";
                return false;
            }

            checkoutCustomer.ScannedPhysicalUnitIds.RemoveAt(lastIndex);
            UpdateCustomerLabel(
                checkoutCustomer,
                $"SCAN {ActiveCheckoutScannedCount}/{ActiveCheckoutItemCount}");
            error = null;
            return true;
        }

        public bool TryCompleteCheckout(out string error)
        {
            if (checkoutCustomer == null ||
                checkoutCustomer.ReservedPhysicalUnitIds.Count == 0 ||
                checkoutCustomer.ScannedPhysicalUnitIds.Count !=
                checkoutCustomer.ReservedPhysicalUnitIds.Count ||
                !CheckoutQueue.HasReservation(checkoutCustomer.CustomerId))
            {
                error = "Every actual customer item must be scanned before payment.";
                return false;
            }

            RuntimeCustomer customer = checkoutCustomer;
            if (!checkout.TryComplete(
                    customer.ReservedPhysicalUnitIds,
                    out _,
                    out CheckoutFailure failure))
            {
                error = $"Customer payment was rejected ({failure}).";
                return false;
            }

            customer.ReservedPhysicalUnitIds.Clear();
            customer.ScannedPhysicalUnitIds.Clear();
            customer.State = StoreCustomerState.Leaving;
            customer.WasAbandoned = false;
            if (!CheckoutQueue.TryCompleteReservation(
                    customer.CustomerId,
                    out BusinessStationQueueFailure queueFailure))
            {
                throw new InvalidOperationException(
                    $"Completed checkout could not release station capacity for '{customer.CustomerId}' ({queueFailure}).");
            }
            checkoutCustomer = null;
            UpdateCustomerLabel(customer, "THANK YOU");
            error = null;
            return true;
        }

        public bool TryCaptureSnapshot(
            out StoreCustomerFlowSnapshot snapshot,
            out string error)
        {
            snapshot = null;
            if (!TryValidateConfiguration(out error))
            {
                return false;
            }

            if (HasActiveCheckout)
            {
                error = "Complete or abandon the active customer checkout before saving.";
                return false;
            }

            if (!TryValidateQueueConsistency(out error))
            {
                return false;
            }

            List<StoreCustomerSnapshot> captured = new(customers.Count);
            List<RuntimeCustomer> ordered = GetSnapshotCustomerOrder();
            foreach (RuntimeCustomer customer in ordered)
            {
                foreach (string unitId in customer.ReservedPhysicalUnitIds)
                {
                    if (!physicalUnits.TryGetUnit(
                            unitId,
                            out ProductItem item,
                            out _) ||
                        !item.IsReservedByCustomer)
                    {
                        error =
                            $"Customer '{customer.CustomerId}' has a missing physical shelf reservation.";
                        return false;
                    }
                }

                Vector3 position = customer.Root.transform.position;
                captured.Add(new StoreCustomerSnapshot(
                    customer.CustomerId,
                    customer.State,
                    customer.RequestedProductIds,
                    customer.ReservedPhysicalUnitIds,
                    customer.PatienceSeconds,
                    customer.PhaseSeconds,
                    position.x,
                    position.y,
                    position.z,
                    customer.WasAbandoned));
            }

            snapshot = new StoreCustomerFlowSnapshot(
                nextCustomerOrdinal,
                Mathf.Max(0f, secondsUntilNextArrival),
                captured);
            error = null;
            return true;
        }

        public bool CanApplySnapshot(
            StoreCustomerFlowSnapshot snapshot,
            IReadOnlyList<PhysicalProductUnitSnapshot> physicalUnitSnapshots,
            StoreOperatingSnapshot operatingSnapshot,
            out string error)
        {
            snapshot ??= StoreCustomerFlowSnapshot.Empty();
            if (!TryValidateConfiguration(out error))
            {
                return false;
            }

            if (snapshot.nextCustomerOrdinal <= 0 ||
                !IsFiniteNonnegative(snapshot.secondsUntilNextArrival) ||
                snapshot.customers == null ||
                snapshot.customers.Count > maximumActiveCustomers ||
                physicalUnitSnapshots == null || operatingSnapshot == null)
            {
                error = "Customer-flow snapshot header is invalid.";
                return false;
            }

            if (snapshot.customers.Count > 0 &&
                operatingSnapshot.state != StoreOperatingState.Open &&
                operatingSnapshot.state != StoreOperatingState.Closing)
            {
                error = "A closed store cannot restore active instantiated customers.";
                return false;
            }

            Dictionary<string, PhysicalProductUnitSnapshot> physicalById =
                new(StringComparer.Ordinal);
            foreach (PhysicalProductUnitSnapshot physical in physicalUnitSnapshots)
            {
                if (physical == null ||
                    !physicalById.TryAdd(physical.physicalUnitId, physical))
                {
                    error = "Customer-flow restore received invalid physical-unit snapshots.";
                    return false;
                }
            }

            HashSet<string> configuredProducts = new(
                checkout.ConfiguredProductIds,
                StringComparer.Ordinal);
            HashSet<string> customerIds = new(StringComparer.Ordinal);
            HashSet<string> reservedUnitIds = new(StringComparer.Ordinal);
            foreach (StoreCustomerSnapshot customer in snapshot.customers)
            {
                if (customer == null ||
                    !FirstStoreIdentifier.IsValid(customer.customerId) ||
                    !customer.customerId.StartsWith(
                        "store-customer-",
                        StringComparison.Ordinal) ||
                    ParseOrdinal(customer.customerId) <= 0 ||
                    ParseOrdinal(customer.customerId) >= snapshot.nextCustomerOrdinal ||
                    !customerIds.Add(customer.customerId) ||
                    !Enum.IsDefined(typeof(StoreCustomerState), customer.state) ||
                    customer.state == StoreCustomerState.Checkout ||
                    customer.requestedProductIds == null ||
                    customer.requestedProductIds.Count == 0 ||
                    customer.requestedProductIds.Count > 2 ||
                    customer.reservedPhysicalUnitIds == null ||
                    !IsFiniteNonnegative(customer.patienceSeconds) ||
                    !IsFiniteNonnegative(customer.phaseSeconds) ||
                    !IsFinite(customer.positionX) ||
                    !IsFinite(customer.positionY) ||
                    !IsFinite(customer.positionZ) ||
                    (customer.wasAbandoned &&
                     customer.state != StoreCustomerState.Leaving))
                {
                    error = "Customer-flow snapshot contains an invalid or active-checkout customer.";
                    return false;
                }

                HashSet<string> requested = new(StringComparer.Ordinal);
                foreach (string productId in customer.requestedProductIds)
                {
                    if (!configuredProducts.Contains(productId) ||
                        !requested.Add(productId))
                    {
                        error =
                            $"Customer '{customer.customerId}' requests an invalid or duplicate product.";
                        return false;
                    }
                }

                if (customer.reservedPhysicalUnitIds.Count > 0 &&
                    customer.state != StoreCustomerState.Queueing)
                {
                    error =
                        $"Customer '{customer.customerId}' has shelf reservations outside the queue.";
                    return false;
                }

                if (customer.state == StoreCustomerState.Queueing &&
                    (customer.reservedPhysicalUnitIds.Count == 0 ||
                     customer.reservedPhysicalUnitIds.Count >
                     customer.requestedProductIds.Count))
                {
                    error =
                        $"Queued customer '{customer.customerId}' has an invalid reservation count.";
                    return false;
                }

                HashSet<string> reservedProducts = new(StringComparer.Ordinal);
                foreach (string unitId in customer.reservedPhysicalUnitIds)
                {
                    if (!reservedUnitIds.Add(unitId) ||
                        !physicalById.TryGetValue(
                            unitId,
                            out PhysicalProductUnitSnapshot physical) ||
                        !requested.Contains(physical.productId) ||
                        !reservedProducts.Add(physical.productId) ||
                        !FirstStoreIdentifier.IsValid(
                            physical.inventoryLocationId) ||
                        string.IsNullOrWhiteSpace(physical.shelfFixtureId) ||
                        string.IsNullOrWhiteSpace(physical.shelfSnapPointId))
                    {
                        error =
                            $"Customer '{customer.customerId}' has a duplicated or invalid physical shelf reservation.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        public bool TryApplySnapshot(
            StoreCustomerFlowSnapshot snapshot,
            IReadOnlyList<PhysicalProductUnitSnapshot> physicalUnitSnapshots,
            StoreOperatingSnapshot operatingSnapshot,
            out string error)
        {
            snapshot ??= StoreCustomerFlowSnapshot.Empty();
            if (!CanApplySnapshot(
                    snapshot,
                    physicalUnitSnapshots,
                    operatingSnapshot,
                    out error))
            {
                return false;
            }

            ResetTransientStateForRestore();
            nextCustomerOrdinal = snapshot.nextCustomerOrdinal;
            secondsUntilNextArrival = snapshot.secondsUntilNextArrival;
            foreach (StoreCustomerSnapshot source in snapshot.customers)
            {
                RuntimeCustomer customer = CreateRuntimeCustomer(
                    source.customerId,
                    ParseOrdinal(source.customerId),
                    source.state,
                    new Vector3(
                        source.positionX,
                        source.positionY,
                        source.positionZ));
                customer.RequestedProductIds.AddRange(source.requestedProductIds);
                customer.PatienceSeconds = source.patienceSeconds;
                customer.PhaseSeconds = source.phaseSeconds;
                customer.WasAbandoned = source.wasAbandoned;
                for (int index = 0;
                     index < source.reservedPhysicalUnitIds.Count;
                     index++)
                {
                    string unitId = source.reservedPhysicalUnitIds[index];
                    Transform attachment = CreateCustomerItemPoint(customer, index);
                    if (!physicalUnits.TryGetUnit(
                            unitId,
                            out ProductItem item,
                            out _) ||
                        !item.TryAttachToCustomer(attachment, out error))
                    {
                        ResetTransientStateForRestore();
                        error =
                            $"Preflighted customer reservation '{unitId}' could not be restored: {error}";
                        return false;
                    }
                    customer.ReservedPhysicalUnitIds.Add(unitId);
                }
                customers.Add(customer);
                if (customer.State == StoreCustomerState.Queueing &&
                    !CheckoutQueue.TryEnqueue(
                        customer.CustomerId,
                        1,
                        out BusinessStationQueueFailure queueFailure))
                {
                    ResetTransientStateForRestore();
                    error =
                        $"Customer queue restore failed for '{customer.CustomerId}' ({queueFailure}).";
                    return false;
                }
                UpdateCustomerLabel(customer, LabelFor(customer));
            }

            started = true;
            error = null;
            return true;
        }

        public bool TryGetDiskSaveBlocker(out string blocker)
        {
            if (HasActiveCheckout)
            {
                blocker = "Complete or abandon the active customer checkout before saving.";
                return true;
            }
            blocker = null;
            return false;
        }

        public bool TryGetRestoreBlocker(out string blocker)
        {
            if (HasActiveCheckout)
            {
                blocker = "Complete or abandon the active customer checkout before loading.";
                return true;
            }
            blocker = null;
            return false;
        }

        public void ResetTransientStateForRestore()
        {
            for (int index = customers.Count - 1; index >= 0; index--)
            {
                DestroyRuntimeObject(customers[index].Root);
            }
            customers.Clear();
            checkoutCustomer = null;
            checkoutQueue?.Clear();
        }

        private void UpdateArrivals(float deltaSeconds)
        {
            if (customers.Count >= maximumActiveCustomers)
            {
                return;
            }

            secondsUntilNextArrival -= deltaSeconds;
            if (secondsUntilNextArrival > 0f)
            {
                return;
            }

            if (!TryAdmitCustomerNow(out _, out string error))
            {
                Debug.LogWarning($"Customer admission was skipped: {error}", this);
            }
            secondsUntilNextArrival = arrivalIntervalSeconds;
        }

        private void UpdateCustomerMovementAndState(float deltaSeconds)
        {
            List<RuntimeCustomer> completedLeaving = null;
            for (int index = 0; index < customers.Count; index++)
            {
                RuntimeCustomer customer = customers[index];
                switch (customer.State)
                {
                    case StoreCustomerState.Entering:
                        if (MoveTowards(
                                customer,
                                browsePoints[customer.Ordinal % browsePoints.Length]))
                        {
                            customer.State = StoreCustomerState.Shopping;
                            customer.PhaseSeconds = shoppingSeconds;
                            UpdateCustomerLabel(customer, "CHOOSING");
                        }
                        break;

                    case StoreCustomerState.Shopping:
                        customer.PhaseSeconds =
                            Mathf.Max(0f, customer.PhaseSeconds - deltaSeconds);
                        if (customer.PhaseSeconds <= 0f)
                        {
                            FinishShopping(customer);
                        }
                        break;

                    case StoreCustomerState.Queueing:
                        customer.PatienceSeconds -= deltaSeconds;
                        if (customer.PatienceSeconds <= 0f)
                        {
                            Abandon(customer);
                            break;
                        }
                        MoveTowards(
                            customer,
                            QueuePointFor(customer));
                        break;

                    case StoreCustomerState.Checkout:
                        customer.PatienceSeconds -= deltaSeconds;
                        if (customer.PatienceSeconds <= 0f)
                        {
                            Abandon(customer);
                            break;
                        }
                        MoveTowards(customer, checkoutCustomerPoint);
                        break;

                    case StoreCustomerState.Leaving:
                        if (MoveTowards(customer, exitPoint))
                        {
                            completedLeaving ??= new List<RuntimeCustomer>();
                            completedLeaving.Add(customer);
                        }
                        break;
                }
            }

            if (completedLeaving == null)
            {
                return;
            }

            foreach (RuntimeCustomer customer in completedLeaving)
            {
                customers.Remove(customer);
                DestroyRuntimeObject(customer.Root);
            }
        }

        private void FinishShopping(RuntimeCustomer customer)
        {
            bool priceRejected = false;
            for (int index = 0;
                 index < customer.RequestedProductIds.Count;
                 index++)
            {
                string productId = customer.RequestedProductIds[index];
                if (!checkout.TryGetCurrentOffer(
                        productId,
                        out MerchandiseOffer offer))
                {
                    continue;
                }

                if (!MerchandisingRules.WillPurchase(
                        customer.CustomerId,
                        productId,
                        offer.SalePriceCents,
                        offer.ReferencePriceCents))
                {
                    priceRejected = true;
                    continue;
                }

                if (!physicalUnits.TryGetAvailableShelvedUnit(
                        productId,
                        offer.InventoryLocationId,
                        out ProductItem item))
                {
                    continue;
                }

                Transform attachment = CreateCustomerItemPoint(
                    customer,
                    customer.ReservedPhysicalUnitIds.Count);
                if (!item.TryAttachToCustomer(attachment, out string error))
                {
                    Debug.LogWarning(
                        $"Customer '{customer.CustomerId}' could not reserve '{item.PhysicalUnitId}': {error}",
                        this);
                    continue;
                }
                customer.ReservedPhysicalUnitIds.Add(item.PhysicalUnitId);
            }

            if (customer.ReservedPhysicalUnitIds.Count == 0)
            {
                customer.State = StoreCustomerState.Leaving;
                customer.WasAbandoned = true;
                UpdateCustomerLabel(
                    customer,
                    priceRejected ? "PRICE TOO HIGH" : "NO STOCK");
                return;
            }

            if (!CheckoutQueue.TryEnqueue(
                    customer.CustomerId,
                    1,
                    out BusinessStationQueueFailure queueFailure))
            {
                throw new InvalidOperationException(
                    $"Customer '{customer.CustomerId}' could not enter the checkout queue ({queueFailure}).");
            }
            customer.State = StoreCustomerState.Queueing;
            customer.PatienceSeconds = queuePatienceSeconds;
            UpdateCustomerLabel(customer, "IN LINE");
        }

        private void Abandon(RuntimeCustomer customer)
        {
            if (customer == checkoutCustomer)
            {
                checkout.TryCancelActiveSession(out _);
                checkoutCustomer = null;
            }

            if ((customer.State == StoreCustomerState.Queueing ||
                 customer.State == StoreCustomerState.Checkout) &&
                !CheckoutQueue.TryAbandon(
                    customer.CustomerId,
                    out BusinessStationQueueFailure queueFailure))
            {
                throw new InvalidOperationException(
                    $"Customer '{customer.CustomerId}' could not leave the checkout queue ({queueFailure}).");
            }

            foreach (string unitId in customer.ReservedPhysicalUnitIds)
            {
                if (!physicalUnits.TryGetUnit(
                        unitId,
                        out ProductItem item,
                        out _))
                {
                    throw new InvalidOperationException(
                        $"Customer abandonment could not find physical unit '{unitId}'.");
                }

                if (!item.TryReturnFromCustomer(out string error))
                {
                    throw new InvalidOperationException(
                        $"Customer abandonment could not return physical unit '{unitId}': {error}");
                }
            }
            customer.ReservedPhysicalUnitIds.Clear();
            customer.ScannedPhysicalUnitIds.Clear();
            customer.State = StoreCustomerState.Leaving;
            customer.WasAbandoned = true;
            UpdateCustomerLabel(customer, "LEFT LINE");
        }

        private void ResolveAllForClosedStore()
        {
            if (checkoutCustomer != null)
            {
                checkout.TryCancelActiveSession(out _);
                checkoutCustomer = null;
            }

            for (int index = customers.Count - 1; index >= 0; index--)
            {
                RuntimeCustomer customer = customers[index];
                foreach (string unitId in customer.ReservedPhysicalUnitIds)
                {
                    if (physicalUnits.TryGetUnit(
                            unitId,
                            out ProductItem item,
                            out _) &&
                        !item.TryReturnFromCustomer(out string error))
                    {
                        throw new InvalidOperationException(
                            $"Closed-store customer resolution failed for '{unitId}': {error}");
                    }
                }
                DestroyRuntimeObject(customer.Root);
            }
            customers.Clear();
            CheckoutQueue.Clear();
        }

        private RuntimeCustomer CreateRuntimeCustomer(
            string customerId,
            int ordinal,
            StoreCustomerState state,
            Vector3 position)
        {
            GameObject root = new($"Customer {customerId}");
            root.transform.position = position;
            LocalNavigationAgent navigation =
                root.AddComponent<LocalNavigationAgent>();
            navigation.Configure(
                movementSpeed,
                35 + Math.Abs(ordinal % 45));

            Material material = customerMaterials != null &&
                                customerMaterials.Length > 0
                ? customerMaterials[Math.Abs(ordinal) % customerMaterials.Length]
                : null;
            CreateShape(
                root.transform,
                "Body",
                PrimitiveType.Capsule,
                new Vector3(0f, 0.9f, 0f),
                new Vector3(0.52f, 0.72f, 0.42f),
                material);
            CreateShape(
                root.transform,
                "Head",
                PrimitiveType.Sphere,
                new Vector3(0f, 1.85f, 0f),
                Vector3.one * 0.42f,
                material);
            CreateShape(
                root.transform,
                "Basket",
                PrimitiveType.Cube,
                new Vector3(0.48f, 0.95f, 0f),
                new Vector3(0.48f, 0.24f, 0.36f),
                null);

            GameObject itemRootObject = new("Customer Items");
            itemRootObject.transform.SetParent(root.transform, false);
            itemRootObject.transform.localPosition = new Vector3(0.48f, 1.1f, 0f);

            TextMesh label = null;
            if (showDeveloperStatusLabels)
            {
                GameObject labelObject = new("Customer Status Diagnostic");
                labelObject.transform.SetParent(root.transform, false);
                labelObject.transform.localPosition = new Vector3(0f, 2.35f, 0f);
                labelObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                labelObject.transform.localScale = Vector3.one * 0.035f;
                label = labelObject.AddComponent<TextMesh>();
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.characterSize = 0.18f;
                label.fontSize = 48;
                label.color = Color.white;
            }

            return new RuntimeCustomer
            {
                CustomerId = customerId,
                Ordinal = ordinal,
                State = state,
                Root = root,
                ItemRoot = itemRootObject.transform,
                StatusLabel = label,
                Navigation = navigation
            };
        }

        private void ChooseRequestedProducts(RuntimeCustomer customer)
        {
            IReadOnlyList<string> productIds = checkout.ConfiguredProductIds;
            int primary = (customer.Ordinal - 1) % productIds.Count;
            if (primary < 0)
            {
                primary += productIds.Count;
            }
            customer.RequestedProductIds.Add(productIds[primary]);
            if (productIds.Count > 1 && customer.Ordinal % 3 == 0)
            {
                customer.RequestedProductIds.Add(
                    productIds[(primary + 1) % productIds.Count]);
            }
        }

        private RuntimeCustomer GetFrontQueuedCustomer()
        {
            string frontJobId = CheckoutQueue.FrontWaitingJobId;
            if (frontJobId == null)
            {
                return null;
            }

            foreach (RuntimeCustomer customer in customers)
            {
                if (string.Equals(
                        customer.CustomerId,
                        frontJobId,
                        StringComparison.Ordinal))
                {
                    return customer;
                }
            }
            throw new InvalidOperationException(
                $"Checkout queue references missing customer '{frontJobId}'.");
        }

        private Transform QueuePointFor(RuntimeCustomer selected)
        {
            int queueIndex = CheckoutQueue.GetWaitingPosition(
                selected.CustomerId);
            if (queueIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Queued customer '{selected.CustomerId}' has no station request.");
            }
            return queuePoints[Mathf.Min(queueIndex, queuePoints.Length - 1)];
        }

        private BusinessStationQueue CheckoutQueue
        {
            get
            {
                EnsureCheckoutQueue();
                return checkoutQueue;
            }
        }

        private void EnsureCheckoutQueue()
        {
            if (checkoutQueue != null)
            {
                return;
            }

            checkoutQueue = new BusinessStationQueue(
                "station-first-store-checkout",
                ConvenienceStoreOperations.CustomerCheckout
                    .Steps[0]
                    .StationCapabilityId,
                1);
        }

        private bool TryValidateQueueConsistency(out string error)
        {
            IReadOnlyList<string> waitingJobIds = CheckoutQueue.WaitingJobIds;
            HashSet<string> queuedCustomerIds = new(StringComparer.Ordinal);
            foreach (RuntimeCustomer customer in customers)
            {
                if (customer.State == StoreCustomerState.Queueing)
                {
                    queuedCustomerIds.Add(customer.CustomerId);
                }
            }

            if (queuedCustomerIds.Count != waitingJobIds.Count)
            {
                error =
                    "Checkout queue membership disagrees with instantiated customer state.";
                return false;
            }
            foreach (string jobId in waitingJobIds)
            {
                if (!queuedCustomerIds.Contains(jobId))
                {
                    error =
                        "Checkout queue membership disagrees with instantiated customer state.";
                    return false;
                }
            }

            if (CheckoutQueue.ReservationCount !=
                    (checkoutCustomer == null ? 0 : 1) ||
                (checkoutCustomer != null &&
                 !CheckoutQueue.HasReservation(checkoutCustomer.CustomerId)))
            {
                error =
                    "Checkout station reservations disagree with instantiated customer state.";
                return false;
            }

            error = null;
            return true;
        }

        private List<RuntimeCustomer> GetSnapshotCustomerOrder()
        {
            List<RuntimeCustomer> ordered = new(customers.Count);
            foreach (string customerId in CheckoutQueue.WaitingJobIds)
            {
                RuntimeCustomer queued = FindCustomer(customerId);
                if (queued == null)
                {
                    throw new InvalidOperationException(
                        $"Checkout queue references missing customer '{customerId}'.");
                }
                ordered.Add(queued);
            }

            List<RuntimeCustomer> remaining = new();
            foreach (RuntimeCustomer customer in customers)
            {
                if (customer.State != StoreCustomerState.Queueing)
                {
                    remaining.Add(customer);
                }
            }
            remaining.Sort((left, right) =>
                string.CompareOrdinal(left.CustomerId, right.CustomerId));
            ordered.AddRange(remaining);
            return ordered;
        }

        private RuntimeCustomer FindCustomer(string customerId)
        {
            foreach (RuntimeCustomer customer in customers)
            {
                if (string.Equals(
                        customer.CustomerId,
                        customerId,
                        StringComparison.Ordinal))
                {
                    return customer;
                }
            }
            return null;
        }

        private static bool MoveTowards(
            RuntimeCustomer customer,
            Transform target)
        {
            return customer?.Navigation != null &&
                   customer.Navigation.NavigateTo(target);
        }

        private static bool IsAtPoint(RuntimeCustomer customer, Transform point)
        {
            if (customer == null || point == null)
            {
                return false;
            }

            if (customer.Navigation != null &&
                customer.Navigation.HasArrivedAt(point))
            {
                return true;
            }

            Vector3 delta = customer.Root.transform.position - point.position;
            delta.y = 0f;
            return delta.sqrMagnitude <= 0.1225f;
        }

        private static bool ContainsAttachedPoint(
            IReadOnlyList<Transform> points,
            string fixtureInstanceId)
        {
            if (points == null)
            {
                return false;
            }

            foreach (Transform point in points)
            {
                if (IsAttachedToFixture(point, fixtureInstanceId))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsAttachedToFixture(
            Transform point,
            string fixtureInstanceId)
        {
            PlaceableFixtureComponent fixture =
                point?.GetComponentInParent<PlaceableFixtureComponent>();
            return fixture != null &&
                   string.Equals(
                       fixture.StableFixtureInstanceId,
                       fixtureInstanceId,
                       StringComparison.Ordinal);
        }

        private static Transform CreateCustomerItemPoint(
            RuntimeCustomer customer,
            int index)
        {
            GameObject point = new($"Item {index + 1}");
            point.transform.SetParent(customer.ItemRoot, false);
            point.transform.localPosition = new Vector3(
                (index % 2) * 0.22f - 0.11f,
                0f,
                0f);
            return point.transform;
        }

        private static void CreateShape(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject shape = GameObject.CreatePrimitive(primitive);
            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = localPosition;
            shape.transform.localScale = localScale;
            Collider collider = shape.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyRuntimeObject(collider);
            }
            Renderer renderer = shape.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void UpdateCustomerLabel(
            RuntimeCustomer customer,
            string text)
        {
            if (customer.StatusLabel != null)
            {
                customer.StatusLabel.text = text;
            }
        }

        private static string LabelFor(RuntimeCustomer customer)
        {
            return customer.State switch
            {
                StoreCustomerState.Entering => "SHOPPING",
                StoreCustomerState.Shopping => "CHOOSING",
                StoreCustomerState.Queueing => "IN LINE",
                StoreCustomerState.Checkout => "CHECKOUT",
                StoreCustomerState.Leaving => customer.WasAbandoned
                    ? "LEAVING"
                    : "THANK YOU",
                _ => string.Empty
            };
        }

        private static int ParseOrdinal(string customerId)
        {
            int separator = customerId?.LastIndexOf('-') ?? -1;
            return separator >= 0 &&
                   int.TryParse(customerId.Substring(separator + 1), out int ordinal)
                ? ordinal
                : 0;
        }

        private static bool ContainsNull(IReadOnlyList<Transform> transforms)
        {
            foreach (Transform point in transforms)
            {
                if (point == null)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsFiniteNonnegative(float value)
        {
            return IsFinite(value) && value >= 0f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
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

        private static void DestroyRuntimeObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
