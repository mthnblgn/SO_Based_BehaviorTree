using UnityEngine;

[CreateAssetMenu(menuName = "Behavior Tree/Action/Queue For Waiting Spot", fileName = "QueueForWaitingSpot")]
public class QueueForWaitingSpot : NPCActionNode
{
    [Tooltip("Queue check interval to avoid busy loop (seconds)")]
    public float pollInterval = 0.1f;

    private NPCController controller;
    private AreaController area;

    protected override void OnStart(GameObject agent)
    {
        controller = GetController(agent);
        area = controller != null ? controller.GetCurrentArea() : null;

        if (controller == null)
            return;

        if (area != null)
        {
            // Ensure NPC is in the queue
            if (!area.IsInQueue(controller))
            {
                area.EnqueueNPC(controller);
            }
            // If NPCSpawner already EnqueueAndGetPosition ile eklediyse burada tekrar eklenmez.
        }
    }

    protected override NodeState OnUpdate(GameObject agent)
    {
        if (controller == null || area == null)
            return NodeState.FAILURE;

        // This node only ensures we are in the queue, then succeeds so next actions can run
        return NodeState.SUCCESS;
    }
}
