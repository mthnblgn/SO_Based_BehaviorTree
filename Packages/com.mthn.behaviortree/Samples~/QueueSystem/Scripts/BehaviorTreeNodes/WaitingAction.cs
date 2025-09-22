using UnityEngine;

[CreateAssetMenu(menuName = "Behavior Tree/Action/Waiting", fileName = "WaitingAction")]
public class WaitingAction : NPCActionNode
{
    public float waitTime = 1.0f; // Default wait time
    private float waitTimer;

    protected override void OnStart(GameObject agent)
    {
        waitTimer = 0; // Initialize timer
    }

    protected override NodeState OnUpdate(GameObject agent)
    {
        if (waitTimer < waitTime)
        {
            waitTimer += Time.deltaTime;
            return NodeState.RUNNING; // Still waiting
        }
        waitTimer = 0; // Reset timer
        return NodeState.SUCCESS; // Finished waiting
    }

    protected override void OnStop(GameObject agent) { }
}