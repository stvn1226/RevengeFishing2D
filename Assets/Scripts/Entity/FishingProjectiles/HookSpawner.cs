using UnityEngine;
using Utils;

public class HookSpawner : MonoBehaviour
{
    [Header("Hook Settings")]
    public GameObject hookHandlerPrefab;
    public Transform spawnPoint;
    public float throwForce = 8f;

    [Header("Distance Control")]
    public float hookMaxDistance = 8f;

    // Store the original max distance
    private float originalMaxDistance;

    private GameObject currentHookHandler; // Changed from currentHook to currentHookHandler
    private HookPartsHandler curHookHandler;
    private FishingProjectile currentHook; // Reference to the actual hook component

    public FishingProjectile CurrentHook => currentHook;

    [SerializeField] private Vector2 throwDirection = new Vector2(1f, 0.2f);
    
    private void Awake()
    {
        originalMaxDistance = hookMaxDistance;
    }

    public void Initialize()
    {
        Debug.Log($"HookSpawner.Initialize() called on {gameObject.name}");

        // AUTO-SETUP MISSING REFERENCES
        if (hookHandlerPrefab == null)
        {
            // Try to find the prefab automatically
            hookHandlerPrefab = Resources.Load<GameObject>(AssetNames.HOOKHANDLER_PREFAB_NAME);
            if (hookHandlerPrefab == null)
            {
                Debug.LogError($"HookSpawner on {gameObject.name}: hookHandlerPrefab is missing! Assign it in Inspector or place FishingHookHandler in Resources folder!");
                return;
            }
            else
            {
                Debug.Log($"HookSpawner on {gameObject.name}: Auto-found hookHandlerPrefab in Resources");
            }
        }

        if (spawnPoint == null)
        {
            // Auto-create spawn point
            GameObject spawnGO = new GameObject("AutoHookSpawnPoint");
            spawnGO.transform.SetParent(transform);
            spawnGO.transform.localPosition = new Vector3(1f, 0f, 0f); // In front of fisherman
            spawnPoint = spawnGO.transform;
            Debug.Log($"HookSpawner on {gameObject.name}: Auto-created spawn point");
        }

        Debug.Log($"HookSpawner initialized successfully on {gameObject.name}: CanThrow={CanThrowHook()}");
    }

    public bool CanThrowHook()
    {
        bool canThrow = hookHandlerPrefab != null && spawnPoint != null && currentHook == null;
        Debug.Log($"CanThrowHook() on {gameObject.name}: Prefab={hookHandlerPrefab != null}, SpawnPoint={spawnPoint != null}, NoHook={currentHook == null} -> Result={canThrow}");
        return canThrow;
    }

    public void ThrowHook()
    {
        Debug.Log($"ThrowHook() called on {gameObject.name}: CanThrow={CanThrowHook()}");
        
        if (!CanThrowHook()) 
        {
            Debug.LogError($"ThrowHook() FAILED on {gameObject.name}: CanThrow=false");
            return;
        }
        
        throwDirection = new Vector2(Random.Range(0.2f, 1f), Random.Range(0.2f, 0.5f));

        // hookMaxDistance = originalMaxDistance;
        hookMaxDistance = originalMaxDistance + (Random.Range(-2f, 2f));

        // Instantiate the hook handler
        currentHookHandler = Instantiate(hookHandlerPrefab, spawnPoint.position, spawnPoint.rotation);
        curHookHandler = currentHookHandler.GetComponent<HookPartsHandler>();
        Debug.Log($"Instantiated hook handler: {currentHookHandler.name} at {spawnPoint.position}");

        // Find the actual fishing hook within the handler
        // currentHook = currentHookHandler.GetComponentInChildren<FishingProjectile>();
        currentHook = curHookHandler.FishingProjectile;
        
        if (currentHook != null)
        {
            // CRITICAL FIX: Set the spawn point BEFORE calling other methods
            currentHook.Initialize();
            currentHook.SetSpawnPoint(spawnPoint);

            currentHook.maxDistance = hookMaxDistance;
            currentHook.SetSpawner(this);

            // Setup water detection
            SetupWaterDetection();

            currentHook.ThrowProjectile(throwDirection, throwForce);

            Debug.Log($"Hook thrown successfully by {gameObject.name}! Spawn point: {spawnPoint.position}");
        }
        else
        {
            Debug.LogError($"No FishingProjectile found in {hookHandlerPrefab.name}!");
            Destroy(currentHookHandler);
            currentHookHandler = null;
            curHookHandler = null;
        }
    }

    private void SetupWaterDetection()
    {
        // WaterCheck waterCheck = currentHookHandler.GetComponentInChildren<WaterCheck>();
        WaterCheck waterCheck = curHookHandler.WaterCheck;

        if (waterCheck != null && currentHook != null)
        {
            // Configure the water check to monitor our hook
            waterCheck.targetCollider = currentHook.GetComponentInChildren<Collider2D>();

            // If the hook has EntityMovement, connect it
            Entity hookMovement = currentHook.GetComponent<Entity>();
            if (hookMovement != null)
            {
                waterCheck.entityMovement = hookMovement;
            }

            Debug.Log("Water detection configured for fishing hook!");
        }
        else
        {
            Debug.LogWarning("WaterCheck component not found in hook handler!");
        }
    }

    public void RetractHook(float retractionAmount)
    {
        if (currentHook != null)
        {
            float newLength = GetLineLength() - retractionAmount;

            if (newLength > 0.05f)
            {
                SetLineLength(newLength);

                // MOVE HOOK TOWARD SPAWN POINT
                Vector3 spawnPosition = currentHook.spawnPoint.position; // Access spawn point
                Vector3 currentPosition = currentHook.transform.position;
                Vector3 direction = (spawnPosition - currentPosition).normalized;

                // Move hook slightly toward spawn point for visual effect
                currentHook.transform.position += direction * (retractionAmount * 0.5f);

                if (currentHook.IsAboveWater) currentHook.GetComponentInChildren<CircleCollider2D>().enabled = false;

                //Debug.Log($"Hook being retracted gradually - remaining length: {newLength:F1}");
            }
            else
            {
                // When line is very short, start destruction
                currentHook.RetractProjectile();
                Debug.Log($"Hook retraction complete - destroying hook");
            }
        }
    }

    // Simple method to change line length
    public void SetLineLength(float newLength)
    {
        hookMaxDistance = newLength;

        if (currentHook != null)
        {
            currentHook.maxDistance = newLength;
        }
    }

    public float GetLineLength()
    {
        if (currentHook != null)
        {
            return currentHook.maxDistance;
        }

        return 0;
    }

    
    public bool HasActiveHook() => currentHookHandler != null;

    /// <summary>
    /// Destroys currentHookHandler and clean it's value
    /// Also clean currentHook and reset hookMaxDistance
    /// </summary>
    public void OnHookDestroyed()
    {
        if (currentHookHandler != null)
        {
            Destroy(currentHookHandler);
            currentHookHandler = null;
            curHookHandler = null;
        }
        currentHook = null;

        // Reset max distance when hook is destroyed
        hookMaxDistance = originalMaxDistance;

        Debug.Log($"Hook handler and references cleared for {gameObject.name}");
    }
}
