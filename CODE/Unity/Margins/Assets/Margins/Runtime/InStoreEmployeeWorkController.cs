using System;
using System.Linq;
using UnityEngine;

namespace Margins
{
    /// <summary>
    /// Runs hired staff through the same detailed interactions the owner learned:
    /// move/open deliveries, carry individual units to fixtures, scan the visible
    /// basket, take payment, and respond to an actual mess.
    /// </summary>
    public sealed class InStoreEmployeeWorkController : MonoBehaviour
    {
        [SerializeField] private PortfolioProgressionController portfolio;
        [SerializeField] private StoreOperatingController store;
        [SerializeField] private DeliveryBoxComponent deliveryBox;
        [SerializeField] private StockingController stocking;
        [SerializeField] private StagedCheckoutInteractionComponent stagedCheckout;
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
            bool canWork = progression != null &&
                           progression.FirstShiftCompleted &&
                           store != null &&
                           store.State == StoreOperatingState.Open &&
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

            SetAvatar(cashierAvatar, cashierLabel, cashier, "CASHIER • SERVING");
            SetAvatar(stockerAvatar, stockerLabel, stocker, "STOCK • RECEIVING");
            SetAvatar(managerAvatar, managerLabel, manager, "MANAGER • SUPERVISING");

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
                                          ActionDelay(cashier, manager != null);
                }
            }

            if (stocker != null)
            {
                Transform destination = GetStockerDestination();
                MoveAvatar(stockerAvatar, destination);
                if (Time.unscaledTime >= nextStockerActionAt &&
                    IsAt(stockerAvatar, destination))
                {
                    TryPerformStockerAction();
                    nextStockerActionAt = Time.unscaledTime +
                                          ActionDelay(stocker, manager != null);
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
                                            ActionDelay(manager, false);
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
                                        ActionDelay(stocker, false);
            }
        }

        public bool TryValidateConfiguration(out string error)
        {
            if (portfolio == null || store == null || deliveryBox == null ||
                stocking == null || stagedCheckout == null || cleaning == null ||
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

            error = null;
            return true;
        }

        public void ResetTransientStateAfterRestore()
        {
            stockerUnit = null;
            employeeMovingBox = false;
            deliveryRelocated = deliveryBox != null &&
                                deliveryDropPoint != null &&
                                HorizontalDistance(
                                    deliveryBox.transform,
                                    deliveryDropPoint) < 1.25f;
        }

        private void TryPerformCashierAction()
        {
            if (stagedCheckout == null || stagedCheckout.AllBasketsComplete)
            {
                return;
            }

            switch (stagedCheckout.NextAction)
            {
                case StagedCheckoutPrimaryAction.Begin:
                    stagedCheckout.TryBeginCustomer(out _);
                    break;
                case StagedCheckoutPrimaryAction.Scan:
                    stagedCheckout.TryScanVisibleProduct(
                        stagedCheckout.ActiveProduct,
                        out _,
                        out _);
                    break;
                case StagedCheckoutPrimaryAction.Complete:
                    if (stagedCheckout.TryTakePayment(out _, out _, out _))
                    {
                        stagedCheckout.TryContinue(out _);
                    }
                    break;
            }
        }

        private void TryPerformStockerAction()
        {
            if (deliveryBox == null || stocking == null ||
                deliveryBox.IsCarried && !employeeMovingBox)
            {
                return;
            }

            if (employeeMovingBox)
            {
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
            bool managerPresent)
        {
            float skill = Mathf.Clamp01(employee.skill / 100f);
            float reliability = Mathf.Clamp01(employee.reliability / 100f);
            float delay = Mathf.Lerp(2.1f, 0.85f, (skill + reliability) * 0.5f);
            return managerPresent ? delay * 0.78f : delay;
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
                string role = assignment;
                int separator = role.IndexOf('\u2022');
                if (separator > 0)
                {
                    role = role.Substring(0, separator).Trim();
                }
                label.text = $"{firstName.ToUpperInvariant()}\n{role}";
            }
        }
    }
}
