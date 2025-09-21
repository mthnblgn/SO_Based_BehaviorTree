using UnityEngine;
using UnityEngine.AI;
using Game.Patterns;

[RequireComponent(typeof(NavMeshAgent), typeof(BehaviorTreeRunner))]
public class NPCController : MonoBehaviour
{
    [Header("AI Components")]
    private NavMeshAgent navMeshAgent;
    private AreaController currentArea;
    private WaitingSpot assignedWaitingSpot;
    private Vector3 targetPosition;
    private ObjectPool<NPCController> pool;

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    /// <summary>
    /// Ensure NavMeshAgent is enabled and its transform is close enough to a NavMesh. If off-mesh,
    /// try to snap the transform onto the nearest NavMesh within maxDistance, then enable the agent.
    /// </summary>
    public bool EnsureNavAgentOnNavMesh(float maxDistance = 2f)
    {
        if (navMeshAgent == null) return false;
        if (navMeshAgent.enabled && navMeshAgent.isOnNavMesh) return true;

        // Try to snap transform first, then enable
        if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out var hit, maxDistance, UnityEngine.AI.NavMesh.AllAreas))
        {
            transform.position = hit.position;
            // Now try enabling
            navMeshAgent.enabled = true;
            return navMeshAgent.isOnNavMesh;
        }

        return false;
    }

    public void SetTargetPosition(Vector3 position)
    {
        targetPosition = position;
        if (navMeshAgent == null) return;

        // Make sure agent is valid before calling SetDestination
        if (!navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
        {
            if (!EnsureNavAgentOnNavMesh(3f))
            {
                // Can't set destination right now
                return;
            }
        }
        navMeshAgent.SetDestination(targetPosition);
    }

    public AreaController GetCurrentArea()
    {
        return currentArea;
    }

    public void SetCurrentArea(AreaController area)
    {
        currentArea = area;
    }

    public void SetPool(ObjectPool<NPCController> poolRef)
    {
        pool = poolRef;
    }

    public void Despawn()
    {
        if (pool != null)
        {
            // Stop movement and return to pool
            if (navMeshAgent != null)
            {
                if (navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.ResetPath();
                    navMeshAgent.velocity = Vector3.zero;
                }
                // Disable while pooled to avoid off-mesh enable errors
                navMeshAgent.enabled = false;
            }
            // Also disable BT runner so it doesn't tick while pooled
            var runner = GetComponent<BehaviorTreeRunner>();
            if (runner != null)
            {
                runner.enabled = false;
            }
            pool.ReturnToPool(this);
        }
        else
        {
            // Fallback: disable if no pool assigned
            gameObject.SetActive(false);
        }
    }

    public void ReleaseWaitingPoint()
    {
        // Logic to release the waiting point
        if (assignedWaitingSpot != null)
        {
            assignedWaitingSpot.Release(this);
            assignedWaitingSpot = null;
        }
    }

    internal void AssignWaitingSpot(WaitingSpot spot)
    {
        assignedWaitingSpot = spot;
    }

    public WaitingSpot GetAssignedWaitingSpot()
    {
        return assignedWaitingSpot;
    }
    public bool HasArrived(NavMeshAgent nav, float arrivalThreshold)
    {
        if (nav == null) return false;
        if (!nav.enabled || !nav.isOnNavMesh) return false;
        if (!nav.pathPending && nav.remainingDistance <= arrivalThreshold)
        {
            return true;
        }
        return false;
    }
}

