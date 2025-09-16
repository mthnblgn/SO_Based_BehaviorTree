using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Behavior Tree/Composite/Deadline Sequence", fileName = "DeadlineSequence")]
public class DeadlineSequence : CompositeNode
{
    [Tooltip("Maximum time allowed (s) for all children to return SUCCESS")]
    public float maxWaitSeconds = 8f;

    // Keep per-agent state because ScriptableObjects are shared
    [System.NonSerialized] private readonly Dictionary<int, float> startByAgent = new Dictionary<int, float>();
    [System.NonSerialized] private readonly Dictionary<int, int> indexByAgent = new Dictionary<int, int>();

    protected override void OnStart(GameObject agent)
    {
        // Wire up children's Parent reference
        if (children != null)
        {
            for (int i = 0; i < children.Count; i++)
                if (children[i] != null) children[i].Parent = this;
        }

        int id = agent != null ? agent.GetInstanceID() : 0;
        startByAgent[id] = Time.time;
        indexByAgent[id] = 0;
    }

    protected override NodeState OnUpdate(GameObject agent)
    {
        if (children == null || children.Count == 0)
            return NodeState.SUCCESS;

        int id = agent != null ? agent.GetInstanceID() : 0;

        // Initialize timer and index if missing
        if (!startByAgent.TryGetValue(id, out float start))
        {
            start = Time.time;
            startByAgent[id] = start;
        }
        if (!indexByAgent.TryGetValue(id, out int currentIndex))
        {
            currentIndex = 0;
            indexByAgent[id] = currentIndex;
        }

        // Deadline check (if time runs out before all children finish -> FAILURE)
        if (Time.time - start >= maxWaitSeconds)
        {
            indexByAgent[id] = 0;
            NPCController controller = agent?.GetComponent<NPCController>();
            controller?.GetCurrentArea().Dequeue(controller);
            return NodeState.FAILURE;
        }

        // Run children sequentially
        while (currentIndex < children.Count)
        {
            var child = children[currentIndex];
            if (child == null)
            {
                currentIndex++;
                continue;
            }

            var st = child.Evaluate(agent);
            switch (st)
            {
                case NodeState.FAILURE:
                    indexByAgent[id] = 0;
                    return NodeState.FAILURE;

                case NodeState.RUNNING:
                    indexByAgent[id] = currentIndex; // stay on the same child
                    return NodeState.RUNNING;

                case NodeState.SUCCESS:
                    currentIndex++; // move to the next
                    indexByAgent[id] = currentIndex;
                    break;
            }
        }

        // All children returned SUCCESS
        indexByAgent[id] = 0;
        return NodeState.SUCCESS;
    }

    protected override void OnStop(GameObject agent)
    {
        int id = agent != null ? agent.GetInstanceID() : 0;
        startByAgent.Remove(id);
        indexByAgent.Remove(id);
    }
}