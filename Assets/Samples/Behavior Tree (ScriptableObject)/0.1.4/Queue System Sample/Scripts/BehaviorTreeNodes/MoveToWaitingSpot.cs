using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "Behavior Tree/Action/Move To Waiting Spot", fileName = "MoveToWaitingSpot")]
public class MoveToWaitingSpot : NPCActionNode
{
    [Tooltip("Arrival acceptance threshold (meters)")]
    public float arrivalThreshold = 0.25f;

    private NPCController controller;
    private NavMeshAgent nmAgent;

    protected override void OnStart(GameObject agent)
    {
        controller = GetController(agent);
        nmAgent = agent.GetComponent<NavMeshAgent>();

        // NavMesh safety
        if (nmAgent != null && nmAgent.enabled)
        {
            if (!nmAgent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(agent.transform.position, out var hit, 2f, NavMesh.AllAreas))
                {
                    nmAgent.Warp(hit.position);
                }
            }
        }

        // If there is a target at start, set destination
        var spot = controller != null ? controller.GetAssignedWaitingSpot() : null;
        if (spot != null && nmAgent != null && nmAgent.enabled)
        {
            var p = spot.SpotTransform.position;
            controller.SetTargetPosition(p);
            nmAgent.isStopped = false;
            nmAgent.SetDestination(p);
        }
    }

    protected override NodeState OnUpdate(GameObject agent)
    {
        if (controller == null || nmAgent == null || !nmAgent.enabled) return NodeState.FAILURE;

        var currentSpot = controller.GetAssignedWaitingSpot();
        if (currentSpot == null)
        {
            // Missing precondition; FindWaitingSpot should run again within the Sequence
            return NodeState.FAILURE;
        }

        // Keep the target updated
        var targetPos = currentSpot.SpotTransform.position;
        controller.SetTargetPosition(targetPos);
        nmAgent.isStopped = false;

        if (!nmAgent.pathPending)
        {
            var dest = nmAgent.destination;
            if ((dest - targetPos).sqrMagnitude > 0.01f)
            {
                nmAgent.SetDestination(targetPos);
            }
        }

        // Arrival check
        if (controller.HasArrived(nmAgent, arrivalThreshold))
        {
            return NodeState.SUCCESS;
        }

        return NodeState.RUNNING;
    }
}