using UnityEngine;

// ScriptableObject asset representing a complete behavior tree.
[CreateAssetMenu(menuName = "Behavior Tree/Behavior Tree", fileName = "BehaviorTree")]
public class BehaviorTree : ScriptableObject
{
    [Tooltip("The tree's starting (root) node.")]
    public Node rootNode;
}
