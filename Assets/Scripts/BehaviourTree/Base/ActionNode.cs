using System.Collections.Generic;
using UnityEngine;

public abstract class ActionNode : Node { }

public abstract class NPCActionNode : ActionNode
{
    protected NPCController GetController(GameObject agent) => agent.GetComponent<NPCController>();
}