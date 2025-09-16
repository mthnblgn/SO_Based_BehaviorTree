using UnityEngine;
using UnityEngine.AI;
using Game.Paterns;
using System;

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

    public void SetTargetPosition(Vector3 position)
    {
        targetPosition = position;
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
                navMeshAgent.ResetPath();
                navMeshAgent.velocity = Vector3.zero;
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
        if (!nav.pathPending && nav.remainingDistance <= arrivalThreshold)
        {
            return true;
        }
        return false;
    }
}

