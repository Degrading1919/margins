using NUnit.Framework;

namespace Margins.Tests.EditMode
{
    public sealed class BusinessOperationsDomainEditModeTests
    {
        private static readonly object[] ArchitectureCompatibilityCases =
        {
            Case("convenience-store", "stock-shelf", "retail-fixture", "physical-product", "location-inventory", BusinessWorkCategory.ResourceFlow),
            Case("coffee-shop", "prepare-order", "beverage-station", "ingredient-allocation", "order-ledger", BusinessWorkCategory.CustomerService),
            Case("laundromat", "load-washer", "washer", "laundry-load", "service-ledger", BusinessWorkCategory.ResourceFlow),
            Case("internet-cafe", "assign-computer", "computer", "time-slot", "reservation-ledger", BusinessWorkCategory.CustomerService),
            Case("pawn-shop", "appraise-item", "appraisal-counter", "pledged-item", "transaction-ledger", BusinessWorkCategory.CustomerService),
            Case("gas-station", "authorize-pump", "fuel-pump", "fuel-allocation", "transaction-ledger", BusinessWorkCategory.CustomerService),
            Case("auto-repair", "repair-vehicle", "service-bay", "vehicle-work-order", "repair-order-ledger", BusinessWorkCategory.ResourceFlow),
            Case("gym", "check-in-member", "access-gate", "membership-reservation", "visit-ledger", BusinessWorkCategory.CustomerService),
            Case("salon", "perform-appointment", "salon-chair", "appointment-slot", "service-transaction", BusinessWorkCategory.CustomerService),
            Case("self-storage", "assign-unit", "storage-unit", "storage-reservation", "rental-ledger", BusinessWorkCategory.CustomerService),
            Case("restaurant", "prepare-meal", "kitchen-station", "ingredient-allocation", "order-ledger", BusinessWorkCategory.ResourceFlow),
            Case("motel", "service-room", "guest-room", "room-work-order", "maintenance-state", BusinessWorkCategory.Standards),
            Case("movie-theater", "seat-guest", "auditorium-seat", "seat-reservation", "ticket-ledger", BusinessWorkCategory.CustomerService),
            Case("warehouse", "move-goods", "loading-dock", "goods-allocation", "location-inventory", BusinessWorkCategory.ResourceFlow),
            Case("car-dealership", "deliver-vehicle", "delivery-bay", "vehicle-reservation", "sales-ledger", BusinessWorkCategory.CustomerService)
        };

        [TestCaseSource(nameof(ArchitectureCompatibilityCases))]
        public void ArchitectureTargetActionComposesFromSharedRecipeAndStationQueue(
            string businessTypeId,
            string operationName,
            string stationName,
            string resourceName,
            string authorityName,
            BusinessWorkCategory category)
        {
            string operationId = $"operation-{businessTypeId}-{operationName}";
            string stationCapabilityId = $"station-{stationName}";
            string resourceCapabilityId = $"resource-{resourceName}";
            string completionAuthorityId = $"authority-{authorityName}";
            BusinessOperationStep[] steps =
            {
                new(
                    "reserve-capacity",
                    category,
                    "capability-trained-operator",
                    stationCapabilityId,
                    resourceCapabilityId,
                    1,
                    1,
                    true),
                new(
                    "perform-work",
                    category,
                    "capability-trained-operator",
                    stationCapabilityId,
                    resourceCapabilityId,
                    1,
                    3,
                    true)
            };

            Assert.That(
                BusinessOperationRecipe.TryCreate(
                    operationId,
                    completionAuthorityId,
                    steps,
                    out BusinessOperationRecipe recipe,
                    out string error),
                Is.True,
                error);
            Assert.That(recipe.Steps.Count, Is.EqualTo(2));
            Assert.That(recipe.Steps[0].RequiresReservation, Is.True);
            Assert.That(recipe.Steps[1].RequiredWorkUnits, Is.EqualTo(3));
            Assert.That(recipe.CompletionAuthorityId, Is.EqualTo(completionAuthorityId));

            BusinessStationQueue station = new(
                $"station-test-{businessTypeId}",
                stationCapabilityId,
                1);
            Assert.That(
                station.TryEnqueue(
                    $"job-{businessTypeId}-001",
                    recipe.Steps[0].RequiredCapacityUnits,
                    out BusinessStationQueueFailure failure),
                Is.True,
                failure.ToString());
            Assert.That(
                station.TryReserveNext(
                    $"job-{businessTypeId}-001",
                    out failure),
                Is.True,
                failure.ToString());
            Assert.That(station.AvailableCapacityUnits, Is.Zero);
            Assert.That(
                station.TryCompleteReservation(
                    $"job-{businessTypeId}-001",
                    out failure),
                Is.True,
                failure.ToString());
            Assert.That(station.AvailableCapacityUnits, Is.EqualTo(1));
            Assert.That(
                station.TryCompleteReservation(
                    $"job-{businessTypeId}-001",
                    out failure),
                Is.False,
                "A completed job must not release station capacity twice.");
        }

        [Test]
        public void StationQueuePreservesFifoCapacityRollbackAndAbandonment()
        {
            BusinessStationQueue queue = new(
                "station-service-bays",
                "station-service-bay",
                2);
            AssertEnqueued(queue, "job-first", 1);
            AssertEnqueued(queue, "job-second", 1);
            AssertEnqueued(queue, "job-third", 1);

            Assert.That(
                queue.TryReserveNext(
                    "job-second",
                    out BusinessStationQueueFailure failure),
                Is.False);
            Assert.That(failure, Is.EqualTo(BusinessStationQueueFailure.NotFront));
            Assert.That(queue.TryReserveNext("job-first", out failure), Is.True);
            Assert.That(queue.TryReserveNext("job-second", out failure), Is.True);
            Assert.That(queue.TryReserveNext("job-third", out failure), Is.False);
            Assert.That(
                failure,
                Is.EqualTo(BusinessStationQueueFailure.InsufficientAvailableCapacity));

            Assert.That(
                queue.TryReturnReservationToFront("job-second", out failure),
                Is.True);
            Assert.That(queue.FrontWaitingJobId, Is.EqualTo("job-second"));
            Assert.That(queue.GetWaitingPosition("job-third"), Is.EqualTo(1));
            Assert.That(queue.TryAbandon("job-second", out failure), Is.True);
            Assert.That(queue.FrontWaitingJobId, Is.EqualTo("job-third"));
            Assert.That(queue.TryCompleteReservation("job-first", out failure), Is.True);
            Assert.That(queue.TryReserveNext("job-third", out failure), Is.True);
        }

        [Test]
        public void SkillReliabilityFocusAndSupervisorAffectSharedWorkCapacity()
        {
            BusinessWorkCapacityProfile service =
                ConvenienceStoreOperations.Simulation.CustomerServiceCapacity;
            EmployeeWorkProfile weak = new(
                30,
                30,
                BusinessWorkFocus.ResourceFlow);
            EmployeeWorkProfile skilled = new(
                90,
                92,
                BusinessWorkFocus.CustomerService);
            EmployeeWorkProfile reliable = new(
                90,
                100,
                BusinessWorkFocus.CustomerService);
            EmployeeWorkProfile manager = new(
                80,
                90,
                BusinessWorkFocus.CustomerService);

            int weakCapacity = service.CalculateCapacity(weak);
            int skilledCapacity = service.CalculateCapacity(skilled);
            int reliableCapacity = service.CalculateCapacity(reliable);
            int managedCapacity = service.CalculateCapacity(skilled, manager);

            Assert.That(skilledCapacity, Is.GreaterThan(weakCapacity));
            Assert.That(reliableCapacity, Is.GreaterThan(skilledCapacity));
            Assert.That(managedCapacity, Is.GreaterThan(skilledCapacity));
        }

        [Test]
        public void TimedTaskUsesRecipeWorkUnitsAndRestoresWithoutRepeatingCompletion()
        {
            int requiredWorkUnits = ConvenienceStoreOperations
                .RestoreStandards
                .Steps[0]
                .RequiredWorkUnits;
            BusinessTaskProgress task = new(requiredWorkUnits, true);

            Assert.That(
                task.TryApplyWork(2),
                Is.EqualTo(BusinessTaskProgressResult.Progressed));
            BusinessTaskProgress restored = new(requiredWorkUnits, false);
            Assert.That(
                restored.TryRestore(
                    task.CompletedWorkUnits,
                    task.IsActive),
                Is.True);
            Assert.That(
                restored.TryApplyWork(2),
                Is.EqualTo(BusinessTaskProgressResult.Completed));
            Assert.That(
                restored.TryApplyWork(1),
                Is.EqualTo(BusinessTaskProgressResult.AlreadyComplete));
        }

        [Test]
        public void ConvenienceRecipesRouteCompletionToExistingAuthorities()
        {
            Assert.That(
                ConvenienceStoreOperations.CustomerCheckout.CompletionAuthorityId,
                Is.EqualTo("authority-checkout-ledger"));
            Assert.That(
                ConvenienceStoreOperations.RestockShelf.CompletionAuthorityId,
                Is.EqualTo("authority-location-inventory"));
            Assert.That(
                ConvenienceStoreOperations.RestoreStandards.CompletionAuthorityId,
                Is.EqualTo("authority-cleaning-state"));
            Assert.That(
                ConvenienceStoreOperations.Simulation
                    .UnitEconomy
                    .VariableUnitCostCents,
                Is.EqualTo(PortfolioProgressionRules.AggregateUnitCostCents));
        }

        private static object[] Case(
            string businessTypeId,
            string operationName,
            string stationName,
            string resourceName,
            string authorityName,
            BusinessWorkCategory category)
        {
            return new object[]
            {
                businessTypeId,
                operationName,
                stationName,
                resourceName,
                authorityName,
                category
            };
        }

        private static void AssertEnqueued(
            BusinessStationQueue queue,
            string jobId,
            int capacityUnits)
        {
            Assert.That(
                queue.TryEnqueue(
                    jobId,
                    capacityUnits,
                    out BusinessStationQueueFailure failure),
                Is.True,
                failure.ToString());
        }
    }
}
