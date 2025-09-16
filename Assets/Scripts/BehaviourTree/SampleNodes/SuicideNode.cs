using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "Behavior Tree/Action/Suicide", fileName = "SuicideNode")]
public class SuicideNode : NPCActionNode
{
    [Header("Movement")]
    [Tooltip("How close to the edge counts as arrived (meters)")]
    public float arrivalThreshold = 0.25f;

    [Tooltip("Back off from edge by this many meters when picking target point (keeps agent on navmesh)")]
    public float edgeBackOffset = 0.15f;

    [Header("Fall")]
    [Tooltip("Forward push strength when starting the fall")]
    public float pushForce = 3.5f;

    [Tooltip("Small upward kick to make sure we clear the edge")]
    public float upKick = 0.5f;

    [Tooltip("Seconds to wait while falling before returning to pool")]
    public float fallDuration = 3f;

    private enum Phase { None, MovingToEdge, Falling }
    private Phase phase = Phase.None;

    private NPCController controller;
    private NavMeshAgent nmAgent;
    private Rigidbody rb;

    private bool hadRigidbodyBefore = false;
    private bool disabledNavThisNode = false;
    private bool prevKinematic = false;
    private bool prevUseGravity = true;

    private Vector3 edgePoint;
    private Vector3 fallDirection;
    private float fallTimer = 0f;

    protected override void OnStart(GameObject agent)
    {
        controller = GetController(agent);
        nmAgent = agent.GetComponent<NavMeshAgent>();

        if (controller == null || nmAgent == null)
        {
            Debug.LogWarning($"SuicideNode: Missing components on {agent?.name}");
            phase = Phase.None;
            return;
        }

        // Ensure the agent is on a NavMesh position
        if (nmAgent.enabled && !nmAgent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(agent.transform.position, out var hitSample, 2f, NavMesh.AllAreas))
            {
                nmAgent.Warp(hitSample.position);
            }
        }

        // Find closest edge on NavMesh
        if (NavMesh.FindClosestEdge(agent.transform.position, out var hit, NavMesh.AllAreas))
        {
            // Move towards a point slightly inside the mesh so the agent can reach it
            edgePoint = hit.position - hit.normal * Mathf.Max(0f, edgeBackOffset);
            // Fall direction is outward from the mesh
            fallDirection = (-hit.normal).normalized;

            nmAgent.isStopped = false;
            nmAgent.SetDestination(edgePoint);
            phase = Phase.MovingToEdge;
        }
        else
        {
            Debug.LogWarning($"SuicideNode: Could not find a NavMesh edge near {agent.name}.");
            phase = Phase.None;
        }
    }

    protected override NodeState OnUpdate(GameObject agent)
    {
        if (controller == null || nmAgent == null)
        {
            return NodeState.FAILURE;
        }

        switch (phase)
        {
            case Phase.MovingToEdge:
                // Keep destination synced in case path recalculates
                if (!nmAgent.pathPending)
                {
                    var dest = nmAgent.destination;
                    if ((dest - edgePoint).sqrMagnitude > 0.01f)
                    {
                        nmAgent.SetDestination(edgePoint);
                    }
                }

                if (controller.HasArrived(nmAgent, arrivalThreshold))
                {
                    StartFall(agent);
                }
                return NodeState.RUNNING;

            case Phase.Falling:
                fallTimer += Time.deltaTime;
                if (fallTimer >= fallDuration)
                {
                    return NodeState.SUCCESS;
                }
                return NodeState.RUNNING;

            default:
                return NodeState.FAILURE;
        }
    }

    private void StartFall(GameObject agent)
    {
        // Disable navmesh movement so physics can take over
        if (nmAgent.enabled)
        {
            if (nmAgent.isOnNavMesh)
            {
                nmAgent.ResetPath();
                nmAgent.velocity = Vector3.zero;
            }
            nmAgent.enabled = false;
            disabledNavThisNode = true;
        }

        // Use existing rigidbody if present, else add one temporarily
        rb = agent.GetComponent<Rigidbody>();
        hadRigidbodyBefore = rb != null;
        if (!hadRigidbodyBefore)
        {
            rb = agent.AddComponent<Rigidbody>();
        }

        // Configure physics for a clean fall
        prevKinematic = rb.isKinematic;
        prevUseGravity = rb.useGravity;
        rb.isKinematic = false;
        rb.useGravity = true;

        // Apply a nudge outward and slightly upward to ensure the agent clears the edge
        var push = fallDirection * Mathf.Max(0f, pushForce) + Vector3.up * Mathf.Max(0f, upKick);
        rb.AddForce(push, ForceMode.VelocityChange);

        fallTimer = 0f;
        phase = Phase.Falling;
    }

    protected override void OnStop(GameObject agent)
    {
        // Release reservations and queue state similar to LeavingAction
        if (controller != null)
        {
            controller.ReleaseWaitingPoint();
            var area = controller.GetCurrentArea();
            if (area != null)
            {
                area.Dequeue(controller);
            }
        }

        // Clean up components back to a pool-friendly state
        // Do NOT enable NavMeshAgent here; the agent is likely off-mesh (falling).
        // Leave it disabled and let spawner/controller safely re-enable after snapping to NavMesh.
        if (disabledNavThisNode && nmAgent != null)
        {
            // Intentionally left blank: keep nmAgent.disabled
        }

        if (rb != null)
        {
            if (!hadRigidbodyBefore)
            {
                // Remove the temporary rigidbody we added
                Object.Destroy(rb);
            }
            else
            {
                // Restore original settings
                rb.isKinematic = prevKinematic;
                rb.useGravity = prevUseGravity;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // Finally return to pool
        if (controller != null)
        {
            controller.Despawn();
        }

        // Reset locals for safety (in case of reuse of runtime clone)
        phase = Phase.None;
        rb = null;
        disabledNavThisNode = false;
        hadRigidbodyBefore = false;
        prevKinematic = false;
        prevUseGravity = true;
        fallTimer = 0f;
    }
}
