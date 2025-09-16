using System.Collections.Generic;
using UnityEngine;

// Base abstract ScriptableObject that all nodes will inherit from.
// Now the Evaluate method takes the agent it operates on as a parameter.
public abstract class Node : ScriptableObject
{
    public enum NodeState
    {
        RUNNING,
        SUCCESS,
        FAILURE
    }
    [HideInInspector] public NodeState currentState;

    [Tooltip("Descriptive name of the node in the Inspector.")]
    [TextArea] public string description;

    // It's important to reset a node's state once per frame.
    // This prevents old states from persisting when switching between different branches of the tree.
    protected bool started = false;

    public Node Parent { get; internal set; }  // eklendi

    public NodeState LastState { get; private set; } = NodeState.RUNNING; // eklendi

    public NodeState Evaluate(GameObject agent)
    {
        if (!started)
        {
            started = true;
            OnStart(agent);
        }

        var result = OnUpdate(agent);

        LastState = result; // eklendi

        if (result != NodeState.RUNNING)
        {
            OnStop(agent);
            started = false;
        }
        return result;
    }

    // Allow composite nodes to cancel a running child cleanly
    public void Abort(GameObject agent)
    {
        if (started)
        {
            OnStop(agent);
            started = false;
            currentState = NodeState.FAILURE; // reset to a terminal state
        }
    }

    // Called when the node runs for the first time.
    protected virtual void OnStart(GameObject agent) { }
    // Called when the node finishes running (SUCCESS or FAILURE).
    protected virtual void OnStop(GameObject agent) { }
    // Called every frame while the node is running.
    protected abstract NodeState OnUpdate(GameObject agent);
}

// Base class for composite nodes that can have multiple child nodes.
public abstract class CompositeNode : Node
{
    [Tooltip("Child nodes attached to this node.")]
    public List<Node> children = new List<Node>();
}