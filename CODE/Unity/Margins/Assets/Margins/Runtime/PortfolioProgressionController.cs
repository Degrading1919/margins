using System;
using System.Linq;
using UnityEngine;

namespace Margins
{
    /// <summary>
    /// Connects the first in-world shift to persistent company management and
    /// presents the same domain used by delegated simulation and disk saves.
    /// </summary>
    public sealed class PortfolioProgressionController : MonoBehaviour
    {
        private enum DeskPage
        {
            Overview,
            People,
            Locations,
            Reports
        }

        [SerializeField] private FirstPersonController firstPersonController;
        [SerializeField] private StoreOperatingController firstStore;
        [SerializeField] private FirstStoreInventoryComponent firstStoreInventory;

        private PortfolioProgression progression;
        private DeskPage page;
        private string selectedLocationId = PortfolioProgressionRules.FirstLocationId;
        private string lastAction = "Complete the hands-on first shift to unlock company management.";
        private bool lastActionSucceeded = true;
        private bool hasOpenedDeskAfterFirstShift;
        private Vector2 peopleScroll;
        private Vector2 reportScroll;

        public PortfolioProgression Progression => progression;
        public bool IsInitialized => progression != null;
        public bool OwnsManagementDesk =>
            progression != null &&
            firstPersonController != null &&
            !GamePauseMenuController.IsAnyMenuOpen &&
            !firstPersonController.IsGameplayMode;
        public string LastAction => lastAction;
        public string SelectedLocationId => selectedLocationId;

        private void Awake()
        {
            progression = PortfolioProgression.CreateInitial();
        }

        private void Start()
        {
            if (!TryValidateConfiguration(out string error))
            {
                Debug.LogError(error, this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (!TrySynchronizeLivePayroll(out string payrollError))
            {
                Record(payrollError, false);
            }
            TrySynchronizeDetailedShift(out _);
            if (!hasOpenedDeskAfterFirstShift &&
                progression != null &&
                progression.FirstShiftCompleted &&
                firstPersonController != null)
            {
                hasOpenedDeskAfterFirstShift = true;
                Record(
                    "Company management is now available from Tab without ending store operations.",
                    true);
            }
        }

        public bool TrySynchronizeLivePayroll(out string error)
        {
            if (progression == null || firstStore == null)
            {
                error = "Company or first-store payroll state is unavailable.";
                return false;
            }

            long payrollCents;
            try
            {
                payrollCents = progression.Employees
                    .Where(employee => string.Equals(
                        employee.assignedLocationId,
                        PortfolioProgressionRules.FirstLocationId,
                        StringComparison.Ordinal))
                    .Sum(employee => employee.dailyWageCents);
            }
            catch (OverflowException)
            {
                error = "Assigned payroll exceeds supported cent storage.";
                return false;
            }

            return firstStore.TrySetLivePayrollCents(payrollCents, out error);
        }

        public bool TryValidateConfiguration(out string error)
        {
            error = null;
            if (firstPersonController == null ||
                firstStore == null ||
                firstStoreInventory == null ||
                !firstStoreInventory.TryInitialize(out error))
            {
                error ??= "Portfolio progression requires explicit player, first-store, and inventory references.";
                return false;
            }

            if (progression == null ||
                !PortfolioProgression.TryValidateSnapshot(
                    progression.CreateSnapshot(),
                    out error))
            {
                error = $"Portfolio progression is not initialized: {error}";
                return false;
            }

            error = null;
            return true;
        }

        public bool TrySynchronizeDetailedShift(out string error)
        {
            if (progression == null || firstStore == null)
            {
                error = "Portfolio progression or first-store state is unavailable.";
                return false;
            }

            PortfolioLocationSnapshot firstLocation = progression.Locations.FirstOrDefault(
                location => string.Equals(
                    location.locationId,
                    PortfolioProgressionRules.FirstLocationId,
                    StringComparison.Ordinal));
            if (firstLocation != null && firstLocation.delegatedDaysOperating > 0)
            {
                // Once an off-site day has advanced, its aggregate inventory and
                // report are authoritative. Do not repost the still-loaded detailed
                // scene on top of delegated sales, purchasing, payroll, or rent.
                error = null;
                return true;
            }

            StoreSessionTotals totals = firstStore.CurrentTotals;
            if (totals == null)
            {
                error = null;
                return true;
            }

            if (!TryGetDetailedInventory(
                    firstStoreInventory.Inventory?.CreateSnapshot(),
                    firstStore.Checkout.ProductUnitCostsCents,
                    out int remainingInventoryUnits,
                    out long inventoryAssetValueCents,
                    out error))
            {
                return false;
            }

            bool success = progression.TryReconcileDetailedOperation(
                firstStore.StableSessionId,
                totals,
                remainingInventoryUnits,
                inventoryAssetValueCents,
                out bool unchanged,
                out error);
            if (success && !unchanged && totals.transactionCount > 0)
            {
                Record(
                    $"Live operation reconciled: cash {FormatCents(progression.CashCents)}, " +
                    $"sales {FormatCents(totals.grossSalesCents)}, COGS {FormatCents(totals.costOfGoodsSoldCents)}.",
                    true);
            }
            return success;
        }

        public bool TryCaptureSnapshot(
            out PortfolioProgressionSnapshot snapshot,
            out string error)
        {
            snapshot = null;
            if (progression == null)
            {
                error = "Portfolio progression is not initialized.";
                return false;
            }

            snapshot = progression.CreateSnapshot();
            return PortfolioProgression.TryValidateSnapshot(snapshot, out error);
        }

        public bool TryValidateSnapshot(
            PortfolioProgressionSnapshot snapshot,
            out string error)
        {
            return PortfolioProgression.TryValidateSnapshot(snapshot, out error);
        }

        public bool TryRestoreSnapshot(
            PortfolioProgressionSnapshot snapshot,
            out string error)
        {
            if (!PortfolioProgression.TryRestore(
                    snapshot,
                    out PortfolioProgression restored,
                    out error))
            {
                return false;
            }

            progression = restored;
            if (!progression.Locations.Any(location => string.Equals(
                    location.locationId,
                    selectedLocationId,
                    StringComparison.Ordinal)))
            {
                selectedLocationId = PortfolioProgressionRules.FirstLocationId;
            }
            Record("Restored company, employees, locations, policies, and reports.", true);
            return true;
        }

        public bool TryCreateLegacyMigrationSnapshot(
            FirstStoreSnapshot firstStoreSnapshot,
            out PortfolioProgressionSnapshot migrated,
            out string error)
        {
            PortfolioProgression migration = PortfolioProgression.CreateInitial();
            StoreOperatingSnapshot operating = firstStoreSnapshot?.storeOperating;
            StoreSessionTotals migratedTotals = operating?.hasResult == true
                ? operating.totals
                : null;
            if (migratedTotals == null && firstStoreSnapshot != null)
            {
                if (!CompletedTransactionLedger.TryRestore(
                        firstStoreSnapshot.transactionLedger,
                        out CompletedTransactionLedger ledger,
                        out error) ||
                    !StoreSessionTotals.TryCreateFromLedger(
                        ledger,
                        firstStore.IncludedOperatingExpensesCents,
                        out migratedTotals,
                        out error))
                {
                    migrated = null;
                    return false;
                }
            }
            if (migratedTotals != null)
            {
                if (!TryGetDetailedInventory(
                        firstStoreSnapshot.inventory,
                        firstStore.Checkout.ProductUnitCostsCents,
                        out int remainingInventoryUnits,
                        out long inventoryAssetValueCents,
                        out error))
                {
                    migrated = null;
                    return false;
                }
                if (!migration.TryReconcileDetailedOperation(
                        operating.sessionId,
                        migratedTotals,
                        remainingInventoryUnits,
                        inventoryAssetValueCents,
                        out _,
                        out error))
                {
                    migrated = null;
                    return false;
                }
            }

            migrated = migration.CreateSnapshot();
            error = null;
            return true;
        }

        private static bool TryGetDetailedInventory(
            FirstStoreInventorySnapshot inventory,
            System.Collections.Generic.IReadOnlyDictionary<string, int> unitCostsCents,
            out int totalUnits,
            out long inventoryAssetValueCents,
            out string error)
        {
            totalUnits = 0;
            inventoryAssetValueCents = 0;
            if (inventory?.locations == null || unitCostsCents == null)
            {
                error = "Detailed first-store inventory is missing.";
                return false;
            }

            try
            {
                foreach (InventoryLocationSnapshot location in inventory.locations)
                {
                    if (location?.quantities == null)
                    {
                        error = "Detailed first-store inventory contains a missing location quantity list.";
                        return false;
                    }
                    foreach (InventoryQuantitySnapshot quantity in location.quantities)
                    {
                        if (quantity == null || quantity.quantityUnits < 0)
                        {
                            error = "Detailed first-store inventory contains an invalid quantity.";
                            return false;
                        }
                        if (!unitCostsCents.TryGetValue(
                                quantity.productId,
                                out int unitCostCents) ||
                            unitCostCents < 0)
                        {
                            error =
                                $"Detailed inventory product '{quantity.productId}' has no authoritative unit cost.";
                            return false;
                        }
                        totalUnits = checked(totalUnits + quantity.quantityUnits);
                        inventoryAssetValueCents = checked(
                            inventoryAssetValueCents +
                            (long)quantity.quantityUnits * unitCostCents);
                    }
                }
            }
            catch (OverflowException)
            {
                error = "Detailed first-store inventory total overflowed integer storage.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryHireCandidate(
            string employeeId,
            string locationId,
            out string error)
        {
            bool success = progression.TryHireCandidate(
                employeeId,
                locationId,
                out error);
            RecordResult(
                success,
                success && PortfolioProgressionRules.TryGetCandidate(
                    employeeId,
                    out PortfolioCandidateDefinition candidate)
                    ? $"Hired {candidate.DisplayName} for {LocationName(locationId)}."
                    : error);
            return success;
        }

        public bool TryAdvanceDelegatedDay(out string error)
        {
            bool success = progression.TryAdvanceDelegatedDay(out error);
            RecordResult(
                success,
                success
                    ? $"Delegated day {progression.CurrentDay} completed and all location reports posted."
                    : error);
            return success;
        }

        public bool TryLeaseLocation(string locationId, out string error)
        {
            bool success = progression.TryLeaseLocation(locationId, out error);
            if (success)
            {
                selectedLocationId = locationId;
                page = DeskPage.People;
            }
            RecordResult(
                success,
                success ? $"Leased and stocked {LocationName(locationId)}." : error);
            return success;
        }

        private void OnGUI()
        {
            if (!OwnsManagementDesk)
            {
                return;
            }

            float scale = Mathf.Max(
                0.7f,
                Mathf.Min(Screen.width / 1600f, Screen.height / 900f));
            Matrix4x4 priorMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            GUI.Box(new Rect(0f, 0f, width, height), "");
            DrawHeader(width);
            DrawNavigation(width);
            GUILayout.BeginArea(new Rect(34f, 150f, width - 68f, height - 238f));
            switch (page)
            {
                case DeskPage.People:
                    DrawPeople();
                    break;
                case DeskPage.Locations:
                    DrawLocations();
                    break;
                case DeskPage.Reports:
                    DrawReports();
                    break;
                default:
                    DrawOverview();
                    break;
            }
            GUILayout.EndArea();
            DrawFooter(width, height);
            GUI.matrix = priorMatrix;
        }

        private void DrawHeader(float width)
        {
            PortfolioProgressionSnapshot snapshot = progression.CreateSnapshot();
            GUI.Box(new Rect(24f, 18f, width - 48f, 75f), "");
            GUI.Label(
                new Rect(45f, 29f, 540f, 30f),
                "MILE 7 HOLDINGS  /  COMPANY DESK");
            GUI.Label(
                new Rect(45f, 57f, 760f, 24f),
                $"DAY {snapshot.currentDay}   CASH {FormatCents(snapshot.cashCents)}   " +
                $"REPUTATION {snapshot.companyReputation}/100   " +
                $"LOCATIONS {snapshot.locations.Count}/2");
            GUI.Label(
                new Rect(width - 470f, 38f, 430f, 28f),
                "TAB return to store  |  F5 save  |  F9 load");
        }

        private void DrawNavigation(float width)
        {
            float buttonWidth = (width - 68f) / 4f;
            float x = 34f;
            if (GUI.Button(new Rect(x, 105f, buttonWidth, 34f), "OVERVIEW"))
            {
                page = DeskPage.Overview;
            }
            x += buttonWidth;
            if (GUI.Button(new Rect(x, 105f, buttonWidth, 34f), "PEOPLE"))
            {
                page = DeskPage.People;
            }
            x += buttonWidth;
            if (GUI.Button(new Rect(x, 105f, buttonWidth, 34f), "LOCATIONS"))
            {
                page = DeskPage.Locations;
            }
            x += buttonWidth;
            if (GUI.Button(new Rect(x, 105f, buttonWidth, 34f), "REPORTS"))
            {
                page = DeskPage.Reports;
            }
        }

        private void DrawOverview()
        {
            PortfolioProgressionSnapshot snapshot = progression.CreateSnapshot();
            GUILayout.Label("OWNER TO PORTFOLIO PROGRESSION");
            GUILayout.Space(6f);
            DrawCheck(true, "Hands-on first shift completed and posted once to company cash");
            bool firstStaffed = IsFullyStaffed(
                snapshot,
                PortfolioProgressionRules.FirstLocationId);
            DrawCheck(firstStaffed, "First store has cashier, stock clerk, and manager");
            PortfolioLocationSnapshot first = snapshot.locations.First(location =>
                location.locationId == PortfolioProgressionRules.FirstLocationId);
            DrawCheck(
                first.delegatedDaysOperating > 0,
                "Manager has proven at least one delegated operating day");
            DrawCheck(snapshot.locations.Count > 1, "Second market selected and lease signed");
            bool portfolioStaffed = snapshot.locations.All(location =>
                IsFullyStaffed(snapshot, location.locationId));
            DrawCheck(
                snapshot.locations.Count > 1 && portfolioStaffed,
                "Both locations can operate without the owner present");

            GUILayout.Space(16f);
            GUILayout.Label("NEXT COMPANY ACTION");
            if (progression.CanAdvanceDelegatedDay(out string blocker))
            {
                if (GUILayout.Button(
                        $"RUN DELEGATED DAY {snapshot.currentDay + 1}",
                        GUILayout.Height(48f)))
                {
                    TryAdvanceDelegatedDay(out _);
                }
                GUILayout.Label(
                    "Managers will execute each location's price and reorder policy. Sales, COGS, payroll, rent, purchases, skills, satisfaction, reputation, and cash all resolve together.");
            }
            else
            {
                GUILayout.Box($"BLOCKED: {blocker}", GUILayout.Height(46f));
                if (!firstStaffed)
                {
                    GUILayout.Label("Open PEOPLE and hire one worker in each role for Mile 7 Market.");
                }
                else if (snapshot.locations.Count == 1 && first.daysOperating > 0)
                {
                    GUILayout.Label("Open LOCATIONS to compare the two expansion markets.");
                }
            }

            if (firstStore.State == StoreOperatingState.ClosedWithResultPending)
            {
                GUILayout.Space(12f);
                if (GUILayout.Button("ACKNOWLEDGE FIRST-SHIFT RESULT", GUILayout.Height(34f)))
                {
                    bool success = firstStore.TryAcknowledgeResult(out string error);
                    RecordResult(
                        success,
                        success ? "First-shift result acknowledged; management remains available from Tab." : error);
                }
            }

            GUILayout.Space(16f);
            DrawPortfolioSummary(snapshot);
        }

        private void DrawPeople()
        {
            PortfolioProgressionSnapshot snapshot = progression.CreateSnapshot();
            DrawLocationSelector(snapshot);
            PortfolioLocationSnapshot selected = SelectedLocation(snapshot);
            GUILayout.Space(8f);
            GUILayout.Label($"TEAM  /  {selected.displayName.ToUpperInvariant()}");
            peopleScroll = GUILayout.BeginScrollView(peopleScroll);
            PortfolioEmployeeSnapshot[] assigned = snapshot.employees
                .Where(employee => employee.assignedLocationId == selected.locationId)
                .OrderBy(employee => employee.role)
                .ToArray();
            if (assigned.Length == 0)
            {
                GUILayout.Box("No employees assigned. Hire a cashier, stock clerk, and manager to delegate this location.");
            }
            foreach (PortfolioEmployeeSnapshot employee in assigned)
            {
                DrawEmployee(employee, snapshot);
            }

            GUILayout.Space(12f);
            GUILayout.Label("AVAILABLE CANDIDATES");
            foreach (PortfolioCandidateDefinition candidate in
                     PortfolioProgressionRules.Candidates)
            {
                if (snapshot.employees.Any(employee =>
                        employee.employeeId == candidate.EmployeeId))
                {
                    continue;
                }
                GUILayout.BeginHorizontal("box");
                GUILayout.Label(
                    $"{candidate.DisplayName}  |  {FriendlyRole(candidate.Role)}  |  " +
                    $"skill {candidate.Skill}  reliability {candidate.Reliability}\n" +
                    $"{candidate.Trait}  |  wage {FormatCents(candidate.DailyWageCents)}/day  |  " +
                    $"hire {FormatCents(candidate.HiringCostCents)}",
                    GUILayout.ExpandWidth(true));
                if (GUILayout.Button("HIRE HERE", GUILayout.Width(120f), GUILayout.Height(42f)))
                {
                    TryHireCandidate(candidate.EmployeeId, selected.locationId, out _);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        private void DrawEmployee(
            PortfolioEmployeeSnapshot employee,
            PortfolioProgressionSnapshot snapshot)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label(
                $"{employee.displayName}  |  {FriendlyRole(employee.role)}  |  " +
                $"focus {employee.taskFocus}\n" +
                $"skill {employee.skill}  reliability {employee.reliability}  " +
                $"satisfaction {employee.satisfaction}  |  {employee.trait}  |  " +
                $"{FormatCents(employee.dailyWageCents)}/day");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(
                    $"TRAIN {FormatCents(PortfolioProgressionRules.TrainingCostCents)}"))
            {
                bool success = progression.TryTrainEmployee(
                    employee.employeeId,
                    out string error);
                RecordResult(
                    success,
                    success ? $"Trained {employee.displayName}; skill and satisfaction improved." : error);
            }
            if (employee.role != PortfolioEmployeeRole.Manager &&
                GUILayout.Button(
                    $"PROMOTE {FormatCents(PortfolioProgressionRules.PromotionCostCents)}"))
            {
                bool success = progression.TryPromoteToManager(
                    employee.employeeId,
                    out string error);
                RecordResult(
                    success,
                    success ? $"Promoted {employee.displayName} to manager." : error);
            }
            if (GUILayout.Button("CYCLE FOCUS"))
            {
                PortfolioTaskFocus next = (PortfolioTaskFocus)(
                    ((int)employee.taskFocus + 1) %
                    Enum.GetValues(typeof(PortfolioTaskFocus)).Length);
                bool success = progression.TrySetTaskFocus(
                    employee.employeeId,
                    next,
                    out string error);
                RecordResult(
                    success,
                    success ? $"Set {employee.displayName}'s focus to {next}." : error);
            }
            PortfolioLocationSnapshot other = snapshot.locations.FirstOrDefault(location =>
                location.locationId != employee.assignedLocationId);
            if (other != null && GUILayout.Button($"MOVE TO {other.displayName.ToUpperInvariant()}"))
            {
                bool success = progression.TryReassignEmployee(
                    employee.employeeId,
                    other.locationId,
                    out string error);
                RecordResult(
                    success,
                    success ? $"Reassigned {employee.displayName} to {other.displayName}." : error);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawLocations()
        {
            PortfolioProgressionSnapshot snapshot = progression.CreateSnapshot();
            DrawLocationSelector(snapshot);
            PortfolioLocationSnapshot location = SelectedLocation(snapshot);
            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical("box", GUILayout.Width(600f));
            GUILayout.Label($"{location.displayName.ToUpperInvariant()}  /  {location.districtName.ToUpperInvariant()}");
            GUILayout.Label(location.marketSummary);
            GUILayout.Label(
                $"BUSINESS SYSTEM  /  {location.businessTypeId}\n{location.operatingModel}");
            GUILayout.Label(
                $"Demand index {location.baseDemandUnits}  |  competition {location.competitionIndex}/100  |  " +
                $"reputation {location.reputation}/100\n" +
                $"Inventory {location.inventoryUnits}/{location.inventoryCapacityUnits}  |  " +
                $"rent {FormatCents(location.dailyRentCents)}/day  |  delegated days {location.delegatedDaysOperating}");
            GUILayout.Space(8f);
            GUILayout.Label("PRICING POLICY");
            GUILayout.BeginHorizontal();
            foreach (PortfolioPricingPolicy policy in
                     Enum.GetValues(typeof(PortfolioPricingPolicy)))
            {
                if (GUILayout.Button(
                        policy == location.pricingPolicy ? $"[{policy}]" : policy.ToString()))
                {
                    bool success = progression.TrySetPricingPolicy(
                        location.locationId,
                        policy,
                        out string error);
                    RecordResult(
                        success,
                        success ? $"{location.displayName} pricing set to {policy}." : error);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Value raises traffic and stock pressure; Premium trades traffic for unit margin.");
            GUILayout.Space(8f);
            GUILayout.Label("MANAGER REORDER POLICY");
            GUILayout.BeginHorizontal();
            foreach (PortfolioReorderPolicy policy in
                     Enum.GetValues(typeof(PortfolioReorderPolicy)))
            {
                if (GUILayout.Button(
                        policy == location.reorderPolicy ? $"[{policy}]" : policy.ToString()))
                {
                    bool success = progression.TrySetReorderPolicy(
                        location.locationId,
                        policy,
                        out string error);
                    RecordResult(
                        success,
                        success ? $"{location.displayName} reorder policy set to {policy}." : error);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Lean protects cash; Resilient protects availability by tying up more cash in stock.");
            GUILayout.EndVertical();

            GUILayout.Space(16f);
            GUILayout.BeginVertical("box");
            if (snapshot.locations.Count == 1)
            {
                GUILayout.Label("EXPANSION SITES");
                foreach (PortfolioLocationDefinition option in
                         PortfolioProgressionRules.ExpansionOptions)
                {
                    long committed = option.LeaseCostCents + option.OpeningInventoryCostCents;
                    GUILayout.BeginVertical("box");
                    GUILayout.Label(
                        $"{option.DisplayName}  /  {option.DistrictName}\n" +
                        $"{option.MarketSummary}\n" +
                        $"Demand {option.BaseDemandUnits}  competition {option.CompetitionIndex}/100  " +
                        $"rent {FormatCents(option.DailyRentCents)}/day\n" +
                        $"Lease + opening stock {FormatCents(committed)}");
                    if (GUILayout.Button($"LEASE {option.DisplayName.ToUpperInvariant()}"))
                    {
                        TryLeaseLocation(option.LocationId, out _);
                    }
                    GUILayout.EndVertical();
                }
            }
            else
            {
                GUILayout.Label("TWO-LOCATION PORTFOLIO ACTIVE");
                GUILayout.Label(
                    "Both sites share the employee, policy, finance, reporting, and aggregate-simulation foundation. Staff every role before advancing the next delegated day.");
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawReports()
        {
            PortfolioProgressionSnapshot snapshot = progression.CreateSnapshot();
            GUILayout.Label($"PORTFOLIO REPORT  /  THROUGH DAY {snapshot.currentDay}");
            GUILayout.Space(8f);
            reportScroll = GUILayout.BeginScrollView(reportScroll);
            long totalSales = 0;
            long totalProfit = 0;
            foreach (PortfolioLocationSnapshot location in snapshot.locations)
            {
                totalSales += location.lifetimeGrossSalesCents;
                totalProfit += location.lifetimeOperatingProfitCents;
                GUILayout.BeginVertical("box");
                GUILayout.Label(
                    $"{location.displayName.ToUpperInvariant()}  /  {location.districtName}\n" +
                    $"Lifetime sales {FormatCents(location.lifetimeGrossSalesCents)}  |  " +
                    $"COGS {FormatCents(location.lifetimeCostOfGoodsSoldCents)}  |  " +
                    $"purchases {FormatCents(location.lifetimeInventoryPurchaseCents)}\n" +
                    $"Payroll {FormatCents(location.lifetimePayrollCents)}  |  " +
                    $"rent {FormatCents(location.lifetimeRentCents)}  |  " +
                    $"operating profit {FormatCents(location.lifetimeOperatingProfitCents)}  |  " +
                    $"reputation {location.reputation}/100  |  inventory {location.inventoryUnits}/{location.inventoryCapacityUnits}");
                PortfolioLocationReportSnapshot report = location.lastReport;
                if (report == null)
                {
                    GUILayout.Label("No delegated day has been reported yet.");
                }
                else
                {
                    GUILayout.Label(
                        $"DAY {report.day}: demand {report.demandUnits}, sold {report.unitsSold}, lost {report.lostDemandUnits}, " +
                        $"price {FormatCents(report.unitPriceCents)}\n" +
                        $"Sales {FormatCents(report.grossSalesCents)} - COGS {FormatCents(report.costOfGoodsSoldCents)} - " +
                        $"payroll {FormatCents(report.payrollCents)} - rent {FormatCents(report.rentCents)} = " +
                        $"operating profit {FormatCents(report.operatingProfitCents)}\n" +
                        $"Reordered {report.reorderedUnits} units for {FormatCents(report.inventoryPurchaseCents)}; " +
                        $"cash change {FormatCents(report.cashChangeCents)}.\n" +
                        $"CAUSE: {report.primaryCause}");
                }
                GUILayout.EndVertical();
            }
            GUILayout.Space(8f);
            GUILayout.Box(
                $"PORTFOLIO LIFETIME SALES {FormatCents(totalSales)}  |  " +
                $"OPERATING PROFIT {FormatCents(totalProfit)}  |  CASH {FormatCents(snapshot.cashCents)}");
            GUILayout.EndScrollView();
        }

        private void DrawPortfolioSummary(PortfolioProgressionSnapshot snapshot)
        {
            GUILayout.Label("CURRENT LOCATIONS");
            foreach (PortfolioLocationSnapshot location in snapshot.locations)
            {
                int staff = snapshot.employees.Count(employee =>
                    employee.assignedLocationId == location.locationId);
                string report = location.lastReport == null
                    ? "no delegated report"
                    : $"day {location.lastReport.day} profit {FormatCents(location.lastReport.operatingProfitCents)}";
                GUILayout.Box(
                    $"{location.displayName} / {location.districtName}  |  staff {staff}/3  |  " +
                    $"price {location.pricingPolicy}  reorder {location.reorderPolicy}  |  {report}");
            }
        }

        private void DrawLocationSelector(PortfolioProgressionSnapshot snapshot)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("ACTIVE LOCATION", GUILayout.Width(130f));
            foreach (PortfolioLocationSnapshot location in snapshot.locations)
            {
                if (GUILayout.Button(
                        selectedLocationId == location.locationId
                            ? $"[{location.displayName}]"
                            : location.displayName,
                        GUILayout.Width(210f)))
                {
                    selectedLocationId = location.locationId;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawFooter(float width, float height)
        {
            Color prior = GUI.color;
            GUI.color = lastActionSucceeded
                ? new Color(0.72f, 1f, 0.84f)
                : new Color(1f, 0.66f, 0.58f);
            GUI.Box(
                new Rect(24f, height - 70f, width - 48f, 44f),
                lastAction);
            GUI.color = prior;
        }

        private PortfolioLocationSnapshot SelectedLocation(
            PortfolioProgressionSnapshot snapshot)
        {
            PortfolioLocationSnapshot selected = snapshot.locations.FirstOrDefault(location =>
                location.locationId == selectedLocationId);
            if (selected != null)
            {
                return selected;
            }

            selected = snapshot.locations[0];
            selectedLocationId = selected.locationId;
            return selected;
        }

        private string LocationName(string locationId)
        {
            PortfolioLocationSnapshot location = progression.Locations.FirstOrDefault(value =>
                value.locationId == locationId);
            if (location != null)
            {
                return location.displayName;
            }

            return PortfolioProgressionRules.TryGetLocationDefinition(
                locationId,
                out PortfolioLocationDefinition definition)
                    ? definition.DisplayName
                    : locationId;
        }

        private static bool IsFullyStaffed(
            PortfolioProgressionSnapshot snapshot,
            string locationId)
        {
            PortfolioEmployeeRole[] roles =
            {
                PortfolioEmployeeRole.Cashier,
                PortfolioEmployeeRole.StockClerk,
                PortfolioEmployeeRole.Manager
            };
            return roles.All(role => snapshot.employees.Any(employee =>
                employee.role == role && employee.assignedLocationId == locationId));
        }

        private static void DrawCheck(bool complete, string text)
        {
            GUILayout.Label($"{(complete ? "[DONE]" : "[    ]")}  {text}");
        }

        private void RecordResult(bool success, string message)
        {
            Record(string.IsNullOrWhiteSpace(message) ? "Action unavailable." : message, success);
        }

        private void Record(string message, bool success)
        {
            lastAction = message;
            lastActionSucceeded = success;
            if (success)
            {
                Debug.Log(message, this);
            }
            else
            {
                Debug.LogWarning(message, this);
            }
        }

        private static string FriendlyRole(PortfolioEmployeeRole role)
        {
            return role switch
            {
                PortfolioEmployeeRole.StockClerk => "Stock clerk",
                PortfolioEmployeeRole.Manager => "Manager",
                _ => "Cashier"
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
