using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "Behavior Tree/Action/Move To Queue Position", fileName = "MoveToQueuePosition")]
public class MoveToQueuePosition : NPCActionNode
{
    [Tooltip("Sufficient proximity to the target (meters)")]
    public float arrivalThreshold = 0.25f;

    private NPCController controller;
    private NavMeshAgent agent;
    private AreaController area;

    protected override void OnStart(GameObject agentGO)
    {
        controller = GetController(agentGO);
        area = controller != null ? controller.GetCurrentArea() : null;
        agent = agentGO.GetComponent<NavMeshAgent>();
        UpdateDestination();
    }

    protected override NodeState OnUpdate(GameObject agentGO)
    {
        if (controller == null || area == null)
            return NodeState.FAILURE;

        // If at the front, proceed to the next node
        if (area.IsFirst(controller) && controller.HasArrived(agent, arrivalThreshold))
        {
            area.DequeueIfFirst(controller); // Remove from queue when progressing successfully
            return NodeState.SUCCESS;
        }

        // Ensure NPC is in the queue
        if (!area.IsInQueue(controller))
        {
            area.EnqueueNPC(controller);
        }

        // Continue moving towards the queue slot
        UpdateDestination();

        // Keep waiting if not at the front
        return NodeState.RUNNING;
    }

    private void UpdateDestination()
    {
        if (area != null && controller != null)
        {
            if (area.TryGetQueuePosition(controller, out var pos))
            {
                controller.SetTargetPosition(pos);
            }
        }
    }
}
