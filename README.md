<div align="center">

# SO Based Behavior Tree

Lightweight, ScriptableObject-driven Behavior Tree framework for Unity (tested on Unity 6000.x) with per-agent runtime cloning, queue/spot reservation sample, and safe NavMesh movement actions.

</div>

## ✨ Features

- ScriptableObject nodes (easy authoring & reusability)
- Automatic deep clone per agent at runtime (no shared mutable node state)
- Core composites: Sequence, Selector, Parallel (if included in project), DeadlineSequence (time-bounded sequence)
- Clean node lifecycle: OnStart / OnUpdate / OnStop + Abort handling
- Safety: running children aborted on composite stop to avoid leaked state
- Sample domain: NPC queue & waiting spot reservation system
- NavMesh-safe movement actions (warp on missing mesh, re-sync destination)
- Object pooling friendly (tree runtime rebuilt on enable if needed)
- Minimal, dependency-free C# (only UnityEngine / NavMesh)

## 🧱 Repository Layout

```
Assets/
	Scripts/
		BehaviorTreeRunner.cs        # MonoBehaviour host that clones & ticks a tree
		BehaviourTree/               # Core behaviour tree implementation
			Base/
				Node.cs                  # Base + CompositeNode
				CompositeNodes/          # Sequence, Selector, etc.
			SampleNodes/               # Example action nodes (movement, queue logic)
		NPCController.cs             # Example agent controller
		NPCSpawner.cs                # Example pooled spawning
		AreaController.cs            # Queue / spot management
```

## 🚀 Quick Start

1. Create a new Behavior Tree asset (ScriptableObject) with a root composite (e.g., Sequence).
2. Add child nodes (drag ScriptableObject node assets into children list of composites).
3. Add a `BehaviorTreeRunner` to your NPC prefab and assign the tree.
4. Add required components (e.g., `NavMeshAgent`, `NPCController`).
5. Enter Play. The runner clones the tree so each NPC has isolated node instances.

### Creating a Node

Use `[CreateAssetMenu(menuName = "Behavior Tree/Action/My Action")]` and inherit from `Node` or a custom base (e.g., `NPCActionNode` used in samples). Implement:

```
protected override void OnStart(GameObject agent) { /* one-time init */ }
protected override NodeState OnUpdate(GameObject agent) { /* return RUNNING/SUCCESS/FAILURE */ }
protected override void OnStop(GameObject agent) { /* cleanup */ }
```

Return semantics:
- `RUNNING`: Will be ticked again next frame
- `SUCCESS` / `FAILURE`: Node finalizes; `OnStop` is invoked and state resets

## 🔄 Lifecycle & State Model

Each node tracks:
- `started` (internal guard to call `OnStart` exactly once per run)
- `LastState` (most recent returned state)

Evaluation flow (`Node.Evaluate`):
1. If not started → set started + call `OnStart`.
2. Call `OnUpdate` → get `NodeState` result.
3. Record result into `LastState`.
4. If terminal (SUCCESS/FAILURE) → call `OnStop` and clear `started`.

Aborting: Composites call `Abort` on running children when they themselves stop. `Abort` forces `OnStop` + sets `LastState` to `FAILURE` (configurable pattern—keeps semantics simple).

Why clone? ScriptableObjects are assets (shared). Cloning avoids:
- Cross-agent state bleeding
- Manual per-agent dictionaries
- Threading concerns (future jobification)

## 🧩 Composites

Sequence:
- Runs children in order.
- Stops on first FAILURE (returns FAILURE).
- Returns RUNNING if a child is RUNNING.
- Returns SUCCESS if all succeed.

Selector:
- Tries children until one SUCCESS.
- Returns SUCCESS immediately on success.
- Returns RUNNING if current child RUNNING.
- Returns FAILURE if all fail.

DeadlineSequence (sample specialized composite):
- Time-bounded variant that fails if allotted duration exceeded.

Parallel (if present in repo):
- Typical patterns: succeed-on-all or succeed-on-one (policy). (Adjust README if policies added.)

All composites abort RUNNING children in `OnStop` to ensure clean handover when the branch is left.

## 🏃‍♂️ BehaviorTreeRunner

Responsibilities:
- Validates assigned tree & root
- Deep clones node graph (maintains identity map)
- Ticks root every `Update()`
- Cleans up runtime clones on disable/destroy

Pool Friendly: If coming from a pool (Awake not called again) `OnEnable` ensures the runtime clone exists.

## 👥 Sample Domain: Queue & Waiting Spots

Included action nodes demonstrate how to layer game logic atop the BT:
- Finding / Reserving waiting spots
- Moving to a spot (NavMeshAgent integration, arrival checks)
- Occupying / Releasing spots
- Queue progression & leaving action

`AreaController` manages available spots & queue limit; defensive checks prevent over-enqueue.

## 🧪 Extending

Add a new composite: inherit `CompositeNode`, manage `children`, implement `OnUpdate` and optionally override `OnStop` (call `base.OnStop` first to abort running children). Keep any per-run counters/indices as fields; cloning makes them per-agent.

Add a decorator (future idea): create a node that wraps a single child and intercepts lifecycle.

## ⚠️ Design Considerations

- State Simplicity: Only `LastState` tracked (no parallel `currentState`).
- Isolation: Runtime cloning removes need for thread locks or per-agent dictionaries.
- Safety: Null & pooling guards (Spawner, Area exit fallback, NavMesh sampling).
- Abort Semantics: Children get a final `OnStop`; adjust `Abort` behavior if you need distinct cancellation code paths.

## 📈 Future Ideas / Roadmap

- Decorator node support (Inverter, Succeeder, Repeater)
- Visual editor (graph view) tooling
- Runtime tree debugging overlay (node color states)

## 🔧 Installation

Option A: Copy the `BehaviourTree` folder (and helper scripts you need) into your project.

Option B: Create a Unity package (UPM) by moving these scripts under `Packages/YourCompany.BehaviorTree/` and adding a `package.json`.

## ▶️ Minimal Usage Example

```
public class PlayOnAwake : MonoBehaviour {
		public BehaviorTreeRunner runner; // assign in Inspector
		void Start() {
				// Tree auto-ticks via BehaviorTreeRunner.Update
		}
}
```

## 🐞 Debugging Tips

- Add `Debug.Log` in `OnStart` to verify node order during authoring.
- Use `LastState` to render a simple in-scene gizmo / label.
- If an agent stalls: check NavMesh availability (SamplePosition) and that a child node returns RUNNING (not silently failing every frame).

## 📜 License

Released under the MIT License (see `LICENSE`).

## 🙌 Contributing

Issues and pull requests welcome. Please keep additions dependency-light.

## ❤️ Acknowledgements

Inspired by common Behavior Tree patterns and Unity community examples; adapted for ScriptableObject-driven authoring with per-agent cloning.

---
If you use this in a project, a star on GitHub is appreciated!

