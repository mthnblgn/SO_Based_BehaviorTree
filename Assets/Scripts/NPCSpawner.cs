using System.Collections.Generic;
using UnityEngine;
using Game.Paterns;

public class NPCSpawner : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Optional: Multiple prefabs to randomize between")]
    [SerializeField] private List<NPCController> npcPrefabs = new List<NPCController>();
    [SerializeField] private AreaController area;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private int initialPoolSize = 3;
    [SerializeField] private bool spawnAtQueue = true;

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

    public NPCController Spawn()
    {
        // Pick a random pool (hence random prefab)
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
