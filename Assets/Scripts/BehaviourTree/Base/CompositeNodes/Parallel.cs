using UnityEngine;
using System.Collections.Generic;

public enum ParallelPolicy { RequireAll, RequireOne }

[CreateAssetMenu(menuName = "Behavior Tree/Composite/Parallel", fileName = "Parallel")]
public class Parallel : CompositeNode
{
    [Tooltip("Requirement for SUCCESS")]
    public ParallelPolicy successPolicy = ParallelPolicy.RequireAll;

    [Tooltip("Requirement for FAILURE")]
    public ParallelPolicy failurePolicy = ParallelPolicy.RequireOne;

    protected override void OnStart(GameObject agent)
    {
        // Wire up children's Parent reference
        if (children != null)
        {
            for (int i = 0; i < children.Count; i++)
                if (children[i] != null) children[i].Parent = this;
        }
    }

    protected override NodeState OnUpdate(GameObject agent)
    {
        if (children == null || children.Count == 0)
            return NodeState.SUCCESS;

        bool anyRunning = false;
        int successCount = 0;
        int failureCount = 0;

        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (child == null) continue;

            var state = child.Evaluate(agent);
            switch (state)
            {
                case NodeState.SUCCESS: successCount++; break;
                case NodeState.FAILURE: failureCount++; break;
                case NodeState.RUNNING: anyRunning = true; break;
            }
        }

        bool successOk = (successPolicy == ParallelPolicy.RequireAll)
            ? successCount == CountValidChildren()
            : successCount > 0;

        bool failureOk = (failurePolicy == ParallelPolicy.RequireAll)
            ? failureCount == CountValidChildren()
            : failureCount > 0;

        if (failureOk) return NodeState.FAILURE;
        if (successOk && !anyRunning) return NodeState.SUCCESS;

        return NodeState.RUNNING;
    }

    private int CountValidChildren()
    {
        int c = 0;
        for (int i = 0; i < children.Count; i++)
            if (children[i] != null) c++;
        return c;
    }
}
