using UnityEngine;

// Sample-specific convenience base providing typed access to NPCController.
// Lives in Samples~ so the core runtime stays decoupled from game domain components.
public abstract class NPCActionNode : ActionNode
{
    protected NPCController GetController(GameObject agent) => agent.GetComponent<NPCController>();
}
