using UnityEngine;

public class BoatFloater : MonoBehaviour
{
    [Header("Float Points")]
    public Transform[] floatPoints = new Transform[3];

    [Header("Components to move the boat")]

    [Tooltip("Testing only - Set to \" 0 \" HorizontalForce & MaxHorizontalForce for normal gameplay")]
    [SerializeField] private float horizontalForce = 0f;

    [Tooltip("Testing only - Set to \" 0 \" HorizontalForce & MaxHorizontalForce for normal gameplay")]
    [SerializeField] private float maxHorizontalForce = 5f;

    [Tooltip("Gameplay only - Set to \" 60 \", MovementSpeed & MaxMovementForce for normal speed")]

    [SerializeField] private float movementSpeed = 2f;

    [Tooltip("Gameplay only - Set to \" 60 \", MovementSpeed & MaxMovementForce for normal speed")]
    [SerializeField] private float maxMovementForce = 6f;

    [Header("Movement Control")]
    [SerializeField] private bool enableFloaterMovement = true;
    [SerializeField] private bool adaptToScaleDirection = true;

    [Header("Visual Direction")]
    [SerializeField] private SpriteRenderer boatSpriteRenderer;
    [SerializeField] private bool useSpriteFip = true;

    [Header("BOAT MOVEMENT SYSTEM")]
    [SerializeField] private bool enableAutomaticMovement = true;


    [SerializeField] private float movementChangeInterval = 3f;
    [SerializeField] private float minMovementTime = 2f;
    [SerializeField] private bool debugMovement = false;

    [Header("Platform References")]
    [SerializeField] private Platform[] cachedPlatforms;

    [Header("Buoyancy Settings")]
    [SerializeField] private float buoyancyForce = 25f;
    [SerializeField] private float waterDrag = 0.85f;
    [SerializeField] private float angularDrag = 0.7f;

    [Header("DYNAMIC MASS COMPENSATION")]
    [SerializeField] private bool enableDynamicBuoyancy = true;
    [SerializeField] private float baseMass = 1f;
    [SerializeField] private float buoyancyPerMass = 8f;
    [SerializeField] private float maxBuoyancyMultiplier = 5f;
    [SerializeField] private bool debugMassChanges = true;

    [Header("Wave Rolling Control")]
    [SerializeField] private float waveRollStrength = 2.5f;
    [SerializeField] private float rollResponseSpeed = 1.5f;
    [SerializeField] private float maxRollAngle = 12f;
    [SerializeField] private bool enableWaveRolling = true;

    [Header("Stability")]
    [SerializeField] private float stabilityForce = 0.3f;

    [Header("Force Limits - Anti-Break Protection")]
    [SerializeField] private float maxTotalForce = 50f;
    [SerializeField] private float maxTorqueLimit = 8f;
    [SerializeField] private float maxAngularVelocity = 180f;
    [SerializeField] private float forceSmoothing = 0.85f;
    [SerializeField] private bool enableForceProtection = true;

    [Header("VERTICAL MOVEMENT CONTROL")]
    [SerializeField] private float maxVerticalSpeed = 3f;
    [SerializeField] private float verticalDamping = 0.8f;
    [SerializeField] private bool enableSpeedLimit = true;
    [SerializeField] private float smoothBuoyancy = 0.5f;

    // Core components
    private Rigidbody2D rb;
    private WaterPhysics waterPhysics;
    private float currentRollAngle = 0f;
    private float rollVelocity = 0f;
    private Vector2 previousVelocity;
    private float currentDirectionMultiplier = 1f;
    private Vector2 smoothedForce = Vector2.zero;
    private float smoothedTorque = 0f;

    // NEW: Platform-based movement system
    private bool isRegisteredToPlatform = false;
    private bool movementActive = false;
    private float currentMovementDirection = 1f;
    private float lastMovementChange = 0f;
    private Vector2 currentMovementTarget;
    private Platform assignedPlatform;

    // NEW: Dynamic mass tracking
    private float lastKnownMass = 0f;
    private float effectiveBuoyancyForce = 6f;
    private float massCheckTimer = 0f;
    private const float MASS_CHECK_INTERVAL = 0.1f; // Check every 0.1 seconds instead of every frame
    private Rigidbody2D[] cachedChildRigidbodies;
    private Enemy[] cachedEnemies;
    private bool componentsCached = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        waterPhysics = WaterPhysics.Instance;

        if (floatPoints[0] == null || floatPoints[1] == null || floatPoints[2] == null)
        {
            Debug.LogError("BoatFloater: Float Points not assigned in inspector.");
        }

        // Initialize mass tracking with initial cache
        baseMass = rb.mass;
        RefreshComponentCache(); // Cache components immediately
        lastKnownMass = CalculateTotalMassOptimized();
        effectiveBuoyancyForce = buoyancyForce;

        // CRITICAL: Ensure buoyancy can overcome gravity for this mass
        float requiredBuoyancy = rb.mass * Physics2D.gravity.magnitude * 1.5f; // 1.5x gravity for good floating
        if (effectiveBuoyancyForce < requiredBuoyancy)
        {
            effectiveBuoyancyForce = requiredBuoyancy;
            Debug.Log($"AUTO-ADJUSTED: Buoyancy increased to {effectiveBuoyancyForce:F1} to support mass {rb.mass}");
        }

        if (debugMassChanges)
        {
            Debug.Log($"BoatFloater: Initial mass setup - Base: {baseMass}, Total: {lastKnownMass}, Buoyancy: {effectiveBuoyancyForce}");
        }

        // Auto-find boat sprite renderer if not assigned
        if (boatSpriteRenderer == null && useSpriteFip)
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.gameObject.name.ToLower().Contains("boat"))
                {
                    boatSpriteRenderer = renderer;
                    Debug.Log($"BoatFloater: Auto-found boat sprite: {renderer.gameObject.name}");
                    break;
                }
            }

            if (boatSpriteRenderer == null && renderers.Length > 0)
            {
                boatSpriteRenderer = renderers[0];
                Debug.Log($"BoatFloater: Using first SpriteRenderer: {renderers[0].gameObject.name}");
            }
        }

        UpdateDirectionMultiplier();

        // NEW: Check if already registered to platform (for prefabs that start on platforms)
        StartCoroutine(CheckForPlatformRegistration());

        if (debugMovement)
        {
            Debug.Log("BoatFloater: Initialized, waiting for platform registration to start movement");
        }
    }

    // OPTIMIZED: Check if boat gets registered to a platform (using cached references)
    private System.Collections.IEnumerator CheckForPlatformRegistration()
    {
        float checkTime = 0f;
        int checkCount = 0;
        const int MAX_CHECKS = 6; // Limit to 6 checks instead of continuous for 5 seconds

        // Cache platforms at start to avoid repeated FindObjectsOfType calls
        if (cachedPlatforms == null || cachedPlatforms.Length == 0)
        {
            cachedPlatforms = FindObjectsOfType<Platform>();
            if (debugMovement)
            {
                Debug.Log($"BoatFloater: Cached {cachedPlatforms.Length} platforms for registration checks");
            }
        }

        while (checkTime < 3f && checkCount < MAX_CHECKS) // Reduced from 5 seconds to 3 seconds
        {
            // OPTIMIZED: Use cached platforms instead of searching every time
            if (checkCount % 2 == 0) // Only search every other check
            {
                foreach (Platform platform in cachedPlatforms)
                {
                    if (platform != null && platform.assignedEnemies != null)
                    {
                        foreach (var enemy in platform.assignedEnemies)
                        {
                            if (enemy != null && enemy.transform.IsChildOf(this.transform))
                            {
                                OnRegisteredToPlatform(platform);
                                yield break;
                            }
                        }
                    }
                }
            }

            checkCount++;
            checkTime += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        if (debugMovement)
        {
            Debug.Log("BoatFloater: No platform registration detected after optimized search, starting movement anyway");
        }
        StartMovement();
    }

    // NEW: Called when boat crew gets registered to a platform
    public void OnRegisteredToPlatform(Platform platform)
    {
        if (isRegisteredToPlatform) return;

        isRegisteredToPlatform = true;
        assignedPlatform = platform;

        if (debugMovement)
        {
            Debug.Log($"BoatFloater: Registered to platform {platform.name}, starting movement!");
        }

        StartMovement();
    }

    // NEW: Start the boat movement system
    public void StartMovement()
    {
        if (movementActive) return;

        movementActive = true;
        lastMovementChange = Time.time;
        ChooseNewMovementDirection();

        if (debugMovement)
        {
            Debug.Log("BoatFloater: Movement started!");
        }
    }

    // NEW: Choose a new random movement direction
    void ChooseNewMovementDirection()
    {
        // Random direction: -1 (left) or 1 (right)
        currentMovementDirection = Random.Range(0, 2) == 0 ? -1f : 1f;

        // Update visual direction
        if (useSpriteFip && boatSpriteRenderer != null)
        {
            boatSpriteRenderer.flipX = currentMovementDirection < 0;
        }

        if (debugMovement)
        {
            string direction = currentMovementDirection > 0 ? "RIGHT" : "LEFT";
            Debug.Log($"BoatFloater: New movement direction: {direction}");
        }
    }

    void FixedUpdate()
    {
        if (!enableFloaterMovement || waterPhysics == null) return;

        previousVelocity = rb.velocity;

        // OPTIMIZED: Update buoyancy less frequently for performance
        if (enableDynamicBuoyancy)
        {
            UpdateDynamicBuoyancyOptimized();
        }

        // Update direction multiplier if scale changes
        if (adaptToScaleDirection)
        {
            UpdateDirectionMultiplier();
        }

        // NEW: Handle automatic boat movement
        if (enableAutomaticMovement && movementActive)
        {
            HandleBoatMovement();
        }

        ApplyBuoyancy();
        ApplyHorizontalForce();
        ApplyWaterResistance();

        if (enableWaveRolling)
        {
            ApplyWaveRolling();
        }

        ApplyStability();

        if (enableSpeedLimit)
        {
            LimitVerticalMovement();
        }

        if (enableForceProtection)
        {
            ApplyForceProtection();
        }
    }

    // NEW: Handle boat movement system
    void HandleBoatMovement()
    {
        // Check if it's time to change direction
        if (Time.time - lastMovementChange > movementChangeInterval)
        {
            if (Time.time - lastMovementChange > minMovementTime)
            {
                ChooseNewMovementDirection();
                lastMovementChange = Time.time;
            }
        }

        // Apply movement force in current direction
        if (IsInWater())
        {
            Vector2 moveForce = Vector2.right * currentMovementDirection * movementSpeed;
            moveForce = Vector2.ClampMagnitude(moveForce, maxMovementForce);

            rb.AddForce(moveForce);

            if (debugMovement)
            {
                Debug.DrawRay(transform.position, moveForce.normalized * 2f, Color.cyan);
            }
        }
    }

    void UpdateDirectionMultiplier()
    {
        if (useSpriteFip && boatSpriteRenderer != null)
        {
            currentDirectionMultiplier = boatSpriteRenderer.flipX ? -1f : 1f;
        }
        else
        {
            currentDirectionMultiplier = transform.localScale.x >= 0 ? 1f : -1f;
        }
    }

    void ApplyHorizontalForce()
    {
        if (Mathf.Abs(horizontalForce) > 0.01f && IsInWater())
        {
            float effectiveForce = Mathf.Clamp(horizontalForce, -maxHorizontalForce, maxHorizontalForce);

            if (adaptToScaleDirection)
            {
                effectiveForce *= currentDirectionMultiplier;
            }

            Vector2 horizontalForceVector = Vector2.right * effectiveForce;
            smoothedForce = Vector2.Lerp(smoothedForce, horizontalForceVector, (1f - forceSmoothing) * Time.fixedDeltaTime * 10f);
            rb.AddForce(smoothedForce);
        }
    }

    void ApplyBuoyancy()
    {
        int submergedPoints = 0;
        Vector2 totalForce = Vector2.zero;

        foreach (Transform point in floatPoints)
        {
            if (point == null) continue;

            Vector2 worldPos = point.position;
            float waterHeight = waterPhysics.GetWaterHeightAt(worldPos);
            float submersion = waterHeight - worldPos.y;

            if (submersion > 0)
            {
                submergedPoints++;

                float speedFactor = 1f;
                if (enableSpeedLimit && rb.velocity.y > 0)
                {
                    speedFactor = Mathf.Lerp(1f, smoothBuoyancy, rb.velocity.y / maxVerticalSpeed);
                }

                // NEW: Use dynamic buoyancy force instead of fixed buoyancy
                float force = submersion * effectiveBuoyancyForce * speedFactor;

                // Apply direction consideration to buoyancy distribution
                if (adaptToScaleDirection && currentDirectionMultiplier < 0)
                {
                    // Slightly modify buoyancy distribution when facing opposite direction
                    force *= 0.95f; // Small adjustment to prevent instability
                }

                totalForce += Vector2.up * force;
            }
        }

        if (submergedPoints > 0)
        {
            // Apply force protection before adding to rigidbody
            if (enableForceProtection)
            {
                totalForce = Vector2.ClampMagnitude(totalForce, maxTotalForce * (effectiveBuoyancyForce / buoyancyForce));
            }

            rb.AddForce(totalForce);
        }
    }

    void LimitVerticalMovement()
    {
        Vector2 velocity = rb.velocity;

        if (Mathf.Abs(velocity.y) > maxVerticalSpeed)
        {
            velocity.y = Mathf.Sign(velocity.y) * maxVerticalSpeed;
        }

        if (Mathf.Abs(velocity.y) > maxVerticalSpeed * 0.7f)
        {
            velocity.y *= verticalDamping;
        }

        float velocityChange = Mathf.Abs(velocity.y - previousVelocity.y);
        if (velocityChange > maxVerticalSpeed * 0.5f)
        {
            velocity.y = Mathf.Lerp(previousVelocity.y, velocity.y, 0.7f);
        }

        rb.velocity = velocity;
    }

    void ApplyWaveRolling()
    {
        if (floatPoints.Length < 3) return;

        float bowHeight = waterPhysics.GetWaterHeightAt(floatPoints[0].position);
        float sternHeight = waterPhysics.GetWaterHeightAt(floatPoints[2].position);

        float heightDifference = bowHeight - sternHeight;

        // Adjust wave rolling based on direction to prevent conflicts
        if (adaptToScaleDirection && currentDirectionMultiplier < 0)
        {
            heightDifference *= 0.8f; // Reduce rolling intensity when direction changes
        }

        float targetRollAngle = heightDifference * waveRollStrength;
        targetRollAngle = Mathf.Clamp(targetRollAngle, -maxRollAngle, maxRollAngle);

        currentRollAngle = Mathf.SmoothDamp(
            currentRollAngle,
            targetRollAngle,
            ref rollVelocity,
            1f / rollResponseSpeed,
            Mathf.Infinity,
            Time.fixedDeltaTime
        );

        float currentBoatAngle = transform.eulerAngles.z;
        if (currentBoatAngle > 180f) currentBoatAngle -= 360f;

        float angleDifference = Mathf.DeltaAngle(currentBoatAngle, currentRollAngle);
        float rollTorque = angleDifference * rollResponseSpeed;

        // Apply torque protection
        if (enableForceProtection)
        {
            rollTorque = Mathf.Clamp(rollTorque, -maxTorqueLimit, maxTorqueLimit);
            smoothedTorque = Mathf.Lerp(smoothedTorque, rollTorque, (1f - forceSmoothing) * Time.fixedDeltaTime * 8f);
            rb.AddTorque(smoothedTorque, ForceMode2D.Force);
        }
        else
        {
            rb.AddTorque(rollTorque, ForceMode2D.Force);
        }
    }

    void ApplyWaterResistance()
    {
        if (IsInWater())
        {
            // Adjust drag based on direction if needed
            float effectiveDrag = waterDrag;
            float effectiveAngularDrag = angularDrag;

            if (adaptToScaleDirection && currentDirectionMultiplier < 0)
            {
                // Slightly increase drag when facing opposite direction for stability
                effectiveDrag *= 1.1f;
                effectiveAngularDrag *= 1.2f;
            }

            rb.velocity *= effectiveDrag;
            rb.angularVelocity *= effectiveAngularDrag;
        }
    }

    void ApplyStability()
    {
        float currentAngle = transform.eulerAngles.z;
        if (currentAngle > 180f) currentAngle -= 360f;

        float stabilityTorque = -currentAngle * stabilityForce * Time.fixedDeltaTime;

        // Apply direction-aware stability adjustments
        if (adaptToScaleDirection && currentDirectionMultiplier < 0)
        {
            stabilityTorque *= 1.3f; // Increase stability when direction changes
        }

        // Apply force protection to stability torque
        if (enableForceProtection)
        {
            stabilityTorque = Mathf.Clamp(stabilityTorque, -maxTorqueLimit * 0.5f, maxTorqueLimit * 0.5f);
        }

        rb.AddTorque(stabilityTorque);
    }

    void ApplyForceProtection()
    {
        // Limit angular velocity to prevent excessive spinning
        if (Mathf.Abs(rb.angularVelocity) > maxAngularVelocity)
        {
            rb.angularVelocity = Mathf.Sign(rb.angularVelocity) * maxAngularVelocity;
        }

        // Clamp total velocity magnitude
        if (rb.velocity.magnitude > maxTotalForce)
        {
            rb.velocity = rb.velocity.normalized * maxTotalForce;
        }

        // Prevent extreme rotations that could cause breakage
        float currentAngle = transform.eulerAngles.z;
        if (currentAngle > 180f) currentAngle -= 360f;

        if (Mathf.Abs(currentAngle) > 45f)
        {
            // Apply corrective torque to prevent excessive rotation
            float correctionTorque = -currentAngle * 0.1f;
            rb.AddTorque(correctionTorque);
        }
    }

    bool IsInWater()
    {
        foreach (Transform point in floatPoints)
        {
            if (point != null)
            {
                Vector2 worldPos = point.position;
                float waterHeight = waterPhysics.GetWaterHeightAt(worldPos);
                if (waterHeight > worldPos.y) return true;
            }
        }
        return false;
    }

    // NEW: Public methods for boat control
    public void SetMovementEnabled(bool enabled)
    {
        enableFloaterMovement = enabled;
    }

    public void SetAutomaticMovementEnabled(bool enabled)
    {
        enableAutomaticMovement = enabled;

        if (enabled && !movementActive && isRegisteredToPlatform)
        {
            StartMovement();
        }
        else if (!enabled)
        {
            movementActive = false;
        }
    }

    public void SetHorizontalForce(float force)
    {
        horizontalForce = Mathf.Clamp(force, -maxHorizontalForce, maxHorizontalForce);
    }

    public void AddHorizontalForce(float additionalForce)
    {
        float newForce = horizontalForce + additionalForce;
        SetHorizontalForce(newForce);
    }

    public float GetCurrentDirectionMultiplier()
    {
        return currentDirectionMultiplier;
    }

    public void SetBoatSpriteRenderer(SpriteRenderer renderer)
    {
        boatSpriteRenderer = renderer;
    }

    // NEW: Status getters
    public bool IsMovementActive() => movementActive;
    public bool IsRegisteredToPlatform() => isRegisteredToPlatform;
    public float GetCurrentMovementDirection() => currentMovementDirection;
    public Platform GetAssignedPlatform() => assignedPlatform;

    // NEW: Manual control methods
    public void ForceStartMovement()
    {
        StartMovement();
    }

    public void StopMovement()
    {
        movementActive = false;
        if (debugMovement)
        {
            Debug.Log("BoatFloater: Movement stopped manually");
        }
    }

    public void SetMovementDirection(float direction)
    {
        currentMovementDirection = Mathf.Sign(direction);

        if (useSpriteFip && boatSpriteRenderer != null)
        {
            boatSpriteRenderer.flipX = currentMovementDirection < 0;
        }

        if (debugMovement)
        {
            string dir = currentMovementDirection > 0 ? "RIGHT" : "LEFT";
            Debug.Log($"BoatFloater: Movement direction set to {dir}");
        }
    }

    // OPTIMIZED: Calculate total mass with caching and less frequent updates
    float CalculateTotalMassOptimized()
    {
        // Cache components if not already cached or if we need to refresh
        if (!componentsCached)
        {
            RefreshComponentCache();
        }

        float totalMass = rb.mass;

        // Use cached components instead of GetComponentsInChildren every frame
        if (cachedChildRigidbodies != null)
        {
            foreach (Rigidbody2D childRb in cachedChildRigidbodies)
            {
                if (childRb != null && childRb != rb)
                {
                    totalMass += childRb.mass;
                }
            }
        }

        // Add gear mass for cached enemies
        if (cachedEnemies != null)
        {
            totalMass += cachedEnemies.Length * 0.5f; // 0.5f gear mass per enemy
        }

        return totalMass;
    }

    // NEW: Cache components to avoid expensive searches
    void RefreshComponentCache()
    {
        cachedChildRigidbodies = GetComponentsInChildren<Rigidbody2D>();
        cachedEnemies = GetComponentsInChildren<Enemy>();
        componentsCached = true;

        if (debugMassChanges)
        {
            Debug.Log($"CACHE REFRESH: Found {cachedChildRigidbodies?.Length ?? 0} rigidbodies, {cachedEnemies?.Length ?? 0} enemies");
        }
    }

    // OPTIMIZED: Update buoyancy force less frequently for performance
    void UpdateDynamicBuoyancyOptimized()
    {
        massCheckTimer += Time.fixedDeltaTime;

        // Only check mass every MASS_CHECK_INTERVAL seconds
        if (massCheckTimer >= MASS_CHECK_INTERVAL)
        {
            massCheckTimer = 0f;

            float currentTotalMass = CalculateTotalMassOptimized();

            // Use less sensitive threshold to avoid constant recalculations
            if (Mathf.Abs(currentTotalMass - lastKnownMass) > 0.5f)
            {
                lastKnownMass = currentTotalMass;

                // Calculate required buoyancy to support current total mass
                float requiredBuoyancy = currentTotalMass * Physics2D.gravity.magnitude * 2.0f;
                effectiveBuoyancyForce = Mathf.Max(buoyancyForce, requiredBuoyancy);
                maxTotalForce = Mathf.Max(50f, effectiveBuoyancyForce * 1.5f);

                if (debugMassChanges)
                {
                    Debug.Log($"MASS UPDATE: Total: {currentTotalMass:F2}, Buoyancy: {effectiveBuoyancyForce:F1}");
                }
            }
        }
    }

    // OPTIMIZED: Manually trigger buoyancy recalculation and refresh cache
    public void RecalculateBuoyancy()
    {
        componentsCached = false; // Force cache refresh
        RefreshComponentCache();
        lastKnownMass = 0f; // Force recalculation
        UpdateDynamicBuoyancyOptimized();

        if (debugMassChanges)
        {
            Debug.Log("BoatFloater: Manual buoyancy recalculation triggered with cache refresh");
        }
    }
}
