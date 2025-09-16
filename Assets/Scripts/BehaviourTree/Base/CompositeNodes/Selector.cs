using UnityEngine;

[CreateAssetMenu(menuName = "Behavior Tree/Composite/Selector", fileName = "Selector")]
public class Selector : CompositeNode
{
    private int currentIndex;

    protected override void OnStart(GameObject agent)
    {
        currentIndex = 0;
    }

    protected override NodeState OnUpdate(GameObject agent)
    {
        if (children == null || children.Count == 0)
            return NodeState.FAILURE;

        while (currentIndex < children.Count)
        {
            var child = children[currentIndex];
            if (child == null)
            {
                currentIndex++;
                continue;
            }

            var state = child.Evaluate(agent);
            switch (state)
            {
                case NodeState.SUCCESS:
                    currentIndex = 0;
                    return NodeState.SUCCESS;
                case NodeState.RUNNING:
                    return NodeState.RUNNING;
                case NodeState.FAILURE:
                    currentIndex++;
                    break;
            }
        }

        currentIndex = 0;
        return NodeState.FAILURE;
    }

    protected override void OnStop(GameObject agent)
    {
        currentIndex = 0;
    }
}
