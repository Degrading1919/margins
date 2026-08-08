using System.Linq;
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

            float strongAlone =
                EmployeeWorkPerformance.CalculateDetailedActionSeconds(
                    strongCashier.CreateWorkProfile(),
                    BusinessWorkCategory.CustomerService);
            float weakAlone =
                EmployeeWorkPerformance.CalculateDetailedActionSeconds(
                    weakCashier.CreateWorkProfile(),
                    BusinessWorkCategory.CustomerService);
            float strongManaged =
                EmployeeWorkPerformance.CalculateDetailedActionSeconds(
                    strongCashier.CreateWorkProfile(),
                    BusinessWorkCategory.CustomerService,
                    manager.CreateWorkProfile());

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
                Is.EqualTo(report.merchandiseSales.Sum(line =>
                    line.GrossSalesCents)));
            Assert.That(report.hasExactMerchandiseSales, Is.True);
            Assert.That(
                report.operatingProfitCents,
                Is.EqualTo(
                    report.grossSalesCents -
                    report.costOfGoodsSoldCents -
                    report.payrollCents -
                    report.rentCents -
                    report.deliveryFeesCents));
            Assert.That(
                after.cashCents,
                Is.EqualTo(beforeCash + report.cashChangeCents));
            Assert.That(
                location.inventoryUnits,
                Is.EqualTo(beforeInventory - report.unitsSold),
                "An order is paid when placed but cannot become inventory before delivery.");
            Assert.That(report.reorderedUnits, Is.GreaterThan(0));
            Assert.That(after.procurement.orders.Single().status, Is.EqualTo(PurchaseOrderStatus.Pending));
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
            Assert.That(
                value.merchandiseSales.Average(line => line.unitPriceCents),
                Is.LessThan(
                    premium.merchandiseSales.Average(line =>
                        line.unitPriceCents)));
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

        [Test]
        public void MerchandisingAssignmentsAndPricesAreAtomicPerLocation()
        {
            PortfolioProgression progression = PortfolioProgression.CreateInitial();
            const string colaShelf = "fixture-shelf-cola-validation";
            const string chipsShelf = "fixture-shelf-chips-validation";

            Assert.That(
                progression.TryUpdateShelfOffer(
                    PortfolioProgressionRules.FirstLocationId,
                    colaShelf,
                    "prod-potato-chips-small",
                    249,
                    null,
                    out string error),
                Is.False);
            StringAssert.Contains("already assigned", error);

            Assert.That(
                progression.TryUpdateShelfOffer(
                    PortfolioProgressionRules.FirstLocationId,
                    chipsShelf,
                    null,
                    0,
                    null,
                    out error),
                Is.True,
                error);
            Assert.That(
                progression.TryUpdateShelfOffer(
                    PortfolioProgressionRules.FirstLocationId,
                    colaShelf,
                    "prod-potato-chips-small",
                    249,
                    "QUICK BITE",
                    out error),
                Is.True,
                error);

            PortfolioLocationSnapshot location = progression.CreateSnapshot()
                .locations.Single();
            Assert.That(
                MerchandisingRules.TryGetOfferForProduct(
                    location,
                    "prod-potato-chips-small",
                    out MerchandiseOffer offer),
                Is.True);
            Assert.That(offer.ShelfFixtureId, Is.EqualTo(colaShelf));
            Assert.That(offer.InventoryLocationId, Is.EqualTo("loc-shelf-cola"));
            Assert.That(offer.SalePriceCents, Is.EqualTo(249));
            Assert.That(offer.CustomDisplayLabel, Is.EqualTo("QUICK BITE"));
        }

        [Test]
        public void CompletedDetailedRevenueKeepsExactHistoricalPriceAfterPriceChange()
        {
            PortfolioProgression progression = PortfolioProgression.CreateInitial();
            StoreSessionTotals totals = new(149, 60, 0, 89, 1, 1);
            MerchandiseSaleLineSnapshot[] sales =
            {
                new()
                {
                    productId = "prod-cola-can-355ml",
                    unitPriceCents = 149,
                    quantityUnits = 1
                }
            };
            Assert.That(
                progression.TryReconcileDetailedOperation(
                    "session-merchandising-price-history",
                    totals,
                    7,
                    945,
                    sales,
                    out _,
                    out string error),
                Is.True,
                error);
            Assert.That(
                progression.TryUpdateShelfOffer(
                    PortfolioProgressionRules.FirstLocationId,
                    "fixture-shelf-cola-validation",
                    "prod-cola-can-355ml",
                    399,
                    null,
                    out error),
                Is.True,
                error);

            PortfolioLocationSnapshot location = progression.CreateSnapshot()
                .locations.Single();
            Assert.That(location.lifetimeGrossSalesCents, Is.EqualTo(149));
            Assert.That(location.lastReport.grossSalesCents, Is.EqualTo(149));
            Assert.That(location.lastReport.hasExactMerchandiseSales, Is.True);
            Assert.That(
                location.lastReport.merchandiseSales.Single().unitPriceCents,
                Is.EqualTo(149));
            Assert.That(
                location.merchandisePrices.Single(value =>
                    value.productId == "prod-cola-can-355ml").salePriceCents,
                Is.EqualTo(399));
        }

        [Test]
        public void DelegatedSalesUseTheSameCurrentProductPricesAndExactLines()
        {
            PortfolioProgression progression = ReadyWithFirstTeam();
            Assert.That(
                progression.TryUpdateShelfOffer(
                    PortfolioProgressionRules.FirstLocationId,
                    "fixture-shelf-cola-validation",
                    "prod-cola-can-355ml",
                    175,
                    null,
                    out string error),
                Is.True,
                error);
            Assert.That(
                progression.TryUpdateShelfOffer(
                    PortfolioProgressionRules.FirstLocationId,
                    "fixture-shelf-chips-validation",
                    "prod-potato-chips-small",
                    225,
                    null,
                    out error),
                Is.True,
                error);
            Assert.That(progression.TryAdvanceDelegatedDay(out error), Is.True, error);

            PortfolioLocationReportSnapshot report = progression.CreateSnapshot()
                .locations.Single().lastReport;
            Assert.That(report.hasExactMerchandiseSales, Is.True);
            Assert.That(report.merchandiseSales, Is.Not.Empty);
            Assert.That(
                report.merchandiseSales.All(line =>
                    line.productId == "prod-cola-can-355ml"
                        ? line.unitPriceCents == 175
                        : line.productId == "prod-potato-chips-small" &&
                          line.unitPriceCents == 225),
                Is.True);
            Assert.That(
                report.grossSalesCents,
                Is.EqualTo(report.merchandiseSales.Sum(line =>
                    line.GrossSalesCents)));
        }

        [Test]
        public void PriceResponseIsDeterministicAndExcessivePricingCutsAcceptance()
        {
            int low = MerchandisingRules.CalculatePurchaseAcceptanceBasisPoints(
                100,
                200);
            int reasonable =
                MerchandisingRules.CalculatePurchaseAcceptanceBasisPoints(
                    200,
                    200);
            int excessive =
                MerchandisingRules.CalculatePurchaseAcceptanceBasisPoints(
                    400,
                    200);
            Assert.That(low, Is.GreaterThan(reasonable));
            Assert.That(reasonable, Is.GreaterThan(excessive));
            Assert.That(excessive, Is.LessThanOrEqualTo(500));
            int lowAccepted = 0;
            int reasonableAccepted = 0;
            int excessiveAccepted = 0;
            for (int index = 0; index < 1_000; index++)
            {
                string customerId = $"customer-price-response-{index:D4}";
                if (MerchandisingRules.WillPurchase(
                        customerId,
                        "prod-cola-can-355ml",
                        100,
                        200))
                {
                    lowAccepted++;
                }
                if (MerchandisingRules.WillPurchase(
                        customerId,
                        "prod-cola-can-355ml",
                        200,
                        200))
                {
                    reasonableAccepted++;
                }
                if (MerchandisingRules.WillPurchase(
                        customerId,
                        "prod-cola-can-355ml",
                        400,
                        200))
                {
                    excessiveAccepted++;
                }
            }

            Assert.That(lowAccepted, Is.EqualTo(1_000));
            Assert.That(reasonableAccepted, Is.InRange(920, 980));
            Assert.That(excessiveAccepted, Is.LessThan(100));
            Assert.That(lowAccepted, Is.GreaterThan(reasonableAccepted));
            Assert.That(reasonableAccepted, Is.GreaterThan(excessiveAccepted));
            bool first = MerchandisingRules.WillPurchase(
                "customer-stable-001",
                "prod-cola-can-355ml",
                400,
                200);
            Assert.That(
                MerchandisingRules.WillPurchase(
                    "customer-stable-001",
                    "prod-cola-can-355ml",
                    400,
                    200),
                Is.EqualTo(first));
            Assert.That(
                MerchandisingRules.ApplyDemandResponse(100, 100, 200),
                Is.GreaterThan(
                    MerchandisingRules.ApplyDemandResponse(100, 400, 200)));
        }

        [Test]
        public void VersionTwoPortfolioMigratesConfiguredMerchandisingDefaults()
        {
            PortfolioProgression source = ReadyForHiring();
            Assert.That(
                source.TrySetPricingPolicy(
                    PortfolioProgressionRules.FirstLocationId,
                    PortfolioPricingPolicy.Premium,
                    out string presetError),
                Is.True,
                presetError);
            PortfolioProgressionSnapshot legacy = source.CreateSnapshot();
            legacy.version = PortfolioProgressionSnapshot.PriorVersion;
            legacy.locations[0].merchandisePrices = null;
            legacy.locations[0].shelfMerchandiseAssignments = null;

            Assert.That(
                PortfolioProgression.TryRestore(
                    legacy,
                    out PortfolioProgression restored,
                    out string error),
                Is.True,
                error);
            PortfolioLocationSnapshot location = restored.CreateSnapshot()
                .locations.Single();
            Assert.That(
                location.merchandisePrices.Single(value =>
                    value.productId == "prod-cola-can-355ml").salePriceCents,
                Is.EqualTo(208));
            Assert.That(
                location.shelfMerchandiseAssignments.Single(value =>
                    value.shelfFixtureId ==
                    "fixture-shelf-cola-validation").assignedProductId,
                Is.EqualTo("prod-cola-can-355ml"));
            Assert.That(
                restored.CreateSnapshot().version,
                Is.EqualTo(PortfolioProgressionSnapshot.CurrentVersion));
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

    }
}
