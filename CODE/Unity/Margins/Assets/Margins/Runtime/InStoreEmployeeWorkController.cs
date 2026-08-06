using System;
using System.Linq;
using UnityEngine;

namespace Margins
{
    /// <summary>
    /// Runs hired staff through the same authoritative detailed interactions the
    /// owner uses: receive and stock physical units, serve live customers by
    /// scanning their exact items, and respond to an actual mess.
    /// </summary>
    public sealed class InStoreEmployeeWorkController : MonoBehaviour
    {
        [SerializeField] private PortfolioProgressionController portfolio;
        [SerializeField] private StoreOperatingController store;
        [SerializeField] private DeliveryBoxComponent deliveryBox;
        [SerializeField] private StockingController stocking;
        [SerializeField] private StoreCustomerFlowController customerFlow;
        [SerializeField] private CleaningTaskComponent cleaning;
        [SerializeField] private ProductDefinition[] products;

        [Header("Visible staff")]
        [SerializeField] private Transform cashierAvatar;
        [SerializeField] private Transform stockerAvatar;
        [SerializeField] private Transform managerAvatar;
        [SerializeField] private TextMesh cashierLabel;
        [SerializeField] private TextMesh stockerLabel;
        [SerializeField] private TextMesh managerLabel;
        [SerializeField] private Transform cashierWorkPoint;
        [SerializeField] private Transform deliveryWorkPoint;
        [SerializeField] private Transform deliveryDropPoint;
        [SerializeField] private Transform shelfWorkPoint;
        [SerializeField] private Transform managerWorkPoint;
        [SerializeField] private Transform stockerBoxCarryPoint;
        [SerializeField] private Transform stockerUnitCarryPoint;
        [SerializeField, Min(0.1f)] private float movementSpeed = 2.4f;

        private float nextCashierActionAt;
        private float nextStockerActionAt;
        private float nextStandardsActionAt;
        private bool employeeMovingBox;
        private bool deliveryRelocated;
        private ProductItem stockerUnit;

        public bool IsHandlingInventory =>
            employeeMovingBox || stockerUnit != null;

        public StoreCustomerFlowController CustomerFlow => customerFlow;

        private void Start()
        {
            if (!TryValidateConfiguration(out string error))
            {
                Debug.LogError($"In-store employee work is unavailable: {error}", this);
                enabled = false;
            }
        }

        private void Update()
        {
            PortfolioProgression progression = portfolio?.Progression;
            bool storeAcceptsWork = store != null &&
                                    (store.State == StoreOperatingState.Open ||
                                     store.State == StoreOperatingState.Closing ||
                                     IsHandlingInventory);
            bool canWork = progression != null &&
                           progression.FirstShiftCompleted &&
                           storeAcceptsWork &&
                           !GamePauseMenuController.IsAnyMenuOpen;

            PortfolioEmployeeSnapshot cashier = canWork
                ? FindAssigned(progression, PortfolioEmployeeRole.Cashier)
                : null;
            PortfolioEmployeeSnapshot stocker = canWork
                ? FindAssigned(progression, PortfolioEmployeeRole.StockClerk)
                : null;
            PortfolioEmployeeSnapshot manager = canWork
                ? FindAssigned(progression, PortfolioEmployeeRole.Manager)
                : null;

            SetAvatar(
                cashierAvatar,
                cashierLabel,
                cashier,
                customerFlow != null &&
                (customerFlow.HasActiveCheckout || customerFlow.QueuedCustomerCount > 0)
                    ? "CASHIER • SERVING"
                    : "CASHIER • READY");
            SetAvatar(
                stockerAvatar,
                stockerLabel,
                stocker,
                IsHandlingInventory ? "STOCK • RESTOCKING" : "STOCK • READY");
            SetAvatar(
                managerAvatar,
                managerLabel,
                manager,
                cleaning != null && cleaning.NeedsCleaning
                    ? "MANAGER • STANDARDS"
                    : "MANAGER • SUPERVISING");

            if (!canWork)
            {
                return;
            }

            if (cashier != null)
            {
                MoveAvatar(cashierAvatar, cashierWorkPoint);
                if (Time.unscaledTime >= nextCashierActionAt &&
                    IsAt(cashierAvatar, cashierWorkPoint))
                {
                    TryPerformCashierAction();
                    nextCashierActionAt = Time.unscaledTime +
                                          ActionDelay(
                                              cashier,
                                              manager,
                                              PortfolioTaskFocus.Service);
                }
            }

            if (stocker != null)
            {
                Transform destination = GetStockerDestination();
                MoveAvatar(stockerAvatar, destination);
                if (Time.unscaledTime >= nextStockerActionAt &&
                    IsAt(stockerAvatar, destination))
                {
                    TryPerformStockerAction(
                        store.State == StoreOperatingState.Open);
                    nextStockerActionAt = Time.unscaledTime +
                                          ActionDelay(
                                              stocker,
                                              manager,
                                              PortfolioTaskFocus.Inventory);
                }
            }

            if (manager != null)
            {
                MoveAvatar(managerAvatar, managerWorkPoint);
                if (Time.unscaledTime >= nextStandardsActionAt &&
                    IsAt(managerAvatar, managerWorkPoint) &&
                    cleaning.NeedsCleaning)
                {
                    cleaning.TryApplyProgress(1);
                    nextStandardsActionAt = Time.unscaledTime +
                                            ActionDelay(
                                                manager,
                                                null,
                                                PortfolioTaskFocus.Standards);
                }
            }
            else if (stocker != null &&
                     stocker.taskFocus == PortfolioTaskFocus.Standards &&
                     Time.unscaledTime >= nextStandardsActionAt &&
                     cleaning.NeedsCleaning &&
                     stockerUnit == null &&
                     !employeeMovingBox)
            {
                cleaning.TryApplyProgress(1);
                nextStandardsActionAt = Time.unscaledTime +
                                        ActionDelay(
                                            stocker,
                                            null,
                                            PortfolioTaskFocus.Standards);
            }
        }

        public bool TryValidateConfiguration(out string error)
        {
            if (portfolio == null || store == null || deliveryBox == null ||
                stocking == null || customerFlow == null || cleaning == null ||
                products == null || products.Length == 0 ||
                products.Any(product => product == null) ||
                cashierAvatar == null || stockerAvatar == null ||
                managerAvatar == null || cashierWorkPoint == null ||
                deliveryWorkPoint == null || deliveryDropPoint == null ||
                shelfWorkPoint == null || managerWorkPoint == null ||
                stockerBoxCarryPoint == null || stockerUnitCarryPoint == null)
            {
                error = "Explicit company, store, product, staff, and workplace references are required.";
                return false;
            }

            if (customerFlow.StoreOperating != store ||
                customerFlow.Checkout != store.Checkout ||
                customerFlow.PhysicalUnits != stocking.PhysicalUnits)
            {
                error =
                    "Employee work must use the store's live customer, checkout, and physical-unit authorities.";
                return false;
            }

            error = null;
            return true;
        }

        public void ResetTransientStateAfterRestore()
        {
            stockerUnit = null;
            employeeMovingBox = false;
            nextCashierActionAt = Time.unscaledTime;
            nextStockerActionAt = Time.unscaledTime;
            nextStandardsActionAt = Time.unscaledTime;
            deliveryRelocated = deliveryBox != null &&
                                deliveryDropPoint != null &&
                                HorizontalDistance(
                                    deliveryBox.transform,
                                    deliveryDropPoint) < 1.25f;
        }

        private void TryPerformCashierAction()
        {
            if (customerFlow == null)
            {
                return;
            }

            if (!customerFlow.HasActiveCheckout)
            {
                customerFlow.TryStartCheckout(out _);
                return;
            }

            foreach (string physicalUnitId in
                     customerFlow.ActiveCheckoutPhysicalUnitIds)
            {
                if (customerFlow.CanScanCustomerItem(physicalUnitId))
                {
                    customerFlow.TryScanCustomerItem(physicalUnitId, out _);
                    return;
                }
            }

            if (customerFlow.ActiveCheckoutScannedCount ==
                customerFlow.ActiveCheckoutItemCount)
            {
                customerFlow.TryCompleteCheckout(out _);
            }
        }

        private void TryPerformStockerAction(bool mayStartNewWork)
        {
            if (deliveryBox == null || stocking == null ||
                deliveryBox.IsCarried && !employeeMovingBox)
            {
                return;
            }

            if (employeeMovingBox)
            {
                if (!deliveryBox.IsCarried ||
                    !deliveryBox.transform.IsChildOf(stockerBoxCarryPoint))
                {
                    employeeMovingBox = false;
                    deliveryRelocated = !deliveryBox.IsCarried &&
                                        HorizontalDistance(
                                            deliveryBox.transform,
                                            deliveryDropPoint) < 1.25f;
                    return;
                }

                if (deliveryBox.TrySetDown(
                        deliveryDropPoint.position,
                        deliveryDropPoint.rotation,
                        out _))
                {
                    employeeMovingBox = false;
                    deliveryRelocated = true;
                }
                return;
            }

            if (stockerUnit != null)
            {
                if (stockerUnit.IsHeld && stocking.TryStockHeldUnit(0, out _))
                {
                    stockerUnit = null;
                }
                else if (!stockerUnit.IsHeld)
                {
                    stockerUnit = null;
                }
                return;
            }

            if (!mayStartNewWork || stocking.HasHeldUnit)
            {
                return;
            }

            if (deliveryRelocated &&
                HorizontalDistance(
                    deliveryBox.transform,
                    deliveryDropPoint) >= 1.25f)
            {
                deliveryRelocated = false;
            }

            if (!deliveryRelocated)
            {
                if (!HasRemainingDeliveryInventory())
                {
                    deliveryRelocated = true;
                    return;
                }

                if (deliveryBox.TryPickUp(stockerBoxCarryPoint, out _))
                {
                    employeeMovingBox = true;
                }
                return;
            }

            if (deliveryBox.IsSealed)
            {
                deliveryBox.TryOpen(out _, out _);
                return;
            }

            ProductDefinition product = FindNextStockableProduct();
            if (product == null ||
                !deliveryBox.TryRemoveOneUnit(
                    product,
                    out ProductItem removed,
                    out _,
                    out _,
                    out _) ||
                !stocking.TryPickUpLooseUnit(
                    removed,
                    stockerUnitCarryPoint,
                    out ProductItem carried,
                    out _))
            {
                return;
            }

            stockerUnit = carried;
        }

        private ProductDefinition FindNextStockableProduct()
        {
            foreach (ProductDefinition product in products)
            {
                if (deliveryBox.TryGetConfiguredProductRemaining(
                        product,
                        out _,
                        out int remaining,
                        out _) &&
                    remaining > 0 &&
                    stocking.HasAvailableShelfPosition(product, out _))
                {
                    return product;
                }
            }
            return null;
        }

        private bool HasRemainingDeliveryInventory()
        {
            return products.Any(product =>
                deliveryBox.TryGetConfiguredProductRemaining(
                    product,
                    out _,
                    out int remaining,
                    out _) && remaining > 0);
        }

        private Transform GetStockerDestination()
        {
            if (employeeMovingBox)
            {
                return deliveryDropPoint;
            }
            if (stockerUnit?.IsHeld == true)
            {
                return shelfWorkPoint;
            }
            if (!deliveryRelocated)
            {
                return deliveryWorkPoint;
            }
            return deliveryDropPoint;
        }

        private static PortfolioEmployeeSnapshot FindAssigned(
            PortfolioProgression progression,
            PortfolioEmployeeRole role)
        {
            return progression.Employees.FirstOrDefault(employee =>
                employee.role == role &&
                string.Equals(
                    employee.assignedLocationId,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal));
        }

        private static float ActionDelay(
            PortfolioEmployeeSnapshot employee,
            PortfolioEmployeeSnapshot manager,
            PortfolioTaskFocus preferredFocus)
        {
            float skill = Mathf.Clamp01(employee.skill / 100f);
            float reliability = Mathf.Clamp01(employee.reliability / 100f);
            float competence = skill * 0.55f + reliability * 0.45f;
            float delay = Mathf.Lerp(2.35f, 0.72f, competence);

            if (employee.taskFocus == preferredFocus)
            {
                delay *= 0.86f;
            }
            else if (employee.taskFocus == PortfolioTaskFocus.Balanced)
            {
                delay *= 0.94f;
            }

            if (manager != null)
            {
                float managerCompetence = Mathf.Clamp01(
                    (manager.skill + manager.reliability) / 200f);
                delay *= Mathf.Lerp(0.9f, 0.7f, managerCompetence);
            }

            return Mathf.Max(0.35f, delay);
        }

        private void MoveAvatar(Transform avatar, Transform destination)
        {
            if (avatar == null || destination == null || !avatar.gameObject.activeSelf)
            {
                return;
            }

            Vector3 target = destination.position;
            target.y = avatar.position.y;
            Vector3 before = avatar.position;
            avatar.position = Vector3.MoveTowards(
                before,
                target,
                movementSpeed * Time.deltaTime);
            Vector3 direction = target - avatar.position;
            if (direction.sqrMagnitude > 0.01f)
            {
                avatar.rotation = Quaternion.RotateTowards(
                    avatar.rotation,
                    Quaternion.LookRotation(direction.normalized, Vector3.up),
                    540f * Time.deltaTime);
            }
        }

        private static bool IsAt(Transform avatar, Transform destination)
        {
            return avatar != null && destination != null &&
                   HorizontalDistance(avatar, destination) < 0.18f;
        }

        private static float HorizontalDistance(Transform left, Transform right)
        {
            Vector3 delta = left.position - right.position;
            delta.y = 0f;
            return delta.magnitude;
        }

        private static void SetAvatar(
            Transform avatar,
            TextMesh label,
            PortfolioEmployeeSnapshot employee,
            string assignment)
        {
            if (avatar == null)
            {
                return;
            }

            bool active = employee != null;
            if (avatar.gameObject.activeSelf != active)
            {
                avatar.gameObject.SetActive(active);
            }
            if (active && label != null)
            {
                string firstName = employee.displayName;
                int firstSpace = firstName.IndexOf(' ');
                if (firstSpace > 0)
                {
                    firstName = firstName.Substring(0, firstSpace);
                }
                string roleAndWork = assignment.Replace(" • ", "\n");
                label.text =
                    $"{firstName.ToUpperInvariant()}\n{roleAndWork}";
            }
        }
    }
}
