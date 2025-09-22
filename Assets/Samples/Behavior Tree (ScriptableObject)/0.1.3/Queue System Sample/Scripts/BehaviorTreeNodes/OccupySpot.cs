using UnityEngine;

[CreateAssetMenu(menuName = "Behavior Tree/Action/Occupy Spot", fileName = "OccupySpot")]
public class OccupySpot : NPCActionNode
{
    protected override NodeState OnUpdate(GameObject agent)
    {
        var controller = GetController(agent);
        if (controller == null) return NodeState.FAILURE;

        var spot = controller.GetAssignedWaitingSpot();
        if (spot == null) return NodeState.FAILURE;

        if (spot.Occupy(controller))
        {
            return NodeState.SUCCESS;
        }
        return NodeState.FAILURE;
    }
}
