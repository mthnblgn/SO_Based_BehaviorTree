using UnityEngine;

[CreateAssetMenu(menuName = "Behavior Tree/Action/Release Spot", fileName = "ReleaseSpot")]
public class ReleaseSpot : NPCActionNode
{
    protected override NodeState OnUpdate(GameObject agent)
    {
        var controller = GetController(agent);
        if (controller == null) return NodeState.FAILURE;
        controller.ReleaseWaitingPoint();
        return NodeState.SUCCESS;
    }
}
