using UnityEngine;

public class BoatBoundsController : MonoBehaviour
{
    [Header("Boat Bounds Configuration")]
    [SerializeField] private float edgeBuffer = 0.5f;
    [SerializeField] private bool enableDebugMessages = true;
    
    [Header("State Management")]
    [SerializeField] private bool enableBoundsForStates = true;
    [SerializeField] private bool onlyApplyToIdleFishermen = false;
    
    [Header("Gizmos Configuration")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color boundsColor = Color.cyan;
    [SerializeField] private Color centerColor = Color.red;
    [SerializeField] private Color fishermanColor = Color.green;
    [SerializeField] private Color violationColor = Color.magenta;
    [SerializeField] private Color pulledColor = Color.yellow;
    [SerializeField] private Color disabledColor = Color.gray;
    
    [Header("Runtime Status - Auto Populated")]
    [SerializeField] private float maxDistanceFromBoatCenterX;
    [SerializeField] private float maxDistanceFromBoatCenterY;
    [SerializeField] private Transform boatCenter;
    [SerializeField] private bool boundsSetup = false;
    [SerializeField] private bool boundsCurrentlyActive = false;
    
    [Header("Hook Detection - Debug Info")]
    [SerializeField] private bool isBeingPulledByHook = false;
    [SerializeField] private FishingProjectile myActiveHook = null;
    [SerializeField] private Player playerReference = null;
    
    [Header("State Debug Info")]
    [SerializeField] private Enemy.EnemyState currentEnemyState;
    [SerializeField] private LandEnemy.LandMovementState currentMovementState;
    [SerializeField] private bool fishingToolEquipped = false;
    
    [Header("Local Position Debug Info")]
    [SerializeField] private Vector3 localPositionInBoat;    // Posición actual en coordenadas locales del barco
    [SerializeField] private float distanceFromBoatCenterX; // Distancia X desde centro del barco
    [SerializeField] private float distanceFromBoatCenterY; // Distancia Y desde centro del barco
    [SerializeField] private bool isWithinBoundsX = true;   // Si está dentro de límites X
    [SerializeField] private bool isWithinBoundsY = true;   // Si está dentro de límites Y
    
    private Rigidbody2D rb;
    private LandEnemy landEnemy;
    private Fisherman fisherman;
    private HookSpawner hookSpawner;
    private bool hasTriedSetup = false;
    private bool isViolatingBounds = false;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        landEnemy = GetComponent<LandEnemy>();
        fisherman = GetComponent<Fisherman>();
        hookSpawner = GetComponent<HookSpawner>();
    }
    
    private void Start()
    {
        // Find player reference
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerReference = playerObj.GetComponent<Player>();
        }
        
        TrySetupBoatBounds();
    }
    
    private void Update()
    {
        // Update debug info
        UpdateDebugInfo();
        
        // Keep trying setup until platform is assigned
        if (!boundsSetup && !hasTriedSetup)
        {
            TrySetupBoatBounds();
        }
        
        // Determine if bounds should be active based on current state
        UpdateBoundsActiveState();
        
        // Only check bounds if they should be active
        if (boundsSetup && boundsCurrentlyActive)
        {
            CheckBoatBounds();
        }
    }
    
    private void UpdateDebugInfo()
    {
        if (landEnemy != null)
        {
            currentEnemyState = landEnemy.State;
            currentMovementState = landEnemy.MovementStateLand;
            fishingToolEquipped = landEnemy.fishingToolEquipped;
        }
        
        // Update local position debug info
        if (boatCenter != null)
        {
            localPositionInBoat = boatCenter.InverseTransformPoint(transform.position);
            distanceFromBoatCenterX = Mathf.Abs(localPositionInBoat.x);
            distanceFromBoatCenterY = Mathf.Abs(localPositionInBoat.y);
            isWithinBoundsX = distanceFromBoatCenterX <= maxDistanceFromBoatCenterX;
            isWithinBoundsY = distanceFromBoatCenterY <= maxDistanceFromBoatCenterY;
        }
    }
    
    private void UpdateBoundsActiveState()
    {
        bool wasPulled = isBeingPulledByHook;
        
        // Check if fisherman is being pulled by player's hook
        CheckForHookPulling();
        
        // Determine if bounds should be active
        boundsCurrentlyActive = ShouldBoundsBeActive();
        
        // Log state changes for debugging
        if (enableDebugMessages && wasPulled != isBeingPulledByHook)
        {
            string reason = isBeingPulledByHook ? "being pulled by hook" : "no longer being pulled";
            Debug.Log($"BOAT BOUNDS: {gameObject.name} - {reason} - bounds {(boundsCurrentlyActive ? "ACTIVE" : "DISABLED")}");
        }
    }
    
    private bool ShouldBoundsBeActive()
    {
        // Don't apply bounds if not setup
        if (!boundsSetup || !enableBoundsForStates) return false;
        
        // Don't apply bounds if fisherman is being pulled by hook
        if (isBeingPulledByHook) return false;
        
        // Don't apply bounds if enemy is not alive
        if (landEnemy != null && landEnemy.State != Enemy.EnemyState.Alive) return false;
        
        // Don't apply bounds if fisherman is defeated, eaten, or dead
        if (currentEnemyState == Enemy.EnemyState.Defeated || 
            currentEnemyState == Enemy.EnemyState.Eaten || 
            currentEnemyState == Enemy.EnemyState.Dead) 
        {
            return false;
        }
        
        // If configured to only apply to idle fishermen
        if (onlyApplyToIdleFishermen)
        {
            return currentMovementState == LandEnemy.LandMovementState.Idle && !fishingToolEquipped;
        }
        
        return true;
    }
    
    private void CheckForHookPulling()
    {
        isBeingPulledByHook = false;
        myActiveHook = null;
        
        // Method 1: Check if this fisherman's own hook is being held by player
        if (hookSpawner != null && hookSpawner.CurrentHook != null)
        {
            FishingProjectile fishermanHook = hookSpawner.CurrentHook;
            if (fishermanHook.isBeingHeld)
            {
                isBeingPulledByHook = true;
                myActiveHook = fishermanHook;
                return;
            }
        }
        
        // Method 2: Check if player is holding ANY hook that could affect this fisherman
        if (playerReference != null && playerReference.activeBitingHooks != null)
        {
            foreach (FishingProjectile hook in playerReference.activeBitingHooks)
            {
                if (hook != null && hook.spawner != null && hook.spawner == hookSpawner)
                {
                    isBeingPulledByHook = true;
                    myActiveHook = hook;
                    return;
                }
            }
        }
    }
    
    private void TrySetupBoatBounds()
    {
        if (landEnemy?.assignedPlatform == null) 
        {
            return;
        }

        hasTriedSetup = true;

        BoatPlatform boatPlatform = landEnemy.assignedPlatform as BoatPlatform;
        if (boatPlatform == null)
        {
            if (enableDebugMessages)
                Debug.LogWarning($"{gameObject.name} - Assigned platform is not a BoatPlatform, bounds disabled");
            return;
        }

        // Use BoatPlatform as the center reference
        boatCenter = boatPlatform.transform;
        
        Collider2D platformCol = boatPlatform.GetComponent<Collider2D>();
        
        if (platformCol != null)
        {
            // Get platform bounds
            Bounds platformBounds = platformCol.bounds;
            
            // Calculate max distances from boat center (in local coordinates)
            float platformWidth = platformBounds.size.x;
            float platformHeight = platformBounds.size.y;
            
            // Maximum allowed distance from boat center with buffer
            maxDistanceFromBoatCenterX = (platformWidth * 0.5f) - edgeBuffer;
            maxDistanceFromBoatCenterY = (platformHeight * 0.5f) - edgeBuffer;
            
            // Ensure minimum values
            maxDistanceFromBoatCenterX = Mathf.Max(maxDistanceFromBoatCenterX, 0.5f);
            maxDistanceFromBoatCenterY = Mathf.Max(maxDistanceFromBoatCenterY, 0.5f);
            
            boundsSetup = true;
            
            if (enableDebugMessages)
            {
                Debug.Log($"BOAT BOUNDS: {gameObject.name} setup complete:\n" +
                         $"- MaxDistanceX: {maxDistanceFromBoatCenterX:F2}\n" +
                         $"- MaxDistanceY: {maxDistanceFromBoatCenterY:F2}\n" +
                         $"- Buffer: {edgeBuffer:F2}\n" +
                         $"- Platform: {boatPlatform.name}\n" +
                         $"- Platform Size: {platformWidth:F2}x{platformHeight:F2}");
            }
        }
        else
        {
            Debug.LogError($"{gameObject.name} - BoatPlatform {boatPlatform.name} has no Collider2D for bounds calculation!");
        }
    }
    
    // SIMPLE: Check bounds using local coordinates relative to boat center
    private void CheckBoatBounds()
    {
        if (boatCenter == null) return;
        
        // Get current position in boat's local coordinate system
        Vector3 localPos = boatCenter.InverseTransformPoint(transform.position);
        
        // Check if outside bounds in X or Y
        bool needsClampingX = Mathf.Abs(localPos.x) > maxDistanceFromBoatCenterX;
        bool needsClampingY = Mathf.Abs(localPos.y) > maxDistanceFromBoatCenterY;
        
        isViolatingBounds = needsClampingX || needsClampingY;
        
        if (isViolatingBounds)
        {
            // Stop movement
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            
            // Clamp position to bounds
            Vector3 clampedLocal = localPos;
            
            if (needsClampingX)
            {
                float direction = Mathf.Sign(localPos.x);
                clampedLocal.x = direction * maxDistanceFromBoatCenterX;
            }
            
            if (needsClampingY)
            {
                float direction = Mathf.Sign(localPos.y);
                clampedLocal.y = direction * maxDistanceFromBoatCenterY;
            }
            
            // Convert back to world coordinates and apply
            Vector3 clampedWorld = boatCenter.TransformPoint(clampedLocal);
            transform.position = clampedWorld;
            
            StopFishermanMovement();
            
            if (enableDebugMessages)
            {
                Debug.Log($"BOAT BOUNDS: {gameObject.name} clamped!\n" +
                         $"- X: {needsClampingX} ({localPos.x:F2} -> {clampedLocal.x:F2})\n" +
                         $"- Y: {needsClampingY} ({localPos.y:F2} -> {clampedLocal.y:F2})");
            }
        }
    }
    
    private void StopFishermanMovement()
    {
        if (landEnemy != null)
        {
            landEnemy.MovementStateLand = LandEnemy.LandMovementState.Idle;
        }
    }
    
    /// <summary>
    /// Called when fisherman is returned to pool - resets all bounds state
    /// </summary>
    public void ResetForPooling()
    {
        boundsSetup = false;
        boundsCurrentlyActive = false;
        hasTriedSetup = false;
        isViolatingBounds = false;
        isBeingPulledByHook = false;
        
        boatCenter = null;
        myActiveHook = null;
        maxDistanceFromBoatCenterX = 0f;
        maxDistanceFromBoatCenterY = 0f;
        
        currentEnemyState = Enemy.EnemyState.Alive;
        currentMovementState = LandEnemy.LandMovementState.Idle;
        fishingToolEquipped = false;
        
        if (enableDebugMessages)
        {
            Debug.Log($"BOAT BOUNDS: {gameObject.name} reset for pooling");
        }
    }
    
    /// <summary>
    /// Called when fisherman is spawned from pool - triggers initial setup
    /// </summary>
    public void OnSpawnFromPool()
    {
        ResetForPooling();
        TrySetupBoatBounds();
        
        if (enableDebugMessages)
        {
            Debug.Log($"BOAT BOUNDS: {gameObject.name} spawned from pool - setup triggered");
        }
    }
    
    // GIZMOS - Show boat bounds
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        DrawBoundsGizmos(0.3f);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        DrawBoundsGizmos(1f);
        DrawDetailedInfo();
    }
    
    private void DrawBoundsGizmos(float alpha)
    {
        if (!boundsSetup || boatCenter == null) return;
        
        Vector3 boatCenterWorld = boatCenter.position;
        
        // Draw boat center
        Gizmos.color = new Color(centerColor.r, centerColor.g, centerColor.b, alpha);
        Gizmos.DrawSphere(boatCenterWorld, 0.15f);
        
        // Choose color based on bounds state
        Color boundsGizmoColor;
        if (!boundsCurrentlyActive)
        {
            boundsGizmoColor = disabledColor;
        }
        else if (isBeingPulledByHook)
        {
            boundsGizmoColor = pulledColor;
        }
        else
        {
            boundsGizmoColor = boundsColor;
        }
        
        // Draw bounds rectangle
        Gizmos.color = new Color(boundsGizmoColor.r, boundsGizmoColor.g, boundsGizmoColor.b, alpha * 0.5f);
        Vector3 boundsSize = new Vector3(maxDistanceFromBoatCenterX * 2, maxDistanceFromBoatCenterY * 2, 0.1f);
        Gizmos.DrawWireCube(boatCenterWorld, boundsSize);
        
        // Draw fisherman position
        Color fishermanGizmoColor;
        if (!boundsCurrentlyActive)
        {
            fishermanGizmoColor = disabledColor;
        }
        else if (isBeingPulledByHook)
        {
            fishermanGizmoColor = pulledColor;
        }
        else if (isViolatingBounds)
        {
            fishermanGizmoColor = violationColor;
        }
        else
        {
            fishermanGizmoColor = fishermanColor;
        }
        
        Gizmos.color = new Color(fishermanGizmoColor.r, fishermanGizmoColor.g, fishermanGizmoColor.b, alpha);
        Gizmos.DrawSphere(transform.position, 0.12f);
        
        // Draw line from boat center to fisherman
        Gizmos.color = new Color(fishermanGizmoColor.r, fishermanGizmoColor.g, fishermanGizmoColor.b, alpha * 0.6f);
        Gizmos.DrawLine(boatCenterWorld, transform.position);
        
        // Draw platform bounds for reference
        Collider2D platformCol = boatCenter.GetComponent<Collider2D>();
        if (platformCol != null)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 1f, alpha * 0.2f);
            Bounds platformBounds = platformCol.bounds;
            Gizmos.DrawWireCube(platformBounds.center, platformBounds.size);
        }
        
        // Draw connection to active hook if being pulled
        if (isBeingPulledByHook && myActiveHook != null)
        {
            Gizmos.color = new Color(1f, 0f, 1f, alpha * 0.8f);
            Gizmos.DrawLine(transform.position, myActiveHook.transform.position);
        }
    }
    
    private void DrawDetailedInfo()
    {
        if (!boundsSetup || boatCenter == null) return;
        
        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        Vector3 midPoint = (boatCenter.position + transform.position) * 0.5f;
        
        string statusText = boundsCurrentlyActive ? "ACTIVE" : "DISABLED";
        string pullText = isBeingPulledByHook ? "PULLED" : "NORMAL";
        string boundsInfo = $"X:{distanceFromBoatCenterX:F2}/{maxDistanceFromBoatCenterX:F2} Y:{distanceFromBoatCenterY:F2}/{maxDistanceFromBoatCenterY:F2}";
        string validInfo = $"ValidX:{isWithinBoundsX} ValidY:{isWithinBoundsY}";
        
        UnityEditor.Handles.Label(midPoint, $"Bounds: {statusText}\nPull: {pullText}\n{boundsInfo}\n{validInfo}\nLocal: {localPositionInBoat}");
        #endif
    }
    
    // Public methods
    public void ForceSetupBounds()
    {
        hasTriedSetup = false;
        boundsSetup = false;
        TrySetupBoatBounds();
    }
    
    public bool IsBoundsSetup => boundsSetup;
    public bool AreBoundsActive => boundsCurrentlyActive;
    public float MaxDistanceX => maxDistanceFromBoatCenterX;
    public float MaxDistanceY => maxDistanceFromBoatCenterY;
    public Transform BoatCenter => boatCenter;
    public bool IsBeingPulled => isBeingPulledByHook;
    public Vector3 LocalPositionInBoat => localPositionInBoat;
    
    private void OnDestroy()
    {
        ResetForPooling();
    }
}
