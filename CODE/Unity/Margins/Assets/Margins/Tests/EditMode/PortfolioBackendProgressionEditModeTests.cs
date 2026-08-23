using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Margins.Tests.EditMode
{
    public sealed class PortfolioBackendProgressionEditModeTests
    {
        private static readonly StoreSessionTotals FirstShift = new(
            150,
            70,
            9_000,
            -8_920,
            1,
            1);

        [Test]
        public void ScheduleAuthorityBudgetAndAlertsDriveDelegatedOperation()
        {
            PortfolioProgression progression = ReadyWithFirstTeam();
            PortfolioEmployeeSnapshot cashier = progression.Employees.Single(value =>
                value.role == PortfolioEmployeeRole.Cashier);
            Assert.That(
                progression.TrySetEmployeeSchedule(
                    cashier.employeeId,
                    new PortfolioEmployeeScheduleSnapshot
                    {
                        scheduledDayMask = 1,
                        shiftStartMinute = 8 * 60,
                        shiftEndMinute = 16 * 60
                    },
                    out string error),
                Is.True,
                error);
            Assert.That(progression.CanAdvanceDelegatedDay(out error), Is.False);
            StringAssert.Contains("scheduled", error);

            Assert.That(
                progression.TrySetEmployeeSchedule(
                    cashier.employeeId,
                    PortfolioOperationsRules.CreateDefaultSchedule(),
                    out error),
                Is.True,
                error);
            PortfolioDelegationPolicySnapshot policy =
                PortfolioOperationsRules.CreateDefaultDelegationPolicy();
            policy.managerCanPurchase = false;
            policy.dailySpendingLimitCents = 0;
            policy.minimumProductAvailabilityBasisPoints = 9_500;
            Assert.That(
                progression.TrySetDelegationPolicy(
                    PortfolioProgressionRules.FirstLocationId,
                    policy,
                    out error),
                Is.True,
                error);
            Assert.That(progression.TryAdvanceDelegatedDay(out error), Is.True, error);

            PortfolioLocationSnapshot location = progression.CreateSnapshot()
                .locations.Single();
            Assert.That(location.lastReport.baseOperatingCostsCents, Is.EqualTo(3_000));
            Assert.That(location.lastReport.inventoryPurchaseCents, Is.Zero);
            Assert.That(
                location.productInventory.Sum(value => value.quantityUnits),
                Is.EqualTo(location.inventoryUnits));
            PortfolioOperatingAlertSnapshot authority = location.operatingAlerts
                .Single(value => value.problemTypeId == "purchasing-authority" &&
                                 value.IsOpen);
            Assert.That(authority.requiresOwnerAttention, Is.True);
            StringAssert.Contains("Grant purchasing authority", authority.recoveryAction);
            Assert.That(
                progression.TryAcknowledgeOperatingAlert(
                    location.locationId,
                    authority.alertId,
                    out error),
                Is.True,
                error);
            Assert.That(
                progression.CreateSnapshot().locations[0].operatingAlerts
                    .Single(value => value.alertId == authority.alertId)
                    .acknowledged,
                Is.True);
        }

        [Test]
        public void DetailedAggregateAndDetailedReturnReconcileWithoutDuplication()
        {
            PortfolioProgression progression = PortfolioProgression.CreateInitial();
            List<PortfolioProductInventorySnapshot> initialInventory = new()
            {
                new()
                {
                    productId = "prod-cola-can-355ml",
                    quantityUnits = 3,
                    unitCostCents = 70
                },
                new()
                {
                    productId = "prod-potato-chips-small",
                    quantityUnits = 3,
                    unitCostCents = 80
                }
            };
            Assert.That(
                progression.TryReconcileDetailedOperation(
                    PortfolioProgressionRules.FirstLocationId,
                    "session-operate-first",
                    FirstShift,
                    6,
                    450,
                    initialInventory,
                    new[]
                    {
                        new MerchandiseSaleLineSnapshot
                        {
                            productId = "prod-cola-can-355ml",
                            unitPriceCents = 150,
                            quantityUnits = 1
                        }
                    },
                    new DetailedOperationMetricsSnapshot
                    {
                        customerVisits = 2,
                        customersServed = 1,
                        customersAbandoned = 1,
                        requestedProductUnits = 2,
                        unavailableProductUnits = 1,
                        standardsTaskComplete = true
                    },
                    out _,
                    out string error),
                Is.True,
                error);
            HireFirstTeam(progression);
            Assert.That(progression.TryAdvanceDelegatedDay(out error), Is.True, error);

            PortfolioProgressionSnapshot afterAggregate =
                progression.CreateSnapshot();
            PortfolioLocationSnapshot aggregateLocation =
                afterAggregate.locations.Single();
            long cashAfterAggregate = afterAggregate.cashCents;
            long salesAfterAggregate =
                aggregateLocation.lifetimeGrossSalesCents;
            int inventoryAfterAggregate = aggregateLocation.inventoryUnits;
            long inventoryValue = aggregateLocation.productInventory.Sum(value =>
                value.InventoryValueCents);

            Assert.That(
                progression.TryReconcileDetailedOperation(
                    aggregateLocation.locationId,
                    "session-return-day-2",
                    new StoreSessionTotals(0, 0, 0, 0, 0, 0),
                    inventoryAfterAggregate,
                    inventoryValue,
                    aggregateLocation.productInventory,
                    new List<MerchandiseSaleLineSnapshot>(),
                    new DetailedOperationMetricsSnapshot(),
                    out bool unchanged,
                    out error),
                Is.True,
                error);
            Assert.That(unchanged, Is.False, "A new detailed session must establish a new baseline.");
            PortfolioLocationSnapshot returned = progression.CreateSnapshot()
                .locations.Single();
            Assert.That(progression.CashCents, Is.EqualTo(cashAfterAggregate));
            Assert.That(returned.lifetimeGrossSalesCents, Is.EqualTo(salesAfterAggregate));
            Assert.That(returned.inventoryUnits, Is.EqualTo(inventoryAfterAggregate));
            Assert.That(returned.detailedReconciliation.sessionId,
                Is.EqualTo("session-return-day-2"));
            Assert.That(returned.detailedReconciliation.inventoryAcquiredCostCents,
                Is.Zero);
        }

        [Test]
        public void CompanyPropertyBrandUnitAndAcquisitionRemainSeparateAndValidated()
        {
            PortfolioProgression progression = ReadyWithFirstTeam();

            PortfolioProgressionSnapshot before = progression.CreateSnapshot();
            PortfolioLocationSnapshot location = before.locations.Single();
            PortfolioCommercialPropertySnapshot property =
                before.company.properties.Single();
            PortfolioCommercialUnitSnapshot unit =
                property.commercialUnits.Single();
            Assert.That(location.brandId, Is.Not.EqualTo(property.propertyId));
            Assert.That(location.locationId, Is.Not.EqualTo(unit.commercialUnitId));
            Assert.That(unit.occupyingLocationId, Is.EqualTo(location.locationId));
            Assert.That(property.tenure, Is.EqualTo(PortfolioPropertyTenure.Leased));

            Assert.That(
                progression.TryInstallPropertyImprovement(
                    location.locationId,
                    "improvement-security-lighting-001",
                    "security-lighting",
                    12_500,
                    out string error),
                Is.True,
                error);
            Assert.That(
                progression.TryAcquireProperty(property.propertyId, out error),
                Is.True,
                error);
            Assert.That(progression.TryAdvanceDelegatedDay(out error), Is.True, error);
            PortfolioConsolidatedReportSnapshot consolidated =
                progression.CreateConsolidatedReport();
            Assert.That(consolidated.ownedPropertyCount, Is.EqualTo(1));
            Assert.That(consolidated.leasedPropertyCount, Is.Zero);
            Assert.That(consolidated.lifetimePropertyAcquisitionCents,
                Is.EqualTo(property.acquisitionCostCents));
            Assert.That(consolidated.lifetimeImprovementCents, Is.EqualTo(12_500));
            Assert.That(
                progression.CreateSnapshot().locations[0].lastReport.rentCents,
                Is.Zero,
                "Owned property must stop posting lease rent on later operating days.");

            PortfolioProgressionSnapshot contradictory = progression.CreateSnapshot();
            contradictory.company.properties[0].commercialUnits[0]
                .occupyingLocationId = "location-not-in-portfolio";
            Assert.That(
                PortfolioProgression.TryRestore(
                    contradictory,
                    out _,
                    out error),
                Is.False);
            StringAssert.Contains("unit", error.ToLowerInvariant());

            contradictory = progression.CreateSnapshot();
            contradictory.cashCents++;
            Assert.That(
                PortfolioProgression.TryRestore(
                    contradictory,
                    out _,
                    out error),
                Is.False);
            StringAssert.Contains("cash", error.ToLowerInvariant());

            contradictory = progression.CreateSnapshot();
            contradictory.company.properties[0].commercialUnits[0]
                .generatedLayout.layoutRevision++;
            Assert.That(
                PortfolioProgression.TryRestore(
                    contradictory,
                    out _,
                    out error),
                Is.False);
            StringAssert.Contains("revision", error.ToLowerInvariant());
        }

        [Test]
        public void OverflowingOrContradictoryAuthoritativeMetricsRejectCleanly()
        {
            DetailedOperationMetricsSnapshot contradictoryMetrics = new()
            {
                customerVisits = int.MaxValue,
                customersServed = int.MaxValue,
                customersAbandoned = int.MaxValue,
                requestedProductUnits = 1,
                unavailableProductUnits = 0
            };
            Assert.That(
                contradictoryMetrics.TryValidate(out string error),
                Is.False);
            StringAssert.Contains("contradict", error.ToLowerInvariant());

            PortfolioProgressionSnapshot overflowing =
                PortfolioProgression.CreateInitial().CreateSnapshot();
            overflowing.locations[0].inventoryUnits = 2;
            overflowing.locations[0].productInventory[0].quantityUnits = 2;
            overflowing.locations[0].productInventory[0].unitCostCents =
                long.MaxValue;
            Assert.That(
                PortfolioProgression.TryRestore(
                    overflowing,
                    out _,
                    out error),
                Is.False);
            StringAssert.Contains("value", error.ToLowerInvariant());
        }

        [Test]
        public void VersionThreeMigratesLocationScopedOperationsAndPropertyGraph()
        {
            PortfolioProgression current = ReadyWithFirstTeam();
            PortfolioProgressionSnapshot legacy = current.CreateSnapshot();
            PortfolioDetailedReconciliationSnapshot detailed =
                legacy.locations[0].detailedReconciliation;
            legacy.version = PortfolioProgressionSnapshot.PriorVersion;
            legacy.detailedOperationInitialized = detailed.initialized;
            legacy.processedDetailedSessionId = detailed.sessionId;
            legacy.reconciledDetailedGrossSalesCents = detailed.grossSalesCents;
            legacy.reconciledDetailedCostOfGoodsSoldCents =
                detailed.costOfGoodsSoldCents;
            legacy.reconciledDetailedOperatingExpensesCents =
                detailed.includedOperatingExpensesCents;
            legacy.reconciledDetailedPayrollCents = detailed.payrollCents;
            legacy.reconciledDetailedRentCents = detailed.rentCents;
            legacy.reconciledDetailedInventoryAcquiredCostCents =
                detailed.inventoryAcquiredCostCents;
            legacy.reconciledDetailedUnitsSold = detailed.unitsSold;
            legacy.reconciledDetailedTransactionCount = detailed.transactionCount;
            legacy.company = null;
            foreach (PortfolioEmployeeSnapshot employee in legacy.employees)
            {
                employee.schedule = null;
            }
            foreach (PortfolioLocationSnapshot location in legacy.locations)
            {
                location.brandId = null;
                location.propertyId = null;
                location.commercialUnitId = null;
                location.productInventory = null;
                location.delegationPolicy = null;
                location.detailedReconciliation = null;
                location.operatingAlerts = null;
                location.maintenanceCondition = 0;
            }

            Assert.That(
                PortfolioProgression.TryRestore(
                    legacy,
                    out PortfolioProgression restored,
                    out string error),
                Is.True,
                error);
            PortfolioProgressionSnapshot migrated = restored.CreateSnapshot();
            Assert.That(migrated.version,
                Is.EqualTo(PortfolioProgressionSnapshot.CurrentVersion));
            Assert.That(migrated.company.properties, Has.Count.EqualTo(1));
            Assert.That(migrated.locations[0].productInventory.Sum(value =>
                value.quantityUnits), Is.EqualTo(migrated.locations[0].inventoryUnits));
            Assert.That(migrated.employees.All(value => value.schedule != null),
                Is.True);
            Assert.That(migrated.locations[0].detailedReconciliation.sessionId,
                Is.EqualTo(detailed.sessionId));
        }

        private static PortfolioProgression ReadyWithFirstTeam()
        {
            PortfolioProgression progression = PortfolioProgression.CreateInitial();
            Assert.That(
                progression.TryPostDetailedShift(
                    "session-backend-ready",
                    FirstShift,
                    6,
                    out _,
                    out string error),
                Is.True,
                error);
            HireFirstTeam(progression);
            return progression;
        }

        private static void HireFirstTeam(PortfolioProgression progression)
        {
            foreach (string employeeId in new[]
                     {
                         "employee-elena-ruiz",
                         "employee-marcus-reed",
                         "employee-priya-shah"
                     })
            {
                Assert.That(
                    progression.TryHireCandidate(
                        employeeId,
                        PortfolioProgressionRules.FirstLocationId,
                        out string error),
                    Is.True,
                    error);
            }
        }
    }
}
