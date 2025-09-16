using UnityEngine;

[CreateAssetMenu(menuName = "Behavior Tree/Action/Find Waiting Spot", fileName = "FindWaitingSpot")]
public class FindWaitingSpot : NPCActionNode
{
    private NPCController controller;
    private AreaController areaController;

    protected override void OnStart(GameObject agent)
    {
        controller = GetController(agent);
        areaController = controller != null ? controller.GetCurrentArea() : null;
    }

    protected override NodeState OnUpdate(GameObject agent)
    {
        if (controller == null) return NodeState.FAILURE;

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

        // If an assigned spot already exists, job's already done
        var currentSpot = controller.GetAssignedWaitingSpot();
        if (currentSpot != null)
        {
            // Optionally keep the target position updated
            controller.SetTargetPosition(currentSpot.SpotTransform.position);
            return NodeState.SUCCESS;
        }

        // Try to reserve any spot
        var spot = areaController.TryReserveAnySpot(controller);
        if (spot == null)
        {
            return NodeState.RUNNING; // no suitable spot right now, retry later
        }

        controller.AssignWaitingSpot(spot);
        return NodeState.SUCCESS;
    }
}