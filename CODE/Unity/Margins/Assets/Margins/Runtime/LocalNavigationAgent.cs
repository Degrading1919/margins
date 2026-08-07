using UnityEngine;
using UnityEngine.AI;

namespace Margins
{
    public enum LocalNavigationState
    {
        Idle,
        WaitingForNavMesh,
        Travelling,
        Arrived,
        PathUnavailable
    }

    /// <summary>
    /// Reusable detailed-simulation movement only. Callers retain ownership of
    /// actor lifecycle, work, reservations, and the target they want reached.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class LocalNavigationAgent : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private CapsuleCollider physicalCollider;
        [SerializeField, Min(0.01f)] private float arrivalDistance = 0.18f;
        [SerializeField, Min(0.05f)] private float navMeshSampleRadius = 1.25f;
        [SerializeField, Min(0.02f)] private float repathIntervalSeconds = 0.15f;
        [SerializeField, Min(0.01f)] private float targetMovementThreshold = 0.08f;

        private Transform currentTarget;
        private Vector3 lastTargetPosition;
        private Vector3 lastRequestedDestination;
        private float nextRepathAt;
        private bool hasRequestedDestination;

        public NavMeshAgent Agent => navMeshAgent;
        public CapsuleCollider PhysicalCollider => physicalCollider;
        public Transform CurrentTarget => currentTarget;
        public Vector3 LastRequestedDestination => lastRequestedDestination;
        public int RepathCount { get; private set; }
        public LocalNavigationState State { get; private set; }
        public bool HasPath =>
            navMeshAgent != null &&
            navMeshAgent.enabled &&
            navMeshAgent.isOnNavMesh &&
            !navMeshAgent.pathPending &&
            navMeshAgent.hasPath &&
            navMeshAgent.pathStatus == NavMeshPathStatus.PathComplete;
        public bool HasArrived =>
            currentTarget != null &&
            hasRequestedDestination &&
            navMeshAgent != null &&
            navMeshAgent.enabled &&
            navMeshAgent.isOnNavMesh &&
            !navMeshAgent.pathPending &&
            navMeshAgent.pathStatus == NavMeshPathStatus.PathComplete &&
            navMeshAgent.remainingDistance <=
            Mathf.Max(navMeshAgent.stoppingDistance, arrivalDistance);

        private void Awake()
        {
            EnsureComponents();
        }

        private void OnEnable()
        {
            EnsureComponents();
            TryPlaceOnNavMesh();
            ForceRepath();
        }

        private void Update()
        {
            if (currentTarget == null)
            {
                State = LocalNavigationState.Idle;
                return;
            }

            RefreshDestinationIfNeeded();
            if (HasArrived)
            {
                State = LocalNavigationState.Arrived;
            }
        }

        public void Configure(float movementSpeed, int avoidancePriority)
        {
            EnsureComponents();

            navMeshAgent.speed = Mathf.Max(0.1f, movementSpeed);
            navMeshAgent.acceleration = Mathf.Max(8f, navMeshAgent.speed * 5f);
            navMeshAgent.angularSpeed = 540f;
            navMeshAgent.stoppingDistance = arrivalDistance;
            navMeshAgent.radius = 0.3f;
            navMeshAgent.height = 1.7f;
            navMeshAgent.baseOffset = 0f;
            navMeshAgent.autoBraking = true;
            navMeshAgent.autoRepath = true;
            navMeshAgent.autoTraverseOffMeshLink = false;
            navMeshAgent.obstacleAvoidanceType =
                ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            navMeshAgent.avoidancePriority = Mathf.Clamp(avoidancePriority, 0, 99);

            physicalCollider.isTrigger = false;
            physicalCollider.direction = 1;
            physicalCollider.center = new Vector3(0f, 0.83f, 0f);
            physicalCollider.height = 1.66f;
            physicalCollider.radius = 0.28f;
        }

        public bool TryValidateConfiguration(out string error)
        {
            EnsureComponents();
            if (navMeshAgent == null || physicalCollider == null ||
                navMeshAgent.speed <= 0f || navMeshAgent.radius <= 0f ||
                navMeshAgent.height <= 0f || physicalCollider.isTrigger ||
                physicalCollider.radius <= 0f || physicalCollider.height <= 0f)
            {
                error = "Local navigation requires a configured NavMeshAgent and solid capsule collider.";
                return false;
            }

            error = null;
            return true;
        }

        public bool NavigateTo(Transform target)
        {
            if (target == null)
            {
                ClearTarget();
                return false;
            }

            if (currentTarget != target)
            {
                currentTarget = target;
                ForceRepath();
            }

            RefreshDestinationIfNeeded();
            if (HasArrived)
            {
                State = LocalNavigationState.Arrived;
                return true;
            }

            return false;
        }

        public bool HasArrivedAt(Transform target)
        {
            return currentTarget == target && HasArrived;
        }

        public void ClearTarget()
        {
            currentTarget = null;
            hasRequestedDestination = false;
            State = LocalNavigationState.Idle;
            if (navMeshAgent != null && navMeshAgent.enabled &&
                navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.ResetPath();
                navMeshAgent.isStopped = true;
            }
        }

        public void ResetNavigationAfterRestore()
        {
            if (navMeshAgent != null && navMeshAgent.enabled &&
                navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.ResetPath();
            }
            TryPlaceOnNavMesh();
            ForceRepath();
        }

        private void RefreshDestinationIfNeeded()
        {
            if (currentTarget == null || navMeshAgent == null ||
                !navMeshAgent.enabled)
            {
                State = LocalNavigationState.Idle;
                return;
            }

            if (!TryPlaceOnNavMesh())
            {
                State = LocalNavigationState.WaitingForNavMesh;
                return;
            }

            Vector3 targetPosition = currentTarget.position;
            bool targetMoved = !hasRequestedDestination ||
                               (targetPosition - lastTargetPosition).sqrMagnitude >=
                               targetMovementThreshold * targetMovementThreshold;
            if (!targetMoved && HasArrived)
            {
                navMeshAgent.isStopped = true;
                State = LocalNavigationState.Arrived;
                return;
            }

            bool pathNeedsRepair = !navMeshAgent.pathPending &&
                                   (!navMeshAgent.hasPath ||
                                    navMeshAgent.pathStatus !=
                                    NavMeshPathStatus.PathComplete);
            if (!targetMoved && !pathNeedsRepair)
            {
                return;
            }
            if (Time.unscaledTime < nextRepathAt)
            {
                return;
            }

            nextRepathAt = Time.unscaledTime + repathIntervalSeconds;
            if (!NavMesh.SamplePosition(
                    targetPosition,
                    out NavMeshHit hit,
                    navMeshSampleRadius,
                    NavMesh.AllAreas))
            {
                State = LocalNavigationState.PathUnavailable;
                return;
            }

            lastTargetPosition = targetPosition;
            lastRequestedDestination = hit.position;
            hasRequestedDestination = true;
            navMeshAgent.isStopped = false;
            if (navMeshAgent.SetDestination(hit.position))
            {
                RepathCount++;
                State = LocalNavigationState.Travelling;
            }
            else
            {
                State = LocalNavigationState.PathUnavailable;
            }
        }

        private bool TryPlaceOnNavMesh()
        {
            if (navMeshAgent == null || !navMeshAgent.enabled)
            {
                return false;
            }

            if (navMeshAgent.isOnNavMesh)
            {
                return true;
            }

            if (!NavMesh.SamplePosition(
                    transform.position,
                    out NavMeshHit hit,
                    navMeshSampleRadius,
                    NavMesh.AllAreas))
            {
                return false;
            }

            return navMeshAgent.Warp(hit.position);
        }

        private void ForceRepath()
        {
            hasRequestedDestination = false;
            nextRepathAt = 0f;
        }

        private void EnsureComponents()
        {
            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponent<NavMeshAgent>();
            }
            if (physicalCollider == null)
            {
                physicalCollider = GetComponent<CapsuleCollider>();
            }
        }
    }
}
