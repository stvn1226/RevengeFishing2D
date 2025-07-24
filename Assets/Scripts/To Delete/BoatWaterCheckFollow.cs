using UnityEngine;

// ! Can be deleted - Keep testing

/// <summary>
/// Enhanced version of WaterCheckFollow specifically for boat systems
/// Replaces WaterCheckFollow functionality and adds boat safety features
/// </summary>
public class BoatWaterCheckFollow : MonoBehaviour
{
    [Header("Base WaterCheckFollow Settings")]
    public float waterSurfaceY = 5.92f;
    public GameObject target;
    
    [Header("🚤 BOAT SAFETY EXTENSIONS")]
    [SerializeField] private float minDistanceFromBoat = 1.0f; // Minimum Y distance below boat
    [SerializeField] private bool enableBoatSafetyCheck = true;
    [SerializeField] private bool debugBoatPositioning = false;
    
    private Transform boatTransform;
    private BoatFloater associatedBoat;

    void Start()
    {
        // Original WaterCheckFollow functionality
        GetComponent<Collider2D>().isTrigger = true;
        
        // Find the associated boat
        FindAssociatedBoat();
        
        if (debugBoatPositioning)
        {
            Debug.Log($"🚤 BoatWaterCheckFollow: Initialized with boat safety for {gameObject.name}");
        }
    }

    void Update()
    {
        if (target == null) return;
        
        // Calculate safe Y position if boat safety is enabled
        if (enableBoatSafetyCheck && ShouldApplyBoatSafety())
        {
            UpdateWithBoatSafety();
        }
        else
        {
            // Use original WaterCheckFollow behavior
            transform.position = new Vector3(target.transform.position.x, waterSurfaceY, transform.position.z);
        }
    }
    
    private void FindAssociatedBoat()
    {
        // Look for boat in parent hierarchy (BoatFishermanHandler should be child of boat)
        Transform current = transform.parent;
        while (current != null)
        {
            BoatFloater boat = current.GetComponent<BoatFloater>();
            if (boat != null)
            {
                associatedBoat = boat;
                boatTransform = boat.transform;
                break;
            }
            current = current.parent;
        }
        
        // If no boat found in parents, look for BoatHandler as sibling
        if (associatedBoat == null && transform.parent != null)
        {
            BoatFloater boat = transform.parent.GetComponentInChildren<BoatFloater>();
            if (boat != null)
            {
                associatedBoat = boat;
                boatTransform = boat.transform;
            }
        }
        
        // Last resort: search in the scene for nearby boats
        if (associatedBoat == null)
        {
            BoatFloater[] allBoats = FindObjectsOfType<BoatFloater>();
            foreach (BoatFloater boat in allBoats)
            {
                float distance = Vector3.Distance(transform.position, boat.transform.position);
                if (distance < 20f) // Within 20 units
                {
                    associatedBoat = boat;
                    boatTransform = boat.transform;
                    break;
                }
            }
        }
        
        if (debugBoatPositioning)
        {
            string status = associatedBoat != null ? $"Found: {associatedBoat.name}" : "Not found";
            Debug.Log($"🚤 Associated boat: {status}");
        }
    }
    
    private bool ShouldApplyBoatSafety()
    {
        return associatedBoat != null && boatTransform != null;
    }
    
    private void UpdateWithBoatSafety()
    {
        if (target == null) return;
        
        // Calculate safe waterline position relative to boat
        float boatY = boatTransform.position.y;
        float safeWaterlineY = boatY - minDistanceFromBoat;
        
        // Use the lower position (safer) between default and calculated
        float targetY = Mathf.Min(waterSurfaceY, safeWaterlineY);
        
        // Follow target horizontally, stay at calculated safe Y
        transform.position = new Vector3(target.transform.position.x, targetY, transform.position.z);
        
        if (debugBoatPositioning)
        {
            Debug.Log($"🚤 Boat Safety: Boat Y={boatY:F2}, Safe Y={safeWaterlineY:F2}, Using Y={targetY:F2}");
        }
    }
    
    /// <summary>
    /// Public method to manually adjust minimum distance
    /// </summary>
    public void SetMinimumDistanceFromBoat(float distance)
    {
        minDistanceFromBoat = distance;
    }
    
    /// <summary>
    /// Public method to enable/disable boat safety
    /// </summary>
    public void SetBoatSafetyEnabled(bool enabled)
    {
        enableBoatSafetyCheck = enabled;
    }
    
    /// <summary>
    /// Get current safe distance status
    /// </summary>
    public bool IsPositionSafe()
    {
        if (!ShouldApplyBoatSafety()) return true;
        
        float currentDistance = boatTransform.position.y - transform.position.y;
        return currentDistance >= minDistanceFromBoat;
    }
    
    #if UNITY_EDITOR
    [ContextMenu("🧪 TEST: Check Current Safety")]
    private void TestCurrentSafety()
    {
        bool isSafe = IsPositionSafe();
        Debug.Log($"🧪 Current Safety Status: {(isSafe ? "SAFE" : "UNSAFE")}");
        
        if (ShouldApplyBoatSafety())
        {
            float distance = boatTransform.position.y - transform.position.y;
            Debug.Log($"🧪 Current distance from boat: {distance:F2} (minimum: {minDistanceFromBoat:F2})");
        }
    }
    
    [ContextMenu("🧪 TEST: Force Find Boat")]
    private void TestFindBoat()
    {
        FindAssociatedBoat();
        TestCurrentSafety();
    }
    #endif
}