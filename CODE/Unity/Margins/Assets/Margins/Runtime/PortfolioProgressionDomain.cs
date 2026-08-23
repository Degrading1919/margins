using System;
using System.Collections.Generic;
using System.Linq;

namespace Margins
{
    public enum PortfolioEmployeeRole
    {
        Cashier,
        StockClerk,
        Manager
    }

    public enum PortfolioTaskFocus
    {
        Service,
        Inventory,
        Standards,
        Balanced
    }

    public enum PortfolioPricingPolicy
    {
        Value,
        Balanced,
        Premium
    }

    public enum PortfolioReorderPolicy
    {
        Lean,
        Balanced,
        Resilient
    }

    [Serializable]
    public sealed class PortfolioEmployeeSnapshot
    {
        public string employeeId;
        public string displayName;
        public string trait;
        public PortfolioEmployeeRole role;
        public PortfolioTaskFocus taskFocus;
        public int skill;
        public int reliability;
        public int satisfaction;
        public long dailyWageCents;
        public long hiringCostCents;
        public string assignedLocationId;
        public int lastTrainingDay;
        public PortfolioEmployeeScheduleSnapshot schedule;

        public EmployeeWorkProfile CreateWorkProfile()
        {
            BusinessWorkFocus workFocus = taskFocus switch
            {
                PortfolioTaskFocus.Service =>
                    BusinessWorkFocus.CustomerService,
                PortfolioTaskFocus.Inventory =>
                    BusinessWorkFocus.ResourceFlow,
                PortfolioTaskFocus.Standards =>
                    BusinessWorkFocus.Standards,
                PortfolioTaskFocus.Balanced =>
                    BusinessWorkFocus.Balanced,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(taskFocus),
                    taskFocus,
                    "Employee task focus is invalid.")
            };
            return new EmployeeWorkProfile(skill, reliability, workFocus);
        }
    }

    [Serializable]
    public sealed class PortfolioLocationReportSnapshot
    {
        public int day;
        public string locationId;
        public int demandUnits;
        public int unitsSold;
        public int lostDemandUnits;
        public int endingInventoryUnits;
        public int reorderedUnits;
        public long unitPriceCents;
        public long grossSalesCents;
        public long costOfGoodsSoldCents;
        public long payrollCents;
        public long rentCents;
        public long inventoryPurchaseCents;
        public long deliveryFeesCents;
        public long baseOperatingCostsCents;
        public long maintenanceCostsCents;
        public long operatingProfitCents;
        public long cashChangeCents;
        public int serviceQuality;
        public int customerSatisfaction;
        public int productAvailabilityBasisPoints;
        public int productMixBasisPoints;
        public int maintenanceCondition;
        public int failurePressure;
        public int openAlertCount;
        public int ownerAttentionAlertCount;
        public string primaryCause;
        public bool isDetailedOperation;
        public bool hasExactMerchandiseSales;
        public List<MerchandiseSaleLineSnapshot> merchandiseSales = new();
    }

    [Serializable]
    public sealed class PortfolioLocationSnapshot
    {
        public string locationId;
        public string displayName;
        public string districtName;
        public string marketSummary;
        public string businessTypeId;
        public string operatingModel;
        public string brandId;
        public string propertyId;
        public string commercialUnitId;
        public int baseDemandUnits;
        public int competitionIndex;
        public int reputation;
        public int inventoryUnits;
        public int inventoryCapacityUnits;
        public long dailyRentCents;
        public long leaseCostCents;
        public long openingInventoryCostCents;
        public PortfolioPricingPolicy pricingPolicy;
        public PortfolioReorderPolicy reorderPolicy;
        public List<MerchandisePriceSnapshot> merchandisePrices = new();
        public List<ShelfMerchandiseAssignmentSnapshot>
            shelfMerchandiseAssignments = new();
        public List<PortfolioProductInventorySnapshot> productInventory = new();
        public PortfolioDelegationPolicySnapshot delegationPolicy;
        public int customerSatisfaction;
        public int serviceQuality;
        public int productAvailabilityBasisPoints;
        public int productMixBasisPoints;
        public int maintenanceCondition;
        public int failurePressure;
        public PortfolioDetailedReconciliationSnapshot detailedReconciliation;
        public List<PortfolioOperatingAlertSnapshot> operatingAlerts = new();
        public int daysOperating;
        public int delegatedDaysOperating;
        public long lifetimeGrossSalesCents;
        public long lifetimeCostOfGoodsSoldCents;
        public long lifetimePayrollCents;
        public long lifetimeRentCents;
        public long lifetimeInventoryPurchaseCents;
        public long lifetimeDeliveryFeesCents;
        public long lifetimeBaseOperatingCostsCents;
        public long lifetimeMaintenanceCostsCents;
        public long lifetimeLeaseAndSetupCents;
        public long lifetimeCashChangeCents;
        public long lifetimeOperatingProfitCents;
        public bool hasLastReport;
        public PortfolioLocationReportSnapshot lastReport;
    }

    [Serializable]
    public sealed class PortfolioProgressionSnapshot
    {
        public const int LegacyVersion = 1;
        public const int VersionTwo = 2;
        public const int PriorVersion = 3;
        public const int CurrentVersion = 4;

        public int version = CurrentVersion;
        public int currentDay = 1;
        public long cashCents;
        public int companyReputation;
        public bool firstShiftCompleted;
        public bool detailedOperationInitialized;
        public string processedDetailedSessionId;
        public long reconciledDetailedGrossSalesCents;
        public long reconciledDetailedCostOfGoodsSoldCents;
        public long reconciledDetailedOperatingExpensesCents;
        public long reconciledDetailedPayrollCents;
        public long reconciledDetailedRentCents;
        public long reconciledDetailedInventoryAcquiredCostCents;
        public int reconciledDetailedUnitsSold;
        public int reconciledDetailedTransactionCount;
        public long lifetimeCorporateCostsCents;
        public ProcurementSnapshot procurement;
        public PortfolioCompanySnapshot company;
        public List<PortfolioEmployeeSnapshot> employees = new();
        public List<PortfolioLocationSnapshot> locations = new();
    }

    public sealed class PortfolioCandidateDefinition
    {
        public PortfolioCandidateDefinition(
            string employeeId,
            string displayName,
            string trait,
            PortfolioEmployeeRole role,
            PortfolioTaskFocus taskFocus,
            int skill,
            int reliability,
            long dailyWageCents,
            long hiringCostCents)
        {
            EmployeeId = employeeId;
            DisplayName = displayName;
            Trait = trait;
            Role = role;
            TaskFocus = taskFocus;
            Skill = skill;
            Reliability = reliability;
            DailyWageCents = dailyWageCents;
            HiringCostCents = hiringCostCents;
        }

        public string EmployeeId { get; }
        public string DisplayName { get; }
        public string Trait { get; }
        public PortfolioEmployeeRole Role { get; }
        public PortfolioTaskFocus TaskFocus { get; }
        public int Skill { get; }
        public int Reliability { get; }
        public long DailyWageCents { get; }
        public long HiringCostCents { get; }
    }

    public sealed class PortfolioLocationDefinition
    {
        public PortfolioLocationDefinition(
            string locationId,
            string displayName,
            string districtName,
            string marketSummary,
            int baseDemandUnits,
            int competitionIndex,
            int startingReputation,
            int openingInventoryUnits,
            int inventoryCapacityUnits,
            long dailyRentCents,
            long leaseCostCents,
            long openingInventoryCostCents,
            BusinessSimulationProfile simulationProfile,
            string businessTypeId = "business-convenience-retail",
            string operatingModel = "Retail goods: receive containers, stock fixtures, scan items, and delegate service.")
        {
            if (simulationProfile == null)
            {
                throw new ArgumentNullException(nameof(simulationProfile));
            }

            LocationId = locationId;
            DisplayName = displayName;
            DistrictName = districtName;
            MarketSummary = marketSummary;
            BaseDemandUnits = baseDemandUnits;
            CompetitionIndex = competitionIndex;
            StartingReputation = startingReputation;
            OpeningInventoryUnits = openingInventoryUnits;
            InventoryCapacityUnits = inventoryCapacityUnits;
            DailyRentCents = dailyRentCents;
            LeaseCostCents = leaseCostCents;
            OpeningInventoryCostCents = openingInventoryCostCents;
            BusinessTypeId = businessTypeId;
            OperatingModel = operatingModel;
            SimulationProfile = simulationProfile;
        }

        public string LocationId { get; }
        public string DisplayName { get; }
        public string DistrictName { get; }
        public string MarketSummary { get; }
        public int BaseDemandUnits { get; }
        public int CompetitionIndex { get; }
        public int StartingReputation { get; }
        public int OpeningInventoryUnits { get; }
        public int InventoryCapacityUnits { get; }
        public long DailyRentCents { get; }
        public long LeaseCostCents { get; }
        public long OpeningInventoryCostCents { get; }
        public string BusinessTypeId { get; }
        public string OperatingModel { get; }
        public BusinessSimulationProfile SimulationProfile { get; }
    }

    public static class PortfolioProgressionRules
    {
        public const string FirstLocationId = "location-mile-7-market";
        public const long StartingCashCents = 1_200_000;
        public const long MinimumCashReserveCents = 50_000;
        public const long AggregateUnitCostCents = 135;
        public const long TrainingCostCents = 25_000;
        public const long PromotionCostCents = 30_000;

        private static readonly PortfolioCandidateDefinition[] CandidateDefinitions =
        {
            new(
                "employee-elena-ruiz",
                "Elena Ruiz",
                "Warm under pressure",
                PortfolioEmployeeRole.Cashier,
                PortfolioTaskFocus.Service,
                62,
                86,
                12_000,
                25_000),
            new(
                "employee-marcus-reed",
                "Marcus Reed",
                "Methodical",
                PortfolioEmployeeRole.StockClerk,
                PortfolioTaskFocus.Inventory,
                58,
                91,
                11_500,
                25_000),
            new(
                "employee-priya-shah",
                "Priya Shah",
                "Decisive",
                PortfolioEmployeeRole.Manager,
                PortfolioTaskFocus.Balanced,
                68,
                84,
                18_500,
                40_000),
            new(
                "employee-jonah-brooks",
                "Jonah Brooks",
                "Patient teacher",
                PortfolioEmployeeRole.Cashier,
                PortfolioTaskFocus.Service,
                55,
                80,
                11_000,
                22_000),
            new(
                "employee-nia-carter",
                "Nia Carter",
                "Fast organizer",
                PortfolioEmployeeRole.StockClerk,
                PortfolioTaskFocus.Inventory,
                65,
                82,
                12_500,
                27_000),
            new(
                "employee-luis-ortega",
                "Luis Ortega",
                "Steady judgment",
                PortfolioEmployeeRole.Manager,
                PortfolioTaskFocus.Standards,
                61,
                93,
                18_000,
                38_000)
        };

        private static readonly PortfolioLocationDefinition FirstLocationDefinition =
            new(
                FirstLocationId,
                "Mile 7 Market",
                "Cedar Junction",
                "Balanced neighborhood traffic with moderate competition.",
                310,
                45,
                50,
                0,
                900,
                9_000,
                0,
                0,
                ConvenienceStoreOperations.Simulation);

        private static readonly PortfolioLocationDefinition[] ExpansionDefinitions =
        {
            new(
                "location-riverbend-market",
                "Riverbend Market",
                "Riverbend",
                "Lower rent and competition; dependable commuter demand.",
                370,
                32,
                46,
                520,
                1_000,
                12_500,
                450_000,
                125_000,
                ConvenienceStoreOperations.Simulation),
            new(
                "location-downtown-market",
                "Exchange Market",
                "Downtown Exchange",
                "High foot traffic and upside, with high rent and aggressive competition.",
                500,
                72,
                44,
                650,
                1_200,
                22_000,
                650_000,
                175_000,
                ConvenienceStoreOperations.Simulation)
        };

        public static IReadOnlyList<PortfolioCandidateDefinition> Candidates =>
            CandidateDefinitions;

        public static IReadOnlyList<PortfolioLocationDefinition> ExpansionOptions =>
            ExpansionDefinitions;

        public static PortfolioLocationDefinition FirstLocation =>
            FirstLocationDefinition;

        public static bool TryGetCandidate(
            string employeeId,
            out PortfolioCandidateDefinition definition)
        {
            definition = CandidateDefinitions.FirstOrDefault(candidate =>
                string.Equals(candidate.EmployeeId, employeeId, StringComparison.Ordinal));
            return definition != null;
        }

        public static bool TryGetLocationDefinition(
            string locationId,
            out PortfolioLocationDefinition definition)
        {
            if (string.Equals(
                    locationId,
                    FirstLocationDefinition.LocationId,
                    StringComparison.Ordinal))
            {
                definition = FirstLocationDefinition;
                return true;
            }

            definition = ExpansionDefinitions.FirstOrDefault(option =>
                string.Equals(option.LocationId, locationId, StringComparison.Ordinal));
            return definition != null;
        }
    }

    /// <summary>
    /// Deterministic company-level simulation shared by the in-world first store,
    /// remote management, delegated days, expansion, reporting, and persistence.
    /// </summary>
    public sealed class PortfolioProgression
    {
        private PortfolioProgressionSnapshot state;

        private PortfolioProgression(PortfolioProgressionSnapshot initialState)
        {
            state = Clone(initialState);
        }

        public PortfolioProgressionSnapshot CreateSnapshot()
        {
            return Clone(state);
        }

        public int CurrentDay => state.currentDay;
        public long CashCents => state.cashCents;
        public int CompanyReputation => state.companyReputation;
        public bool FirstShiftCompleted => state.firstShiftCompleted;
        public long ProcurementTick => state.procurement.currentTick;
        public IReadOnlyList<PurchaseOrderSnapshot> PurchaseOrders =>
            ProcurementLedger.CopySnapshot(state.procurement).orders.AsReadOnly();
        public IReadOnlyList<PortfolioEmployeeSnapshot> Employees =>
            state.employees.AsReadOnly();
        public IReadOnlyList<PortfolioLocationSnapshot> Locations =>
            state.locations.AsReadOnly();

        public static PortfolioProgression CreateInitial()
        {
            PortfolioProgressionSnapshot initial = new()
            {
                cashCents = PortfolioProgressionRules.StartingCashCents,
                companyReputation = 50,
                firstShiftCompleted = false,
                processedDetailedSessionId = null,
                procurement = ProcurementLedger.CreateInitial(
                    ConvenienceStoreProcurement.Catalog).CreateSnapshot()
            };
            initial.locations.Add(CreateLocation(
                PortfolioProgressionRules.FirstLocation));
            initial.company = PortfolioPropertyRules.CreateForLocations(
                initial.locations);
            return new PortfolioProgression(initial);
        }

        public static bool TryRestore(
            PortfolioProgressionSnapshot snapshot,
            out PortfolioProgression progression,
            out string error)
        {
            progression = null;
            if (!TryValidateSnapshot(snapshot, out error))
            {
                return false;
            }

            progression = new PortfolioProgression(snapshot);
            error = null;
            return true;
        }

        public static bool TryValidateSnapshot(
            PortfolioProgressionSnapshot snapshot,
            out string error)
        {
            if (!TryNormalizeSnapshot(
                    snapshot,
                    out PortfolioProgressionSnapshot accepted,
                    out error))
            {
                return false;
            }
            snapshot = accepted;

            if (snapshot.version != PortfolioProgressionSnapshot.CurrentVersion)
            {
                error = "Portfolio snapshot version is missing or unsupported.";
                return false;
            }

            if (snapshot.currentDay < 1 ||
                snapshot.cashCents < 0 ||
                snapshot.companyReputation < 0 ||
                snapshot.companyReputation > 100 ||
                snapshot.employees == null ||
                snapshot.locations == null ||
                snapshot.locations.Count == 0)
            {
                error = "Portfolio snapshot contains invalid company totals or collections.";
                return false;
            }

            if (!ProcurementLedger.TryValidateSnapshot(
                    ConvenienceStoreProcurement.Catalog,
                    snapshot.procurement,
                    out error))
            {
                return false;
            }

            if (snapshot.detailedOperationInitialized ||
                !string.IsNullOrWhiteSpace(snapshot.processedDetailedSessionId) ||
                snapshot.reconciledDetailedGrossSalesCents != 0 ||
                snapshot.reconciledDetailedCostOfGoodsSoldCents != 0 ||
                snapshot.reconciledDetailedOperatingExpensesCents != 0 ||
                snapshot.reconciledDetailedPayrollCents != 0 ||
                snapshot.reconciledDetailedRentCents != 0 ||
                snapshot.reconciledDetailedInventoryAcquiredCostCents != 0 ||
                snapshot.reconciledDetailedUnitsSold != 0 ||
                snapshot.reconciledDetailedTransactionCount != 0 ||
                snapshot.lifetimeCorporateCostsCents < 0)
            {
                error = "Current portfolio snapshots must use location-scoped detailed reconciliation state.";
                return false;
            }

            HashSet<string> locationIds = new(StringComparer.Ordinal);
            foreach (PortfolioLocationSnapshot location in snapshot.locations)
            {
                if (!TryValidateLocation(location, out error) ||
                    !locationIds.Add(location.locationId))
                {
                    error ??= "Portfolio contains duplicate location identifiers.";
                    return false;
                }
            }

            if (!locationIds.Contains(PortfolioProgressionRules.FirstLocationId))
            {
                error = "Portfolio snapshot is missing the first store.";
                return false;
            }

            if (!PortfolioPropertyRules.TryValidate(
                    snapshot.company,
                    snapshot.locations,
                    snapshot.currentDay,
                    out error))
            {
                return false;
            }

            PortfolioLocationSnapshot firstLocation = snapshot.locations.First(
                location => string.Equals(
                    location.locationId,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal));
            if ((!snapshot.firstShiftCompleted &&
                 firstLocation.detailedReconciliation?.transactionCount > 0) ||
                (snapshot.firstShiftCompleted &&
                 (firstLocation.daysOperating < 1 ||
                  firstLocation.lifetimeGrossSalesCents <= 0)))
            {
                error = "First-shift progression disagrees with first-location operating history.";
                return false;
            }

            foreach (PurchaseOrderSnapshot order in snapshot.procurement.orders)
            {
                if (!locationIds.Contains(order.locationId))
                {
                    error =
                        $"Purchase order '{order.orderId}' references an unavailable portfolio location.";
                    return false;
                }
            }

            try
            {
                foreach (PortfolioLocationSnapshot location in snapshot.locations)
                {
                    GetProcurementTotals(
                        snapshot.procurement,
                        location.locationId,
                        out long procurementPurchases,
                        out long procurementDeliveryFees,
                        out _);
                    if (location.lifetimeInventoryPurchaseCents <
                            procurementPurchases ||
                        location.lifetimeDeliveryFeesCents !=
                            procurementDeliveryFees)
                    {
                        error =
                            $"Location '{location.locationId}' procurement costs do not reconcile to its purchase orders.";
                        return false;
                    }
                }
            }
            catch (OverflowException)
            {
                error = "Portfolio procurement totals overflowed integer-cent storage.";
                return false;
            }

            if (!TryValidateFinancialReconciliation(snapshot, out error))
            {
                return false;
            }

            if (!snapshot.firstShiftCompleted &&
                !firstLocation.detailedReconciliation.initialized &&
                (snapshot.currentDay != 1 ||
                 snapshot.cashCents != PortfolioProgressionRules.StartingCashCents ||
                 snapshot.companyReputation != 50 ||
                 snapshot.employees.Count != 0 ||
                 snapshot.locations.Count != 1 ||
                 snapshot.locations[0].inventoryUnits !=
                 PortfolioProgressionRules.FirstLocation.OpeningInventoryUnits ||
                 snapshot.locations[0].pricingPolicy != PortfolioPricingPolicy.Balanced ||
                 snapshot.locations[0].reorderPolicy != PortfolioReorderPolicy.Balanced ||
                 snapshot.locations[0].daysOperating != 0 ||
                 snapshot.locations[0].lifetimeGrossSalesCents != 0 ||
                 snapshot.locations[0].lifetimeOperatingProfitCents != 0 ||
                 snapshot.procurement.currentTick != 0 ||
                 snapshot.procurement.orders.Count != 0))
            {
                error = "Portfolio progression exists before the hands-on first shift was completed.";
                return false;
            }

            foreach (PortfolioLocationSnapshot location in snapshot.locations)
            {
                if (location.daysOperating > snapshot.currentDay ||
                    location.delegatedDaysOperating > location.daysOperating ||
                    (location.daysOperating == 0 && location.hasLastReport) ||
                     (location.daysOperating > 0 &&
                      (!location.hasLastReport ||
                       location.lastReport.day > snapshot.currentDay)))
                {
                    error = "Portfolio location operating history contradicts the company day or latest report.";
                    return false;
                }
            }

            HashSet<string> employeeIds = new(StringComparer.Ordinal);
            HashSet<string> occupiedAssignments = new(StringComparer.Ordinal);
            foreach (PortfolioEmployeeSnapshot employee in snapshot.employees)
            {
                if (!TryValidateEmployee(
                        employee,
                        locationIds,
                        snapshot.currentDay,
                        out error) ||
                    !employeeIds.Add(employee.employeeId))
                {
                    error ??= "Portfolio contains duplicate employee identifiers.";
                    return false;
                }

                string assignmentKey =
                    $"{employee.assignedLocationId}:{employee.role}";
                if (!occupiedAssignments.Add(assignmentKey))
                {
                    error =
                        $"Location '{employee.assignedLocationId}' has more than one assigned {employee.role}.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public bool TryPostDetailedShift(
            string sessionId,
            StoreSessionTotals totals,
            int remainingInventoryUnits,
            out bool alreadyPosted,
            out string error)
        {
            long inventoryAssetValueCents;
            try
            {
                inventoryAssetValueCents = checked(
                    (long)remainingInventoryUnits *
                    PortfolioProgressionRules.AggregateUnitCostCents);
            }
            catch (OverflowException)
            {
                alreadyPosted = false;
                error = "Detailed inventory value overflowed integer-cent storage.";
                return false;
            }

            return TryReconcileDetailedOperation(
                sessionId,
                totals,
                remainingInventoryUnits,
                inventoryAssetValueCents,
                out alreadyPosted,
                out error);
        }

        public bool TryReconcileDetailedOperation(
            string sessionId,
            StoreSessionTotals totals,
            int remainingInventoryUnits,
            long inventoryAssetValueCents,
            out bool unchanged,
            out string error)
        {
            return TryReconcileDetailedOperation(
                sessionId,
                totals,
                remainingInventoryUnits,
                inventoryAssetValueCents,
                null,
                out unchanged,
                out error);
        }

        public bool TryReconcileDetailedOperation(
            string sessionId,
            StoreSessionTotals totals,
            int remainingInventoryUnits,
            long inventoryAssetValueCents,
            IReadOnlyList<MerchandiseSaleLineSnapshot> merchandiseSales,
            out bool unchanged,
            out string error)
        {
            return TryReconcileDetailedOperation(
                PortfolioProgressionRules.FirstLocationId,
                sessionId,
                totals,
                remainingInventoryUnits,
                inventoryAssetValueCents,
                null,
                merchandiseSales,
                null,
                out unchanged,
                out error);
        }

        public bool TryRebaseDetailedOperation(
            string locationId,
            string sessionId,
            StoreSessionTotals totals,
            int detailedInventoryUnits,
            long detailedInventoryAssetValueCents,
            IReadOnlyList<PortfolioProductInventorySnapshot>
                detailedProductInventory,
            DetailedOperationMetricsSnapshot metrics,
            out string error)
        {
            if (!FirstStoreIdentifier.IsValid(locationId) ||
                !FirstStoreIdentifier.IsValid(sessionId) || totals == null ||
                !totals.IsValid || detailedInventoryAssetValueCents < 0 ||
                !TryGetLocation(
                    state,
                    locationId,
                    out PortfolioLocationSnapshot current) ||
                detailedInventoryUnits < 0 ||
                detailedInventoryUnits > current.inventoryCapacityUnits)
            {
                error =
                    "A valid location, detailed session, totals, and physical inventory are required to rebase detailed operation.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(
                    state.company.activeDetailedLocationId) &&
                !string.Equals(
                    state.company.activeDetailedLocationId,
                    locationId,
                    StringComparison.Ordinal))
            {
                error =
                    "A different portfolio location is currently active in detailed simulation.";
                return false;
            }

            List<PortfolioProductInventorySnapshot> acceptedInventory =
                detailedProductInventory?
                    .Select(PortfolioOperationsRules.Clone)
                    .ToList();
            if (!PortfolioOperationsRules.TryValidateProductInventory(
                    acceptedInventory,
                    current.merchandisePrices,
                    detailedInventoryUnits,
                    out error) ||
                CalculateProductInventoryValue(acceptedInventory) !=
                detailedInventoryAssetValueCents)
            {
                error ??=
                    "Detailed inventory value does not reconcile to its product baseline.";
                return false;
            }

            DetailedOperationMetricsSnapshot acceptedMetrics = metrics == null
                ? new DetailedOperationMetricsSnapshot
                {
                    customerVisits = totals.transactionCount,
                    customersServed = totals.transactionCount,
                    requestedProductUnits = totals.unitsSold,
                    standardsTaskComplete = true
                }
                : new DetailedOperationMetricsSnapshot
                {
                    customerVisits = metrics.customerVisits,
                    customersServed = metrics.customersServed,
                    customersAbandoned = metrics.customersAbandoned,
                    requestedProductUnits = metrics.requestedProductUnits,
                    unavailableProductUnits = metrics.unavailableProductUnits,
                    standardsTaskComplete = metrics.standardsTaskComplete
                };
            if (!acceptedMetrics.TryValidate(out error))
            {
                return false;
            }

            PortfolioDetailedReconciliationSnapshot prior =
                current.detailedReconciliation;
            if (prior == null || !prior.initialized ||
                prior.sessionStartedDay >= state.currentDay ||
                !current.hasLastReport || current.lastReport == null ||
                current.lastReport.day != state.currentDay ||
                current.lastReport.isDetailedOperation)
            {
                error =
                    "Detailed operation can only be rebased after aggregate simulation has advanced this location beyond its prior detailed baseline.";
                return false;
            }

            long scheduledPayroll;
            try
            {
                scheduledPayroll = state.employees
                    .Where(employee => string.Equals(
                                           employee.assignedLocationId,
                                           locationId,
                                           StringComparison.Ordinal) &&
                                       PortfolioOperationsRules.IsScheduled(
                                           employee.schedule,
                                           state.currentDay))
                    .Sum(employee => employee.dailyWageCents);
            }
            catch (OverflowException)
            {
                error =
                    "Detailed scheduled payroll overflowed integer-cent storage.";
                return false;
            }

            long detailedPayrollCents = Math.Min(
                scheduledPayroll,
                totals.includedOperatingExpensesCents);
            long afterPayroll = totals.includedOperatingExpensesCents -
                                detailedPayrollCents;
            long detailedRentCents = Math.Min(
                EffectiveDailyRent(state.company, current),
                afterPayroll);
            long detailedOperatingCostCents = afterPayroll -
                                              detailedRentCents;
            GetProcurementTotals(
                state.procurement,
                locationId,
                out long procurementPurchaseCents,
                out long procurementDeliveryFeesCents,
                out long deliveredProcurementInventoryCents);

            PortfolioProgressionSnapshot candidate = Clone(state);
            PortfolioLocationSnapshot location = candidate.locations.First(
                value => string.Equals(
                    value.locationId,
                    locationId,
                    StringComparison.Ordinal));
            location.detailedReconciliation =
                new PortfolioDetailedReconciliationSnapshot
                {
                    initialized = true,
                    sessionId = sessionId,
                    sessionStartedDay = candidate.currentDay,
                    startingInventoryAssetValueCents =
                        CalculateProductInventoryValue(
                            location.productInventory),
                    startingDeliveredProcurementInventoryCents =
                        deliveredProcurementInventoryCents,
                    startingProcurementPurchaseCents =
                        procurementPurchaseCents,
                    startingProcurementDeliveryFeesCents =
                        procurementDeliveryFeesCents,
                    startingCustomerSatisfaction =
                        location.customerSatisfaction,
                    startingMaintenanceCondition =
                        location.maintenanceCondition,
                    grossSalesCents = totals.grossSalesCents,
                    costOfGoodsSoldCents =
                        totals.costOfGoodsSoldCents,
                    includedOperatingExpensesCents =
                        totals.includedOperatingExpensesCents,
                    payrollCents = detailedPayrollCents,
                    rentCents = detailedRentCents,
                    operatingCostCents = detailedOperatingCostCents,
                    inventoryAcquiredCostCents = 0,
                    unitsSold = totals.unitsSold,
                    transactionCount = totals.transactionCount,
                    metrics = acceptedMetrics,
                    usesDetailedInventoryBaseline = true,
                    detailedInventoryBaselineValueCents =
                        detailedInventoryAssetValueCents,
                    detailedProductInventoryBaseline = acceptedInventory
                };
            return TryCommit(candidate, out error);
        }

        /// <summary>
        /// Establishes a fresh reconciliation baseline when an existing Unity
        /// detailed-store rig is rebound to a portfolio location. This does not
        /// post money, inventory, customers, or progress; subsequent detailed
        /// deltas remain authoritative through TryReconcileDetailedOperation.
        /// </summary>
        public bool TryEstablishDetailedOperationBaseline(
            string locationId,
            string sessionId,
            StoreSessionTotals totals,
            int detailedInventoryUnits,
            long detailedInventoryAssetValueCents,
            IReadOnlyList<PortfolioProductInventorySnapshot>
                detailedProductInventory,
            DetailedOperationMetricsSnapshot metrics,
            out string error)
        {
            if (!FirstStoreIdentifier.IsValid(locationId) ||
                !FirstStoreIdentifier.IsValid(sessionId) || totals == null ||
                !totals.IsValid || detailedInventoryAssetValueCents < 0 ||
                !TryGetLocation(
                    state,
                    locationId,
                    out PortfolioLocationSnapshot current) ||
                detailedInventoryUnits < 0 ||
                detailedInventoryUnits > current.inventoryCapacityUnits)
            {
                error =
                    "A valid active location, detailed session, totals, and inventory baseline are required.";
                return false;
            }

            if (!string.Equals(
                    state.company.activeDetailedLocationId,
                    locationId,
                    StringComparison.Ordinal))
            {
                error =
                    "The selected portfolio location must be active before its detailed baseline is established.";
                return false;
            }

            List<PortfolioProductInventorySnapshot> acceptedInventory =
                detailedProductInventory?
                    .Select(PortfolioOperationsRules.Clone)
                    .ToList();
            if (!PortfolioOperationsRules.TryValidateProductInventory(
                    acceptedInventory,
                    current.merchandisePrices,
                    detailedInventoryUnits,
                    out error) ||
                CalculateProductInventoryValue(acceptedInventory) !=
                detailedInventoryAssetValueCents)
            {
                error ??=
                    "Detailed inventory value does not reconcile to its product baseline.";
                return false;
            }

            DetailedOperationMetricsSnapshot acceptedMetrics = metrics == null
                ? new DetailedOperationMetricsSnapshot
                {
                    customerVisits = totals.transactionCount,
                    customersServed = totals.transactionCount,
                    requestedProductUnits = totals.unitsSold,
                    standardsTaskComplete = true
                }
                : new DetailedOperationMetricsSnapshot
                {
                    customerVisits = metrics.customerVisits,
                    customersServed = metrics.customersServed,
                    customersAbandoned = metrics.customersAbandoned,
                    requestedProductUnits = metrics.requestedProductUnits,
                    unavailableProductUnits = metrics.unavailableProductUnits,
                    standardsTaskComplete = metrics.standardsTaskComplete
                };
            if (!acceptedMetrics.TryValidate(out error))
            {
                return false;
            }

            long scheduledPayroll;
            try
            {
                scheduledPayroll = state.employees
                    .Where(employee => string.Equals(
                                           employee.assignedLocationId,
                                           locationId,
                                           StringComparison.Ordinal) &&
                                       PortfolioOperationsRules.IsScheduled(
                                           employee.schedule,
                                           state.currentDay))
                    .Sum(employee => employee.dailyWageCents);
            }
            catch (OverflowException)
            {
                error =
                    "Detailed scheduled payroll overflowed integer-cent storage.";
                return false;
            }

            long detailedPayrollCents = Math.Min(
                scheduledPayroll,
                totals.includedOperatingExpensesCents);
            long afterPayroll = totals.includedOperatingExpensesCents -
                                detailedPayrollCents;
            long detailedRentCents = Math.Min(
                EffectiveDailyRent(state.company, current),
                afterPayroll);
            long detailedOperatingCostCents = afterPayroll -
                                              detailedRentCents;
            GetProcurementTotals(
                state.procurement,
                locationId,
                out long procurementPurchaseCents,
                out long procurementDeliveryFeesCents,
                out long deliveredProcurementInventoryCents);

            PortfolioProgressionSnapshot candidate = Clone(state);
            PortfolioLocationSnapshot location = candidate.locations.First(
                value => string.Equals(
                    value.locationId,
                    locationId,
                    StringComparison.Ordinal));
            location.detailedReconciliation =
                new PortfolioDetailedReconciliationSnapshot
                {
                    initialized = true,
                    sessionId = sessionId,
                    sessionStartedDay = candidate.currentDay,
                    startingInventoryAssetValueCents =
                        CalculateProductInventoryValue(
                            location.productInventory),
                    startingDeliveredProcurementInventoryCents =
                        deliveredProcurementInventoryCents,
                    startingProcurementPurchaseCents =
                        procurementPurchaseCents,
                    startingProcurementDeliveryFeesCents =
                        procurementDeliveryFeesCents,
                    startingCustomerSatisfaction =
                        location.customerSatisfaction,
                    startingMaintenanceCondition =
                        location.maintenanceCondition,
                    grossSalesCents = totals.grossSalesCents,
                    costOfGoodsSoldCents =
                        totals.costOfGoodsSoldCents,
                    includedOperatingExpensesCents =
                        totals.includedOperatingExpensesCents,
                    payrollCents = detailedPayrollCents,
                    rentCents = detailedRentCents,
                    operatingCostCents = detailedOperatingCostCents,
                    inventoryAcquiredCostCents = 0,
                    unitsSold = totals.unitsSold,
                    transactionCount = totals.transactionCount,
                    metrics = acceptedMetrics,
                    usesDetailedInventoryBaseline = true,
                    detailedInventoryBaselineValueCents =
                        detailedInventoryAssetValueCents,
                    detailedProductInventoryBaseline = acceptedInventory
                };
            return TryCommit(candidate, out error);
        }

        public bool TryReconcileDetailedOperation(
            string locationId,
            string sessionId,
            StoreSessionTotals totals,
            int remainingInventoryUnits,
            long inventoryAssetValueCents,
            IReadOnlyList<PortfolioProductInventorySnapshot> productInventory,
            IReadOnlyList<MerchandiseSaleLineSnapshot> merchandiseSales,
            DetailedOperationMetricsSnapshot metrics,
            out bool unchanged,
            out string error)
        {
            unchanged = false;
            if (!FirstStoreIdentifier.IsValid(locationId) ||
                !FirstStoreIdentifier.IsValid(sessionId) || totals == null ||
                !totals.IsValid || inventoryAssetValueCents < 0 ||
                !TryGetLocation(state, locationId, out PortfolioLocationSnapshot current) ||
                !PortfolioProgressionRules.TryGetLocationDefinition(
                    locationId,
                    out PortfolioLocationDefinition locationDefinition))
            {
                error = "A valid portfolio location, detailed session, inventory value, and reconciled totals are required.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(
                    state.company.activeDetailedLocationId) &&
                !string.Equals(
                    state.company.activeDetailedLocationId,
                    locationId,
                    StringComparison.Ordinal))
            {
                error = "A different portfolio location is currently active in detailed simulation.";
                return false;
            }

            bool hasExactMerchandiseSales = merchandiseSales != null;
            if (hasExactMerchandiseSales &&
                !MerchandisingRules.TryValidateSalesBreakdown(
                    merchandiseSales,
                    totals.unitsSold,
                    totals.grossSalesCents,
                    out error))
            {
                return false;
            }

            if (remainingInventoryUnits < 0 ||
                remainingInventoryUnits > current.inventoryCapacityUnits)
            {
                error = "Detailed physical inventory cannot be reconciled to the selected business location.";
                return false;
            }

            List<PortfolioProductInventorySnapshot> detailedInventory =
                productInventory == null
                    ? PortfolioOperationsRules.CreateProvisionalProductInventory(
                        current.merchandisePrices,
                        remainingInventoryUnits,
                        PortfolioProgressionRules.AggregateUnitCostCents)
                    : productInventory
                        .Select(PortfolioOperationsRules.Clone)
                        .ToList();
            if (!PortfolioOperationsRules.TryValidateProductInventory(
                    detailedInventory,
                    current.merchandisePrices,
                    remainingInventoryUnits,
                    out error))
            {
                return false;
            }

            DetailedOperationMetricsSnapshot acceptedMetrics = metrics == null
                ? new DetailedOperationMetricsSnapshot
                {
                    customerVisits = totals.transactionCount,
                    customersServed = totals.transactionCount,
                    customersAbandoned = 0,
                    requestedProductUnits = totals.unitsSold,
                    unavailableProductUnits = 0,
                    standardsTaskComplete = true
                }
                : new DetailedOperationMetricsSnapshot
                {
                    customerVisits = metrics.customerVisits,
                    customersServed = metrics.customersServed,
                    customersAbandoned = metrics.customersAbandoned,
                    requestedProductUnits = metrics.requestedProductUnits,
                    unavailableProductUnits = metrics.unavailableProductUnits,
                    standardsTaskComplete = metrics.standardsTaskComplete
                };
            if (!acceptedMetrics.TryValidate(out error))
            {
                return false;
            }

            PortfolioDetailedReconciliationSnapshot prior =
                current.detailedReconciliation ??
                new PortfolioDetailedReconciliationSnapshot();
            bool newSession = !prior.initialized ||
                              !string.Equals(
                                  prior.sessionId,
                                  sessionId,
                                  StringComparison.Ordinal);
            if (newSession && prior.initialized &&
                prior.sessionStartedDay >= state.currentDay)
            {
                error = "A different detailed operating session is already authoritative for this location and day.";
                return false;
            }

            GetProcurementTotals(
                state.procurement,
                locationId,
                out long procurementPurchaseCents,
                out long procurementDeliveryFeesCents,
                out long deliveredProcurementInventoryCents);
            if (newSession)
            {
                prior = new PortfolioDetailedReconciliationSnapshot
                {
                    initialized = true,
                    sessionId = sessionId,
                    sessionStartedDay = state.currentDay,
                    startingInventoryAssetValueCents =
                        CalculateProductInventoryValue(current.productInventory),
                    startingDeliveredProcurementInventoryCents =
                        deliveredProcurementInventoryCents,
                    startingProcurementPurchaseCents =
                        procurementPurchaseCents,
                    startingProcurementDeliveryFeesCents =
                        procurementDeliveryFeesCents,
                    startingCustomerSatisfaction =
                        current.customerSatisfaction,
                    startingMaintenanceCondition =
                        current.maintenanceCondition,
                    metrics = new DetailedOperationMetricsSnapshot()
                };
            }

            List<PortfolioProductInventorySnapshot> reconciledInventory;
            if (prior.usesDetailedInventoryBaseline && !newSession)
            {
                if (!TryApplyDetailedInventoryDelta(
                        current.productInventory,
                        prior.detailedProductInventoryBaseline,
                        detailedInventory,
                        current.inventoryCapacityUnits,
                        out reconciledInventory,
                        out error))
                {
                    return false;
                }
            }
            else
            {
                reconciledInventory = detailedInventory;
            }
            int reconciledInventoryUnits = reconciledInventory.Sum(value =>
                value.quantityUnits);

            long scheduledPayroll;
            try
            {
                scheduledPayroll = state.employees
                    .Where(employee => string.Equals(
                                           employee.assignedLocationId,
                                           locationId,
                                           StringComparison.Ordinal) &&
                                       PortfolioOperationsRules.IsScheduled(
                                           employee.schedule,
                                           state.currentDay))
                    .Sum(employee => employee.dailyWageCents);
            }
            catch (OverflowException)
            {
                error = "Detailed scheduled payroll overflowed integer-cent storage.";
                return false;
            }

            long detailedPayrollCents = Math.Min(
                scheduledPayroll,
                totals.includedOperatingExpensesCents);
            long afterPayroll = totals.includedOperatingExpensesCents -
                                detailedPayrollCents;
            long detailedRentCents = Math.Min(
                EffectiveDailyRent(state.company, current),
                afterPayroll);
            long detailedOperatingCostCents = afterPayroll - detailedRentCents;

            long acquiredInventoryCost;
            long grossSalesDelta;
            long costOfGoodsDelta;
            long expenseDelta;
            long payrollDelta;
            long rentDelta;
            long operatingCostDelta;
            long purchaseDelta;
            long cashDelta;
            long profitDelta;
            long sessionPurchaseCents;
            long sessionDeliveryFeesCents;
            try
            {
                grossSalesDelta = checked(
                    totals.grossSalesCents - prior.grossSalesCents);
                costOfGoodsDelta = checked(
                    totals.costOfGoodsSoldCents - prior.costOfGoodsSoldCents);
                expenseDelta = checked(
                    totals.includedOperatingExpensesCents -
                    prior.includedOperatingExpensesCents);
                payrollDelta = checked(
                    detailedPayrollCents - prior.payrollCents);
                rentDelta = checked(detailedRentCents - prior.rentCents);
                operatingCostDelta = checked(
                    detailedOperatingCostCents - prior.operatingCostCents);
                long deliveredDuringSession = checked(
                    deliveredProcurementInventoryCents -
                    prior.startingDeliveredProcurementInventoryCents);
                if (prior.usesDetailedInventoryBaseline && !newSession)
                {
                    purchaseDelta = checked(
                        inventoryAssetValueCents -
                        prior.detailedInventoryBaselineValueCents +
                        costOfGoodsDelta - deliveredDuringSession);
                    acquiredInventoryCost = checked(
                        prior.inventoryAcquiredCostCents + purchaseDelta);
                }
                else
                {
                    long physicalInventoryAcquiredCost = checked(
                        inventoryAssetValueCents +
                        totals.costOfGoodsSoldCents);
                    acquiredInventoryCost = checked(
                        physicalInventoryAcquiredCost -
                        prior.startingInventoryAssetValueCents -
                        deliveredDuringSession);
                    purchaseDelta = checked(
                        acquiredInventoryCost -
                        prior.inventoryAcquiredCostCents);
                }
                cashDelta = checked(
                    grossSalesDelta - expenseDelta - purchaseDelta);
                profitDelta = checked(
                    grossSalesDelta - costOfGoodsDelta - expenseDelta);
                sessionPurchaseCents = checked(
                    procurementPurchaseCents -
                    prior.startingProcurementPurchaseCents);
                sessionDeliveryFeesCents = checked(
                    procurementDeliveryFeesCents -
                    prior.startingProcurementDeliveryFeesCents);
            }
            catch (OverflowException)
            {
                error = "Detailed operation reconciliation overflowed integer-cent storage.";
                return false;
            }

            if (acquiredInventoryCost < 0 || grossSalesDelta < 0 ||
                costOfGoodsDelta < 0 || expenseDelta < 0 ||
                payrollDelta < 0 || rentDelta < 0 ||
                operatingCostDelta < 0 || purchaseDelta < 0 ||
                sessionPurchaseCents < 0 || sessionDeliveryFeesCents < 0 ||
                totals.unitsSold < prior.unitsSold ||
                totals.transactionCount < prior.transactionCount ||
                acceptedMetrics.customerVisits < prior.metrics.customerVisits ||
                acceptedMetrics.customersServed < prior.metrics.customersServed ||
                acceptedMetrics.customersAbandoned <
                prior.metrics.customersAbandoned ||
                acceptedMetrics.requestedProductUnits <
                prior.metrics.requestedProductUnits ||
                acceptedMetrics.unavailableProductUnits <
                prior.metrics.unavailableProductUnits)
            {
                error = "Detailed operation totals moved backward or no longer reconcile to session-start inventory.";
                return false;
            }

            bool noChange = !newSession && grossSalesDelta == 0 &&
                            costOfGoodsDelta == 0 && expenseDelta == 0 &&
                            purchaseDelta == 0 &&
                            totals.unitsSold == prior.unitsSold &&
                            totals.transactionCount == prior.transactionCount &&
                            DetailedMetricsEqual(acceptedMetrics, prior.metrics) &&
                            ProductInventoryEqual(
                                current.productInventory,
                                reconciledInventory);
            if (noChange)
            {
                unchanged = true;
                error = null;
                return true;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            PortfolioLocationSnapshot location = candidate.locations.First(value =>
                string.Equals(value.locationId, locationId, StringComparison.Ordinal));
            bool firstCompletedSale =
                string.Equals(
                    locationId,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal) &&
                !candidate.firstShiftCompleted && totals.transactionCount > 0;
            try
            {
                candidate.cashCents = checked(candidate.cashCents + cashDelta);
                location.lifetimeGrossSalesCents = checked(
                    location.lifetimeGrossSalesCents + grossSalesDelta);
                location.lifetimeCostOfGoodsSoldCents = checked(
                    location.lifetimeCostOfGoodsSoldCents + costOfGoodsDelta);
                location.lifetimePayrollCents = checked(
                    location.lifetimePayrollCents + payrollDelta);
                location.lifetimeRentCents = checked(
                    location.lifetimeRentCents + rentDelta);
                location.lifetimeBaseOperatingCostsCents = checked(
                    location.lifetimeBaseOperatingCostsCents +
                    operatingCostDelta);
                location.lifetimeInventoryPurchaseCents = checked(
                    location.lifetimeInventoryPurchaseCents + purchaseDelta);
                location.lifetimeOperatingProfitCents = checked(
                    location.lifetimeOperatingProfitCents + profitDelta);
                location.lifetimeCashChangeCents = checked(
                    location.lifetimeCashChangeCents + cashDelta);
            }
            catch (OverflowException)
            {
                error = "Detailed operation would overflow company or location totals.";
                return false;
            }

            if (candidate.cashCents < 0)
            {
                error = "Detailed operation would create unsupported negative cash.";
                return false;
            }

            PortfolioDetailedReconciliationSnapshot reconciliation =
                PortfolioOperationsRules.Clone(prior);
            reconciliation.grossSalesCents = totals.grossSalesCents;
            reconciliation.costOfGoodsSoldCents = totals.costOfGoodsSoldCents;
            reconciliation.includedOperatingExpensesCents =
                totals.includedOperatingExpensesCents;
            reconciliation.payrollCents = detailedPayrollCents;
            reconciliation.rentCents = detailedRentCents;
            reconciliation.operatingCostCents = detailedOperatingCostCents;
            reconciliation.inventoryAcquiredCostCents = acquiredInventoryCost;
            reconciliation.unitsSold = totals.unitsSold;
            reconciliation.transactionCount = totals.transactionCount;
            reconciliation.metrics = acceptedMetrics;
            if (reconciliation.usesDetailedInventoryBaseline)
            {
                reconciliation.startingDeliveredProcurementInventoryCents =
                    deliveredProcurementInventoryCents;
                reconciliation.detailedInventoryBaselineValueCents =
                    inventoryAssetValueCents;
                reconciliation.detailedProductInventoryBaseline =
                    detailedInventory
                        .Select(PortfolioOperationsRules.Clone)
                        .ToList();
            }
            location.detailedReconciliation = reconciliation;

            if (string.Equals(
                    locationId,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal) && totals.transactionCount > 0)
            {
                candidate.firstShiftCompleted = true;
            }
            if (firstCompletedSale)
            {
                candidate.companyReputation = Clamp(
                    candidate.companyReputation + 2,
                    0,
                    100);
            }

            location.productInventory = reconciledInventory;
            location.inventoryUnits = reconciledInventoryUnits;
            int demand = Math.Max(
                totals.unitsSold,
                acceptedMetrics.requestedProductUnits);
            int lostDemand = demand - totals.unitsSold;
            location.serviceQuality = acceptedMetrics.customerVisits <= 0
                ? 100
                : Clamp(
                    acceptedMetrics.customersServed * 100 /
                    Math.Max(1, acceptedMetrics.customerVisits),
                    0,
                    100);
            location.productAvailabilityBasisPoints =
                acceptedMetrics.requestedProductUnits <= 0
                    ? PortfolioOperationsRules.BasisPoints
                    : Clamp(
                        (acceptedMetrics.requestedProductUnits -
                         acceptedMetrics.unavailableProductUnits) *
                        PortfolioOperationsRules.BasisPoints /
                        Math.Max(1, acceptedMetrics.requestedProductUnits),
                        0,
                        PortfolioOperationsRules.BasisPoints);
            location.productMixBasisPoints =
                PortfolioOperationsRules.CalculateProductMixBasisPoints(
                    location.shelfMerchandiseAssignments,
                    reconciledInventory,
                    locationDefinition.SimulationProfile
                        .PreferredProductMixCount);
            int dailySatisfaction =
                PortfolioOperationsRules.CalculateDailySatisfaction(
                    location.serviceQuality,
                    location.productAvailabilityBasisPoints,
                    location.productMixBasisPoints,
                    acceptedMetrics.standardsTaskComplete);
            location.customerSatisfaction = Clamp(
                (prior.startingCustomerSatisfaction * 7 +
                 dailySatisfaction * 3) / 10,
                0,
                100);
            BusinessOperatingCostProfile operatingCosts =
                locationDefinition.SimulationProfile.OperatingCosts;
            int wear = acceptedMetrics.standardsTaskComplete
                ? operatingCosts.DailyWearUnits / 2
                : operatingCosts.DailyWearUnits;
            location.maintenanceCondition = Clamp(
                prior.startingMaintenanceCondition - wear,
                0,
                100);
            location.failurePressure = 100 - location.maintenanceCondition;
            location.reputation = Clamp(
                (location.reputation * 3 + location.customerSatisfaction) / 4,
                0,
                100);
            if (!location.hasLastReport ||
                location.lastReport.day != candidate.currentDay)
            {
                location.daysOperating++;
            }

            RefreshOperatingAlerts(
                location,
                candidate.currentDay,
                reorderBlockedByAuthority: false,
                reorderBlockedByBudget: false);
            int openAlerts = location.operatingAlerts.Count(alert => alert.IsOpen);
            int ownerAlerts = location.operatingAlerts.Count(alert =>
                alert.IsOpen && alert.requiresOwnerAttention);
            location.lastReport = new PortfolioLocationReportSnapshot
            {
                day = candidate.currentDay,
                locationId = location.locationId,
                demandUnits = demand,
                unitsSold = totals.unitsSold,
                lostDemandUnits = lostDemand,
                endingInventoryUnits = reconciledInventoryUnits,
                reorderedUnits = 0,
                unitPriceCents = hasExactMerchandiseSales &&
                                 merchandiseSales.Count == 1
                    ? merchandiseSales[0].unitPriceCents
                    : 0,
                grossSalesCents = totals.grossSalesCents,
                costOfGoodsSoldCents = totals.costOfGoodsSoldCents,
                payrollCents = detailedPayrollCents,
                rentCents = detailedRentCents,
                inventoryPurchaseCents = checked(
                    acquiredInventoryCost + sessionPurchaseCents),
                deliveryFeesCents = sessionDeliveryFeesCents,
                baseOperatingCostsCents = detailedOperatingCostCents,
                maintenanceCostsCents = 0,
                operatingProfitCents = checked(
                    totals.grossSalesCents -
                    totals.costOfGoodsSoldCents -
                    totals.includedOperatingExpensesCents -
                    sessionDeliveryFeesCents),
                cashChangeCents = checked(
                    totals.grossSalesCents -
                    totals.includedOperatingExpensesCents -
                    acquiredInventoryCost -
                    sessionPurchaseCents -
                    sessionDeliveryFeesCents),
                serviceQuality = location.serviceQuality,
                customerSatisfaction = location.customerSatisfaction,
                productAvailabilityBasisPoints =
                    location.productAvailabilityBasisPoints,
                productMixBasisPoints = location.productMixBasisPoints,
                maintenanceCondition = location.maintenanceCondition,
                failurePressure = location.failurePressure,
                openAlertCount = openAlerts,
                ownerAttentionAlertCount = ownerAlerts,
                primaryCause = BuildDetailedPrimaryCause(
                    totals,
                    acceptedMetrics,
                    location),
                isDetailedOperation = true,
                hasExactMerchandiseSales = hasExactMerchandiseSales,
                merchandiseSales = merchandiseSales?
                    .Select(CloneMerchandiseSaleLine)
                    .ToList() ?? new List<MerchandiseSaleLineSnapshot>()
            };
            location.hasLastReport = true;
            return TryCommit(candidate, out error);
        }

        private static bool TryApplyDetailedInventoryDelta(
            IReadOnlyList<PortfolioProductInventorySnapshot> portfolioInventory,
            IReadOnlyList<PortfolioProductInventorySnapshot> detailedBaseline,
            IReadOnlyList<PortfolioProductInventorySnapshot> detailedInventory,
            int inventoryCapacityUnits,
            out List<PortfolioProductInventorySnapshot> result,
            out string error)
        {
            result = null;
            if (portfolioInventory == null || detailedBaseline == null ||
                detailedInventory == null || inventoryCapacityUnits < 0)
            {
                error = "Detailed inventory baseline is missing.";
                return false;
            }

            Dictionary<string, PortfolioProductInventorySnapshot> baselineById =
                detailedBaseline.ToDictionary(
                    value => value.productId,
                    StringComparer.Ordinal);
            Dictionary<string, PortfolioProductInventorySnapshot> detailedById =
                detailedInventory.ToDictionary(
                    value => value.productId,
                    StringComparer.Ordinal);
            result = portfolioInventory
                .Select(PortfolioOperationsRules.Clone)
                .OrderBy(value => value.productId, StringComparer.Ordinal)
                .ToList();
            if (baselineById.Count != result.Count ||
                detailedById.Count != result.Count)
            {
                error =
                    "Detailed inventory baseline no longer matches the location merchandise catalog.";
                result = null;
                return false;
            }

            long totalUnits = 0;
            foreach (PortfolioProductInventorySnapshot product in result)
            {
                if (!baselineById.TryGetValue(
                        product.productId,
                        out PortfolioProductInventorySnapshot baseline) ||
                    !detailedById.TryGetValue(
                        product.productId,
                        out PortfolioProductInventorySnapshot detailed))
                {
                    error =
                        "Detailed inventory baseline no longer matches the location merchandise catalog.";
                    result = null;
                    return false;
                }

                long quantity = (long)product.quantityUnits +
                                detailed.quantityUnits -
                                baseline.quantityUnits;
                if (quantity < 0 || quantity > int.MaxValue)
                {
                    error =
                        $"Detailed inventory change for '{product.productId}' exceeds the location's aggregate stock.";
                    result = null;
                    return false;
                }

                product.quantityUnits = (int)quantity;
                if (product.quantityUnits > 0 && product.unitCostCents == 0)
                {
                    product.unitCostCents = detailed.unitCostCents;
                }
                totalUnits += quantity;
            }

            if (totalUnits > inventoryCapacityUnits)
            {
                error =
                    "Detailed inventory changes exceed the location inventory capacity.";
                result = null;
                return false;
            }

            error = null;
            return true;
        }

        private static long CalculateProductInventoryValue(
            IReadOnlyList<PortfolioProductInventorySnapshot> inventory)
        {
            long value = 0;
            foreach (PortfolioProductInventorySnapshot product in
                     inventory ?? Array.Empty<PortfolioProductInventorySnapshot>())
            {
                value = checked(value + product.InventoryValueCents);
            }
            return value;
        }

        private static bool ProductInventoryEqual(
            IReadOnlyList<PortfolioProductInventorySnapshot> left,
            IReadOnlyList<PortfolioProductInventorySnapshot> right)
        {
            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }

            Dictionary<string, PortfolioProductInventorySnapshot> rightById =
                right.ToDictionary(value => value.productId, StringComparer.Ordinal);
            return left.All(value =>
                rightById.TryGetValue(
                    value.productId,
                    out PortfolioProductInventorySnapshot other) &&
                value.quantityUnits == other.quantityUnits &&
                value.unitCostCents == other.unitCostCents);
        }

        private static bool DetailedMetricsEqual(
            DetailedOperationMetricsSnapshot left,
            DetailedOperationMetricsSnapshot right)
        {
            return left != null && right != null &&
                   left.customerVisits == right.customerVisits &&
                   left.customersServed == right.customersServed &&
                   left.customersAbandoned == right.customersAbandoned &&
                   left.requestedProductUnits == right.requestedProductUnits &&
                   left.unavailableProductUnits == right.unavailableProductUnits &&
                   left.standardsTaskComplete == right.standardsTaskComplete;
        }

        private static string BuildDetailedPrimaryCause(
            StoreSessionTotals totals,
            DetailedOperationMetricsSnapshot metrics,
            PortfolioLocationSnapshot location)
        {
            if (metrics.unavailableProductUnits > 0)
            {
                return "Customers encountered unavailable products; replenish the affected product mix.";
            }
            if (metrics.customersAbandoned > 0)
            {
                return "Customers abandoned service; review staffing schedules and checkout standards.";
            }
            if (!metrics.standardsTaskComplete)
            {
                return "Operating standards were missed; complete standards work to reduce maintenance pressure.";
            }
            return totals.transactionCount == 0
                ? "The detailed location is open with exact occupancy, inventory, and operating costs reconciled."
                : "Hands-on demand, sales, COGS, cash, product inventory, and service results reconcile exactly.";
        }

        private static void RefreshOperatingAlerts(
            PortfolioLocationSnapshot location,
            int day,
            bool reorderBlockedByAuthority,
            bool reorderBlockedByBudget)
        {
            PortfolioDelegationPolicySnapshot policy =
                location.delegationPolicy;
            SetOperatingAlert(
                location,
                day,
                "service-standard",
                location.serviceQuality < policy.minimumServiceQuality,
                location.serviceQuality < policy.minimumServiceQuality - 20
                    ? PortfolioAlertSeverity.Critical
                    : PortfolioAlertSeverity.Warning,
                "Service quality is below the owner's operating standard.",
                "Adjust schedules, task focus, training, or on-site checkout support.",
                location.serviceQuality < policy.minimumServiceQuality - 20);
            SetOperatingAlert(
                location,
                day,
                "product-availability",
                location.productAvailabilityBasisPoints <
                policy.minimumProductAvailabilityBasisPoints,
                location.productAvailabilityBasisPoints < 5_000
                    ? PortfolioAlertSeverity.Critical
                    : PortfolioAlertSeverity.Warning,
                "Product availability is below the configured standard.",
                "Increase the reorder target, purchasing budget, or inventory staffing focus.",
                location.productAvailabilityBasisPoints < 5_000);
            SetOperatingAlert(
                location,
                day,
                "maintenance-pressure",
                location.maintenanceCondition <
                policy.minimumMaintenanceCondition,
                location.maintenanceCondition < 30
                    ? PortfolioAlertSeverity.Critical
                    : PortfolioAlertSeverity.Warning,
                "Maintenance condition is below the configured standard.",
                "Authorize maintenance, raise the spending limit, or complete standards work.",
                location.maintenanceCondition < 30);
            SetOperatingAlert(
                location,
                day,
                "purchasing-authority",
                reorderBlockedByAuthority,
                PortfolioAlertSeverity.Warning,
                "Inventory reached its reorder point, but the manager lacks purchasing authority.",
                "Grant purchasing authority or place the order as owner.",
                true);
            SetOperatingAlert(
                location,
                day,
                "spending-limit",
                reorderBlockedByBudget,
                PortfolioAlertSeverity.Warning,
                "The delegated spending limit blocked the configured reorder or maintenance response.",
                "Raise the daily spending limit or intervene as owner.",
                true);
        }

        private static void SetOperatingAlert(
            PortfolioLocationSnapshot location,
            int day,
            string problemTypeId,
            bool active,
            PortfolioAlertSeverity severity,
            string summary,
            string recoveryAction,
            bool ownerAttention)
        {
            PortfolioOperatingAlertSnapshot open = location.operatingAlerts
                .FirstOrDefault(alert =>
                    alert.IsOpen && string.Equals(
                        alert.problemTypeId,
                        problemTypeId,
                        StringComparison.Ordinal));
            if (!active)
            {
                if (open != null)
                {
                    open.resolvedDay = day;
                }
                return;
            }

            if (open != null)
            {
                open.severity = severity;
                open.requiresOwnerAttention = ownerAttention;
                open.summary = summary;
                open.recoveryAction = recoveryAction;
                return;
            }

            int occurrence = location.operatingAlerts.Count(alert =>
                string.Equals(
                    alert.problemTypeId,
                    problemTypeId,
                    StringComparison.Ordinal)) + 1;
            location.operatingAlerts.Add(new PortfolioOperatingAlertSnapshot
            {
                alertId =
                    $"alert-{location.locationId}-{problemTypeId}-{occurrence:D3}",
                problemTypeId = problemTypeId,
                severity = severity,
                openedDay = day,
                resolvedDay = 0,
                acknowledged = false,
                requiresOwnerAttention = ownerAttention,
                summary = summary,
                recoveryAction = recoveryAction
            });
        }

        public bool TryPlacePurchaseOrder(
            string locationId,
            string supplierId,
            IReadOnlyList<ProcurementOrderRequestLine> requestedLines,
            out PurchaseOrderSnapshot order,
            out string error)
        {
            order = null;
            if (!state.firstShiftCompleted)
            {
                error = "Complete the hands-on first shift before placing purchase orders.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            if (!TryPlaceOrderOnCandidate(
                    candidate,
                    locationId,
                    supplierId,
                    requestedLines,
                    true,
                    out order,
                    out error) ||
                !TryCommit(candidate, out error))
            {
                order = null;
                return false;
            }

            return true;
        }

        public bool TryCancelPurchaseOrder(
            string orderId,
            out bool unchanged,
            out string error)
        {
            unchanged = false;
            if (!ProcurementLedger.TryRestore(
                    ConvenienceStoreProcurement.Catalog,
                    state.procurement,
                    out ProcurementLedger currentLedger,
                    out error) ||
                !currentLedger.TryGetOrder(
                    orderId,
                    out PurchaseOrderSnapshot currentOrder))
            {
                error ??= "Purchase order is unavailable.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            if (!ProcurementLedger.TryRestore(
                    ConvenienceStoreProcurement.Catalog,
                    candidate.procurement,
                    out ProcurementLedger ledger,
                    out error) ||
                !ledger.TryCancelOrder(
                    orderId,
                    out long refundCents,
                    out unchanged,
                    out error))
            {
                return false;
            }

            if (unchanged)
            {
                error = null;
                return true;
            }

            PortfolioLocationSnapshot location = candidate.locations.First(value =>
                string.Equals(
                    value.locationId,
                    currentOrder.locationId,
                    StringComparison.Ordinal));
            try
            {
                candidate.cashCents = checked(candidate.cashCents + refundCents);
                location.lifetimeInventoryPurchaseCents = checked(
                    location.lifetimeInventoryPurchaseCents -
                    currentOrder.subtotalCostCents);
                location.lifetimeDeliveryFeesCents = checked(
                    location.lifetimeDeliveryFeesCents -
                    currentOrder.deliveryFeeCents);
                location.lifetimeOperatingProfitCents = checked(
                    location.lifetimeOperatingProfitCents +
                    currentOrder.deliveryFeeCents);
                location.lifetimeCashChangeCents = checked(
                    location.lifetimeCashChangeCents + refundCents);
            }
            catch (OverflowException)
            {
                error = "Purchase order cancellation overflowed company totals.";
                return false;
            }

            if (location.lifetimeInventoryPurchaseCents < 0 ||
                location.lifetimeDeliveryFeesCents < 0)
            {
                error = "Purchase order cancellation contradicts recorded location costs.";
                return false;
            }

            candidate.procurement = ledger.CreateSnapshot();
            return TryCommit(candidate, out error);
        }

        public bool TryAdvanceProcurementTicks(
            int elapsedTicks,
            out int fulfilledOrderCount,
            out string error)
        {
            fulfilledOrderCount = 0;
            if (!state.firstShiftCompleted)
            {
                error = "Procurement time begins after the hands-on first shift.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            if (!ProcurementLedger.TryRestore(
                    ConvenienceStoreProcurement.Catalog,
                    candidate.procurement,
                    out ProcurementLedger ledger,
                    out error) ||
                !ledger.TryAdvanceTicks(
                    elapsedTicks,
                    out fulfilledOrderCount,
                    out error))
            {
                return false;
            }

            candidate.procurement = ledger.CreateSnapshot();
            return TryCommit(candidate, out error);
        }

        public bool TryCreatePurchaseOrderDelivery(
            string orderId,
            out PurchaseOrderSnapshot order,
            out bool unchanged,
            out string error)
        {
            order = null;
            unchanged = false;
            PortfolioProgressionSnapshot candidate = Clone(state);
            if (!ProcurementLedger.TryRestore(
                    ConvenienceStoreProcurement.Catalog,
                    candidate.procurement,
                    out ProcurementLedger ledger,
                    out error) ||
                !ledger.TryCreateDelivery(
                    orderId,
                    out order,
                    out unchanged,
                    out error))
            {
                return false;
            }

            if (unchanged)
            {
                return true;
            }

            candidate.procurement = ledger.CreateSnapshot();
            if (!TryCommit(candidate, out error))
            {
                order = null;
                return false;
            }

            return true;
        }

        public bool TryRecordPurchaseOrderReceipt(
            string orderId,
            IReadOnlyDictionary<string, int> receivedQuantities,
            out PurchaseOrderSnapshot order,
            out bool unchanged,
            out string error)
        {
            order = null;
            unchanged = false;
            PortfolioProgressionSnapshot candidate = Clone(state);
            if (!ProcurementLedger.TryRestore(
                    ConvenienceStoreProcurement.Catalog,
                    candidate.procurement,
                    out ProcurementLedger ledger,
                    out error) ||
                !ledger.TryRecordAbsoluteReceipt(
                    orderId,
                    receivedQuantities,
                    out order,
                    out unchanged,
                    out error))
            {
                return false;
            }

            if (unchanged)
            {
                return true;
            }

            candidate.procurement = ledger.CreateSnapshot();
            if (!TryCommit(candidate, out error))
            {
                order = null;
                return false;
            }

            return true;
        }

        public bool TryHireCandidate(
            string employeeId,
            string locationId,
            out string error)
        {
            if (!state.firstShiftCompleted)
            {
                error = "Complete the hands-on first shift before hiring.";
                return false;
            }

            if (!PortfolioProgressionRules.TryGetCandidate(
                    employeeId,
                    out PortfolioCandidateDefinition definition))
            {
                error = "That employee candidate is unavailable.";
                return false;
            }

            if (state.employees.Any(employee => string.Equals(
                    employee.employeeId,
                    employeeId,
                    StringComparison.Ordinal)))
            {
                error = $"{definition.DisplayName} is already employed.";
                return false;
            }

            if (!TryGetLocation(state, locationId, out _))
            {
                error = "The selected location is not part of the company.";
                return false;
            }

            if (HasRole(state, locationId, definition.Role))
            {
                error = $"The selected location already has a {FriendlyRole(definition.Role)}.";
                return false;
            }

            if (state.cashCents - definition.HiringCostCents <
                PortfolioProgressionRules.MinimumCashReserveCents)
            {
                error = "Hiring would breach the protected operating reserve.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            candidate.cashCents -= definition.HiringCostCents;
            candidate.lifetimeCorporateCostsCents = checked(
                candidate.lifetimeCorporateCostsCents + definition.HiringCostCents);
            candidate.employees.Add(new PortfolioEmployeeSnapshot
            {
                employeeId = definition.EmployeeId,
                displayName = definition.DisplayName,
                trait = definition.Trait,
                role = definition.Role,
                taskFocus = definition.TaskFocus,
                skill = definition.Skill,
                reliability = definition.Reliability,
                satisfaction = 72,
                dailyWageCents = definition.DailyWageCents,
                hiringCostCents = definition.HiringCostCents,
                assignedLocationId = locationId,
                lastTrainingDay = 0,
                schedule = PortfolioOperationsRules.CreateDefaultSchedule()
            });
            SortCollections(candidate);
            return TryCommit(candidate, out error);
        }

        public bool TryTrainEmployee(string employeeId, out string error)
        {
            if (!TryGetEmployee(state, employeeId, out PortfolioEmployeeSnapshot employee))
            {
                error = "That employee is not part of the company.";
                return false;
            }

            if (employee.lastTrainingDay == state.currentDay)
            {
                error = "That employee has already trained today.";
                return false;
            }

            if (employee.skill >= 100)
            {
                error = "That employee has reached the current skill cap.";
                return false;
            }

            if (state.cashCents - PortfolioProgressionRules.TrainingCostCents <
                PortfolioProgressionRules.MinimumCashReserveCents)
            {
                error = "Training would breach the protected operating reserve.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            PortfolioEmployeeSnapshot candidateEmployee = candidate.employees.First(value =>
                string.Equals(value.employeeId, employeeId, StringComparison.Ordinal));
            candidate.cashCents -= PortfolioProgressionRules.TrainingCostCents;
            candidate.lifetimeCorporateCostsCents = checked(
                candidate.lifetimeCorporateCostsCents +
                PortfolioProgressionRules.TrainingCostCents);
            candidateEmployee.skill = Clamp(candidateEmployee.skill + 6, 0, 100);
            candidateEmployee.satisfaction = Clamp(
                candidateEmployee.satisfaction + 4,
                0,
                100);
            candidateEmployee.lastTrainingDay = candidate.currentDay;
            return TryCommit(candidate, out error);
        }

        public bool TryPromoteToManager(string employeeId, out string error)
        {
            if (!TryGetEmployee(state, employeeId, out PortfolioEmployeeSnapshot employee))
            {
                error = "That employee is not part of the company.";
                return false;
            }

            if (employee.role == PortfolioEmployeeRole.Manager)
            {
                error = "That employee is already a manager.";
                return false;
            }

            if (employee.skill < 65)
            {
                error = "Promotion requires skill 65; use training or operating experience first.";
                return false;
            }

            if (HasRole(
                    state,
                    employee.assignedLocationId,
                    PortfolioEmployeeRole.Manager))
            {
                error = "That location already has a manager.";
                return false;
            }

            if (state.cashCents - PortfolioProgressionRules.PromotionCostCents <
                PortfolioProgressionRules.MinimumCashReserveCents)
            {
                error = "Promotion would breach the protected operating reserve.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            PortfolioEmployeeSnapshot promoted = candidate.employees.First(value =>
                string.Equals(value.employeeId, employeeId, StringComparison.Ordinal));
            candidate.cashCents -= PortfolioProgressionRules.PromotionCostCents;
            candidate.lifetimeCorporateCostsCents = checked(
                candidate.lifetimeCorporateCostsCents +
                PortfolioProgressionRules.PromotionCostCents);
            promoted.role = PortfolioEmployeeRole.Manager;
            promoted.taskFocus = PortfolioTaskFocus.Balanced;
            promoted.dailyWageCents = Math.Max(promoted.dailyWageCents, 17_500);
            promoted.satisfaction = Clamp(promoted.satisfaction + 8, 0, 100);
            return TryCommit(candidate, out error);
        }

        public bool TryReassignEmployee(
            string employeeId,
            string locationId,
            out string error)
        {
            if (!TryGetEmployee(state, employeeId, out PortfolioEmployeeSnapshot employee) ||
                !TryGetLocation(state, locationId, out _))
            {
                error = "The employee or destination location is unavailable.";
                return false;
            }

            if (string.Equals(
                    employee.assignedLocationId,
                    locationId,
                    StringComparison.Ordinal))
            {
                error = "That employee is already assigned there.";
                return false;
            }

            if (HasRole(state, locationId, employee.role))
            {
                error = $"The destination already has a {FriendlyRole(employee.role)}.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            candidate.employees.First(value => string.Equals(
                    value.employeeId,
                    employeeId,
                    StringComparison.Ordinal))
                .assignedLocationId = locationId;
            return TryCommit(candidate, out error);
        }

        public bool TrySetTaskFocus(
            string employeeId,
            PortfolioTaskFocus focus,
            out string error)
        {
            if (!Enum.IsDefined(typeof(PortfolioTaskFocus), focus) ||
                !TryGetEmployee(state, employeeId, out _))
            {
                error = "The employee task-focus request is invalid.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            candidate.employees.First(value => string.Equals(
                    value.employeeId,
                    employeeId,
                    StringComparison.Ordinal))
                .taskFocus = focus;
            return TryCommit(candidate, out error);
        }

        public bool TrySetPricingPolicy(
            string locationId,
            PortfolioPricingPolicy policy,
            out string error)
        {
            if (!state.firstShiftCompleted)
            {
                error = "Complete the hands-on first shift before setting company policy.";
                return false;
            }

            if (!Enum.IsDefined(typeof(PortfolioPricingPolicy), policy) ||
                !TryGetLocation(state, locationId, out _))
            {
                error = "The location or pricing policy is invalid.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            PortfolioLocationSnapshot location = candidate.locations.First(value => string.Equals(
                    value.locationId,
                    locationId,
                    StringComparison.Ordinal));
            location.pricingPolicy = policy;
            foreach (MerchandisePriceSnapshot price in location.merchandisePrices)
            {
                price.salePriceCents = MerchandisingRules.CalculatePresetSalePrice(
                    price.referencePriceCents,
                    policy);
            }
            return TryCommit(candidate, out error);
        }

        public bool TryUpdateShelfOffer(
            string locationId,
            string shelfFixtureId,
            string assignedProductId,
            int salePriceCents,
            string customDisplayLabel,
            out string error)
        {
            if (!TryGetLocation(state, locationId, out _) ||
                !FirstStoreIdentifier.IsValid(shelfFixtureId))
            {
                error = "The location or shelf merchandise request is invalid.";
                return false;
            }

            assignedProductId = string.IsNullOrWhiteSpace(assignedProductId)
                ? null
                : assignedProductId.Trim();
            customDisplayLabel = string.IsNullOrWhiteSpace(customDisplayLabel)
                ? null
                : customDisplayLabel.Trim();
            if (customDisplayLabel?.Length >
                    MerchandisingRules.MaximumCustomDisplayLabelLength ||
                (customDisplayLabel?.Any(char.IsControl) ?? false))
            {
                error =
                    $"Shelf label text must be {MerchandisingRules.MaximumCustomDisplayLabelLength} characters or fewer on one line.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            PortfolioLocationSnapshot location = candidate.locations.First(value =>
                string.Equals(
                    value.locationId,
                    locationId,
                    StringComparison.Ordinal));
            ShelfMerchandiseAssignmentSnapshot assignment =
                location.shelfMerchandiseAssignments.FirstOrDefault(value =>
                    string.Equals(
                        value.shelfFixtureId,
                        shelfFixtureId,
                        StringComparison.Ordinal));
            if (assignment == null)
            {
                error = $"Shelf '{shelfFixtureId}' is not a sellable merchandise area.";
                return false;
            }

            MerchandisePriceSnapshot price = null;
            if (assignedProductId != null)
            {
                price = location.merchandisePrices.FirstOrDefault(value =>
                    string.Equals(
                        value.productId,
                        assignedProductId,
                        StringComparison.Ordinal));
                bool assignedElsewhere = location.shelfMerchandiseAssignments.Any(value =>
                    !ReferenceEquals(value, assignment) &&
                    string.Equals(
                        value.assignedProductId,
                        assignedProductId,
                        StringComparison.Ordinal));
                if (price == null || assignedElsewhere ||
                    salePriceCents <= 0 ||
                    salePriceCents > MerchandisingRules.MaximumSalePriceCents)
                {
                    error = assignedElsewhere
                        ? "That product is already assigned to another shelf. Unassign it there first."
                        : "Select a valid product and positive sale price.";
                    return false;
                }
            }

            assignment.assignedProductId = assignedProductId;
            assignment.customDisplayLabel = customDisplayLabel;
            if (price != null)
            {
                price.salePriceCents = salePriceCents;
            }
            return TryCommit(candidate, out error);
        }

        public bool TrySetSalePrice(
            string locationId,
            string productId,
            int salePriceCents,
            out string error)
        {
            if (!TryGetLocation(state, locationId, out _) ||
                !FirstStoreIdentifier.IsValid(productId) ||
                salePriceCents <= 0 ||
                salePriceCents > MerchandisingRules.MaximumSalePriceCents)
            {
                error = "The location, product, or sale price is invalid.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            PortfolioLocationSnapshot location = candidate.locations.First(value =>
                string.Equals(value.locationId, locationId, StringComparison.Ordinal));
            MerchandisePriceSnapshot price = location.merchandisePrices.FirstOrDefault(value =>
                string.Equals(value.productId, productId, StringComparison.Ordinal));
            if (price == null)
            {
                error = $"Product '{productId}' is not in this business's merchandise catalog.";
                return false;
            }
            price.salePriceCents = salePriceCents;
            return TryCommit(candidate, out error);
        }

        public bool TrySetReorderPolicy(
            string locationId,
            PortfolioReorderPolicy policy,
            out string error)
        {
            if (!state.firstShiftCompleted)
            {
                error = "Complete the hands-on first shift before setting company policy.";
                return false;
            }

            if (!Enum.IsDefined(typeof(PortfolioReorderPolicy), policy) ||
                !TryGetLocation(state, locationId, out _))
            {
                error = "The location or reorder policy is invalid.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            candidate.locations.First(location => string.Equals(
                    location.locationId,
                    locationId,
                    StringComparison.Ordinal))
                .reorderPolicy = policy;
            return TryCommit(candidate, out error);
        }

        public bool TrySetEmployeeSchedule(
            string employeeId,
            PortfolioEmployeeScheduleSnapshot schedule,
            out string error)
        {
            if (!PortfolioOperationsRules.TryValidateSchedule(
                    schedule,
                    out error) ||
                !TryGetEmployee(state, employeeId, out _))
            {
                error ??= "The selected employee is not part of the company.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            TryGetEmployee(
                candidate,
                employeeId,
                out PortfolioEmployeeSnapshot employee);
            employee.schedule = PortfolioOperationsRules.Clone(schedule);
            return TryCommit(candidate, out error);
        }

        public bool TrySetDelegationPolicy(
            string locationId,
            PortfolioDelegationPolicySnapshot policy,
            out string error)
        {
            if (!PortfolioOperationsRules.TryValidateDelegationPolicy(
                    policy,
                    out error) ||
                !TryGetLocation(state, locationId, out _))
            {
                error ??= "The selected location is not part of the company.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            TryGetLocation(
                candidate,
                locationId,
                out PortfolioLocationSnapshot location);
            location.delegationPolicy =
                PortfolioOperationsRules.Clone(policy);
            return TryCommit(candidate, out error);
        }

        public bool TryAcknowledgeOperatingAlert(
            string locationId,
            string alertId,
            out string error)
        {
            if (!TryGetLocation(
                    state,
                    locationId,
                    out PortfolioLocationSnapshot current) ||
                current.operatingAlerts.All(alert => !string.Equals(
                    alert.alertId,
                    alertId,
                    StringComparison.Ordinal)))
            {
                error = "The selected operating alert is unavailable.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            PortfolioOperatingAlertSnapshot alert = candidate.locations
                .First(location => location.locationId == locationId)
                .operatingAlerts.First(value => value.alertId == alertId);
            if (alert.acknowledged)
            {
                error = null;
                return true;
            }
            alert.acknowledged = true;
            return TryCommit(candidate, out error);
        }

        public bool TryPerformEmergencyMaintenance(
            string locationId,
            out string error)
        {
            if (!TryGetLocation(
                    state,
                    locationId,
                    out PortfolioLocationSnapshot current) ||
                !PortfolioProgressionRules.TryGetLocationDefinition(
                    locationId,
                    out PortfolioLocationDefinition definition))
            {
                error = "The selected location has no maintenance profile.";
                return false;
            }

            if (current.maintenanceCondition >= 100)
            {
                error = "The location does not currently require maintenance.";
                return false;
            }

            BusinessOperatingCostProfile costs =
                definition.SimulationProfile.OperatingCosts;
            long required = checked(
                costs.EmergencyMaintenanceCostCents +
                PortfolioProgressionRules.MinimumCashReserveCents);
            if (state.cashCents < required)
            {
                error = "Emergency maintenance would breach the protected cash reserve.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            TryGetLocation(
                candidate,
                locationId,
                out PortfolioLocationSnapshot location);
            candidate.cashCents -= costs.EmergencyMaintenanceCostCents;
            location.maintenanceCondition = Clamp(
                location.maintenanceCondition + costs.EmergencyRecoveryUnits,
                0,
                100);
            location.failurePressure = 100 - location.maintenanceCondition;
            location.lifetimeMaintenanceCostsCents = checked(
                location.lifetimeMaintenanceCostsCents +
                costs.EmergencyMaintenanceCostCents);
            location.lifetimeOperatingProfitCents = checked(
                location.lifetimeOperatingProfitCents -
                costs.EmergencyMaintenanceCostCents);
            location.lifetimeCashChangeCents = checked(
                location.lifetimeCashChangeCents -
                costs.EmergencyMaintenanceCostCents);
            RefreshOperatingAlerts(
                location,
                candidate.currentDay,
                false,
                false);
            return TryCommit(candidate, out error);
        }

        public bool TryEnterDetailedLocation(
            string locationId,
            out string error)
        {
            if (!TryGetLocation(state, locationId, out _))
            {
                error = "The selected detailed location is not part of the portfolio.";
                return false;
            }
            if (!string.IsNullOrWhiteSpace(
                    state.company.activeDetailedLocationId) &&
                !string.Equals(
                    state.company.activeDetailedLocationId,
                    locationId,
                    StringComparison.Ordinal))
            {
                error = "Leave the current detailed location before entering another one.";
                return false;
            }

            if (string.Equals(
                    state.company.activeDetailedLocationId,
                    locationId,
                    StringComparison.Ordinal))
            {
                error = null;
                return true;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            candidate.company.activeDetailedLocationId = locationId;
            return TryCommit(candidate, out error);
        }

        public bool TryLeaveDetailedLocation(
            string locationId,
            out string error)
        {
            if (!string.Equals(
                    state.company.activeDetailedLocationId,
                    locationId,
                    StringComparison.Ordinal))
            {
                error = "The selected location is not the active detailed location.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            candidate.company.activeDetailedLocationId = null;
            return TryCommit(candidate, out error);
        }

        public bool TryBindGeneratedLayout(
            string locationId,
            string canonicalSignature,
            out string error)
        {
            if (string.IsNullOrWhiteSpace(canonicalSignature) ||
                canonicalSignature.Length != 8 ||
                canonicalSignature.Any(value => !Uri.IsHexDigit(value)) ||
                !TryGetCommercialUnit(
                    state.company,
                    locationId,
                    out PortfolioCommercialUnitSnapshot currentUnit))
            {
                error = "A portfolio location and valid generated-layout signature are required.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(
                    currentUnit.generatedLayout.canonicalSignature) &&
                !string.Equals(
                    currentUnit.generatedLayout.canonicalSignature,
                    canonicalSignature,
                    StringComparison.OrdinalIgnoreCase))
            {
                error = "Regenerated layout signature differs from the location's persistent authoritative layout.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            TryGetCommercialUnit(
                candidate.company,
                locationId,
                out PortfolioCommercialUnitSnapshot unit);
            unit.generatedLayout.canonicalSignature =
                canonicalSignature.ToLowerInvariant();
            return TryCommit(candidate, out error);
        }

        public bool TryRecordLocationModification(
            string locationId,
            PortfolioLocationModificationSnapshot modification,
            out string error)
        {
            if (modification == null ||
                !TryGetCommercialUnit(
                    state.company,
                    locationId,
                    out PortfolioCommercialUnitSnapshot currentUnit))
            {
                error = "A persistent location and modification are required.";
                return false;
            }
            if (currentUnit.generatedLayout.modifications.Any(value =>
                    string.Equals(
                        value.modificationId,
                        modification.modificationId,
                        StringComparison.Ordinal)))
            {
                error = "That persistent location modification already exists.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            TryGetCommercialUnit(
                candidate.company,
                locationId,
                out PortfolioCommercialUnitSnapshot unit);
            PortfolioLocationModificationSnapshot accepted = new()
            {
                modificationId = modification.modificationId,
                modificationTypeId = modification.modificationTypeId,
                targetStableId = modification.targetStableId,
                localPositionX = modification.localPositionX,
                localPositionY = modification.localPositionY,
                localPositionZ = modification.localPositionZ,
                localYawDegrees = modification.localYawDegrees,
                payload = modification.payload,
                appliedDay = candidate.currentDay
            };
            unit.generatedLayout.modifications.Add(accepted);
            unit.generatedLayout.layoutRevision++;
            SortCollections(candidate);
            return TryCommit(candidate, out error);
        }

        public bool TryInstallPropertyImprovement(
            string locationId,
            string improvementId,
            string improvementTypeId,
            long costCents,
            out string error)
        {
            if (!StableIdentifier.IsValid(improvementId) ||
                !StableIdentifier.IsValid(improvementTypeId) ||
                costCents < 0 ||
                !TryGetCommercialUnit(
                    state.company,
                    locationId,
                    out PortfolioCommercialUnitSnapshot currentUnit))
            {
                error = "A valid location, improvement identity, and nonnegative cost are required.";
                return false;
            }
            if (currentUnit.improvements.Any(value => string.Equals(
                    value.improvementId,
                    improvementId,
                    StringComparison.Ordinal)))
            {
                error = "That commercial-unit improvement already exists.";
                return false;
            }
            if (state.cashCents - costCents <
                PortfolioProgressionRules.MinimumCashReserveCents)
            {
                error = "The improvement would breach the protected cash reserve.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            candidate.cashCents -= costCents;
            TryGetCommercialUnit(
                candidate.company,
                locationId,
                out PortfolioCommercialUnitSnapshot unit);
            unit.improvements.Add(new PortfolioImprovementSnapshot
            {
                improvementId = improvementId,
                improvementTypeId = improvementTypeId,
                commercialUnitId = unit.commercialUnitId,
                installedDay = candidate.currentDay,
                costCents = costCents,
                condition = 100
            });
            SortCollections(candidate);
            return TryCommit(candidate, out error);
        }

        public bool TryAcquireProperty(string propertyId, out string error)
        {
            PortfolioCommercialPropertySnapshot current =
                state.company.properties.FirstOrDefault(property =>
                    string.Equals(
                        property.propertyId,
                        propertyId,
                        StringComparison.Ordinal));
            if (current == null)
            {
                error = "The selected property is not part of the portfolio.";
                return false;
            }
            if (current.tenure == PortfolioPropertyTenure.Owned)
            {
                error = "The company already owns that property.";
                return false;
            }
            long required = checked(
                current.acquisitionCostCents +
                PortfolioProgressionRules.MinimumCashReserveCents);
            if (state.cashCents < required)
            {
                error = "Property acquisition would breach the protected cash reserve.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            PortfolioCommercialPropertySnapshot property =
                candidate.company.properties.First(value => string.Equals(
                    value.propertyId,
                    propertyId,
                    StringComparison.Ordinal));
            candidate.cashCents -= property.acquisitionCostCents;
            property.tenure = PortfolioPropertyTenure.Owned;
            property.acquiredDay = candidate.currentDay;
            return TryCommit(candidate, out error);
        }

        public PortfolioConsolidatedReportSnapshot CreateConsolidatedReport()
        {
            return new PortfolioConsolidatedReportSnapshot
            {
                day = state.currentDay,
                companyId = state.company.companyId,
                brandCount = state.company.brands.Count,
                locationCount = state.locations.Count,
                leasedPropertyCount = state.company.properties.Count(property =>
                    property.tenure == PortfolioPropertyTenure.Leased),
                ownedPropertyCount = state.company.properties.Count(property =>
                    property.tenure == PortfolioPropertyTenure.Owned),
                openAlertCount = state.locations.Sum(location =>
                    location.operatingAlerts.Count(alert => alert.IsOpen)),
                ownerAttentionAlertCount = state.locations.Sum(location =>
                    location.operatingAlerts.Count(alert =>
                        alert.IsOpen && alert.requiresOwnerAttention)),
                cashCents = state.cashCents,
                lifetimeGrossSalesCents = state.locations.Sum(location =>
                    location.lifetimeGrossSalesCents),
                lifetimeOperatingCostsCents = state.locations.Sum(location =>
                    checked(
                        location.lifetimeCostOfGoodsSoldCents +
                        location.lifetimePayrollCents +
                        location.lifetimeRentCents +
                        location.lifetimeDeliveryFeesCents +
                        location.lifetimeBaseOperatingCostsCents +
                        location.lifetimeMaintenanceCostsCents)),
                lifetimeOperatingProfitCents = state.locations.Sum(location =>
                    location.lifetimeOperatingProfitCents),
                lifetimePropertyAcquisitionCents = state.company.properties
                    .Where(property =>
                        property.tenure == PortfolioPropertyTenure.Owned)
                    .Sum(property => property.acquisitionCostCents),
                lifetimeImprovementCents = state.company.properties.Sum(property =>
                    property.commercialUnits.Sum(unit =>
                        unit.improvements.Sum(improvement =>
                            improvement.costCents)))
            };
        }

        public bool TryLeaseLocation(string locationId, out string error)
        {
            if (!state.firstShiftCompleted)
            {
                error = "Complete the hands-on first shift before expanding.";
                return false;
            }

            if (!PortfolioProgressionRules.TryGetLocationDefinition(
                    locationId,
                    out PortfolioLocationDefinition definition) ||
                string.Equals(
                    locationId,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal))
            {
                error = "That expansion site is unavailable.";
                return false;
            }

            if (!TryGetLocation(
                    state,
                    PortfolioProgressionRules.FirstLocationId,
                    out PortfolioLocationSnapshot firstLocation) ||
                !IsFullyStaffed(state, firstLocation.locationId))
            {
                error = "Staff the first store with a cashier, stock clerk, and manager before expanding.";
                return false;
            }

            if (firstLocation.delegatedDaysOperating < 1)
            {
                error = "Prove one delegated operating day before signing a second lease.";
                return false;
            }

            long required;
            try
            {
                required = checked(
                    definition.LeaseCostCents +
                    definition.OpeningInventoryCostCents +
                    PortfolioProgressionRules.MinimumCashReserveCents);
            }
            catch (OverflowException)
            {
                error = "The expansion cost overflowed integer-cent storage.";
                return false;
            }

            if (state.cashCents < required)
            {
                error =
                    $"Expansion requires {FormatCents(required)} including the protected reserve.";
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            candidate.cashCents -=
                definition.LeaseCostCents + definition.OpeningInventoryCostCents;
            PortfolioLocationSnapshot newLocation = CreateLocation(definition);
            newLocation.lifetimeInventoryPurchaseCents =
                definition.OpeningInventoryCostCents;
            newLocation.lifetimeLeaseAndSetupCents = definition.LeaseCostCents;
            newLocation.lifetimeCashChangeCents = checked(
                -definition.LeaseCostCents - definition.OpeningInventoryCostCents);
            candidate.locations.Add(newLocation);
            if (!PortfolioPropertyRules.TryGetDefinitionForLocation(
                    locationId,
                    out PortfolioPropertyDefinition propertyDefinition))
            {
                error = "The expansion site has no persistent property definition.";
                return false;
            }
            candidate.company.properties.Add(
                PortfolioPropertyRules.CreateLeasedProperty(propertyDefinition));
            SortCollections(candidate);
            return TryCommit(candidate, out error);
        }

        public bool CanAdvanceDelegatedDay(out string blocker)
        {
            if (!state.firstShiftCompleted)
            {
                blocker = "Complete the hands-on first shift before delegating.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(
                    state.company.activeDetailedLocationId))
            {
                blocker =
                    "Leave the active detailed location before advancing delegated simulation.";
                return false;
            }

            int simulatedDay = state.currentDay + 1;

            foreach (PortfolioLocationSnapshot location in state.locations
                         .OrderBy(value => value.locationId, StringComparer.Ordinal))
            {
                if (!HasScheduledRole(
                        state,
                        location.locationId,
                        PortfolioEmployeeRole.Cashier,
                        simulatedDay))
                {
                    blocker = $"{location.displayName} needs a cashier scheduled for the next operating day.";
                    return false;
                }
                if (!HasScheduledRole(
                        state,
                        location.locationId,
                        PortfolioEmployeeRole.StockClerk,
                        simulatedDay))
                {
                    blocker = $"{location.displayName} needs a stock clerk scheduled for the next operating day.";
                    return false;
                }
                if (!HasScheduledRole(
                        state,
                        location.locationId,
                        PortfolioEmployeeRole.Manager,
                        simulatedDay))
                {
                    blocker = $"{location.displayName} needs a manager scheduled to operate while you are absent.";
                    return false;
                }
            }

            if (!TryCalculatePortfolioFixedCosts(
                    state,
                    simulatedDay,
                    out long fixedCosts,
                    out blocker))
            {
                return false;
            }
            if (state.cashCents <
                fixedCosts + PortfolioProgressionRules.MinimumCashReserveCents)
            {
                blocker =
                    $"Cash must cover {FormatCents(fixedCosts)} in scheduled payroll, rent, and base operating costs plus the protected reserve.";
                return false;
            }

            blocker = null;
            return true;
        }

        public bool TryAdvanceDelegatedDay(out string error)
        {
            if (!CanAdvanceDelegatedDay(out error))
            {
                return false;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            int simulatedDay = candidate.currentDay + 1;
            if (!ProcurementLedger.TryRestore(
                    ConvenienceStoreProcurement.Catalog,
                    candidate.procurement,
                    out ProcurementLedger procurement,
                    out error) ||
                !procurement.TryAdvanceTicks(
                    ConvenienceStoreProcurement.TicksPerDelegatedDay,
                    out _,
                    out error))
            {
                return false;
            }
            candidate.procurement = procurement.CreateSnapshot();
            if (!TryCalculatePortfolioFixedCosts(
                    candidate,
                    simulatedDay,
                    out long portfolioFixedCosts,
                    out error))
            {
                return false;
            }
            candidate.cashCents -= portfolioFixedCosts;
            foreach (PortfolioLocationSnapshot location in candidate.locations
                         .OrderBy(value => value.locationId, StringComparer.Ordinal))
            {
                if (!TryProcessAggregateDelivery(
                        location,
                        procurement,
                        out _,
                        out error) ||
                    !TrySimulateLocationDay(
                        candidate,
                        location,
                        simulatedDay,
                        procurement,
                        out error))
                {
                    return false;
                }
            }

            candidate.procurement = procurement.CreateSnapshot();
            candidate.currentDay = simulatedDay;
            candidate.companyReputation = Clamp(
                (int)Math.Round(candidate.locations.Average(location =>
                    location.reputation)),
                0,
                100);
            SortCollections(candidate);
            return TryCommit(candidate, out error);
        }

        private static bool TryProcessAggregateDelivery(
            PortfolioLocationSnapshot location,
            ProcurementLedger procurement,
            out int receivedOrderUnits,
            out string error)
        {
            receivedOrderUnits = 0;
            if (!procurement.TryGetActiveOrderForLocation(
                    location.locationId,
                    out PurchaseOrderSnapshot order) ||
                order.status == PurchaseOrderStatus.Pending)
            {
                error = null;
                return true;
            }

            if (order.status == PurchaseOrderStatus.Fulfilled)
            {
                int orderedUnits = order.OrderedQuantityUnits;
                if (orderedUnits >
                    location.inventoryCapacityUnits - location.inventoryUnits)
                {
                    // Keep the fulfilled order intact until receiving capacity
                    // becomes available; do not invent overflow inventory.
                    error = null;
                    return true;
                }

                if (!procurement.TryCreateDelivery(
                        order.orderId,
                        out order,
                        out _,
                        out error))
                {
                    return false;
                }

                AddDeliveredProductInventory(location, order);
                location.inventoryUnits = location.productInventory.Sum(product =>
                    product.quantityUnits);
                receivedOrderUnits = orderedUnits;
            }

            if (order.status == PurchaseOrderStatus.Delivered ||
                order.status == PurchaseOrderStatus.PartiallyReceived)
            {
                Dictionary<string, int> completedReceipt =
                    order.lines.ToDictionary(
                        line => line.resourceId,
                        line => line.orderedQuantityUnits,
                        StringComparer.Ordinal);
                if (!procurement.TryRecordAbsoluteReceipt(
                        order.orderId,
                        completedReceipt,
                        out _,
                        out _,
                        out error))
                {
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static void AddDeliveredProductInventory(
            PortfolioLocationSnapshot location,
            PurchaseOrderSnapshot order)
        {
            Dictionary<string, PortfolioProductInventorySnapshot> byProduct =
                location.productInventory.ToDictionary(
                    value => value.productId,
                    StringComparer.Ordinal);
            foreach (PurchaseOrderLineSnapshot line in order.lines
                         .OrderBy(value => value.resourceId, StringComparer.Ordinal))
            {
                if (string.Equals(
                        line.resourceId,
                        ConvenienceStoreProcurement.AggregateResourceId,
                        StringComparison.Ordinal))
                {
                    List<PortfolioProductInventorySnapshot> orderedProducts =
                        byProduct.Values
                            .OrderBy(value => value.quantityUnits)
                            .ThenBy(value => value.productId, StringComparer.Ordinal)
                            .ToList();
                    for (int unit = 0;
                         unit < line.orderedQuantityUnits;
                         unit++)
                    {
                        PortfolioProductInventorySnapshot target =
                            orderedProducts[unit % orderedProducts.Count];
                        target.quantityUnits = checked(target.quantityUnits + 1);
                    }
                    continue;
                }

                if (!byProduct.TryGetValue(
                        line.resourceId,
                        out PortfolioProductInventorySnapshot product))
                {
                    throw new InvalidOperationException(
                        $"Delivered resource '{line.resourceId}' is not a configured merchandise product.");
                }
                product.quantityUnits = checked(
                    product.quantityUnits + line.orderedQuantityUnits);
                product.unitCostCents = line.unitCostCents;
            }
        }

        private static bool TrySimulateLocationDay(
            PortfolioProgressionSnapshot candidate,
            PortfolioLocationSnapshot location,
            int simulatedDay,
            ProcurementLedger procurement,
            out string error)
        {
            if (!PortfolioProgressionRules.TryGetLocationDefinition(
                    location.locationId,
                    out PortfolioLocationDefinition locationDefinition) ||
                locationDefinition.SimulationProfile == null)
            {
                error =
                    $"Location '{location.locationId}' has no aggregate simulation profile.";
                return false;
            }

            BusinessSimulationProfile simulation =
                locationDefinition.SimulationProfile;
            List<PortfolioEmployeeSnapshot> assigned = candidate.employees
                .Where(employee =>
                    string.Equals(
                        employee.assignedLocationId,
                        location.locationId,
                        StringComparison.Ordinal) &&
                    PortfolioOperationsRules.IsScheduled(
                        employee.schedule,
                        simulatedDay))
                .OrderBy(employee => employee.employeeId, StringComparer.Ordinal)
                .ToList();
            PortfolioEmployeeSnapshot cashier = assigned.First(employee =>
                employee.role == PortfolioEmployeeRole.Cashier);
            PortfolioEmployeeSnapshot stocker = assigned.First(employee =>
                employee.role == PortfolioEmployeeRole.StockClerk);
            PortfolioEmployeeSnapshot manager = assigned.First(employee =>
                employee.role == PortfolioEmployeeRole.Manager);

            long payroll = assigned.Sum(employee => employee.dailyWageCents);
            long baseOperatingCosts =
                simulation.OperatingCosts.BaseOperatingCostCents;
            long effectiveRent = EffectiveDailyRent(candidate.company, location);
            long fixedCosts = checked(
                payroll + effectiveRent + baseOperatingCosts);

            ResolveReorderPolicy(
                location,
                out int reorderPoint,
                out int reorderTarget);
            int reorderedUnits = 0;
            long inventoryPurchase = 0;
            long deliveryFees = 0;
            long maintenanceCosts = 0;

            List<MerchandiseOffer> offers = location.shelfMerchandiseAssignments
                .Where(assignment =>
                    assignment != null &&
                    !string.IsNullOrWhiteSpace(assignment.assignedProductId))
                .Select(assignment =>
                    MerchandisingRules.TryGetOfferForProduct(
                        location,
                        assignment.assignedProductId,
                        out MerchandiseOffer offer)
                        ? (MerchandiseOffer?)offer
                        : null)
                .Where(offer => offer.HasValue)
                .Select(offer => offer.Value)
                .OrderBy(offer => offer.ProductId, StringComparer.Ordinal)
                .ToList();
            int managerDemandAdjustment = (manager.skill - 50) * 2;
            if (manager.taskFocus == PortfolioTaskFocus.Standards)
            {
                managerDemandAdjustment += 20;
            }
            int dayVariance = DeterministicVariance(
                simulatedDay,
                location.locationId,
                -24,
                24);
            int potentialDemand = Math.Max(
                0,
                location.baseDemandUnits +
                (location.reputation - 50) * 3 -
                location.competitionIndex +
                managerDemandAdjustment +
                dayVariance);

            EmployeeWorkProfile managerWork = manager.CreateWorkProfile();
            int serviceCapacity =
                simulation.CustomerServiceCapacity.CalculateCapacity(
                    cashier.CreateWorkProfile(),
                    managerWork);
            int stockedAvailability =
                simulation.ResourceFlowCapacity.CalculateCapacity(
                    stocker.CreateWorkProfile(),
                    managerWork);
            int standardsCapacity = simulation.StandardsCapacity.CalculateCapacity(
                manager.CreateWorkProfile(),
                managerWork);
            serviceCapacity = serviceCapacity *
                              (50 + location.maintenanceCondition) / 150;
            stockedAvailability = stockedAvailability *
                                  (60 + location.maintenanceCondition) / 160;

            Dictionary<string, int> willingDemandByProduct =
                new(StringComparer.Ordinal);
            int willingDemand = 0;
            for (int index = 0; index < offers.Count; index++)
            {
                int potential = offers.Count == 0
                    ? 0
                    : potentialDemand / offers.Count +
                      (index < potentialDemand % offers.Count ? 1 : 0);
                int willing = MerchandisingRules.ApplyDemandResponse(
                    potential,
                    offers[index].SalePriceCents,
                    offers[index].ReferencePriceCents);
                willingDemandByProduct.Add(offers[index].ProductId, willing);
                willingDemand = checked(willingDemand + willing);
            }

            Dictionary<string, int> inventoryAvailableByProduct =
                location.productInventory.ToDictionary(
                    value => value.productId,
                    value => value.quantityUnits,
                    StringComparer.Ordinal);
            int stockSupportedDemand = willingDemandByProduct.Sum(pair =>
                Math.Min(
                    pair.Value,
                    inventoryAvailableByProduct.TryGetValue(
                        pair.Key,
                        out int available)
                        ? available
                        : 0));
            int salesCapacity = Math.Min(serviceCapacity, stockedAvailability);
            List<MerchandiseSaleLineSnapshot> merchandiseSales =
                AllocateMerchandiseSales(
                    offers,
                    willingDemandByProduct,
                    inventoryAvailableByProduct,
                    salesCapacity);
            int unitsSold = merchandiseSales.Sum(line => line.quantityUnits);
            // Reports preserve baseline interest rejected by high prices while
            // also exposing the modest demand lift created by a low price.
            int demand = Math.Max(potentialDemand, willingDemand);
            int lostDemand = demand - unitsSold;
            long grossSales;
            long costOfGoodsSold;
            long operatingProfit;
            try
            {
                grossSales = merchandiseSales.Sum(line =>
                    line.GrossSalesCents);
                costOfGoodsSold = 0;
                foreach (MerchandiseSaleLineSnapshot sale in merchandiseSales)
                {
                    PortfolioProductInventorySnapshot product =
                        location.productInventory.First(value => string.Equals(
                            value.productId,
                            sale.productId,
                            StringComparison.Ordinal));
                    costOfGoodsSold = checked(
                        costOfGoodsSold +
                        product.unitCostCents * sale.quantityUnits);
                    product.quantityUnits -= sale.quantityUnits;
                }
                operatingProfit = checked(
                    grossSales - costOfGoodsSold - fixedCosts);
                candidate.cashCents = checked(candidate.cashCents + grossSales);
            }
            catch (OverflowException)
            {
                error = "Delegated sales overflowed integer-cent storage.";
                return false;
            }

            location.inventoryUnits = location.productInventory.Sum(product =>
                product.quantityUnits);

            bool standardsMet = standardsCapacity >=
                                simulation.StandardsCapacity.BaseCapacityUnits;
            int wear = simulation.OperatingCosts.DailyWearUnits +
                       (standardsMet ? 0 : 3);
            location.maintenanceCondition = Clamp(
                location.maintenanceCondition - wear,
                0,
                100);
            bool maintenanceNeeded =
                location.delegationPolicy.maintenancePolicy switch
                {
                    PortfolioMaintenancePolicy.Preventive =>
                        location.maintenanceCondition <
                        location.delegationPolicy.minimumMaintenanceCondition + 15,
                    PortfolioMaintenancePolicy.Routine =>
                        location.maintenanceCondition <
                        location.delegationPolicy.minimumMaintenanceCondition,
                    _ => location.maintenanceCondition < 25
                };
            bool spendingBlocked = false;
            long delegatedSpendRemaining =
                location.delegationPolicy.dailySpendingLimitCents;
            if (maintenanceNeeded &&
                location.delegationPolicy.managerCanAuthorizeMaintenance)
            {
                ResolveMaintenanceIntervention(
                    location,
                    simulation.OperatingCosts,
                    out long requestedMaintenanceCost,
                    out int maintenanceRecovery);
                long cashAvailable = Math.Max(
                    0,
                    candidate.cashCents -
                    PortfolioProgressionRules.MinimumCashReserveCents);
                if (requestedMaintenanceCost <= delegatedSpendRemaining &&
                    requestedMaintenanceCost <= cashAvailable)
                {
                    maintenanceCosts = requestedMaintenanceCost;
                    delegatedSpendRemaining -= maintenanceCosts;
                    candidate.cashCents -= maintenanceCosts;
                    location.maintenanceCondition = Clamp(
                        location.maintenanceCondition + maintenanceRecovery,
                        0,
                        100);
                    operatingProfit = checked(
                        operatingProfit - maintenanceCosts);
                    location.lifetimeMaintenanceCostsCents = checked(
                        location.lifetimeMaintenanceCostsCents +
                        maintenanceCosts);
                    location.lifetimeCashChangeCents = checked(
                        location.lifetimeCashChangeCents - maintenanceCosts);
                }
                else
                {
                    spendingBlocked = true;
                }
            }

            bool reorderBlockedByAuthority = false;
            if (location.inventoryUnits <= reorderPoint &&
                !procurement.TryGetActiveOrderForLocation(
                    location.locationId,
                    out _))
            {
                int desiredUnits = Math.Max(
                    0,
                    reorderTarget - location.inventoryUnits);
                long cashSpendable = Math.Max(
                    0,
                    candidate.cashCents -
                    PortfolioProgressionRules.MinimumCashReserveCents);
                long allowedSpend = Math.Min(
                    cashSpendable,
                    delegatedSpendRemaining);
                if (!location.delegationPolicy.managerCanPurchase)
                {
                    reorderBlockedByAuthority = desiredUnits > 0;
                }
                else if (TryBuildProductReorderLines(
                             location,
                             desiredUnits,
                             allowedSpend,
                             out List<ProcurementOrderRequestLine> lines,
                             out reorderedUnits))
                {
                    if (!TryPlaceOrderOnCandidate(
                            candidate,
                            location,
                            procurement,
                            ConvenienceStoreProcurement.SupplierId,
                            lines,
                            false,
                            true,
                            out PurchaseOrderSnapshot order,
                            out error))
                    {
                        return false;
                    }

                    inventoryPurchase = order.subtotalCostCents;
                    deliveryFees = order.deliveryFeeCents;
                    delegatedSpendRemaining -= checked(
                        inventoryPurchase + deliveryFees);
                }
                else
                {
                    spendingBlocked |= desiredUnits > 0;
                }
            }

            string primaryCause = "Demand served within current capacity.";
            if (lostDemand > 0)
            {
                if (offers.Count == 0)
                {
                    primaryCause =
                        "No shelf merchandise was assigned, so demand could not be served.";
                }
                else if (stockSupportedDemand < willingDemand ||
                         unitsSold >= stockedAvailability)
                {
                    primaryCause = "Shelf availability limited sales; raise the reorder buffer or inventory focus.";
                }
                else if (unitsSold >= serviceCapacity)
                {
                    primaryCause = "Checkout capacity limited sales; train staff or emphasize service.";
                }
                else if (willingDemand < potentialDemand)
                {
                    primaryCause =
                        "Current shelf prices reduced willing demand relative to reference pricing.";
                }
                else
                {
                    primaryCause = "Demand exceeded the current operating system.";
                }
            }
            else if (offers.Any(offer =>
                         offer.SalePriceCents < offer.ReferencePriceCents))
            {
                primaryCause =
                    "Lower shelf prices improved purchase acceptance and consumed inventory faster.";
            }
            if (reorderBlockedByAuthority)
            {
                primaryCause +=
                    " The manager could not reorder without purchasing authority.";
            }
            if (spendingBlocked)
            {
                primaryCause +=
                    " Available delegated budget or cash could not fund the configured response.";
            }

            int serviceRatioPercent = willingDemand <= 0
                ? 100
                : Math.Min(willingDemand, serviceCapacity) * 100 /
                  Math.Max(1, willingDemand);
            int availabilityBasisPoints = willingDemand <= 0
                ? PortfolioOperationsRules.BasisPoints
                : stockSupportedDemand * PortfolioOperationsRules.BasisPoints /
                  Math.Max(1, willingDemand);
            int productMixBasisPoints =
                PortfolioOperationsRules.CalculateProductMixBasisPoints(
                    location.shelfMerchandiseAssignments,
                    location.productInventory,
                    simulation.PreferredProductMixCount);
            int dailySatisfaction =
                PortfolioOperationsRules.CalculateDailySatisfaction(
                    Clamp(serviceRatioPercent, 0, 100),
                    Clamp(
                        availabilityBasisPoints,
                        0,
                        PortfolioOperationsRules.BasisPoints),
                    productMixBasisPoints,
                    standardsMet);
            location.serviceQuality = Clamp(serviceRatioPercent, 0, 100);
            location.productAvailabilityBasisPoints = Clamp(
                availabilityBasisPoints,
                0,
                PortfolioOperationsRules.BasisPoints);
            location.productMixBasisPoints = productMixBasisPoints;
            location.customerSatisfaction = Clamp(
                (location.customerSatisfaction * 7 +
                 dailySatisfaction * 3) / 10,
                0,
                100);
            location.failurePressure = 100 - location.maintenanceCondition;
            int reputationChange = location.customerSatisfaction >= 85
                ? 2
                : location.customerSatisfaction >= 65
                    ? 0
                    : -3;
            if (manager.taskFocus == PortfolioTaskFocus.Standards)
            {
                reputationChange++;
            }
            location.reputation = Clamp(
                location.reputation + reputationChange,
                0,
                100);
            location.daysOperating++;
            location.delegatedDaysOperating++;
            location.lifetimeGrossSalesCents = checked(
                location.lifetimeGrossSalesCents + grossSales);
            location.lifetimeCostOfGoodsSoldCents = checked(
                location.lifetimeCostOfGoodsSoldCents + costOfGoodsSold);
            location.lifetimePayrollCents = checked(
                location.lifetimePayrollCents + payroll);
            location.lifetimeRentCents = checked(
                location.lifetimeRentCents + effectiveRent);
            location.lifetimeBaseOperatingCostsCents = checked(
                location.lifetimeBaseOperatingCostsCents +
                baseOperatingCosts);
            location.lifetimeOperatingProfitCents = checked(
                location.lifetimeOperatingProfitCents + operatingProfit);
            location.lifetimeCashChangeCents = checked(
                location.lifetimeCashChangeCents +
                grossSales - fixedCosts);
            RefreshOperatingAlerts(
                location,
                simulatedDay,
                reorderBlockedByAuthority,
                spendingBlocked);
            int openAlerts = location.operatingAlerts.Count(alert => alert.IsOpen);
            int ownerAlerts = location.operatingAlerts.Count(alert =>
                alert.IsOpen && alert.requiresOwnerAttention);
            location.lastReport = new PortfolioLocationReportSnapshot
            {
                day = simulatedDay,
                locationId = location.locationId,
                demandUnits = demand,
                unitsSold = unitsSold,
                lostDemandUnits = lostDemand,
                endingInventoryUnits = location.inventoryUnits,
                reorderedUnits = reorderedUnits,
                unitPriceCents = merchandiseSales.Count == 1
                    ? merchandiseSales[0].unitPriceCents
                    : 0,
                grossSalesCents = grossSales,
                costOfGoodsSoldCents = costOfGoodsSold,
                payrollCents = payroll,
                rentCents = effectiveRent,
                inventoryPurchaseCents = inventoryPurchase,
                deliveryFeesCents = deliveryFees,
                baseOperatingCostsCents = baseOperatingCosts,
                maintenanceCostsCents = maintenanceCosts,
                operatingProfitCents = checked(
                    operatingProfit - deliveryFees),
                cashChangeCents = checked(
                    grossSales - fixedCosts - maintenanceCosts -
                    inventoryPurchase - deliveryFees),
                serviceQuality = location.serviceQuality,
                customerSatisfaction = location.customerSatisfaction,
                productAvailabilityBasisPoints =
                    location.productAvailabilityBasisPoints,
                productMixBasisPoints = location.productMixBasisPoints,
                maintenanceCondition = location.maintenanceCondition,
                failurePressure = location.failurePressure,
                openAlertCount = openAlerts,
                ownerAttentionAlertCount = ownerAlerts,
                primaryCause = primaryCause,
                isDetailedOperation = false,
                hasExactMerchandiseSales = true,
                merchandiseSales = merchandiseSales
            };
            location.hasLastReport = true;

            bool strained = serviceRatioPercent < 75;
            foreach (PortfolioEmployeeSnapshot employee in assigned)
            {
                int reliabilityGrowth = employee.reliability >= 90 ? 2 : 1;
                employee.skill = Clamp(
                    employee.skill + reliabilityGrowth,
                    0,
                    100);
                employee.satisfaction = Clamp(
                    employee.satisfaction + (strained ? -2 : 1),
                    0,
                    100);
            }

            error = null;
            return true;
        }

        private static List<MerchandiseSaleLineSnapshot>
            AllocateMerchandiseSales(
                IReadOnlyList<MerchandiseOffer> offers,
                IReadOnlyDictionary<string, int> willingDemandByProduct,
                IReadOnlyDictionary<string, int> availableByProduct,
                int maximumUnits)
        {
            List<MerchandiseSaleLineSnapshot> lines = new();
            if (offers == null || willingDemandByProduct == null ||
                availableByProduct == null || maximumUnits <= 0)
            {
                return lines;
            }

            Dictionary<string, MerchandiseSaleLineSnapshot> byProduct =
                offers.ToDictionary(
                    offer => offer.ProductId,
                    offer => new MerchandiseSaleLineSnapshot
                    {
                        productId = offer.ProductId,
                        unitPriceCents = offer.SalePriceCents,
                        quantityUnits = 0
                    },
                    StringComparer.Ordinal);
            int remainingCapacity = maximumUnits;
            while (remainingCapacity > 0)
            {
                bool allocatedAny = false;
                foreach (MerchandiseOffer offer in offers)
                {
                    MerchandiseSaleLineSnapshot line = byProduct[offer.ProductId];
                    int willing = willingDemandByProduct.TryGetValue(
                        offer.ProductId,
                        out int requested)
                        ? requested
                        : 0;
                    int available = availableByProduct.TryGetValue(
                        offer.ProductId,
                        out int inStock)
                        ? inStock
                        : 0;
                    if (line.quantityUnits >= willing ||
                        line.quantityUnits >= available)
                    {
                        continue;
                    }
                    line.quantityUnits++;
                    remainingCapacity--;
                    allocatedAny = true;
                    if (remainingCapacity == 0)
                    {
                        break;
                    }
                }
                if (!allocatedAny)
                {
                    break;
                }
            }

            return byProduct.Values
                .Where(line => line.quantityUnits > 0)
                .OrderBy(line => line.productId, StringComparer.Ordinal)
                .ToList();
        }

        private static void ResolveMaintenanceIntervention(
            PortfolioLocationSnapshot location,
            BusinessOperatingCostProfile costs,
            out long costCents,
            out int recoveryUnits)
        {
            switch (location.delegationPolicy.maintenancePolicy)
            {
                case PortfolioMaintenancePolicy.Preventive:
                    costCents = costs.PreventiveMaintenanceCostCents;
                    recoveryUnits = costs.PreventiveRecoveryUnits;
                    break;
                case PortfolioMaintenancePolicy.Routine:
                    costCents = costs.RoutineMaintenanceCostCents;
                    recoveryUnits = costs.RoutineRecoveryUnits;
                    break;
                default:
                    costCents = costs.EmergencyMaintenanceCostCents;
                    recoveryUnits = costs.EmergencyRecoveryUnits;
                    break;
            }
        }

        private static bool TryBuildProductReorderLines(
            PortfolioLocationSnapshot location,
            int desiredUnits,
            long maximumSpendCents,
            out List<ProcurementOrderRequestLine> lines,
            out int orderedUnits)
        {
            lines = new List<ProcurementOrderRequestLine>();
            orderedUnits = 0;
            if (desiredUnits <= 0 || maximumSpendCents < 0 ||
                !ConvenienceStoreProcurement.Catalog.TryGetSupplier(
                    ConvenienceStoreProcurement.SupplierId,
                    out ProcurementSupplierDefinition supplier))
            {
                return false;
            }

            List<PortfolioProductInventorySnapshot> products =
                location.productInventory
                    .OrderBy(value => value.quantityUnits)
                    .ThenBy(value => value.productId, StringComparer.Ordinal)
                    .ToList();
            Dictionary<string, int> requested = products.ToDictionary(
                value => value.productId,
                _ => 0,
                StringComparer.Ordinal);
            for (int unit = 0; unit < desiredUnits; unit++)
            {
                PortfolioProductInventorySnapshot target =
                    products[unit % products.Count];
                requested[target.productId]++;
            }

            long subtotal = requested.Sum(pair => checked(
                supplier.TryGetResource(
                    pair.Key,
                    out ProcurementResourceDefinition resource)
                    ? resource.UnitCostCents * pair.Value
                    : long.MaxValue));
            while (requested.Values.Sum() > 0 &&
                   checked(subtotal + supplier.DeliveryFeeCents) >
                   maximumSpendCents)
            {
                string removeProduct = requested
                    .Where(pair => pair.Value > 0)
                    .OrderByDescending(pair => pair.Value)
                    .ThenByDescending(pair => pair.Key, StringComparer.Ordinal)
                    .First().Key;
                supplier.TryGetResource(
                    removeProduct,
                    out ProcurementResourceDefinition resource);
                requested[removeProduct]--;
                subtotal -= resource.UnitCostCents;
            }

            orderedUnits = requested.Values.Sum();
            if (orderedUnits <= 0)
            {
                return false;
            }

            lines = requested
                .Where(pair => pair.Value > 0)
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new ProcurementOrderRequestLine(
                    pair.Key,
                    pair.Value))
                .ToList();
            return true;
        }

        private static bool TryPlaceOrderOnCandidate(
            PortfolioProgressionSnapshot candidate,
            string locationId,
            string supplierId,
            IReadOnlyList<ProcurementOrderRequestLine> requestedLines,
            bool protectCashReserve,
            out PurchaseOrderSnapshot order,
            out string error)
        {
            order = null;
            error = null;
            if (!TryGetLocation(candidate, locationId, out PortfolioLocationSnapshot location) ||
                !ProcurementLedger.TryRestore(
                    ConvenienceStoreProcurement.Catalog,
                    candidate.procurement,
                    out ProcurementLedger ledger,
                    out error))
            {
                error ??= "Purchase order location is unavailable.";
                return false;
            }

            if (!TryPlaceOrderOnCandidate(
                    candidate,
                    location,
                    ledger,
                    supplierId,
                    requestedLines,
                    string.Equals(
                        location.locationId,
                        PortfolioProgressionRules.FirstLocationId,
                        StringComparison.Ordinal) &&
                    location.delegatedDaysOperating == 0,
                    protectCashReserve,
                    out order,
                    out error))
            {
                return false;
            }

            candidate.procurement = ledger.CreateSnapshot();
            return true;
        }

        private static bool TryPlaceOrderOnCandidate(
            PortfolioProgressionSnapshot candidate,
            PortfolioLocationSnapshot location,
            ProcurementLedger ledger,
            string supplierId,
            IReadOnlyList<ProcurementOrderRequestLine> requestedLines,
            bool requiresPhysicalProducts,
            bool protectCashReserve,
            out PurchaseOrderSnapshot order,
            out string error)
        {
            order = null;
            error = null;
            if (candidate == null || location == null || ledger == null)
            {
                error = "Purchase order state is unavailable.";
                return false;
            }

            bool hasAggregateLine = requestedLines?.Any(line => string.Equals(
                line.ResourceId,
                ConvenienceStoreProcurement.AggregateResourceId,
                StringComparison.Ordinal)) ?? false;
            bool hasProductLine = requestedLines?.Any(line =>
                ConvenienceStoreProcurement.IsDetailedResource(
                    line.ResourceId)) ?? false;
            if (requestedLines == null ||
                requestedLines.Any(line =>
                    !ConvenienceStoreProcurement.IsDetailedResource(
                        line.ResourceId) &&
                    !string.Equals(
                        line.ResourceId,
                        ConvenienceStoreProcurement.AggregateResourceId,
                        StringComparison.Ordinal)) ||
                (requiresPhysicalProducts && hasAggregateLine) ||
                (hasAggregateLine && hasProductLine))
            {
                error = requiresPhysicalProducts
                    ? "The detailed convenience store accepts only configured physical product resources."
                    : "An off-site convenience store accepts one configured aggregate assortment or configured product resources, but not both in one order.";
                return false;
            }

            if (!ledger.TryPlaceOrder(
                    location.locationId,
                    supplierId,
                    requestedLines,
                    out order,
                    out long chargeCents,
                    out error))
            {
                return false;
            }

            long requiredCash;
            try
            {
                requiredCash = checked(
                    chargeCents +
                    (protectCashReserve
                        ? PortfolioProgressionRules.MinimumCashReserveCents
                        : 0));
            }
            catch (OverflowException)
            {
                order = null;
                error = "Purchase order cash requirement overflowed supported storage.";
                return false;
            }

            if (candidate.cashCents < requiredCash)
            {
                order = null;
                error = protectCashReserve
                    ? $"Purchase order requires {FormatCents(chargeCents)} plus the protected cash reserve."
                    : $"Purchase order requires {FormatCents(chargeCents)} in available cash.";
                return false;
            }

            try
            {
                candidate.cashCents = checked(candidate.cashCents - chargeCents);
                location.lifetimeInventoryPurchaseCents = checked(
                    location.lifetimeInventoryPurchaseCents +
                    order.subtotalCostCents);
                location.lifetimeDeliveryFeesCents = checked(
                    location.lifetimeDeliveryFeesCents +
                    order.deliveryFeeCents);
                location.lifetimeOperatingProfitCents = checked(
                    location.lifetimeOperatingProfitCents -
                    order.deliveryFeeCents);
                location.lifetimeCashChangeCents = checked(
                    location.lifetimeCashChangeCents - chargeCents);
            }
            catch (OverflowException)
            {
                order = null;
                error = "Purchase order would overflow company or location totals.";
                return false;
            }

            error = null;
            return true;
        }

        private static void GetProcurementTotals(
            ProcurementSnapshot procurement,
            string locationId,
            out long netPurchaseCents,
            out long netDeliveryFeesCents,
            out long deliveredInventoryCents)
        {
            netPurchaseCents = 0;
            netDeliveryFeesCents = 0;
            deliveredInventoryCents = 0;
            foreach (PurchaseOrderSnapshot order in procurement.orders)
            {
                if (!string.Equals(
                        order.locationId,
                        locationId,
                        StringComparison.Ordinal) ||
                    order.status == PurchaseOrderStatus.Canceled)
                {
                    continue;
                }

                netPurchaseCents = checked(
                    netPurchaseCents + order.subtotalCostCents);
                netDeliveryFeesCents = checked(
                    netDeliveryFeesCents + order.deliveryFeeCents);
                if (order.status == PurchaseOrderStatus.Delivered ||
                    order.status == PurchaseOrderStatus.PartiallyReceived ||
                    order.status == PurchaseOrderStatus.Completed)
                {
                    deliveredInventoryCents = checked(
                        deliveredInventoryCents + order.subtotalCostCents);
                }
            }
        }

        private bool TryCommit(
            PortfolioProgressionSnapshot candidate,
            out string error)
        {
            if (!TryValidateSnapshot(candidate, out error))
            {
                return false;
            }

            state = candidate;
            error = null;
            return true;
        }

        private static bool TryValidateFinancialReconciliation(
            PortfolioProgressionSnapshot snapshot,
            out string error)
        {
            try
            {
                long expectedCompanyCash = checked(
                    PortfolioProgressionRules.StartingCashCents -
                    snapshot.lifetimeCorporateCostsCents);
                foreach (PortfolioLocationSnapshot location in snapshot.locations)
                {
                    long expectedOperatingProfit = checked(
                        location.lifetimeGrossSalesCents -
                        location.lifetimeCostOfGoodsSoldCents -
                        location.lifetimePayrollCents -
                        location.lifetimeRentCents -
                        location.lifetimeBaseOperatingCostsCents -
                        location.lifetimeMaintenanceCostsCents -
                        location.lifetimeDeliveryFeesCents);
                    long expectedLocationCashChange = checked(
                        location.lifetimeGrossSalesCents -
                        location.lifetimePayrollCents -
                        location.lifetimeRentCents -
                        location.lifetimeBaseOperatingCostsCents -
                        location.lifetimeMaintenanceCostsCents -
                        location.lifetimeInventoryPurchaseCents -
                        location.lifetimeDeliveryFeesCents -
                        location.lifetimeLeaseAndSetupCents);
                    if (location.lifetimeOperatingProfitCents !=
                            expectedOperatingProfit ||
                        location.lifetimeCashChangeCents !=
                            expectedLocationCashChange)
                    {
                        error =
                            $"Location '{location.locationId}' lifetime money totals do not reconcile.";
                        return false;
                    }

                    expectedCompanyCash = checked(
                        expectedCompanyCash +
                        location.lifetimeCashChangeCents);
                }

                foreach (PortfolioCommercialPropertySnapshot property in
                         snapshot.company.properties)
                {
                    if (property.tenure == PortfolioPropertyTenure.Owned)
                    {
                        expectedCompanyCash = checked(
                            expectedCompanyCash -
                            property.acquisitionCostCents);
                    }

                    foreach (PortfolioImprovementSnapshot improvement in
                             property.commercialUnits.SelectMany(unit =>
                                 unit.improvements))
                    {
                        expectedCompanyCash = checked(
                            expectedCompanyCash - improvement.costCents);
                    }
                }

                if (snapshot.cashCents != expectedCompanyCash)
                {
                    error =
                        "Company cash does not reconcile to operating, corporate, property, and improvement history.";
                    return false;
                }
            }
            catch (OverflowException)
            {
                error =
                    "Portfolio lifetime financial reconciliation overflowed integer-cent storage.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryValidateLocation(
            PortfolioLocationSnapshot location,
            out string error)
        {
            if (location == null ||
                !FirstStoreIdentifier.IsValid(location.locationId) ||
                string.IsNullOrWhiteSpace(location.displayName) ||
                string.IsNullOrWhiteSpace(location.districtName) ||
                string.IsNullOrWhiteSpace(location.marketSummary) ||
                !PortfolioProgressionRules.TryGetLocationDefinition(
                    location.locationId,
                    out PortfolioLocationDefinition definition) ||
                !string.Equals(
                    location.displayName,
                    definition.DisplayName,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    location.districtName,
                    definition.DistrictName,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    location.marketSummary,
                    definition.MarketSummary,
                    StringComparison.Ordinal) ||
                (!string.IsNullOrWhiteSpace(location.businessTypeId) &&
                 !string.Equals(
                     location.businessTypeId,
                     definition.BusinessTypeId,
                     StringComparison.Ordinal)) ||
                (!string.IsNullOrWhiteSpace(location.operatingModel) &&
                 !string.Equals(
                     location.operatingModel,
                     definition.OperatingModel,
                     StringComparison.Ordinal)) ||
                !StableIdentifier.IsValid(location.businessTypeId) ||
                !StableIdentifier.IsValid(location.brandId) ||
                !StableIdentifier.IsValid(location.propertyId) ||
                !StableIdentifier.IsValid(location.commercialUnitId) ||
                location.baseDemandUnits != definition.BaseDemandUnits ||
                location.competitionIndex != definition.CompetitionIndex ||
                location.reputation < 0 ||
                location.reputation > 100 ||
                location.inventoryUnits < 0 ||
                location.inventoryCapacityUnits != definition.InventoryCapacityUnits ||
                location.inventoryUnits > location.inventoryCapacityUnits ||
                location.dailyRentCents != definition.DailyRentCents ||
                location.leaseCostCents != definition.LeaseCostCents ||
                location.openingInventoryCostCents !=
                definition.OpeningInventoryCostCents ||
                location.daysOperating < 0 ||
                location.delegatedDaysOperating < 0 ||
                location.lifetimeGrossSalesCents < 0 ||
                location.lifetimeCostOfGoodsSoldCents < 0 ||
                location.lifetimePayrollCents < 0 ||
                location.lifetimeRentCents < 0 ||
                location.lifetimeInventoryPurchaseCents < 0 ||
                location.lifetimeDeliveryFeesCents < 0 ||
                location.lifetimeBaseOperatingCostsCents < 0 ||
                location.lifetimeMaintenanceCostsCents < 0 ||
                location.lifetimeLeaseAndSetupCents < 0 ||
                location.customerSatisfaction < 0 ||
                location.customerSatisfaction > 100 ||
                location.serviceQuality < 0 ||
                location.serviceQuality > 100 ||
                location.productAvailabilityBasisPoints < 0 ||
                location.productAvailabilityBasisPoints >
                PortfolioOperationsRules.BasisPoints ||
                location.productMixBasisPoints < 0 ||
                location.productMixBasisPoints >
                PortfolioOperationsRules.BasisPoints ||
                location.maintenanceCondition < 0 ||
                location.maintenanceCondition > 100 ||
                location.failurePressure < 0 ||
                location.failurePressure > 100 ||
                !Enum.IsDefined(typeof(PortfolioPricingPolicy), location.pricingPolicy) ||
                !Enum.IsDefined(typeof(PortfolioReorderPolicy), location.reorderPolicy))
            {
                error = "Portfolio location state is invalid or contradicts its market definition.";
                return false;
            }

            if (!MerchandisingRules.TryValidate(
                    location.merchandisePrices,
                    location.shelfMerchandiseAssignments,
                    out error))
            {
                error =
                    $"Location '{location.locationId}' merchandising is invalid: {error}";
                return false;
            }

            if (!PortfolioOperationsRules.TryValidateProductInventory(
                    location.productInventory,
                    location.merchandisePrices,
                    location.inventoryUnits,
                    out error) ||
                !PortfolioOperationsRules.TryValidateDelegationPolicy(
                    location.delegationPolicy,
                    out error) ||
                !TryValidateDetailedReconciliation(
                    location.detailedReconciliation,
                    location.merchandisePrices,
                    out error) ||
                location.operatingAlerts == null)
            {
                error =
                    $"Location '{location.locationId}' operations state is invalid: {error}";
                return false;
            }

            HashSet<string> alertIds = new(StringComparer.Ordinal);
            foreach (PortfolioOperatingAlertSnapshot alert in
                     location.operatingAlerts)
            {
                if (!PortfolioOperationsRules.TryValidateAlert(
                        alert,
                        Math.Max(1, location.lastReport?.day ?? 1),
                        out error) ||
                    !alertIds.Add(alert.alertId))
                {
                    error ??=
                        $"Location '{location.locationId}' has duplicate operating alerts.";
                    return false;
                }
            }

            if (!location.hasLastReport)
            {
                if (location.lastReport != null &&
                    !IsSerializedNoReportPlaceholder(location.lastReport))
                {
                    error = "Portfolio report presence flag and report data disagree.";
                    return false;
                }
            }
            else if (location.lastReport == null)
            {
                error = "Portfolio report presence flag requires report data.";
                return false;
            }
            else if (!TryValidateReport(location, location.lastReport, out error))
            {
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryValidateDetailedReconciliation(
            PortfolioDetailedReconciliationSnapshot reconciliation,
            IReadOnlyList<MerchandisePriceSnapshot> merchandisePrices,
            out string error)
        {
            error = null;
            if (reconciliation == null || reconciliation.metrics == null ||
                reconciliation.sessionStartedDay < 0 ||
                reconciliation.startingInventoryAssetValueCents < 0 ||
                reconciliation.startingDeliveredProcurementInventoryCents < 0 ||
                reconciliation.startingProcurementPurchaseCents < 0 ||
                reconciliation.startingProcurementDeliveryFeesCents < 0 ||
                reconciliation.startingCustomerSatisfaction < 0 ||
                reconciliation.startingCustomerSatisfaction > 100 ||
                reconciliation.startingMaintenanceCondition < 0 ||
                reconciliation.startingMaintenanceCondition > 100 ||
                reconciliation.grossSalesCents < 0 ||
                reconciliation.costOfGoodsSoldCents < 0 ||
                reconciliation.includedOperatingExpensesCents < 0 ||
                reconciliation.payrollCents < 0 ||
                reconciliation.rentCents < 0 ||
                reconciliation.operatingCostCents < 0 ||
                reconciliation.inventoryAcquiredCostCents < 0 ||
                reconciliation.unitsSold < 0 ||
                reconciliation.transactionCount < 0 ||
                reconciliation.detailedInventoryBaselineValueCents < 0 ||
                reconciliation.payrollCents >
                reconciliation.includedOperatingExpensesCents ||
                reconciliation.rentCents >
                reconciliation.includedOperatingExpensesCents -
                reconciliation.payrollCents ||
                reconciliation.operatingCostCents !=
                reconciliation.includedOperatingExpensesCents -
                reconciliation.payrollCents -
                reconciliation.rentCents ||
                reconciliation.initialized !=
                !string.IsNullOrWhiteSpace(reconciliation.sessionId) ||
                (reconciliation.initialized &&
                 (!FirstStoreIdentifier.IsValid(reconciliation.sessionId) ||
                  reconciliation.sessionStartedDay < 1)) ||
                (!reconciliation.initialized &&
                 (reconciliation.sessionStartedDay != 0 ||
                  reconciliation.startingInventoryAssetValueCents != 0 ||
                  reconciliation.startingDeliveredProcurementInventoryCents != 0 ||
                  reconciliation.startingProcurementPurchaseCents != 0 ||
                  reconciliation.startingProcurementDeliveryFeesCents != 0 ||
                  reconciliation.startingCustomerSatisfaction != 0 ||
                   reconciliation.startingMaintenanceCondition != 0 ||
                   reconciliation.grossSalesCents != 0 ||
                  reconciliation.costOfGoodsSoldCents != 0 ||
                  reconciliation.includedOperatingExpensesCents != 0 ||
                   reconciliation.inventoryAcquiredCostCents != 0 ||
                   reconciliation.unitsSold != 0 ||
                   reconciliation.transactionCount != 0)) ||
                (!reconciliation.usesDetailedInventoryBaseline &&
                 (reconciliation.detailedInventoryBaselineValueCents != 0 ||
                  reconciliation.detailedProductInventoryBaseline?.Count > 0)) ||
                !reconciliation.metrics.TryValidate(out error))
            {
                error ??= "Detailed reconciliation fields disagree.";
                return false;
            }

            if (reconciliation.usesDetailedInventoryBaseline)
            {
                if (!reconciliation.initialized ||
                    reconciliation.detailedProductInventoryBaseline == null)
                {
                    error =
                        "Detailed return baseline requires an initialized session and product inventory.";
                    return false;
                }

                int baselineUnits;
                try
                {
                    baselineUnits = reconciliation
                        .detailedProductInventoryBaseline
                        .Sum(value => value.quantityUnits);
                }
                catch (OverflowException)
                {
                    error =
                        "Detailed return baseline exceeds supported inventory storage.";
                    return false;
                }
                if (!PortfolioOperationsRules.TryValidateProductInventory(
                        reconciliation.detailedProductInventoryBaseline,
                        merchandisePrices,
                        baselineUnits,
                        out error) ||
                    CalculateProductInventoryValue(
                        reconciliation.detailedProductInventoryBaseline) !=
                    reconciliation.detailedInventoryBaselineValueCents)
                {
                    error ??=
                        "Detailed return inventory baseline does not reconcile.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static bool TryValidateReport(
            PortfolioLocationSnapshot location,
            PortfolioLocationReportSnapshot report,
            out string error)
        {
            if (!PortfolioProgressionRules.TryGetLocationDefinition(
                    location.locationId,
                    out PortfolioLocationDefinition definition) ||
                definition.SimulationProfile?.UnitEconomy == null)
            {
                error = "Portfolio location report has no simulation definition.";
                return false;
            }

            long aggregateUnitCost =
                definition.SimulationProfile.UnitEconomy.VariableUnitCostCents;
            long legacyExpectedGrossSales;
            long expectedCostOfGoods;
            long expectedInventoryPurchase;
            long expectedOperatingProfit;
            long expectedCashChange;
            try
            {
                legacyExpectedGrossSales = checked(
                    report.unitPriceCents * report.unitsSold);
                expectedCostOfGoods = report.hasExactMerchandiseSales &&
                                      report.merchandiseSales != null
                    ? CalculateReportCostOfGoods(
                        report.merchandiseSales,
                        aggregateUnitCost)
                    : checked(aggregateUnitCost * report.unitsSold);
                expectedInventoryPurchase = checked(
                    aggregateUnitCost * report.reorderedUnits);
                expectedOperatingProfit = checked(
                    report.grossSalesCents -
                    report.costOfGoodsSoldCents -
                     report.payrollCents -
                     report.rentCents -
                     report.baseOperatingCostsCents -
                     report.maintenanceCostsCents -
                     report.deliveryFeesCents);
                expectedCashChange = checked(
                    report.grossSalesCents -
                     report.payrollCents -
                     report.rentCents -
                     report.baseOperatingCostsCents -
                     report.maintenanceCostsCents -
                     report.inventoryPurchaseCents -
                    report.deliveryFeesCents);
            }
            catch (OverflowException)
            {
                error = "Portfolio location report arithmetic overflowed integer-cent storage.";
                return false;
            }

            if (report.hasExactMerchandiseSales &&
                !MerchandisingRules.TryValidateSalesBreakdown(
                    report.merchandiseSales,
                    report.unitsSold,
                    report.grossSalesCents,
                    out error))
            {
                return false;
            }

            long exactSummaryPrice = 0;
            if (report.hasExactMerchandiseSales &&
                report.merchandiseSales != null &&
                report.merchandiseSales.Count == 1)
            {
                exactSummaryPrice = report.merchandiseSales[0].unitPriceCents;
            }

            bool commonValid =
                report.day >= (report.isDetailedOperation ? 1 : 2) &&
                string.Equals(
                    report.locationId,
                    location.locationId,
                    StringComparison.Ordinal) &&
                report.demandUnits >= 0 &&
                report.unitsSold >= 0 &&
                report.lostDemandUnits >= 0 &&
                report.lostDemandUnits == report.demandUnits - report.unitsSold &&
                report.endingInventoryUnits == location.inventoryUnits &&
                report.reorderedUnits >= 0 &&
                report.payrollCents >= 0 &&
                report.deliveryFeesCents >= 0 &&
                report.baseOperatingCostsCents >= 0 &&
                report.maintenanceCostsCents >= 0 &&
                report.serviceQuality >= 0 &&
                report.serviceQuality <= 100 &&
                report.customerSatisfaction >= 0 &&
                report.customerSatisfaction <= 100 &&
                report.productAvailabilityBasisPoints >= 0 &&
                report.productAvailabilityBasisPoints <=
                PortfolioOperationsRules.BasisPoints &&
                report.productMixBasisPoints >= 0 &&
                report.productMixBasisPoints <=
                PortfolioOperationsRules.BasisPoints &&
                report.maintenanceCondition >= 0 &&
                report.maintenanceCondition <= 100 &&
                report.failurePressure >= 0 &&
                report.failurePressure <= 100 &&
                report.openAlertCount >= 0 &&
                report.ownerAttentionAlertCount >= 0 &&
                report.ownerAttentionAlertCount <= report.openAlertCount &&
                report.operatingProfitCents == expectedOperatingProfit &&
                report.cashChangeCents == expectedCashChange &&
                !string.IsNullOrWhiteSpace(report.primaryCause);
            bool priceBreakdownValid = report.hasExactMerchandiseSales
                ? report.unitPriceCents == exactSummaryPrice
                : report.isDetailedOperation
                    ? report.unitPriceCents == 0
                    : report.unitPriceCents > 0 &&
                      report.grossSalesCents == legacyExpectedGrossSales;
            bool modeValid = report.isDetailedOperation
                ? priceBreakdownValid &&
                  report.reorderedUnits == 0 &&
                  report.grossSalesCents >= 0 &&
                  report.costOfGoodsSoldCents >= 0 &&
                  report.rentCents >= 0 &&
                  report.inventoryPurchaseCents >= 0
                : priceBreakdownValid &&
                  report.costOfGoodsSoldCents == expectedCostOfGoods &&
                  (report.rentCents == location.dailyRentCents ||
                   report.rentCents == 0) &&
                  (report.reorderedUnits == 0
                      ? report.inventoryPurchaseCents == 0
                      : report.inventoryPurchaseCents > 0);
            bool valid = commonValid && modeValid;
            if (!valid)
            {
                error = "Portfolio location report does not reconcile.";
                return false;
            }

            error = null;
            return true;
        }

        private static long CalculateReportCostOfGoods(
            IReadOnlyList<MerchandiseSaleLineSnapshot> sales,
            long fallbackUnitCostCents)
        {
            ConvenienceStoreProcurement.Catalog.TryGetSupplier(
                ConvenienceStoreProcurement.SupplierId,
                out ProcurementSupplierDefinition supplier);
            long total = 0;
            foreach (MerchandiseSaleLineSnapshot line in sales)
            {
                long unitCost = supplier != null &&
                                supplier.TryGetResource(
                                    line.productId,
                                    out ProcurementResourceDefinition resource)
                    ? resource.UnitCostCents
                    : fallbackUnitCostCents;
                total = checked(total + unitCost * line.quantityUnits);
            }
            return total;
        }

        private static bool TryValidateEmployee(
            PortfolioEmployeeSnapshot employee,
            ISet<string> locationIds,
            int currentDay,
            out string error)
        {
            error = null;
            if (employee == null ||
                !FirstStoreIdentifier.IsValid(employee.employeeId) ||
                !PortfolioProgressionRules.TryGetCandidate(
                    employee.employeeId,
                    out PortfolioCandidateDefinition definition) ||
                string.IsNullOrWhiteSpace(employee.displayName) ||
                string.IsNullOrWhiteSpace(employee.trait) ||
                !Enum.IsDefined(typeof(PortfolioEmployeeRole), employee.role) ||
                !Enum.IsDefined(typeof(PortfolioTaskFocus), employee.taskFocus) ||
                employee.skill < 0 || employee.skill > 100 ||
                employee.reliability < 0 || employee.reliability > 100 ||
                employee.satisfaction < 0 || employee.satisfaction > 100 ||
                employee.dailyWageCents <= 0 ||
                employee.hiringCostCents < 0 ||
                employee.lastTrainingDay < 0 ||
                employee.lastTrainingDay > currentDay ||
                !locationIds.Contains(employee.assignedLocationId) ||
                !PortfolioOperationsRules.TryValidateSchedule(
                    employee.schedule,
                    out error))
            {
                error ??= "Portfolio employee state is invalid.";
                return false;
            }

            bool roleIsOriginal = employee.role == definition.Role;
            bool validPromotion =
                definition.Role != PortfolioEmployeeRole.Manager &&
                employee.role == PortfolioEmployeeRole.Manager;
            long expectedWage = validPromotion
                ? Math.Max(definition.DailyWageCents, 17_500)
                : definition.DailyWageCents;
            if ((!roleIsOriginal && !validPromotion) ||
                !string.Equals(
                    employee.displayName,
                    definition.DisplayName,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    employee.trait,
                    definition.Trait,
                    StringComparison.Ordinal) ||
                employee.reliability != definition.Reliability ||
                employee.dailyWageCents != expectedWage ||
                employee.hiringCostCents != definition.HiringCostCents)
            {
                error = "Portfolio employee state contradicts the persistent candidate record.";
                return false;
            }

            error = null;
            return true;
        }

        private static PortfolioLocationSnapshot CreateLocation(
            PortfolioLocationDefinition definition)
        {
            List<MerchandisePriceSnapshot> merchandisePrices =
                MerchandisingRules.CreateConvenienceStorePrices(
                    PortfolioPricingPolicy.Balanced);
            if (!PortfolioPropertyRules.TryGetDefinitionForLocation(
                    definition.LocationId,
                    out PortfolioPropertyDefinition propertyDefinition))
            {
                throw new InvalidOperationException(
                    $"Location '{definition.LocationId}' has no persistent property definition.");
            }

            return new PortfolioLocationSnapshot
            {
                locationId = definition.LocationId,
                displayName = definition.DisplayName,
                districtName = definition.DistrictName,
                marketSummary = definition.MarketSummary,
                businessTypeId = definition.BusinessTypeId,
                operatingModel = definition.OperatingModel,
                brandId = PortfolioPropertyRules.ConvenienceBrandId,
                propertyId = propertyDefinition.PropertyId,
                commercialUnitId = propertyDefinition.CommercialUnitId,
                baseDemandUnits = definition.BaseDemandUnits,
                competitionIndex = definition.CompetitionIndex,
                reputation = definition.StartingReputation,
                inventoryUnits = definition.OpeningInventoryUnits,
                inventoryCapacityUnits = definition.InventoryCapacityUnits,
                dailyRentCents = definition.DailyRentCents,
                leaseCostCents = definition.LeaseCostCents,
                openingInventoryCostCents = definition.OpeningInventoryCostCents,
                pricingPolicy = PortfolioPricingPolicy.Balanced,
                reorderPolicy = PortfolioReorderPolicy.Balanced,
                merchandisePrices = merchandisePrices,
                shelfMerchandiseAssignments =
                    MerchandisingRules.CreateConvenienceStoreAssignments(
                        definition.LocationId),
                productInventory =
                    PortfolioOperationsRules.CreateProvisionalProductInventory(
                        merchandisePrices,
                        definition.OpeningInventoryUnits,
                        PortfolioProgressionRules.AggregateUnitCostCents),
                delegationPolicy =
                    PortfolioOperationsRules.CreateDefaultDelegationPolicy(),
                customerSatisfaction = definition.StartingReputation,
                serviceQuality = 100,
                productAvailabilityBasisPoints =
                    definition.OpeningInventoryUnits > 0
                        ? PortfolioOperationsRules.BasisPoints
                        : 0,
                productMixBasisPoints =
                    definition.OpeningInventoryUnits > 0
                        ? PortfolioOperationsRules.BasisPoints
                        : 0,
                maintenanceCondition = 100,
                failurePressure = 0,
                detailedReconciliation =
                    new PortfolioDetailedReconciliationSnapshot(),
                operatingAlerts = new List<PortfolioOperatingAlertSnapshot>(),
                daysOperating = 0,
                delegatedDaysOperating = 0,
                lifetimeGrossSalesCents = 0,
                lifetimeCostOfGoodsSoldCents = 0,
                lifetimePayrollCents = 0,
                lifetimeRentCents = 0,
                lifetimeInventoryPurchaseCents = 0,
                lifetimeDeliveryFeesCents = 0,
                lifetimeBaseOperatingCostsCents = 0,
                lifetimeMaintenanceCostsCents = 0,
                lifetimeLeaseAndSetupCents = 0,
                lifetimeCashChangeCents = 0,
                lifetimeOperatingProfitCents = 0,
                hasLastReport = false,
                lastReport = null
            };
        }

        private static bool TryGetLocation(
            PortfolioProgressionSnapshot snapshot,
            string locationId,
            out PortfolioLocationSnapshot location)
        {
            location = snapshot.locations.FirstOrDefault(value => string.Equals(
                value.locationId,
                locationId,
                StringComparison.Ordinal));
            return location != null;
        }

        private static bool TryGetEmployee(
            PortfolioProgressionSnapshot snapshot,
            string employeeId,
            out PortfolioEmployeeSnapshot employee)
        {
            employee = snapshot.employees.FirstOrDefault(value => string.Equals(
                value.employeeId,
                employeeId,
                StringComparison.Ordinal));
            return employee != null;
        }

        private static bool TryGetCommercialUnit(
            PortfolioCompanySnapshot company,
            string locationId,
            out PortfolioCommercialUnitSnapshot unit)
        {
            unit = company?.properties?
                .SelectMany(property =>
                    property?.commercialUnits ??
                    Enumerable.Empty<PortfolioCommercialUnitSnapshot>())
                .FirstOrDefault(value => string.Equals(
                    value?.occupyingLocationId,
                    locationId,
                    StringComparison.Ordinal));
            return unit != null;
        }

        private static long EffectiveDailyRent(
            PortfolioCompanySnapshot company,
            PortfolioLocationSnapshot location)
        {
            PortfolioCommercialPropertySnapshot property =
                company?.properties?.FirstOrDefault(value => string.Equals(
                    value.propertyId,
                    location.propertyId,
                    StringComparison.Ordinal));
            return property?.tenure == PortfolioPropertyTenure.Owned
                ? 0
                : location.dailyRentCents;
        }

        private static bool TryCalculatePortfolioFixedCosts(
            PortfolioProgressionSnapshot snapshot,
            int simulationDay,
            out long fixedCosts,
            out string error)
        {
            fixedCosts = 0;
            if (snapshot?.company == null || snapshot.locations == null ||
                snapshot.employees == null || simulationDay < 1)
            {
                error = "Portfolio fixed-cost inputs are unavailable.";
                return false;
            }

            try
            {
                foreach (PortfolioLocationSnapshot location in snapshot.locations)
                {
                    if (!PortfolioProgressionRules.TryGetLocationDefinition(
                            location.locationId,
                            out PortfolioLocationDefinition definition) ||
                        definition.SimulationProfile?.OperatingCosts == null)
                    {
                        error =
                            $"Location '{location.locationId}' has no operating-cost profile.";
                        return false;
                    }

                    fixedCosts = checked(
                        fixedCosts +
                        EffectiveDailyRent(snapshot.company, location) +
                        definition.SimulationProfile.OperatingCosts
                            .BaseOperatingCostCents);
                }

                foreach (PortfolioEmployeeSnapshot employee in snapshot.employees)
                {
                    if (PortfolioOperationsRules.IsScheduled(
                            employee.schedule,
                            simulationDay))
                    {
                        fixedCosts = checked(
                            fixedCosts + employee.dailyWageCents);
                    }
                }
            }
            catch (OverflowException)
            {
                fixedCosts = 0;
                error = "Portfolio fixed costs overflowed integer-cent storage.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool HasRole(
            PortfolioProgressionSnapshot snapshot,
            string locationId,
            PortfolioEmployeeRole role)
        {
            return snapshot.employees.Any(employee =>
                employee.role == role &&
                string.Equals(
                    employee.assignedLocationId,
                    locationId,
                    StringComparison.Ordinal));
        }

        private static bool HasScheduledRole(
            PortfolioProgressionSnapshot snapshot,
            string locationId,
            PortfolioEmployeeRole role,
            int day)
        {
            return snapshot.employees.Any(employee =>
                employee.role == role &&
                string.Equals(
                    employee.assignedLocationId,
                    locationId,
                    StringComparison.Ordinal) &&
                PortfolioOperationsRules.IsScheduled(employee.schedule, day));
        }

        private static bool IsFullyStaffed(
            PortfolioProgressionSnapshot snapshot,
            string locationId)
        {
            return HasRole(snapshot, locationId, PortfolioEmployeeRole.Cashier) &&
                   HasRole(snapshot, locationId, PortfolioEmployeeRole.StockClerk) &&
                   HasRole(snapshot, locationId, PortfolioEmployeeRole.Manager);
        }

        private static void ResolveReorderPolicy(
            PortfolioLocationSnapshot location,
            out int reorderPoint,
            out int reorderTarget)
        {
            float targetRatio = location.reorderPolicy switch
            {
                PortfolioReorderPolicy.Lean => 0.48f,
                PortfolioReorderPolicy.Resilient => 0.9f,
                _ => 0.72f
            };
            reorderTarget = Math.Min(
                location.inventoryCapacityUnits,
                (int)Math.Round(location.inventoryCapacityUnits * targetRatio));
            reorderPoint = location.reorderPolicy switch
            {
                PortfolioReorderPolicy.Lean => reorderTarget / 3,
                PortfolioReorderPolicy.Resilient => reorderTarget * 2 / 3,
                _ => reorderTarget / 2
            };
        }

        private static int DeterministicVariance(
            int day,
            string stableId,
            int minimum,
            int maximum)
        {
            unchecked
            {
                uint hash = 2166136261;
                for (int index = 0; index < stableId.Length; index++)
                {
                    hash ^= stableId[index];
                    hash *= 16777619;
                }
                hash ^= (uint)day;
                hash *= 16777619;
                int range = maximum - minimum + 1;
                return minimum + (int)(hash % (uint)range);
            }
        }

        private static bool TryNormalizeSnapshot(
            PortfolioProgressionSnapshot source,
            out PortfolioProgressionSnapshot normalized,
            out string error)
        {
            normalized = null;
            if (source == null ||
                (source.version != PortfolioProgressionSnapshot.LegacyVersion &&
                 source.version != PortfolioProgressionSnapshot.VersionTwo &&
                 source.version != PortfolioProgressionSnapshot.PriorVersion &&
                 source.version != PortfolioProgressionSnapshot.CurrentVersion))
            {
                error = "Portfolio snapshot version is missing or unsupported.";
                return false;
            }

            if (source.version != PortfolioProgressionSnapshot.LegacyVersion &&
                source.procurement == null)
            {
                error = "Current portfolio snapshot is missing procurement state.";
                return false;
            }

            if (source.version >= PortfolioProgressionSnapshot.PriorVersion &&
                source.locations?.Any(location =>
                    location == null || location.merchandisePrices == null ||
                    location.shelfMerchandiseAssignments == null) == true)
            {
                error = "Version-three or newer portfolio state is missing merchandising data.";
                return false;
            }

            normalized = source.version == PortfolioProgressionSnapshot.CurrentVersion
                ? source
                : Clone(source);
            error = null;
            return true;
        }

        private static PortfolioProgressionSnapshot Clone(
            PortfolioProgressionSnapshot source)
        {
            PortfolioProgressionSnapshot clone = new()
            {
                version = PortfolioProgressionSnapshot.CurrentVersion,
                currentDay = source.currentDay,
                cashCents = source.cashCents,
                companyReputation = source.companyReputation,
                firstShiftCompleted = source.firstShiftCompleted,
                lifetimeCorporateCostsCents = source.lifetimeCorporateCostsCents,
                procurement = source.procurement == null
                    ? ProcurementLedger.CreateInitial(
                        ConvenienceStoreProcurement.Catalog).CreateSnapshot()
                    : ProcurementLedger.CopySnapshot(source.procurement),
                employees = source.employees?
                    .Select(employee => employee == null
                        ? null
                        : CloneEmployee(employee))
                    .ToList() ?? new List<PortfolioEmployeeSnapshot>(),
                locations = source.locations?
                    .Select(location => location == null
                        ? null
                        : CloneLocation(
                            location,
                            source.version <=
                            PortfolioProgressionSnapshot.VersionTwo,
                            source.version !=
                            PortfolioProgressionSnapshot.CurrentVersion))
                    .ToList() ?? new List<PortfolioLocationSnapshot>()
            };
            if (source.version != PortfolioProgressionSnapshot.CurrentVersion &&
                clone.locations.FirstOrDefault(location => string.Equals(
                    location.locationId,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal)) is PortfolioLocationSnapshot first)
            {
                first.detailedReconciliation =
                    new PortfolioDetailedReconciliationSnapshot
                    {
                        initialized = source.detailedOperationInitialized,
                        sessionId = source.processedDetailedSessionId,
                        sessionStartedDay = source.detailedOperationInitialized
                            ? 1
                            : 0,
                        startingInventoryAssetValueCents = 0,
                        startingDeliveredProcurementInventoryCents = 0,
                        startingProcurementPurchaseCents = 0,
                        startingProcurementDeliveryFeesCents = 0,
                        startingCustomerSatisfaction =
                            source.detailedOperationInitialized
                                ? first.reputation
                                : 0,
                        startingMaintenanceCondition =
                            source.detailedOperationInitialized ? 100 : 0,
                        grossSalesCents =
                            source.reconciledDetailedGrossSalesCents,
                        costOfGoodsSoldCents =
                            source.reconciledDetailedCostOfGoodsSoldCents,
                        includedOperatingExpensesCents =
                            source.reconciledDetailedOperatingExpensesCents,
                        payrollCents = source.reconciledDetailedPayrollCents,
                        rentCents = source.reconciledDetailedRentCents,
                        operatingCostCents = 0,
                        inventoryAcquiredCostCents =
                            source.reconciledDetailedInventoryAcquiredCostCents,
                        unitsSold = source.reconciledDetailedUnitsSold,
                        transactionCount =
                            source.reconciledDetailedTransactionCount,
                        metrics = new DetailedOperationMetricsSnapshot
                        {
                            customerVisits = source.reconciledDetailedTransactionCount,
                            customersServed = source.reconciledDetailedTransactionCount,
                            requestedProductUnits =
                                source.reconciledDetailedUnitsSold,
                            standardsTaskComplete = true
                        }
                    };
                if (first.detailedReconciliation.initialized &&
                    first.detailedReconciliation.includedOperatingExpensesCents > 0 &&
                    first.detailedReconciliation.payrollCents == 0 &&
                    first.detailedReconciliation.rentCents == 0)
                {
                    first.detailedReconciliation.rentCents =
                        first.detailedReconciliation.includedOperatingExpensesCents;
                }
            }
            clone.company = source.version ==
                            PortfolioProgressionSnapshot.CurrentVersion
                ? PortfolioPropertyRules.Clone(source.company)
                : PortfolioPropertyRules.CreateForLocations(clone.locations);
            SortCollections(clone);
            return clone;
        }

        private static PortfolioEmployeeSnapshot CloneEmployee(
            PortfolioEmployeeSnapshot source)
        {
            return new PortfolioEmployeeSnapshot
            {
                employeeId = source.employeeId,
                displayName = source.displayName,
                trait = source.trait,
                role = source.role,
                taskFocus = source.taskFocus,
                skill = source.skill,
                reliability = source.reliability,
                satisfaction = source.satisfaction,
                dailyWageCents = source.dailyWageCents,
                hiringCostCents = source.hiringCostCents,
                assignedLocationId = source.assignedLocationId,
                lastTrainingDay = source.lastTrainingDay,
                schedule = source.schedule == null
                    ? PortfolioOperationsRules.CreateDefaultSchedule()
                    : PortfolioOperationsRules.Clone(source.schedule)
            };
        }

        private static PortfolioLocationSnapshot CloneLocation(
            PortfolioLocationSnapshot source,
            bool migrateLegacyMerchandising,
            bool migrateOperations)
        {
            PortfolioProgressionRules.TryGetLocationDefinition(
                source.locationId,
                out PortfolioLocationDefinition definition);
            PortfolioPropertyRules.TryGetDefinitionForLocation(
                source.locationId,
                out PortfolioPropertyDefinition propertyDefinition);
            bool legacyLocation = string.IsNullOrWhiteSpace(source.businessTypeId);
            return new PortfolioLocationSnapshot
            {
                locationId = source.locationId,
                displayName = source.displayName,
                districtName = source.districtName,
                marketSummary = source.marketSummary,
                businessTypeId = string.IsNullOrWhiteSpace(source.businessTypeId)
                    ? definition?.BusinessTypeId
                    : source.businessTypeId,
                operatingModel = string.IsNullOrWhiteSpace(source.operatingModel)
                    ? definition?.OperatingModel
                    : source.operatingModel,
                brandId = migrateOperations ||
                          string.IsNullOrWhiteSpace(source.brandId)
                    ? PortfolioPropertyRules.ConvenienceBrandId
                    : source.brandId,
                propertyId = migrateOperations ||
                             string.IsNullOrWhiteSpace(source.propertyId)
                    ? propertyDefinition?.PropertyId
                    : source.propertyId,
                commercialUnitId = migrateOperations ||
                                   string.IsNullOrWhiteSpace(
                                       source.commercialUnitId)
                    ? propertyDefinition?.CommercialUnitId
                    : source.commercialUnitId,
                baseDemandUnits = source.baseDemandUnits,
                competitionIndex = source.competitionIndex,
                reputation = source.reputation,
                inventoryUnits = source.inventoryUnits,
                inventoryCapacityUnits = source.inventoryCapacityUnits,
                dailyRentCents = source.dailyRentCents,
                leaseCostCents = source.leaseCostCents,
                openingInventoryCostCents = source.openingInventoryCostCents,
                pricingPolicy = source.pricingPolicy,
                reorderPolicy = source.reorderPolicy,
                merchandisePrices = migrateLegacyMerchandising
                    ? MerchandisingRules.CreateConvenienceStorePrices(
                        source.pricingPolicy)
                    : source.merchandisePrices?
                        .Select(CloneMerchandisePrice)
                        .ToList(),
                shelfMerchandiseAssignments = migrateLegacyMerchandising
                    ? MerchandisingRules.CreateConvenienceStoreAssignments(
                        source.locationId)
                    : source.shelfMerchandiseAssignments?
                        .Select(CloneShelfAssignment)
                        .ToList(),
                productInventory = migrateOperations
                    ? PortfolioOperationsRules.CreateProvisionalProductInventory(
                        migrateLegacyMerchandising
                            ? MerchandisingRules.CreateConvenienceStorePrices(
                                source.pricingPolicy)
                            : source.merchandisePrices,
                        source.inventoryUnits,
                        PortfolioProgressionRules.AggregateUnitCostCents)
                    : source.productInventory?
                        .Select(PortfolioOperationsRules.Clone)
                        .ToList(),
                delegationPolicy = migrateOperations
                    ? PortfolioOperationsRules.CreateDefaultDelegationPolicy()
                    : PortfolioOperationsRules.Clone(source.delegationPolicy),
                customerSatisfaction = migrateOperations
                    ? source.reputation
                    : source.customerSatisfaction,
                serviceQuality = migrateOperations ? 100 : source.serviceQuality,
                productAvailabilityBasisPoints = migrateOperations
                    ? (source.inventoryUnits > 0
                        ? PortfolioOperationsRules.BasisPoints
                        : 0)
                    : source.productAvailabilityBasisPoints,
                productMixBasisPoints = migrateOperations
                    ? (source.inventoryUnits > 0
                        ? PortfolioOperationsRules.BasisPoints
                        : 0)
                    : source.productMixBasisPoints,
                maintenanceCondition = migrateOperations
                    ? 100
                    : source.maintenanceCondition,
                failurePressure = migrateOperations
                    ? 0
                    : source.failurePressure,
                detailedReconciliation = migrateOperations
                    ? new PortfolioDetailedReconciliationSnapshot()
                    : PortfolioOperationsRules.Clone(
                        source.detailedReconciliation),
                operatingAlerts = migrateOperations
                    ? new List<PortfolioOperatingAlertSnapshot>()
                    : source.operatingAlerts?
                        .Select(PortfolioOperationsRules.Clone)
                        .ToList(),
                daysOperating = source.daysOperating,
                delegatedDaysOperating = legacyLocation
                    ? source.daysOperating
                    : source.delegatedDaysOperating,
                lifetimeGrossSalesCents = source.lifetimeGrossSalesCents,
                lifetimeCostOfGoodsSoldCents = source.lifetimeCostOfGoodsSoldCents,
                lifetimePayrollCents = source.lifetimePayrollCents,
                lifetimeRentCents = source.lifetimeRentCents,
                lifetimeInventoryPurchaseCents = source.lifetimeInventoryPurchaseCents,
                lifetimeDeliveryFeesCents = source.lifetimeDeliveryFeesCents,
                lifetimeBaseOperatingCostsCents = migrateOperations
                    ? 0
                    : source.lifetimeBaseOperatingCostsCents,
                lifetimeMaintenanceCostsCents = migrateOperations
                    ? 0
                    : source.lifetimeMaintenanceCostsCents,
                lifetimeLeaseAndSetupCents = source.lifetimeLeaseAndSetupCents,
                lifetimeCashChangeCents = source.lifetimeCashChangeCents,
                lifetimeOperatingProfitCents = source.lifetimeOperatingProfitCents,
                hasLastReport = source.hasLastReport,
                lastReport = source.hasLastReport
                    ? CloneReport(source.lastReport, migrateLegacyMerchandising)
                    : null
            };
        }

        private static bool IsSerializedNoReportPlaceholder(
            PortfolioLocationReportSnapshot report)
        {
            return report.day == 0 &&
                   string.IsNullOrEmpty(report.locationId) &&
                   report.demandUnits == 0 &&
                   report.unitsSold == 0 &&
                   report.lostDemandUnits == 0 &&
                   report.endingInventoryUnits == 0 &&
                   report.reorderedUnits == 0 &&
                   report.unitPriceCents == 0 &&
                   report.grossSalesCents == 0 &&
                   report.costOfGoodsSoldCents == 0 &&
                   report.payrollCents == 0 &&
                   report.rentCents == 0 &&
                   report.inventoryPurchaseCents == 0 &&
                   report.deliveryFeesCents == 0 &&
                   report.baseOperatingCostsCents == 0 &&
                   report.maintenanceCostsCents == 0 &&
                   report.operatingProfitCents == 0 &&
                   report.cashChangeCents == 0 &&
                   !report.isDetailedOperation &&
                   !report.hasExactMerchandiseSales &&
                   (report.merchandiseSales == null ||
                    report.merchandiseSales.Count == 0) &&
                   string.IsNullOrEmpty(report.primaryCause);
        }

        private static PortfolioLocationReportSnapshot CloneReport(
            PortfolioLocationReportSnapshot source,
            bool migrateLegacyMerchandising = false)
        {
            if (source == null)
            {
                return null;
            }

            return new PortfolioLocationReportSnapshot
            {
                day = source.day,
                locationId = source.locationId,
                demandUnits = source.demandUnits,
                unitsSold = source.unitsSold,
                lostDemandUnits = source.lostDemandUnits,
                endingInventoryUnits = source.endingInventoryUnits,
                reorderedUnits = source.reorderedUnits,
                unitPriceCents = source.unitPriceCents,
                grossSalesCents = source.grossSalesCents,
                costOfGoodsSoldCents = source.costOfGoodsSoldCents,
                payrollCents = source.payrollCents,
                rentCents = source.rentCents,
                inventoryPurchaseCents = source.inventoryPurchaseCents,
                deliveryFeesCents = source.deliveryFeesCents,
                baseOperatingCostsCents = source.baseOperatingCostsCents,
                maintenanceCostsCents = source.maintenanceCostsCents,
                operatingProfitCents = source.operatingProfitCents,
                cashChangeCents = source.cashChangeCents,
                serviceQuality = source.serviceQuality,
                customerSatisfaction = source.customerSatisfaction,
                productAvailabilityBasisPoints =
                    source.productAvailabilityBasisPoints,
                productMixBasisPoints = source.productMixBasisPoints,
                maintenanceCondition = source.maintenanceCondition,
                failurePressure = source.failurePressure,
                openAlertCount = source.openAlertCount,
                ownerAttentionAlertCount =
                    source.ownerAttentionAlertCount,
                primaryCause = source.primaryCause,
                isDetailedOperation = source.isDetailedOperation,
                hasExactMerchandiseSales = migrateLegacyMerchandising
                    ? false
                    : source.hasExactMerchandiseSales,
                merchandiseSales = migrateLegacyMerchandising
                    ? new List<MerchandiseSaleLineSnapshot>()
                    : source.merchandiseSales?
                        .Select(CloneMerchandiseSaleLine)
                        .ToList()
            };
        }

        private static MerchandisePriceSnapshot CloneMerchandisePrice(
            MerchandisePriceSnapshot source)
        {
            return source == null
                ? null
                : new MerchandisePriceSnapshot
                {
                    productId = source.productId,
                    referencePriceCents = source.referencePriceCents,
                    salePriceCents = source.salePriceCents
                };
        }

        private static ShelfMerchandiseAssignmentSnapshot CloneShelfAssignment(
            ShelfMerchandiseAssignmentSnapshot source)
        {
            return source == null
                ? null
                : new ShelfMerchandiseAssignmentSnapshot
                {
                    shelfFixtureId = source.shelfFixtureId,
                    inventoryLocationId = source.inventoryLocationId,
                    assignedProductId = source.assignedProductId,
                    customDisplayLabel = source.customDisplayLabel
                };
        }

        private static MerchandiseSaleLineSnapshot CloneMerchandiseSaleLine(
            MerchandiseSaleLineSnapshot source)
        {
            return source == null
                ? null
                : new MerchandiseSaleLineSnapshot
                {
                    productId = source.productId,
                    unitPriceCents = source.unitPriceCents,
                    quantityUnits = source.quantityUnits
                };
        }

        private static void SortCollections(PortfolioProgressionSnapshot snapshot)
        {
            snapshot.employees.Sort((left, right) => string.CompareOrdinal(
                left?.employeeId,
                right?.employeeId));
            snapshot.locations.Sort((left, right) => string.CompareOrdinal(
                left?.locationId,
                right?.locationId));
            foreach (PortfolioLocationSnapshot location in snapshot.locations)
            {
                location?.merchandisePrices?.Sort((left, right) =>
                    string.CompareOrdinal(left?.productId, right?.productId));
                location?.shelfMerchandiseAssignments?.Sort((left, right) =>
                    string.CompareOrdinal(
                        left?.shelfFixtureId,
                        right?.shelfFixtureId));
                location?.productInventory?.Sort((left, right) =>
                    string.CompareOrdinal(left?.productId, right?.productId));
                location?.detailedReconciliation?
                    .detailedProductInventoryBaseline?.Sort((left, right) =>
                        string.CompareOrdinal(
                            left?.productId,
                            right?.productId));
                location?.operatingAlerts?.Sort((left, right) =>
                    string.CompareOrdinal(left?.alertId, right?.alertId));
                location?.lastReport?.merchandiseSales?.Sort((left, right) =>
                {
                    int product = string.CompareOrdinal(
                        left?.productId,
                        right?.productId);
                    return product != 0
                        ? product
                        : (left?.unitPriceCents ?? 0).CompareTo(
                            right?.unitPriceCents ?? 0);
                });
            }
            snapshot.company?.brands?.Sort((left, right) =>
                string.CompareOrdinal(left?.brandId, right?.brandId));
            snapshot.company?.properties?.Sort((left, right) =>
                string.CompareOrdinal(left?.propertyId, right?.propertyId));
            foreach (PortfolioCommercialPropertySnapshot property in
                     snapshot.company?.properties ??
                     Enumerable.Empty<PortfolioCommercialPropertySnapshot>())
            {
                property?.commercialUnits?.Sort((left, right) =>
                    string.CompareOrdinal(
                        left?.commercialUnitId,
                        right?.commercialUnitId));
                foreach (PortfolioCommercialUnitSnapshot unit in
                         property?.commercialUnits ??
                         Enumerable.Empty<PortfolioCommercialUnitSnapshot>())
                {
                    unit?.improvements?.Sort((left, right) =>
                        string.CompareOrdinal(
                            left?.improvementId,
                            right?.improvementId));
                    unit?.generatedLayout?.modifications?.Sort((left, right) =>
                        string.CompareOrdinal(
                            left?.modificationId,
                            right?.modificationId));
                }
            }
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return Math.Min(maximum, Math.Max(minimum, value));
        }

        private static string FriendlyRole(PortfolioEmployeeRole role)
        {
            return role switch
            {
                PortfolioEmployeeRole.StockClerk => "stock clerk",
                PortfolioEmployeeRole.Manager => "manager",
                _ => "cashier"
            };
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
