using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(BehaviorTree))]
public class BehaviorTreeEditor : Editor
{
    // Drag & Drop state (static: consistent across editor instances)
    private static bool s_isDragging;
    private static Node s_draggedNode;
    private static CompositeNode s_sourceParent;
    private static int s_sourceIndex;

    // Drop target and visual indicator
    private static CompositeNode s_dropTargetParent;
    private static int s_dropIndex;
    private static Rect s_dropIndicatorRect;

    // Caches
    private static readonly Dictionary<string, GUIContent> s_guiContentCache = new Dictionary<string, GUIContent>(128);
    private static readonly HashSet<Node> s_visitedNodes = new HashSet<Node>(128);
    private static GUIContent s_nullNodeContent;

    // UI content
    private static readonly GUIContent s_btnAdd = new GUIContent("+", "Add child");
    private static readonly GUIContent s_btnAddDisabled = new GUIContent("+", "Only Composite nodes can hold children");
    private static readonly GUIContent s_btnRemove = new GUIContent("-", "Remove this node");
    private static readonly GUIContent s_btnRemoveSlot = new GUIContent("-", "Remove this empty child slot");

    private const int kIndent = 20;

    // Drop indicator colors
    private static readonly Color kDropIndicatorColor = new Color(0f, 1f, 1f, 0.85f);
    private static readonly Color kDropIndicatorStrongColor = new Color(0.20f, 0.60f, 1f, 0.95f); // stronger when same parent
    private static readonly Color kDropIndicatorOuterColor = new Color(0f, 0f, 0f, 0.35f);        // outline

    private void OnEnable()
    {
        if (s_nullNodeContent == null)
            s_nullNodeContent = new GUIContent("None (Node)");
    }

    public override void OnInspectorGUI()
    {
        var tree = (BehaviorTree)target;
        if (!tree) return;

        var evt = Event.current;

    // Reset for this frame
        s_dropTargetParent = null;
        s_dropIndicatorRect = Rect.zero;

        // Root atama
        EditorGUILayout.LabelField("Root Node", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        var newRoot = EditorGUILayout.ObjectField(tree.rootNode, typeof(Node), false) as Node;
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(tree, "Change Root Node");
            tree.rootNode = newRoot;
            EditorUtility.SetDirty(tree);
        }

        GUILayout.Space(15);
        EditorGUILayout.LabelField("Behavior Tree Visualizer & Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Children are evaluated from top to bottom. You can reorder with drag-and-drop and use '+' to add an empty slot.", MessageType.Info);

        // Draw the tree
        s_visitedNodes.Clear();
        if (tree.rootNode)
        {
            DrawNodeRecursive(tree.rootNode, 0, null, -1);
        }
        else
        {
            EditorGUILayout.HelpBox("Assign a Root Node to start building the tree.", MessageType.Info);
        }

        // Only draw drop indicator during Repaint
        if (s_isDragging && s_dropIndicatorRect != Rect.zero && evt.type == EventType.Repaint)
        {
            DrawDropIndicator();
        }

        // Drop/cancel with MouseUp or Escape
        if (s_isDragging)
        {
            if (evt.type == EventType.MouseUp && evt.button == 0)
            {
                HandleDragAndDrop();
                evt.Use();
            }
            else if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
            {
                CancelDrag();
                evt.Use();
            }
        }

        // Repaint only when necessary
        if (Application.isPlaying || s_isDragging || evt.type == EventType.MouseMove)
        {
            Repaint();
        }
    }

    private void DrawNodeRecursive(Node node, int level, CompositeNode parent, int childIndex)
    {
        if (node == null)
        {
            DrawNullNodeSlot(level, parent, childIndex);
            return;
        }

        // Circular reference protection
        if (!s_visitedNodes.Add(node))
        {
            DrawCircularReference(level, node, parent, childIndex);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(level * kIndent);

        var prevColor = GUI.color;
        GUI.color = GetStateColor(node.currentState);

    // Get row rect
        Rect nodeRect = EditorGUILayout.GetControlRect();

        // ObjectField
        // Show child index (if parent and index are known)
        string label = node.name;
        if (parent != null && childIndex >= 0)
            label = $"[{childIndex}] {label}";
        GUIContent nodeContent = GetGUIContent(label);
        EditorGUI.BeginChangeCheck();
        var newNode = EditorGUI.ObjectField(nodeRect, nodeContent, node, typeof(Node), false) as Node;
        if (EditorGUI.EndChangeCheck())
        {
            UpdateNodeReference(parent, childIndex, newNode);
        }

    // Drag events: allow starting drag even for root (parent null)
        HandleDragEvents(node, parent, childIndex, nodeRect);

        GUI.color = prevColor;

    // Quick controls
        DrawNodeControls(node, parent, childIndex);

        EditorGUILayout.EndHorizontal();

        // Draw children
        if (node is CompositeNode composite)
        {
            for (int i = 0; i < composite.children.Count; i++)
            {
                DrawNodeRecursive(composite.children[i], level + 1, composite, i);
            }
            DrawEndDropArea(level, composite);
        }

        s_visitedNodes.Remove(node);
    }

    private void HandleDragEvents(Node node, CompositeNode parent, int childIndex, Rect nodeRect)
    {
        var evt = Event.current;

        // Drag start: begin dragging even if parent is null (root)
        if (!s_isDragging && evt.type == EventType.MouseDown && evt.button == 0 && nodeRect.Contains(evt.mousePosition))
        {
            s_draggedNode = node;
            s_sourceParent = parent;     // null for root
            s_sourceIndex = childIndex;  // -1 for root
            s_isDragging = true;
            evt.Use();
            return;
        }

        // Target visualization: only show above/below lines in a parent context
        if (s_isDragging && s_draggedNode != null && s_draggedNode != node && parent != null && nodeRect.Contains(evt.mousePosition))
        {
            float midpoint = nodeRect.y + nodeRect.height * 0.5f;
            s_dropTargetParent = parent;

            bool dropAbove = evt.mousePosition.y < midpoint;
            s_dropIndex = dropAbove ? childIndex : childIndex + 1;
            s_dropIndex = ClampDropIndex(s_dropTargetParent, s_dropIndex);

            s_dropIndicatorRect = new Rect(nodeRect.x - 5f, dropAbove ? nodeRect.y - 2f : nodeRect.yMax - 2f, nodeRect.width + 10f, 4f);
        }
    }

    private void HandleDragAndDrop()
    {
        var tree = (BehaviorTree)target;

        if (s_draggedNode != null && s_dropTargetParent != null && ValidateDropTarget(s_dropTargetParent))
        {
            int insertIndex = ClampDropIndex(s_dropTargetParent, s_dropIndex);

            // Undo records
            Undo.RecordObject(s_dropTargetParent, "Reorder Node");
            if (s_sourceParent != null)
            {
                Undo.RecordObject(s_sourceParent, "Reorder Node");
            }
            else
            {
                Undo.RecordObject(tree, "Move Root Node");
            }

            // Remove from source
            if (s_sourceParent != null)
            {
                s_sourceParent.children.RemoveAt(s_sourceIndex);
            }
            else
            {
                // Moving root
                tree.rootNode = null;
                EditorUtility.SetDirty(tree);
            }

            // Fix index when moving forward within the same parent
            if (s_sourceParent == s_dropTargetParent && s_sourceIndex >= 0 && s_sourceIndex < insertIndex)
            {
                insertIndex--;
            }

            // Insert into target
            s_dropTargetParent.children.Insert(insertIndex, s_draggedNode);

            EditorUtility.SetDirty(s_dropTargetParent);
            if (s_sourceParent != null)
                EditorUtility.SetDirty(s_sourceParent);
        }

        CancelDrag();
    }

    private void CancelDrag()
    {
        s_isDragging = false;
        s_draggedNode = null;
        s_sourceParent = null;
        s_dropTargetParent = null;
        s_dropIndicatorRect = Rect.zero;
        s_sourceIndex = -1;
        s_dropIndex = -1;
    }

    private void UpdateNodeReference(CompositeNode parent, int childIndex, Node newNode)
    {
        if (parent != null)
        {
            Undo.RecordObject(parent, "Change Child Node");
            parent.children[childIndex] = newNode;
            EditorUtility.SetDirty(parent);
        }
        else
        {
            Undo.RecordObject(target, "Change Root Node");
            ((BehaviorTree)target).rootNode = newNode;
            EditorUtility.SetDirty(target);
        }
    }

    private void DrawNodeControls(Node node, CompositeNode parent, int childIndex)
    {
        // "+" button: show disabled with tooltip if not a Composite
        if (node is CompositeNode composite)
        {
            if (GUILayout.Button(s_btnAdd, GUILayout.Width(25)))
            {
                Undo.RecordObject(composite, "Add Child Slot");
                // Add new slot at the end to preserve natural top-to-bottom flow
                composite.children.Add(null);
                EditorUtility.SetDirty(composite);
            }
        }
        else
        {
            using (new EditorGUI.DisabledScope(true))
            {
                GUILayout.Button(s_btnAddDisabled, GUILayout.Width(25));
            }
        }

        // "-" button if a parent exists
        if (parent != null)
        {
            if (GUILayout.Button(s_btnRemove, GUILayout.Width(25)))
            {
                Undo.RecordObject(parent, "Remove Child Node");
                parent.children.RemoveAt(childIndex);
                EditorUtility.SetDirty(parent);
                GUIUtility.ExitGUI();
            }
        }
    }

    private void DrawEndDropArea(int level, CompositeNode composite)
    {
        if (s_isDragging && s_draggedNode != composite)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space((level + 1) * kIndent);
            Rect endDropRect = EditorGUILayout.GetControlRect(false, 10);
            EditorGUILayout.EndHorizontal();

            if (endDropRect.Contains(Event.current.mousePosition) && ValidateDropTarget(composite))
            {
                s_dropTargetParent = composite;
                s_dropIndex = ClampDropIndex(composite, composite.children.Count);
                s_dropIndicatorRect = new Rect(endDropRect.x - 5f, endDropRect.y, endDropRect.width + 10f, 4f);
            }
        }
    }

    private void DrawNullNodeSlot(int level, CompositeNode parent, int childIndex)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(level * kIndent);

        EditorGUI.BeginChangeCheck();
        Node newNode = EditorGUILayout.ObjectField(s_nullNodeContent, null, typeof(Node), false) as Node;
        if (EditorGUI.EndChangeCheck() && newNode != null)
        {
            Undo.RecordObject(parent, "Assign Child Node");
            parent.children[childIndex] = newNode;
            EditorUtility.SetDirty(parent);
        }

        if (GUILayout.Button(s_btnRemoveSlot, GUILayout.Width(25)))
        {
            Undo.RecordObject(parent, "Remove Child Node Slot");
            parent.children.RemoveAt(childIndex);
            EditorUtility.SetDirty(parent);
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawCircularReference(int level, Node node, CompositeNode parent, int childIndex)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(level * kIndent);

        var prev = GUI.color;
        GUI.color = Color.red;
    EditorGUILayout.LabelField(GetGUIContent($"-> (CIRCULAR REFERENCE) {node.name}"));
        GUI.color = prev;

        if (parent != null)
        {
            if (GUILayout.Button(s_btnRemove, GUILayout.Width(25)))
            {
                Undo.RecordObject(parent, "Remove Circular Reference Node");
                parent.children[childIndex] = null;
                EditorUtility.SetDirty(parent);
                GUIUtility.ExitGUI();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private static int ClampDropIndex(CompositeNode parent, int index)
    {
        if (parent == null) return 0;
        if (index < 0) return 0;
        if (index > parent.children.Count) return parent.children.Count;
        return index;
    }

    // Drop target validation: prevent moving into own subtree, null parent, etc.
    private bool ValidateDropTarget(CompositeNode targetParent)
    {
        if (s_draggedNode == null || targetParent == null)
            return false;

        // Moving onto/under the same node (root) is meaningless
        if (ReferenceEquals(targetParent, s_draggedNode))
            return false;

        // Block if target is in dragged node's subtree (prevent cycles)
        if (IsInSubtree(s_draggedNode, targetParent))
            return false;

        return true;
    }

    private static bool IsInSubtree(Node root, Node search)
    {
        if (root == null || search == null) return false;
        if (root == search) return true;

        if (root is CompositeNode comp)
        {
            var children = comp.children;
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child == null) continue;
                if (IsInSubtree(child, search)) return true;
            }
        }
        return false;
    }

    private GUIContent GetGUIContent(string text)
    {
        if (string.IsNullOrEmpty(text)) text = "Unnamed Node";
        if (s_guiContentCache.TryGetValue(text, out var content))
            return content;

        content = new GUIContent(text);
        s_guiContentCache[text] = content;
        return content;
    }

    private Color GetStateColor(Node.NodeState state)
    {
        if (!Application.isPlaying) return Color.white;

        switch (state)
        {
            case Node.NodeState.RUNNING: return Color.yellow;
            case Node.NodeState.SUCCESS: return Color.green;
            case Node.NodeState.FAILURE: return Color.red;
            default: return Color.grey;
        }
    }

    private void DrawDropIndicator()
    {
        // More pronounced color when within the same parent
        var innerColor = (s_sourceParent == s_dropTargetParent) ? kDropIndicatorStrongColor : kDropIndicatorColor;

        // Outer outline (slightly thicker)
        var outerRect = s_dropIndicatorRect;
        outerRect.y -= 1f;
        outerRect.height += 2f;
        outerRect.x -= 1f;
        outerRect.width += 2f;
        EditorGUI.DrawRect(outerRect, kDropIndicatorOuterColor);

        // Inner line
        EditorGUI.DrawRect(s_dropIndicatorRect, innerColor);
    }
}

