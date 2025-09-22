<div align="center">

# SO Based Behavior Tree

Minimal ScriptableObject-based Behavior Tree for Unity. Per-agent deep cloning, clean lifecycle, optional queue sample.

</div>

## Features
* Nodes are ScriptableObjects (reusable assets)
* Deep clone per agent (no shared mutable runtime state)
* Composites: Sequence, Selector, DeadlineSequence, Parallel
* Simple lifecycle: OnStart / OnUpdate / OnStop / Abort
* Queue System Sample (domain kept out of core)

## Installation (UPM)
Package Manager > Add package from git URL:
```
https://github.com/mthnblgn/SO_Based_BehaviorTree.git?path=Packages/com.mthn.behaviortree#v0.1.4
```
Sample: select the package > Samples > Queue System Sample > Import

## Quick Usage
1. Create > Behavior Tree > Behavior Tree (asset)
2. Assign a Sequence / Selector as root
3. Create Action / Composite node assets and drag them as children
4. Add `BehaviorTreeRunner` to your agent GameObject and assign the tree
5. Press Play

### Example Node
```csharp
[CreateAssetMenu(menuName="Behavior Tree/Action/Log")]
public class LogNode : ActionNode {
    public string message;
    protected override NodeState OnUpdate(GameObject agent) {
        Debug.Log(message);
        return NodeState.SUCCESS;
    }
}
```

## DeadlineSequence
Acts like a Sequence but if the time budget expires it returns FAILURE immediately and resets its child index.

## Sample (Queue System)
Demonstrates spot reservation, queue progression, and leaving behavior. Core package stays domain‑agnostic.

## License
MIT

## Contributing
Issues / PRs welcome. Please keep things lightweight and focused.

---
Kept intentionally concise. See commit history for deeper details.

Note: package.json "unity": "2022.3" denotes the minimum supported LTS baseline; tested also on 6000.x.

