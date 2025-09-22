using System.Collections.Generic;
using UnityEngine;
using Game.Patterns;

public class NPCSpawner : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Optional: Multiple prefabs to randomize between")]
    [SerializeField] private List<NPCController> npcPrefabs = new List<NPCController>();
    [SerializeField] private AreaController area;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private int initialPoolSize = 3;
    [SerializeField] private bool spawnAtQueue = true;
    [SerializeField] private float spawnTime = 1f;
    private float timer = 0f;

    private readonly List<ObjectPool<NPCController>> pools = new List<ObjectPool<NPCController>>();

    private void Awake()
    {
        // Build pools for provided prefabs (multiple or single fallback)
        if (npcPrefabs != null)
        {
            for (int i = 0; i < npcPrefabs.Count; i++)
            {
                var prefab = npcPrefabs[i];
                if (prefab != null)
                {
                    pools.Add(new ObjectPool<NPCController>(prefab, initialPoolSize, transform));
                }
            }
        }
    }
    void Update()
    {
        timer += Time.deltaTime;
        if(area.IsQueueFull() == false && timer >= spawnTime)
        {
            Spawn();
            timer = 0f;
        }
    }

    public NPCController Spawn()
    {
        // Pick a random pool (hence random prefab)
        if (pools.Count == 0)
        {
            Debug.LogWarning("NPCSpawner: No NPC prefabs/pools configured.");
            return null;
        }

        var chosenPool = pools[Random.Range(0, pools.Count)];
        var npc = chosenPool.Get();
        npc.SetPool(chosenPool);
        npc.SetCurrentArea(area);

        if (spawnAtQueue && area != null)
        {
            // Enqueue first to get a stable slot, then place NPC there
            var pos = area.EnqueueAndGetPosition(npc);
            npc.transform.position = pos;
            // Align rotation to queue direction (approx.)
            // Derive direction from anchor if available
            var anchor = area.transform; // fallback
            npc.transform.rotation = Quaternion.LookRotation(anchor.forward, Vector3.up);
        }
        else
        {
            npc.transform.position = GetSpawnPosition();
            npc.transform.rotation = Quaternion.identity;
            // Ensure not double-enqueued; QueueForWaitingSpot will enqueue later
        }

        // After placing, ensure NavMeshAgent is valid and on navmesh
        var nav = npc.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (nav != null)
        {
            // Keep disabled during pooling; enable if we can snap to navmesh
            if (!nav.enabled)
            {
                // Try to snap and enable via controller helper
                npc.EnsureNavAgentOnNavMesh(5f);
            }
            else if (!nav.isOnNavMesh)
            {
                // If enabled but off-mesh, try to sample and warp
                if (UnityEngine.AI.NavMesh.SamplePosition(npc.transform.position, out var hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    nav.Warp(hit.position);
                }
                else
                {
                    // As a last resort, disable to avoid API spam; it will be re-enabled when needed
                    nav.enabled = false;
                }
            }
        }
        // Re-enable BT runner after we ensured agent placement is valid
        var runner = npc.GetComponent<BehaviorTreeRunner>();
        if (runner != null)
        {
            runner.enabled = true;
        }
        return npc;
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Count > 0)
        {
            var t = spawnPoints[Random.Range(0, spawnPoints.Count)];
            if (t != null) return t.position;
        }
        return transform.position;
    }
}
