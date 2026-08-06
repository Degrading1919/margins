using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Margins.Tests.EditMode
{
    public sealed class PortfolioProgressionDomainEditModeTests
    {
        private static readonly StoreSessionTotals FirstShiftTotals = new(
            grossSalesCents: 348,
            costOfGoodsSoldCents: 150,
            includedOperatingExpensesCents: 90,
            contributionAfterCostOfGoodsCents: 108,
            unitsSold: 2,
            transactionCount: 1);

        [Test]
        public void DetailedEmployeePacingUsesCompetenceFocusAndManagerPresence()
        {
            MethodInfo actionDelay = typeof(InStoreEmployeeWorkController)
                .GetMethod(
                    "ActionDelay",
                    BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(actionDelay, Is.Not.Null);

            PortfolioEmployeeSnapshot strongCashier = new()
            {
                skill = 90,
                reliability = 95,
                taskFocus = PortfolioTaskFocus.Service
            };
            PortfolioEmployeeSnapshot weakCashier = new()
            {
                skill = 30,
                reliability = 35,
                taskFocus = PortfolioTaskFocus.Inventory
            };
            PortfolioEmployeeSnapshot manager = new()
            {
                skill = 80,
                reliability = 90,
                taskFocus = PortfolioTaskFocus.Balanced
            };

            float strongAlone = InvokeDelay(
                actionDelay,
                strongCashier,
                null,
                PortfolioTaskFocus.Service);
            float weakAlone = InvokeDelay(
                actionDelay,
                weakCashier,
                null,
                PortfolioTaskFocus.Service);
            float strongManaged = InvokeDelay(
                actionDelay,
                strongCashier,
                manager,
                PortfolioTaskFocus.Service);

            Assert.That(strongAlone, Is.LessThan(weakAlone));
            Assert.That(strongManaged, Is.LessThan(strongAlone));
        }

        [Test]
        public void DetailedShiftPostsCashBasisExactlyOnce()
        {
            PortfolioProgression progression = PortfolioProgression.CreateInitial();
            long startingCash = progression.CashCents;

            Assert.That(
                progression.TryPostDetailedShift(
                    "session-first-store-validation-001",
                    FirstShiftTotals,
                    7,
                    out bool alreadyPosted,
                    out string error),
                Is.True,
                error);
            Assert.That(alreadyPosted, Is.False);
            Assert.That(progression.FirstShiftCompleted, Is.True);
            long acquiredInventoryCost = (7 * 135) + 150;
            long expectedCash = startingCash + 348 - 90 - acquiredInventoryCost;
            Assert.That(progression.CashCents, Is.EqualTo(expectedCash));
            Assert.That(
                progression.Locations.Single().inventoryUnits,
                Is.EqualTo(7),
                "The aggregate first location must begin from the exact detailed remainder.");

            Assert.That(
                progression.TryPostDetailedShift(
                    "session-first-store-validation-001",
                    FirstShiftTotals,
                    7,
                    out alreadyPosted,
                    out error),
                Is.True,
                error);
            Assert.That(alreadyPosted, Is.True);
            Assert.That(progression.CashCents, Is.EqualTo(expectedCash));

            Assert.That(
                progression.TryPostDetailedShift(
                    "session-different",
                    FirstShiftTotals,
                    7,
                    out _,
                    out _),
                Is.False);
            Assert.That(progression.CashCents, Is.EqualTo(expectedCash));
        }

        [Test]
        public void DelegatedDayRequiresEveryWorkerRoleAndManager()
        {
            PortfolioProgression progression = ReadyForHiring();

            Assert.That(progression.CanAdvanceDelegatedDay(out string blocker), Is.False);
            StringAssert.Contains("cashier", blocker);

            Assert.That(
                progression.TryHireCandidate(
                    "employee-elena-ruiz",
                    PortfolioProgressionRules.FirstLocationId,
                    out string error),
                Is.True,
                error);
            Assert.That(progression.CanAdvanceDelegatedDay(out blocker), Is.False);
            StringAssert.Contains("stock clerk", blocker);

            Assert.That(
                progression.TryHireCandidate(
                    "employee-marcus-reed",
                    PortfolioProgressionRules.FirstLocationId,
                    out error),
                Is.True,
                error);
            Assert.That(progression.CanAdvanceDelegatedDay(out blocker), Is.False);
            StringAssert.Contains("manager", blocker);

            Assert.That(
                progression.TryHireCandidate(
                    "employee-priya-shah",
                    PortfolioProgressionRules.FirstLocationId,
                    out error),
                Is.True,
                error);
            Assert.That(progression.CanAdvanceDelegatedDay(out blocker), Is.True, blocker);
        }

        [Test]
        public void DetailedInventoryPurchasesCogsRentPayrollAndCashReconcileWithoutDoubleCounting()
        {
            PortfolioProgression progression = PortfolioProgression.CreateInitial();
            long startingCash = progression.CashCents;
            StoreSessionTotals beforeSales = new(
                grossSalesCents: 0,
                costOfGoodsSoldCents: 0,
                includedOperatingExpensesCents: 9_000,
                contributionAfterCostOfGoodsCents: -9_000,
                unitsSold: 0,
                transactionCount: 0);

            Assert.That(
                progression.TryReconcileDetailedOperation(
                    "session-live-store-001",
                    beforeSales,
                    remainingInventoryUnits: 8,
                    inventoryAssetValueCents: 600,
                    out bool unchanged,
                    out string error),
                Is.True,
                error);
            Assert.That(unchanged, Is.False);
            Assert.That(progression.CashCents, Is.EqualTo(startingCash - 600 - 9_000));

            StoreSessionTotals firstSale = new(
                grossSalesCents: 348,
                costOfGoodsSoldCents: 150,
                includedOperatingExpensesCents: 9_000,
                contributionAfterCostOfGoodsCents: -8_802,
                unitsSold: 2,
                transactionCount: 1);
            Assert.That(
                progression.TryReconcileDetailedOperation(
                    "session-live-store-001",
                    firstSale,
                    remainingInventoryUnits: 6,
                    inventoryAssetValueCents: 450,
                    out unchanged,
                    out error),
                Is.True,
                error);
            long cashAfterSale = startingCash - 600 - 9_000 + 348;
            Assert.That(progression.CashCents, Is.EqualTo(cashAfterSale));
            Assert.That(
                progression.Locations.Single().lifetimeCostOfGoodsSoldCents,
                Is.EqualTo(150),
                "COGS must reduce profit but must not be a second inventory cash outflow.");

            Assert.That(
                progression.TryHireCandidate(
                    "employee-elena-ruiz",
                    PortfolioProgressionRules.FirstLocationId,
                    out error),
                Is.True,
                error);
            StoreSessionTotals staffed = new(
                grossSalesCents: 348,
                costOfGoodsSoldCents: 150,
                includedOperatingExpensesCents: 21_000,
                contributionAfterCostOfGoodsCents: -20_802,
                unitsSold: 2,
                transactionCount: 1);
            Assert.That(
                progression.TryReconcileDetailedOperation(
                    "session-live-store-001",
                    staffed,
                    remainingInventoryUnits: 6,
                    inventoryAssetValueCents: 450,
                    out unchanged,
                    out error),
                Is.True,
                error);

            PortfolioProgressionSnapshot snapshot = progression.CreateSnapshot();
            PortfolioLocationSnapshot location = snapshot.locations.Single();
            Assert.That(location.lifetimeGrossSalesCents, Is.EqualTo(348));
            Assert.That(location.lifetimeCostOfGoodsSoldCents, Is.EqualTo(150));
            Assert.That(location.lifetimeInventoryPurchaseCents, Is.EqualTo(600));
            Assert.That(location.lifetimeRentCents, Is.EqualTo(9_000));
            Assert.That(location.lifetimePayrollCents, Is.EqualTo(12_000));
            Assert.That(location.lifetimeOperatingProfitCents, Is.EqualTo(-20_802));
            Assert.That(location.lifetimeCashChangeCents, Is.EqualTo(-21_252));
            Assert.That(location.lastReport.cashChangeCents, Is.EqualTo(-21_252));
            Assert.That(
                snapshot.cashCents,
                Is.EqualTo(
                    startingCash -
                    600 -
                    9_000 +
                    348 -
                    25_000 -
                    12_000));

            Assert.That(
                progression.TryReconcileDetailedOperation(
                    "session-live-store-001",
                    staffed,
                    remainingInventoryUnits: 6,
                    inventoryAssetValueCents: 450,
                    out unchanged,
                    out error),
                Is.True,
                error);
            Assert.That(unchanged, Is.True);
            Assert.That(
                PortfolioProgression.TryValidateSnapshot(
                    progression.CreateSnapshot(),
                    out error),
                Is.True,
                error);
        }

        [Test]
        public void DelegatedDayReconcilesMoneyInventoryPeopleAndReport()
        {
            PortfolioProgression progression = ReadyWithFirstTeam();
            PortfolioProgressionSnapshot before = progression.CreateSnapshot();
            long beforeCash = before.cashCents;
            int beforeInventory = before.locations[0].inventoryUnits;

            Assert.That(
                progression.TryAdvanceDelegatedDay(out string error),
                Is.True,
                error);

            PortfolioProgressionSnapshot after = progression.CreateSnapshot();
            PortfolioLocationSnapshot location = after.locations.Single();
            PortfolioLocationReportSnapshot report = location.lastReport;
            Assert.That(after.currentDay, Is.EqualTo(2));
            Assert.That(location.daysOperating, Is.EqualTo(2));
            Assert.That(location.delegatedDaysOperating, Is.EqualTo(1));
            Assert.That(report, Is.Not.Null);
            Assert.That(report.day, Is.EqualTo(2));
            Assert.That(report.demandUnits, Is.EqualTo(report.unitsSold + report.lostDemandUnits));
            Assert.That(
                report.grossSalesCents,
                Is.EqualTo(report.unitPriceCents * report.unitsSold));
            Assert.That(
                report.operatingProfitCents,
                Is.EqualTo(
                    report.grossSalesCents -
                    report.costOfGoodsSoldCents -
                    report.payrollCents -
                    report.rentCents));
            Assert.That(
                after.cashCents,
                Is.EqualTo(beforeCash + report.cashChangeCents));
            Assert.That(
                location.inventoryUnits,
                Is.EqualTo(beforeInventory - report.unitsSold + report.reorderedUnits));
            Assert.That(location.inventoryUnits, Is.InRange(0, location.inventoryCapacityUnits));
            Assert.That(
                after.employees.All(employee =>
                    employee.skill > before.employees.Single(prior =>
                        prior.employeeId == employee.employeeId).skill),
                Is.True);
            Assert.That(
                PortfolioProgression.TryValidateSnapshot(after, out error),
                Is.True,
                error);
        }

        [Test]
        public void SameStateAndInputsProduceIdenticalDelegatedResult()
        {
            PortfolioProgression left = ReadyWithFirstTeam();
            PortfolioProgression right = ReadyWithFirstTeam();

            Assert.That(left.TryAdvanceDelegatedDay(out string leftError), Is.True, leftError);
            Assert.That(right.TryAdvanceDelegatedDay(out string rightError), Is.True, rightError);

            PortfolioProgressionSnapshot leftSnapshot = left.CreateSnapshot();
            PortfolioProgressionSnapshot rightSnapshot = right.CreateSnapshot();
            PortfolioLocationReportSnapshot leftReport = leftSnapshot.locations[0].lastReport;
            PortfolioLocationReportSnapshot rightReport = rightSnapshot.locations[0].lastReport;
            Assert.That(rightSnapshot.cashCents, Is.EqualTo(leftSnapshot.cashCents));
            Assert.That(rightReport.demandUnits, Is.EqualTo(leftReport.demandUnits));
            Assert.That(rightReport.unitsSold, Is.EqualTo(leftReport.unitsSold));
            Assert.That(rightReport.reorderedUnits, Is.EqualTo(leftReport.reorderedUnits));
            Assert.That(rightReport.operatingProfitCents, Is.EqualTo(leftReport.operatingProfitCents));
            Assert.That(rightReport.primaryCause, Is.EqualTo(leftReport.primaryCause));
        }

        [Test]
        public void PricingAndReorderPoliciesCreateDifferentTradeoffs()
        {
            PortfolioProgression valueLean = ReadyWithFirstTeam();
            PortfolioProgression premiumResilient = ReadyWithFirstTeam();

            Assert.That(
                valueLean.TrySetPricingPolicy(
                    PortfolioProgressionRules.FirstLocationId,
                    PortfolioPricingPolicy.Value,
                    out string error),
                Is.True,
                error);
            Assert.That(
                valueLean.TrySetReorderPolicy(
                    PortfolioProgressionRules.FirstLocationId,
                    PortfolioReorderPolicy.Lean,
                    out error),
                Is.True,
                error);
            Assert.That(
                premiumResilient.TrySetPricingPolicy(
                    PortfolioProgressionRules.FirstLocationId,
                    PortfolioPricingPolicy.Premium,
                    out error),
                Is.True,
                error);
            Assert.That(
                premiumResilient.TrySetReorderPolicy(
                    PortfolioProgressionRules.FirstLocationId,
                    PortfolioReorderPolicy.Resilient,
                    out error),
                Is.True,
                error);

            Assert.That(valueLean.TryAdvanceDelegatedDay(out error), Is.True, error);
            Assert.That(premiumResilient.TryAdvanceDelegatedDay(out error), Is.True, error);

            PortfolioLocationReportSnapshot value =
                valueLean.CreateSnapshot().locations[0].lastReport;
            PortfolioLocationReportSnapshot premium =
                premiumResilient.CreateSnapshot().locations[0].lastReport;
            Assert.That(value.demandUnits, Is.GreaterThan(premium.demandUnits));
            Assert.That(value.unitPriceCents, Is.LessThan(premium.unitPriceCents));
            Assert.That(value.reorderedUnits, Is.LessThan(premium.reorderedUnits));
            Assert.That(
                value.inventoryPurchaseCents,
                Is.LessThan(premium.inventoryPurchaseCents));
        }

        [Test]
        public void ExpansionRequiresProvenDelegationAndMarketChoiceChangesCost()
        {
            PortfolioProgression tooEarly = ReadyWithFirstTeam();
            Assert.That(
                tooEarly.TryLeaseLocation("location-riverbend-market", out string error),
                Is.False);
            StringAssert.Contains("delegated operating day", error);

            PortfolioProgression riverbend = ReadyWithFirstTeam();
            PortfolioProgression downtown = ReadyWithFirstTeam();
            Assert.That(riverbend.TryAdvanceDelegatedDay(out error), Is.True, error);
            Assert.That(downtown.TryAdvanceDelegatedDay(out error), Is.True, error);
            long sameCash = riverbend.CashCents;
            Assert.That(downtown.CashCents, Is.EqualTo(sameCash));

            Assert.That(
                riverbend.TryLeaseLocation("location-riverbend-market", out error),
                Is.True,
                error);
            Assert.That(
                downtown.TryLeaseLocation("location-downtown-market", out error),
                Is.True,
                error);
            Assert.That(riverbend.Locations.Count, Is.EqualTo(2));
            Assert.That(downtown.Locations.Count, Is.EqualTo(2));
            Assert.That(riverbend.CashCents, Is.GreaterThan(downtown.CashCents));
            Assert.That(
                riverbend.Locations.Single(location =>
                    location.locationId == "location-riverbend-market").baseDemandUnits,
                Is.LessThan(
                    downtown.Locations.Single(location =>
                        location.locationId == "location-downtown-market").baseDemandUnits));
        }

        [Test]
        public void FullTwoLocationPortfolioRunsAndReportsAsOneAtomicDay()
        {
            PortfolioProgression progression = ReadyWithFirstTeam();
            Assert.That(
                progression.TryAdvanceDelegatedDay(out string error),
                Is.True,
                error);
            Assert.That(
                progression.TryLeaseLocation("location-riverbend-market", out error),
                Is.True,
                error);
            HireSecondTeam(progression, "location-riverbend-market");
            Assert.That(
                progression.TrySetPricingPolicy(
                    "location-riverbend-market",
                    PortfolioPricingPolicy.Value,
                    out error),
                Is.True,
                error);
            Assert.That(
                progression.TrySetReorderPolicy(
                    PortfolioProgressionRules.FirstLocationId,
                    PortfolioReorderPolicy.Lean,
                    out error),
                Is.True,
                error);
            long cashBefore = progression.CashCents;

            Assert.That(
                progression.TryAdvanceDelegatedDay(out error),
                Is.True,
                error);

            PortfolioProgressionSnapshot snapshot = progression.CreateSnapshot();
            Assert.That(snapshot.currentDay, Is.EqualTo(3));
            Assert.That(snapshot.locations.Count, Is.EqualTo(2));
            Assert.That(snapshot.employees.Count, Is.EqualTo(6));
            Assert.That(snapshot.locations.All(location => location.lastReport.day == 3), Is.True);
            Assert.That(
                snapshot.cashCents,
                Is.EqualTo(
                    cashBefore + snapshot.locations.Sum(location =>
                        location.lastReport.cashChangeCents)));
            Assert.That(
                snapshot.cashCents,
                Is.GreaterThanOrEqualTo(
                    PortfolioProgressionRules.MinimumCashReserveCents));
            Assert.That(
                PortfolioProgression.TryValidateSnapshot(snapshot, out error),
                Is.True,
                error);
        }

        [Test]
        public void TrainingEnablesPersistentPromotionPath()
        {
            PortfolioProgression progression = ReadyForHiring();
            Assert.That(
                progression.TryHireCandidate(
                    "employee-elena-ruiz",
                    PortfolioProgressionRules.FirstLocationId,
                    out string error),
                Is.True,
                error);
            Assert.That(
                progression.TryPromoteToManager("employee-elena-ruiz", out error),
                Is.False);
            StringAssert.Contains("skill 65", error);

            Assert.That(
                progression.TryTrainEmployee("employee-elena-ruiz", out error),
                Is.True,
                error);
            Assert.That(
                progression.TryTrainEmployee("employee-elena-ruiz", out error),
                Is.False);
            StringAssert.Contains("already trained", error);
            Assert.That(
                progression.TryPromoteToManager("employee-elena-ruiz", out error),
                Is.True,
                error);

            PortfolioEmployeeSnapshot promoted = progression.Employees.Single();
            Assert.That(promoted.role, Is.EqualTo(PortfolioEmployeeRole.Manager));
            Assert.That(promoted.skill, Is.EqualTo(68));
            Assert.That(promoted.satisfaction, Is.GreaterThan(72));
        }

        [Test]
        public void InvalidRestoreRejectsWithoutMutatingAcceptedState()
        {
            PortfolioProgression progression = ReadyWithFirstTeam();
            Assert.That(
                progression.TryAdvanceDelegatedDay(out string error),
                Is.True,
                error);
            PortfolioProgressionSnapshot accepted = progression.CreateSnapshot();
            PortfolioProgressionSnapshot invalid = progression.CreateSnapshot();
            invalid.locations[0].lastReport.grossSalesCents++;

            Assert.That(
                PortfolioProgression.TryRestore(
                    invalid,
                    out _,
                    out error),
                Is.False);
            StringAssert.Contains("reconcile", error);
            Assert.That(progression.CashCents, Is.EqualTo(accepted.cashCents));
            Assert.That(
                progression.Locations[0].lastReport.grossSalesCents,
                Is.EqualTo(accepted.locations[0].lastReport.grossSalesCents));

            PortfolioProgressionSnapshot wageTamper = progression.CreateSnapshot();
            wageTamper.employees[0].dailyWageCents = 1;
            Assert.That(
                PortfolioProgression.TryRestore(
                    wageTamper,
                    out _,
                    out error),
                Is.False);
            StringAssert.Contains("candidate record", error);

            PortfolioProgressionSnapshot overflow = progression.CreateSnapshot();
            overflow.locations[0].lastReport.unitPriceCents = long.MaxValue;
            overflow.locations[0].lastReport.unitsSold = 2;
            Assert.That(
                PortfolioProgression.TryRestore(
                    overflow,
                    out _,
                    out error),
                Is.False);
            StringAssert.Contains("overflowed", error);

            Assert.That(
                PortfolioProgression.TryRestore(
                    accepted,
                    out PortfolioProgression restored,
                    out error),
                Is.True,
                error);
            Assert.That(restored.CashCents, Is.EqualTo(accepted.cashCents));
            Assert.That(restored.Employees.Count, Is.EqualTo(3));
        }

        private static PortfolioProgression ReadyForHiring()
        {
            PortfolioProgression progression = PortfolioProgression.CreateInitial();
            Assert.That(
                progression.TryPostDetailedShift(
                    "session-first-store-validation-001",
                    FirstShiftTotals,
                    7,
                    out _,
                    out string error),
                Is.True,
                error);
            return progression;
        }

        private static PortfolioProgression ReadyWithFirstTeam()
        {
            PortfolioProgression progression = ReadyForHiring();
            HireFirstTeam(progression);
            return progression;
        }

        private static void HireFirstTeam(PortfolioProgression progression)
        {
            Assert.That(
                progression.TryHireCandidate(
                    "employee-elena-ruiz",
                    PortfolioProgressionRules.FirstLocationId,
                    out string error),
                Is.True,
                error);
            Assert.That(
                progression.TryHireCandidate(
                    "employee-marcus-reed",
                    PortfolioProgressionRules.FirstLocationId,
                    out error),
                Is.True,
                error);
            Assert.That(
                progression.TryHireCandidate(
                    "employee-priya-shah",
                    PortfolioProgressionRules.FirstLocationId,
                    out error),
                Is.True,
                error);
        }

        private static void HireSecondTeam(
            PortfolioProgression progression,
            string locationId)
        {
            Assert.That(
                progression.TryHireCandidate(
                    "employee-jonah-brooks",
                    locationId,
                    out string error),
                Is.True,
                error);
            Assert.That(
                progression.TryHireCandidate(
                    "employee-nia-carter",
                    locationId,
                    out error),
                Is.True,
                error);
            Assert.That(
                progression.TryHireCandidate(
                    "employee-luis-ortega",
                    locationId,
                    out error),
                Is.True,
                error);
        }

        private static float InvokeDelay(
            MethodInfo actionDelay,
            PortfolioEmployeeSnapshot employee,
            PortfolioEmployeeSnapshot manager,
            PortfolioTaskFocus preferredFocus)
        {
            return (float)actionDelay.Invoke(
                null,
                new object[] { employee, manager, preferredFocus });
        }
    }
}
