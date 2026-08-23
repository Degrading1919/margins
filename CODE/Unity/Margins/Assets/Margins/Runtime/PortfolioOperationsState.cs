using System;
using System.Collections.Generic;
using System.Linq;

namespace Margins
{
    public enum PortfolioMaintenancePolicy
    {
        Deferred = 0,
        Routine = 1,
        Preventive = 2
    }

    public enum PortfolioAlertSeverity
    {
        Advisory = 0,
        Warning = 1,
        Critical = 2
    }

    [Serializable]
    public sealed class PortfolioEmployeeScheduleSnapshot
    {
        public int scheduledDayMask = PortfolioOperationsRules.AllDaysMask;
        public int shiftStartMinute = 8 * 60;
        public int shiftEndMinute = 16 * 60;
    }

    [Serializable]
    public sealed class PortfolioDelegationPolicySnapshot
    {
        public bool managerCanPurchase = true;
        public bool managerCanAuthorizeMaintenance = true;
        public bool managerCanAdjustPrices;
        public long dailySpendingLimitCents = 100_000;
        public int minimumServiceQuality = 70;
        public int minimumProductAvailabilityBasisPoints = 8_000;
        public int minimumMaintenanceCondition = 60;
        public PortfolioMaintenancePolicy maintenancePolicy =
            PortfolioMaintenancePolicy.Routine;
    }

    [Serializable]
    public sealed class PortfolioProductInventorySnapshot
    {
        public string productId;
        public int quantityUnits;
        public long unitCostCents;

        public long InventoryValueCents => checked(unitCostCents * quantityUnits);
    }

    [Serializable]
    public sealed class PortfolioOperatingAlertSnapshot
    {
        public string alertId;
        public string problemTypeId;
        public PortfolioAlertSeverity severity;
        public int openedDay;
        public int resolvedDay;
        public bool acknowledged;
        public bool requiresOwnerAttention;
        public string summary;
        public string recoveryAction;

        public bool IsOpen => resolvedDay == 0;
    }

    /// <summary>
    /// Cumulative detailed-session observations. Monetary and inventory
    /// authority remains with StoreSessionTotals and the inventory snapshot.
    /// </summary>
    [Serializable]
    public sealed class DetailedOperationMetricsSnapshot
    {
        public int customerVisits;
        public int customersServed;
        public int customersAbandoned;
        public int requestedProductUnits;
        public int unavailableProductUnits;
        public bool standardsTaskComplete = true;

        public bool TryValidate(out string error)
        {
            if (customerVisits < 0 || customersServed < 0 ||
                customersAbandoned < 0 || requestedProductUnits < 0 ||
                unavailableProductUnits < 0 ||
                (long)customersServed + customersAbandoned > customerVisits ||
                unavailableProductUnits > requestedProductUnits)
            {
                error = "Detailed customer, availability, or standards metrics contradict one another.";
                return false;
            }

            error = null;
            return true;
        }
    }

    [Serializable]
    public sealed class PortfolioDetailedReconciliationSnapshot
    {
        public bool initialized;
        public string sessionId;
        public int sessionStartedDay;
        public long startingInventoryAssetValueCents;
        public long startingDeliveredProcurementInventoryCents;
        public long startingProcurementPurchaseCents;
        public long startingProcurementDeliveryFeesCents;
        public int startingCustomerSatisfaction;
        public int startingMaintenanceCondition;
        public long grossSalesCents;
        public long costOfGoodsSoldCents;
        public long includedOperatingExpensesCents;
        public long payrollCents;
        public long rentCents;
        public long operatingCostCents;
        public long inventoryAcquiredCostCents;
        public int unitsSold;
        public int transactionCount;
        public DetailedOperationMetricsSnapshot metrics = new();
    }

    [Serializable]
    public sealed class PortfolioConsolidatedReportSnapshot
    {
        public int day;
        public string companyId;
        public int brandCount;
        public int locationCount;
        public int leasedPropertyCount;
        public int ownedPropertyCount;
        public int openAlertCount;
        public int ownerAttentionAlertCount;
        public long cashCents;
        public long lifetimeGrossSalesCents;
        public long lifetimeOperatingCostsCents;
        public long lifetimeOperatingProfitCents;
        public long lifetimePropertyAcquisitionCents;
        public long lifetimeImprovementCents;
    }

    public static class PortfolioOperationsRules
    {
        public const int AllDaysMask = 0x7f;
        public const int MinutesPerDay = 24 * 60;
        public const int BasisPoints = 10_000;

        public static PortfolioEmployeeScheduleSnapshot CreateDefaultSchedule()
        {
            return new PortfolioEmployeeScheduleSnapshot();
        }

        public static PortfolioDelegationPolicySnapshot
            CreateDefaultDelegationPolicy()
        {
            return new PortfolioDelegationPolicySnapshot();
        }

        public static bool TryValidateSchedule(
            PortfolioEmployeeScheduleSnapshot schedule,
            out string error)
        {
            if (schedule == null || schedule.scheduledDayMask <= 0 ||
                (schedule.scheduledDayMask & ~AllDaysMask) != 0 ||
                schedule.shiftStartMinute < 0 ||
                schedule.shiftStartMinute >= MinutesPerDay ||
                schedule.shiftEndMinute <= schedule.shiftStartMinute ||
                schedule.shiftEndMinute > MinutesPerDay)
            {
                error = "Employee schedules require at least one weekday and one valid same-day shift.";
                return false;
            }

            error = null;
            return true;
        }

        public static bool IsScheduled(
            PortfolioEmployeeScheduleSnapshot schedule,
            int simulationDay)
        {
            if (!TryValidateSchedule(schedule, out _) || simulationDay < 1)
            {
                return false;
            }

            int dayBit = 1 << ((simulationDay - 1) % 7);
            return (schedule.scheduledDayMask & dayBit) != 0;
        }

        public static bool TryValidateDelegationPolicy(
            PortfolioDelegationPolicySnapshot policy,
            out string error)
        {
            if (policy == null || policy.dailySpendingLimitCents < 0 ||
                policy.minimumServiceQuality < 0 ||
                policy.minimumServiceQuality > 100 ||
                policy.minimumProductAvailabilityBasisPoints < 0 ||
                policy.minimumProductAvailabilityBasisPoints > BasisPoints ||
                policy.minimumMaintenanceCondition < 0 ||
                policy.minimumMaintenanceCondition > 100 ||
                !Enum.IsDefined(
                    typeof(PortfolioMaintenancePolicy),
                    policy.maintenancePolicy))
            {
                error = "Delegation policy contains an invalid authority, budget, or operating standard.";
                return false;
            }

            error = null;
            return true;
        }

        public static bool TryValidateProductInventory(
            IReadOnlyList<PortfolioProductInventorySnapshot> inventory,
            IReadOnlyList<MerchandisePriceSnapshot> merchandisePrices,
            int expectedTotalUnits,
            out string error)
        {
            if (inventory == null || merchandisePrices == null ||
                expectedTotalUnits < 0)
            {
                error = "Product-level inventory or merchandise configuration is missing.";
                return false;
            }

            HashSet<string> configuredProducts = new(
                merchandisePrices
                    .Where(item => item != null)
                    .Select(item => item.productId),
                StringComparer.Ordinal);
            HashSet<string> productIds = new(StringComparer.Ordinal);
            long total = 0;
            long inventoryValue = 0;
            foreach (PortfolioProductInventorySnapshot product in inventory)
            {
                if (product == null ||
                    !StableIdentifier.IsValid(product.productId) ||
                    !configuredProducts.Contains(product.productId) ||
                    product.quantityUnits < 0 || product.unitCostCents < 0 ||
                    !productIds.Add(product.productId))
                {
                    error = "Product-level inventory contains an invalid, duplicate, or unconfigured product.";
                    return false;
                }

                total += product.quantityUnits;
                if (total > int.MaxValue)
                {
                    error = "Product-level inventory exceeds supported integer-unit storage.";
                    return false;
                }

                try
                {
                    inventoryValue = checked(
                        inventoryValue +
                        checked(product.unitCostCents * product.quantityUnits));
                }
                catch (OverflowException)
                {
                    error =
                        "Product-level inventory value exceeds supported integer-cent storage.";
                    return false;
                }
            }

            if (productIds.Count != configuredProducts.Count ||
                total != expectedTotalUnits)
            {
                error = "Product-level inventory does not reconcile to the location total or merchandise catalog.";
                return false;
            }

            error = null;
            return true;
        }

        public static List<PortfolioProductInventorySnapshot>
            CreateProvisionalProductInventory(
                IReadOnlyList<MerchandisePriceSnapshot> merchandisePrices,
                int totalUnits,
                long fallbackUnitCostCents)
        {
            if (merchandisePrices == null || totalUnits < 0 ||
                fallbackUnitCostCents < 0)
            {
                throw new ArgumentException(
                    "Product inventory migration requires configured products, nonnegative units, and a nonnegative fallback cost.");
            }

            List<string> products = merchandisePrices
                .Where(item => item != null)
                .Select(item => item.productId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToList();
            if (products.Count == 0 && totalUnits > 0)
            {
                throw new ArgumentException(
                    "Positive aggregate inventory cannot be migrated without a merchandise product.");
            }

            ConvenienceStoreProcurement.Catalog.TryGetSupplier(
                ConvenienceStoreProcurement.SupplierId,
                out ProcurementSupplierDefinition supplier);
            List<PortfolioProductInventorySnapshot> result = new();
            for (int index = 0; index < products.Count; index++)
            {
                int quantity = totalUnits / products.Count +
                               (index < totalUnits % products.Count ? 1 : 0);
                long unitCost = supplier != null &&
                                supplier.TryGetResource(
                                    products[index],
                                    out ProcurementResourceDefinition resource)
                    ? resource.UnitCostCents
                    : fallbackUnitCostCents;
                result.Add(new PortfolioProductInventorySnapshot
                {
                    productId = products[index],
                    quantityUnits = quantity,
                    unitCostCents = unitCost
                });
            }
            return result;
        }

        public static bool TryValidateAlert(
            PortfolioOperatingAlertSnapshot alert,
            int currentDay,
            out string error)
        {
            if (alert == null || !StableIdentifier.IsValid(alert.alertId) ||
                !StableIdentifier.IsValid(alert.problemTypeId) ||
                !Enum.IsDefined(typeof(PortfolioAlertSeverity), alert.severity) ||
                alert.openedDay < 1 || alert.openedDay > currentDay ||
                alert.resolvedDay < 0 || alert.resolvedDay > currentDay ||
                (alert.resolvedDay != 0 &&
                 alert.resolvedDay < alert.openedDay) ||
                string.IsNullOrWhiteSpace(alert.summary) ||
                string.IsNullOrWhiteSpace(alert.recoveryAction))
            {
                error = "Operating alert contains invalid identity, timing, severity, or recovery guidance.";
                return false;
            }

            error = null;
            return true;
        }

        public static PortfolioEmployeeScheduleSnapshot Clone(
            PortfolioEmployeeScheduleSnapshot source)
        {
            return source == null
                ? null
                : new PortfolioEmployeeScheduleSnapshot
                {
                    scheduledDayMask = source.scheduledDayMask,
                    shiftStartMinute = source.shiftStartMinute,
                    shiftEndMinute = source.shiftEndMinute
                };
        }

        public static PortfolioDelegationPolicySnapshot Clone(
            PortfolioDelegationPolicySnapshot source)
        {
            return source == null
                ? null
                : new PortfolioDelegationPolicySnapshot
                {
                    managerCanPurchase = source.managerCanPurchase,
                    managerCanAuthorizeMaintenance =
                        source.managerCanAuthorizeMaintenance,
                    managerCanAdjustPrices = source.managerCanAdjustPrices,
                    dailySpendingLimitCents = source.dailySpendingLimitCents,
                    minimumServiceQuality = source.minimumServiceQuality,
                    minimumProductAvailabilityBasisPoints =
                        source.minimumProductAvailabilityBasisPoints,
                    minimumMaintenanceCondition =
                        source.minimumMaintenanceCondition,
                    maintenancePolicy = source.maintenancePolicy
                };
        }

        public static PortfolioProductInventorySnapshot Clone(
            PortfolioProductInventorySnapshot source)
        {
            return source == null
                ? null
                : new PortfolioProductInventorySnapshot
                {
                    productId = source.productId,
                    quantityUnits = source.quantityUnits,
                    unitCostCents = source.unitCostCents
                };
        }

        public static PortfolioOperatingAlertSnapshot Clone(
            PortfolioOperatingAlertSnapshot source)
        {
            return source == null
                ? null
                : new PortfolioOperatingAlertSnapshot
                {
                    alertId = source.alertId,
                    problemTypeId = source.problemTypeId,
                    severity = source.severity,
                    openedDay = source.openedDay,
                    resolvedDay = source.resolvedDay,
                    acknowledged = source.acknowledged,
                    requiresOwnerAttention = source.requiresOwnerAttention,
                    summary = source.summary,
                    recoveryAction = source.recoveryAction
                };
        }

        public static PortfolioDetailedReconciliationSnapshot Clone(
            PortfolioDetailedReconciliationSnapshot source)
        {
            return source == null
                ? null
                : new PortfolioDetailedReconciliationSnapshot
                {
                    initialized = source.initialized,
                    sessionId = source.sessionId,
                    sessionStartedDay = source.sessionStartedDay,
                    startingInventoryAssetValueCents =
                        source.startingInventoryAssetValueCents,
                    startingDeliveredProcurementInventoryCents =
                        source.startingDeliveredProcurementInventoryCents,
                    startingProcurementPurchaseCents =
                        source.startingProcurementPurchaseCents,
                    startingProcurementDeliveryFeesCents =
                        source.startingProcurementDeliveryFeesCents,
                    startingCustomerSatisfaction =
                        source.startingCustomerSatisfaction,
                    startingMaintenanceCondition =
                        source.startingMaintenanceCondition,
                    grossSalesCents = source.grossSalesCents,
                    costOfGoodsSoldCents = source.costOfGoodsSoldCents,
                    includedOperatingExpensesCents =
                        source.includedOperatingExpensesCents,
                    payrollCents = source.payrollCents,
                    rentCents = source.rentCents,
                    operatingCostCents = source.operatingCostCents,
                    inventoryAcquiredCostCents =
                        source.inventoryAcquiredCostCents,
                    unitsSold = source.unitsSold,
                    transactionCount = source.transactionCount,
                    metrics = source.metrics == null
                        ? null
                        : new DetailedOperationMetricsSnapshot
                        {
                            customerVisits = source.metrics.customerVisits,
                            customersServed = source.metrics.customersServed,
                            customersAbandoned =
                                source.metrics.customersAbandoned,
                            requestedProductUnits =
                                source.metrics.requestedProductUnits,
                            unavailableProductUnits =
                                source.metrics.unavailableProductUnits,
                            standardsTaskComplete =
                                source.metrics.standardsTaskComplete
                        }
                };
        }
    }
}
