using UnityEngine;

// ============================================================================
// TemplateActionNode
// --------------------------------------------------------------------------
// Copy this file, rename the class & file, and adjust the CreateAssetMenu
// attributes to quickly implement a new Behavior Tree action.
// ============================================================================
// HOW TO USE
// 1. Duplicate this file (Ctrl+D) and rename it (e.g., MoveToTargetNode.cs)
// 2. Rename the class below (e.g., MoveToTargetNode)
// 3. Adjust the [CreateAssetMenu] 'menuName' & 'fileName' values
// 4. Implement your logic in OnStart / OnUpdate / OnStop
// 5. Return RUNNING while in progress, SUCCESS or FAILURE when done
// 6. Keep state in private fields (cloned per-agent at runtime)
// ============================================================================

[CreateAssetMenu(menuName = "Behavior Tree/Action/Template Action", fileName = "TemplateActionNode")]
public class TemplateActionNode : NPCActionNode
{
    // Exposed tunables (appear in Inspector)
    [Header("Tuning")] 
    [Tooltip("Example numeric parameter – replace or remove.")] 
    public float exampleValue = 1f;

    // Cached per-run references (safe: node instances are per-agent at runtime)
    private NPCController _controller; 
    private GameObject _agent; 

    // One-time setup when this node starts executing
    protected override void OnStart(GameObject agent)
    {
        _agent = agent;
        _controller = GetController(agent);

        // TODO: Initialize pathing / choose target / allocate resources
        // Debug.Log($"[{name}] OnStart for {agent.name}");
    }

    // Called every frame while the node is RUNNING
    protected override NodeState OnUpdate(GameObject agent)
    {
        // OPTIONAL: safety guard if something became invalid mid-run
        if (_controller == null)
        {
            return NodeState.FAILURE; // early failure if required component missing
        }

        // TODO: Core action logic here
        // Example pseudo pattern:
        // if (!HasBegunWork) BeginWork();
        // if (WorkNotFinished) return NodeState.RUNNING;
        // if (EncounteredIrrecoverableIssue) return NodeState.FAILURE;
        // return NodeState.SUCCESS;

        return NodeState.SUCCESS; // replace with real result
    }

    // Cleanup after SUCCESS / FAILURE OR if aborted by a parent composite
    protected override void OnStop(GameObject agent)
    {
        // TODO: Release resources / stop movement / null references if desired
        // Debug.Log($"[{name}] OnStop for {agent.name}");
    }
}
