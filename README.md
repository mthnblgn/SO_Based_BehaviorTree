<div align="center">

# SO Based Behavior Tree

Lightweight, ScriptableObject-driven Behavior Tree framework for Unity (tested on Unity 6000.x) with per-agent runtime cloning, queue/spot reservation sample, optional basic game UI sample, and safe NavMesh movement actions.

</div>

## ✨ Features

- ScriptableObject nodes (easy authoring & reusability)
- Automatic deep clone per agent (no shared mutable node state)
- Core composites: Sequence, Selector, Parallel (if added), DeadlineSequence (time-bounded)
- Clean lifecycle: OnStart / OnUpdate / OnStop + explicit Abort
- Safe: running children aborted when composites stop
- Samples: Queue system domain + minimal Game (pause/start/quit)
- NavMesh-friendly movement actions (handles missing mesh + resync)
- Pool friendly (rebuilds runtime graph on enable)
- Dependency-light (UnityEngine only)

## 🧱 Package Layout

```
Packages/com.mthn.behaviortree/
	package.json
	Runtime/
		BehaviorTreeRunner.cs
		Base/Node.cs
		Base/CompositeNodes/ (Sequence, Selector, Parallel, DeadlineSequence)
		Templates/TemplateActionNode.cs
	Editor/ (placeholder for future tooling)
	Samples~/
		QueueSystem/
			Domain/AreaController.cs
			NPC/NPCController.cs, NPCSpawner.cs
			BehaviorTreeNodes/... (FindWaitingSpot, MoveToQueuePosition, etc.)
			(optional) Scenes/
		Game/
			GameManager.cs
			Scenes
```

Imported samples are copied under `Assets/Samples/Behavior Tree (ScriptableObject)/<version>/...` so you can modify them safely.

## 🚀 Quick Start (Core)

1. Create a Behavior Tree ScriptableObject (root = Sequence/Selector etc.).
2. Create node assets and wire them as children.
3. Put `BehaviorTreeRunner` on an agent prefab; assign the tree asset.
4. Add required components (e.g. `NavMeshAgent`).
5. Play: each prefab instance gets its own deep-cloned node graph.

### Creating a Node

```csharp
[CreateAssetMenu(menuName = "Behavior Tree/Action/My Action")]
public class MyAction : ActionNode {
		protected override void OnStart(GameObject agent) { }
		protected override NodeState OnUpdate(GameObject agent) { return NodeState.SUCCESS; }
		protected override void OnStop(GameObject agent) { }
}
```

Return values:
- RUNNING: tick again next frame
- SUCCESS / FAILURE: lifecycle ends → OnStop then node resets `started`

## 🔄 Lifecycle & State

`Evaluate()` flow:
1. First tick → set `started` + `OnStart()`
2. Call `OnUpdate()` → capture result in `LastState`
3. If terminal → `OnStop()` + clear `started`

Abort path: `Abort()` forces a final `OnStop()` and sets `LastState` (simplified cancellation). Composites abort running children they are leaving.

Why deep clone? Avoid shared mutable fields on ScriptableObjects (separate per-agent counters, indices, timers) with zero bookkeeping dictionaries.

## 🧩 Composites & Time Limits

Sequence / Selector: Classic semantics.

DeadlineSequence:
- Runs like a Sequence but enforces a time budget.
- On timeout returns FAILURE and resets internal index.

Parallel: (If included) implement your preferred success policy.

All composites call `Abort` on RUNNING children when they themselves stop to prevent dangling state.

## 🏃‍♂️ BehaviorTreeRunner

- Validates & deep clones tree on Awake/OnEnable
- Maintains original→clone map so child references stay intact
- Ticks root each Update
- Safe when pooled (recreates missing runtime clone on enable)

## 👥 Samples

1. Queue System Sample
	 - Reservation of waiting spots, queue ordering, timed leaving behavior (uses plain `DeadlineSequence`).
2. Game Setup Sample
	 - Minimal `GameManager` (space to start, escape to pause/quit) + UI panel logic.
	 - Pure convenience; not required for BT.

### Queue Domain Details

`AreaController` manages spots, enqueue/dequeue, cooldown gating, exit position fallback.
`NPCActionNode` (sample) narrows `ActionNode` for convenience helpers without polluting the runtime API.

## 🧪 Extending

- New Composite: inherit `CompositeNode`, manage children iteration state.
- Decorator (future): wrap child, forward lifecycle, transform result.
- Add debug overlays by reading each node's `LastState`.

## ⚠️ Design Notes

- Single explicit `LastState` simplifies reasoning.
- Domain kept out of Runtime (no queue/game logic references in core).
- DeadlineSequence kept minimal (no domain callbacks) to stay generic.
- Defensive null & fallback behaviors (NavMesh, exit position, queue limit).

## 🔧 Installation (UPM)

Option A (Git URL): Package Manager → Add package from git URL…
```
https://github.com/mthnblgn/SO_Based_BehaviorTree.git?path=Packages/com.mthn.behaviortree#v0.1.2
```
Or track a branch instead of a tag (not deterministic):
```
https://github.com/mthnblgn/SO_Based_BehaviorTree.git?path=Packages/com.mthn.behaviortree#refactor/upm-package
```
Use a version tag (recommended) for reproducible installs.

Quick steps:
1. Window > Package Manager
2. + (Add) > Add package from git URL…
3. Yapıştır & Add
4. (İsteğe bağlı) Samples bölümünden örnekleri Import et

Option B (Embedded Local): Clone repo; keep `Packages/com.mthn.behaviortree` inside project root.

Option C (Manual Copy): Copy `Runtime/` into `Assets/` (not recommended—harder updates).

### Importing Samples
1. Open Package Manager → select “Behavior Tree (ScriptableObject)”.
2. In Samples section click Import for:
	 - Queue System Sample
	 - Game Setup Sample
3. Open imported scenes or wire provided prefabs.
4. Modify freely (they are copies under `Assets/Samples/...`).

## ▶️ Minimal Usage Example

```csharp
public class PlayOnAwake : MonoBehaviour {
		public BehaviorTreeRunner runner;
		void Start() {
				// Runner auto ticks assigned tree
		}
}
```

## 🐞 Debugging Tips

- Log in `OnStart` for execution trace.
- Color-code Gizmos by `LastState` (SUCCESS=green, FAILURE=red, RUNNING=yellow).
- If stuck RUNNING forever: check child returning RUNNING and terminal conditions.
- Validate NavMeshAgent has a valid path (sample movement node does warp fallback).

## 📈 Roadmap

- Decorators (Inverter / Repeater / Succeeder)
- Graph editor (Unity UI Toolkit)
- Live inspector window (node state tree)

## 📜 License

MIT (see `LICENSE`).

## 🙌 Contributing

Issues & PRs welcome. Keep it lightweight; propose large features before implementing.

## ❤️ Acknowledgements

Inspired by standard BT literature & community patterns; adapted for ScriptableObject asset workflows + per-agent cloning.

---
If this helps you ship something, a GitHub star is appreciated.

