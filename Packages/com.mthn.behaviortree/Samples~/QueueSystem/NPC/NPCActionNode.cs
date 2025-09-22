public abstract class NPCActionNode : ActionNode
{
    protected NPCController GetController(GameObject agent) => agent.GetComponent<NPCController>();
}