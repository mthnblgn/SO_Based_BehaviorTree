using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class AreaController : MonoBehaviour
{
    [SerializeField] private Transform spotParent;
    [SerializeField] private List<WaitingSpot> waitingSpotList;
    // Simple FIFO queue for NPCs waiting to claim a spot
    private readonly List<NPCController> npcQueue = new List<NPCController>();

    [Header("Queue Setup")]
    [Tooltip("Anchor transform for the physical queue. Index 0 stands at this position.")]
    [SerializeField] private Transform queueAnchor;
    [Tooltip("Distance between two adjacent NPCs in the queue.")]
    [SerializeField] private float queueSpacing = 0.8f;
    [Tooltip("Queue direction from the anchor. If null, uses -anchor.forward.")]
    [SerializeField] private Vector3 queueDirection = Vector3.back;

    [Header("Queue Flow Control")]
    [Tooltip("Minimum delay between consecutive NPCs leaving the queue (seconds)")]
    [SerializeField] private float queueCooldown = 0.4f;
    [Header("Queue Limit")]
    [Tooltip("Maximum number of NPCs allowed in the queue. 0 = unlimited.")]
    [SerializeField] private int queueLimit = 15;
    private float nextReleaseTime = 0f;

    [Header("Exit Setup")]
    [Tooltip("NPCs will head here when leaving.")]
    [SerializeField] private Transform exitAnchor;
    void Start()
    {
        AddChilderenAsSpot(spotParent, out waitingSpotList);
    }

    // Helper to reserve one available spot for the requester atomically
    public WaitingSpot TryReserveAnySpot(NPCController requester)
    {
        if (requester == null) return null;
        CleanupQueue();

        int count = waitingSpotList.Count;
        if (count == 0) return null;
        if (AnyAvailableSpot() == false) return null;
        int start = Random.Range(0, count);
        for (int i = 0; i < count; i++)
        {
            var spot = waitingSpotList[(start + i) % count];
            if (spot != null && spot.IsAvailable())
            {
                if (spot.TryReserve(requester))
                {
                    return spot;
                }
            }
        }
        return null;
    }

    public bool AnyAvailableSpot()
    {
        foreach (var spot in waitingSpotList)
        {
            if (spot != null && spot.IsAvailable()) return true;
        }
        return false;
    }

    // Queue management
    public void EnqueueNPC(NPCController npc)
    {
        if (npc == null) return;
        CleanupQueue();
        bool wasEmpty = npcQueue.Count == 0;
        if (!npcQueue.Contains(npc))
        {
            npcQueue.Add(npc);
            // If queue was empty, this NPC just became the head: start cooldown now
            if (wasEmpty && queueCooldown > 0f)
            {
                nextReleaseTime = Time.time + queueCooldown;
            }
        }
    }

    // Enqueue and immediately return the world position for this NPC's slot (for spawning at correct place)
    public Vector3 EnqueueAndGetPosition(NPCController npc)
    {
        EnqueueNPC(npc);
        int idx = GetQueuePosition(npc);
        return GetQueueWorldPosition(idx < 0 ? 0 : idx);
    }

    public bool IsInQueue(NPCController npc)
    {
        if (npc == null) return false;
        CleanupQueue();
        return npcQueue.Contains(npc);
    }

    public bool IsFirst(NPCController npc)
    {
        if (npc == null) return false;
        CleanupQueue();
        if (npcQueue.Count == 0) return false;
        if (npcQueue[0] != npc) return false;
        // Apply cooldown gating so agents don't leave in bursts
        return Time.time >= nextReleaseTime;
    }

    public bool DequeueIfFirst(NPCController npc)
    {
        if (npc == null) return false;
        // Note: We check strictly the head equality here; cooldown handled by IsFirst callers
        CleanupQueue();
        if (npcQueue.Count > 0 && npcQueue[0] == npc)
        {
            npcQueue.RemoveAt(0);
            // Set next allowed release time (next head just reached the front)
            if (npcQueue.Count > 0 && queueCooldown > 0f)
            {
                nextReleaseTime = Time.time + queueCooldown;
            }
            else
            {
                // If queue is empty, do not block future entrants; they will set cooldown on enqueue
                nextReleaseTime = 0f;
            }
            return true;
        }
        return false;
    }

    public void Dequeue(NPCController npc)
    {
        if (npc == null) return;
        CleanupQueue();
        npcQueue.Remove(npc);
    }

    public int GetQueuePosition(NPCController npc)
    {
        if (npc == null) return -1;
        CleanupQueue();
        return npcQueue.IndexOf(npc);
    }

    public int GetQueueLength()
    {
        CleanupQueue();
        return npcQueue.Count;
    }

    private void CleanupQueue()
    {
        // Remove nulls or disabled GOs that may remain after despawn
        for (int i = npcQueue.Count - 1; i >= 0; i--)
        {
            var n = npcQueue[i];
            if (n == null || !n.gameObject.activeInHierarchy)
            {
                npcQueue.RemoveAt(i);
            }
        }
    }

    // Physical queue helpers
    public bool TryGetQueuePosition(NPCController npc, out Vector3 position)
    {
        position = default;
        if (npc == null) return false;
        int idx = GetQueuePosition(npc);
        if (idx < 0) return false;
        CleanupQueue();
        position = GetQueueWorldPosition(idx);
        return true;
    }

    public Vector3 GetQueueWorldPosition(int index)
    {
        if (queueAnchor == null)
        {
            // Fallback: use this transform as anchor
            var dir = queueDirection != Vector3.zero ? queueDirection.normalized : -transform.forward;
            return transform.position + dir * (queueSpacing * index);
        }
        else
        {
            var dir = queueDirection != Vector3.zero ? queueDirection.normalized : -queueAnchor.forward;
            return queueAnchor.position + dir * (queueSpacing * index);
        }
    }

    public Vector3 GetExitPosition()
    {
        return exitAnchor.position;
    }

    private void AddChilderenAsSpot(Transform spotsParent, out List<WaitingSpot> waitingSpotList)
    {
        waitingSpotList = new List<WaitingSpot>();
        foreach (Transform child in spotsParent)
        {
            var spot = child.GetComponent<WaitingSpot>();
            if (spot != null)
                waitingSpotList.Add(spot);
        }
    }
    public bool IsQueueFull()
    {
        if (queueLimit <= 0) return false; // Unlimited
        CleanupQueue();
        return npcQueue.Count >= queueLimit;
    }
}
