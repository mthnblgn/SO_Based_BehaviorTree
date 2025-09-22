using UnityEngine;

[CreateAssetMenu(menuName = "Behavior Tree/Composite/Deadline Sequence", fileName = "DeadlineSequence")]
public class DeadlineSequence : CompositeNode
{
    [Tooltip("Maximum time allowed (s) for all children to return SUCCESS")]
    public float maxWaitSeconds = 8f;

    // Runtime state (each BT is cloned per agent so instance fields are safe)
    private float _startTime;
    private int _currentIndex;

    protected override void OnStart(GameObject agent)
    {
        // Wire up children's Parent reference
        if (children != null)
        {
            for (int i = 0; i < children.Count; i++)
                if (children[i] != null) children[i].Parent = this;
        }

        _startTime = Time.time;
        _currentIndex = 0;
    }

    protected override NodeState OnUpdate(GameObject agent)
    {
        if (children == null || children.Count == 0)
            return NodeState.SUCCESS;

        // Deadline check (if time runs out before all children finish -> FAILURE)
        if (Time.time - _startTime >= maxWaitSeconds)
        {
            _currentIndex = 0;
            return NodeState.FAILURE;
        }

        // Run children sequentially
        while (_currentIndex < children.Count)
        {
            var child = children[_currentIndex];
            if (child == null)
            {
                _currentIndex++;
                continue;
            }

            var st = child.Evaluate(agent);
            switch (st)
            {
                case NodeState.FAILURE:
                    _currentIndex = 0;
                    return NodeState.FAILURE;

                case NodeState.RUNNING:
                    // stay on same child
                    return NodeState.RUNNING;

                case NodeState.SUCCESS:
                    _currentIndex++; // move to the next
                    break;
            }
        }

        // All children returned SUCCESS
        _currentIndex = 0;
        return NodeState.SUCCESS;
    }

    protected override void OnStop(GameObject agent)
    {
        _startTime = 0f;
        _currentIndex = 0;
        base.OnStop(agent); // abort any running child
    }
}