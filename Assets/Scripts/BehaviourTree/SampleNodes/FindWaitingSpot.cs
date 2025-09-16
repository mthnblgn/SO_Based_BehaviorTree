using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "Behavior Tree/Action/Find Waiting Spot", fileName = "FindWaitingSpot")]
public class FindWaitingSpot : NPCActionNode
{
    [Tooltip("Arrival acceptance threshold (meters)")]
    public float arrivalThreshold = 0.25f;

    [Tooltip("Ignore queue rules and directly reserve a free spot")]

    private NPCController controller;
    private AreaController areaController;
    private NavMeshAgent nmAgent;

    protected override void OnStart(GameObject agent)
    {
        controller = GetController(agent);
        areaController = controller != null ? controller.GetCurrentArea() : null;
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
    }

    protected override NodeState OnUpdate(GameObject agent)
    {
        if (controller == null || nmAgent == null) return NodeState.FAILURE;

        // If Area is missing, find and assign
        if (areaController == null)
        {
            areaController = controller.GetCurrentArea();
            if (areaController == null)
            {
                areaController = Object.FindFirstObjectByType<AreaController>();
                if (areaController != null) controller.SetCurrentArea(areaController);
            }
            if (areaController == null) return NodeState.RUNNING;
        }

        // If an assigned spot already exists, go to it
        var currentSpot = controller.GetAssignedWaitingSpot();
        if (currentSpot == null)
        {
            // Try to reserve (optionally bypassing queue rules)
            var spot = areaController.TryReserveAnySpot(controller);

            if (spot == null)
            {
                return NodeState.RUNNING; // no suitable spot
            }

            controller.AssignWaitingSpot(spot);
            currentSpot = spot;

            // start movement
            var p = currentSpot.SpotTransform.position;
            controller.SetTargetPosition(p);
            nmAgent.isStopped = false;
            nmAgent.SetDestination(p);
            return NodeState.RUNNING;
        }

        // keep destination updated
        var targetPos = currentSpot.SpotTransform.position;
        controller.SetTargetPosition(targetPos);
        nmAgent.isStopped = false;
        if (!nmAgent.pathPending && (nmAgent.destination - targetPos).sqrMagnitude > 0.01f)
            nmAgent.SetDestination(targetPos);

        // Arrival check
        if (controller.HasArrived(nmAgent, arrivalThreshold))
        {
            return NodeState.SUCCESS;
        }

        return NodeState.RUNNING;
    }


}