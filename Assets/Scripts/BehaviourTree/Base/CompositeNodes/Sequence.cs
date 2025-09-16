using UnityEngine;
[CreateAssetMenu(menuName = "Behavior Tree/Composite/Sequence", fileName = "Sequence")]
public class Sequence : CompositeNode
{
    private int currentIndex;

    protected override void OnStart(GameObject agent)
    {
        currentIndex = 0;

        // Wire up parents (children's Parent = this)
        if (children != null)
        {
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] != null) children[i].Parent = this;
            }
        }
    }

    protected override NodeState OnUpdate(GameObject agent)
    {
        if (children == null || children.Count == 0)
            return NodeState.SUCCESS; // empty sequence -> success

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
                case NodeState.FAILURE:
                    currentIndex = 0;
                    return NodeState.FAILURE;
                case NodeState.RUNNING:
                    return NodeState.RUNNING;
                case NodeState.SUCCESS:
                    currentIndex++;
                    break;
            }
        }

        currentIndex = 0;
        return NodeState.SUCCESS;
    }

    protected override void OnStop(GameObject agent)
    {
        currentIndex = 0;
    }
}
