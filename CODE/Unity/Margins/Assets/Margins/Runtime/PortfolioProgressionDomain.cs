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
        public long operatingProfitCents;
        public long cashChangeCents;
        public string primaryCause;
        public bool isDetailedOperation;
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
        public int daysOperating;
        public int delegatedDaysOperating;
        public long lifetimeGrossSalesCents;
        public long lifetimeCostOfGoodsSoldCents;
        public long lifetimePayrollCents;
        public long lifetimeRentCents;
        public long lifetimeInventoryPurchaseCents;
        public long lifetimeLeaseAndSetupCents;
        public long lifetimeCashChangeCents;
        public long lifetimeOperatingProfitCents;
        public bool hasLastReport;
        public PortfolioLocationReportSnapshot lastReport;
    }

    [Serializable]
    public sealed class PortfolioProgressionSnapshot
    {
        public const int CurrentVersion = 1;

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
                processedDetailedSessionId = null
            };
            initial.locations.Add(CreateLocation(
                PortfolioProgressionRules.FirstLocation));
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
            if (snapshot == null ||
                snapshot.version != PortfolioProgressionSnapshot.CurrentVersion)
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
                snapshot.locations.Count == 0 ||
                snapshot.locations.Count > 2)
            {
                error = "Portfolio snapshot contains invalid company totals or collections.";
                return false;
            }

            bool legacyDetailedPosting =
                snapshot.firstShiftCompleted &&
                !snapshot.detailedOperationInitialized &&
                FirstStoreIdentifier.IsValid(snapshot.processedDetailedSessionId) &&
                snapshot.reconciledDetailedGrossSalesCents == 0 &&
                snapshot.reconciledDetailedCostOfGoodsSoldCents == 0 &&
                snapshot.reconciledDetailedOperatingExpensesCents == 0 &&
                snapshot.reconciledDetailedPayrollCents == 0 &&
                snapshot.reconciledDetailedRentCents == 0 &&
                snapshot.reconciledDetailedInventoryAcquiredCostCents == 0 &&
                snapshot.reconciledDetailedUnitsSold == 0 &&
                snapshot.reconciledDetailedTransactionCount == 0;
            if ((!legacyDetailedPosting &&
                 snapshot.detailedOperationInitialized !=
                 !string.IsNullOrWhiteSpace(snapshot.processedDetailedSessionId)) ||
                (snapshot.detailedOperationInitialized &&
                 !FirstStoreIdentifier.IsValid(snapshot.processedDetailedSessionId)) ||
                snapshot.reconciledDetailedGrossSalesCents < 0 ||
                snapshot.reconciledDetailedCostOfGoodsSoldCents < 0 ||
                snapshot.reconciledDetailedOperatingExpensesCents < 0 ||
                snapshot.reconciledDetailedPayrollCents < 0 ||
                snapshot.reconciledDetailedRentCents < 0 ||
                snapshot.reconciledDetailedInventoryAcquiredCostCents < 0 ||
                snapshot.reconciledDetailedUnitsSold < 0 ||
                snapshot.reconciledDetailedTransactionCount < 0 ||
                snapshot.lifetimeCorporateCostsCents < 0 ||
                (!legacyDetailedPosting &&
                 snapshot.firstShiftCompleted !=
                 (snapshot.reconciledDetailedTransactionCount > 0)))
            {
                error = "Portfolio detailed-operation reconciliation fields disagree.";
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

            if (!snapshot.detailedOperationInitialized &&
                !legacyDetailedPosting &&
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
                 snapshot.locations[0].lifetimeOperatingProfitCents != 0))
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
                      location.lastReport.day != snapshot.currentDay)))
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
            unchanged = false;
            if (!FirstStoreIdentifier.IsValid(sessionId) ||
                totals == null ||
                !totals.IsValid ||
                inventoryAssetValueCents < 0)
            {
                error = "A valid detailed operation, inventory value, and reconciled totals are required.";
                return false;
            }

            if (remainingInventoryUnits < 0 ||
                remainingInventoryUnits >
                PortfolioProgressionRules.FirstLocation.InventoryCapacityUnits)
            {
                error = "Detailed first-store inventory cannot be reconciled to the company location.";
                return false;
            }

            if (state.detailedOperationInitialized &&
                !string.Equals(
                    state.processedDetailedSessionId,
                    sessionId,
                    StringComparison.Ordinal))
            {
                error = "A different detailed operating session is already authoritative.";
                return false;
            }

            PortfolioLocationSnapshot currentLocation = state.locations.First(location =>
                string.Equals(
                    location.locationId,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal));
            long assignedPayrollCents;
            try
            {
                assignedPayrollCents = state.employees
                    .Where(employee => string.Equals(
                        employee.assignedLocationId,
                        currentLocation.locationId,
                        StringComparison.Ordinal))
                    .Sum(employee => employee.dailyWageCents);
            }
            catch (OverflowException)
            {
                error = "Detailed assigned payroll overflowed integer-cent storage.";
                return false;
            }
            long detailedPayrollCents = Math.Min(
                assignedPayrollCents,
                totals.includedOperatingExpensesCents);
            long detailedRentCents = checked(
                totals.includedOperatingExpensesCents - detailedPayrollCents);

            long acquiredInventoryCost;
            long grossSalesDelta;
            long costOfGoodsDelta;
            long expenseDelta;
            long payrollDelta;
            long rentDelta;
            long purchaseDelta;
            long cashDelta;
            long profitDelta;
            try
            {
                acquiredInventoryCost = checked(
                    inventoryAssetValueCents + totals.costOfGoodsSoldCents);
                grossSalesDelta = checked(
                    totals.grossSalesCents - state.reconciledDetailedGrossSalesCents);
                costOfGoodsDelta = checked(
                    totals.costOfGoodsSoldCents -
                    state.reconciledDetailedCostOfGoodsSoldCents);
                expenseDelta = checked(
                    totals.includedOperatingExpensesCents -
                    state.reconciledDetailedOperatingExpensesCents);
                payrollDelta = checked(
                    detailedPayrollCents - state.reconciledDetailedPayrollCents);
                rentDelta = checked(
                    detailedRentCents - state.reconciledDetailedRentCents);
                purchaseDelta = checked(
                    acquiredInventoryCost -
                    state.reconciledDetailedInventoryAcquiredCostCents);
                cashDelta = checked(grossSalesDelta - expenseDelta - purchaseDelta);
                profitDelta = checked(grossSalesDelta - costOfGoodsDelta - expenseDelta);
            }
            catch (OverflowException)
            {
                error = "Detailed operation reconciliation overflowed integer-cent storage.";
                return false;
            }

            if (grossSalesDelta < 0 ||
                costOfGoodsDelta < 0 ||
                expenseDelta < 0 ||
                payrollDelta < 0 ||
                rentDelta < 0 ||
                purchaseDelta < 0 ||
                totals.unitsSold < state.reconciledDetailedUnitsSold ||
                totals.transactionCount < state.reconciledDetailedTransactionCount)
            {
                error = "Detailed operation totals moved backward and cannot be reconciled without a new session.";
                return false;
            }

            bool noFinancialChange =
                state.detailedOperationInitialized &&
                grossSalesDelta == 0 &&
                costOfGoodsDelta == 0 &&
                expenseDelta == 0 &&
                payrollDelta == 0 &&
                rentDelta == 0 &&
                purchaseDelta == 0 &&
                totals.unitsSold == state.reconciledDetailedUnitsSold &&
                totals.transactionCount == state.reconciledDetailedTransactionCount;
            if (noFinancialChange && currentLocation.inventoryUnits == remainingInventoryUnits)
            {
                unchanged = true;
                error = null;
                return true;
            }

            PortfolioProgressionSnapshot candidate = Clone(state);
            PortfolioLocationSnapshot location = candidate.locations.First(value =>
                string.Equals(
                    value.locationId,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal));
            bool firstCompletedSale = !candidate.firstShiftCompleted &&
                                      totals.transactionCount > 0;
            bool migratingLegacyPosting =
                candidate.firstShiftCompleted &&
                !candidate.detailedOperationInitialized;
            if (migratingLegacyPosting)
            {
                // The prior implementation posted accounting contribution to cash.
                // Convert it once to cash basis: received inventory is a purchase
                // outflow, while COGS is profit recognition rather than a second
                // cash deduction.
                cashDelta = checked(
                    totals.costOfGoodsSoldCents - acquiredInventoryCost);
            }
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

            candidate.detailedOperationInitialized = true;
            candidate.processedDetailedSessionId = sessionId;
            candidate.reconciledDetailedGrossSalesCents = totals.grossSalesCents;
            candidate.reconciledDetailedCostOfGoodsSoldCents =
                totals.costOfGoodsSoldCents;
            candidate.reconciledDetailedOperatingExpensesCents =
                totals.includedOperatingExpensesCents;
            candidate.reconciledDetailedPayrollCents = detailedPayrollCents;
            candidate.reconciledDetailedRentCents = detailedRentCents;
            candidate.reconciledDetailedInventoryAcquiredCostCents =
                acquiredInventoryCost;
            candidate.reconciledDetailedUnitsSold = totals.unitsSold;
            candidate.reconciledDetailedTransactionCount = totals.transactionCount;
            candidate.firstShiftCompleted = totals.transactionCount > 0;
            if (firstCompletedSale)
            {
                candidate.companyReputation = Clamp(
                    candidate.companyReputation + 2,
                    0,
                    100);
            }

            location.inventoryUnits = remainingInventoryUnits;
            location.daysOperating = Math.Max(location.daysOperating, 1);
            location.lastReport = new PortfolioLocationReportSnapshot
            {
                day = candidate.currentDay,
                locationId = location.locationId,
                demandUnits = totals.unitsSold,
                unitsSold = totals.unitsSold,
                lostDemandUnits = 0,
                endingInventoryUnits = remainingInventoryUnits,
                reorderedUnits = 0,
                unitPriceCents = 0,
                grossSalesCents = totals.grossSalesCents,
                costOfGoodsSoldCents = totals.costOfGoodsSoldCents,
                payrollCents = detailedPayrollCents,
                rentCents = detailedRentCents,
                inventoryPurchaseCents = acquiredInventoryCost,
                operatingProfitCents = totals.contributionAfterCostOfGoodsCents,
                cashChangeCents = checked(
                    totals.grossSalesCents -
                    totals.includedOperatingExpensesCents -
                    acquiredInventoryCost),
                primaryCause = totals.transactionCount == 0
                    ? "The store is operating; received inventory and occupancy costs are live."
                    : "Hands-on sales, historical COGS, cash, and remaining physical inventory reconcile live.",
                isDetailedOperation = true
            };
            location.hasLastReport = true;
            return TryCommit(candidate, out error);
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
                lastTrainingDay = 0
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
            candidate.locations.First(location => string.Equals(
                    location.locationId,
                    locationId,
                    StringComparison.Ordinal))
                .pricingPolicy = policy;
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

        public bool TryLeaseLocation(string locationId, out string error)
        {
            if (!state.firstShiftCompleted)
            {
                error = "Complete the hands-on first shift before expanding.";
                return false;
            }

            if (state.locations.Count >= 2)
            {
                error = "The current production build supports two active locations.";
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

            foreach (PortfolioLocationSnapshot location in state.locations
                         .OrderBy(value => value.locationId, StringComparer.Ordinal))
            {
                if (!HasRole(state, location.locationId, PortfolioEmployeeRole.Cashier))
                {
                    blocker = $"{location.displayName} needs a cashier.";
                    return false;
                }
                if (!HasRole(state, location.locationId, PortfolioEmployeeRole.StockClerk))
                {
                    blocker = $"{location.displayName} needs a stock clerk.";
                    return false;
                }
                if (!HasRole(state, location.locationId, PortfolioEmployeeRole.Manager))
                {
                    blocker = $"{location.displayName} needs a manager to operate while you are absent.";
                    return false;
                }
            }

            long fixedCosts = state.locations.Sum(location => location.dailyRentCents) +
                              state.employees.Sum(employee => employee.dailyWageCents);
            if (state.cashCents <
                fixedCosts + PortfolioProgressionRules.MinimumCashReserveCents)
            {
                blocker =
                    $"Cash must cover {FormatCents(fixedCosts)} in payroll and rent plus the protected reserve.";
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
            long portfolioFixedCosts =
                candidate.locations.Sum(location => location.dailyRentCents) +
                candidate.employees.Sum(employee => employee.dailyWageCents);
            candidate.cashCents -= portfolioFixedCosts;
            foreach (PortfolioLocationSnapshot location in candidate.locations
                         .OrderBy(value => value.locationId, StringComparer.Ordinal))
            {
                if (!TrySimulateLocationDay(
                        candidate,
                        location,
                        simulatedDay,
                        out error))
                {
                    return false;
                }
            }

            candidate.currentDay = simulatedDay;
            candidate.companyReputation = Clamp(
                (int)Math.Round(candidate.locations.Average(location =>
                    location.reputation)),
                0,
                100);
            SortCollections(candidate);
            return TryCommit(candidate, out error);
        }

        private static bool TrySimulateLocationDay(
            PortfolioProgressionSnapshot candidate,
            PortfolioLocationSnapshot location,
            int simulatedDay,
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
                .Where(employee => string.Equals(
                    employee.assignedLocationId,
                    location.locationId,
                    StringComparison.Ordinal))
                .OrderBy(employee => employee.employeeId, StringComparer.Ordinal)
                .ToList();
            PortfolioEmployeeSnapshot cashier = assigned.First(employee =>
                employee.role == PortfolioEmployeeRole.Cashier);
            PortfolioEmployeeSnapshot stocker = assigned.First(employee =>
                employee.role == PortfolioEmployeeRole.StockClerk);
            PortfolioEmployeeSnapshot manager = assigned.First(employee =>
                employee.role == PortfolioEmployeeRole.Manager);

            long payroll = assigned.Sum(employee => employee.dailyWageCents);
            long fixedCosts = payroll + location.dailyRentCents;

            ResolveReorderPolicy(
                location,
                out int reorderPoint,
                out int reorderTarget);
            int reorderedUnits = 0;
            long inventoryPurchase = 0;
            if (location.inventoryUnits <= reorderPoint)
            {
                int desiredUnits = Math.Max(0, reorderTarget - location.inventoryUnits);
                long spendable = Math.Max(
                    0,
                    candidate.cashCents -
                    PortfolioProgressionRules.MinimumCashReserveCents);
                int affordableUnits =
                    simulation.UnitEconomy.VariableUnitCostCents == 0
                        ? desiredUnits
                        : (int)Math.Min(
                            int.MaxValue,
                            spendable /
                            simulation.UnitEconomy.VariableUnitCostCents);
                reorderedUnits = Math.Min(desiredUnits, affordableUnits);
                inventoryPurchase = checked(
                    reorderedUnits *
                    simulation.UnitEconomy.VariableUnitCostCents);
                candidate.cashCents -= inventoryPurchase;
                location.inventoryUnits += reorderedUnits;
            }
            int inventoryAvailableAtOpen = location.inventoryUnits;

            long unitPrice = location.pricingPolicy switch
            {
                PortfolioPricingPolicy.Value =>
                    simulation.UnitEconomy.ValuePriceCents,
                PortfolioPricingPolicy.Premium =>
                    simulation.UnitEconomy.PremiumPriceCents,
                _ => simulation.UnitEconomy.BalancedPriceCents
            };
            int priceDemandAdjustment = location.pricingPolicy switch
            {
                PortfolioPricingPolicy.Value =>
                    simulation.UnitEconomy.ValueDemandAdjustmentUnits,
                PortfolioPricingPolicy.Premium =>
                    simulation.UnitEconomy.PremiumDemandAdjustmentUnits,
                _ => 0
            };
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
            int demand = Math.Max(
                0,
                location.baseDemandUnits +
                priceDemandAdjustment +
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

            int unitsSold = Math.Min(
                demand,
                Math.Min(
                    serviceCapacity,
                    Math.Min(stockedAvailability, location.inventoryUnits)));
            int lostDemand = demand - unitsSold;
            long grossSales;
            long costOfGoodsSold;
            long operatingProfit;
            try
            {
                grossSales = checked(unitPrice * unitsSold);
                costOfGoodsSold = checked(
                    simulation.UnitEconomy.VariableUnitCostCents * unitsSold);
                operatingProfit = checked(
                    grossSales - costOfGoodsSold - payroll - location.dailyRentCents);
                candidate.cashCents = checked(candidate.cashCents + grossSales);
            }
            catch (OverflowException)
            {
                error = "Delegated sales overflowed integer-cent storage.";
                return false;
            }

            location.inventoryUnits -= unitsSold;

            string primaryCause = "Demand served within current capacity.";
            if (lostDemand > 0)
            {
                if (unitsSold >= inventoryAvailableAtOpen ||
                    unitsSold >= stockedAvailability)
                {
                    primaryCause = "Shelf availability limited sales; raise the reorder buffer or inventory focus.";
                }
                else if (unitsSold >= serviceCapacity)
                {
                    primaryCause = "Checkout capacity limited sales; train staff or emphasize service.";
                }
                else if (location.pricingPolicy == PortfolioPricingPolicy.Premium)
                {
                    primaryCause = "Premium pricing reduced traffic but increased unit margin.";
                }
                else
                {
                    primaryCause = "Demand exceeded the current operating system.";
                }
            }
            else if (location.pricingPolicy == PortfolioPricingPolicy.Value)
            {
                primaryCause = "Value pricing increased traffic and consumed inventory faster.";
            }

            int serviceRatioPercent = demand <= 0
                ? 100
                : unitsSold * 100 / Math.Max(1, demand);
            int reputationChange = serviceRatioPercent >= 92
                ? 2
                : serviceRatioPercent >= 75
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
                location.lifetimeRentCents + location.dailyRentCents);
            location.lifetimeInventoryPurchaseCents = checked(
                location.lifetimeInventoryPurchaseCents + inventoryPurchase);
            location.lifetimeOperatingProfitCents = checked(
                location.lifetimeOperatingProfitCents + operatingProfit);
            location.lifetimeCashChangeCents = checked(
                location.lifetimeCashChangeCents +
                grossSales - fixedCosts - inventoryPurchase);
            location.lastReport = new PortfolioLocationReportSnapshot
            {
                day = simulatedDay,
                locationId = location.locationId,
                demandUnits = demand,
                unitsSold = unitsSold,
                lostDemandUnits = lostDemand,
                endingInventoryUnits = location.inventoryUnits,
                reorderedUnits = reorderedUnits,
                unitPriceCents = unitPrice,
                grossSalesCents = grossSales,
                costOfGoodsSoldCents = costOfGoodsSold,
                payrollCents = payroll,
                rentCents = location.dailyRentCents,
                inventoryPurchaseCents = inventoryPurchase,
                operatingProfitCents = operatingProfit,
                cashChangeCents = grossSales - fixedCosts - inventoryPurchase,
                primaryCause = primaryCause,
                isDetailedOperation = false
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
                location.lifetimeLeaseAndSetupCents < 0 ||
                !Enum.IsDefined(typeof(PortfolioPricingPolicy), location.pricingPolicy) ||
                !Enum.IsDefined(typeof(PortfolioReorderPolicy), location.reorderPolicy))
            {
                error = "Portfolio location state is invalid or contradicts its market definition.";
                return false;
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
            long expectedGrossSales;
            long expectedCostOfGoods;
            long expectedInventoryPurchase;
            long expectedOperatingProfit;
            long expectedCashChange;
            try
            {
                expectedGrossSales = checked(
                    report.unitPriceCents * report.unitsSold);
                expectedCostOfGoods = checked(
                    aggregateUnitCost * report.unitsSold);
                expectedInventoryPurchase = checked(
                    aggregateUnitCost * report.reorderedUnits);
                expectedOperatingProfit = checked(
                    report.grossSalesCents -
                    report.costOfGoodsSoldCents -
                    report.payrollCents -
                    report.rentCents);
                expectedCashChange = checked(
                    report.grossSalesCents -
                    report.payrollCents -
                    report.rentCents -
                    report.inventoryPurchaseCents);
            }
            catch (OverflowException)
            {
                error = "Portfolio location report arithmetic overflowed integer-cent storage.";
                return false;
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
                report.operatingProfitCents == expectedOperatingProfit &&
                report.cashChangeCents == expectedCashChange &&
                !string.IsNullOrWhiteSpace(report.primaryCause);
            bool modeValid = report.isDetailedOperation
                ? report.unitPriceCents == 0 &&
                  report.demandUnits == report.unitsSold &&
                  report.lostDemandUnits == 0 &&
                  report.reorderedUnits == 0 &&
                  report.grossSalesCents >= 0 &&
                  report.costOfGoodsSoldCents >= 0 &&
                  report.rentCents >= 0 &&
                  report.inventoryPurchaseCents >= 0
                : report.unitPriceCents > 0 &&
                  report.grossSalesCents == expectedGrossSales &&
                  report.costOfGoodsSoldCents == expectedCostOfGoods &&
                  report.rentCents == location.dailyRentCents &&
                  report.inventoryPurchaseCents == expectedInventoryPurchase;
            bool valid = commonValid && modeValid;
            if (!valid)
            {
                error = "Portfolio location report does not reconcile.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryValidateEmployee(
            PortfolioEmployeeSnapshot employee,
            ISet<string> locationIds,
            int currentDay,
            out string error)
        {
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
                !locationIds.Contains(employee.assignedLocationId))
            {
                error = "Portfolio employee state is invalid.";
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
            return new PortfolioLocationSnapshot
            {
                locationId = definition.LocationId,
                displayName = definition.DisplayName,
                districtName = definition.DistrictName,
                marketSummary = definition.MarketSummary,
                businessTypeId = definition.BusinessTypeId,
                operatingModel = definition.OperatingModel,
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
                daysOperating = 0,
                delegatedDaysOperating = 0,
                lifetimeGrossSalesCents = 0,
                lifetimeCostOfGoodsSoldCents = 0,
                lifetimePayrollCents = 0,
                lifetimeRentCents = 0,
                lifetimeInventoryPurchaseCents = 0,
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

        private static PortfolioProgressionSnapshot Clone(
            PortfolioProgressionSnapshot source)
        {
            PortfolioProgressionSnapshot clone = new()
            {
                version = source.version,
                currentDay = source.currentDay,
                cashCents = source.cashCents,
                companyReputation = source.companyReputation,
                firstShiftCompleted = source.firstShiftCompleted,
                detailedOperationInitialized = source.detailedOperationInitialized,
                processedDetailedSessionId = source.processedDetailedSessionId,
                reconciledDetailedGrossSalesCents = source.reconciledDetailedGrossSalesCents,
                reconciledDetailedCostOfGoodsSoldCents = source.reconciledDetailedCostOfGoodsSoldCents,
                reconciledDetailedOperatingExpensesCents = source.reconciledDetailedOperatingExpensesCents,
                reconciledDetailedPayrollCents = source.reconciledDetailedPayrollCents,
                reconciledDetailedRentCents = source.reconciledDetailedRentCents,
                reconciledDetailedInventoryAcquiredCostCents = source.reconciledDetailedInventoryAcquiredCostCents,
                reconciledDetailedUnitsSold = source.reconciledDetailedUnitsSold,
                reconciledDetailedTransactionCount = source.reconciledDetailedTransactionCount,
                lifetimeCorporateCostsCents = source.lifetimeCorporateCostsCents,
                employees = source.employees?
                    .Select(CloneEmployee)
                    .ToList() ?? new List<PortfolioEmployeeSnapshot>(),
                locations = source.locations?
                    .Select(CloneLocation)
                    .ToList() ?? new List<PortfolioLocationSnapshot>()
            };
            if (clone.detailedOperationInitialized &&
                clone.reconciledDetailedOperatingExpensesCents > 0 &&
                clone.reconciledDetailedPayrollCents == 0 &&
                clone.reconciledDetailedRentCents == 0)
            {
                clone.reconciledDetailedRentCents =
                    clone.reconciledDetailedOperatingExpensesCents;
            }
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
                lastTrainingDay = source.lastTrainingDay
            };
        }

        private static PortfolioLocationSnapshot CloneLocation(
            PortfolioLocationSnapshot source)
        {
            PortfolioProgressionRules.TryGetLocationDefinition(
                source.locationId,
                out PortfolioLocationDefinition definition);
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
                daysOperating = source.daysOperating,
                delegatedDaysOperating = legacyLocation
                    ? source.daysOperating
                    : source.delegatedDaysOperating,
                lifetimeGrossSalesCents = source.lifetimeGrossSalesCents,
                lifetimeCostOfGoodsSoldCents = source.lifetimeCostOfGoodsSoldCents,
                lifetimePayrollCents = source.lifetimePayrollCents,
                lifetimeRentCents = source.lifetimeRentCents,
                lifetimeInventoryPurchaseCents = source.lifetimeInventoryPurchaseCents,
                lifetimeLeaseAndSetupCents = source.lifetimeLeaseAndSetupCents,
                lifetimeCashChangeCents = source.lifetimeCashChangeCents,
                lifetimeOperatingProfitCents = source.lifetimeOperatingProfitCents,
                hasLastReport = source.hasLastReport,
                lastReport = source.hasLastReport
                    ? CloneReport(source.lastReport)
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
                   report.operatingProfitCents == 0 &&
                   report.cashChangeCents == 0 &&
                   !report.isDetailedOperation &&
                   string.IsNullOrEmpty(report.primaryCause);
        }

        private static PortfolioLocationReportSnapshot CloneReport(
            PortfolioLocationReportSnapshot source)
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
                operatingProfitCents = source.operatingProfitCents,
                cashChangeCents = source.cashChangeCents,
                primaryCause = source.primaryCause,
                isDetailedOperation = source.isDetailedOperation
            };
        }

        private static void SortCollections(PortfolioProgressionSnapshot snapshot)
        {
            snapshot.employees.Sort((left, right) => string.CompareOrdinal(
                left.employeeId,
                right.employeeId));
            snapshot.locations.Sort((left, right) => string.CompareOrdinal(
                left.locationId,
                right.locationId));
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
