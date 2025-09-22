using UnityEngine;

public class WaitingSpot : MonoBehaviour
{
    [Header("State")]
    public bool isOccupied = false;
    [SerializeField] private bool isReserved = false;

    [Header("Refs/Visuals")]
    public Transform SpotTransform => transform;
    [SerializeField] private Material availableMaterial;
    [SerializeField] private Material occupiedMaterial;
    private Renderer _spotRenderer;

    // Track who reserved/occupies to avoid stealing
    private NPCController reservedBy;
    private NPCController occupiedBy;

    private void Awake()
    {
        _spotRenderer = GetComponent<Renderer>();
        UpdateMaterial();
    }

    // Backward-compatible setter used by legacy code
    public void SetOccupied(bool status)
    {
        isOccupied = status;
        if (!status)
        {
            occupiedBy = null;
        }
        UpdateMaterial();
    }

    public bool IsAvailable() => !isOccupied && !isReserved;

    public bool TryReserve(NPCController requester)
    {
        if (requester == null) return false;
        if (isOccupied || isReserved) return false;
        isReserved = true;
        reservedBy = requester;
        UpdateMaterial();
        return true;
    }

    public bool Occupy(NPCController requester)
    {
        if (requester == null) return false;
        if (isOccupied) return false;
        // If reserved by someone else, deny
        if (isReserved && reservedBy != requester) return false;

        isReserved = false;
        reservedBy = null;
        isOccupied = true;
        occupiedBy = requester;
        UpdateMaterial();
        return true;
    }

    public void Release(NPCController requester)
    {
        if (requester == null)
        {
            // Fallback: clear both states
            isReserved = false;
            reservedBy = null;
            isOccupied = false;
            occupiedBy = null;
            UpdateMaterial();
            return;
        }

        if (occupiedBy == requester)
        {
            isOccupied = false;
            occupiedBy = null;
        }
        else if (reservedBy == requester)
        {
            isReserved = false;
            reservedBy = null;
        }
        UpdateMaterial();
    }

    private void UpdateMaterial()
    {
        if (_spotRenderer != null)
        {
            // Only change color when truly occupied; reservation alone shouldn't alter appearance
            _spotRenderer.material = isOccupied ? occupiedMaterial : availableMaterial;
        }
    }
    
}
