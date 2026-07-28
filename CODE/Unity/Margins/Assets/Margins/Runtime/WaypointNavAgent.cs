using UnityEngine;
using UnityEngine.AI;

namespace Margins
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class WaypointNavAgent : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private Transform pointA;
        [SerializeField] private Transform pointB;
        [SerializeField, Min(0.01f)] private float arrivalDistance = 0.15f;

        private Transform currentTarget;

        public NavMeshAgent Agent => navMeshAgent;
        public Transform PointA => pointA;
        public Transform PointB => pointB;

        private void Awake()
        {
            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponent<NavMeshAgent>();
            }
        }

        private void Start()
        {
            if (navMeshAgent != null && !navMeshAgent.enabled)
            {
                navMeshAgent.enabled = true;
            }
            SetTarget(pointB);
        }

        private void Update()
        {
            if (navMeshAgent == null || !navMeshAgent.isOnNavMesh || currentTarget == null || navMeshAgent.pathPending)
            {
                return;
            }

            if (navMeshAgent.remainingDistance <= Mathf.Max(navMeshAgent.stoppingDistance, arrivalDistance))
            {
                SetTarget(currentTarget == pointA ? pointB : pointA);
            }
        }

        private void SetTarget(Transform target)
        {
            currentTarget = target;
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh && currentTarget != null)
            {
                navMeshAgent.SetDestination(currentTarget.position);
            }
        }
    }
}
