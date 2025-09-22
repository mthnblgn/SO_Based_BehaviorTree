using System.Collections.Generic;
using UnityEngine;

// MonoBehaviour that runs a BehaviorTree asset in the scene.
// At runtime we clone the ScriptableObject graph so each NPC has an isolated instance.
public class BehaviorTreeRunner : MonoBehaviour
{
    [Tooltip("The behavior tree asset this agent will use (design-time). At runtime it will be cloned per agent.")]
    public BehaviorTree tree;

    // Runtime, per-agent cloned root node. Never stored as an asset.
    private Node _runtimeRoot;

    private void Awake()
    {
        if (tree == null || tree.rootNode == null)
        {
            Debug.LogError($"Behavior Tree or its root node is not set for {gameObject.name}");
            enabled = false;
            return;
        }

        // Deep-clone the BT so this agent has its own node instances/state.
        _runtimeRoot = CloneNodeRecursive(tree.rootNode, new Dictionary<Node, Node>(64));
    }

    private void OnEnable()
    {
        // When coming back from a pool, Awake may not be called again. Ensure we have a runtime graph.
        if (_runtimeRoot == null && tree != null && tree.rootNode != null)
        {
            _runtimeRoot = CloneNodeRecursive(tree.rootNode, new Dictionary<Node, Node>(64));
        }
    }

    private void Update()
    {
        if (_runtimeRoot == null) return;
        _runtimeRoot.Evaluate(gameObject);
    }

    private void OnDestroy()
    {
        CleanupRuntimeNodes();
    }

    private void OnDisable()
    {
        // Optional: clean up early when this runner is disabled
        // (nodes will be recreated on next Awake if re-enabled via pooling lifecycle).
        CleanupRuntimeNodes();
    }

    private void CleanupRuntimeNodes()
    {
        if (_runtimeRoot == null) return;
        // Destroy the whole cloned graph. Since these are ScriptableObjects created at runtime
        // and not saved as assets, DestroyImmediate is safe even in edit mode, but we are in play.
        DestroyClonedGraph(_runtimeRoot, new HashSet<Node>());
        _runtimeRoot = null;
    }

    private void DestroyClonedGraph(Node root, HashSet<Node> visited)
    {
        if (root == null || !visited.Add(root)) return;
        if (root is CompositeNode comp && comp.children != null)
        {
            for (int i = 0; i < comp.children.Count; i++)
            {
                DestroyClonedGraph(comp.children[i], visited);
            }
        }
        Destroy(root);
    }

    // Deep clone the given node graph. Maintains a map so repeated references in the source
    // become a single instance per agent clone.
    private Node CloneNodeRecursive(Node original, Dictionary<Node, Node> map)
    {
        if (original == null) return null;
        if (map.TryGetValue(original, out var cached)) return cached;

        // Clone the ScriptableObject (runtime only, not saved)
        var clone = ScriptableObject.Instantiate(original);
        clone.name = original.name + " (Runtime)";
        clone.hideFlags = HideFlags.DontSave;

        map[original] = clone;

        // For composite nodes, clone/rewire children
        if (clone is CompositeNode compClone && original is CompositeNode compOrig)
        {
            var srcChildren = compOrig.children ?? new List<Node>();
            var dstChildren = compClone.children ?? new List<Node>(srcChildren.Count);
            dstChildren.Clear();
            for (int i = 0; i < srcChildren.Count; i++)
            {
                dstChildren.Add(CloneNodeRecursive(srcChildren[i], map));
            }
            compClone.children = dstChildren;
        }

        return clone;
    }
}
