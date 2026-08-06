using System;
using System.Collections.Generic;

namespace Margins
{
    /// <summary>
    /// Broad work lanes shared by detailed actions and aggregate throughput.
    /// Business-specific roles remain presentation and staffing configuration.
    /// </summary>
    public enum BusinessWorkCategory
    {
        CustomerService = 0,
        ResourceFlow = 1,
        Standards = 2
    }

    public enum BusinessWorkFocus
    {
        Balanced = 0,
        CustomerService = 1,
        ResourceFlow = 2,
        Standards = 3
    }

    /// <summary>
    /// Describes one typed step without taking authority over the resource,
    /// station, transaction, or maintenance system that performs it.
    /// </summary>
    public sealed class BusinessOperationStep
    {
        public BusinessOperationStep(
            string stepId,
            BusinessWorkCategory workCategory,
            string workerCapabilityId,
            string stationCapabilityId,
            string resourceCapabilityId,
            int requiredCapacityUnits,
            int requiredWorkUnits,
            bool requiresReservation)
        {
            StepId = stepId;
            WorkCategory = workCategory;
            WorkerCapabilityId = workerCapabilityId;
            StationCapabilityId = stationCapabilityId;
            ResourceCapabilityId = resourceCapabilityId;
            RequiredCapacityUnits = requiredCapacityUnits;
            RequiredWorkUnits = requiredWorkUnits;
            RequiresReservation = requiresReservation;
        }

        public string StepId { get; }
        public BusinessWorkCategory WorkCategory { get; }
        public string WorkerCapabilityId { get; }
        public string StationCapabilityId { get; }
        public string ResourceCapabilityId { get; }
        public int RequiredCapacityUnits { get; }
        public int RequiredWorkUnits { get; }
        public bool RequiresReservation { get; }

        internal bool TryValidate(out string error)
        {
            if (!StableIdentifier.IsValid(StepId) ||
                !StableIdentifier.IsValid(WorkerCapabilityId) ||
                !StableIdentifier.IsValid(StationCapabilityId) ||
                !StableIdentifier.IsValid(ResourceCapabilityId) ||
                !Enum.IsDefined(typeof(BusinessWorkCategory), WorkCategory) ||
                RequiredCapacityUnits <= 0 || RequiredWorkUnits <= 0)
            {
                error = "Operation steps require stable capability ids and positive capacity and work units.";
                return false;
            }

            error = null;
            return true;
        }
    }

    /// <summary>
    /// Ordered configuration for a business action. Concrete adapters still
    /// invoke the existing authoritative systems named by CompletionAuthorityId.
    /// </summary>
    public sealed class BusinessOperationRecipe
    {
        private readonly List<BusinessOperationStep> steps;
        private readonly IReadOnlyList<BusinessOperationStep> readOnlySteps;

        private BusinessOperationRecipe(
            string operationId,
            string completionAuthorityId,
            List<BusinessOperationStep> steps)
        {
            OperationId = operationId;
            CompletionAuthorityId = completionAuthorityId;
            this.steps = steps;
            readOnlySteps = steps.AsReadOnly();
        }

        public string OperationId { get; }
        public string CompletionAuthorityId { get; }
        public IReadOnlyList<BusinessOperationStep> Steps => readOnlySteps;
        public BusinessWorkCategory PrimaryWorkCategory => steps[0].WorkCategory;

        public static bool TryCreate(
            string operationId,
            string completionAuthorityId,
            IEnumerable<BusinessOperationStep> steps,
            out BusinessOperationRecipe recipe,
            out string error)
        {
            recipe = null;
            error = null;
            if (!StableIdentifier.IsValid(operationId) ||
                !StableIdentifier.IsValid(completionAuthorityId) ||
                steps == null)
            {
                error = "Operation recipes require stable operation and completion-authority ids.";
                return false;
            }

            List<BusinessOperationStep> captured = new();
            HashSet<string> stepIds = new(StringComparer.Ordinal);
            foreach (BusinessOperationStep step in steps)
            {
                if (step == null || !step.TryValidate(out error) ||
                    !stepIds.Add(step.StepId))
                {
                    error ??= "Operation recipes cannot contain duplicate step ids.";
                    return false;
                }
                captured.Add(step);
            }

            if (captured.Count == 0)
            {
                error = "Operation recipes require at least one ordered step.";
                return false;
            }

            recipe = new BusinessOperationRecipe(
                operationId,
                completionAuthorityId,
                captured);
            return true;
        }
    }

    public enum BusinessStationQueueFailure
    {
        None = 0,
        InvalidJob = 1,
        InvalidCapacity = 2,
        DuplicateJob = 3,
        QueueEmpty = 4,
        NotFront = 5,
        InsufficientAvailableCapacity = 6,
        NotReserved = 7,
        NotFound = 8
    }

    /// <summary>
    /// Owns deterministic FIFO order and capacity reservations for a station.
    /// It does not own customer, equipment, resource, or transaction state.
    /// </summary>
    public sealed class BusinessStationQueue
    {
        private sealed class CapacityRequest
        {
            public CapacityRequest(string jobId, int capacityUnits)
            {
                JobId = jobId;
                CapacityUnits = capacityUnits;
            }

            public string JobId { get; }
            public int CapacityUnits { get; }
        }

        private readonly List<CapacityRequest> waiting = new();
        private readonly SortedDictionary<string, int> reservations =
            new(StringComparer.Ordinal);
        private int reservedCapacityUnits;

        public BusinessStationQueue(
            string stationId,
            string stationCapabilityId,
            int capacityUnits)
        {
            if (!StableIdentifier.IsValid(stationId) ||
                !StableIdentifier.IsValid(stationCapabilityId) ||
                capacityUnits <= 0)
            {
                throw new ArgumentException(
                    "Station queues require stable ids and positive capacity.");
            }

            StationId = stationId;
            StationCapabilityId = stationCapabilityId;
            CapacityUnits = capacityUnits;
        }

        public string StationId { get; }
        public string StationCapabilityId { get; }
        public int CapacityUnits { get; }
        public int WaitingCount => waiting.Count;
        public int ReservationCount => reservations.Count;
        public int AvailableCapacityUnits => CapacityUnits - reservedCapacityUnits;
        public string FrontWaitingJobId => waiting.Count == 0
            ? null
            : waiting[0].JobId;

        public IReadOnlyList<string> WaitingJobIds
        {
            get
            {
                List<string> ids = new(waiting.Count);
                foreach (CapacityRequest request in waiting)
                {
                    ids.Add(request.JobId);
                }
                return ids;
            }
        }

        public bool TryEnqueue(
            string jobId,
            int requiredCapacityUnits,
            out BusinessStationQueueFailure failure)
        {
            if (!StableIdentifier.IsValid(jobId))
            {
                failure = BusinessStationQueueFailure.InvalidJob;
                return false;
            }
            if (requiredCapacityUnits <= 0 ||
                requiredCapacityUnits > CapacityUnits)
            {
                failure = BusinessStationQueueFailure.InvalidCapacity;
                return false;
            }
            if (Contains(jobId))
            {
                failure = BusinessStationQueueFailure.DuplicateJob;
                return false;
            }

            waiting.Add(new CapacityRequest(jobId, requiredCapacityUnits));
            failure = BusinessStationQueueFailure.None;
            return true;
        }

        public bool TryReserveNext(
            string expectedJobId,
            out BusinessStationQueueFailure failure)
        {
            if (waiting.Count == 0)
            {
                failure = BusinessStationQueueFailure.QueueEmpty;
                return false;
            }

            CapacityRequest next = waiting[0];
            if (!string.Equals(
                    next.JobId,
                    expectedJobId,
                    StringComparison.Ordinal))
            {
                failure = BusinessStationQueueFailure.NotFront;
                return false;
            }
            if (next.CapacityUnits > AvailableCapacityUnits)
            {
                failure = BusinessStationQueueFailure.InsufficientAvailableCapacity;
                return false;
            }

            waiting.RemoveAt(0);
            reservations.Add(next.JobId, next.CapacityUnits);
            reservedCapacityUnits += next.CapacityUnits;
            failure = BusinessStationQueueFailure.None;
            return true;
        }

        public bool TryCompleteReservation(
            string jobId,
            out BusinessStationQueueFailure failure)
        {
            if (!reservations.TryGetValue(jobId, out int capacityUnits))
            {
                failure = BusinessStationQueueFailure.NotReserved;
                return false;
            }

            reservations.Remove(jobId);
            reservedCapacityUnits -= capacityUnits;
            failure = BusinessStationQueueFailure.None;
            return true;
        }

        public bool TryReturnReservationToFront(
            string jobId,
            out BusinessStationQueueFailure failure)
        {
            if (!reservations.TryGetValue(jobId, out int capacityUnits))
            {
                failure = BusinessStationQueueFailure.NotReserved;
                return false;
            }

            reservations.Remove(jobId);
            reservedCapacityUnits -= capacityUnits;
            waiting.Insert(0, new CapacityRequest(jobId, capacityUnits));
            failure = BusinessStationQueueFailure.None;
            return true;
        }

        public bool TryAbandon(
            string jobId,
            out BusinessStationQueueFailure failure)
        {
            for (int index = 0; index < waiting.Count; index++)
            {
                if (!string.Equals(
                        waiting[index].JobId,
                        jobId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                waiting.RemoveAt(index);
                failure = BusinessStationQueueFailure.None;
                return true;
            }

            if (reservations.TryGetValue(jobId, out int capacityUnits))
            {
                reservations.Remove(jobId);
                reservedCapacityUnits -= capacityUnits;
                failure = BusinessStationQueueFailure.None;
                return true;
            }

            failure = BusinessStationQueueFailure.NotFound;
            return false;
        }

        public int GetWaitingPosition(string jobId)
        {
            for (int index = 0; index < waiting.Count; index++)
            {
                if (string.Equals(
                        waiting[index].JobId,
                        jobId,
                        StringComparison.Ordinal))
                {
                    return index;
                }
            }
            return -1;
        }

        public bool HasReservation(string jobId)
        {
            return StableIdentifier.IsValid(jobId) &&
                   reservations.ContainsKey(jobId);
        }

        public void Clear()
        {
            waiting.Clear();
            reservations.Clear();
            reservedCapacityUnits = 0;
        }

        private bool Contains(string jobId)
        {
            if (reservations.ContainsKey(jobId))
            {
                return true;
            }

            foreach (CapacityRequest request in waiting)
            {
                if (string.Equals(
                        request.JobId,
                        jobId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public enum BusinessTaskProgressResult
    {
        Progressed = 0,
        Completed = 1,
        AlreadyComplete = 2,
        InvalidAmount = 3
    }

    /// <summary>
    /// Small state holder for interruptible work and timed processes. The
    /// caller's snapshot remains the persistence authority.
    /// </summary>
    public sealed class BusinessTaskProgress
    {
        public BusinessTaskProgress(
            int requiredWorkUnits,
            bool startsActive)
        {
            if (requiredWorkUnits <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredWorkUnits));
            }

            RequiredWorkUnits = requiredWorkUnits;
            IsActive = startsActive;
        }

        public int RequiredWorkUnits { get; }
        public int CompletedWorkUnits { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsComplete =>
            !IsActive || CompletedWorkUnits >= RequiredWorkUnits;

        public BusinessTaskProgressResult TryApplyWork(int workUnits)
        {
            if (IsComplete)
            {
                return BusinessTaskProgressResult.AlreadyComplete;
            }
            if (workUnits <= 0)
            {
                return BusinessTaskProgressResult.InvalidAmount;
            }

            int remaining = RequiredWorkUnits - CompletedWorkUnits;
            CompletedWorkUnits += Math.Min(remaining, workUnits);
            return IsComplete
                ? BusinessTaskProgressResult.Completed
                : BusinessTaskProgressResult.Progressed;
        }

        public bool TryActivate()
        {
            if (IsActive && !IsComplete)
            {
                return false;
            }

            IsActive = true;
            CompletedWorkUnits = 0;
            return true;
        }

        public bool TryRestore(int completedWorkUnits, bool isActive)
        {
            if (completedWorkUnits < 0 ||
                completedWorkUnits > RequiredWorkUnits)
            {
                return false;
            }

            CompletedWorkUnits = completedWorkUnits;
            IsActive = isActive;
            return true;
        }
    }

    public readonly struct EmployeeWorkProfile
    {
        public EmployeeWorkProfile(
            int skill,
            int reliability,
            BusinessWorkFocus focus)
        {
            Skill = skill;
            Reliability = reliability;
            Focus = focus;
        }

        public int Skill { get; }
        public int Reliability { get; }
        public BusinessWorkFocus Focus { get; }
        public bool IsValid =>
            Skill >= 0 && Skill <= 100 &&
            Reliability >= 0 && Reliability <= 100 &&
            Enum.IsDefined(typeof(BusinessWorkFocus), Focus);
    }

    /// <summary>
    /// Configures aggregate work throughput without embedding a business role.
    /// </summary>
    public sealed class BusinessWorkCapacityProfile
    {
        public BusinessWorkCapacityProfile(
            BusinessWorkCategory workCategory,
            int baseCapacityUnits,
            int skillCapacityPerPoint,
            int reliabilityCapacityPerPoint,
            int matchingFocusBonusUnits,
            int supervisorCompetenceBonusUnits,
            int supervisorMatchingFocusBonusUnits)
        {
            if (!Enum.IsDefined(typeof(BusinessWorkCategory), workCategory) ||
                baseCapacityUnits < 0 || skillCapacityPerPoint < 0 ||
                reliabilityCapacityPerPoint < 0 ||
                matchingFocusBonusUnits < 0 ||
                supervisorCompetenceBonusUnits < 0 ||
                supervisorMatchingFocusBonusUnits < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(baseCapacityUnits),
                    "Work-capacity configuration cannot be negative.");
            }

            WorkCategory = workCategory;
            BaseCapacityUnits = baseCapacityUnits;
            SkillCapacityPerPoint = skillCapacityPerPoint;
            ReliabilityCapacityPerPoint = reliabilityCapacityPerPoint;
            MatchingFocusBonusUnits = matchingFocusBonusUnits;
            SupervisorCompetenceBonusUnits = supervisorCompetenceBonusUnits;
            SupervisorMatchingFocusBonusUnits =
                supervisorMatchingFocusBonusUnits;
        }

        public BusinessWorkCategory WorkCategory { get; }
        public int BaseCapacityUnits { get; }
        public int SkillCapacityPerPoint { get; }
        public int ReliabilityCapacityPerPoint { get; }
        public int MatchingFocusBonusUnits { get; }
        public int SupervisorCompetenceBonusUnits { get; }
        public int SupervisorMatchingFocusBonusUnits { get; }

        public int CalculateCapacity(
            EmployeeWorkProfile worker,
            EmployeeWorkProfile? supervisor = null)
        {
            if (!worker.IsValid ||
                (supervisor.HasValue && !supervisor.Value.IsValid))
            {
                throw new ArgumentException("Employee work profiles are invalid.");
            }

            long capacity = BaseCapacityUnits;
            capacity += (long)worker.Skill * SkillCapacityPerPoint;
            capacity += (long)worker.Reliability * ReliabilityCapacityPerPoint;
            if (EmployeeWorkPerformance.FocusMatches(
                    worker.Focus,
                    WorkCategory))
            {
                capacity += MatchingFocusBonusUnits;
            }

            if (supervisor.HasValue)
            {
                EmployeeWorkProfile manager = supervisor.Value;
                capacity += (long)Math.Round(
                    EmployeeWorkPerformance.Competence(manager) *
                    SupervisorCompetenceBonusUnits,
                    MidpointRounding.AwayFromZero);
                if (EmployeeWorkPerformance.FocusMatches(
                        manager.Focus,
                        WorkCategory))
                {
                    capacity += SupervisorMatchingFocusBonusUnits;
                }
            }

            return capacity >= int.MaxValue
                ? int.MaxValue
                : (int)capacity;
        }
    }

    public sealed class BusinessSimulationProfile
    {
        public BusinessSimulationProfile(
            string profileId,
            BusinessUnitEconomyProfile unitEconomy,
            BusinessWorkCapacityProfile customerServiceCapacity,
            BusinessWorkCapacityProfile resourceFlowCapacity)
        {
            if (!StableIdentifier.IsValid(profileId) ||
                unitEconomy == null ||
                customerServiceCapacity == null ||
                resourceFlowCapacity == null ||
                customerServiceCapacity.WorkCategory !=
                    BusinessWorkCategory.CustomerService ||
                resourceFlowCapacity.WorkCategory !=
                    BusinessWorkCategory.ResourceFlow)
            {
                throw new ArgumentException(
                    "Simulation profiles require stable ids and service and resource-flow capacities.");
            }

            ProfileId = profileId;
            UnitEconomy = unitEconomy;
            CustomerServiceCapacity = customerServiceCapacity;
            ResourceFlowCapacity = resourceFlowCapacity;
        }

        public string ProfileId { get; }
        public BusinessUnitEconomyProfile UnitEconomy { get; }
        public BusinessWorkCapacityProfile CustomerServiceCapacity { get; }
        public BusinessWorkCapacityProfile ResourceFlowCapacity { get; }
    }

    /// <summary>
    /// Per-completion economics used by the current aggregate model. Businesses
    /// with genuinely different economics can provide a small specialized rule.
    /// </summary>
    public sealed class BusinessUnitEconomyProfile
    {
        public BusinessUnitEconomyProfile(
            long variableUnitCostCents,
            long valuePriceCents,
            long balancedPriceCents,
            long premiumPriceCents,
            int valueDemandAdjustmentUnits,
            int premiumDemandAdjustmentUnits)
        {
            if (variableUnitCostCents < 0 || valuePriceCents <= 0 ||
                balancedPriceCents <= 0 || premiumPriceCents <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(variableUnitCostCents),
                    "Per-completion costs cannot be negative and prices must be positive.");
            }

            VariableUnitCostCents = variableUnitCostCents;
            ValuePriceCents = valuePriceCents;
            BalancedPriceCents = balancedPriceCents;
            PremiumPriceCents = premiumPriceCents;
            ValueDemandAdjustmentUnits = valueDemandAdjustmentUnits;
            PremiumDemandAdjustmentUnits = premiumDemandAdjustmentUnits;
        }

        public long VariableUnitCostCents { get; }
        public long ValuePriceCents { get; }
        public long BalancedPriceCents { get; }
        public long PremiumPriceCents { get; }
        public int ValueDemandAdjustmentUnits { get; }
        public int PremiumDemandAdjustmentUnits { get; }
    }

    public static class EmployeeWorkPerformance
    {
        public static double Competence(EmployeeWorkProfile employee)
        {
            if (!employee.IsValid)
            {
                throw new ArgumentException("Employee work profile is invalid.");
            }

            return employee.Skill / 100d * 0.55d +
                   employee.Reliability / 100d * 0.45d;
        }

        public static float CalculateDetailedActionSeconds(
            EmployeeWorkProfile employee,
            BusinessWorkCategory workCategory,
            EmployeeWorkProfile? supervisor = null)
        {
            if (!Enum.IsDefined(typeof(BusinessWorkCategory), workCategory) ||
                (supervisor.HasValue && !supervisor.Value.IsValid))
            {
                throw new ArgumentException("Detailed work inputs are invalid.");
            }

            double competence = Competence(employee);
            double delay = 2.35d + (0.72d - 2.35d) * competence;
            if (FocusMatches(employee.Focus, workCategory))
            {
                delay *= 0.86d;
            }
            else if (employee.Focus == BusinessWorkFocus.Balanced)
            {
                delay *= 0.94d;
            }

            if (supervisor.HasValue)
            {
                double managerCompetence = Competence(supervisor.Value);
                delay *= 0.9d + (0.7d - 0.9d) * managerCompetence;
            }

            return (float)Math.Max(0.35d, delay);
        }

        public static bool FocusMatches(
            BusinessWorkFocus focus,
            BusinessWorkCategory category)
        {
            return (focus == BusinessWorkFocus.CustomerService &&
                    category == BusinessWorkCategory.CustomerService) ||
                   (focus == BusinessWorkFocus.ResourceFlow &&
                    category == BusinessWorkCategory.ResourceFlow) ||
                   (focus == BusinessWorkFocus.Standards &&
                    category == BusinessWorkCategory.Standards);
        }
    }

    /// <summary>
    /// First-store configuration expressed through the shared operation boundary.
    /// Specialized controllers still perform every physical and transactional step.
    /// </summary>
    public static class ConvenienceStoreOperations
    {
        public static readonly BusinessOperationRecipe CustomerCheckout =
            CreateRecipe(
                "operation-customer-checkout",
                "authority-checkout-ledger",
                new BusinessOperationStep(
                    "reserve-register",
                    BusinessWorkCategory.CustomerService,
                    "capability-customer-service",
                    "station-point-of-sale",
                    "resource-customer-job",
                    1,
                    1,
                    true),
                new BusinessOperationStep(
                    "scan-physical-item",
                    BusinessWorkCategory.CustomerService,
                    "capability-customer-service",
                    "station-point-of-sale",
                    "resource-physical-product",
                    1,
                    1,
                    true),
                new BusinessOperationStep(
                    "complete-sale",
                    BusinessWorkCategory.CustomerService,
                    "capability-customer-service",
                    "station-point-of-sale",
                    "resource-customer-job",
                    1,
                    1,
                    true));

        public static readonly BusinessOperationRecipe RestockShelf =
            CreateRecipe(
                "operation-restock-shelf",
                "authority-location-inventory",
                new BusinessOperationStep(
                    "receive-container",
                    BusinessWorkCategory.ResourceFlow,
                    "capability-resource-handling",
                    "station-receiving-area",
                    "resource-delivery-container",
                    1,
                    1,
                    true),
                new BusinessOperationStep(
                    "move-physical-unit",
                    BusinessWorkCategory.ResourceFlow,
                    "capability-resource-handling",
                    "station-stock-route",
                    "resource-physical-product",
                    1,
                    1,
                    false),
                new BusinessOperationStep(
                    "stock-fixture",
                    BusinessWorkCategory.ResourceFlow,
                    "capability-resource-handling",
                    "station-retail-fixture",
                    "resource-physical-product",
                    1,
                    1,
                    true));

        public static readonly BusinessOperationRecipe RestoreStandards =
            CreateRecipe(
                "operation-restore-standards",
                "authority-cleaning-state",
                new BusinessOperationStep(
                    "clean-work-area",
                    BusinessWorkCategory.Standards,
                    "capability-standards-work",
                    "station-maintenance-area",
                    "resource-cleaning-condition",
                    1,
                    4,
                    true));

        public static readonly BusinessSimulationProfile Simulation = new(
            "simulation-convenience-retail",
            new BusinessUnitEconomyProfile(
                135,
                299,
                349,
                419,
                55,
                -70),
            new BusinessWorkCapacityProfile(
                BusinessWorkCategory.CustomerService,
                120,
                2,
                1,
                35,
                100,
                45),
            new BusinessWorkCapacityProfile(
                BusinessWorkCategory.ResourceFlow,
                140,
                3,
                1,
                50,
                100,
                55));

        private static BusinessOperationRecipe CreateRecipe(
            string operationId,
            string completionAuthorityId,
            params BusinessOperationStep[] steps)
        {
            if (!BusinessOperationRecipe.TryCreate(
                    operationId,
                    completionAuthorityId,
                    steps,
                    out BusinessOperationRecipe recipe,
                    out string error))
            {
                throw new InvalidOperationException(error);
            }
            return recipe;
        }
    }
}
