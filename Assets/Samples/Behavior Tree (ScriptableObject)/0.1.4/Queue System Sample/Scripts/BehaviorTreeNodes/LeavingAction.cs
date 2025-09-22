using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "Behavior Tree/Action/Leaving", fileName = "LeavingAction")]
public class LeavingAction : NPCActionNode
{
    private NPCController controller;
    private NavMeshAgent nmAgent;
    public float arrivalThreshold = 2f;

    protected override void OnStart(GameObject agent)
    {
        controller = GetController(agent);
        nmAgent = agent.GetComponent<NavMeshAgent>();
        if (controller == null)
        {
            Debug.LogWarning($"LeavingAction: NPCController missing on {agent?.name}");
        }
        else
        {
            var area = controller.GetCurrentArea();
            if (area != null)
            {
                var exitPos = area.GetExitPosition();
                controller.SetTargetPosition(exitPos);
                area.Dequeue(controller);
            }
        }
    }

    protected override NodeState OnUpdate(GameObject agent)
    {
        // Wait until we arrive the exit point, then succeed to trigger OnStop
        if (controller.HasArrived(nmAgent, arrivalThreshold))
        {
            return NodeState.SUCCESS;
        }
        return NodeState.RUNNING;
    }

    protected override void OnStop(GameObject agent)
    {
        if (controller != null)
        {
            // Release any reserved/occupied spot first
            controller.ReleaseWaitingPoint();
            // Remove from area queue if present
            var area = controller.GetCurrentArea();
            controller.Despawn();
        }
    }
}
